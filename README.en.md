# Steam Song Presence

Show your current Beat Saber song, difficulty and pause state in the **Steam friends list**, using the game's existing Steam session.

```text
Playing: Medicine [Expert+]
Paused: Medicine [Expert+]
In menus
```

No additional login or companion application is required. The plugin keeps the original Beat Saber App ID (620980) and only updates Rich Presence. Steam continues to track the game normally.

## Compatibility

Windows x64 / Steam / BSIPA. Official and custom song metadata are read from the game. The available builds target different game APIs and are **not interchangeable**.

| Game version | Status |
| --- | --- |
| 1.40.8 | v0.2.0 tested in game; Steam rendering, pause, menu clearing and native playtime verified |
| 1.46.0 | v0.3.0 compatibility work and live testing in progress; not yet declared supported |

## Install

1. Close Beat Saber and install a compatible [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded/releases).
2. Download the release ZIP for your exact game version.
3. Extract it into that game's directory so `Plugins/SteamSongPresence.dll` exists.
4. Launch the selected game instance with Steam signed in.

For BSManager, install into the instance you actually launch. Remove the DLL while the game is closed to uninstall. Restart the game after replacing the DLL. The plugin uses Harmony and Newtonsoft.Json supplied by BSIPA; it does not bundle game assemblies or other mods.

## How it works

Beat Saber's published Steam localization contains `#prelocalizedkey = %pre_localized%`. The plugin sets `pre_localized` to the song text, selects `steam_display=#prelocalizedkey`, and also supplies the legacy `status` key. It restores these values after the game's native presence updates.

The template was confirmed through Steam's public app localization response and the [official Rich Presence Tester](https://steamcommunity.com/dev/testrichpresence). See [Valve's SetRichPresence documentation](https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence).

The plugin does not initialize or shut down Steam, change the App ID, send `GamesPlayed`, alter Steam configuration or fabricate playtime. It has no external network requests. Local status changes are checked every 0.5 seconds; friend visibility also depends on Steam propagation and privacy settings.

## Local diagnostics

`http://127.0.0.1:6749/status` is a read-only loopback endpoint containing song, artist, difficulty, characteristic, pause state and progress. It is not needed for Steam display. It binds only to loopback, rejects foreign Host headers, and sends no CORS headers. Other programs running as the local user can read it. A port conflict does not stop Steam updates.

Search the game's `Logs/_latest.log` for `Steam Song Presence`, `Current song` or `Steam friends status updated`. Validation scope and limitations are in [VALIDATION.md](VALIDATION.md).

## Build

Install .NET SDK 6+ and the .NET Framework 4.7.2 targeting pack. Game and BSIPA assemblies must already exist in your selected installation.

```powershell
./build.ps1 -GameDir 'C:\Path\To\Beat Saber'
```

The script reads the actual game version from Unity assets, selects the appropriate Steamworks reference and game API, embeds a matching manifest, runs the checks, and produces `dist/SteamSongPresence-<version>-bs<game-version>.zip`. The ZIP contains only the plugin DLL under `Plugins`, as required by BeatMods.

The default build target directory is `%USERPROFILE%\BSManager\BSInstances\1.40.8`. The repository's `install.ps1 -GameDir ...` installs a locally built DLL for the detected game version.

## License

[MIT](LICENSE). Beat Saber, Steam, BSIPA and their respective libraries remain the property of their owners. This is an independent community project.
