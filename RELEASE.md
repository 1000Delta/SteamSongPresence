Steam song, difficulty and pause status using the existing game session. No extra login or companion application.

**Separate installation packages: Beat Saber 1.40.8 and 1.46.0 / Steam / Windows x64. Do not mix the builds.**

The project supports exactly two game targets: **1.40.8 is the project-recommended version** for a fixed BSManager instance, and **1.46.0 is the official latest version** verified against the Steam public installation on 2026-11-25. This is the project's own recommendation, not an endorsement by BeatMods or BSMG. Other game versions are not currently supported. Both builds share the plugin version number and are registered separately for their respective game versions on BeatMods when submitted.

Download the ZIP matching your game version. In BSManager, choose that **game instance → Mods**, then drag the ZIP onto the page or select **⋮ → Import mods**. Keep the ZIP intact and launch the game after import. BSIPA must already be installed; 1.46.0 requires BSIPA 4.3.7+. After first setting up BSIPA or upgrading a directory, launch to the menu and exit once before importing this plugin.

The ZIP contains only `Plugins/SteamSongPresence.dll`. Do not use the automatically generated Source code archives as installation packages.

Both packages are independently compiled and tested from the tagged source by the public **Build and release** Actions workflow. Each has a versioned `BUILDINFO-bs<game-version>.json` and `SHA256SUMS-bs<game-version>.txt`. GitHub artifact attestations bind the assets to the public workflow run, source commit and hashes. Actions and dependencies are pinned; no locally built binary is uploaded.

1.46.0 was live-tested with v0.3.1: custom song metadata, Steam presence updates, repeated pause/resume, exit cleanup and native playtime. 1.40.8 rendering, pause, menu cleanup and native playtime were live-tested with v0.2.0. The capture/presence behavior is retained in v0.4.0. Each target also runs 21 checks and package metadata checks; see [validation details](https://github.com/1000Delta/SteamSongPresence/blob/main/VALIDATION.md) and [build transparency](https://github.com/1000Delta/SteamSongPresence/blob/main/SECURITY.md).

中文：按游戏版本下载对应 ZIP，在 BSManager 选择对应实例的 **Mods（模组）**，直接拖入 ZIP 或通过 **⋮ → Import mods（导入模组）** 选择，无需手动解压。首次安装 BSIPA 或升级游戏目录后，先运行到主菜单并退出一次，再导入本插件。两个版本分别由 Actions 从源码编译，依赖、日志、校验值和构建来源证明公开。
