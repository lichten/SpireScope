using Windows.Media.Ocr;
using StS2Shared.Services;

namespace StS2Capture.Recognition;

/// <summary>
/// 「エンシェントレリックを選択する」画面の検出器。各行のレリック**名テキスト**を固定バンドで OCR し、
/// レリック名索引（<see cref="CardNameIndex.BuildRelics"/>）にファジー照合して候補を絞る。
/// ただし「パエルの◯」のように同一プレフィックスで末尾漢字だけが違うレリック群は OCR が末尾漢字を
/// 読み切れず一意に定まらない。そこで名前で絞った候補集合の中を**アイコンの色（HSV ヒストグラム）**で
/// 見分ける名前＋アイコンのハイブリッド方式にする。結果は <see cref="ShopItemRecognizer.Result"/>
/// （relic Items）で返し、既存の表示経路（Form1 の BuildShopRows）をそのまま使う。
/// </summary>
public sealed class AncientRelicRecognizer
{
    /// <summary>レリック名1行を囲む固定バンド（クライアント相対・中心＋大きさ）。</summary>
    public readonly record struct NameBand(double CxFrac, double CyFrac, double WFrac, double HFrac);

    /// <summary>1バンドの候補（名前編集距離＋アイコン距離）。</summary>
    readonly record struct BandCand(string Id, int NameDist, double IconChi);

    /// <summary>較正/検証用の1バンド診断情報。</summary>
    public sealed record BandDiag(Rectangle NameRect, Rectangle IconRect, string Ocr,
        IReadOnlyList<string> Family, string? ChosenId, string? ChosenName, int NameDist, double IconChi, bool Accepted);

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

    /// <summary>この数以上の行が一致したらエンシェントレリック選択画面とみなす。</summary>
    public int MinMatches { get; set; } = 2;

    /// <summary>
    /// 名前照合の許容編集距離。末尾漢字（例「涙」）が OCR で不安定なため、単一最良（<see cref="CardNameIndex.FindBest"/>）
    /// より緩めに候補集合（family）を作り、その中をアイコンで一意に決める。
    /// </summary>
    public int MaxNameDist { get; set; } = 2;

    // ── アイコン照合（名前 family の中を見分ける）─────────────────────────────
    /// <summary>アイコン中心の名前バンド中心からの水平オフセット（クライアント幅比。負＝左）。</summary>
    public double IconDxFrac { get; set; } = -0.112;

    /// <summary>アイコン正方形の一辺（クライアント高さ比）。</summary>
    public double IconSizeFrac { get; set; } = 0.052;

    /// <summary>アイコン照合の採否しきい値（カイ二乗、これ以下で採用）。実機で較正する。</summary>
    public double IconMaxDistance { get; set; } = 0.7;

    /// <summary>DB アイコン（透過 PNG）を合成する背景色。エンシェント画面の暗い半透明バーに近い暗色。</summary>
    public Color IconBackground { get; set; } = Color.FromArgb(38, 38, 38);

    /// <summary>アイコン HSV 集計の S/V ビン数（小アイコン識別のため 4）。</summary>
    const int IconSatBins = 4, IconValBins = 4;

    /// <summary>非 null の間、各バンドの二値化名前クロップとアイコンクロップを PNG 保存する（較正用）。</summary>
    public string? SaveCropsDir { get; set; }
    static int _cropSeq;

    readonly OcrEngine? _engine = OcrEngineHelper.CreateEngine();
    readonly string? _relicsDir = ResolveImagesDir(RelicImageService.RelicsDirName);
    CardNameIndex? _index;
    Dictionary<string, float[]>? _iconDb;

    /// <summary>OCR エンジンが利用可能か（言語パック未導入だと null）。</summary>
    public bool IsAvailable => _engine is not null;

    CardNameIndex Index => _index ??= CardNameIndex.BuildRelics();

    /// <summary>名前＋アイコンで各行を識別し、検出したレリックを返す。matched(IsShop)＝MinMatches 以上採用。</summary>
    public ShopItemRecognizer.Result Detect(Bitmap frame, Rectangle client)
    {
        var rects = new List<Rectangle>(NameBands.Count);
        var perBand = new List<IReadOnlyList<BandCand>>(NameBands.Count);
        foreach (var band in NameBands)
        {
            var nameRect = ToPixels(client, band, frame);
            var iconRect = IconRect(client, band, frame);
            rects.Add(nameRect);
            perBand.Add(RankBand(frame, nameRect, iconRect, out _));
        }

        var chosen = AssignUnique(perBand);
        var items = new List<ShopItemRecognizer.Item>(rects.Count);
        for (int i = 0; i < rects.Count; i++)
        {
            bool accept = chosen[i] is { } c && IsAccepted(c);
            IReadOnlyList<ShopItemRecognizer.Candidate> cands = accept
                ? new[] { ToCandidate(chosen[i]!.Value) } : Array.Empty<ShopItemRecognizer.Candidate>();
            items.Add(new ShopItemRecognizer.Item(ShopItemRecognizer.Kind.Relic, rects[i], cands, accept));
        }
        bool matched = _engine is not null && items.Count(i => i.Accepted) >= MinMatches;
        return new ShopItemRecognizer.Result(matched, items);
    }

