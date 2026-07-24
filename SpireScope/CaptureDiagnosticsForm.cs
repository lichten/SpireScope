using System.Diagnostics;
using StS2Capture;
using StS2Capture.Recognition;
using SpireScope.Services;
using StS2Shared.Services;

namespace SpireScope;

/// <summary>
/// ライブキャプチャの診断ウィンドウ（旧メイン画面統合 UI の切り出し）。
/// 検出結果の生リスト（カード／ショップ・OCR 候補、確信度・距離つき）とキャプチャ画像プレビューを表示し、
/// キャプチャ性能の確認に使う。CaptureLoop と Preview Bitmap の所有権は Form1 側にあり、
/// 本フォームは Form1 の ApplyResult から fan-out された参照を表示するだけ（Dispose しない）。
/// </summary>
public partial class CaptureDiagnosticsForm : Form
{
    // カード／レリッククリックで開く外部リンク（URLテンプレート）の選択メニュー。
    readonly ContextMenuStrip _linkMenu = new();

    /// <summary>検出結果リスト行に付与する、リンク生成用の種別＋ID。</summary>
    sealed record LinkTarget(string Kind, string Id);

    public CaptureDiagnosticsForm()
    {
        InitializeComponent();
        // 分割比は実サイズ確定後に設定（リスト55% / カード検出50%）。
        Shown += (_, _) =>
        {
            SetSplitterDistance(_outer, (int)(_outer.Width * 0.55));
            SetSplitterDistance(_left, (int)(_left.Height * 0.5));
        };
        // 検出結果リスト（カード／ショップ）左クリックで情報ページリンクを開く。
        _list.MouseClick += OnListItemClick;
        _ocrList.MouseClick += OnListItemClick;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Preview Bitmap の所有権は Form1（Dispose すると Form1 側の表示・次回差し替えが壊れる）。
            _capturePreview.Image = null;
            _linkMenu.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// ステータス行とキャプチャ画像プレビューを更新する（参照差し替えのみ）。
    /// Form1 が旧 Preview を Dispose するため、非表示（Hide）中も毎フレーム呼んで参照を最新に保つこと。
    /// </summary>
    public void UpdatePreview(CaptureLoop.Result result)
    {
        _diagStatus.Text = result.Status;
        _capturePreview.Image = result.Preview;   // 旧画像の Dispose は Form1 が行う
    }

    /// <summary>検出結果リスト（カード行／ショップ・OCR 候補行）を再構築する。</summary>
    public void UpdateLists(CaptureLoop.Result result)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var c in result.Cards)
        {
            var item = new ListViewItem(c.CardId);
            item.SubItems.Add(CardDatabaseService.GetName(c.CardId, japanese: false));
            item.SubItems.Add(CardDatabaseService.GetName(c.CardId, japanese: true));
            item.SubItems.Add(c.Confidence.ToString("F2"));
            item.SubItems.Add(c.Recognizer);
            item.SubItems.Add(TierScoreService.FormatCardTier(c.CardId));
            item.Tag = new LinkTarget("card", c.CardId);
            _list.Items.Add(item);
        }
        _list.EndUpdate();

