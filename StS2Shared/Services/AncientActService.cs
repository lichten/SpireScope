using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// Ancient の登場アクト（第何幕に出るか）を読む。
/// card-type-extractor が DLL の <c>ActModel.get_AllAncients</c> から生成した
/// ancient_acts.json（バージョンフォルダ）を参照する。コスト・出現重みは DLL に無いため対象外。
/// <see cref="AncientOptionService"/>（報酬プール）の補完。
/// </summary>
public static class AncientActService
{
    static readonly IReadOnlyDictionary<string, int> _acts = Load();

    static IReadOnlyDictionary<string, int> Load()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var asm = Assembly.GetExecutingAssembly();
        var name = ResourceResolver.ResolveVersioned(asm, "ancient_acts.json");
        if (name is null) return result;

        using var stream = asm.GetManifestResourceStream(name)!;
        using var doc = JsonDocument.Parse(stream);
        foreach (var prop in doc.RootElement.EnumerateObject())
            if (prop.Value.TryGetProperty("act", out var a) && a.ValueKind == JsonValueKind.Number)
                result[prop.Name] = a.GetInt32();
        return result;
    }

    /// <summary>登場アクト番号（1/2/3）。掲載が無い（Darv / TheArchitect 等の特殊）場合は null。</summary>
    public static int? GetAct(string ancientId) =>
        _acts.TryGetValue(ancientId, out var a) ? a : null;
}
