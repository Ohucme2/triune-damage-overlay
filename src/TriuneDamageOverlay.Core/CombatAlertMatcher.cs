using System.Text.RegularExpressions;

namespace TriuneDamageOverlay.Core;

public sealed record CombatAlertRule(string Id, string TriggerPhrase, string DisplayText);
public sealed record CombatAlertMatch(string RuleId, string Text, string MobName, string OriginalLine);

/// <summary>
/// Matches configured phrases only in system/emote-style log lines. Chat lines are
/// rejected before matching so another player cannot create alerts by quoting a trigger.
/// </summary>
public sealed class CombatAlertMatcher
{
    private static readonly Regex Timestamp = new(@"^\[[^\]]+\]\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ChatLine = new(
        @"^(?:You\s+(?:say|tell|shout|auction)|.+?\s+(?:says?|tells?|shouts?|auctions?|says\s+out\s+of\s+character|tells\s+the\s+(?:group|guild|raid)))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool TryMatch(string line, CombatAlertRule rule, out CombatAlertMatch? alert)
    {
        alert = null;
        if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(rule.TriggerPhrase)) return false;

        var message = Timestamp.Replace(line.Trim(), string.Empty, 1);
        if (ChatLine.IsMatch(message)) return false;

        var triggerIndex = message.IndexOf(rule.TriggerPhrase, StringComparison.OrdinalIgnoreCase);
        if (triggerIndex < 0) return false;

        var mobName = message[..triggerIndex].Trim().TrimEnd('-', ':', '!', '.', ',');
        var output = (rule.DisplayText.Length == 0 ? rule.TriggerPhrase : rule.DisplayText)
            .Replace("{mob}", mobName, StringComparison.OrdinalIgnoreCase)
            .Replace("{line}", message, StringComparison.OrdinalIgnoreCase)
            .Trim();
        alert = new CombatAlertMatch(rule.Id, output, mobName, line);
        return true;
    }
}
