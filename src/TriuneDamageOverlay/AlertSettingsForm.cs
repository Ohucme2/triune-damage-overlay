namespace TriuneDamageOverlay;

internal sealed class AlertSettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly OverlayForm _overlay;
    private readonly AlertHandleForm _alertHandle;
    private readonly CheckedListBox _rules = new() { CheckOnClick = true, Width = 235, Height = 235 };
    private readonly TextBox _name = new() { Width = 270 };
    private readonly TextBox _trigger = new() { Width = 270 };
    private readonly TextBox _display = new() { Width = 270 };
    private readonly NumericUpDown _duration = new() { Minimum = 0.5M, Maximum = 15, Increment = 0.5M, DecimalPlaces = 1, Width = 80 };
    private readonly NumericUpDown _cooldown = new() { Minimum = 0, Maximum = 60, Increment = 0.5M, DecimalPlaces = 1, Width = 80 };
    private readonly Button _color = new() { Text = "Choose color…", AutoSize = true };
    private readonly CheckBox _enabled = new() { Text = "Enable combat alerts", AutoSize = true };
    private readonly TrackBar _alertSize = new() { Minimum = 40, Maximum = 200, TickFrequency = 20, Width = 280 };
    private readonly Label _alertSizeValue = new() { AutoSize = true, Padding = new Padding(4, 6, 0, 0) };
    private readonly CheckBox _showAlertHandle = new() { Text = "Show movable orange ALERT handle", AutoSize = true };
    private readonly TextBox _ignoredSources = new() { Width = 345 };
    private Color _chosenColor = Color.Red;

    public AlertSettingsForm(AppSettings settings, OverlayForm overlay, AlertHandleForm alertHandle)
    {
        _settings = settings;
        _overlay = overlay;
        _alertHandle = alertHandle;
        Text = "Combat Alerts · TRIUNE v" + AppInfo.Version;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 600);
        MinimumSize = new Size(700, 600);
        BackColor = Color.FromArgb(25, 27, 34);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);

        _enabled.Checked = settings.CombatAlertsEnabled;
        _enabled.CheckedChanged += (_, _) => { settings.CombatAlertsEnabled = _enabled.Checked; settings.Save(); };
        _alertSize.Value = Math.Clamp(settings.CombatAlertScalePercent, _alertSize.Minimum, _alertSize.Maximum);
        UpdateAlertSizeLabel();
        _alertSize.ValueChanged += (_, _) =>
        {
            settings.CombatAlertScalePercent = _alertSize.Value;
            UpdateAlertSizeLabel();
            settings.Save();
        };
        _showAlertHandle.Checked = settings.ShowAlertHandle;
        _showAlertHandle.CheckedChanged += (_, _) =>
        {
            settings.ShowAlertHandle = _showAlertHandle.Checked;
            if (_showAlertHandle.Checked) _alertHandle.Show(); else _alertHandle.Hide();
            settings.Save();
        };
        _ignoredSources.Text = settings.IgnoredCombatAlertSources;
        _ignoredSources.TextChanged += (_, _) =>
        {
            settings.IgnoredCombatAlertSources = _ignoredSources.Text;
            settings.Save();
        };
        _rules.SelectedIndexChanged += (_, _) => LoadSelected();
        _rules.ItemCheck += (_, e) =>
        {
            if (e.Index >= 0 && e.Index < _settings.AlertRules.Count)
            {
                _settings.AlertRules[e.Index].Enabled = e.NewValue == CheckState.Checked;
                _settings.Save();
            }
        };
        _color.Click += (_, _) => ChooseColor();

        BuildControls();
        ReloadRules();
        FormClosed += (_, _) => _settings.Save();
    }

    private void BuildControls()
    {
        var add = new Button { Text = "Add custom", AutoSize = true };
        var remove = new Button { Text = "Remove", AutoSize = true };
        var save = new Button { Text = "Save changes", AutoSize = true };
        var test = new Button { Text = "Test selected alert", AutoSize = true };
        add.Click += (_, _) => AddCustom();
        remove.Click += (_, _) => RemoveSelected();
        save.Click += (_, _) => SaveSelected();
        test.Click += (_, _) => TestSelected();

        var leftButtons = new FlowLayoutPanel { AutoSize = true };
        leftButtons.Controls.AddRange(new Control[] { add, remove });
        var left = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, AutoSize = true };
        left.Controls.Add(new Label { Text = "ALERT RULES", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        left.Controls.Add(_rules);
        left.Controls.Add(leftButtons);

        var editor = new TableLayoutPanel { ColumnCount = 2, RowCount = 7, Dock = DockStyle.Fill, AutoSize = true };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddEditorRow(editor, 0, "Name", _name);
        AddEditorRow(editor, 1, "Trigger phrase", _trigger);
        AddEditorRow(editor, 2, "Alert text", _display);
        AddEditorRow(editor, 3, "Duration", WithSuffix(_duration, "seconds"));
        AddEditorRow(editor, 4, "Cooldown", WithSuffix(_cooldown, "seconds"));
        AddEditorRow(editor, 5, "Color", _color);
        var editorButtons = new FlowLayoutPanel { AutoSize = true };
        editorButtons.Controls.AddRange(new Control[] { save, test });
        AddEditorRow(editor, 6, string.Empty, editorButtons);

        var right = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, AutoSize = true };
        right.Controls.Add(new Label
        {
            Text = "Use {mob} in the alert text to include everything before the trigger phrase. Built-in rules can be disabled or edited; custom rules can also be removed.",
            AutoSize = true,
            MaximumSize = new Size(350, 0),
            ForeColor = Color.Gainsboro
        });
        right.Controls.Add(editor);

        var sizeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 2) };
        sizeRow.Controls.Add(new Label { Text = "Alert size", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        sizeRow.Controls.Add(_alertSize);
        sizeRow.Controls.Add(_alertSizeValue);
        sizeRow.Controls.Add(new Label { Text = "40% subtle  →  200% BLAMO!", AutoSize = true, ForeColor = Color.Gainsboro, Padding = new Padding(8, 6, 0, 0) });

        var handleRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 2) };
        handleRow.Controls.Add(_showAlertHandle);
        handleRow.Controls.Add(new Label { Text = "Drag it anywhere; alerts appear there.", AutoSize = true, ForeColor = Color.Gainsboro, Padding = new Padding(8, 3, 0, 0) });

        var ignoredRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(20, 0, 0, 3) };
        ignoredRow.Controls.Add(new Label { Text = "Ignore friendly names", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        ignoredRow.Controls.Add(_ignoredSources);
        ignoredRow.Controls.Add(new Label { Text = "separate with commas", AutoSize = true, ForeColor = Color.Gainsboro, Padding = new Padding(6, 6, 0, 0) });

        var columns = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, Padding = new Padding(20) };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        columns.Controls.Add(left, 0, 0);
        columns.Controls.Add(right, 1, 0);

        var root = new TableLayoutPanel { RowCount = 6, Dock = DockStyle.Fill };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label
        {
            Text = "COMBAT ALERTS",
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 208, 63),
            Padding = new Padding(20, 15, 0, 0)
        }, 0, 0);
        root.Controls.Add(_enabled, 0, 1);
        _enabled.Margin = new Padding(23, 8, 0, 4);
        root.Controls.Add(sizeRow, 0, 2);
        root.Controls.Add(handleRow, 0, 3);
        root.Controls.Add(ignoredRow, 0, 4);
        root.Controls.Add(columns, 0, 5);
        Controls.Add(root);
    }

    private static void AddEditorRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 7, 8, 0) }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static FlowLayoutPanel WithSuffix(Control control, string suffix)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        row.Controls.Add(control);
        row.Controls.Add(new Label { Text = suffix, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        return row;
    }

    private void ReloadRules(int selectIndex = 0)
    {
        _rules.Items.Clear();
        foreach (var rule in _settings.AlertRules)
            _rules.Items.Add(rule.Name + (rule.BuiltIn ? "  [built-in]" : string.Empty), rule.Enabled);
        if (_rules.Items.Count > 0) _rules.SelectedIndex = Math.Clamp(selectIndex, 0, _rules.Items.Count - 1);
    }

    private void LoadSelected()
    {
        if (_rules.SelectedIndex < 0 || _rules.SelectedIndex >= _settings.AlertRules.Count) return;
        var rule = _settings.AlertRules[_rules.SelectedIndex];
        _name.Text = rule.Name;
        _trigger.Text = rule.TriggerPhrase;
        _display.Text = rule.DisplayText;
        _duration.Value = Math.Clamp(rule.DurationMilliseconds / 1000M, _duration.Minimum, _duration.Maximum);
        _cooldown.Value = Math.Clamp(rule.CooldownMilliseconds / 1000M, _cooldown.Minimum, _cooldown.Maximum);
        _chosenColor = Color.FromArgb(rule.ColorArgb);
        UpdateColorButton();
    }

    private void SaveSelected()
    {
        if (_rules.SelectedIndex < 0 || string.IsNullOrWhiteSpace(_trigger.Text)) return;
        var index = _rules.SelectedIndex;
        var rule = _settings.AlertRules[index];
        rule.Name = string.IsNullOrWhiteSpace(_name.Text) ? "Alert" : _name.Text.Trim();
        rule.TriggerPhrase = _trigger.Text.Trim();
        rule.DisplayText = string.IsNullOrWhiteSpace(_display.Text) ? rule.TriggerPhrase : _display.Text.Trim();
        rule.DurationMilliseconds = (int)(_duration.Value * 1000);
        rule.CooldownMilliseconds = (int)(_cooldown.Value * 1000);
        rule.ColorArgb = _chosenColor.ToArgb();
        _settings.Save();
        ReloadRules(index);
    }

    private void AddCustom()
    {
        _settings.AlertRules.Add(new AlertRuleSettings
        {
            Name = "New custom alert",
            TriggerPhrase = "enter exact log phrase",
            DisplayText = "SPECIAL! — {mob}",
            ColorArgb = Color.FromArgb(255, 70, 210).ToArgb(),
            BuiltIn = false
        });
        _settings.Save();
        ReloadRules(_settings.AlertRules.Count - 1);
    }

    private void RemoveSelected()
    {
        if (_rules.SelectedIndex < 0) return;
        var index = _rules.SelectedIndex;
        if (_settings.AlertRules[index].BuiltIn)
        {
            MessageBox.Show(this, "Built-in alerts can be disabled but not removed.", "TRIUNE Combat Alerts", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _settings.AlertRules.RemoveAt(index);
        _settings.Save();
        ReloadRules(Math.Max(0, index - 1));
    }

    private void TestSelected()
    {
        if (_rules.SelectedIndex < 0) return;
        var rule = _settings.AlertRules[_rules.SelectedIndex];
        var text = rule.DisplayText.Replace("{mob}", "Test Enemy", StringComparison.OrdinalIgnoreCase)
            .Replace("{line}", "Test combat line", StringComparison.OrdinalIgnoreCase);
        _overlay.AddCombatAlert(text, Color.FromArgb(rule.ColorArgb), rule.DurationMilliseconds);
    }

    private void ChooseColor()
    {
        using var dialog = new ColorDialog { Color = _chosenColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _chosenColor = dialog.Color;
        UpdateColorButton();
    }

    private void UpdateColorButton()
    {
        _color.BackColor = _chosenColor;
        _color.ForeColor = _chosenColor.GetBrightness() < .45f ? Color.White : Color.Black;
    }

    private void UpdateAlertSizeLabel() => _alertSizeValue.Text = _alertSize.Value + "%";
}
