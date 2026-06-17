using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// キャラクターの色（DLL 由来）。card-type-extractor が各 <c>CharacterModel</c> の Color プロパティと
/// <c>Helpers.StsColors</c> から生成した character_colors.json（バージョンフォルダ）を読む。
/// 識別色 <c>NameColor</c>（StsColors の red/green/blue/purple/orange）と、UI 用パレット
/// （MapDrawing / Dialogue / EnergyOutline / TargetingLine）を保持する。
/// サイトのアクセント色には、白文字背景でも可読な <see cref="GetAccent"/>（= MapDrawingColor）を用いる。
/// </summary>
public static class CharacterColorService
{
    public sealed record Colors(
        string Name, string NameColor, string MapDrawingColor,
        string DialogueColor, string EnergyOutlineColor, string TargetingLineColor);

    static readonly IReadOnlyDictionary<string, Colors> _map = Load();

    static IReadOnlyDictionary<string, Colors> Load()
    {
        var result = new Dictionary<string, Colors>(StringComparer.OrdinalIgnoreCase);
        var asm = Assembly.GetExecutingAssembly();
        var name = ResourceResolver.ResolveVersioned(asm, "character_colors.json");
        if (name is null) return result;

        using var stream = asm.GetManifestResourceStream(name)!;
        using var doc = JsonDocument.Parse(stream);
        static string S(JsonElement e, string p) =>
            e.TryGetProperty(p, out var v) ? v.GetString() ?? "" : "";
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var e = prop.Value;
            result[prop.Name] = new Colors(
                S(e, "name"), S(e, "nameColor"), S(e, "mapDrawingColor"),
                S(e, "dialogueColor"), S(e, "energyOutlineColor"), S(e, "targetingLineColor"));
        }
        return result;
    }

    /// <summary>全色（キャラ ID は大小文字不問、例 "ironclad"）。無ければ null。</summary>
    public static Colors? Get(string characterId) =>
        _map.TryGetValue(characterId, out var c) ? c : null;

    /// <summary>
    /// アクセント色（= <c>MapDrawingColor</c>）。白文字を載せる前提のため、明るい NameColor ではなく
    /// 濃いめのマップ描画色を採用する。データが無い場合は <paramref name="fallback"/> を返す。
    /// </summary>
    public static string GetAccent(string characterId, string fallback = "#888") =>
        _map.TryGetValue(characterId, out var c) && c.MapDrawingColor.Length > 0
            ? c.MapDrawingColor : fallback;
}
