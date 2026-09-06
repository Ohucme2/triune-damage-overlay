using System.Runtime.InteropServices;
using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class ControlForm : Form
{
    private const int HotkeyId = 0x5444;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkD = 0x44;

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly OverlayForm _overlay;
    private readonly HandleForm _handle;
    private readonly AlertHandleForm _alertHandle;
    private readonly LogTailer _tailer = new();
    private readonly CombatAlertEngine _combatAlerts;
    private readonly NotifyIcon _tray;
    private readonly TextBox _logPath = new() { ReadOnly = true };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Silver };
    private readonly Label _character = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly Button _watchButton = new() { Text = "Start watching", AutoSize = true };
    private readonly Button _pauseButton = new() { Text = "Pause floating text", AutoSize = true };
    private bool _reallyExit;
    private bool _hotkeyRegistered;
    private AlertSettingsForm? _alertSettingsForm;

    public ControlForm()
    {
        Text = AppInfo.DisplayName;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 590);
        MinimumSize = new Size(640, 520);
        BackColor = Color.FromArgb(25, 27, 34);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);

        _overlay = new OverlayForm(_settings);
        _combatAlerts = new CombatAlertEngine(_settings, _overlay);
        _handle = new HandleForm(_settings);
        _handle.ToggleRequested += ToggleControlWindow;
        _handle.PositionChanged += () => _settings.Save();
        _alertHandle = new AlertHandleForm(_settings);
        _alertHandle.ToggleRequested += ToggleControlWindow;
        _alertHandle.PositionChanged += () => _settings.Save();

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = AppInfo.DisplayName,
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += (_, _) => ToggleControlWindow();

        BuildControls();
        _tailer.Damage += damage => _overlay.AddDamage(damage);
        _tailer.LineRead += _combatAlerts.ProcessLine;
        _tailer.Status += text => _status.Text = text;

        Load += (_, _) =>
        {
            _overlay.Show();
            if (_settings.ShowHandle) _handle.Show();
            if (_settings.ShowAlertHandle) _alertHandle.Show();
            _hotkeyRegistered = RegisterHotKey(Handle, HotkeyId, ModControl | ModShift, VkD);
            _status.Text = _hotkeyRegistered
                ? "Ready · Ctrl+Shift+D shows or hides this window"
                : "Ready · the hotkey is already in use; double-click the gold handle";
            if (File.Exists(_settings.LogPath)) StartWatching();
        };
        FormClosing += OnClosing;
    }

    private void BuildControls()
    {
        var title = new Label
        {
            Text = "TRIUNE DAMAGE OVERLAY  ·  VERSION " + AppInfo.Version,
            AutoSize = true,
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 208, 63)
        };
        var intro = new Label
        {
            Text = "Shows only damage caused by the selected client character. Other players and pets are ignored.",
            AutoSize = true,
            MaximumSize = new Size(550, 0),
            ForeColor = Color.Gainsboro
        };

        _logPath.Text = _settings.LogPath;
        _logPath.Width = 430;
        var browse = new Button { Text = "Choose log…", AutoSize = true };
        browse.Click += (_, _) => ChooseLog();
        _watchButton.Click += (_, _) => { if (_tailer.Running) StopWatching(); else StartWatching(); };
        _pauseButton.Click += (_, _) => ToggleText();
        var test = new Button { Text = "Preview all colors", AutoSize = true };
        test.Click += async (_, _) =>
        {
            test.Enabled = false;
            await ShowColorTestAsync();
            test.Enabled = true;
        };
        var hide = new Button { Text = "Hide this window", AutoSize = true };
        hide.Click += (_, _) => Hide();
        var alerts = new Button { Text = "Combat alerts…", AutoSize = true };
        alerts.Click += (_, _) => OpenCombatAlerts();

        var showAbility = new CheckBox { Text = "Show attack/spell name", AutoSize = true, Checked = _settings.ShowAbility };
        showAbility.CheckedChanged += (_, _) => { _settings.ShowAbility = showAbility.Checked; _settings.Save(); };
        var showHandle = new CheckBox { Text = "Show movable gold handle", AutoSize = true, Checked = _settings.ShowHandle };
        showHandle.CheckedChanged += (_, _) =>
        {
            _settings.ShowHandle = showHandle.Checked;
            if (showHandle.Checked) _handle.Show(); else _handle.Hide();
            _settings.Save();
        };

        var fontLabel = new Label { Text = "Text size", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var fontSize = new TrackBar { Minimum = 18, Maximum = 52, TickFrequency = 4, Value = Math.Clamp(_settings.FontSize, 18, 52), Width = 250 };
        fontSize.ValueChanged += (_, _) => { _settings.FontSize = fontSize.Value; _settings.Save(); };

        var speedLabel = new Label { Text = "Scroll speed", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var scrollSpeed = new TrackBar { Minimum = 30, Maximum = 160, TickFrequency = 10, Value = Math.Clamp(_settings.ScrollPixelsPerSecond, 30, 160), Width = 250 };
        scrollSpeed.ValueChanged += (_, _) => { _settings.ScrollPixelsPerSecond = scrollSpeed.Value; _settings.Save(); };

        var fadeLabel = new Label { Text = "Fade after", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var fadeSeconds = new NumericUpDown
        {
            DecimalPlaces = 1,
            Increment = 0.2M,
            Minimum = 0.8M,
            Maximum = 8.0M,
            Value = Math.Clamp(_settings.LifetimeMilliseconds / 1000M, 0.8M, 8.0M),
            Width = 70
        };
        var secondsLabel = new Label { Text = "seconds per hit", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        fadeSeconds.ValueChanged += (_, _) => { _settings.LifetimeMilliseconds = (int)(fadeSeconds.Value * 1000); _settings.Save(); };

        var logRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        logRow.Controls.Add(_logPath);
        logRow.Controls.Add(browse);
        var actionRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(580, 0) };
        actionRow.Controls.AddRange(new Control[] { _watchButton, _pauseButton, test, alerts, hide });
        var sizeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        sizeRow.Controls.Add(fontLabel);
        sizeRow.Controls.Add(fontSize);
        var speedRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        speedRow.Controls.Add(speedLabel);
        speedRow.Controls.Add(scrollSpeed);
        var fadeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        fadeRow.Controls.Add(fadeLabel);
        fadeRow.Controls.Add(fadeSeconds);
        fadeRow.Controls.Add(secondsLabel);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24),
            AutoScroll = true
        };
        layout.Controls.Add(title);
        layout.Controls.Add(intro);
        layout.SetFlowBreak(intro, true);
        layout.Controls.Add(new Label { Text = "EverQuest log file", AutoSize = true, Padding = new Padding(0, 12, 0, 0) });
        layout.Controls.Add(logRow);
        layout.Controls.Add(_character);
        layout.Controls.Add(actionRow);
        layout.Controls.Add(showAbility);
        layout.Controls.Add(showHandle);
        layout.Controls.Add(sizeRow);
        layout.Controls.Add(speedRow);
        layout.Controls.Add(fadeRow);
        layout.Controls.Add(new Label { Text = "COLOR LEGEND", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Padding(0, 8, 0, 0) });
        layout.Controls.Add(BuildColorLegend());
        layout.Controls.Add(new Label
        {
            Text = "Drag the gold handle to position the floating text. Double-click it—or press Ctrl+Shift+D—to show or hide this window. Floating text keeps running while this window is hidden.",
            AutoSize = true,
            MaximumSize = new Size(540, 0),
            Padding = new Padding(0, 12, 0, 0),
            ForeColor = Color.LightGray
        });
        layout.Controls.Add(_status);
        Controls.Add(layout);
    }

    private void OpenCombatAlerts()
    {
        if (_alertSettingsForm is { IsDisposed: false })
        {
            _alertSettingsForm.Show();
            _alertSettingsForm.Activate();
            return;
        }
        _alertSettingsForm = new AlertSettingsForm(_settings, _overlay, _alertHandle);
        _alertSettingsForm.Show(this);
    }

    private static FlowLayoutPanel BuildColorLegend()
    {
        var legend = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(565, 0) };
        var entries = new (string Label, DamageKind Kind)[]
        {
            ("CRIT", DamageKind.Critical), ("Slash", DamageKind.Slashing),
            ("Pierce", DamageKind.Piercing), ("Crush", DamageKind.Crushing),
            ("Kick", DamageKind.Kick), ("Bash", DamageKind.Bash),
            ("Backstab", DamageKind.Backstab), ("Ranged", DamageKind.Ranged),
            ("Spell", DamageKind.Spell), ("Proc", DamageKind.Proc),
            ("DoT", DamageKind.DamageOverTime)
        };
        foreach (var entry in entries)
        {
            legend.Controls.Add(new Label
            {
                Text = "● " + entry.Label,
                AutoSize = true,
                ForeColor = OverlayForm.ColorFor(entry.Kind),
                Margin = new Padding(3, 2, 9, 2)
            });
        }
        return legend;
    }

    private async Task ShowColorTestAsync()
    {
        var samples = new (int Amount, DamageKind Kind, string Ability)[]
        {
            (112, DamageKind.Slashing, "slash"),
            (124, DamageKind.Piercing, "pierce"),
            (138, DamageKind.Crushing, "crush"),
            (96, DamageKind.Kick, "kick"),
            (104, DamageKind.Bash, "bash"),
            (287, DamageKind.Backstab, "backstab"),
            (143, DamageKind.Ranged, "shoot"),
            (436, DamageKind.Spell, "Fireball"),
            (211, DamageKind.Proc, "Flame Proc"),
            (78, DamageKind.DamageOverTime, "Immolate"),
            (1284, DamageKind.Critical, "CRIT")
        };
        _status.Text = "Showing color preview in hit order…";
        foreach (var sample in samples)
        {
            _overlay.AddDamage(new DamageEvent(sample.Amount, sample.Kind, sample.Ability, "test target", string.Empty));
            await Task.Delay(140);
        }
        _status.Text = "Color preview complete · critical hits are red";
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / hide controls", null, (_, _) => ToggleControlWindow());
        menu.Items.Add("Pause / resume floating text", null, (_, _) => ToggleText());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ChooseLog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose this character's EverQuest log",
            Filter = "EverQuest logs (eqlog_*.txt)|eqlog_*.txt|Text files (*.txt)|*.txt",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings.LogPath = dialog.FileName;
        _settings.Save();
        _logPath.Text = dialog.FileName;
        StartWatching();
    }

    private void StartWatching()
    {
        if (_tailer.Start(_settings.LogPath))
        {
            _combatAlerts.ClientCharacterName = _tailer.CharacterName;
            _watchButton.Text = "Stop watching";
            _character.Text = $"Character: {_tailer.CharacterName}";
            _character.ForeColor = Color.FromArgb(112, 226, 148);
        }
    }

    private void StopWatching()
    {
        _tailer.Stop();
        _watchButton.Text = "Start watching";
        _status.Text = "Log watching stopped. Floating text is still ready.";
    }

    private void ToggleText()
    {
        _overlay.TextEnabled = !_overlay.TextEnabled;
        _pauseButton.Text = _overlay.TextEnabled ? "Pause floating text" : "Resume floating text";
        _status.Text = _overlay.TextEnabled ? "Floating text resumed" : "Floating text paused";
    }

    private void ToggleControlWindow()
    {
        if (Visible) Hide();
        else { Show(); WindowState = FormWindowState.Normal; Activate(); }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId) ToggleControlWindow();
        base.WndProc(ref m);
    }

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBalloonTip(1500, AppInfo.DisplayName, "Controls hidden. Floating damage is still running.", ToolTipIcon.Info);
            return;
        }

        if (_hotkeyRegistered) UnregisterHotKey(Handle, HotkeyId);
        _tailer.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _settings.Save();
        _handle.Close();
        _alertHandle.Close();
        _overlay.Close();
    }

    private void ExitApplication()
    {
        _reallyExit = true;
        Close();
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
