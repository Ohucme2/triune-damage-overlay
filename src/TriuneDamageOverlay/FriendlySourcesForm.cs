using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class FriendlySourcesForm : Form
{
    private readonly AppSettings _settings;
    private readonly CombatAlertEngine _engine;
    private readonly ListView _sources = new();
    private readonly ListView _history = new();
    private readonly TextBox _manualName = new() { Width = 230, PlaceholderText = "Type a pet or box name", AccessibleName = "Pet or box name" };

    public FriendlySourcesForm(AppSettings settings, CombatAlertEngine engine)
    {
        _settings = settings;
        _engine = engine;
        Text = "Friendly Sources and Alert History · TRIUNE v" + AppInfo.Version;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(790, 570);
        MinimumSize = new Size(700, 520);
        BackColor = Color.FromArgb(25, 27, 34);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);

        ConfigureSourcesList();
        ConfigureHistoryList();
        BuildControls();
        _engine.ObservationsChanged += RefreshViews;
        FormClosed += (_, _) => _engine.ObservationsChanged -= RefreshViews;
        RefreshViews();
    }

    private void ConfigureSourcesList()
    {
        _sources.View = View.Details;
        _sources.FullRowSelect = true;
        _sources.MultiSelect = false;
        _sources.HideSelection = false;
        _sources.Dock = DockStyle.Fill;
        _sources.Columns.Add("Name", 220);
        _sources.Columns.Add("Last event", 180);
        _sources.Columns.Add("Status", 180);
        _sources.Columns.Add("Last seen", 130);
    }

    private void ConfigureHistoryList()
    {
        _history.View = View.Details;
        _history.FullRowSelect = true;
        _history.HideSelection = false;
        _history.ShowItemToolTips = true;
        _history.Dock = DockStyle.Fill;
        _history.Columns.Add("Time", 90);
        _history.Columns.Add("Source", 210);
        _history.Columns.Add("Rule", 160);
        _history.Columns.Add("Result", 250);
    }

    private void BuildControls()
    {
        var markFriendly = new Button { Text = "Mark selected friendly", AutoSize = true };
        var allow = new Button { Text = "Allow selected", AutoSize = true };
        var addManual = new Button { Text = "Add friendly name", AutoSize = true };
        var clearHistory = new Button { Text = "Clear history", AutoSize = true };
        markFriendly.Click += (_, _) => MarkSelectedFriendly();
        allow.Click += (_, _) => AllowSelected();
        addManual.Click += (_, _) => AddManualFriendly();
        clearHistory.Click += (_, _) => _engine.ClearHistory();
        _manualName.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            AddManualFriendly();
            e.SuppressKeyPress = true;
        };

        var sourceButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
        sourceButtons.Controls.AddRange(new Control[] { markFriendly, allow, _manualName, addManual });

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, Padding = new Padding(18) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = "FRIENDLY SOURCES",
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 208, 63)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = $"Profile: {_settings.ActiveProfile.DisplayName}  ·  Select a recently seen box or pet and mark it friendly. The choice saves automatically.",
            AutoSize = true,
            MaximumSize = new Size(740, 0),
            ForeColor = Color.Gainsboro,
            Margin = new Padding(0, 4, 0, 8)
        }, 0, 1);
        layout.Controls.Add(_sources, 0, 2);
        layout.Controls.Add(sourceButtons, 0, 3);
        layout.Controls.Add(new Label
        {
            Text = "ALERT HISTORY",
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 190, 95),
            Margin = new Padding(0, 12, 0, 4)
        }, 0, 4);
        layout.Controls.Add(_history, 0, 5);
        layout.Controls.Add(clearHistory, 0, 6);
        Controls.Add(layout);
    }

    private void RefreshViews()
    {
        if (IsDisposed) return;
        _sources.BeginUpdate();
        var selectedName = _sources.SelectedItems.Count > 0 ? _sources.SelectedItems[0].Tag as string : null;
        _sources.Items.Clear();
        var recentSources = _engine.GetRecentSources().ToList();
        foreach (var friendly in _settings.FriendlySources)
            if (!recentSources.Any(x => x.Name.Equals(friendly, StringComparison.OrdinalIgnoreCase)))
                recentSources.Add(new RecentAlertSource(friendly, "Saved friendly", DateTime.MinValue));
        foreach (var source in recentSources.OrderByDescending(x => x.LastSeen).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var status = source.Name.Equals(_engine.ClientCharacterName, StringComparison.OrdinalIgnoreCase)
                ? "This client · ignored"
                : CombatAlertSourceFilter.IsIgnored(source.Name, _settings.FriendlySources)
                    ? "Friendly · ignored"
                    : "Allowed";
            var lastSeen = source.LastSeen == DateTime.MinValue ? "—" : source.LastSeen.ToLocalTime().ToString("h:mm:ss tt");
            var item = new ListViewItem(new[] { source.Name, source.LastRuleName, status, lastSeen })
            {
                Tag = source.Name
            };
            _sources.Items.Add(item);
            if (source.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase)) item.Selected = true;
        }
        _sources.EndUpdate();

        _history.BeginUpdate();
        _history.Items.Clear();
        foreach (var observation in _engine.GetHistory())
        {
            var result = observation.Result switch
            {
                CombatAlertResult.Displayed => "Displayed",
                CombatAlertResult.FriendlyFiltered => "Suppressed · friendly",
                CombatAlertResult.ClientFiltered => "Suppressed · this client",
                CombatAlertResult.Cooldown => "Suppressed · cooldown",
                _ => observation.Result.ToString()
            };
            _history.Items.Add(new ListViewItem(new[]
            {
                observation.Time.ToLocalTime().ToString("h:mm:ss tt"), observation.Source, observation.RuleName, result
            }) { ToolTipText = observation.OriginalLine });
        }
        _history.EndUpdate();
    }

    private void MarkSelectedFriendly()
    {
        if (_sources.SelectedItems.Count == 0) return;
        AddFriendly(_sources.SelectedItems[0].Tag as string);
    }

    private void AllowSelected()
    {
        if (_sources.SelectedItems.Count == 0) return;
        var name = _sources.SelectedItems[0].Tag as string;
        if (string.IsNullOrWhiteSpace(name)) return;
        _settings.FriendlySources.RemoveAll(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
        _settings.Save();
        RefreshViews();
    }

    private void AddManualFriendly()
    {
        foreach (var name in _manualName.Text.Split(
            new[] { ',', ';', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            AddFriendly(name);
        _manualName.Clear();
    }

    private void AddFriendly(string? name)
    {
        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!_settings.FriendlySources.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
            _settings.FriendlySources.Add(name);
        _settings.Save();
        RefreshViews();
    }
}
