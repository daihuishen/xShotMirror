using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace XShotMirror;

internal sealed class ReceiverHost : IDisposable
{
    private Process? process;

    public event Action<string>? StatusChanged;
    public event Action<string>? DeviceChanged;
    public event Action<string>? PinChanged;

    public bool IsRunning => process is { HasExited: false };

    public int? ProcessId => IsRunning ? process!.Id : null;

    public void Start()
    {
        if (IsRunning) return;
        if (Process.GetProcessesByName("uxplay").Any())
            throw new InvalidOperationException("另一个 xShot Mirror 接收器正在运行。请先关闭它，再启动此窗口。");

        string receiver = FindReceiver();
        string runtime = @"C:\msys64\ucrt64\bin";
        if (!File.Exists(Path.Combine(runtime, "libgstreamer-1.0-0.dll")))
            throw new FileNotFoundException("找不到 GStreamer 运行环境。请先按 README 安装 MSYS2 UCRT64。", runtime);

        string pin = RandomNumberGenerator.GetInt32(1000, 10000).ToString(CultureInfo.InvariantCulture);
        var info = new ProcessStartInfo(receiver)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(receiver)!
        };
        info.Environment["PATH"] = runtime + @";C:\msys64\usr\bin;" + Environment.GetEnvironmentVariable("PATH");
        foreach (string argument in new[]
        {
            "-n", "xShot Mirror", "-nh", "-as", "0", "-vsync", "no",
            "-pin", pin
        }) info.ArgumentList.Add(argument);

        var next = new Process { StartInfo = info, EnableRaisingEvents = true };
        next.OutputDataReceived += (_, e) => { if (ReferenceEquals(process, next)) HandleLine(e.Data); };
        next.ErrorDataReceived += (_, e) => { if (ReferenceEquals(process, next)) HandleLine(e.Data); };
        next.Exited += (_, _) =>
        {
            if (!ReferenceEquals(process, next)) return;
            process = null;
            StatusChanged?.Invoke("接收程序意外退出；检查 Bonjour、网络和运行环境");
            DeviceChanged?.Invoke("未连接");
            PinChanged?.Invoke("—");
        };
        try
        {
            if (!next.Start()) throw new InvalidOperationException("无法启动接收程序。");
            process = next;
            next.BeginOutputReadLine();
            next.BeginErrorReadLine();
            PinChanged?.Invoke(pin);
            DeviceChanged?.Invoke("未连接");
            StatusChanged?.Invoke("等待 iPhone 连接（同一局域网）");
        }
        catch
        {
            next.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        Process? current = process;
        process = null;
        if (current is null) return;
        try
        {
            if (!current.HasExited)
            {
                // UxPlay has no control channel for a GUI host; process exit releases Bonjour registration.
                current.Kill(entireProcessTree: true);
                current.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException) { }
        finally
        {
            current.Dispose();
            PinChanged?.Invoke("—");
            DeviceChanged?.Invoke("未连接");
            StatusChanged?.Invoke("接收已停止");
        }
    }

    private static string FindReceiver()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string path = Path.Combine(dir.FullName, "third_party", "UxPlay", "build-bonjour", "uxplay.exe");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("找不到接收程序。请先运行 scripts/build-uxplay.ps1。");
    }

    private void HandleLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        const string request = "connection request from ";
        int position = line.IndexOf(request, StringComparison.OrdinalIgnoreCase);
        if (position >= 0)
        {
            string name = line[(position + request.Length)..];
            int suffix = name.IndexOf(" (", StringComparison.Ordinal);
            if (suffix >= 0) name = name[..suffix];
            if (name.Length > 64) name = name[..64];
            DeviceChanged?.Invoke(string.IsNullOrWhiteSpace(name) ? "iPhone 已请求连接" : name + "（连接中）");
            StatusChanged?.Invoke("正在建立镜像连接");
        }
        else if (line.Contains("lost connection with client", StringComparison.OrdinalIgnoreCase))
        {
            DeviceChanged?.Invoke("未连接");
            StatusChanged?.Invoke("连接中断；检查 Wi-Fi 和防火墙");
        }
        else if (line.Contains("GStreamer", StringComparison.OrdinalIgnoreCase) &&
                 line.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke("视频解码失败；检查 GStreamer 插件");
        }
        else if (line.Contains("unknown option", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke("接收程序参数不兼容；请重新运行构建脚本");
        }
    }

    public void Dispose() => Stop();
}
