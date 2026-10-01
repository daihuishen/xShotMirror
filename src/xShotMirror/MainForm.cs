// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 xShot Mirror contributors

using System.Drawing;

namespace XShotMirror;

internal sealed class MainForm : Form
{
    private readonly ReceiverHost receiver = new();
    private readonly VideoWindowDock dock;
    private readonly System.Windows.Forms.Timer windowTimer = new() { Interval = 400 };
    private readonly Label status = new() { AutoSize = true };
    private readonly Label device = new() { AutoSize = true };
    private readonly Label statusCaption = new() { AutoSize = true };
    private readonly Label deviceCaption = new() { AutoSize = true };
    private readonly Label pinCaption = new() { AutoSize = true };
    private readonly Label pin = new() { AutoSize = true, Text = "—", Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Panel video = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 22, 28) };
    private readonly Button start = new() { AutoSize = true };
    private readonly Button stop = new() { AutoSize = true, Enabled = false };
    private readonly Button fullScreen = new() { AutoSize = true };
    private readonly Button about = new() { AutoSize = true };
    private readonly Button quit = new() { AutoSize = true };
    private readonly ComboBox languageChoice = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 86 };
    private readonly TableLayoutPanel root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20) };
    private readonly FlowLayoutPanel details = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
    private readonly Label footer = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.DimGray
    };
    private bool wasMirroring;
    private UiLanguage language = UiLanguage.English;
    private string statusSource = "接收已停止";
    private string deviceSource = "未连接";
    private bool deviceConnecting;
    private bool isFullScreen;
    private Rectangle normalBounds;
    private FormWindowState normalState;

    public MainForm()
    {
        Text = "xShot Mirror";
        MinimumSize = new Size(900, 500);
        Size = new Size(1080, 700);
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
        languageChoice.Items.AddRange(new object[] { "English", "中文" });
        languageChoice.SelectedIndex = 0;
        actions.Controls.Add(languageChoice);
        actions.Controls.Add(start);
        actions.Controls.Add(stop);
        actions.Controls.Add(fullScreen);
        actions.Controls.Add(about);
        actions.Controls.Add(quit);
        header.Controls.Add(actions, 1, 0);

        details.Controls.Add(statusCaption);
        details.Controls.Add(status);
        details.Controls.Add(deviceCaption);
        details.Controls.Add(device);
        details.Controls.Add(pinCaption);
        details.Controls.Add(pin);
        header.Controls.Add(details, 0, 1);
        header.SetColumnSpan(details, 2);
        root.Controls.Add(header, 0, 0);

        root.Controls.Add(video, 0, 1);
        root.Controls.Add(footer, 0, 2);

        receiver.StatusChanged += value => UpdateUi(() => { statusSource = value; ApplyLanguage(); RefreshButtons(); });
        receiver.DeviceChanged += value => UpdateUi(() => { deviceSource = value; deviceConnecting = value.EndsWith("（连接中）", StringComparison.Ordinal); ApplyLanguage(); });
        receiver.PinChanged += value => UpdateUi(() => pin.Text = value);
        languageChoice.SelectedIndexChanged += (_, _) => { language = languageChoice.SelectedIndex == 1 ? UiLanguage.Chinese : UiLanguage.English; ApplyLanguage(); };
        start.Click += (_, _) => StartReceiving();
        stop.Click += (_, _) => { windowTimer.Stop(); receiver.Stop(); dock.Forget(); wasMirroring = false; RefreshButtons(); };
        fullScreen.Click += (_, _) => ToggleFullScreen();
        about.Click += (_, _) => MessageBox.Show(this, Localization.About(language), Localization.Text(language, "aboutTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                statusSource = "正在镜像";
                deviceSource = deviceSource.Replace("（连接中）", "", StringComparison.Ordinal);
                deviceConnecting = false;
                ApplyLanguage();
                wasMirroring = true;
            }
            else if (wasMirroring)
            {
                wasMirroring = false;
                deviceSource = "未连接";
                statusSource = "等待 iPhone 连接（同一局域网）";
                ApplyLanguage();
            }
        };
        FormClosing += (_, _) => { windowTimer.Stop(); receiver.Dispose(); };
        ApplyLanguage();
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
            statusSource = "无法启动接收";
            ApplyLanguage();
            MessageBox.Show(this, Localization.StartupError(language, error), Localization.Text(language, "startupTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshButtons()
    {
        start.Enabled = !receiver.IsRunning;
        stop.Enabled = receiver.IsRunning;
    }

    private void ApplyLanguage()
    {
        start.Text = Localization.Text(language, "start");
        stop.Text = Localization.Text(language, "stop");
        fullScreen.Text = Localization.Text(language, isFullScreen ? "exitFullscreen" : "fullscreen");
        about.Text = Localization.Text(language, "about");
        quit.Text = Localization.Text(language, "exit");
        statusCaption.Text = Localization.Text(language, "status");
        deviceCaption.Text = Localization.Text(language, "device");
        pinCaption.Text = Localization.Text(language, "pin");
        footer.Text = Localization.Text(language, "instructions");
        status.Text = Localization.Status(language, statusSource);
        device.Text = Localization.Device(language, deviceSource, deviceConnecting);
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
            isFullScreen = false;
        }
        ApplyLanguage();
    }

    private void UpdateUi(Action change)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(change); }
        catch (InvalidOperationException) { }
    }
}
