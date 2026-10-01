# xShot Mirror — Windows AirPlay 屏幕镜像接收原型

xShot Mirror 让 iPhone 从系统“控制中心 → 屏幕镜像”向 Windows 电脑投屏。首版仅限同一局域网、单台 iPhone、视频画面，不需要修改 iPhone 上的 xShot。接收协议由 [UxPlay](https://github.com/FDH2/UxPlay) 实际运行，GStreamer 负责 H.264 解码和显示。

## 当前状态

这是已在真实 iPhone 上通过基本端到端验证的**原型**，还不是完整的产品界面。2026-10-01 在 Windows 10 22H2 x64、电脑有线接入与 iPhone 非访客 Wi-Fi 同一局域网的条件下，用户确认 iPhone 系统“屏幕镜像”列表发现 `xShot Mirror`、完成 PIN 配对、Windows 实时显示画面、切换其他 App 后画面同步变化、断开后重新连接正常。iPhone 具体机型/iOS 版本待补。接收端收到正常 Ctrl+C 后进程退出，Bonjour 浏览器不再发现广播；重新启动后广播再次出现。

首次使用 UxPlay 实验性内置 mDNS 时，iPhone 未发现接收器；改用本机 Bonjour 服务发现后端才通过上述验证。本机 Bonjour 浏览器也能发现 `xShot Mirror`。

Windows 窗口现在显示接收状态、连接设备、配对码和画面，并有开始接收、停止接收、退出按钮。iPhone 实测已确认实时画面能显示在窗口的画面区域，且画面内部没有额外标题栏。`scripts/run.ps1` 保留直接运行接收器的诊断方式，画面会在独立原生窗口显示。没有录制或保存镜像画面的功能。
窗口提供全屏按钮；全屏时可点退出全屏，也可按 Esc 或 F11 返回。用户已确认真实 iPhone 画面全屏显示与返回窗口正常。界面默认使用英语，窗口右上角可选择 English 或中文，按钮、连接状态、错误提示和许可说明会随之切换。“About / Licenses”（关于/许可）列出项目许可、第三方声明和源码地址。

## 依赖与构建

Windows 安装包的构建与分发步骤见 [installer/RELEASE.md](installer/RELEASE.md)。可运行 `./scripts/build-installer.ps1` 生成安装器、对应源码包和校验清单；发布前检查事项仍见 [DISTRIBUTION.md](DISTRIBUTION.md)。

普通用户应运行 `dist/xShotMirror-<版本>-win-x64-Setup.exe`。该安装包自带所需的 .NET 和视频运行库。不要单独复制或双击 `src/xShotMirror/bin` 下的开发版 `xShotMirror.exe`；开发版依赖构建环境，缺少系统 .NET 时会显示安装 .NET 的提示。

安装 [MSYS2](https://www.msys2.org/) 的 UCRT64 环境。该环境提供免费编译器和 GStreamer。本机安装的是 MSYS2 Installer 20260611。首次安装后按 MSYS2 官方说明运行 `pacman -Syu`，如更新基础运行时后终端退出，重新打开 UCRT64 终端再运行一次。

在 UCRT64 终端安装以下包：

```sh
pacman -S --needed mingw-w64-ucrt-x86_64-cmake mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-ninja mingw-w64-ucrt-x86_64-pkgconf mingw-w64-ucrt-x86_64-openssl mingw-w64-ucrt-x86_64-libplist mingw-w64-ucrt-x86_64-gstreamer mingw-w64-ucrt-x86_64-gst-plugins-base mingw-w64-ucrt-x86_64-gst-plugins-good mingw-w64-ucrt-x86_64-gst-plugins-bad mingw-w64-ucrt-x86_64-gst-libav
```

桌面界面当前面向 .NET 10 Windows Desktop。开发环境需要 .NET 10 SDK；构建脚本会优先使用项目本地 `.tools/dotnet/dotnet.exe`，若不存在则使用系统 `dotnet`。然后在项目根目录的 PowerShell 中运行：

```powershell
./scripts/fetch-uxplay.ps1
./scripts/build-uxplay.ps1
./scripts/build-app.ps1
./scripts/run-app.ps1
```

`fetch-uxplay.ps1` 固定官方 UxPlay 提交 `1ad348a890f48e4178099a697445a02e920af259`（开发中的 1.74）。构建脚本应用 [小补丁](patches/uxplay-bonjour-no-import-lib.patch)：Windows 后端在运行时加载 `dnssd.dll`，无需 Bonjour SDK 的导入库；编译使用仓库中固定版本的 Apple [dns_sd.h](third_party/bonjour-header/dns_sd.h)。运行前须有正在运行的 **Bonjour Service**；本项目不打包 Bonjour 安装器或 DLL。界面以随机四位 PIN 启动接收器，只播放视频；接收器生成的原生视频窗口由界面放入画面区域。若系统要求防火墙许可，可在管理员 PowerShell 中运行 `./scripts/allow-lan.ps1`，仅允许该程序接收本地子网的 TCP/UDP 流量。重新编译接收器或界面前，先退出 xShot Mirror。

如需排查界面问题，可关闭窗口后运行 `./scripts/run.ps1`，直接启动 UxPlay。此时画面和配对码分别在独立窗口、终端显示；在终端按 Ctrl+C 停止。

本机测试时 Windows 电脑通过以太网取得 `192.168.0.90`，iPhone 可以通过同一局域网的非访客 Wi-Fi 接入；两端不必都使用无线网卡，但路由器必须允许 Wi-Fi 与有线设备互通和 mDNS 组播。构建与启动脚本不会自动修改 Windows 防火墙规则；`allow-lan.ps1` 仅在管理员主动运行时添加本地子网入站规则。

首版目标范围是 iPhone 12 或更新机型上各机型可安装的最新 iOS，以及 Windows 10 22H2/Windows 11 x64。当前只验证了上述一台 Windows 10 电脑与一台尚未记录型号和版本的 iPhone；目标范围不代表已逐一验证兼容性。

## 真实设备验收

1. 在 Windows 上运行 `./scripts/run-app.ps1`，保持 xShot Mirror 窗口运行。
2. 在 iPhone 12 或更新机型、该机型可安装的最新 iOS 上打开“控制中心 → 屏幕镜像”，查找 `xShot Mirror`。
3. 选择接收器，在 iPhone 上输入窗口显示的 PIN。
4. 检查窗口中的画面区域是否连续显示主屏幕、xShot 画面和切换到其他 App 后的画面；记录 iPhone/iOS 和网络条件。
5. 从 iPhone 停止镜像，再连接一次；随后点击“停止接收”并确认名称从镜像列表消失，再点“开始接收”确认名称重新出现。
6. 对比镜像开启前后 xShot 的 240 fps 视频处理表现。不要假设镜像本身为 240 fps。

可运行 `python ./scripts/probe-mdns.py --interface <Windows局域网IPv4>` 或 `python ./scripts/probe-bonjour.py` 辅助诊断服务发现。首次内置 mDNS 构建下两个探针没有收到匹配应答，不能代替 iPhone 验收。

## 常见故障

- **iPhone 看不到名称：**确认 Bonjour Service 正在运行、同一局域网、没有访客隔离，检查组播和 Windows 防火墙 UDP 5353；若电脑走有线网络，确认路由器允许有线与 Wi-Fi 互通。
- **看得到但连接失败：**检查接收器的 TCP/UDP 入站连接和终端协议错误；只允许本地子网，不要关闭整个防火墙。
- **连接成功但无画面：**检查 GStreamer 的 H.264 解码插件及视频输出插件；可用 `gst-inspect-1.0 avdec_h264` 与 `gst-inspect-1.0 d3d12videosink` 检查本机安装。
- **窗口画面区域不显示：**先退出界面，使用 `./scripts/run.ps1` 核对接收器能否在独立窗口显示画面；若独立窗口正常，则是界面放置视频窗口的问题。
- **受保护内容黑屏：**原型不支持 DRM 视频，也不尝试绕过 App 限制。

## 开源与分发

本项目自身源码采用 GPL-3.0-only，详见 [LICENSE](LICENSE)。接收核心 UxPlay 也是 GPLv3。Apple 头文件保留自己的 BSD 式许可；来源与修改记录见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。当前从 MSYS2 安装的整套开发环境**不是**准备好可直接发布的便携包。发布二进制前还须完成对应源码、组件许可证、PlayFair 法律状态及编解码相关审查，详见 [DISTRIBUTION.md](DISTRIBUTION.md) 和 [DEPENDENCIES.md](DEPENDENCIES.md)。
