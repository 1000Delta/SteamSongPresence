# Validation record

Date: 2026-11-25 (Asia/Shanghai).

## Automated checks

- .NET Framework 4.7.2 / Windows x64 plugin, compiled against the selected game's assemblies.
- v0.2.0 and the v0.3.0 build for 1.40.8: zero build warnings/errors, 21 checks passed.
- Checks cover Unicode/UTF-8 limits, malformed UTF-16, control characters, pause/resume state, menu clearing, local HTTP JSON, foreign Host rejection, read-only requests, concurrent snapshots, and the actual Harmony targets in the selected game.
- State tests and method-existence checks do not substitute for a live game test.
- The same 21 checks also passed locally against pinned public 1.40.8 reference assemblies, using the official BSIPA Mono.Cecil implementation for the test runner. Package checks confirmed exactly one DLL and matching embedded metadata. The public Actions run separately records its actual result and toolchain.

## Beat Saber 1.40.8

Live test performed with v0.2.0 on an actual BSManager 1.40.8_7379 instance:

- Plugin loaded and captured the custom song Medicine, Sasuke Haraguchi, Expert+ / Standard.
- Pause diagnostics reported `state=paused`, `isCustom=true`, elapsed 23.955 seconds, duration 123.352 seconds.
- Steam's official Rich Presence Tester rendered `Playing: Medicine [Expert+]`, with `pre_localized`, `status` and `steam_display` present.
- The English fallback notice for `#prelocalizedkey` did not prevent successful rendering.
- Game logs showed Playing → Paused → In menus. The tester reported resuming before leaving, but a separate resume transition was not captured in the log; it is not counted as a completed live verification.
- Steam's native recorded playtime increased by two minutes. After normal exit, App 620980 reported `Running=0`, no game process remained and port 6749 was no longer listening.
- Continuous song changes and other songs were not tested in this session.

## Beat Saber 1.46.0

The Steam public installation was identified from `globalgamemanagers` as 1.46.0_34060 / Unity 6000.3.19f1, build 26247017. A stale `BeatSaberVersion.txt` from an earlier BSIPA installation was disregarded.

Compatibility changes account for the renamed Steamworks assembly and the new `BeatmapKey.characteristic` enum. Live test completed with v0.3.1 on the actual Steam 1.46.0 installation using FPFC:

- On the first launch, BSIPA migrated a plugin directory left over from an old version of the same Steam installation. This did not affect the separate BSManager 1.40.8 instance. Importing via `IPA/Pending/Plugins` after the migration allowed normal loading on restart.
- Captured the custom song `[Super ONGEKI Music Pack] Random Access Emotions`, artist aran, Expert+ / Standard, and correctly reported its pause state and duration.
- Game logs confirmed successful native Steam presence updates through Playing → Paused → Playing, including repeated resume transitions.
- Steam's native playtime increased by two minutes. After exit, `Running=0`, no game process remained and port 6749 was no longer listening.
- A separate return-to-menu transition was not captured in this live test; menu clearing was live-verified on 1.40.8 and covered by state checks.
- v0.4.0 builds both game targets against pinned public references, using explicit BSIPA build dependencies. Both local public-reference builds passed 21 checks and package metadata checks. Public Actions runs record their actual results separately.

## Steam integration

The published localization response for App 620980 contains `#prelocalizedkey = %pre_localized%`. The plugin supplies its substitution through the game's existing Steam session and restores it after native presence updates. It does not call Steam Init/Shutdown, change App IDs, send GamesPlayed or require a companion application.

## BeatMods duplicate check

- The public 1.46.0 API listing returned an empty array.
- The most recent version with public listings, 1.44.1, returned 115 projects. Relevant status integrations were DiscordCore and bsrpc (Discord), with no matching Steam song-status plugin found in project names, summaries and descriptions.
- Sources: [1.46.0 public API](https://beatmods.com/api/v3/mods?gameName=beatsaber&gameVersion=1.46.0), [1.44.1 public API](https://beatmods.com/api/v3/mods?gameName=beatsaber&gameVersion=1.44.1).
- This check covers the public catalog at the time of the query, not private submissions.
