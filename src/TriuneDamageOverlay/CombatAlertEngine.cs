using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class CombatAlertEngine
{
    private readonly AppSettings _settings;
    private readonly OverlayForm _overlay;
    private readonly CombatAlertMatcher _matcher = new();
    private readonly Dictionary<string, DateTime> _lastTriggered = new(StringComparer.OrdinalIgnoreCase);

    public CombatAlertEngine(AppSettings settings, OverlayForm overlay)
    {
        _settings = settings;
        _overlay = overlay;
    }

    public void ProcessLine(string line)
    {
        if (!_settings.CombatAlertsEnabled) return;
        var now = DateTime.UtcNow;
        foreach (var setting in _settings.AlertRules.Where(x => x.Enabled))
        {
            if (!_matcher.TryMatch(line, setting.ToRule(), out var match) || match is null) continue;
            if (_lastTriggered.TryGetValue(setting.Id, out var last) &&
                (now - last).TotalMilliseconds < setting.CooldownMilliseconds) continue;

            _lastTriggered[setting.Id] = now;
            _overlay.AddCombatAlert(match.Text, Color.FromArgb(setting.ColorArgb), setting.DurationMilliseconds);
        }
    }
}