    /// <summary>較正/検証用。各バンドの OCR 生テキスト・名前 family・（一意割当後の）採用レリックを返す。</summary>
    public IReadOnlyList<BandDiag> Diagnose(Bitmap frame, Rectangle client)
    {
        var nameRects = new List<Rectangle>(NameBands.Count);
        var iconRects = new List<Rectangle>(NameBands.Count);
        var texts = new List<string>(NameBands.Count);
        var perBand = new List<IReadOnlyList<BandCand>>(NameBands.Count);
        foreach (var band in NameBands)
        {
            var nameRect = ToPixels(client, band, frame);
            var iconRect = IconRect(client, band, frame);
            nameRects.Add(nameRect);
            iconRects.Add(iconRect);
            perBand.Add(RankBand(frame, nameRect, iconRect, out var text));
            texts.Add(text);
        }

        var chosen = AssignUnique(perBand);
        var list = new List<BandDiag>(nameRects.Count);
        for (int i = 0; i < nameRects.Count; i++)
        {
            var family = perBand[i].Take(6).Select(c => c.Id).ToList();
            var c = chosen[i];
            bool accept = c is { } cc && IsAccepted(cc);
            list.Add(new BandDiag(nameRects[i], iconRects[i], texts[i], family,
                c?.Id, c is { } m ? CardDatabaseService.GetRelicTitle(m.Id, japanese: true) : null,
                c?.NameDist ?? int.MaxValue, c?.IconChi ?? double.MaxValue, accept));
        }
        return list;
    }

    /// <summary>
    /// 採否。名前 family に入っていれば（＝名前 OCR がそのレリックを距離内で読めていれば）採用。
    /// OCR は文字を返したが名前照合に失敗した時のみ、アイコン距離が十分近ければ採用する
    /// （OCR が空＝背景のバンドは <see cref="RankBand"/> で候補が空になり、ここには来ない）。
    /// </summary>
    bool IsAccepted(BandCand c) => c.NameDist <= MaxNameDist || c.IconChi <= IconMaxDistance;

    static ShopItemRecognizer.Candidate ToCandidate(BandCand c) =>
        new(c.Id, CardDatabaseService.GetRelicTitle(c.Id, japanese: true),
            c.NameDist == int.MaxValue ? c.IconChi : c.NameDist);

    /// <summary>
    /// 1バンドを名前 OCR で family に絞り、その中をアイコン色で順位付けした候補列（アイコン距離昇順）を返す。
    /// 名前が全く読めなければアイコン全 DB をフォールバック候補にする。
    /// </summary>
    IReadOnlyList<BandCand> RankBand(Bitmap frame, Rectangle nameRect, Rectangle iconRect, out string ocrText)
    {
        ocrText = "";

        // 1) 名前 OCR → family（id → 最小編集距離）。
        var nameDist = new Dictionary<string, int>(StringComparer.Ordinal);
        if (_engine is not null && nameRect.Width >= 8 && nameRect.Height >= 6)
        {
            using var crop = ImageOps.CropUpscaleBinarize(frame, nameRect, Math.Max(1, TitleScale));
            TrySaveCrop(crop, "name");
            var res = OcrEngineHelper.RunOcr(_engine, crop);
            if (res is not null)
            {
                ocrText = string.Join(" ", res.Lines.Select(l => l.Text));
                foreach (var line in res.Lines)
                    foreach (var m in Index.FindRanked(line.Text, MaxNameDist))
                        if (!nameDist.TryGetValue(m.CardId, out var ex) || m.Distance < ex)
                            nameDist[m.CardId] = m.Distance;
            }
        }

        // 2) アイコン HSV（実背景上なので合成なし）。
        var db = IconDb();
        float[]? qIcon = null;
        if (iconRect.Width >= 6 && iconRect.Height >= 6)
        {
            qIcon = HsvHistogram.Compute(frame, iconRect, null, IconSatBins, IconValBins);
            TrySaveRegion(frame, iconRect, "icon");
        }

        // 3) 候補集合＝family。名前が読めなければアイコン全 DB フォールバック。
        //    ただし OCR が1文字も返さないバンド（＝レリック名が無い背景。カード選択画面の
        //    カード下の暗い帯など）は空にする。ここで全 DB にフォールバックすると、暗い背景の
        //    HSV が暗色レリックアイコンに近く誤一致し、カード選択画面をエンシェント選択と
        //    誤判定する（実測: band1=KUNAI/band2=SAI が iconChi<0.7 で誤採用）。
        bool ocrHasText = !string.IsNullOrWhiteSpace(ocrText);
        IEnumerable<string> ids = nameDist.Count > 0 ? nameDist.Keys
            : (ocrHasText && qIcon is not null ? (IEnumerable<string>)db.Keys : Array.Empty<string>());

        var list = new List<BandCand>();
        foreach (var id in ids)
        {
            int nd = nameDist.TryGetValue(id, out var d) ? d : int.MaxValue;
            double chi = (qIcon is not null && db.TryGetValue(id, out var h))
                ? HsvHistogram.ChiSquare(qIcon, h) : double.MaxValue;
            list.Add(new BandCand(id, nd, chi));
        }
        // 名前編集距離が主、アイコン距離が従（同一プレフィックスで名前が並ぶ時だけアイコンで決める）。
        list.Sort((a, b) => a.NameDist != b.NameDist ? a.NameDist.CompareTo(b.NameDist) : a.IconChi.CompareTo(b.IconChi));
        return list;
    }

