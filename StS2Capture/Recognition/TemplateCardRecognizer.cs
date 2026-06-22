using StS2Shared.Services;

namespace StS2Capture.Recognition;

/// <summary>
/// card_portraits_png の絵とフレーム内のカード絵を知覚ハッシュ（aHash）で照合する実験実装。
/// インターフェースを OCR と差し替え可能にするための土台。カード絵領域の切り出し
/// （セグメンテーション）は本試験では未実装のため <see cref="Recognize"/> は領域ヒント無しでは
/// 空を返す。領域が得られれば <see cref="Identify"/> で最近傍カードを特定できる。
/// </summary>
public sealed class TemplateCardRecognizer : ICardRecognizer
{
    public string Name => "Template";

    public readonly record struct Match(string CardId, int Distance, double Confidence);

    // CardId → 8x8 aHash（64bit）。遅延構築。
    Dictionary<string, ulong>? _hashes;
    readonly string? _portraitsDir;

    public TemplateCardRecognizer()
    {
        _portraitsDir = ResolvePortraitsDir();
    }

    public bool IsAvailable => _portraitsDir is not null;

    public RecognitionResult Recognize(Bitmap frame)
    {
        // 領域セグメンテーション未実装のため、フレーム全体からの自動検出は試験対象外。
        // 領域が与えられれば Identify() で特定できる（差し替え土台の確認用）。
        return RecognitionResult.Empty;
    }

    /// <summary>切り出したカード絵領域に最も近いカードを返す（しきい値超過なら null）。</summary>
    public Match? Identify(Bitmap cardArtRegion)
    {
        var db = EnsureHashes();
        if (db.Count == 0) return null;

        ulong h = AverageHash(cardArtRegion);
        string? bestId = null;
        int bestDist = int.MaxValue;
        foreach (var (id, th) in db)
        {
            int d = Hamming(h, th);
            if (d < bestDist) { bestDist = d; bestId = id; }
        }
        if (bestId is null || bestDist > 12) return null;
        return new Match(bestId, bestDist, 1.0 - bestDist / 64.0);
    }

    Dictionary<string, ulong> EnsureHashes()
    {
        if (_hashes is not null) return _hashes;
        _hashes = new();
        if (_portraitsDir is null) return _hashes;

        foreach (var id in CardDatabaseService.GetAllCardIds())
        {
            var path = CardImageService.GetSourcePath(_portraitsDir, id);
            if (path is null || !File.Exists(path)) continue;
            try
            {
                using var bmp = new Bitmap(path);
                _hashes[id] = AverageHash(bmp);
            }
            catch { /* 壊れた画像はスキップ */ }
        }
        return _hashes;
    }

    /// <summary>8x8 グレースケールの平均ハッシュ（64bit）。</summary>
    static ulong AverageHash(Bitmap src)
    {
        const int N = 8;
        using var small = new Bitmap(N, N);
        using (var g = Graphics.FromImage(small))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, N, N);
        }

        Span<int> lum = stackalloc int[N * N];
        int sum = 0;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var c = small.GetPixel(x, y);
                int l = (c.R * 30 + c.G * 59 + c.B * 11) / 100;
                lum[y * N + x] = l;
                sum += l;
            }

        int avg = sum / (N * N);
        ulong hash = 0;
        for (int i = 0; i < N * N; i++)
            if (lum[i] >= avg) hash |= 1UL << i;
        return hash;
    }

    static int Hamming(ulong a, ulong b) => System.Numerics.BitOperations.PopCount(a ^ b);

    /// <summary>tools/extracted/images/card_portraits_png を上位ディレクトリに遡って探す。</summary>
    static string? ResolvePortraitsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "extracted", "images",
                CardImageService.PortraitsDirName);
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
