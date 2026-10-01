# Windows 安装包

本目录保存 xShot Mirror 的 Inno Setup 安装器和用户说明。版本默认 1.0.0，目标 Windows 10 22H2 / Windows 11 x64。

## 构建

准备 .NET 10 SDK、Python 3.10+、Inno Setup 6.7+，以及 README 中的 MSYS2 UCRT64 原生构建依赖。项目会优先使用 `.tools/dotnet` 和 `.tools/InnoSetup`。工具本身不提交到仓库。中文语言文件来自 Inno Setup 官方翻译目录，版权说明见 `InnoSetup-LICENSE.txt`。

```powershell
./scripts/fetch-uxplay.ps1
./scripts/build-installer.ps1
# 工具安装在其他目录时：
./scripts/build-installer.ps1 -Version 1.0.0 -Iscc 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe'
```

构建脚本重新编译 UxPlay，发布自带 .NET 的界面，解析所选 GStreamer 插件和原生程序的 DLL 依赖，复制对应包的许可证。然后从 MSYS2 官方仓库获取相同版本的源码包，补齐上游版权声明，生成源码 ZIP，最后编译安装器与 SHA-256 清单。MSYS2 的 FFmpeg 构建链接了许多库，所以即使只选 6 个插件，也会带入额外的 DLL；没有复制完整开发环境。

源码 ZIP 中 `xShotMirror/` 包含当前源码、修改后的 UxPlay 和构建脚本；`dependencies/` 保存原始 MSYS2 源码包、PKGBUILD、补丁及下载哈希。重建依赖时，在 MSYS2 中解包相应源码包，按其 PKGBUILD 使用 `makepkg-mingw` 构建并安装，再构建本项目。UxPlay 使用固定上游提交和仓库中的 Bonjour 补丁；`build-uxplay.ps1` 同时接受已经应用补丁的源码。应用与原生 DLL 均未单文件封装，便于替换重建的组件。

输出目录 `dist/`：

- `xShotMirror-1.0.0-win-x64-Setup.exe`：用户安装包。
- `xShotMirror-1.0.0-sources.zip`：同版本应用和原生依赖源码。
- `SHA256SUMS.txt`：下载完整性校验。

`-SkipSources` 仅供本地打包调试，不应据此公开发布。安装器默认需要管理员权限，安装到 Program Files，添加开始菜单入口；桌面快捷方式默认勾选。防火墙规则只针对已安装的 `receiver/uxplay.exe`，远端限制 LocalSubnet，卸载时移除；Bonjour 仍由用户从 Apple 渠道另行安装。

## 本版选择与验证范围

发布版显式使用 `avdec_h264` 软件解码和 `d3d11videosink` 输出。开发版仍保留原有自动选择。发布版固定自己的插件目录与扫描器，缓存写入用户 LocalAppData，不依赖开发机的 MSYS2 路径。安装目录可包含空格和中文。

本机执行的原生运行库测试会清除 PATH 中的 MSYS2 路径，使用合成 H.264 片段验证解码与 Direct3D 输出。这不是干净 Windows 机器或真实 iPhone 的验收替代。公开上线前请在无 .NET、无 MSYS2 的机器安装 Bonjour 后检查：发现、PIN、镜像、全屏、断开重连、退出、覆盖安装和卸载。

尚无发行者代码签名证书，当前 EXE 为未签名构建。需要正式签名时应同时签署应用、安装器和卸载器，保存签名后的校验值；不要用自签名证书宣称受信任发行者。

公开上传时应把安装器、对应源码 ZIP 和校验清单放在同一个下载页面。项目的发布前审查事项仍见根目录 `DISTRIBUTION.md`；本次工程打包不代表该审查已经完成。
