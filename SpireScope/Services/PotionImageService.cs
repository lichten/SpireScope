namespace SpireScope.Services;

/// <summary>
/// ポーションの個別 PNG（<c>tools/extracted/images/potions_png/</c> 配下）の Bitmap キャッシュ。
/// パスは <see cref="StS2Shared.Services.PotionImageService.GetRelativePath"/>（potion_images.json）で解決するため、
/// ポーション追加・画像差し替えは JSON/PNG の更新だけで追従する（<see cref="RelicImageService.GetRelicPng"/> のポーション版）。
/// 画像が無い（未マッピング／未生成）ポーションは null。
/// </summary>
static class PotionImageService
{
    static readonly Dictionary<string, Bitmap?> _pngCache = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap? GetPotionPng(string potionId)
    {
        if (_pngCache.TryGetValue(potionId, out var cached)) return cached;

        var rel = StS2Shared.Services.PotionImageService.GetRelativePath(potionId);
        var root = StS2Shared.Services.AssetLocator.FindExtractedRoot();
        if (rel is null || root is null) return _pngCache[potionId] = null;

        var path = Path.Combine(root, "images",
            StS2Shared.Services.PotionImageService.PotionsDirName,
            rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return _pngCache[potionId] = null;

        try
        {
            using var fs = File.OpenRead(path);
            using var img = Image.FromStream(fs);
            return _pngCache[potionId] = new Bitmap(img);
        }
        catch { return _pngCache[potionId] = null; }
    }
}
