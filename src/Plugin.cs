using System;
using HarmonyLib;
using IPA;
using Logger = IPA.Logging.Logger;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SteamSongPresence;

[Plugin(RuntimeOptions.SingleStartInit)]
public sealed class Plugin
{
    internal static Logger Log = null!;
    internal static volatile PresenceState Snapshot = new PresenceState();
    internal static AudioTimeSyncController? Clock;
    internal static bool Paused;
    private Harmony? harmony;
    private LoopbackServer? server;
    private GameObject? runner;
    private static string? lastSteamText;
    private static bool steamErrorReported;

    [Init]
    public Plugin(Logger logger) { Log = logger; }

    [OnStart]
    public void Start()
    {
        try
        {
            harmony = new Harmony("me.1000delta.beatsaber.steamsongpresence");
            harmony.PatchAll(typeof(Plugin).Assembly);
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            runner = new GameObject("SteamSongPresence");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<PresenceTicker>();
            try { server = new LoopbackServer(() => Snapshot); }
            catch (Exception ex) { Log.Warn("Local song bridge could not start: " + ex.Message); }
            Log.Info("Steam Song Presence " + typeof(Plugin).Assembly.GetName().Version + " started. Using the official #prelocalizedkey template.");
            if (server != null) Log.Info("Local song diagnostics: http://127.0.0.1:6749/status");
        }
        catch (Exception ex)
        {
            Stop(); Log.Error("Steam Song Presence could not initialize: " + ex);
        }
    }

    [OnExit]
    public void Stop()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneUnloaded -= SceneUnloaded;
        server?.Dispose(); server = null;
        harmony?.UnpatchSelf(); harmony = null;
        if (runner != null) UnityEngine.Object.Destroy(runner);
        runner = null; Clock = null; Snapshot = new PresenceState();
        // Steam owns its own shutdown / presence cleanup.
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "MenuCore") Reset();
    }
    private static void SceneUnloaded(Scene scene)
    {
        if (scene.name == "GameCore") Reset();
    }
    internal static void Reset()
    {
        Snapshot = new PresenceState(); Clock = null; Paused = false; UpdateSteam();
    }
    internal static void Begin(GameplayCoreSceneSetupData data)
    {
        var level = data.beatmapLevel;
        if (level == null) return;
        var key = data.beatmapKey;
        string difficulty = key.difficulty == BeatmapDifficulty.ExpertPlus ? "Expert+" : key.difficulty.ToString();
#if BS_146
        string characteristic = key.characteristic.ToString();
#else
        string characteristic = key.beatmapCharacteristic?.serializedName ?? "";
#endif
        Snapshot = new PresenceState("playing", level.songName, level.songAuthorName,
            difficulty, characteristic,
            (level.levelID ?? "").StartsWith("custom_level_", StringComparison.OrdinalIgnoreCase),
            duration: level.songDuration);
        Paused = false; lastSteamText = null; UpdateSteam();
        Log.Info("Current song: " + Snapshot.status);
    }
    internal static void Tick()
    {
        if (Snapshot.state != "menu" && Clock != null)
            Snapshot = Snapshot.WithPlayback(Paused, Clock.songTime, Clock.songLength);
        UpdateSteam();
    }
    internal static void SetPaused(bool paused)
    {
        Paused = paused;
        if (Snapshot.state != "menu")
            Snapshot = Snapshot.WithPlayback(paused, Snapshot.elapsed, Snapshot.duration);
        UpdateSteam();
    }
    internal static void UpdateSteam(bool force = false)
    {
        string text = Snapshot.nativeSteamStatus;
        if (!force && text == lastSteamText) return;
        try
        {
            // Beat Games publishes '#prelocalizedkey' = '%pre_localized%'. Fill
            // its substitution before selecting it, using the existing session.
            if (SteamUtils.GetAppID().m_AppId != 620980) return;
            bool valueSet = SteamFriends.SetRichPresence("pre_localized", text);
            bool statusSet = SteamFriends.SetRichPresence("status", text);
            bool displaySet = valueSet && SteamFriends.SetRichPresence("steam_display", "#prelocalizedkey");
            if (valueSet && statusSet && displaySet)
            {
                lastSteamText = text;
                steamErrorReported = false;
                Log.Debug("Steam friends status updated: " + text);
            }
            else if (!steamErrorReported)
            { Log.Warn("Steam rejected a presence update; will retry."); steamErrorReported = true; }
        }
        catch (Exception ex)
        {
            if (!steamErrorReported) { Log.Debug("Steam status pending: " + ex.Message); steamErrorReported = true; }
        }
    }
}

internal sealed class PresenceTicker : MonoBehaviour
{
    private float nextUpdate;
    private void Update()
    {
        if (Time.unscaledTime < nextUpdate) return;
        nextUpdate = Time.unscaledTime + 0.5f;
        Plugin.Tick();
    }
}

[HarmonyPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
internal static class SongPatch
{
    private static void Postfix(GameplayCoreSceneSetupData ____sceneSetupData)
    { try { Plugin.Begin(____sceneSetupData); } catch (Exception ex) { Plugin.Log.Error("Song capture failed: " + ex); } }
}
[HarmonyPatch(typeof(AudioTimeSyncController), "Start")]
internal static class ClockPatch
{
    private static void Postfix(AudioTimeSyncController __instance) { Plugin.Clock = __instance; }
}
[HarmonyPatch(typeof(GamePause), nameof(GamePause.Pause))]
internal static class PausePatch
{
    private static void Postfix(GamePause __instance) { Plugin.SetPaused(__instance.isPaused); }
}
[HarmonyPatch(typeof(GamePause), nameof(GamePause.Resume))]
internal static class ResumePatch
{
    private static void Postfix(GamePause __instance) { Plugin.SetPaused(__instance.isPaused); }
}
[HarmonyPatch(typeof(SteamRichPresencePlatformHandler), nameof(SteamRichPresencePlatformHandler.SetPresence))]
internal static class NativePresencePatch
{
    // Restore the official template and its substitution after native updates.
    private static void Postfix() { Plugin.UpdateSteam(true); }
}
