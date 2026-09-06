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
                loaded.AlertRules ??= AlertRuleSettings.Defaults();
                foreach (var builtIn in AlertRuleSettings.Defaults())
                    if (!loaded.AlertRules.Any(x => x.Id.Equals(builtIn.Id, StringComparison.OrdinalIgnoreCase)))
                        loaded.AlertRules.Add(builtIn);
                return loaded;
            }
        }
        catch { }

        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        return new AppSettings { AnchorX = screen.Left + (int)(screen.Width * .66), AnchorY = screen.Top + (int)(screen.Height * .34) };
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
