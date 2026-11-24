using System.Text;
using Mono.Cecil;
using SteamSongPresence;

int checks = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    checks++; Console.WriteLine("PASS: " + name);
}

var playing = PresenceState.ForSong("献向镜水的月光 🎵", "Expert+");
Check(playing.State == PlaybackState.Playing && playing.SteamText == "Playing: 献向镜水的月光 🎵 [Expert+]", "Native Steam song text preserves Chinese and emoji");
var paused = playing.WithPaused(true);
Check(paused.State == PlaybackState.Paused && paused.SteamText == "Paused: 献向镜水的月光 🎵 [Expert+]", "Pause retains the song and difficulty");
Check(paused.WithPaused(false).SteamText == playing.SteamText, "Resume restores playing text");
Check(playing.State == PlaybackState.Playing, "State transitions do not mutate previous state");
Check(PresenceState.Menu.SteamText == "In menus", "Menu clears the previous song from native Steam text");
Check(PresenceState.Menu.WithPaused(true) == PresenceState.Menu, "A late pause callback cannot restore a song in menus");
Check(PresenceState.ForSong(null!, null!).SteamText == "Playing: []", "Missing metadata cannot throw while formatting");
var longState = PresenceState.ForSong(string.Concat(Enumerable.Repeat("光🎵", 200)), "Expert+");
Check(Encoding.UTF8.GetByteCount(longState.SteamText) <= 255 && !char.IsHighSurrogate(longState.SteamText[^1]), "Actual native Steam text stays within 255 UTF-8 bytes without splitting emoji");
Check(TextLimit.Normalize("a\0\r\nb", 255) == "a b", "Control characters and line breaks are removed");
Check(TextLimit.Normalize("a\ud800b", 255) == "a�b", "Malformed UTF-16 cannot reach Steam");

var errors = new List<string>();
var guard = new CallbackGuard((operation, _) => errors.Add(operation));
Check(!guard.Run("callback", () => throw new InvalidOperationException("injected")), "A callback exception does not escape");
guard.Run("callback", () => throw new InvalidOperationException("injected again"));
Check(errors.Count == 1, "Repeated callback failures do not flood logs");
Check(guard.Run("callback", () => { }), "A callback can recover after failure");
guard.Run("callback", () => throw new InvalidOperationException("new failure"));
Check(errors.Count == 2, "A failure after recovery is reported again");
var brokenLogger = new CallbackGuard((_, _) => throw new IOException("logger unavailable"));
Check(!brokenLogger.Run("callback", () => throw new InvalidOperationException()), "A failing logger cannot leak the original exception or its own exception");
bool cleaned = false;
brokenLogger.Run("first cleanup", () => throw new InvalidOperationException());
brokenLogger.Run("second cleanup", () => cleaned = true);
Check(cleaned, "Independent cleanup stages continue after an earlier failure");

var calls = new List<(string Key, string Value)>();
uint appId = 620980;
bool appReady = true;
bool throwWrite = false;
string? rejectedKey = null;
var publisher = new SteamPresence(
    () => appReady ? appId : throw new InvalidOperationException("Steam not initialized"),
    (key, value) =>
    {
        calls.Add((key, value));
        if (throwWrite) throw new InvalidOperationException("Steam write unavailable");
        return key != rejectedKey;
    });

Check(publisher.Update(playing.SteamText), "A successful update is committed");
Check(calls.SequenceEqual(new[] { ("pre_localized", playing.SteamText), ("status", playing.SteamText), ("steam_display", "#prelocalizedkey") }), "Template substitution is written before steam_display, using only the intended keys");
calls.Clear();
Check(!publisher.Update(playing.SteamText) && calls.Count == 0, "Unchanged successful text is not resent");
Check(publisher.Update(playing.SteamText, true) && calls.Count == 3, "Native game updates can force restoration of the same text");
calls.Clear();
publisher.Invalidate();
Check(publisher.Update(playing.SteamText) && calls.Count == 3, "A new song session can invalidate the deduplication cache");
calls.Clear();
Check(publisher.Update(paused.SteamText) && calls[0].Value.StartsWith("Paused:"), "Pause is published as native Steam text");
Check(publisher.Update(PresenceState.Menu.SteamText) && calls[^1].Value == "#prelocalizedkey", "Menu state uses the same official template");

