using System.Text.Json;
using StS2Shared.Services;

namespace StS2Toys.Services;

/// <summary>
/// 情報ページ URL テンプレートの永続化（ライブキャプチャの検出結果リンク用）。
/// ウィンドウ設定とは独立した url_templates.json に保存し、互いに上書きしないようにする。
/// </summary>
static class UrlTemplateService
{
    static readonly string Path_ = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StS2Toys", "url_templates.json");

    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<UrlTemplate> Load()
    {
        if (!File.Exists(Path_)) return Defaults();
        try
        {
            using var stream = File.OpenRead(Path_);
            return JsonSerializer.Deserialize<List<UrlTemplate>>(stream, Options) ?? Defaults();
        }
        catch
        {
            return Defaults();
        }
    }

    public static void Save(List<UrlTemplate> templates)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path_)!);
        using var stream = File.Create(Path_);
        JsonSerializer.Serialize(stream, templates, Options);
    }

    /// <summary>初期テンプレート（ユーザ提示の例：公式サイト＋外部 Wiki）。</summary>
    public static List<UrlTemplate> Defaults() => new()
    {
        new("公式", "card", "https://lichtenlab.com/sts2/cards/{cardclass}/{idraw}.html"),
        new("公式", "relic", "https://lichtenlab.com/sts2/relics/{idraw}.html"),
        new("公式", "event", "https://lichtenlab.com/sts2/events/{idraw}.html"),
        new("Wiki", "any", "https://wikiwiki.jp/sts2/{jp}"),
    };
}
