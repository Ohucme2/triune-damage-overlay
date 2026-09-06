using TriuneDamageOverlay.Core;

namespace TriuneDamageOverlay;

internal sealed class AlertRuleSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Custom alert";
    public string TriggerPhrase { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public int ColorArgb { get; set; } = Color.Red.ToArgb();
    public int DurationMilliseconds { get; set; } = 3000;
    public int CooldownMilliseconds { get; set; } = 5000;
    public bool Enabled { get; set; } = true;
    public bool BuiltIn { get; set; }

    public CombatAlertRule ToRule() => new(Id, TriggerPhrase, DisplayText);

    public static List<AlertRuleSettings> Defaults() => new()
    {
        new() { Id = "builtin-rampage", Name = "Rampage", TriggerPhrase = "goes on a RAMPAGE", DisplayText = "RAMPAGE! — {mob}", ColorArgb = Color.FromArgb(255, 55, 55).ToArgb(), DurationMilliseconds = 3500, CooldownMilliseconds = 5000, BuiltIn = true },
        new() { Id = "builtin-enrage", Name = "Enrage", TriggerPhrase = "has become ENRAGED", DisplayText = "ENRAGED! — {mob}", ColorArgb = Color.FromArgb(255, 126, 42).ToArgb(), DurationMilliseconds = 3500, CooldownMilliseconds = 5000, BuiltIn = true },
        new() { Id = "builtin-flurry", Name = "Flurry", TriggerPhrase = "executes a FLURRY", DisplayText = "FLURRY! — {mob}", ColorArgb = Color.FromArgb(255, 211, 48).ToArgb(), DurationMilliseconds = 2800, CooldownMilliseconds = 2500, BuiltIn = true },
        new() { Id = "builtin-casting", Name = "Enemy begins casting", TriggerPhrase = "begins to cast", DisplayText = "CASTING — {mob}", ColorArgb = Color.FromArgb(87, 205, 255).ToArgb(), DurationMilliseconds = 2200, CooldownMilliseconds = 1000, Enabled = false, BuiltIn = true }
    };
}
