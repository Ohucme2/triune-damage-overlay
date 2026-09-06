using System.Globalization;
using System.Text.RegularExpressions;

namespace TriuneDamageOverlay.Core;

/// <summary>
/// Parses only damage explicitly attributed to the local character. It deliberately
/// rejects generic "X hits Y" lines unless X exactly matches the configured character.
/// Pet and other-player damage therefore remain excluded.
/// </summary>
public sealed class EverQuestDamageParser
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex Timestamp = new(@"^\[[^\]]+\]\s*", Options);
    private static readonly Regex YouDirect = new(
        @"^You\s+(?<verb>hit|slash|pierce|crush|kick|bash|punch|backstab|shoot|strike|maul|bite|claw)\s+(?<target>.+?)\s+for\s+(?<amount>[\d,]+)\s+points?\s+of\s+(?:(?<damageType>[\w-]+)\s+)?damage(?:\s+by\s+(?<effect>.+?))?(?:\.|\s+\(|$)",
        Options);
    private static readonly Regex YourAbility = new(
        @"^Your\s+(?<ability>.+?)\s+(?:hits?|strikes?|slashes?|pierces?|crushes?|burns?|blasts?|damages?)\s+(?<target>.+?)\s+for\s+(?<amount>[\d,]+)\s+points?\s+of\s+(?:non-melee\s+)?damage\b",
        Options);
    private static readonly Regex Dot = new(
        @"^(?<target>.+?)\s+has\s+taken\s+(?<amount>[\d,]+)\s+(?:points?\s+of\s+)?damage\s+from\s+your\s+(?<ability>.+?)(?:\.\s*)?(?:\((?<critical>Lucky\s+Critical|Critical(?:\s+Twincast)?)\))?$",
        Options);
    private static readonly Regex Critical = new(
        @"^You\s+(?:have\s+scored\s+a\s+critical\s+hit|deliver\s+a\s+critical\s+blast|have\s+landed\s+a\s+crippling\s+blow)!?\s*\((?<amount>[\d,]+)\)",
        Options);

    private readonly string _characterName;
    private readonly Regex? _namedDirect;

    public EverQuestDamageParser(string? characterName)
    {
        _characterName = characterName?.Trim() ?? string.Empty;
        if (_characterName.Length > 0)
        {
            _namedDirect = new Regex(
                @"^" + Regex.Escape(_characterName) + @"\s+(?<verb>hits?|slashes?|pierces?|crushes?|kicks?|bashes?|punches?|backstabs?|shoots?|strikes?)\s+(?<target>.+?)\s+for\s+(?<amount>[\d,]+)\s+points?\s+of\s+(?:[\w-]+\s+)?damage\b",
                Options);
        }
    }

    public bool TryParse(string line, out DamageEvent? damage)
    {
        damage = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        var message = Timestamp.Replace(line.Trim(), string.Empty, 1);

        var match = YouDirect.Match(message);
        if (match.Success)
        {
            var effect = match.Groups["effect"].Value.Trim();
            var kind = IsCritical(message) ? DamageKind.Critical : effect.Length > 0 ? DamageKind.Proc : ClassifyMelee(match.Groups["verb"].Value);
            damage = Create(match, kind, effect.Length > 0 ? effect : match.Groups["verb"].Value, line);
            return damage is not null;
        }

        match = YourAbility.Match(message);
        if (match.Success)
        {
            damage = Create(match, IsCritical(message) ? DamageKind.Critical : DamageKind.Spell, match.Groups["ability"].Value, line);
            return damage is not null;
        }

        match = Dot.Match(message);
        if (match.Success)
        {
            damage = Create(match, IsCritical(message) ? DamageKind.Critical : DamageKind.DamageOverTime, match.Groups["ability"].Value, line);
            return damage is not null;
        }

        match = Critical.Match(message);
        if (match.Success && TryAmount(match, out var criticalAmount))
        {
            damage = new DamageEvent(criticalAmount, DamageKind.Critical, "CRIT", string.Empty, line);
            return true;
        }

        if (_namedDirect is not null)
        {
            match = _namedDirect.Match(message);
            if (match.Success)
            {
                damage = Create(match, IsCritical(message) ? DamageKind.Critical : ClassifyMelee(match.Groups["verb"].Value), match.Groups["verb"].Value, line);
                return damage is not null;
            }
        }

        return false;
    }

    private static DamageEvent? Create(Match match, DamageKind kind, string ability, string original)
    {
        if (!TryAmount(match, out var amount) || amount <= 0) return null;
        return new DamageEvent(
            amount,
            kind,
            ability.Trim(),
            match.Groups["target"].Success ? match.Groups["target"].Value.Trim() : string.Empty,
            original);
    }

    private static bool TryAmount(Match match, out int amount) =>
        int.TryParse(match.Groups["amount"].Value.Replace(",", string.Empty), NumberStyles.None, CultureInfo.InvariantCulture, out amount);

    private static bool IsCritical(string message) =>
        message.Contains("(Critical", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("(Lucky Critical", StringComparison.OrdinalIgnoreCase);

    private static DamageKind ClassifyMelee(string verb)
    {
        var value = verb.Trim().ToLowerInvariant();
        if (value.StartsWith("slash")) return DamageKind.Slashing;
        if (value.StartsWith("pierc")) return DamageKind.Piercing;
        if (value.StartsWith("crush") || value.StartsWith("maul")) return DamageKind.Crushing;
        if (value.StartsWith("kick")) return DamageKind.Kick;
        if (value.StartsWith("bash")) return DamageKind.Bash;
        if (value.StartsWith("backstab")) return DamageKind.Backstab;
        if (value.StartsWith("shoot")) return DamageKind.Ranged;
        return DamageKind.Melee;
    }
}
