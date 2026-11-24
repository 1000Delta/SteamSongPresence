# Steam Song Presence

[中文说明](README.md)

[GitHub Releases](https://github.com/1000Delta/SteamSongPresence/releases) · [BeatMods](https://beatmods.com/mods/8110) · [Public build logs](https://github.com/1000Delta/SteamSongPresence/actions/workflows/release.yml)

A BSIPA plugin that displays the current Beat Saber song, difficulty and pause state in the Steam friends list. Supports official and custom songs.

```text
Playing: Medicine [Expert+]
Paused: Medicine [Expert+]
In menus
```

## Supported versions

| Beat Saber | Project target | Package |
| --- | --- | --- |
| 1.40.8 | Project-recommended version for a fixed BSManager instance | [SteamSongPresence-0.5.0-bs1.40.8.zip](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.5.0/SteamSongPresence-0.5.0-bs1.40.8.zip) |
| 1.46.0 | Official latest version, verified against the Steam public installation on 2026-11-25 | [SteamSongPresence-0.5.0-bs1.46.0.zip](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.5.0/SteamSongPresence-0.5.0-bs1.46.0.zip) |

The recommendation is this project's choice. Both targets share a plugin version number and are compiled against their respective game assemblies. Install each package into its matching instance. Current support is limited to these two game versions.

See [VALIDATION.md](VALIDATION.md) for v0.5.0 live regression and release status.

## Installation

Requirements: Windows x64, Steam, the matching Beat Saber version and BSIPA. The 1.40.8 build requires BSIPA 4.3.5+; the 1.46.0 build requires BSIPA 4.3.7+. Harmony is supplied by BSIPA.

1. Close Beat Saber and download the matching installation ZIP.
2. In BSManager, select that **game instance → Mods**.
3. Drag the ZIP onto the Mods page, or select **⋮ → Import mods**.
4. Launch that instance through Steam or BSManager and check its Steam friends status.

For manual installation, extract `Plugins/SteamSongPresence.dll` into the matching game directory. After first installing BSIPA or upgrading a game directory, launch to the menu and exit once before importing this plugin so the loader completes its version migration. Different BSManager instances manage separate plugin directories.

Use the named game-version installation asset. GitHub's **Source code** archives contain source. See the [official BSManager import guide](https://github.com/Zagrios/bs-manager/wiki/install-mods-on-your-version).

Restart the game after replacing the DLL. To uninstall, close the game and remove `Plugins/SteamSongPresence.dll`.

## Implementation

Postfix patches read the song name, difficulty and pause state during game callbacks. Steam Rich Presence uses the current game's Steamworks context and App 620980's published template:

```text
#prelocalizedkey = %pre_localized%
```

The plugin writes `pre_localized` and `status`, then selects `steam_display=#prelocalizedkey`. A postfix restores the song text after the game's native presence updates. Pending updates are checked every 0.5 seconds; successful unchanged text is deduplicated.

Game callbacks, Steam updates and independent exit-cleanup stages use exception guards. Repeated failures are reported once per operation until recovery. Search the game's `Logs` directory for `Steam Song Presence`, `Current song` and `Steam friends status updated`. Friend-side visibility depends on Steam client propagation.

References: [Valve SetRichPresence](https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence), [official Rich Presence Tester](https://steamcommunity.com/dev/testrichpresence).

## Build and validation

Install .NET SDK 6+ and the .NET Framework 4.7.2 targeting pack:

```powershell
./build.ps1 -GameDir 'C:\Path\To\Beat Saber'
```

The script detects the game version, selects its references and embeds a matching manifest. Checks cover state transitions, UTF-8 limits, exception isolation, Steam rejection and recovery, forced-refresh retries and game-hook metadata.

GitHub Actions independently compiles, checks and packages both targets. Each ZIP contains `Plugins/SteamSongPresence.dll`, with a separate build report, SHA-256 checksums and build provenance. Candidates awaiting live validation are marked Pre-release. The same attachments are promoted after validation, followed by BeatMods submission.

[Build transparency and security](SECURITY.md) · [Validation scope](VALIDATION.md) · [MIT license](LICENSE)
