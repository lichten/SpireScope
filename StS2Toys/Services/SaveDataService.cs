using System.Text.Json;
using StS2Toys.Models;

namespace StS2Toys.Services;

static class SaveDataService
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static RunSaveData Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<RunSaveData>(stream, Options)
            ?? throw new InvalidDataException("セーブデータを読み込めませんでした。");
    }

    public static string GetDefaultSavePath()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(roaming, "SlayTheSpire2", "steam");

        if (!Directory.Exists(baseDir))
            return "";

        // steam ID ディレクトリを自動検出（最初に見つかったものを使用）
        var steamIdDir = Directory.EnumerateDirectories(baseDir).FirstOrDefault();
        if (steamIdDir is null)
            return "";

        return Path.Combine(steamIdDir, "profile1", "saves", "current_run.save");
    }
}
