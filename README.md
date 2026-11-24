# Steam Song Presence

[English documentation](README.en.md)

**[GitHub Releases](https://github.com/1000Delta/SteamSongPresence/releases/latest)** · **[1.40.8 安装包](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.4.0/SteamSongPresence-0.4.0-bs1.40.8.zip)** · **[1.46.0 安装包](https://github.com/1000Delta/SteamSongPresence/releases/download/v0.4.0/SteamSongPresence-0.4.0-bs1.46.0.zip)**

[公开构建记录](https://github.com/1000Delta/SteamSongPresence/actions/workflows/release.yml) · [构建流程与安全说明](SECURITY.md)

游戏内 BSIPA 插件，直接在 Steam 好友列表显示当前歌曲、难度和暂停状态，支持官方曲和自定义曲。

**无需额外登录、MuSync、SteamKit 或外部状态程序。** 插件复用游戏已有的 Steam 登录和 App ID 620980，只更新 Rich Presence。不会启动或关闭 Steam、改游戏身份、发送 GamesPlayed、修改 Steam 配置或伪造游玩时长。

示例：`Playing: Medicine [Expert+]`；暂停时为 `Paused: Medicine [Expert+]`。返回菜单后为 `In menus`，不会残留上一首歌。

## 安装

需要 Windows x64 / Steam、Beat Saber 1.40.8 或 1.46.0、已启用的 BSIPA（1.46.0 需要 4.3.7+）。复用 BSIPA 的 Harmony 和 Newtonsoft.Json，不附带游戏程序集。

1. 退出 Beat Saber。
2. 下载对应游戏版本的 ZIP，将其中 `Plugins/SteamSongPresence.dll` 放到**你实际启动的实例**的 `Plugins` 文件夹。
3. 从 Steam 或 BSManager 启动选定实例，Steam 客户端保持已登录。

### 使用 BSManager 导入

1. 下载上方 Release 的 ZIP 安装包，保留 ZIP 格式。
2. 在 BSManager 中选择与安装包对应的 **游戏实例 → Mods（模组）**。
3. 将 ZIP 拖入模组页面，或点击 **⋮ → Import mods（导入模组）** 并选择 ZIP。
4. 导入完成后启动游戏。BSIPA 会应用待安装的插件。

安装包采用 BSManager 原生支持的 `Plugins/SteamSongPresence.dll` 结构，无需手动解压或运行安装脚本。GitHub 自动生成的 **Source code** ZIP 是源码，请下载名称含有游戏版本的安装包。参考 [BSManager 官方导入说明](https://github.com/Zagrios/bs-manager/wiki/install-mods-on-your-version)。

Release 附件由 GitHub Actions 在全新运行器上从源码编译、测试、打包并发布，不上传本地编译包。每个游戏版本附带独立的构建报告、SHA-256 校验值和 GitHub 构建来源证明，方便核对源码提交、依赖来源与产物。

两个版本使用独立安装包，请勿混用。首次安装 BSIPA 或切换游戏版本后，先运行游戏到主菜单并退出，再导入本插件，避免被加载器的首次版本迁移移入旧插件目录。BSManager 的不同实例分别安装，不会共享插件文件。

实测范围：1.40.8 的歌名、暂停、回菜单和原生时长记录；1.46.0 的歌名、暂停/恢复、退出清理和原生时长记录。详情见 [VALIDATION.md](VALIDATION.md)。

从源码构建后，也可运行 `./install.ps1 -GameDir '你的游戏目录'`。卸载时关闭游戏并移除这一份 DLL。换 DLL 后必须重新启动游戏。

## 原理

Beat Games 已为 App 620980 发布官方动态模板：

```text
#prelocalizedkey = %pre_localized%
```

插件先设置 `pre_localized` 为当前歌曲文字，再把 `steam_display` 设为 `#prelocalizedkey`，并更新旧式 `status`。因此直接使用已有 Steam 会话即可在好友列表显示任意歌名。游戏切换场景时会清空 Presence，插件在原生更新后恢复这些键。

模板于 2026-11-25 通过 Steam 的公开应用本地化响应查询确认，并在官方状态测试页成功渲染。诊断程序不随插件发布。

API 说明：[Valve SetRichPresence](https://partner.steamgames.com/doc/api/ISteamFriends#SetRichPresence)，[官方状态测试页](https://steamcommunity.com/dev/testrichpresence)。好友通常会在 Steam 同步后看到更新。

## 本地诊断

`http://127.0.0.1:6749/status` 返回歌曲、作者、难度、模式、暂停状态和进度，每 0.5 秒刷新。只监听本机地址，无写入接口，也不发送 CORS 标头；本机其他程序能够读取。Steam 显示不依赖它，端口占用也不会阻止 Steam 状态更新。

日志路径：游戏 `Logs/_latest.log`，搜索 `Steam Song Presence`。成功发送时有 `Steam friends status updated`。

## 编译与验证

安装 .NET SDK 6+ 和 .NET Framework 4.7.2 targeting pack，运行 `./build.ps1`。可用 `-GameDir` 指向其他 1.40.8 实例。

构建脚本从 Unity 资源读取真实游戏版本，选择对应 API 并嵌入匹配的 manifest。安装包仅包含 `Plugins/SteamSongPresence.dll`，符合当前 BeatMods ZIP 规范。

自动检查覆盖文本字节上限、中文/emoji、无效 UTF-16、控制字符、暂停/恢复、菜单清理、本地 HTTP JSON、并发读取和目标游戏的 Harmony 接口。游戏实测和 Steam 验证见 [VALIDATION.md](VALIDATION.md)。

采用 [MIT 许可证](LICENSE)。
