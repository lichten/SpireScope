using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// ancient_options.json から抽出した Ancient の報酬候補データを提供する。
/// このファイルは card-type-extractor を実行することで生成される。
/// </summary>
public static class AncientOptionService
{
    /// <summary>
    /// ancient_options.json が正常にロードされた場合 true。
    /// false の場合は card-type-extractor が未実行か、ゲームアップデートで抽出失敗した可能性がある。
    /// </summary>
    public static readonly bool IsDataAvailable;

    // ancientId → (グループ名 → アイテム ID リスト)
    static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> _data;

    static AncientOptionService()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resName = ResourceResolver.ResolveVersioned(asm, "ancient_options.json");

        if (resName is null)
        {
            IsDataAvailable = false;
            _data = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>();
            return;
        }

        try
        {
            using var stream = asm.GetManifestResourceStream(resName)!;
            using var doc = JsonDocument.Parse(stream);

            var result = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var ancientProp in doc.RootElement.EnumerateObject())
            {
                var groups = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var groupProp in ancientProp.Value.EnumerateObject())
                {
                    var items = groupProp.Value.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    if (items.Count > 0)
                        groups[groupProp.Name] = items.AsReadOnly();
                }
                if (groups.Count > 0)
                    result[ancientProp.Name] = groups;
            }

            _data = result;
            IsDataAvailable = true;
        }
        catch
        {
            IsDataAvailable = false;
            _data = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        }
    }

    /// <summary>
    /// 指定した Ancient のオプショングループを返す。
    /// データが存在しない場合は null。
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>>? GetGroups(string ancientId)
    {
        _data.TryGetValue(ancientId, out var groups);
        return groups;
    }
}
