namespace XShotMirror;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var instance = new Mutex(true, "xShotMirror.SingleInstance", out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show(
                "xShot Mirror is already running. Switch to the open window.\n\n" +
                "xShot Mirror 已在运行，请切换到已打开的窗口。",
                "xShot Mirror");
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
