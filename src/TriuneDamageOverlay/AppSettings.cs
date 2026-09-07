using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TriuneDamageOverlay;

internal sealed class AppSettings
{
    public int SettingsVersion { get; set; } = 2;
    public string ActiveProfileKey { get; set; } = string.Empty;
    public List<CharacterProfileSettings> Profiles { get; set; } = new();

    [JsonIgnore]
    public CharacterProfileSettings ActiveProfile
    {
        get
        {
            var profile = Profiles.FirstOrDefault(x => x.Key.Equals(ActiveProfileKey, StringComparison.OrdinalIgnoreCase))
                ?? Profiles.FirstOrDefault();
            if (profile is not null) return profile;
            profile = CreateProfile(string.Empty, null);
            Profiles.Add(profile);
            ActiveProfileKey = profile.Key;
            return profile;
        }
    }

    [JsonIgnore] public string LogPath { get => ActiveProfile.LogPath; set => ActiveProfile.LogPath = value; }
    [JsonIgnore] public int AnchorX { get => ActiveProfile.AnchorX; set => ActiveProfile.AnchorX = value; }
    [JsonIgnore] public int AnchorY { get => ActiveProfile.AnchorY; set => ActiveProfile.AnchorY = value; }
    [JsonIgnore] public bool ShowHandle { get => ActiveProfile.ShowHandle; set => ActiveProfile.ShowHandle = value; }
    [JsonIgnore] public bool ShowAbility { get => ActiveProfile.ShowAbility; set => ActiveProfile.ShowAbility = value; }
    [JsonIgnore] public int FontSize { get => ActiveProfile.FontSize; set => ActiveProfile.FontSize = value; }
    [JsonIgnore] public int LifetimeMilliseconds { get => ActiveProfile.LifetimeMilliseconds; set => ActiveProfile.LifetimeMilliseconds = value; }
    [JsonIgnore] public int ScrollPixelsPerSecond { get => ActiveProfile.ScrollPixelsPerSecond; set => ActiveProfile.ScrollPixelsPerSecond = value; }
    [JsonIgnore] public bool CombatAlertsEnabled { get => ActiveProfile.CombatAlertsEnabled; set => ActiveProfile.CombatAlertsEnabled = value; }
    [JsonIgnore] public int CombatAlertScalePercent { get => ActiveProfile.CombatAlertScalePercent; set => ActiveProfile.CombatAlertScalePercent = value; }
    [JsonIgnore] public int AlertAnchorX { get => ActiveProfile.AlertAnchorX; set => ActiveProfile.AlertAnchorX = value; }
    [JsonIgnore] public int AlertAnchorY { get => ActiveProfile.AlertAnchorY; set => ActiveProfile.AlertAnchorY = value; }
    [JsonIgnore] public bool ShowAlertHandle { get => ActiveProfile.ShowAlertHandle; set => ActiveProfile.ShowAlertHandle = value; }
    [JsonIgnore] public bool PositionHandlesLocked { get => ActiveProfile.PositionHandlesLocked; set => ActiveProfile.PositionHandlesLocked = value; }
    [JsonIgnore] public List<string> FriendlySources => ActiveProfile.FriendlySources;
    [JsonIgnore] public List<AlertRuleSettings> AlertRules => ActiveProfile.AlertRules;

    private static string SettingsDirectory => Environment.GetEnvironmentVariable("TRIUNE_OVERLAY_SETTINGS_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TriuneDamageOverlay");
    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");
    private static string BackupPath => Path.Combine(SettingsDirectory, "settings.backup.json");

    public static AppSettings Load()
    {
        foreach (var path in new[] { SettingsPath, BackupPath })
        {
            try
            {
                if (!File.Exists(path)) continue;
                var json = File.ReadAllText(path);
                using var document = JsonDocument.Parse(json);
                AppSettings loaded;
                if (document.RootElement.TryGetProperty("Profiles", out _))
                    loaded = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
                else
                    loaded = MigrateLegacy(JsonSerializer.Deserialize<LegacyAppSettings>(json) ?? new LegacyAppSettings());
                loaded.Normalize();
                return loaded;
            }
            catch { }
        }

        return CreateDefault();
    }

