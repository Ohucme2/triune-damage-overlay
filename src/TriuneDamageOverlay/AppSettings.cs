using System.Text.Json;

namespace TriuneDamageOverlay;

internal sealed class AppSettings
{
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
    public int AlertAnchorX { get; set; } = int.MinValue;
    public int AlertAnchorY { get; set; } = int.MinValue;
    public bool ShowAlertHandle { get; set; }
    public string IgnoredCombatAlertSources { get; set; } = string.Empty;
    public List<AlertRuleSettings> AlertRules { get; set; } = AlertRuleSettings.Defaults();

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TriuneDamageOverlay",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
                if (loaded.AlertAnchorX == int.MinValue) loaded.AlertAnchorX = loaded.AnchorX;
                if (loaded.AlertAnchorY == int.MinValue) loaded.AlertAnchorY = loaded.AnchorY - 150;
                loaded.AlertRules ??= AlertRuleSettings.Defaults();
                foreach (var builtIn in AlertRuleSettings.Defaults())
                    if (!loaded.AlertRules.Any(x => x.Id.Equals(builtIn.Id, StringComparison.OrdinalIgnoreCase)))
                        loaded.AlertRules.Add(builtIn);
                return loaded;
            }
        }
        catch { }

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var anchorX = screen.Left + (int)(screen.Width * .66);
        var anchorY = screen.Top + (int)(screen.Height * .34);
        return new AppSettings
        {
            AnchorX = anchorX,
            AnchorY = anchorY,
            AlertAnchorX = anchorX,
            AlertAnchorY = anchorY - 150
        };
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