    /// <summary>
    /// 各バンドに相異なるレリックをアイコン距離昇順で貪欲割当する。確信の高い行が先にアイコンで id を確保し、
    /// 同一プレフィックスの残り行は消去法で正しい id に決まる。
    /// </summary>
    static BandCand?[] AssignUnique(List<IReadOnlyList<BandCand>> perBand)
    {
        var assigned = new BandCand?[perBand.Count];
        var used = new HashSet<string>(StringComparer.Ordinal);
        var all = perBand
            .SelectMany((cands, bi) => cands.Select(c => (Band: bi, Cand: c)))
            .OrderBy(x => x.Cand.NameDist).ThenBy(x => x.Cand.IconChi)
            .ToList();
        foreach (var (bi, c) in all)
        {
            if (assigned[bi] is not null || used.Contains(c.Id)) continue;
            assigned[bi] = c;
            used.Add(c.Id);
        }
        return assigned;
    }

    /// <summary>レリックアイコン（透過 PNG）→ 背景合成済み HSV ヒストグラムの索引を遅延構築する。</summary>
    Dictionary<string, float[]> IconDb()
    {
        if (_iconDb is not null) return _iconDb;
        _iconDb = new(StringComparer.Ordinal);
        if (_relicsDir is null) return _iconDb;
        foreach (var id in CardDatabaseService.GetAllRelicIds())
        {
            var path = RelicImageService.GetSourcePath(_relicsDir, id);
            if (path is null || !File.Exists(path)) continue;
            try
            {
                using var bmp = new Bitmap(path);
                _iconDb[id] = HsvHistogram.Compute(bmp, HsvHistogram.AlphaBoundingBox(bmp),
                    IconBackground, IconSatBins, IconValBins);
            }
            catch { /* 壊れた画像はスキップ */ }
        }
        return _iconDb;
    }

    Rectangle IconRect(Rectangle client, NameBand band, Bitmap frame)
    {
        int side = Math.Max(1, (int)(client.Height * IconSizeFrac));
        int cx = client.X + (int)(client.Width * (band.CxFrac + IconDxFrac));
        int cy = client.Y + (int)(client.Height * band.CyFrac);
        return Rectangle.Intersect(
            new Rectangle(cx - side / 2, cy - side / 2, side, side),
            new Rectangle(0, 0, frame.Width, frame.Height));
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

    void TrySaveCrop(Bitmap crop, string tag)
    {
        var dir = SaveCropsDir;
        if (dir is null) return;
        try
        {
            Directory.CreateDirectory(dir);
            int n = System.Threading.Interlocked.Increment(ref _cropSeq);
            crop.Save(Path.Combine(dir, $"ancient_{n:D4}_{tag}.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        catch { /* 保存失敗は無視 */ }
    }

    void TrySaveRegion(Bitmap frame, Rectangle rect, string tag)
    {
        if (SaveCropsDir is null || rect.Width <= 0 || rect.Height <= 0) return;
        try
        {
            using var crop = frame.Clone(rect, frame.PixelFormat);
            TrySaveCrop(crop, tag);
        }
        catch { /* 保存失敗は無視 */ }
    }

    static string? ResolveImagesDir(string dirName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "extracted", "images", dirName);
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
