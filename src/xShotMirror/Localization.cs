// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 xShot Mirror contributors

namespace XShotMirror;

internal enum UiLanguage { English, Chinese }

internal static class Localization
{
    public static string Text(UiLanguage language, string key)
    {
        (string english, string chinese) = key switch
        {
            "start" => ("Start receiving", "开始接收"),
            "stop" => ("Stop receiving", "停止接收"),
            "fullscreen" => ("Full screen", "全屏显示"),
            "exitFullscreen" => ("Exit full screen", "退出全屏"),
            "about" => ("About / Licenses", "关于/许可"),
            "exit" => ("Exit", "退出"),
            "status" => ("Status: ", "状态："),
            "device" => ("    Device: ", "    设备："),
            "pin" => ("    Pairing code: ", "    配对码："),
            "instructions" => ("On iPhone, open Control Center > Screen Mirroring and choose xShot Mirror. Both devices must be on the same local network.",
                               "在 iPhone 控制中心打开“屏幕镜像”，选择 xShot Mirror。电脑和 iPhone 需位于同一局域网。"),
            "startFailed" => ("Could not start receiving", "无法启动接收"),
            "startupTitle" => ("xShot Mirror startup", "xShot Mirror 启动"),
            "aboutTitle" => ("About xShot Mirror and Licenses", "关于 xShot Mirror 与许可"),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown localization key")
        };
        return language == UiLanguage.English ? english : chinese;
    }

    public static string Status(UiLanguage language, string source)
    {
        if (language == UiLanguage.Chinese) return source;
        return source switch
        {
            "接收已停止" => "Receiver stopped",
            "等待 iPhone 连接（同一局域网）" => "Waiting for iPhone on the same local network",
            "正在建立镜像连接" => "Connecting to iPhone",
            "正在镜像" => "Mirroring",
            "连接中断；检查 Wi-Fi 和防火墙" => "Connection lost. Check Wi-Fi and Windows Firewall.",
            "视频解码失败；检查 GStreamer 插件" => "Video decoding failed. Check GStreamer plugins.",
            "接收程序参数不兼容；请重新运行构建脚本" => "Receiver options are incompatible. Rebuild the receiver.",
            "接收程序意外退出；检查 Bonjour、网络和运行环境" => "Receiver exited unexpectedly. Check Bonjour, network, and runtime.",
            "无法启动接收" => "Could not start receiving",
            _ => "Receiver status changed. Check the connection."
        };
    }

    public static string Device(UiLanguage language, string source, bool connecting)
    {
        if (source == "未连接") return language == UiLanguage.English ? "Not connected" : source;
        if (source == "iPhone 已请求连接")
            return language == UiLanguage.English ? "iPhone requested connection" : source;
        string name = source.Replace("（连接中）", "", StringComparison.Ordinal);
        if (!connecting) return name;
        return language == UiLanguage.English ? name + " (connecting)" : name + "（连接中）";
    }

    public static string StartupError(UiLanguage language, Exception error)
    {
        if (language == UiLanguage.Chinese) return error.Message;
        string message = error.Message;
        if (message.Contains("另一个 xShot Mirror", StringComparison.Ordinal))
            return "Another xShot Mirror receiver is running. Close it before starting this window.";
        if (message.Contains("找不到视频运行库", StringComparison.Ordinal))
            return "Video runtime is missing. Reinstall xShot Mirror, or install MSYS2 UCRT64 for a development build.";
        if (message.Contains("请先安装 Apple Bonjour", StringComparison.Ordinal))
            return "Install Apple Bonjour and make sure Bonjour Service is running. See the installation instructions for the official download.";
        if (message.Contains("找不到接收程序", StringComparison.Ordinal))
            return "Receiver executable is missing. Run scripts/build-uxplay.ps1 first.";
        if (message.Contains("无法启动接收程序", StringComparison.Ordinal))
            return "The receiver could not start. Check Bonjour and the installed runtime.";
        return "The receiver could not start. Check Bonjour, the local network, and the installed runtime.";
    }

    public static string About(UiLanguage language) => language == UiLanguage.English
        ? "xShot Mirror\nCopyright (C) 2026 xShot Mirror contributors\n\n" +
          "This source code is licensed under GNU GPL v3.0, without any warranty.\n" +
          "Receiver core: UxPlay (GPLv3). Apple's dns_sd.h retains its BSD-style license.\n\n" +
          "Full licenses, third-party notices, and source code:\nhttps://github.com/daihuishen/xShotMirror"
        : "xShot Mirror\nCopyright (C) 2026 xShot Mirror contributors\n\n" +
          "本项目源码按 GNU GPL v3.0 许可提供，不提供任何担保。\n" +
          "接收核心 UxPlay 按 GPLv3 提供；Apple dns_sd.h 保留其 BSD 式许可。\n\n" +
          "完整许可、第三方声明与源码：\nhttps://github.com/daihuishen/xShotMirror";
}