        // ショップ・エンシェントレリック選択はどちらもレリック/ポーション行（result.Shop.Items）を持つ。
        bool hasShopItems = result.Shop is { Items.Count: > 0 };
        _ocrHeaderLabel.Text = result.Screen == ScreenRecognizer.ScreenType.AncientSelect
            ? "エンシェントレリック候補"
            : "ショップ候補（レリック／ポーション）";
        _ocrList.BeginUpdate();
        _ocrList.Items.Clear();
        if (hasShopItems)
            foreach (var lvi in BuildShopRows(result.Shop!)) _ocrList.Items.Add(lvi);
        else
            foreach (var lvi in BuildOcrRows(result.TextSpans)) _ocrList.Items.Add(lvi);
        _ocrList.EndUpdate();
    }

    static IEnumerable<ListViewItem> BuildOcrRows(IReadOnlyList<OcrTextSpan> spans)
    {
        foreach (var s in spans)
        {
            var item = new ListViewItem(s.Text);
            item.SubItems.Add(s.Source == "title" ? "帯" : "全");
            item.SubItems.Add(s.MatchedCardId ?? "(none)");
            item.SubItems.Add(s.Distance?.ToString() ?? "-");
            item.SubItems.Add(s.MatchedCardId is null ? "" : TierScoreService.FormatCardTier(s.MatchedCardId));
            if (s.MatchedCardId is not null) item.Tag = new LinkTarget("card", s.MatchedCardId);
            if (s.MatchedCardId is not null)
                item.BackColor = s.Source == "title"
                    ? Color.FromArgb(255, 235, 200)
                    : Color.FromArgb(220, 245, 220);
            else if (s.Source == "title")
                item.BackColor = Color.FromArgb(245, 245, 245);
            yield return item;
        }
    }

    static IEnumerable<ListViewItem> BuildShopRows(ShopItemRecognizer.Result res)
    {
        foreach (var it in res.Items)
        {
            var label = it.Candidates.Count == 0
                ? "(no match)"
                : string.Join(" / ", it.Candidates.Select(c => $"{c.Name} [{c.Id}]"));
            var dist = it.Candidates.Count == 0
                ? "-"
                : string.Join(" / ", it.Candidates.Select(c => c.Distance.ToString("F2")));
            var lvi = new ListViewItem(label);
            lvi.SubItems.Add(it.Kind == ShopItemRecognizer.Kind.Relic ? "R" : "P");
            lvi.SubItems.Add(it.Accepted ? (it.Candidates.Count > 1 ? $"OK×{it.Candidates.Count}" : "OK") : "-");
            lvi.SubItems.Add(dist);
            lvi.SubItems.Add(it.Candidates.Count == 0 ? "" : string.Join(" / ",
                it.Candidates.Select(c => it.Kind == ShopItemRecognizer.Kind.Relic
                    ? TierScoreService.FormatRelicTier(c.Id)
                    : TierScoreService.FormatPotionTier(c.Id))));
            if (it.Candidates.Count > 0)
                lvi.Tag = new LinkTarget(
                    it.Kind == ShopItemRecognizer.Kind.Relic ? "relic" : "potion",
                    it.Candidates[0].Id);
            if (it.Accepted)
                lvi.BackColor = it.Kind == ShopItemRecognizer.Kind.Relic
                    ? Color.FromArgb(220, 245, 220) : Color.FromArgb(255, 235, 200);
            yield return lvi;
        }
    }

    void OnListItemClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (sender is not ListView lv) return;
        if (lv.HitTest(e.Location).Item is not { Tag: LinkTarget target }) return;

        var links = SiteLinkService.BuildLinks(UrlTemplateService.Load(), target.Kind, target.Id);
        if (links.Count == 0) return;
        if (links.Count == 1) { OpenUrl(links[0].Url); return; }

        // 複数テンプレートはクリック位置のメニューで選択。
        _linkMenu.Items.Clear();
        foreach (var link in links)
        {
            var url = link.Url;
            var item = new ToolStripMenuItem($"{link.Label}: {url}");
            item.Click += (_, _) => OpenUrl(url);
            _linkMenu.Items.Add(item);
        }
        _linkMenu.Show(lv, e.Location);
    }

    void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { _diagStatus.Text = $"リンク起動エラー：{ex.Message}"; }
    }

    static void SetSplitterDistance(SplitContainer sc, int distance)
    {
        int extent = sc.Orientation == Orientation.Vertical ? sc.Width : sc.Height;
        int min = sc.Panel1MinSize;
        int max = extent - sc.Panel2MinSize - sc.SplitterWidth;
        if (max < min) return;
        try { sc.SplitterDistance = Math.Clamp(distance, min, max); } catch { }
    }
}