foreach (string key in new[] { "pre_localized", "status", "steam_display" })
{
    publisher.Invalidate(); calls.Clear(); rejectedKey = key;
    Check(!guard.Run("publish", () => publisher.Update(playing.SteamText)), $"Rejected {key} is contained by the guard");
    if (key == "pre_localized")
        Check(calls.All(call => call.Key != "steam_display"), "A rejected substitution does not select the template");
    rejectedKey = null; calls.Clear();
    Check(guard.Run("publish", () => publisher.Update(playing.SteamText)) && calls.Count == 3, $"Rejected {key} remains retryable");
}

rejectedKey = "steam_display";
Check(!guard.Run("publish", () => publisher.Update(playing.SteamText, true)), "A failed forced refresh is contained");
rejectedKey = null; calls.Clear();
Check(guard.Run("publish", () => publisher.Update(playing.SteamText)) && calls.Count == 3, "Normal polling retries a failed forced refresh even if the text did not change");

publisher.Invalidate(); appReady = false; calls.Clear();
Check(!guard.Run("publish", () => publisher.Update(playing.SteamText)) && calls.Count == 0, "Steam initialization failure is contained before any write");
appReady = true;
Check(guard.Run("publish", () => publisher.Update(playing.SteamText)) && calls.Count == 3, "Existing Steam session can become ready without plugin initialization calls");
publisher.Invalidate(); throwWrite = true;
Check(!guard.Run("publish", () => publisher.Update(paused.SteamText)), "A Steam write exception is contained");
throwWrite = false; calls.Clear();
Check(guard.Run("publish", () => publisher.Update(paused.SteamText)) && calls.Count == 3, "A Steam write exception does not poison retries");
publisher.Invalidate(); appId = 480; calls.Clear();
Check(!publisher.Update(playing.SteamText) && calls.Count == 0, "Another App ID never receives presence writes");
appId = 620980;
Check(publisher.Update(playing.SteamText), "Wrong-App-ID rejection does not poison later retries");

string game = args[0];
using (var main = AssemblyDefinition.ReadAssembly(Path.Combine(game, "Beat Saber_Data", "Managed", "Main.dll")))
{
    TypeDefinition Type(string name) => main.MainModule.Types.Single(t => t.Name == name);
    Check(Type("GameplayCoreInstaller").Fields.Any(f => f.Name == "_sceneSetupData" && f.FieldType.Name == "GameplayCoreSceneSetupData"), "Game song hook field exists");
    Check(Type("GameplayCoreInstaller").Methods.Count(m => m.Name == "InstallBindings") == 1, "Game song hook is unambiguous");
    Check(Type("GamePause").Methods.Any(m => m.Name == "Pause") && Type("GamePause").Methods.Any(m => m.Name == "Resume"), "Game pause and resume hooks exist");
    Check(Type("SteamRichPresencePlatformHandler").Methods.Any(m => m.Name == "SetPresence"), "Game native Steam presence hook exists");
}
using (var plugin = AssemblyDefinition.ReadAssembly(args[1]))
{
    Check(!plugin.MainModule.GetTypeReferences().Any(t => t.FullName.StartsWith("System.Net.") || t.FullName.StartsWith("Newtonsoft.Json.")), "Compiled plugin has no HTTP/socket or JSON type references");
    Check(!plugin.MainModule.Types.Any(t => t.Name == "LoopbackServer" || t.Name == "ClockPatch"), "Compiled plugin contains neither diagnostic server nor obsolete audio-clock patch");
}
Console.WriteLine($"{checks} checks passed. Live Unity callbacks, Steam visibility and playtime still require in-game validation.");
