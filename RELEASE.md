Steam song, difficulty and pause status using the existing game session. No extra login or companion application.

**Installation package: Beat Saber 1.40.8 / Steam / Windows x64.**

Download `SteamSongPresence-0.3.0-bs1.40.8.zip` below. In BSManager, choose **1.40.8 → Mods**, then drag the ZIP onto the page or select **⋮ → Import mods**. Keep the ZIP intact and launch the game after import. BSIPA must already be installed. Do not use the automatically generated Source code archives as installation packages.

The ZIP contains only `Plugins/SteamSongPresence.dll`. It is compiled and packaged from the tagged source by the public **Build and release** GitHub Actions workflow. `BUILDINFO.json` records source, build URL, SDK, pinned references and hashes. `SHA256SUMS.txt` provides integrity checks, and GitHub artifact attestations bind the files to their workflow run.

Build checks cover Unicode handling, state transitions, local diagnostics and target game hook metadata. Steam status rendering, pause, menu cleanup and native playtime were live-verified with v0.2.0 on 1.40.8; that behavior is retained in this build. See [validation details](https://github.com/1000Delta/SteamSongPresence/blob/main/VALIDATION.md).

**1.46.0 live testing is pending. This release does not include a 1.46.0 binary.**

中文：下载下方带有 `bs1.40.8` 的 ZIP，在 BSManager 选择 **1.40.8 → Mods（模组）**，直接拖入 ZIP 或通过 **⋮ → Import mods（导入模组）** 选择，无需手动解压。构建日志、源码提交、依赖及校验值均公开；安装包由 Actions 从源码生成。
