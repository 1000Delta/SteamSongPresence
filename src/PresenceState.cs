using System;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SteamSongPresence;

// Immutable snapshots are passed from Unity's thread to the HTTP worker.
public sealed class PresenceState
{
    public string state { get; }
    public string song { get; }
    public string artist { get; }
    public string difficulty { get; }
    public string characteristic { get; }
    public bool isCustom { get; }
    public bool isPlaying => state == "playing";
    public bool isPaused => state == "paused";
    public double elapsed { get; }
    public double duration { get; }
    public string updatedUtc { get; }
    // Empty in menus so an external client can clear stale song activity.
    public string status => state == "menu" ? "" : TextLimit.Normalize(
        $"Beat Saber · {song} [{difficulty}]" + (isPaused ? " · 暂停" : ""), 128);
    public string nativeSteamStatus => state == "menu" ? "In menus" : TextLimit.Normalize(
        $"{(isPaused ? "Paused" : "Playing")}: {song} [{difficulty}]", 255);

    public PresenceState(string state = "menu", string song = "", string artist = "",
        string difficulty = "", string characteristic = "", bool isCustom = false,
        double elapsed = 0, double duration = 0)
    {
        this.state = state; this.song = song; this.artist = artist;
        this.difficulty = difficulty; this.characteristic = characteristic;
        this.isCustom = isCustom;
        this.elapsed = Finite(elapsed); this.duration = Finite(duration);
        updatedUtc = DateTime.UtcNow.ToString("O");
    }

    public PresenceState WithPlayback(bool paused, double position, double length) =>
        new PresenceState(paused ? "paused" : "playing", song, artist, difficulty,
            characteristic, isCustom, position, length);
    public string ToJson() => JsonConvert.SerializeObject(this);
    private static double Finite(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0 : Math.Max(0, value);
}

internal static class TextLimit
{
    public static string Normalize(string value, int maxBytes)
    {
        var clean = Regex.Replace(value ?? "", @"[\p{Cc}\p{Z}\s]+", " ").Trim();
        var result = new StringBuilder();
        int bytes = 0;
        for (int i = 0; i < clean.Length; i++)
        {
            int length = char.IsHighSurrogate(clean[i]) && i + 1 < clean.Length &&
                char.IsLowSurrogate(clean[i + 1]) ? 2 : 1;
            string scalar = clean.Substring(i, length);
            // Malformed UTF-16 must not leak a dangling surrogate into Steam.
            if (length == 1 && char.IsSurrogate(clean[i])) scalar = "�";
            int count = Encoding.UTF8.GetByteCount(scalar);
            if (bytes + count > maxBytes) break;
            result.Append(scalar); bytes += count; i += length - 1;
        }
        return result.ToString();
    }
}
