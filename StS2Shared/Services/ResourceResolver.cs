using System.Reflection;
using System.Text.RegularExpressions;

namespace StS2Shared.Services;

/// <summary>
/// バージョン別フォルダ（Resources/v{version}/）に埋め込まれた JSON リソースを
/// 「最新バージョン」で決定論的に解決するヘルパー。
///
/// 各サービスが従来使っていた <c>GetManifestResourceNames().FirstOrDefault(n =&gt; n.EndsWith("xxx.json"))</c>
/// は、(1) 複数バージョン（v0.106.1 / v0.107.0 …）が同名で埋め込まれた場合に列挙順依存になり、
/// (2) 同名のローカライズ埋め込み（StS2Shared.localization.eng.card_keywords.json 等）まで誤って一致しうる。
/// 本ヘルパーは <c>.Resources.v{version}.{fileName}</c> 形式のみを対象とし、最大バージョンを選ぶことで両問題を解消する。
/// </summary>
internal static class ResourceResolver
{
    /// <summary>
    /// 指定ファイル名のバージョン別リソースのうち、最新バージョンのリソース名を返す。
    /// 該当が無ければ null。
    /// </summary>
    public static string? ResolveVersioned(Assembly asm, string fileName)
    {
        // 例: "StS2Shared.Resources.v0._107._0.card_costs.json"
        // フォルダ "v0.107.0" は数値セグメントが識別子化されて "v0._107._0" になる。
        var rx = new Regex(@"\.Resources\.(v.+?)\." + Regex.Escape(fileName) + "$");
        return asm.GetManifestResourceNames()
            .Select(n => (name: n, m: rx.Match(n)))
            .Where(x => x.m.Success)
            .OrderByDescending(x => VersionKey(x.m.Groups[1].Value), StringComparer.Ordinal)
            .Select(x => x.name)
            .FirstOrDefault();
    }

    /// <summary>最新バージョンのリソースストリームを直接開く。該当が無ければ null。</summary>
    public static Stream? OpenVersioned(Assembly asm, string fileName)
        => ResolveVersioned(asm, fileName) is { } name ? asm.GetManifestResourceStream(name) : null;

    // バージョン token（例 "v0._107._0"）内の整数列をゼロ埋め連結し、文字列比較で数値順になるキーを作る。
    // セグメント数に依存しない（"v0.107.0" → "00000000.00000107.00000000"）。
    static string VersionKey(string token) =>
        string.Join('.', Regex.Matches(token, @"\d+").Select(m => m.Value.PadLeft(8, '0')));
}
