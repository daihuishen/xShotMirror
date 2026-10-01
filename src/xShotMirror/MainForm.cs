using System.Drawing;

namespace XShotMirror;

internal sealed class MainForm : Form
{
    private readonly ReceiverHost receiver = new();
    private readonly VideoWindowDock dock;
    private readonly System.Windows.Forms.Timer windowTimer = new() { Interval = 400 };
    private readonly Label status = new() { AutoSize = true, Text = "接收已停止" };
    private readonly Label device = new() { AutoSize = true, Text = "未连接" };
    private readonly Label pin = new() { AutoSize = true, Text = "—", Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Panel video = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 22, 28) };
    private readonly Button start = new() { Text = "开始接收", AutoSize = true };
    private readonly Button stop = new() { Text = "停止接收", AutoSize = true, Enabled = false };
    private readonly Button fullScreen = new() { Text = "全屏显示", AutoSize = true };
    private readonly TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20) };
    private readonly FlowLayoutPanel details = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
    private readonly Label footer = new()
    {
        Text = "在 iPhone 控制中心打开“屏幕镜像”，选择 xShot Mirror。电脑和 iPhone 需位于同一局域网。",
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.DimGray
    };
    private bool wasMirroring;
    private bool isFullScreen;
    private Rectangle normalBounds;
    private FormWindowState normalState;

    public MainForm()
    {
        Text = "xShot Mirror";
        MinimumSize = new Size(700, 500);
        Size = new Size(980, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);
        KeyPreview = true;

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);
        dock = new VideoWindowDock(video);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = new Label { Text = "xShot Mirror", AutoSize = true, Font = new Font("Segoe UI", 21, FontStyle.Bold) };
        header.Controls.Add(title, 0, 0);

        var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var quit = new Button { Text = "退出", AutoSize = true };
        actions.Controls.Add(start);
        actions.Controls.Add(stop);
        actions.Controls.Add(fullScreen);
        actions.Controls.Add(quit);
        header.Controls.Add(actions, 1, 0);

        details.Controls.Add(new Label { Text = "状态：", AutoSize = true });
        details.Controls.Add(status);
        details.Controls.Add(new Label { Text = "    设备：", AutoSize = true });
        details.Controls.Add(device);
        details.Controls.Add(new Label { Text = "    配对码：", AutoSize = true });
        details.Controls.Add(pin);
        header.Controls.Add(details, 0, 1);
        header.SetColumnSpan(details, 2);
        root.Controls.Add(header, 0, 0);

        root.Controls.Add(video, 0, 1);
        root.Controls.Add(footer, 0, 2);

        receiver.StatusChanged += value => UpdateUi(() => { status.Text = value; RefreshButtons(); });
        receiver.DeviceChanged += value => UpdateUi(() => device.Text = value);
        receiver.PinChanged += value => UpdateUi(() => pin.Text = value);
        start.Click += (_, _) => StartReceiving();
        stop.Click += (_, _) => { windowTimer.Stop(); receiver.Stop(); dock.Forget(); wasMirroring = false; RefreshButtons(); };
        fullScreen.Click += (_, _) => ToggleFullScreen();
        quit.Click += (_, _) => Close();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F11 || (isFullScreen && e.KeyCode == Keys.Escape))
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        };
        Shown += (_, _) => StartReceiving();
        windowTimer.Tick += (_, _) =>
        {
            int? processId = receiver.ProcessId;
            if (processId is not null && dock.TryDock(processId.Value))
            {
                status.Text = "正在镜像";
                device.Text = device.Text.Replace("（连接中）", "");
                wasMirroring = true;
            }
            else if (wasMirroring)
            {
                wasMirroring = false;
                device.Text = "未连接";
                status.Text = "等待 iPhone 连接（同一局域网）";
            }
        };
        FormClosing += (_, _) => { windowTimer.Stop(); receiver.Dispose(); };
    }

    private void StartReceiving()
    {
        try
        {
            receiver.Start();
            windowTimer.Start();
            RefreshButtons();
        }
        catch (Exception error)
        {
            status.Text = "无法启动接收";
            MessageBox.Show(this, error.Message, "xShot Mirror", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshButtons()
    {
        start.Enabled = !receiver.IsRunning;
        stop.Enabled = receiver.IsRunning;
    }

    private void ToggleFullScreen()
    {
        if (!isFullScreen)
        {
            normalState = WindowState;
            normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            root.Padding = Padding.Empty;
            root.RowStyles[0].Height = 52;
            root.RowStyles[2].Height = 0;
            details.Visible = false;
            footer.Visible = false;
            WindowState = FormWindowState.Maximized;
            fullScreen.Text = "退出全屏";
            isFullScreen = true;
        }
        else
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.Sizable;
            Bounds = normalBounds;
            root.Padding = new Padding(20);
            root.RowStyles[0].Height = 115;
            root.RowStyles[2].Height = 42;
            details.Visible = true;
            footer.Visible = true;
            WindowState = normalState;
            fullScreen.Text = "全屏显示";
            isFullScreen = false;
        }
    }

    private void UpdateUi(Action change)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(change); }
        catch (InvalidOperationException) { }
    }
}
