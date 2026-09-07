namespace TriuneDamageOverlay;

internal sealed class CharacterProfileSettings
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Default";
    public string LogPath { get; set; } = string.Empty;
    public int AnchorX { get; set; } = 900;
    public int AnchorY { get; set; } = 350;
    public bool ShowHandle { get; set; } = true;
    public bool ShowAbility { get; set; } = true;
    public int FontSize { get; set; } = 28;
    public int LifetimeMilliseconds { get; set; } = 1800;
    public int ScrollPixelsPerSecond { get; set; } = 70;
    public bool CombatAlertsEnabled { get; set; } = true;
    public int CombatAlertScalePercent { get; set; } = 100;
    public int AlertAnchorX { get; set; } = 900;
    public int AlertAnchorY { get; set; } = 200;
    public bool ShowAlertHandle { get; set; }
    public bool PositionHandlesLocked { get; set; }
    public List<string> FriendlySources { get; set; } = new();
    public List<AlertRuleSettings> AlertRules { get; set; } = AlertRuleSettings.Defaults();
}
