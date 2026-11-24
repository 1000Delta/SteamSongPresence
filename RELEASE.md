Displays the current Beat Saber song, difficulty and pause state in the Steam friends list.

## v0.5.0

- Removes the HTTP diagnostic service and the audio-clock hook used for its progress data.
- Retains song name, difficulty and pause state for native Steam Rich Presence.
- Adds exception guards for game callbacks, the Unity ticker and independent exit-cleanup stages. Error-reporting failures are contained, and repeated errors are suppressed until recovery.
- Retries rejected Steam writes and failed forced refreshes, including when the text has not changed.
- Includes 44 automated state, fault-injection, Steam-adapter and assembly checks.

**Beat Saber 1.40.8 live regression completed using the Actions-built v0.5.0 package:** consecutive custom songs, pause, resume, menu clearing and normal exit. The user confirmed the song display, native Steam playtime increased across the test sessions, and plugin logs recorded successful state updates.

## Packages

- **Beat Saber 1.40.8:** project-recommended version for a fixed BSManager instance; BSIPA 4.3.5+.

Download the ZIP for your exact game version. In BSManager, select **game instance → Mods** and drag in the ZIP, or use **⋮ → Import mods**. Each package contains `Plugins/SteamSongPresence.dll`. After first installing BSIPA or upgrading a directory, launch to the menu and exit once before importing the plugin.

The installation package is compiled, checked and published by the public Actions workflow. The build report, SHA-256 checksums and GitHub artifact attestation identify the source and output. This release targets 1.40.8.

[Validation details](https://github.com/1000Delta/SteamSongPresence/blob/main/VALIDATION.md) · [Build transparency](https://github.com/1000Delta/SteamSongPresence/blob/main/SECURITY.md)
