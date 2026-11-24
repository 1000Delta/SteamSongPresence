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
    private static Logger? log;
    private static bool running;
    private static PresenceState state = PresenceState.Menu;
    private static readonly CallbackGuard Guard = new CallbackGuard((operation, error) =>
    {
        if (operation == "Steam presence update")
            log?.Warn("Steam presence pending: " + error.Message);
        else log?.Error(operation + " failed: " + error);
    });
    private static readonly SteamPresence Presence = new SteamPresence(
        () => SteamUtils.GetAppID().m_AppId,
        (key, value) => SteamFriends.SetRichPresence(key, value));
    private Harmony? harmony;
    private GameObject? runner;

    [Init]
    public Plugin(Logger logger) { log = logger; }

    [OnStart]
    public void Start()
    {
        if (running) return;
        if (!Guard.Run("Plugin startup", () =>
        {
            state = PresenceState.Menu;
            Presence.Invalidate();
            harmony = new Harmony("me.1000delta.beatsaber.steamsongpresence");
            harmony.PatchAll(typeof(Plugin).Assembly);
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            runner = new GameObject("SteamSongPresence");
            UnityEngine.Object.DontDestroyOnLoad(runner);
            runner.AddComponent<PresenceTicker>();
            running = true;
            log?.Info("Steam Song Presence " + typeof(Plugin).Assembly.GetName().Version +
                " started. Native Steam presence only; no HTTP listener.");
        })) Stop();
    }

    [OnExit]
    public void Stop()
    {
        // Disable callbacks first. Every cleanup stage still runs if another fails.
        running = false;
        state = PresenceState.Menu;
        Presence.Invalidate();
        Guard.Run("Unsubscribe scene loaded", () => SceneManager.sceneLoaded -= SceneLoaded);
        Guard.Run("Unsubscribe scene unloaded", () => SceneManager.sceneUnloaded -= SceneUnloaded);
        Guard.Run("Remove plugin patches", () => { harmony?.UnpatchSelf(); harmony = null; });
        Guard.Run("Destroy presence ticker", () =>
        {
            if (runner != null) UnityEngine.Object.Destroy(runner);
            runner = null;
        });
        // The game owns Steam shutdown and its final presence cleanup.
    }

    internal static void RunSafely(string operation, Action action)
    {
        if (running) Guard.Run(operation, action);
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode) =>
        RunSafely("Scene loaded callback", () =>
        {
            if (scene.name == "MainMenu" || scene.name == "MenuCore") Reset();
        });

    private static void SceneUnloaded(Scene scene) =>
        RunSafely("Scene unloaded callback", () =>
        {
            if (scene.name == "GameCore") Reset();
        });

    private static void Reset()
    {
        state = PresenceState.Menu;
        Publish();
    }

    internal static void SongStarted(GameplayCoreSceneSetupData data) =>
        RunSafely("Song capture callback", () =>
        {
            var level = data?.beatmapLevel;
            if (level == null) { Reset(); return; }
            var difficulty = data!.beatmapKey.difficulty;
            state = PresenceState.ForSong(level.songName,
                difficulty == BeatmapDifficulty.ExpertPlus ? "Expert+" : difficulty.ToString());
            Presence.Invalidate();
            Publish();
            log?.Info("Current song: " + state.SteamText);
        });

    internal static void PauseChanged(GamePause gamePause) =>
        RunSafely("Pause callback", () =>
        {
            state = state.WithPaused(gamePause.isPaused);
            Publish();
        });

    internal static void Tick() => Publish();
    internal static void NativePresenceChanged() => Publish(true);

    private static void Publish(bool force = false) =>
        RunSafely("Steam presence update", () =>
        {
            if (Presence.Update(state.SteamText, force))
                log?.Debug("Steam friends status updated: " + state.SteamText);
        });
}

internal sealed class PresenceTicker : MonoBehaviour
{
    private float nextUpdate;
    private Action? updateAction;
    private void Update() => Plugin.RunSafely("Presence ticker callback",
        updateAction ?? (updateAction = UpdateCore));

    private void UpdateCore()
    {
        float now = Time.unscaledTime;
        if (now < nextUpdate) return;
        nextUpdate = now + 0.5f;
        Plugin.Tick();
    }
}

[HarmonyPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
internal static class SongPatch
{
    private static void Postfix(GameplayCoreSceneSetupData ____sceneSetupData) =>
        Plugin.SongStarted(____sceneSetupData);
}

[HarmonyPatch(typeof(GamePause), nameof(GamePause.Pause))]
internal static class PausePatch
{
    private static void Postfix(GamePause __instance) => Plugin.PauseChanged(__instance);
}

[HarmonyPatch(typeof(GamePause), nameof(GamePause.Resume))]
internal static class ResumePatch
{
    private static void Postfix(GamePause __instance) => Plugin.PauseChanged(__instance);
}

[HarmonyPatch(typeof(SteamRichPresencePlatformHandler), nameof(SteamRichPresencePlatformHandler.SetPresence))]
internal static class NativePresencePatch
{
    private static void Postfix() => Plugin.NativePresenceChanged();
}
