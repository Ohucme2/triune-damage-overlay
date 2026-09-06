namespace TriuneDamageOverlay.Core;

public static class CombatAlertSourceFilter
{
    public static bool IsIgnored(string sourceName, string ignoredNames)
    {
        if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(ignoredNames)) return false;

        return ignoredNames
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(name => name.Equals(sourceName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