    public CharacterProfileSettings ActivateLog(string path)
    {
        var key = ProfileKey(path);
        var profile = Profiles.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            var template = Profiles.FirstOrDefault(x => x.Key.Equals(ActiveProfileKey, StringComparison.OrdinalIgnoreCase));
            if (Profiles.Count == 1 && string.IsNullOrWhiteSpace(Profiles[0].LogPath))
            {
                profile = Profiles[0];
                profile.Key = key;
                profile.LogPath = path;
            }
            else
            {
                profile = CreateProfile(path, template);
                Profiles.Add(profile);
            }
        }
        profile.LogPath = path;
        profile.DisplayName = ProfileDisplayName(path);
        ActiveProfileKey = profile.Key;
        NormalizeProfile(profile);
        return profile;
    }

    public bool ActivateProfile(string key)
    {
        var profile = Profiles.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (profile is null) return false;
        ActiveProfileKey = profile.Key;
        NormalizeProfile(profile);
        return true;
    }

    public void ResetPositions()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        AnchorX = screen.Left + (int)(screen.Width * .66);
        AnchorY = screen.Top + (int)(screen.Height * .40);
        AlertAnchorX = screen.Left + screen.Width / 2;
        AlertAnchorY = screen.Top + (int)(screen.Height * .24);
    }

    public void Save()
    {
        using var mutex = new Mutex(false, "Local\\TriuneDamageOverlaySettings");
        var ownsMutex = false;
        try
        {
            try { ownsMutex = mutex.WaitOne(TimeSpan.FromSeconds(2)); }
            catch (AbandonedMutexException) { ownsMutex = true; }
            if (!ownsMutex) return;
            Directory.CreateDirectory(SettingsDirectory);
            Normalize();
            MergeOtherProfilesFromDisk();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            var temporaryPath = Path.Combine(SettingsDirectory, "settings.tmp.json");
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(SettingsPath)) File.Replace(temporaryPath, SettingsPath, BackupPath, true);
            else File.Move(temporaryPath, SettingsPath);
        }
        catch { }
        finally
        {
            if (ownsMutex) mutex.ReleaseMutex();
        }
    }

    private void MergeOtherProfilesFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("Profiles", out _)) return;
            var disk = JsonSerializer.Deserialize<AppSettings>(json);
            if (disk?.Profiles is null) return;
            disk.Normalize();
            foreach (var diskProfile in disk.Profiles.Where(x => !x.Key.Equals(ActiveProfileKey, StringComparison.OrdinalIgnoreCase)))
            {
                var index = Profiles.FindIndex(x => x.Key.Equals(diskProfile.Key, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) Profiles[index] = diskProfile;
                else Profiles.Add(diskProfile);
            }
            Normalize();
        }
        catch { }
    }

    private void Normalize()
    {
        SettingsVersion = 2;
        Profiles ??= new List<CharacterProfileSettings>();
        if (Profiles.Count == 0) Profiles.Add(CreateProfile(string.Empty, null));
        foreach (var profile in Profiles) NormalizeProfile(profile);
        if (!Profiles.Any(x => x.Key.Equals(ActiveProfileKey, StringComparison.OrdinalIgnoreCase)))
            ActiveProfileKey = Profiles[0].Key;
    }

    private static void NormalizeProfile(CharacterProfileSettings profile)
    {
        profile.Key = string.IsNullOrWhiteSpace(profile.Key) ? ProfileKey(profile.LogPath) : profile.Key;
        profile.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? ProfileDisplayName(profile.LogPath) : profile.DisplayName;
        profile.FontSize = Math.Clamp(profile.FontSize, 18, 52);
        profile.LifetimeMilliseconds = Math.Clamp(profile.LifetimeMilliseconds, 800, 8000);
        profile.ScrollPixelsPerSecond = Math.Clamp(profile.ScrollPixelsPerSecond, 30, 160);
        profile.CombatAlertScalePercent = Math.Clamp(profile.CombatAlertScalePercent, 40, 200);
        var visible = SystemInformation.VirtualScreen;
        if (!visible.Contains(profile.AnchorX, profile.AnchorY))
        {
            profile.AnchorX = visible.Left + (int)(visible.Width * .66);
            profile.AnchorY = visible.Top + (int)(visible.Height * .40);
        }
        if (!visible.Contains(profile.AlertAnchorX, profile.AlertAnchorY))
        {
            profile.AlertAnchorX = visible.Left + visible.Width / 2;
            profile.AlertAnchorY = visible.Top + (int)(visible.Height * .24);
        }
        profile.FriendlySources ??= new List<string>();
        profile.FriendlySources = profile.FriendlySources
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        profile.AlertRules ??= AlertRuleSettings.Defaults();
        foreach (var builtIn in AlertRuleSettings.Defaults())
            if (!profile.AlertRules.Any(x => x.Id.Equals(builtIn.Id, StringComparison.OrdinalIgnoreCase)))
                profile.AlertRules.Add(builtIn);
    }

    private static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        var profile = CreateProfile(string.Empty, null);
        settings.Profiles.Add(profile);
        settings.ActiveProfileKey = profile.Key;
        return settings;
    }

    private static CharacterProfileSettings CreateProfile(string path, CharacterProfileSettings? template)
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        return new CharacterProfileSettings
        {
            Key = ProfileKey(path), DisplayName = ProfileDisplayName(path), LogPath = path,
            AnchorX = screen.Left + (int)(screen.Width * .66), AnchorY = screen.Top + (int)(screen.Height * .40),
            AlertAnchorX = screen.Left + screen.Width / 2, AlertAnchorY = screen.Top + (int)(screen.Height * .24),
            ShowAbility = template?.ShowAbility ?? true, FontSize = template?.FontSize ?? 28,
            LifetimeMilliseconds = template?.LifetimeMilliseconds ?? 1800,
            ScrollPixelsPerSecond = template?.ScrollPixelsPerSecond ?? 70,
            CombatAlertsEnabled = template?.CombatAlertsEnabled ?? true,
            CombatAlertScalePercent = template?.CombatAlertScalePercent ?? 100,
            AlertRules = template is null ? AlertRuleSettings.Defaults() : template.AlertRules.Select(CloneRule).ToList()
        };
    }

    private static AlertRuleSettings CloneRule(AlertRuleSettings source) => new()
    {
        Id = source.Id, Name = source.Name, TriggerPhrase = source.TriggerPhrase, DisplayText = source.DisplayText,
        ColorArgb = source.ColorArgb, DurationMilliseconds = source.DurationMilliseconds,
        CooldownMilliseconds = source.CooldownMilliseconds, Enabled = source.Enabled, BuiltIn = source.BuiltIn
    };

    private static string ProfileKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "default";
        try { return Path.GetFullPath(path).ToLowerInvariant(); }
        catch { return path.Trim().ToLowerInvariant(); }
    }

    private static string ProfileDisplayName(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Default";
        var file = Path.GetFileName(path);
        var match = Regex.Match(file, @"^eqlog_(?<character>[^_]+)_(?<server>.+)\.txt$", RegexOptions.IgnoreCase);
        return match.Success ? $"{match.Groups["character"].Value} · {match.Groups["server"].Value}" : Path.GetFileNameWithoutExtension(file);
    }

    private static AppSettings MigrateLegacy(LegacyAppSettings legacy)
    {
        var profile = new CharacterProfileSettings
        {
            Key = ProfileKey(legacy.LogPath), DisplayName = ProfileDisplayName(legacy.LogPath), LogPath = legacy.LogPath,
            AnchorX = legacy.AnchorX, AnchorY = legacy.AnchorY, ShowHandle = legacy.ShowHandle,
            ShowAbility = legacy.ShowAbility, FontSize = legacy.FontSize,
            LifetimeMilliseconds = legacy.LifetimeMilliseconds, ScrollPixelsPerSecond = legacy.ScrollPixelsPerSecond,
            CombatAlertsEnabled = legacy.CombatAlertsEnabled, CombatAlertScalePercent = legacy.CombatAlertScalePercent,
            AlertAnchorX = legacy.AlertAnchorX == int.MinValue ? legacy.AnchorX : legacy.AlertAnchorX,
            AlertAnchorY = legacy.AlertAnchorY == int.MinValue ? legacy.AnchorY - 150 : legacy.AlertAnchorY,
            ShowAlertHandle = legacy.ShowAlertHandle,
            FriendlySources = ParseLegacyFriendlySources(legacy.IgnoredCombatAlertSources),
            AlertRules = legacy.AlertRules ?? AlertRuleSettings.Defaults()
        };
        return new AppSettings { ActiveProfileKey = profile.Key, Profiles = new List<CharacterProfileSettings> { profile } };
    }

    private static List<string> ParseLegacyFriendlySources(string value) => value
        .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private sealed class LegacyAppSettings
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
        public List<AlertRuleSettings>? AlertRules { get; set; }
    }
}
