using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// 焚き火（休憩所）選択肢の EN/JP 表示名。ラン履歴の <c>rest_site_choices</c>（生 ID = "SMITH" 等）を
/// 日本語名（鍛冶・休憩・発掘…）へ解決する。
/// ローカライズ <c>rest_site_ui.json</c> の <c>OPTION_{ID}.name</c> を読む
/// （card-type-extractor が版固定コピーして <see cref="ResourceResolver"/> で最新版を解決）。
/// </summary>
public static class RestSiteOptionService
{
    static readonly IReadOnlyDictionary<string, string> _eng = LoadJson("eng");
    static readonly IReadOnlyDictionary<string, string> _jpn = LoadJson("jpn");

    static IReadOnlyDictionary<string, string> LoadJson(string lang)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = ResourceResolver.OpenText(asm, $"localization.{lang}.rest_site_ui.json");
        if (stream is null) return new Dictionary<string, string>();

        using var doc = JsonDocument.Parse(stream);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.GetString() ?? "";
        return result;
    }

    static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    /// <summary>
    /// 焚き火選択肢 ID（例 "SMITH"・"HEAL"。大文字小文字は不問）→ 表示名。
    /// 未知/欠落時は Title-case した素の ID（例 "Smith"）にフォールバックする。
    /// </summary>
    public static string GetName(string optionId, bool japanese = false)
    {
        if (string.IsNullOrEmpty(optionId)) return optionId;
        var dict = japanese ? _jpn : _eng;
        var key = $"OPTION_{optionId.ToUpperInvariant()}.name";
        return dict.TryGetValue(key, out var v) && v.Length > 0 ? v : TitleCase(optionId);
    }
}
