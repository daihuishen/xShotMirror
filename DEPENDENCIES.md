# 依赖与许可记录

2026-10-01 的本机原型构建：

| 组件 | 本机版本 | 用途 | 许可与分发提示 |
| --- | --- | --- | --- |
| UxPlay | 开发中 1.74，提交 `1ad348a890f48e4178099a697445a02e920af259`，加仓库内 Bonjour 构建补丁 | AirPlay 接收、配对、Bonjour 服务注册 | GPLv3；上游说明内置 PlayFair 的法律状态仍有不确定性。需随发布提供适用源码、补丁与声明。 |
| Apple mDNSResponder `dns_sd.h` | 提交 `d4658af3f5f291311c6aee4210aa6d39bda82bbe`，SHA-256 `5D0CA50F207F6EB02E09D743F9B65D2ADE65E8F81862DCD3703845B4FE87A9C1` | Bonjour API 编译头文件 | BSD-3-Clause；仓库保留头部版权与许可声明。 |
| Bonjour Service | 开发机已安装的 Windows 系统服务 | DNS-SD/mDNS 广播 | 原型仅调用本机现有 `dnssd.dll`，不在发行包中再分发 Apple 二进制或安装器；正式交付需说明用户端安装来源与条款。 |
| MSYS2 Installer | 20260611 | 构建环境 | BSD-3-Clause；仅为开发机工具。 |
| GCC / CMake | 16.2.0 / 4.4.3 | 编译 | 仅为构建工具，运行时依赖另行核对。 |
| OpenSSL | 3.6.5 | 协议加密 | Apache-2.0；UxPlay 上游建议使用 3.0 或更新版本处理旧版本 GPL 兼容问题。 |
| libplist | 2.7.0 | AirPlay plist 解析 | LGPL-2.1-or-later。 |
| GStreamer、base/good/bad/libav | 1.28.7 | 视频解码与显示 | 具体插件及其间接依赖许可证需按最终分发清单逐项核对；当前不打包分发。 |
| .NET Windows Desktop | SDK 6.0.400，运行时 6.0.8 | WinForms 界面 | 开发机预装；当前不打包运行时。正式发布前应升级到仍受支持的 .NET 版本并核对部署许可。 |

本机已确认 `avdec_h264` 与 `d3d12videosink` 插件可加载。安装 `gst-plugins-bad`/`gst-libav` 会通过 MSYS2 拉入许多与本原型无关的编解码库，**不得直接复制整个 UCRT64 目录作为发行包**。后续发布还要审计 FFmpeg、可能的编解码专利和上游 PlayFair 来源。此表只记录原型构建环境，不是发行许可清单。

上游资料：[UxPlay](https://github.com/FDH2/UxPlay)、[GStreamer 授权说明](https://gstreamer.freedesktop.org/documentation/frequently-asked-questions/licensing.html)、[OpenSSL 许可](https://www.openssl.org/source/license.html)、[libplist](https://github.com/libimobiledevice/libplist)。
