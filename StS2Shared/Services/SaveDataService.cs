using System.Text.Json;
using StS2Shared.Models;

namespace StS2Shared.Services;

public static class SaveDataService
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

    /// <summary>
    /// 進行中ランの current_run.save から現在キャラの正規化 ID（接頭辞なし大文字、例 "DEFECT"）を返す。
    /// セーブが見つからない・読めない・キャラ未設定なら null。
    /// </summary>
    public static string? TryGetCurrentCharacterId()
    {
        try
        {
            var path = GetDefaultSavePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            return NormalizeCharacterId(Load(path).Players.FirstOrDefault()?.CharacterId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>"CHARACTER.DEFECT" 等の生 ID を接頭辞なし大文字（"DEFECT"）に正規化する。空なら null。</summary>
    public static string? NormalizeCharacterId(string? rawCharacterId)
    {
        if (string.IsNullOrWhiteSpace(rawCharacterId))
            return null;
        int dot = rawCharacterId.LastIndexOf('.');
        var id = dot >= 0 ? rawCharacterId[(dot + 1)..] : rawCharacterId;
        return id.Length == 0 ? null : id.ToUpperInvariant();
    }
}
