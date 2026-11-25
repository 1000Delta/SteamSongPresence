# Steam Song Presence

[English documentation](README.en.md)

[GitHub Releases](https://github.com/1000Delta/SteamSongPresence/releases) · [BeatMods](https://beatmods.com/mods/8110) · [公开构建记录](https://github.com/1000Delta/SteamSongPresence/actions/workflows/release.yml)

游戏内 BSIPA 插件，在 Steam 好友列表显示当前歌曲、难度和暂停状态，支持官方曲和自定义曲。

```text
Playing: Medicine [Expert+]
Paused: Medicine [Expert+]
In menus
```

## 支持版本

| Beat Saber | 本项目定位 | 安装包 |
| --- | --- | --- |
| 1.40.8 | 推荐版本，适合在 BSManager 中固定使用 | [SteamSongPresence-0.5.0-bs1.40.8.zip](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.5.0/SteamSongPresence-0.5.0-bs1.40.8.zip) |
| 1.46.0 | Steam 官方最新版（2026-11-25 核对） | [SteamSongPresence-0.5.0-bs1.46.0.zip](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.5.0/SteamSongPresence-0.5.0-bs1.46.0.zip) |

推荐版本是本项目的选择。两个目标分别打包并完成实机回归；其他游戏版本暂不在支持范围内。GitHub v0.5.0 保留为预发布候选包，BeatMods 审核状态以项目页面为准。

v0.5.0 的实机回归和发布状态见 [VALIDATION.md](VALIDATION.md)。

## 安装

环境：Windows x64、Steam。1.40.8 使用 BSIPA 4.3.5+，1.46.0 使用 BSIPA 4.3.7+。Harmony 使用 BSIPA 随附版本。

1. 退出 Beat Saber，下载对应游戏版本的安装 ZIP。
2. 在 BSManager 选择对应的 **游戏实例 → Mods（模组）**。
3. 将 ZIP 拖入模组页面，或通过 **⋮ → Import mods（导入模组）** 选择 ZIP。
4. 从 Steam 或 BSManager 启动该实例，检查 Steam 好友列表里的歌曲状态。

手动安装时，将 ZIP 中的 `Plugins/SteamSongPresence.dll` 放入对应游戏目录的 `Plugins` 文件夹。

首次安装 BSIPA 或升级同一个游戏目录后，先运行到主菜单并退出一次，再导入本插件，让加载器完成版本迁移。BSManager 的不同实例分别管理插件目录。

下载名称含游戏版本的安装包；GitHub 的 **Source code** 附件用于源码。参考 [BSManager 官方导入说明](https://github.com/Zagrios/bs-manager/wiki/install-mods-on-your-version)。

更换 DLL 后重新启动游戏。卸载时关闭游戏并移除 `Plugins/SteamSongPresence.dll`。

## 实现

插件在游戏初始化关卡及暂停、恢复时读取歌名、难度和暂停状态，通过当前游戏的 Steamworks 会话更新 Rich Presence。Beat Saber 为 App 620980 发布的动态模板是：

```text
#prelocalizedkey = %pre_localized%
```

插件填入 `pre_localized` 和 `status`，再选择 `steam_display=#prelocalizedkey`。游戏原生 Presence 更新完成后，插件恢复歌曲文案。每 0.5 秒检查待重试的更新；已经成功发送且文字相同的更新会被合并。

Unity 回调、Steam 更新和退出清理使用异常保护。清理按独立步骤执行；重复故障按操作抑制重复日志，成功恢复后重新允许记录新故障。

日志位于游戏的 `Logs` 目录，搜索 `Steam Song Presence`、`Current song` 和 `Steam friends status updated`。Steam 好友端显示时间取决于客户端同步。

接口资料：[Valve SetRichPresence](https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence)、[官方状态测试页](https://steamcommunity.com/dev/testrichpresence)。

## 构建与验证

安装 .NET SDK 6+ 和 .NET Framework 4.7.2 targeting pack，运行：

```powershell
./build.ps1 -GameDir '你的游戏目录'
```

构建脚本读取实际游戏版本，选择对应引用并嵌入匹配的 manifest。自动检查覆盖状态转换、UTF-8 长度、异常隔离、Steam 拒绝和恢复、强制刷新重试，以及游戏补丁目标。

GitHub Actions 在托管运行器上分别为两个目标编译、检查并打包。ZIP 仅包含 `Plugins/SteamSongPresence.dll`，另附构建报告、SHA-256 和构建来源证明。BeatMods 使用与实机验证相同的 Actions 附件。

[构建流程与安全说明](SECURITY.md) · [验证范围](VALIDATION.md) · [MIT 许可证](LICENSE)
