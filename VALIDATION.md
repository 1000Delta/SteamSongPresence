# Validation record

Date: 2026-11-25 (Asia/Shanghai).

## v0.5.0 automated checks

Each game target runs 44 checks covering native Steam text, pause/resume/menu transitions, malformed UTF-16 and UTF-8 limits, callback fault isolation, logging faults, independent cleanup stages, Steam write rejection and recovery, wrong App IDs, duplicate suppression and failed forced-refresh retries.

Mono.Cecil checks the selected game's patch targets and the compiled plugin. The plugin assembly is checked for diagnostic-server/audio-clock types and HTTP/socket/JSON type references. Package checks require one DLL and matching embedded manifest, target game and assembly versions.

Fault-injection checks exercise the same guard and Steam-adapter classes used in the plugin. They do not execute Unity native code or prove compatibility with every other mod. Actual build results and toolchain inputs are recorded by the public Actions runs and per-target BUILDINFO files.

## v0.5.0 live regression

Pending. Both final Actions-built packages must be installed into their matching game instances and checked for song capture, pause, resume, menu clearing, normal exit, native Steam playtime and the absence of the removed listener before formal release and BeatMods submission.

## Earlier live evidence

Earlier tests are historical evidence, not a substitute for v0.5.0 regression:

- **1.40.8_7379 / v0.2.0:** custom song Medicine, Expert+ / Standard; Steam's official Rich Presence Tester rendered `Playing: Medicine [Expert+]`. Pause and menu transitions were captured, native Steam playtime increased by two minutes and the process exited. A separate resume transition was not captured.
- **1.46.0_34060 / v0.3.1:** custom song `[Super ONGEKI Music Pack] Random Access Emotions`, Expert+ / Standard; logs showed native presence updates and repeated pause/resume transitions. Native playtime increased by two minutes and the process exited. A separate menu transition was not captured. This test used FPFC.
- v0.4.0 was built for both targets by Actions and submitted separately to [BeatMods project 8110](https://beatmods.com/mods/8110). The original packages were read back and their SHA-256 values matched the Actions release assets.

## Steam integration

App 620980's published localization response contains `#prelocalizedkey = %pre_localized%`. The plugin supplies song text using the game's Steamworks context and restores it after native presence updates. SetRichPresence success records API acceptance; friend-side display requires a separate observation.

## Supported scope

Game targets are 1.40.8 and 1.46.0 only. Multiplayer, spectator modes and all combinations of other mods have not been comprehensively tested. Two directories are used for live regression: a BSManager 1.40.8 instance and the separate Steam 1.46.0 installation.
