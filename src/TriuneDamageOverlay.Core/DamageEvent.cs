namespace TriuneDamageOverlay.Core;

public enum DamageKind
{
    Melee,
    Slashing,
    Piercing,
    Crushing,
    Kick,
    Bash,
    Backstab,
    Ranged,
    Spell,
    Proc,
    DamageOverTime,
    Critical
}

public sealed record DamageEvent(
    int Amount,
    DamageKind Kind,
    string Ability,
    string Target,
    string OriginalLine);
