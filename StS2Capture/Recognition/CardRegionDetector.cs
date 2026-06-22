namespace StS2Capture.Recognition;

/// <summary>
/// キャプチャ画像からカードの矩形領域を「縁の色（明るい低彩度の銀フレーム）」で検出する。
/// 明るく低彩度のマスク → 膨張で隙間を埋める → 連結成分 → カード形状でフィルタ。
/// 検出したカード矩形は、タイトル／絵／説明文の切り出しや Template 照合の基盤になる。
/// </summary>
public sealed class CardRegionDetector
{
    // 青フレーム判定のしきい値（調整可能）。実機計測の青フレームに合わせた既定値。
    public int FrameMinB { get; set; } = 95;        // B がこれ以上
    public int FrameMinBminusR { get; set; } = 50;  // B - R がこれ以上（青さ）
    public int FrameMinBminusG { get; set; } = 14;  // B - G がこれ以上（青緑を除外）
    public int FrameMaxR { get; set; } = 80;         // R がこれ以下

    // カード形状フィルタ（フレーム幅比・縦横比・リング状の充填率）。
    public double MinWidthRatio { get; set; } = 0.05;
    public double MaxWidthRatio { get; set; } = 0.33;
    public double MinAspect { get; set; } = 1.10; // H/W
    public double MaxAspect { get; set; } = 1.80;
    public double MinFill { get; set; } = 0.015;
    public double MaxFill { get; set; } = 0.65;

    /// <summary>直近に作ったマスク（デバッグ保存用）。</summary>
    public bool[]? LastMask { get; private set; }
    public int LastMaskW { get; private set; }
    public int LastMaskH { get; private set; }

    public List<Rectangle> Detect(Bitmap frame)
    {
        int w = frame.Width, h = frame.Height;
        var mask = ImageOps.BuildFrameMask(frame, FrameMinB, FrameMinBminusR, FrameMinBminusG, FrameMaxR);

        int dilate = Math.Max(2, w / 400);
        var dil = Dilate(mask, w, h, dilate);
        LastMask = dil; LastMaskW = w; LastMaskH = h;

        int minW = (int)(w * MinWidthRatio);
        int maxW = (int)(w * MaxWidthRatio);

        var boxes = new List<Rectangle>();
        foreach (var (box, pixels) in ConnectedComponents(dil, w, h, minArea: minW * minW / 4))
        {
            if (box.Width < minW || box.Width > maxW) continue;
            double aspect = (double)box.Height / box.Width;
            if (aspect < MinAspect || aspect > MaxAspect) continue;
            double fill = (double)pixels / (box.Width * box.Height);
            if (fill < MinFill || fill > MaxFill) continue;
            boxes.Add(box);
        }

        return MergeOverlaps(boxes).OrderBy(b => b.Left).ToList();
    }

    /// <summary>
    /// カード矩形から絵（portrait）の窓を算出する。タイトルの下、本体矩形の上部にある。
    /// 全体の絵窓は 上 +0.05×幅・高さ 0.55×幅・左右 0.04×幅。ただし下端には在ゲーム特有の
    /// 「アタック」等の種別装飾が重なるため、下 15% を切り詰める（0.55×0.85≈0.4675）。
    /// 照合の対応を保つため portrait 側も上 85% を使う（TemplateCardRecognizer 側で同じトリム）。
    /// </summary>
    public static Rectangle ArtRegionOf(Rectangle card)
    {
        const double TopFrac = 0.05, HeightFrac = 0.55 * 0.85, SideInset = 0.04;
        int x1 = card.Left + (int)(card.Width * SideInset);
        int x2 = card.Right - (int)(card.Width * SideInset);
        int y1 = card.Top + (int)(card.Width * TopFrac);
        int y2 = y1 + (int)(card.Width * HeightFrac);
        return Rectangle.FromLTRB(x1, y1, Math.Max(x1 + 4, x2), Math.Max(y1 + 4, y2));
    }

    /// <summary>
    /// カード矩形からタイトル小領域を算出する。青フレームは「飾りバー＋コスト玉」と「本体矩形」に
    /// 分かれ、間のタイトルバナーは青くないため、検出される矩形は本体（タイトルの下）になる。
    /// よってタイトルは検出矩形の **上** にある。実測：本体 box.Top の上 ~0.03〜0.19×幅 が文字帯。
    /// 左はコスト玉を除外するためインセットする。幅を縦の単位に使い解像度不変にする。
    /// </summary>
    public static Rectangle TitleRegionOf(Rectangle card)
    {
        const double AboveTopFrac = 0.19, AboveBotFrac = 0.02;
        const double LeftInset = 0.16, RightInset = 0.04;
        int y1 = card.Top - (int)(card.Width * AboveTopFrac);
        int y2 = card.Top - (int)(card.Width * AboveBotFrac);
        int x1 = card.Left + (int)(card.Width * LeftInset);
        int x2 = card.Right - (int)(card.Width * RightInset);
        return Rectangle.FromLTRB(x1, Math.Max(0, y1), Math.Max(x1 + 4, x2), Math.Max(y1 + 4, y2));
    }

    // ---- 内部処理 ----

    static bool[] Dilate(bool[] mask, int w, int h, int r)
    {
        // 分離可能（水平→垂直）な箱膨張。
        var tmp = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            int baseY = y * w;
            for (int x = 0; x < w; x++)
            {
                bool on = false;
                for (int dx = -r; dx <= r && !on; dx++)
                {
                    int nx = x + dx;
                    if (nx >= 0 && nx < w && mask[baseY + nx]) on = true;
                }
                tmp[baseY + x] = on;
            }
        }
        var outp = new bool[w * h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                bool on = false;
                for (int dy = -r; dy <= r && !on; dy++)
                {
                    int ny = y + dy;
                    if (ny >= 0 && ny < h && tmp[ny * w + x]) on = true;
                }
                outp[y * w + x] = on;
            }
        }
        return outp;
    }

    static IEnumerable<(Rectangle Box, int Pixels)> ConnectedComponents(bool[] mask, int w, int h, int minArea)
    {
        var visited = new bool[w * h];
        var stack = new Stack<int>();
        for (int i = 0; i < mask.Length; i++)
        {
            if (!mask[i] || visited[i]) continue;
            int minX = w, minY = h, maxX = 0, maxY = 0, count = 0;
            stack.Push(i);
            visited[i] = true;
            while (stack.Count > 0)
            {
                int idx = stack.Pop();
                int x = idx % w, y = idx / w;
                count++;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
                // 8 近傍
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        int nIdx = ny * w + nx;
                        if (mask[nIdx] && !visited[nIdx]) { visited[nIdx] = true; stack.Push(nIdx); }
                    }
            }
            var box = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
            if (box.Width * box.Height >= minArea)
                yield return (box, count);
        }
    }

    static List<Rectangle> MergeOverlaps(List<Rectangle> boxes)
    {
        var result = new List<Rectangle>();
        foreach (var b in boxes)
        {
            bool merged = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].IntersectsWith(b))
                {
                    result[i] = Rectangle.Union(result[i], b);
                    merged = true;
                    break;
                }
            }
            if (!merged) result.Add(b);
        }
        return result;
    }
}
