namespace TriuneDamageOverlay.Core;

public static class CombatAlertSourceFilter
{
    public static bool IsIgnored(string sourceName, string ignoredNames)
    {
        if (string.IsNullOrWhiteSpace(ignoredNames)) return false;
        return IsIgnored(sourceName, ignoredNames.Split(
            new[] { ',', ';', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static bool IsIgnored(string sourceName, IEnumerable<string> ignoredNames) =>
        !string.IsNullOrWhiteSpace(sourceName) &&
        ignoredNames.Any(name => name.Trim().Equals(sourceName.Trim(), StringComparison.OrdinalIgnoreCase));
}
