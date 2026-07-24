using System.Diagnostics;
using SpireScope.Services;
using StS2Capture;
using StS2Capture.Recognition;
using StS2Shared.Services;

namespace SpireScope;

/// <summary>
/// ライブキャプチャ検出結果のメイン画面表示（画像タイル）。カードはサムネイル、レリック／ポーションは
/// PNG タイルで描画し、名前と Tier バッジを併記する。クリックで情報サイトリンクを開く。
/// 検出対象外の画面に切り替わっても最後の検出を保持し、ヘッダに「前回の検出」注記を付ける。
/// 生の確信度・距離・キャプチャ画像は <see cref="CaptureDiagnosticsForm"/>（キャプチャ診断）側で確認する。
/// </summary>
sealed class CaptureResultView : UserControl
{
    const int CardW = 120, CardH = 91, ItemTile = 72, NameH = 16, Gap = 6, PadX = 8, PadY = 8, HeaderH = 30;

    sealed record Hit(Rectangle Rect, string Kind, string Id, string Tooltip);

    readonly Panel _scrollPanel = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = SystemColors.Control };
    readonly PictureBox _pictureBox = new() { Location = new Point(0, 0), SizeMode = PictureBoxSizeMode.Normal };
    readonly ToolTip _hoverTip = new() { InitialDelay = 400, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
    readonly ContextMenuStrip _linkMenu = new();
    readonly Dictionary<string, Bitmap?> _thumbCache = new(StringComparer.OrdinalIgnoreCase);
    readonly List<Hit> _hitMap = [];
    string? _hoveredKey;

    CaptureLoop.Result? _detection;   // 最後に検出があった Result（保持表示用）
    bool _stale;                      // 現在のゲーム画面は検出対象外＝保持表示中
    bool _everCaptured;               // 一度もキャプチャ結果を受けていなければ操作案内を出す

    public CaptureResultView()
    {
        Controls.Add(_scrollPanel);
        _scrollPanel.Controls.Add(_pictureBox);
        _pictureBox.MouseMove += OnPictureBoxMouseMove;
        _pictureBox.MouseClick += OnPictureBoxClick;
        _pictureBox.MouseLeave += (_, _) => { _hoverTip.Hide(_pictureBox); _hoveredKey = null; _pictureBox.Cursor = Cursors.Default; };
        Resize += (_, _) => Recompose();
        VisibleChanged += (_, _) => { if (Visible) Recompose(); };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var bmp in _thumbCache.Values) bmp?.Dispose();
            _pictureBox.Image?.Dispose();
            _hoverTip.Dispose();
            _linkMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// キャプチャ結果を反映する。検出（カード or レリック/ポーション）があればそれを表示し、
    /// 検出ゼロなら直前の検出を保持したまま「前回の検出」注記に切り替える。
    /// 言語切替・ティアスコア更新時は Form1 が同じ Result で再適用する（合成時に毎回引き直すため追従する）。
    /// </summary>
    public void ShowResult(CaptureLoop.Result? result)
    {
        if (result is not null)
        {
            _everCaptured = true;
            bool hasDetection = result.Cards.Count > 0 || result.Shop is { Items.Count: > 0 };
            if (hasDetection)
            {
                _detection = result;
                _stale = false;
            }
            else
            {
                _stale = _detection is not null;
            }
        }
        Recompose();
    }

    void Recompose()
    {
        int width = _scrollPanel.ClientSize.Width;
        if (width <= 40) return;

        var bmp = _detection is null ? ComposePlaceholder(width) : ComposeTiles(width, _detection);
        var oldImage = _pictureBox.Image;
        _pictureBox.Size = new Size(bmp.Width, bmp.Height);
        _pictureBox.Image = bmp;
        oldImage?.Dispose();
    }

    // ---- 合成 ----

    Bitmap ComposePlaceholder(int width)
    {
        bool ja = AppLanguage.IsJapanese;
        string message = !_everCaptured
            ? (ja ? "自動監視をオンにするか、手動キャプチャを実行してください"
                  : "Turn on auto-monitoring or run a manual capture")
            : (ja ? "対象画面なし（カード選択／ショップ／エンシェント画面で検出します）"
                  : "No target screen (detects card select / shop / ancient screens)");

        _hitMap.Clear();
        int height = Math.Max(_scrollPanel.ClientSize.Height, 120);
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(SystemColors.Control);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var font = new Font("Segoe UI", 9.5f);
        using var fg = new SolidBrush(SystemColors.GrayText);
        using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(message, font, fg, new RectangleF(PadX, 0, width - PadX * 2, height), fmt);
        return bmp;
    }

    Bitmap ComposeTiles(int width, CaptureLoop.Result result)
    {
        bool ja = AppLanguage.IsJapanese;
        var cards = result.Cards;
        var items = result.Shop is { Items.Count: > 0 } ? result.Shop.Items : [];

        int cardsPerRow = Math.Max(1, (width - 2 * PadX + Gap) / (CardW + Gap));
        int itemsPerRow = Math.Max(1, (width - 2 * PadX + Gap) / (ItemTile + Gap));
        int cardRows = cards.Count > 0 ? (cards.Count + cardsPerRow - 1) / cardsPerRow : 0;
        int itemRows = items.Count > 0 ? (items.Count + itemsPerRow - 1) / itemsPerRow : 0;

        int totalHeight = PadY + HeaderH;
        if (cardRows > 0) totalHeight += cardRows * (CardH + NameH + Gap);
        if (itemRows > 0) totalHeight += itemRows * (ItemTile + NameH + Gap);
        totalHeight += PadY;

        _hitMap.Clear();
        var bmp = new Bitmap(width, Math.Max(totalHeight, 1));
        using var g = Graphics.FromImage(bmp);
        g.Clear(SystemColors.Control);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int y = PadY;
        DrawHeader(g, result, cards.Count + items.Count, new Rectangle(PadX, y, width - 2 * PadX, HeaderH));
        y += HeaderH;

        for (int i = 0; i < cards.Count; i++)
        {
            int col = i % cardsPerRow, row = i / cardsPerRow;
            var rect = new Rectangle(PadX + col * (CardW + Gap), y + row * (CardH + NameH + Gap), CardW, CardH);
            DrawCardTile(g, cards[i], rect, ja);
        }
        if (cardRows > 0) y += cardRows * (CardH + NameH + Gap);

        for (int i = 0; i < items.Count; i++)
        {
            int col = i % itemsPerRow, row = i / itemsPerRow;
            var rect = new Rectangle(PadX + col * (ItemTile + Gap), y + row * (ItemTile + NameH + Gap), ItemTile, ItemTile);
            DrawShopItemTile(g, items[i], rect, ja);
        }

        // 保持表示（前回の検出）は全体をわずかに沈ませて「今の画面ではない」ことを示す。
        if (_stale)
        {
            using var veil = new SolidBrush(Color.FromArgb(70, SystemColors.Control));
            g.FillRectangle(veil, new Rectangle(0, PadY + HeaderH, width, bmp.Height));
        }

        return bmp;
    }

    void DrawHeader(Graphics g, CaptureLoop.Result result, int count, Rectangle rect)
    {
        bool ja = AppLanguage.IsJapanese;
        string label = result.Screen switch
        {
            ScreenRecognizer.ScreenType.CardSelect => ja ? "カード選択" : "Card Select",
            ScreenRecognizer.ScreenType.Shop => ja ? "ショップ" : "Shop",
            ScreenRecognizer.ScreenType.AncientSelect => ja ? "エンシェントレリック候補" : "Ancient Relics",
            _ => ja ? "検出結果" : "Detected",
        };
        string countStr = ja ? $"{count}件" : count.ToString();
        string staleNote = _stale ? (ja ? "（前回の検出）" : " (previous)") : "";

        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var brush = new SolidBrush(_stale ? SystemColors.GrayText : SystemColors.ControlText);
        float textH = font.GetHeight(g);
        g.DrawString($"── {label}  ({countStr}){staleNote}", font, brush,
            new PointF(rect.X, rect.Y + (rect.Height - textH) / 2f));
    }

    void DrawCardTile(Graphics g, RecognizedCard card, Rectangle rect, bool ja)
    {
        var thumbnail = GetCardThumbnail(card.CardId);
        if (thumbnail is not null)
            g.DrawImage(thumbnail, rect);
        else
            DeckOverviewForm.DrawPlaceholder(g, CardDatabaseService.GetName(card.CardId, japanese: false), rect);

        using var borderPen = new Pen(Color.FromArgb(100, 0, 0, 0));
        g.DrawRectangle(borderPen, rect);

        DrawTierBadge(g, TierScoreService.FormatCardTier(card.CardId), rect);
        DrawNameStrip(g, CardDatabaseService.GetName(card.CardId, japanese: ja), rect);

        var tooltip = BuildCardTooltip(card);
        _hitMap.Add(new Hit(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height + NameH), "card", card.CardId, tooltip));
    }

    void DrawShopItemTile(Graphics g, ShopItemRecognizer.Item item, Rectangle rect, bool ja)
    {
        if (item.Candidates.Count == 0) return;   // 候補なしはメイン表示では出さない（診断側で確認）

        bool isRelic = item.Kind == ShopItemRecognizer.Kind.Relic;
        var id = item.Candidates[0].Id;
        var name = isRelic
            ? CardDatabaseService.GetRelicTitle(id, japanese: ja)
            : CardDatabaseService.GetPotionTitle(id, japanese: ja);

        if (isRelic)
        {
            DeckOverviewForm.DrawRelicTile(g, new RelicEntry(id,
                CardDatabaseService.GetRelicTitle(id, japanese: false),
                CardDatabaseService.GetRelicTitle(id, japanese: true)), rect);
        }
        else
        {
            DrawPotionTile(g, id, name, rect);
        }

        // 弱一致（Accepted=false）は半透明グレーで沈ませて確度の低さを示す。
        if (!item.Accepted)
        {
            using var veil = new SolidBrush(Color.FromArgb(130, 220, 220, 220));
            g.FillRectangle(veil, rect);
        }

        DrawTierBadge(g, isRelic ? TierScoreService.FormatRelicTier(id) : TierScoreService.FormatPotionTier(id), rect);
        if (item.Candidates.Count > 1) DrawMoreBadge(g, item.Candidates.Count - 1, rect);
        DrawNameStrip(g, name, rect);

        var tooltip = BuildShopItemTooltip(item, isRelic);
        _hitMap.Add(new Hit(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height + NameH),
            isRelic ? "relic" : "potion", id, tooltip));
    }

    static void DrawPotionTile(Graphics g, string potionId, string name, Rectangle rect)
    {
        using var bg = new SolidBrush(Color.FromArgb(222, 232, 240));
        g.FillRectangle(bg, rect);
        using var borderPen = new Pen(Color.FromArgb(120, 70, 100, 130));
        g.DrawRectangle(borderPen, rect);

        var img = Services.PotionImageService.GetPotionPng(potionId);
        if (img is not null)
        {
            // アスペクト比を保持してタイル内に内接・中央寄せ（DrawRelicTile と同方式）。
            const int ImgPad = 2;
            int boxW = rect.Width - ImgPad * 2;
            int boxH = rect.Height - ImgPad * 2;
            double scale = Math.Min((double)boxW / img.Width, (double)boxH / img.Height);
            int w = Math.Max(1, (int)Math.Round(img.Width * scale));
            int h = Math.Max(1, (int)Math.Round(img.Height * scale));
            var oldInterp = g.InterpolationMode;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, new Rectangle(rect.X + (rect.Width - w) / 2, rect.Y + (rect.Height - h) / 2, w, h));
            g.InterpolationMode = oldInterp;
            return;
        }

        // フォールバック：PNG 未生成（クリーン展開直後など）は名前を中央表示。
        using var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var fg = new SolidBrush(Color.FromArgb(50, 80, 110));
        g.DrawString(name, font, fg,
            new RectangleF(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), fmt);
    }

    static void DrawNameStrip(Graphics g, string name, Rectangle tileRect)
    {
        var r = new RectangleF(tileRect.X, tileRect.Bottom + 1, tileRect.Width, NameH - 2);
        using var font = new Font("Segoe UI", 7.5f);
        using var fg = new SolidBrush(SystemColors.ControlText);
        using var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        g.DrawString(name, font, fg, r, fmt);
    }

    static void DrawTierBadge(Graphics g, string tier, Rectangle tileRect)
    {
        if (string.IsNullOrEmpty(tier)) return;
        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var sz = g.MeasureString(tier, font);
        float bw = sz.Width + 4, bh = sz.Height + 2;
        var r = new RectangleF(tileRect.Right - bw - 1, tileRect.Y + 1, bw, bh);
        using var bg = new SolidBrush(TierBadgeColor(tier));
        g.FillRectangle(bg, r);
        using var fg = new SolidBrush(Color.White);
        g.DrawString(tier, font, fg, r.X + 2, r.Y + 1);
    }

    static Color TierBadgeColor(string tier) => tier.Length == 0 ? Color.DimGray : tier[0] switch
    {
        'S' => Color.FromArgb(210, 190, 120, 20),   // 金
        'A' => Color.FromArgb(210, 60, 130, 50),    // 緑
        'B' => Color.FromArgb(210, 50, 100, 170),   // 青
        'C' => Color.FromArgb(210, 110, 110, 110),  // 灰
        _   => Color.FromArgb(210, 140, 80, 60),    // D 以下は茶
    };

    static void DrawMoreBadge(Graphics g, int extraCount, Rectangle tileRect)
    {
        string text = $"+{extraCount}";
        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var sz = g.MeasureString(text, font);
        float bw = sz.Width + 4, bh = sz.Height + 2;
        var r = new RectangleF(tileRect.Right - bw - 1, tileRect.Bottom - bh - 1, bw, bh);
        using var bg = new SolidBrush(Color.FromArgb(190, 0, 0, 0));
        g.FillRectangle(bg, r);
        using var fg = new SolidBrush(Color.White);
        g.DrawString(text, font, fg, r.X + 2, r.Y + 1);
    }

    // ---- ツールチップ ----

    static string BuildCardTooltip(RecognizedCard card)
    {
        bool ja = AppLanguage.IsJapanese;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(CardDatabaseService.GetName(card.CardId, japanese: ja));
        var (descEn, descJa) = CardDatabaseService.GetDescription(card.CardId);
        var descText = DescriptionFormatter.Resolve(ja ? descJa : descEn,
            CardDatabaseService.GetCardStats(card.CardId), japanese: ja);
        if (!string.IsNullOrWhiteSpace(descText)) sb.AppendLine(descText);
        sb.Append(ja
            ? $"確信度 {card.Confidence:F2}（{card.Recognizer}）"
            : $"Confidence {card.Confidence:F2} ({card.Recognizer})");
        return sb.ToString().Trim();
    }

    static string BuildShopItemTooltip(ShopItemRecognizer.Item item, bool isRelic)
    {
        bool ja = AppLanguage.IsJapanese;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < item.Candidates.Count; i++)
        {
            var c = item.Candidates[i];
            var name = isRelic
                ? CardDatabaseService.GetRelicTitle(c.Id, japanese: ja)
                : CardDatabaseService.GetPotionTitle(c.Id, japanese: ja);
            sb.AppendLine(i == 0 ? name : $"{(ja ? "候補" : "Alt")}: {name} [{c.Id}] ({c.Distance:F2})");
        }
        if (!item.Accepted) sb.Append(ja ? "（弱一致）" : "(weak match)");
        return sb.ToString().Trim();
    }

    // ---- カードサムネイル（DeckOverviewForm.GetCardThumbnail と同パターン） ----

    Bitmap? GetCardThumbnail(string cardId)
    {
        if (_thumbCache.TryGetValue(cardId, out var cached)) return cached;

        var path = DeckOverviewForm.FindCardImage(cardId);
        if (path is not null)
        {
            try
            {
                using var original = Image.FromFile(path);
                return CacheThumb(cardId, original);
            }
            catch { }
        }

        // フォールバック: カードアトラスから取得
        var atlasBmp = CardAtlasService.GetCardBitmap(cardId);
        if (atlasBmp is null) return _thumbCache[cardId] = null;
        try { return CacheThumb(cardId, atlasBmp); }
        catch { return _thumbCache[cardId] = null; }
    }

    Bitmap? CacheThumb(string cardId, Image source)
    {
        var thumb = new Bitmap(CardW, CardH);
        using var tg = Graphics.FromImage(thumb);
        tg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        tg.DrawImage(source, 0, 0, CardW, CardH);
        return _thumbCache[cardId] = thumb;
    }

    // ---- クリック／ホバー ----

    void OnPictureBoxClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var hit = _hitMap.FirstOrDefault(h => h.Rect.Contains(e.Location));
        if (hit is null) return;

        var links = SiteLinkService.BuildLinks(UrlTemplateService.Load(), hit.Kind, hit.Id);
        if (links.Count == 0) return;
        if (links.Count == 1) { OpenUrl(links[0].Url); return; }

        // 複数テンプレートはメニューで選択。
        _linkMenu.Items.Clear();
        foreach (var link in links)
        {
            var url = link.Url;
            var item = new ToolStripMenuItem($"{link.Label}: {url}");
            item.Click += (_, _) => OpenUrl(url);
            _linkMenu.Items.Add(item);
        }
        _linkMenu.Show(_pictureBox, e.Location);
    }

    static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    void OnPictureBoxMouseMove(object? sender, MouseEventArgs e)
    {
        var hit = _hitMap.FirstOrDefault(h => h.Rect.Contains(e.Location));
        _pictureBox.Cursor = hit is not null ? Cursors.Hand : Cursors.Default;   // クリックでリンクを開ける示唆
        var key = hit is null ? null : $"{hit.Kind}:{hit.Id}";
        if (key == _hoveredKey) return;
        _hoveredKey = key;
        if (hit is null) { _hoverTip.Hide(_pictureBox); return; }
        _hoverTip.Show(hit.Tooltip, _pictureBox, e.X + 16, e.Y + 16);
    }
}
