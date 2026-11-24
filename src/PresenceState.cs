using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamSongPresence;

internal enum PlaybackState { Menu, Playing, Paused }

// Only the song and difficulty needed by Steam are retained, on the main thread.
internal sealed class PresenceState
{
    public static readonly PresenceState Menu = new PresenceState(PlaybackState.Menu, "", "");
    public PlaybackState State { get; }
    public string Song { get; }
    public string Difficulty { get; }
    public string SteamText { get; }

    private PresenceState(PlaybackState state, string song, string difficulty)
    {
        State = state; Song = song; Difficulty = difficulty;
        SteamText = state == PlaybackState.Menu ? "In menus" : TextLimit.Normalize(
            $"{(state == PlaybackState.Paused ? "Paused" : "Playing")}: {song} [{difficulty}]", 255);
    }
    public static PresenceState ForSong(string song, string difficulty) =>
        new PresenceState(PlaybackState.Playing, song ?? "", difficulty ?? "");
    public PresenceState WithPaused(bool paused) => State == PlaybackState.Menu ? this :
        new PresenceState(paused ? PlaybackState.Paused : PlaybackState.Playing, Song, Difficulty);
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
