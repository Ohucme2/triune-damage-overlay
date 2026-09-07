namespace TriuneDamageOverlay;

internal enum CombatAlertResult
{
    Displayed,
    FriendlyFiltered,
    ClientFiltered,
    Cooldown
}

internal sealed record CombatAlertObservation(
    DateTime Time,
    string Source,
    string RuleName,
    CombatAlertResult Result,
    string OriginalLine);

internal sealed record RecentAlertSource(
    string Name,
    string LastRuleName,
    DateTime LastSeen);
