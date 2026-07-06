using System.Text.Encodings.Web;
using System.Text.Json;

namespace StS2Shared.Assets;

/// <summary>
/// PCK の生ローカライズ（<c>cards.json</c>/<c>relics.json</c>/<c>potions.json</c>/<c>monsters.json</c>）から、
/// StS2Shared が実行時に読む派生テキスト JSON を生成する。配布ビルドでは埋め込みからゲームテキストを
/// 除外するため、抽出時にこれらを外部（抽出ルート直下）へ生成して 2 段解決の外部側を埋める。
///
/// 生成ロジックは <c>card-type-extractor</c>（<c>Program.cs:1403-1469, 2183-2200, 1577-1620</c>）と同一方針：
/// 生ローカライズの単一セグメント <c>{ID}.title</c> / <c>{ID}.description</c> / <c>{SNAKE}.name</c> を集約する（DLL 不要）。
/// </summary>
public static class LocTextDeriver
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <summary>生成する派生ファイル数（進捗表示用）。</summary>
    public const int FileCount = 4;

    /// <summary>派生テキストを <paramref name="outputRoot"/> 直下に生成する。</summary>
    public static void Derive(PckReader pck, string outputRoot, IProgress<ExtractProgress>? progress = null,
        CancellationToken ct = default)
    {
        int done = 0;
        void Step(string file, object data)
        {
            ct.ThrowIfCancellationRequested();
            File.WriteAllText(Path.Combine(outputRoot, file), JsonSerializer.Serialize(data, JsonOpts));
            progress?.Report(new ExtractProgress("game_text", ++done, FileCount));
        }

        // card_database.json（カード・レリック名 EN/JP）
        var engCards = LoadLocSuffix(pck, "localization/eng/cards.json", ".title");
        var jpnCards = LoadLocSuffix(pck, "localization/jpn/cards.json", ".title");
        var engRelics = LoadLocSuffix(pck, "localization/eng/relics.json", ".title");
        var jpnRelics = LoadLocSuffix(pck, "localization/jpn/relics.json", ".title");
        var db = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (p, en) in engCards) db[$"CARD.{p}"] = EnJa(en, jpnCards, p);
        foreach (var (p, en) in engRelics) db[$"RELIC.{p}"] = EnJa(en, jpnRelics, p);
        Step("card_database.json", Sorted(db));

        // card_descriptions.json（カード説明文 生テキスト＝タグ・{Var} 保持）
        var engDesc = LoadLocSuffix(pck, "localization/eng/cards.json", ".description");
        var jpnDesc = LoadLocSuffix(pck, "localization/jpn/cards.json", ".description");
        var desc = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (p, en) in engDesc) desc[$"CARD.{p}"] = EnJa(en, jpnDesc, p);
        Step("card_descriptions.json", Sorted(desc));

        // potion_database.json（ポーション名 EN/JP）
        var engPot = LoadLocSuffix(pck, "localization/eng/potions.json", ".title");
        var jpnPot = LoadLocSuffix(pck, "localization/jpn/potions.json", ".title");
        var pot = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (p, en) in engPot) pot[$"POTION.{p}"] = EnJa(en, jpnPot, p);
        Step("potion_database.json", Sorted(pot));

        // monster_names.json（配列 [{dirName, en, ja}]。dirName = SNAKE の小文字）
        var engMon = LoadLocSuffix(pck, "localization/eng/monsters.json", ".name");
        var jpnMon = LoadLocSuffix(pck, "localization/jpn/monsters.json", ".name");
        var monsters = engMon.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new Dictionary<string, string>
            {
                ["dirName"] = kv.Key.ToLowerInvariant(),
                ["en"] = kv.Value,
                ["ja"] = jpnMon.TryGetValue(kv.Key, out var j) && j.Length > 0 ? j : kv.Value,
            })
            .ToList();
        Step("monster_names.json", monsters);
    }

    // 指定 loc ファイルの {PREFIX}{suffix} キー（PREFIX にドットを含まない単一セグメント）を PREFIX→値 に集約。
    static Dictionary<string, string> LoadLocSuffix(PckReader pck, string resPath, string suffix)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!pck.TryRead(resPath, out var bytes)) return map;

        using var doc = JsonDocument.Parse(bytes);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!prop.Name.EndsWith(suffix, StringComparison.Ordinal)) continue;
            var prefix = prop.Name[..^suffix.Length];
            if (prefix.Contains('.')) continue;
            map[prefix] = prop.Value.GetString() ?? "";
        }
        return map;
    }

    // { "en": en, "ja": (jpn[key] または en) }。ja が空なら en にフォールバック。
    static Dictionary<string, string> EnJa(string en, Dictionary<string, string> jpn, string key) =>
        new()
        {
            ["en"] = en,
            ["ja"] = jpn.TryGetValue(key, out var j) && j.Length > 0 ? j : en,
        };

    static Dictionary<string, object> Sorted(Dictionary<string, object> d) =>
        d.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value);
}
