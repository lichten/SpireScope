using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

public enum KeywordCategory { CardKeyword, Affliction, Enchantment }

public record KeywordEntry(
    string Id,
    KeywordCategory Category,
    string TitleEn,
    string TitleJa,
    string DescEn,
    string DescJa,
    string ExtraCardTextEn,
    string ExtraCardTextJa,
    string DevNoteEn = "");

/// <summary>
/// sts2.xml 由来の開発者ノート（ローカライズの無い Power / enum 用のメモ専用エントリ）。
/// </summary>
public record DevNote(string Id, string TitleEn, string TextEn);

public static class KeywordDatabaseService
{
    // sts2.xml 由来の開発者ノート（category 接頭辞付き ID → 英語 summary）。card-type-extractor が生成。
    static readonly IReadOnlyDictionary<string, string> _devNotes = LoadDevNotes();

    static readonly IReadOnlyList<KeywordEntry> _cardKeywords =
        BuildEntries(LoadJson("eng.card_keywords"), LoadJson("jpn.card_keywords"), KeywordCategory.CardKeyword);
    static readonly IReadOnlyList<KeywordEntry> _afflictions =
        BuildEntries(LoadJson("eng.afflictions"), LoadJson("jpn.afflictions"), KeywordCategory.Affliction);
    static readonly IReadOnlyList<KeywordEntry> _enchantments =
        BuildEntries(LoadJson("eng.enchantments"), LoadJson("jpn.enchantments"), KeywordCategory.Enchantment);

    static readonly IReadOnlyList<DevNote> _powerNotes = BuildNotes("POWER.");
    static readonly IReadOnlyList<DevNote> _enumNotes  = BuildNotes("ENUM.");

    static IReadOnlyDictionary<string, string> LoadJson(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = ResourceResolver.ResolveVersioned(asm, $"localization.{suffix}.json");
        if (name is null) return new Dictionary<string, string>();

        using var stream = asm.GetManifestResourceStream(name)!;
        var doc = JsonDocument.Parse(stream);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.GetString() ?? "";
        return result;
    }

    static IReadOnlyList<KeywordEntry> BuildEntries(
        IReadOnlyDictionary<string, string> eng,
        IReadOnlyDictionary<string, string> jpn,
        KeywordCategory category)
    {
        return eng.Keys
            .Where(k => k.EndsWith(".title", StringComparison.OrdinalIgnoreCase))
            .Select(k => k[..^6])
            .Where(id =>
            {
                eng.TryGetValue($"{id}.title",       out var t);
                eng.TryGetValue($"{id}.description", out var d);
                return !string.IsNullOrEmpty(t) && !string.IsNullOrEmpty(d);
            })
            .Select(id =>
            {
                eng.TryGetValue($"{id}.title",         out var titleEn);
                eng.TryGetValue($"{id}.description",   out var descEn);
                eng.TryGetValue($"{id}.extraCardText",  out var extraEn);
                jpn.TryGetValue($"{id}.title",         out var titleJa);
                jpn.TryGetValue($"{id}.description",   out var descJa);
                jpn.TryGetValue($"{id}.extraCardText",  out var extraJa);
                _devNotes.TryGetValue($"{DevNotePrefix(category)}{id}", out var devNote);
                return new KeywordEntry(id, category,
                    titleEn ?? "", titleJa ?? titleEn ?? "",
                    descEn  ?? "", descJa  ?? descEn  ?? "",
                    extraEn ?? "", extraJa ?? extraEn ?? "",
                    devNote ?? "");
            })
            .OrderBy(e => e.TitleJa, StringComparer.CurrentCulture)
            .ToList();
    }

    static string DevNotePrefix(KeywordCategory category) => category switch
    {
        KeywordCategory.Affliction  => "AFFLICTION.",
        KeywordCategory.Enchantment => "ENCHANTMENT.",
        _                           => "CARDKEYWORD.",   // 現状 sts2.xml に該当なし（将来用）
    };

    // keyword_dev_notes.json（接頭辞付き ID → 英語 summary）を読む。
    static IReadOnlyDictionary<string, string> LoadDevNotes()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = ResourceResolver.ResolveVersioned(asm, "keyword_dev_notes.json");
        if (name is null) return new Dictionary<string, string>();

        using var stream = asm.GetManifestResourceStream(name)!;
        var doc = JsonDocument.Parse(stream);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = prop.Value.GetString() ?? "";
        return result;
    }

    // ローカライズの無い Power / enum 用のメモ専用エントリを接頭辞から構築する。
    static IReadOnlyList<DevNote> BuildNotes(string prefix) =>
        _devNotes
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kv =>
            {
                var id = kv.Key[prefix.Length..];
                // POWER.PARRY_POWER → "Parry Power"、ENUM.CardTag.Strike → "CardTag.Strike"（そのまま）
                var title = prefix == "POWER." ? SnakeToTitle(id) : id;
                return new DevNote(id, title, kv.Value);
            })
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .ToList();

    // PARRY_POWER → "Parry Power"
    static string SnakeToTitle(string snake) =>
        string.Join(' ', snake.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

    public static IReadOnlyList<KeywordEntry> GetCardKeywords() => _cardKeywords;
    public static IReadOnlyList<KeywordEntry> GetAfflictions()  => _afflictions;
    public static IReadOnlyList<KeywordEntry> GetEnchantments() => _enchantments;
    public static IReadOnlyList<KeywordEntry> GetAll() =>
        [.._cardKeywords, .._afflictions, .._enchantments];

    // sts2.xml 由来のメモ専用エントリ（ローカライズの無い Power / カード系 enum）。
    public static IReadOnlyList<DevNote> GetPowerNotes() => _powerNotes;
    public static IReadOnlyList<DevNote> GetEnumNotes()  => _enumNotes;
}
