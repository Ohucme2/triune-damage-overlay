namespace TriuneDamageOverlay.Core;

public sealed class CombatAlertCooldownTracker
{
    private readonly Dictionary<string, DateTime> _lastTriggered = new(StringComparer.OrdinalIgnoreCase);

    public bool IsCoolingDown(string ruleId, string sourceName, DateTime now, int cooldownMilliseconds)
    {
        var key = ruleId + "\u001f" + sourceName.Trim();
        if (_lastTriggered.TryGetValue(key, out var last) &&
            (now - last).TotalMilliseconds < cooldownMilliseconds) return true;
        _lastTriggered[key] = now;
        return false;
    }

    public void Clear() => _lastTriggered.Clear();
}
