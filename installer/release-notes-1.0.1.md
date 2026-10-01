xShot Mirror 1.0.1 fixes mixed languages in the Windows installer.

- Choosing English now shows an English user guide, Bonjour setup instructions, shortcut and firewall options, and completion messages.
- Choosing Simplified Chinese keeps the corresponding Chinese instructions.
- The installed Start menu user guide follows the selected installer language. Both guide files are included.
- A desktop shortcut is still created by default.

Download `xShotMirror-1.0.1-win-x64-Setup.exe` for Windows 10 22H2 or Windows 11 on Intel/AMD x64. .NET and the audio/video runtime are included. [Apple Bonjour](https://support.apple.com/106380) must be installed separately with Bonjour Service running. Connect your iPhone and PC to the same local network.

Mirroring audio plays through the current default Windows output device. Recording and DRM-protected playback are not supported. The installer is unsigned. `SHA256SUMS.txt` provides download hashes, and `xShotMirror-1.0.1-sources.zip` contains the corresponding application and dependency sources.

Validation: installer compilation and English/Chinese message coverage checks. This update has not undergone a new interactive installation test.

简体中文：修复英文安装界面中混入中文说明的问题。使用说明、Bonjour 提示、快捷方式和防火墙选项现在跟随安装语言；仍默认创建桌面图标。
