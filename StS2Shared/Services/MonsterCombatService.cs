using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

public sealed record MonsterMovePower(string Id, int? Value, int? ValueAsc);

public sealed record MonsterMove(
    string Id,
    IReadOnlyList<string> Intents,
    int? Damage, int? DamageAsc, int? Hits,
    int? Block, int? BlockAsc,
    IReadOnlyList<MonsterMovePower> Powers);

public sealed record MonsterHp(int? Min, int? Max, int? MinAsc, int? MaxAsc);

public sealed record MonsterCombat(
    string Id,
    MonsterHp? Hp,
    IReadOnlyList<string> StartingPowers,
    IReadOnlyList<MonsterMove> Moves);

/// <summary>
/// モンスターの戦闘データ（HP・ムーブ・インテント種別・ダメージ/ブロック値・開始パワー）と
/// 手動アノテーションの行動パターン文を提供する。
///
/// データ元:
///  - <c>monster_combat.json</c>（card-type-extractor が DLL の MonsterModel から IL 抽出。バージョン管理）
///  - <c>monster_move_patterns.json</c>（手動。AI シーケンスの自然文 EN/JA。バージョン非依存 = Resources 直下）
/// ムーブ名/インテント名/パワー名はローカライズ（monsters / intents / powers）から解決する。
/// </summary>
public static class MonsterCombatService
{
    static readonly IReadOnlyDictionary<string, MonsterCombat> _combat = LoadCombat();
    static readonly IReadOnlyDictionary<string, (string En, string Ja)> _patterns = LoadPatterns();
    static readonly IReadOnlyDictionary<string, string> _engMoves   = LoadLoc("eng.monsters");
    static readonly IReadOnlyDictionary<string, string> _jpnMoves   = LoadLoc("jpn.monsters");
    static readonly IReadOnlyDictionary<string, string> _engIntents = LoadLoc("eng.intents");
    static readonly IReadOnlyDictionary<string, string> _jpnIntents = LoadLoc("jpn.intents");
    static readonly IReadOnlyDictionary<string, string> _engPowers  = LoadLoc("eng.powers");
    static readonly IReadOnlyDictionary<string, string> _jpnPowers  = LoadLoc("jpn.powers");

    static int? Int(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    static IReadOnlyDictionary<string, MonsterCombat> LoadCombat()
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, MonsterCombat>(StringComparer.OrdinalIgnoreCase);
        var name = ResourceResolver.ResolveVersioned(asm, "monster_combat.json");
        if (name is null) return result;
        using var stream = asm.GetManifestResourceStream(name)!;
        var doc = JsonDocument.Parse(stream);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var o = prop.Value;

            MonsterHp? hp = null;
            if (o.TryGetProperty("hp", out var h))
                hp = new MonsterHp(Int(h, "min"), Int(h, "max"), Int(h, "minAsc"), Int(h, "maxAsc"));

            var startPowers = o.TryGetProperty("startingPowers", out var sp)
                ? sp.EnumerateArray().Select(e => e.GetString()!).ToArray()
                : Array.Empty<string>();

            var moves = new List<MonsterMove>();
            if (o.TryGetProperty("moves", out var ms))
                foreach (var m in ms.EnumerateArray())
                {
                    var intents = m.TryGetProperty("intents", out var it)
                        ? it.EnumerateArray().Select(e => e.GetString()!).ToArray()
                        : Array.Empty<string>();
                    var powers = new List<MonsterMovePower>();
                    if (m.TryGetProperty("powers", out var ps))
                        foreach (var p in ps.EnumerateArray())
                            powers.Add(new MonsterMovePower(
                                p.GetProperty("id").GetString()!, Int(p, "value"), Int(p, "valueAsc")));

                    moves.Add(new MonsterMove(
                        m.GetProperty("id").GetString()!, intents,
                        Int(m, "damage"), Int(m, "damageAsc"), Int(m, "hits"),
                        Int(m, "block"), Int(m, "blockAsc"), powers));
                }

            result[prop.Name] = new MonsterCombat(prop.Name, hp, startPowers, moves);
        }
        return result;
    }

    static IReadOnlyDictionary<string, (string En, string Ja)> LoadPatterns()
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        // バージョン非依存。Resources 直下の埋め込み（StS2Shared.Resources.monster_move_patterns.json）。
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(".Resources.monster_move_patterns.json", StringComparison.Ordinal));
        if (name is null) return result;
        using var stream = asm.GetManifestResourceStream(name)!;
        var doc = JsonDocument.Parse(stream);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var en = prop.Value.TryGetProperty("patternEn", out var e) ? e.GetString() ?? "" : "";
            var ja = prop.Value.TryGetProperty("patternJa", out var j) ? j.GetString() ?? "" : "";
            result[prop.Name] = (en, ja);
        }
        return result;
    }

    static IReadOnlyDictionary<string, string> LoadLoc(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var stream = ResourceResolver.OpenText(asm, $"localization.{suffix}.json");
        if (stream is null) return result;
        var doc = JsonDocument.Parse(stream);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.GetString() ?? "";
        return result;
    }

    static string TitleCase(string s) =>
        string.Join(' ', s.Split('_')
            .Select(w => w.Length == 0 ? w : char.ToUpper(w[0]) + w[1..].ToLower()));

    public static bool Has(string dirName) => _combat.ContainsKey(dirName);

    public static MonsterCombat? Get(string dirName) =>
        _combat.TryGetValue(dirName, out var c) ? c : null;

    /// <summary>手動アノテーションの行動パターン文（EN/JA）。未登録なら null。</summary>
    public static (string En, string Ja)? GetMovePattern(string dirName) =>
        _patterns.TryGetValue(dirName, out var p) ? p : null;

    /// <summary>ムーブの表示名。monsters.json の <c>{MONSTER}.moves.{MOVE}.title</c>。</summary>
    public static string GetMoveName(string dirName, string moveId, bool japanese = false)
    {
        var key = $"{dirName.ToUpperInvariant()}.moves.{moveId}.title";
        var dict = japanese ? _jpnMoves : _engMoves;
        return dict.TryGetValue(key, out var v) && v.Length > 0 ? v : TitleCase(moveId);
    }

    /// <summary>インテント種別の表示名。intents.json の <c>{CATEGORY}.title</c>（例 ATTACK→Aggressive）。</summary>
    public static string GetIntentLabel(string category, bool japanese = false)
    {
        var dict = japanese ? _jpnIntents : _engIntents;
        return dict.TryGetValue($"{category}.title", out var v) && v.Length > 0 ? v : TitleCase(category);
    }

    /// <summary>パワーの表示名。powers.json の <c>{POWER}.title</c>（例 STRENGTH_POWER→Strength）。</summary>
    public static string GetPowerName(string powerId, bool japanese = false)
    {
        var dict = japanese ? _jpnPowers : _engPowers;
        if (dict.TryGetValue($"{powerId}.title", out var v) && v.Length > 0) return v;
        var stripped = powerId.EndsWith("_POWER", StringComparison.Ordinal) ? powerId[..^6] : powerId;
        return TitleCase(stripped);
    }

    /// <summary>パワーの説明文（BBタグ除去・テンプレート解決済み）。powers.json の <c>{POWER}.description</c>。</summary>
    public static string GetPowerDescription(string powerId, bool japanese = false)
    {
        var dict = japanese ? _jpnPowers : _engPowers;
        return dict.TryGetValue($"{powerId}.description", out var v)
            ? DescriptionFormatter.Clean(v, japanese) : "";
    }
}
