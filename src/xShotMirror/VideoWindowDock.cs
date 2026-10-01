using System.Runtime.InteropServices;

namespace XShotMirror;

internal sealed class VideoWindowDock
{
    private const int GwlStyle = -16;
    private const long WsChild = 0x40000000;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsVisible = 0x10000000;
    private const long WindowChrome = 0x00C00000 | 0x00080000 | 0x00040000 | 0x00030000;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private readonly Control surface;
    private nint docked;

    public VideoWindowDock(Control surface)
    {
        this.surface = surface;
        surface.Resize += (_, _) => Resize();
    }

    public bool IsDocked => docked != 0 && IsWindow(docked);

    public bool TryDock(int processId)
    {
        if (IsDocked) return true;
        docked = 0;
        nint videoWindow = 0;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint owner);
            if (owner != processId || !IsWindowVisible(window) || !GetWindowRect(window, out Rect bounds))
                return true;
            if (bounds.Right - bounds.Left < 100 || bounds.Bottom - bounds.Top < 100)
                return true;
            videoWindow = window;
            return false;
        }, 0);
        if (videoWindow == 0) return false;

        long style = (long)GetWindowLongPtr(videoWindow, GwlStyle);
        SetWindowLongPtr(videoWindow, GwlStyle, (nint)((style & ~WsPopup & ~WindowChrome) | WsChild | WsVisible));
        SetLastError(0);
        nint previousParent = SetParent(videoWindow, surface.Handle);
        if (previousParent == 0 && Marshal.GetLastWin32Error() != 0)
        {
            SetWindowLongPtr(videoWindow, GwlStyle, (nint)style);
            return false;
        }
        docked = videoWindow;
        SetWindowPos(videoWindow, 0, 0, 0, 0, 0,
                     SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder);
        Resize();
        return true;
    }

    public void Forget() => docked = 0;

    private void Resize()
    {
        if (IsDocked)
            MoveWindow(docked, 0, 0, surface.ClientSize.Width, surface.ClientSize.Height, true);
    }

    private delegate bool EnumWindowCallback(nint window, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowCallback callback, nint data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rect bounds);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetParent(nint child, nint parent);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool MoveWindow(nint window, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("kernel32.dll")] private static extern void SetLastError(uint code);
}
