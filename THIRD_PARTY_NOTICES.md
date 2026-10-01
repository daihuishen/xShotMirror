# 第三方源码与版权声明

本仓库仅包含 xShot Mirror 自身源码、构建脚本、一个 UxPlay 补丁，以及 Apple 的 `dns_sd.h` 头文件。没有提交 UxPlay 完整源码、Bonjour 服务二进制、GStreamer 插件或编译产物。

## UxPlay

- 来源：[FDH2/UxPlay](https://github.com/FDH2/UxPlay)，固定提交 [`1ad348a890f48e4178099a697445a02e920af259`](https://github.com/FDH2/UxPlay/tree/1ad348a890f48e4178099a697445a02e920af259)。上游整体按 GPLv3 提供，源码文件还包含其继承组件的原版权与许可声明。
- 本项目于 **2026-10-01** 修改 Windows 构建配置；修改内容在 [`patches/uxplay-bonjour-no-import-lib.patch`](patches/uxplay-bonjour-no-import-lib.patch)。补丁只去掉 Windows 构建时对 Bonjour SDK 导入库的链接要求，运行时仍由 UxPlay 加载系统中的 `dnssd.dll`。本项目未改动 UxPlay 的 PlayFair 或 DRM 处理代码。
- 上游 [README 的 Disclaimer](https://github.com/FDH2/UxPlay#disclaimer) 明确称其使用的第三方 PlayFair 库法律状态不清楚。GPL 许可本身不能消除这一不确定性；**公开发布可执行文件前应先完成针对目标发行地区的法律审查**。

## Apple `dns_sd.h`

- 来源：Apple 开源 [mDNSResponder 的 `dns_sd.h`](https://github.com/apple-oss-distributions/mDNSResponder/blob/d4658af3f5f291311c6aee4210aa6d39bda82bbe/mDNSShared/dns_sd.h)，提交 `d4658af3f5f291311c6aee4210aa6d39bda82bbe`。
- 本仓库的 [`third_party/bonjour-header/dns_sd.h`](third_party/bonjour-header/dns_sd.h) 保留了文件开头完整的 Apple 版权、三条 BSD 式再分发条件和免责声明。SHA-256 为 `5D0CA50F207F6EB02E09D743F9B65D2ADE65E8F81862DCD3703845B4FE87A9C1`，构建脚本会校验该值。
- Apple 并未赞助或认可 xShot Mirror。该头文件的许可与本项目自身的 GPLv3 许可分别保留，不将 Apple 的文件改称为本项目原创代码。

其余开发机依赖、版本与初步许可信息见 [DEPENDENCIES.md](DEPENDENCIES.md)。实际二进制发布必须重新核对**所携带的每一个组件**及其版本、版权和许可。
