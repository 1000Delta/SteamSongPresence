# Validation record

Date: 2026-11-25 (Asia/Shanghai).

## v0.5.0 automated checks

Each game target runs 44 checks covering native Steam text, pause/resume/menu transitions, malformed UTF-16 and UTF-8 limits, callback fault isolation, logging faults, independent cleanup stages, Steam write rejection and recovery, wrong App IDs, duplicate suppression and failed forced-refresh retries.

Mono.Cecil checks the selected game's patch targets and the compiled plugin. The plugin assembly is checked for diagnostic-server/audio-clock types and HTTP/socket/JSON type references. Package checks require one DLL and matching embedded manifest, target game and assembly versions.

Fault-injection checks exercise the same guard and Steam-adapter classes used in the plugin. They do not execute Unity native code or prove compatibility with every other mod. Actual build results and toolchain inputs are recorded by the public Actions runs and per-target BUILDINFO files.

## v0.5.0 / 1.40.8 live regression — completed

Installed the 1.40.8 package produced by [Actions run 45468303595](https://github.com/1000Delta/SteamSongPresence/actions/runs/45468303595), source `a13c100a60616ba294fa21750f013517087cd6c9`. Installed DLL SHA-256 matched the build report. The ZIP SHA-256 is `9c6283e65d0c1cf4776919999cd5f7000c60e01a5a1953f43f53b882e7dde7c4`.

- The actual BSManager 1.40.8 instance loaded plugin 0.5.0.0.
- Consecutive custom-song tests included three different songs. Logs captured Playing, Paused and In menus through song changes.
- A targeted follow-up captured Playing → Paused → Playing → Paused → In menus, including playback after resuming.
- The user confirmed the song display and reported the consecutive-song test worked. This is user-observed display evidence; logs separately establish successful API calls.
- Plugin logs contained no Error/Critical entries. The initial Steam-not-ready warning recovered on retry.
- Native Steam playtime increased by four minutes across the sessions. After exit, the game process count and Steam App Running flag were zero; port 6749 had no listener.
- Assembly checks confirmed removal of the HTTP/socket/JSON references and diagnostic server. Runtime state handling and callback error guards also passed automated checks.

## v0.5.0 / 1.46.0 live regression — completed

The Actions-built candidate loaded, updated the song, paused and resumed, then exited. Native Steam playtime increased by two minutes. A monitor sampled the running game 43 times with zero listeners on port 6749. Plugin logs contained no Error/Critical entries.

The initial test was deferred because the user reported FPFC UI navigation prevented song switching. A follow-up on 2026-11-25 completed the missing transitions using the same Actions-built candidate from run 45468303595, source `a13c100a60616ba294fa21750f013517087cd6c9`. The ZIP SHA-256 is `54b09c5eea69166b6a73a1bcbea8422e74c891539ce62854fb0c3aa1dd78dc6b`; the installed DLL matched its build report.

- The actual Steam 1.46.0 installation loaded plugin 0.5.0.0.
- Logs captured the first song Playing → Paused → Playing, followed by over two minutes of playback, In menus, a different second song Playing, and a final In menus.
- The user confirmed completion of the requested song-display, pause/resume, consecutive-song and normal-exit protocol.
- Steam's official Rich Presence Tester captured the second song's rendered text. Refreshing after exit reported User not in-game.
- Plugin logs contained no Error/Critical entries; the initial Steam-not-ready warning recovered on retry.
- Native Steam playtime increased from 6169 to 6177 minutes across the follow-up sessions. After exit, the game process count and Steam App Running flag were zero; port 6749 had no listener.

Both targets are now in the BeatMods submission scope. GitHub v0.5.0 retains its prerelease status and all original Actions assets.

## Earlier live evidence

Earlier tests are historical evidence, not a substitute for v0.5.0 regression:

- **1.40.8_7379 / v0.2.0:** custom song Medicine, Expert+ / Standard; Steam's official Rich Presence Tester rendered `Playing: Medicine [Expert+]`. Pause and menu transitions were captured, native Steam playtime increased by two minutes and the process exited. A separate resume transition was not captured.
- **1.46.0_34060 / v0.3.1:** custom song `[Super ONGEKI Music Pack] Random Access Emotions`, Expert+ / Standard; logs showed native presence updates and repeated pause/resume transitions. Native playtime increased by two minutes and the process exited. A separate menu transition was not captured. This test used FPFC.
- v0.4.0 was built for both targets by Actions and submitted separately to [BeatMods project 8110](https://beatmods.com/mods/8110). The original packages were read back and their SHA-256 values matched the Actions release assets.

## Steam integration

App 620980's published localization response contains `#prelocalizedkey = %pre_localized%`. The plugin supplies song text using the game's Steamworks context and restores it after native presence updates. SetRichPresence success records API acceptance; friend-side display requires a separate observation.

## Supported scope

The supported targets are 1.40.8 and 1.46.0, tested in separate BSManager and Steam installations. Multiplayer, spectator modes and all combinations of other mods have not been comprehensively tested.
