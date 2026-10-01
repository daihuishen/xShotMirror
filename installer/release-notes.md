xShot Mirror 1.0.0 brings iPhone screen mirroring to Windows 10 22H2 and Windows 11 (x64).

- Windowed and full-screen viewing, four-digit PIN pairing, English/Chinese interface.
- Installer includes .NET 10 and the required audio/video runtime. No MSYS2 or developer tools are needed.
- A desktop shortcut is created by default, alongside the Start menu entry.
- Optional firewall rules allow the installed receiver to accept connections from the local subnet. Uninstall removes these rules.

Download `xShotMirror-1.0.0-win-x64-Setup.exe` to install. Apple Bonjour must be installed separately and Bonjour Service must be running: [Apple's official download information](https://support.apple.com/106380). Keep the computer and iPhone on the same local network.

Mirroring audio plays through the current default Windows output device. Recording and DRM-protected playback are not supported. The installer is currently unsigned, so Windows may show an unknown-publisher or SmartScreen prompt.

`xShotMirror-1.0.0-sources.zip` contains the matching application source, patched UxPlay source, build scripts, and corresponding MSYS2 source packages. `SHA256SUMS.txt` provides download hashes. Source code is distributed under its respective licenses; see the included licenses and notices.

---

简体中文：下载 Setup.exe 安装，默认创建桌面图标。已包含 .NET 和音视频运行库；Apple Bonjour 仍需单独安装并启动服务。电脑与 iPhone 须处于同一局域网。镜像声音通过 Windows 当前默认播放设备输出；不支持录制或受保护视频。安装包暂未进行发行者数字签名。

发布状态：草稿。已完成本机安装、重复安装、桌面快捷方式、防火墙范围、卸载清理及独立视频解码验证。干净 Windows 环境下的真实 iPhone 验收，以及仓库 DISTRIBUTION.md 记录的发布前审查尚待完成。
