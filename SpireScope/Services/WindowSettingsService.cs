using System.Text.Json;

namespace SpireScope.Services;

record WindowSettings(int X, int Y, int Width, int Height, string State);
record SubWindowSettings(int X, int Y, int Width, int Height, bool Visible = false);
record AppSettings(
    WindowSettings? Main = null,
    SubWindowSettings? HpHistory = null,
    SubWindowSettings? EncounterOverview = null,
    int? SidePanelWidth = null,
    SubWindowSettings? CharacterOverview = null,
    SubWindowSettings? CombinedOverview = null,
    string? Language = null,
    // 配布モードのアセットセットアップ状態（再プロンプト制御用。実体判定は AssetLocator を正とする）。
    string? AssetsInstalledVersion = null,
    string? AssetsSkippedVersion = null);

static class WindowSettingsService
{
    static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpireScope", "settings.json");

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try
        {
            using var stream = File.OpenRead(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(stream, Options) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        using var stream = File.Create(SettingsPath);
        JsonSerializer.Serialize(stream, settings, Options);
    }
}
