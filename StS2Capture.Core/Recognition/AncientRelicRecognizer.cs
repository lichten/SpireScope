using Windows.Media.Ocr;
using StS2Shared.Services;

namespace StS2Capture.Recognition;

/// <summary>
/// 「エンシェントレリックを選択する」画面の検出器。アイコン隣に出るレリック**名テキスト**を固定バンドで
/// OCR し、レリック名索引（<see cref="CardNameIndex.BuildRelics"/>）にファジー照合して特定する。
/// 小さく減光されたアイコンの色照合より堅牢。結果は <see cref="ShopItemRecognizer.Result"/>（relic Items）で
/// 返し、既存の表示経路（Form1 の BuildShopRows）をそのまま使う。
/// </summary>
public sealed class AncientRelicRecognizer
{
    /// <summary>レリック名1行を囲む固定バンド（クライアント相対・中心＋大きさ）。</summary>
    public readonly record struct NameBand(double CxFrac, double CyFrac, double WFrac, double HFrac);

    /// <summary>
    /// 名前バンド（提供スクショ＝テスカタラ縦3行から実測した初期較正値）。アイコン右の名前テキストを囲む。
    /// 実機で <see cref="SaveCropsDir"/> を使い微調整する。
    /// </summary>
    // 提供スクショ（テスカタラ 1287×765）で OCR が3レリックとも編集距離0で読めた実測較正値。
    public List<NameBand> NameBands { get; set; } = new()
    {
        new(0.382, 0.719, 0.21, 0.042),
        new(0.382, 0.809, 0.21, 0.042),
        new(0.382, 0.899, 0.21, 0.042),
    };

    /// <summary>タイトル帯の拡大倍率（二値化 OCR 前処理）。小さな漢字の認識のため 4。</summary>
    public int TitleScale { get; set; } = 4;

    /// <summary>この数以上の名前が一致したらエンシェントレリック選択画面とみなす。</summary>
    public int MinMatches { get; set; } = 2;

    /// <summary>非 null の間、各バンドの二値化クロップを PNG 保存する（較正用）。</summary>
    public string? SaveCropsDir { get; set; }
    static int _cropSeq;

    readonly OcrEngine? _engine = OcrEngineHelper.CreateEngine();
    CardNameIndex? _index;

    /// <summary>OCR エンジンが利用可能か（言語パック未導入だと null）。</summary>
    public bool IsAvailable => _engine is not null;

    CardNameIndex Index => _index ??= CardNameIndex.BuildRelics();

    /// <summary>名前バンドを OCR・照合し、検出したレリックを返す。matched(IsShop)＝MinMatches 以上一致。</summary>
    public ShopItemRecognizer.Result Detect(Bitmap frame, Rectangle client)
    {
        var items = new List<ShopItemRecognizer.Item>(NameBands.Count);
        foreach (var band in NameBands)
        {
            var rect = ToPixels(client, band, frame);
            var cand = MatchBand(frame, rect, out _);
            IReadOnlyList<ShopItemRecognizer.Candidate> cands =
                cand is { } c ? new[] { c } : Array.Empty<ShopItemRecognizer.Candidate>();
            items.Add(new ShopItemRecognizer.Item(ShopItemRecognizer.Kind.Relic, rect, cands, cands.Count > 0));
        }
        bool matched = _engine is not null && items.Count(i => i.Accepted) >= MinMatches;
        return new ShopItemRecognizer.Result(matched, items);
    }

    /// <summary>較正/検証用。各バンドの OCR 生テキストと最良一致レリックを返す（capture-verify 用）。</summary>
    public IReadOnlyList<(Rectangle Region, string Ocr, ShopItemRecognizer.Candidate? Match)> Diagnose(
        Bitmap frame, Rectangle client)
    {
        var list = new List<(Rectangle, string, ShopItemRecognizer.Candidate?)>(NameBands.Count);
        foreach (var band in NameBands)
        {
            var rect = ToPixels(client, band, frame);
            var cand = MatchBand(frame, rect, out var text);
            list.Add((rect, text, cand));
        }
        return list;
    }

    ShopItemRecognizer.Candidate? MatchBand(Bitmap frame, Rectangle rect, out string ocrText)
    {
        ocrText = "";
        if (_engine is null || rect.Width < 8 || rect.Height < 6) return null;

        using var crop = ImageOps.CropUpscaleBinarize(frame, rect, Math.Max(1, TitleScale));
        TrySaveCrop(crop);
        var res = OcrEngineHelper.RunOcr(_engine, crop);
        if (res is null) return null;
        ocrText = string.Join(" ", res.Lines.Select(l => l.Text));

        CardNameIndex.Match? best = null;
        foreach (var line in res.Lines)
        {
            var m = Index.FindBest(line.Text);
            if (m is null) continue;
            if (best is null || m.Value.Confidence > best.Value.Confidence) best = m;
        }
        if (best is null) return null;
        return new ShopItemRecognizer.Candidate(best.Value.CardId,
            CardDatabaseService.GetRelicTitle(best.Value.CardId, japanese: true), best.Value.Distance);
    }

    static Rectangle ToPixels(Rectangle client, NameBand band, Bitmap frame)
    {
        int w = Math.Max(1, (int)(client.Width * band.WFrac));
        int h = Math.Max(1, (int)(client.Height * band.HFrac));
        int cx = client.X + (int)(client.Width * band.CxFrac);
        int cy = client.Y + (int)(client.Height * band.CyFrac);
        return Rectangle.Intersect(
            new Rectangle(cx - w / 2, cy - h / 2, w, h),
            new Rectangle(0, 0, frame.Width, frame.Height));
    }

    void TrySaveCrop(Bitmap crop)
    {
        var dir = SaveCropsDir;
        if (dir is null) return;
        try
        {
            Directory.CreateDirectory(dir);
            int n = System.Threading.Interlocked.Increment(ref _cropSeq);
            crop.Save(Path.Combine(dir, $"ancient_{n:D4}.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        catch { /* 保存失敗は無視 */ }
    }
}
