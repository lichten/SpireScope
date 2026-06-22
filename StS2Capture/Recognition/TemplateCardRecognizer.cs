using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StS2Shared.Services;

namespace StS2Capture.Recognition;

/// <summary>
/// カード矩形の絵窓を card_portraits_png の portrait と HSV カラーヒストグラムで照合して識別する。
/// 事前検証でグレースケール構造照合は失敗、HSV カラーヒストグラム＋カイ二乗距離は明確な差で正解1位だった。
/// </summary>
public sealed class TemplateCardRecognizer : ICardRecognizer
{
    public string Name => "Template";

    // HSV ヒストグラムの bin 数（H×S×V = 108）。
    const int HB = 12, SB = 3, VB = 3, BINS = HB * SB * VB;
    const int SampleN = 40; // ヒストグラム計算時のリサイズ解像度
    // portrait の下端トリム（在ゲーム絵窓の種別装飾を除外するため対応させる。ArtRegionOf と同値）。
    const double PortraitBottomTrim = 0.15;

    // 採否しきい値（調整可能）。best が小さく、2位とのマージンがある時のみ採用。
    public double MaxDistance { get; set; } = 0.85;
    public double MinMargin { get; set; } = 0.04;

    readonly CardRegionDetector _detector = new();
    readonly string? _portraitsDir;
    Dictionary<string, float[]>? _db; // CardId → 正規化 HSV ヒストグラム（遅延構築）

    public TemplateCardRecognizer()
    {
        _portraitsDir = ResolvePortraitsDir();
    }

    public bool IsAvailable => _portraitsDir is not null;

    /// <summary>枠色プロファイル（キャラ別）。矩形検出器へ転送する。</summary>
    public FrameColorProfile FrameProfile
    {
        get => _detector.ActiveProfile;
        set => _detector.ActiveProfile = value;
    }

    public readonly record struct Match(string CardId, double Distance, double Confidence);

    public RecognitionResult Recognize(Bitmap frame)
    {
        var db = EnsureDb();
        var cardBoxes = _detector.Detect(frame);
        if (db.Count == 0 || cardBoxes.Count == 0)
            return new RecognitionResult(
                Array.Empty<RecognizedCard>(), Array.Empty<OcrTextSpan>(),
                cardBoxes, Array.Empty<Rectangle>());

        var cards = new List<RecognizedCard>();
        var artRegions = new List<Rectangle>(cardBoxes.Count);
        foreach (var card in cardBoxes)
        {
            var art = Rectangle.Intersect(
                CardRegionDetector.ArtRegionOf(card),
                new Rectangle(0, 0, frame.Width, frame.Height));
            if (art.Width < 8 || art.Height < 8) continue;
            artRegions.Add(art);

            var m = Identify(frame, art, db);
            if (m is not null)
                cards.Add(new RecognizedCard(m.Value.CardId, "", art, m.Value.Confidence, "Template"));
        }

        return new RecognitionResult(cards, Array.Empty<OcrTextSpan>(), cardBoxes, artRegions);
    }

    /// <summary>絵窓のヒストグラムを DB 全件と照合し、最近傍を返す（しきい値・マージンで採否）。</summary>
    Match? Identify(Bitmap frame, Rectangle art, Dictionary<string, float[]> db)
    {
        var q = Histogram(frame, art);
        string? bestId = null; double best = double.MaxValue, second = double.MaxValue;
        foreach (var (id, h) in db)
        {
            double d = ChiSquare(q, h);
            if (d < best) { second = best; best = d; bestId = id; }
            else if (d < second) second = d;
        }
        if (bestId is null || best > MaxDistance || (second - best) < MinMargin) return null;

        // 距離とマージンから簡易 confidence。
        double conf = Math.Clamp((1.0 - best) * 0.5 + Math.Min(1.0, (second - best) / 0.3) * 0.5, 0, 1);
        return new Match(bestId, best, conf);
    }

    Dictionary<string, float[]> EnsureDb()
    {
        if (_db is not null) return _db;
        _db = new();
        if (_portraitsDir is null) return _db;

        foreach (var id in CardDatabaseService.GetAllCardIds())
        {
            var path = CardImageService.GetSourcePath(_portraitsDir, id);
            if (path is null || !File.Exists(path)) continue;
            try
            {
                using var bmp = new Bitmap(path);
                // 在ゲーム絵窓は下 15%（種別装飾）を除外するので、portrait も上 85% に揃える。
                int ph = Math.Max(1, (int)(bmp.Height * (1.0 - PortraitBottomTrim)));
                _db[id] = Histogram(bmp, new Rectangle(0, 0, bmp.Width, ph));
            }
            catch { /* 壊れた画像はスキップ */ }
        }
        return _db;
    }

    /// <summary>領域を 40×40 にリサイズして正規化 HSV ヒストグラム（108bin）を作る。</summary>
    static float[] Histogram(Bitmap src, Rectangle region)
    {
        using var small = new Bitmap(SampleN, SampleN, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(small))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(src, new Rectangle(0, 0, SampleN, SampleN), region, GraphicsUnit.Pixel);
        }

        var hist = new float[BINS];
        var data = small.LockBits(new Rectangle(0, 0, SampleN, SampleN),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = data.Stride;
            var row = new byte[stride];
            for (int y = 0; y < SampleN; y++)
            {
                Marshal.Copy(data.Scan0 + y * stride, row, 0, stride);
                for (int x = 0; x < SampleN; x++)
                {
                    int p = x * 4;
                    ToHsv(row[p + 2], row[p + 1], row[p], out double h, out double s, out double v);
                    int hi = Math.Min(HB - 1, (int)(h * HB));
                    int si = Math.Min(SB - 1, (int)(s * SB));
                    int vi = Math.Min(VB - 1, (int)(v * VB));
                    hist[(hi * SB + si) * VB + vi] += 1f;
                }
            }
        }
        finally { small.UnlockBits(data); }

        float tot = SampleN * SampleN;
        for (int i = 0; i < BINS; i++) hist[i] /= tot;
        return hist;
    }

    /// <summary>RGB(0-255) → HSV(0..1)。.NET の HSL とは別なので手計算する。</summary>
    static void ToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf)), min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;
        v = max;
        s = max <= 0 ? 0 : delta / max;
        if (delta <= 0) { h = 0; return; }
        double hue;
        if (max == rf) hue = (gf - bf) / delta % 6;
        else if (max == gf) hue = (bf - rf) / delta + 2;
        else hue = (rf - gf) / delta + 4;
        hue /= 6;
        if (hue < 0) hue += 1;
        h = hue;
    }

    static double ChiSquare(float[] u, float[] v)
    {
        double s = 0;
        for (int i = 0; i < u.Length; i++)
        {
            double d = u[i] - v[i], den = u[i] + v[i];
            if (den > 0) s += d * d / den;
        }
        return s;
    }

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
