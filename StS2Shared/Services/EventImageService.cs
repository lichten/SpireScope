using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// イベント ID → イベント画像のソース相対パス（<c>events_png/</c> 基準、例 "abyssal_baths.png"）。
/// card-type-extractor が実ファイル（<c>.png.import</c>）をスキャンして生成した event_images.json
/// （バージョンフォルダ）を参照する。PNG 実体は <c>ctex-to-png -- events</c> が
/// <c>tools/extracted/images/events_png/</c> に変換生成する。
/// 1 イベント 1 主画像の対応をここに一元化する。<see cref="RelicImageService"/> のイベント版。
/// </summary>
public static class EventImageService
{
    /// <summary>画像ソースのベースディレクトリ名（<c>tools/extracted/images/</c> 配下）。</summary>
    public const string EventsDirName = "events_png";

    static readonly IReadOnlyDictionary<string, string> _paths = Load();

    static IReadOnlyDictionary<string, string> Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var name = ResourceResolver.ResolveVersioned(asm, "event_images.json");
        if (name is null) return result;

        using var stream = asm.GetManifestResourceStream(name)!;
        var doc = JsonDocument.Parse(stream);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.GetString() ?? "";
        return result;
    }

    /// <summary>
    /// イベント画像のソース相対パス（例 "abyssal_baths.png"）。接頭辞なし ID で引く（大文字小文字不問）。
    /// 画像が無いイベントは null。
    /// </summary>
    public static string? GetRelativePath(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return null;
        return _paths.TryGetValue(eventId, out var p) ? p : null;
    }

    /// <summary>
    /// <paramref name="eventsRoot"/>（= <c>events_png</c> ディレクトリ）配下の画像相対パス（OS 区切り適用）。
    /// 画像が無いイベントは null。
    /// </summary>
    public static string? GetSourcePath(string eventsRoot, string eventId)
    {
        var rel = GetRelativePath(eventId);
        return rel is null ? null : Path.Combine(eventsRoot, rel.Replace('/', Path.DirectorySeparatorChar));
    }
}
