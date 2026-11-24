using System;

namespace SteamSongPresence;

// The delegates call the game's existing Steam session, on Unity's main thread.
// Failures propagate only as far as CallbackGuard, and remain retryable.
internal sealed class SteamPresence
{
    private readonly Func<uint> appId;
    private readonly Func<string, string, bool> setPresence;
    private string? lastText;

    public SteamPresence(Func<uint> appId, Func<string, string, bool> setPresence)
    { this.appId = appId; this.setPresence = setPresence; }

    public void Invalidate() { lastText = null; }

    public bool Update(string text, bool force = false)
    {
        if (!force && text == lastText) return false;
        // A failed forced refresh must also be retried by the normal ticker.
        lastText = null;
        if (appId() != 620980) return false;
        bool valueSet = setPresence("pre_localized", text);
        bool statusSet = setPresence("status", text);
        bool displaySet = valueSet && setPresence("steam_display", "#prelocalizedkey");
        if (!valueSet || !statusSet || !displaySet)
            throw new InvalidOperationException("Steam rejected a presence update; will retry.");
        lastText = text;
        return true;
    }
}
