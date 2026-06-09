public class SiteBuilderSettings
{
    public string? DistDir { get; set; }

    static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StS2SiteBuilder", "settings.json");

    public static SiteBuilderSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new();
            return System.Text.Json.JsonSerializer.Deserialize<SiteBuilderSettings>(
                       File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath,
                System.Text.Json.JsonSerializer.Serialize(this));
        }
        catch { }
    }
}
