using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class CombatAlertEngine
{
    private readonly AppSettings _settings;
    private readonly OverlayForm _overlay;
    private readonly CombatAlertMatcher _matcher = new();
    private readonly CombatAlertCooldownTracker _cooldowns = new();
    private readonly Dictionary<string, RecentAlertSource> _recentSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CombatAlertObservation> _history = new();

    public string ClientCharacterName { get; set; } = string.Empty;
    public event Action? ObservationsChanged;

    public CombatAlertEngine(AppSettings settings, OverlayForm overlay)
    {
        _settings = settings;
        _overlay = overlay;
    }

    public IReadOnlyList<RecentAlertSource> GetRecentSources() => _recentSources.Values
        .OrderByDescending(x => x.LastSeen)
        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public IReadOnlyList<CombatAlertObservation> GetHistory() => _history
        .OrderByDescending(x => x.Time)
        .ToList();

    public void ClearHistory()
    {
        _history.Clear();
        ObservationsChanged?.Invoke();
    }

    public void ResetForProfile()
    {
        ClientCharacterName = string.Empty;
        _cooldowns.Clear();
        _recentSources.Clear();
        _history.Clear();
        ObservationsChanged?.Invoke();
    }

    public void ProcessLine(string line)
    {
        if (!_settings.CombatAlertsEnabled) return;
        var now = DateTime.UtcNow;
        foreach (var setting in _settings.AlertRules.Where(x => x.Enabled))
        {
            if (!_matcher.TryMatch(line, setting.ToRule(), out var match) || match is null) continue;
            var source = string.IsNullOrWhiteSpace(match.MobName) ? "(unknown source)" : match.MobName;
            _recentSources[source] = new RecentAlertSource(source, setting.Name, now);

            if (source.Equals(ClientCharacterName, StringComparison.OrdinalIgnoreCase))
            {
                Record(now, source, setting.Name, CombatAlertResult.ClientFiltered, line);
                continue;
            }
            if (CombatAlertSourceFilter.IsIgnored(source, _settings.FriendlySources))
            {
                Record(now, source, setting.Name, CombatAlertResult.FriendlyFiltered, line);
                continue;
            }
            if (_cooldowns.IsCoolingDown(setting.Id, source, now, setting.CooldownMilliseconds))
            {
                Record(now, source, setting.Name, CombatAlertResult.Cooldown, line);
                continue;
            }

            _overlay.AddCombatAlert(match.Text, Color.FromArgb(setting.ColorArgb), setting.DurationMilliseconds);
            Record(now, source, setting.Name, CombatAlertResult.Displayed, line);
        }
    }

    private void Record(DateTime time, string source, string ruleName, CombatAlertResult result, string line)
    {
        _history.Add(new CombatAlertObservation(time, source, ruleName, result, line));
        if (_history.Count > 100) _history.RemoveAt(0);
        if (_recentSources.Count > 100)
        {
            var oldest = _recentSources.Values.MinBy(x => x.LastSeen);
            if (oldest is not null) _recentSources.Remove(oldest.Name);
        }
        ObservationsChanged?.Invoke();
    }
}
