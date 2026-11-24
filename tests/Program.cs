using System.Net;
using System.Net.Sockets;
using System.Text;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using SteamSongPresence;

int checks = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    checks++; Console.WriteLine("PASS: " + name);
}

var snapshot = new PresenceState("playing", "献向镜水的月光 🎵", "作曲者", "Expert+", "Standard", true, 30, 180);
Check(snapshot.status.Contains("献向镜水的月光") && snapshot.isPlaying, "Chinese / emoji song captured");
Check(snapshot.WithPlayback(true, 31, 180).status.EndsWith("暂停"), "Pause retains current song");
Check(!snapshot.WithPlayback(false, 32, 180).isPaused, "Resume returns to playing");
Check(new PresenceState().status == "", "Menu clears external song activity");
Check(new PresenceState(elapsed: double.NaN, duration: double.PositiveInfinity).ToJson().Contains("\"elapsed\":0"), "Non-finite playback data sanitized");
string longText = TextLimit.Normalize(string.Concat(Enumerable.Repeat("光🎵", 100)), 128);
Check(Encoding.UTF8.GetByteCount(longText) <= 128 && !char.IsHighSurrogate(longText[^1]), "UTF-8 limit preserves complete emoji");
Check(TextLimit.Normalize("a\0\r\nb", 128) == "a b", "Control characters removed from Steam text");
Check(TextLimit.Normalize("a\ud800b", 128) == "a�b", "Malformed UTF-16 sanitized");

using (var server = new LoopbackServer(() => snapshot, 0))
using (var http = new HttpClient(new HttpClientHandler { UseProxy = false }))
{
    var url = $"http://127.0.0.1:{server.Port}/status";
    var response = await http.GetAsync(url);
    var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
    Check(response.StatusCode == HttpStatusCode.OK && parsed.Value<string>("song") == snapshot.song, "HTTP serves valid UTF-8 song JSON");
    Check(response.Headers.CacheControl?.NoStore == true, "Responses cannot reuse stale cached songs");
    snapshot = snapshot.WithPlayback(true, 33, 180);
    Check(JObject.Parse(await http.GetStringAsync(url)).Value<string>("state") == "paused", "HTTP follows live pause state");
    snapshot = new PresenceState();
    Check(JObject.Parse(await http.GetStringAsync(url)).Value<string>("status") == "", "HTTP clears song on menu transition");
    Check((await http.PostAsync(url, new StringContent("{}"))).StatusCode == HttpStatusCode.MethodNotAllowed, "HTTP refuses mutation requests");
    var request = new HttpRequestMessage(HttpMethod.Get, url); request.Headers.Host = "untrusted.example";
    Check((await http.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, "HTTP refuses non-loopback Host");
    Check((await http.GetAsync(url + "/unknown")).StatusCode == HttpStatusCode.NotFound, "HTTP rejects unknown routes");
    var parallel = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => http.GetStringAsync(url)));
    Check(parallel.All(x => JObject.Parse(x).Value<string>("state") == "menu"), "Concurrent readers see consistent snapshots");
}

string game = args.Length > 0 ? args[0] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "BSManager", "BSInstances", "1.40.8");
using (var main = AssemblyDefinition.ReadAssembly(Path.Combine(game, "Beat Saber_Data", "Managed", "Main.dll")))
{
    TypeDefinition Type(string name) => main.MainModule.Types.Single(t => t.Name == name);
    Check(Type("GameplayCoreInstaller").Fields.Any(f => f.Name == "_sceneSetupData" && f.FieldType.Name == "GameplayCoreSceneSetupData"), "Target game song hook and injected data field exist");
    Check(Type("GameplayCoreInstaller").Methods.Count(m => m.Name == "InstallBindings") == 1, "Target game song hook is unambiguous");
    Check(Type("AudioTimeSyncController").Methods.Any(m => m.Name == "Start"), "Target game audio clock hook exists");
    Check(Type("GamePause").Methods.Any(m => m.Name == "Pause") && Type("GamePause").Methods.Any(m => m.Name == "Resume"), "Target game pause / resume hooks exist");
    Check(Type("SteamRichPresencePlatformHandler").Methods.Any(m => m.Name == "SetPresence"), "Target game native Steam hook exists");
}
Console.WriteLine($"{checks} checks passed. Live Steam friends visibility / playtime still requires an in-game check.");
