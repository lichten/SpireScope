using StS2Capture.Capture;
using StS2Capture.Recognition;
using StS2Shared.Services;

namespace StS2Capture;

/// <summary>
/// 試験アプリの最小 UI。ゲーム画面を監視し、カード提示画面で検出したカードを一覧表示する。
/// </summary>
public sealed class MainForm : Form
{
    readonly CaptureLoop _loop;
    readonly OcrCardRecognizer _ocr;
    readonly TemplateCardRecognizer _template;

    readonly Label _status = new();
    readonly RadioButton _rbOcr = new() { Text = "OCR", AutoSize = true };
    readonly RadioButton _rbTemplate = new() { Text = "Template", AutoSize = true };
    readonly RadioButton _rbWgc = new() { Text = "WGC", AutoSize = true };
    readonly RadioButton _rbGdi = new() { Text = "GDI", AutoSize = true };
    readonly Button _btnCapture = new() { Text = "手動キャプチャ", AutoSize = true };
    readonly Button _btnSave = new() { Text = "キャプチャ保存", AutoSize = true };
    readonly CheckBox _cbAuto = new() { Text = "自動監視", AutoSize = true };
    readonly CheckBox _cbSaveCrops = new() { Text = "切出し/マスク保存", AutoSize = true };
    readonly ComboBox _cbCharacter = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 150,
        Margin = new Padding(8, 3, 0, 0),
    };

    // 枠色プロファイルの手動上書き候補。先頭は「自動（セーブから解決）」。
    static readonly string[] CharacterChoices =
        { "DEFECT", "SILENT", "IRONCLAD", "NECROBINDER", "REGENT" };
    readonly ListView _list = new();
    readonly ListView _ocrList = new();
    readonly PictureBox _capturePreview = new();
    readonly PictureBox _thumb = new();
    readonly SplitContainer _outer = new();
    readonly SplitContainer _left = new();
    readonly SplitContainer _right = new();

    string _lastSignature = "";
    readonly string? _portraitsDir = ResolvePortraitsDir();

    public MainForm()
    {
        Text = "StS2 Capture（試験：カード提示画面検出）";
        Width = 720;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;

        _ocr = new OcrCardRecognizer(CardNameIndex.Build());
        _template = new TemplateCardRecognizer();

        // 既定：WGC（GPU 描画対応・主）＋ OCR。
        _loop = new CaptureLoop(new WgcFrameSource(), _ocr);
        _loop.Updated += OnLoopUpdated;

        BuildLayout();
        WireEvents();

        _rbWgc.Checked = true;
        _rbOcr.Checked = true;

        UpdateStartupStatus();
    }

    void BuildLayout()
    {
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = true,
        };
        top.Controls.Add(new Label { Text = "認識:", AutoSize = true, Margin = new Padding(0, 6, 2, 0) });
        top.Controls.Add(_rbOcr);
        top.Controls.Add(_rbTemplate);
        top.Controls.Add(new Label { Text = "  取得:", AutoSize = true, Margin = new Padding(8, 6, 2, 0) });
        top.Controls.Add(_rbWgc);
        top.Controls.Add(_rbGdi);
        top.Controls.Add(_cbAuto);
        top.Controls.Add(_btnCapture);
        top.Controls.Add(_btnSave);
        top.Controls.Add(_cbSaveCrops);
        top.Controls.Add(new Label { Text = "  枠キャラ:", AutoSize = true, Margin = new Padding(8, 6, 2, 0) });
        _cbCharacter.Items.Add("自動（セーブ）");
        foreach (var c in CharacterChoices) _cbCharacter.Items.Add(c);
        _cbCharacter.SelectedIndex = 0;
        top.Controls.Add(_cbCharacter);

        _status.Dock = DockStyle.Top;
        _status.AutoSize = false;
        _status.Height = 26;
        _status.Padding = new Padding(8, 4, 8, 4);
        _status.Text = "初期化中...";

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.Columns.Add("CardId", 180);
        _list.Columns.Add("EN", 130);
        _list.Columns.Add("JP", 120);
        _list.Columns.Add("確信度", 60);
        _list.Columns.Add("認識器", 70);

        _ocrList.Dock = DockStyle.Fill;
        _ocrList.View = View.Details;
        _ocrList.FullRowSelect = true;
        _ocrList.Columns.Add("OCR テキスト", 230);
        _ocrList.Columns.Add("元", 45);
        _ocrList.Columns.Add("Matched", 150);
        _ocrList.Columns.Add("Dist", 45);

        _capturePreview.Dock = DockStyle.Fill;
        _capturePreview.SizeMode = PictureBoxSizeMode.Zoom;
        _capturePreview.BackColor = SystemColors.ControlDarkDark;

        _thumb.Dock = DockStyle.Fill;
        _thumb.SizeMode = PictureBoxSizeMode.Zoom;
        _thumb.BackColor = SystemColors.ControlLight;

        // 左：検出カード（上）＋ OCR テキスト（下）
        _left.Dock = DockStyle.Fill;
        _left.Orientation = Orientation.Horizontal;
        _left.Panel1.Controls.Add(_list);
        _left.Panel2.Controls.Add(WithHeader("OCR 検出テキスト（全行）", _ocrList));

        // 右：キャプチャプレビュー（上）＋ portrait サムネ（下）
        _right.Dock = DockStyle.Fill;
        _right.Orientation = Orientation.Horizontal;
        _right.Panel1.Controls.Add(WithHeader("キャプチャ画像（縮小プレビュー）", _capturePreview));
        _right.Panel2.Controls.Add(WithHeader("選択カードの portrait", _thumb));

        _outer.Dock = DockStyle.Fill;
        _outer.Orientation = Orientation.Vertical;
        _outer.Panel1.Controls.Add(_left);
        _outer.Panel2.Controls.Add(_right);

        Controls.Add(_outer);
        Controls.Add(_status);
        Controls.Add(top);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // フォームが実サイズを持ってから SplitterDistance を設定（早すぎると例外になるため）。
        SetSplitterDistance(_outer, (int)(_outer.Width * 0.62));
        SetSplitterDistance(_left, (int)(_left.Height * 0.45));
        SetSplitterDistance(_right, (int)(_right.Height * 0.5));
    }

    static void SetSplitterDistance(SplitContainer sc, int distance)
    {
        int extent = sc.Orientation == Orientation.Vertical ? sc.Width : sc.Height;
        int min = sc.Panel1MinSize;
        int max = extent - sc.Panel2MinSize - sc.SplitterWidth;
        if (max < min) return;
        try { sc.SplitterDistance = Math.Clamp(distance, min, max); } catch { }
    }

    /// <summary>コントロールの上に見出しラベルを付けた Panel を返す。</summary>
    static Control WithHeader(string title, Control inner)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        inner.Dock = DockStyle.Fill;
        var header = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 18,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = SystemColors.ControlDark,
            ForeColor = SystemColors.ControlLightLight,
            Padding = new Padding(4, 0, 0, 0),
        };
        panel.Controls.Add(inner);
        panel.Controls.Add(header);
        return panel;
    }

    void WireEvents()
    {
        _rbOcr.CheckedChanged += (_, _) => { if (_rbOcr.Checked) _loop.SetRecognizer(_ocr); };
        _rbTemplate.CheckedChanged += (_, _) => { if (_rbTemplate.Checked) _loop.SetRecognizer(_template); };

        _rbWgc.CheckedChanged += (_, _) => { if (_rbWgc.Checked) _loop.SetFrameSource(new WgcFrameSource()); };
        _rbGdi.CheckedChanged += (_, _) => { if (_rbGdi.Checked) _loop.SetFrameSource(new GdiFrameSource()); };

        _cbAuto.CheckedChanged += (_, _) =>
        {
            if (_cbAuto.Checked) _loop.Start();
            else _loop.Stop();
        };

        _btnCapture.Click += (_, _) => Task.Run(_loop.CaptureOnce);
        _btnSave.Click += (_, _) => SaveCapture();
        _cbSaveCrops.CheckedChanged += (_, _) =>
        {
            if (_cbSaveCrops.Checked)
            {
                var dir = Path.Combine(Path.GetTempPath(), "sts2_title_crops");
                _ocr.SaveTitleCropsDir = dir;
                _status.Text = $"タイトル切出し保存: ON → {dir}";
            }
            else _ocr.SaveTitleCropsDir = null;
        };

        _cbCharacter.SelectedIndexChanged += (_, _) =>
            _loop.CharacterOverride = _cbCharacter.SelectedIndex <= 0
                ? null
                : (string)_cbCharacter.SelectedItem!;

        _list.SelectedIndexChanged += (_, _) => UpdateThumbnail();

        FormClosed += (_, _) => _loop.Dispose();
    }

    void UpdateStartupStatus()
    {
        var game = GameWindowLocator.Find();
        var parts = new List<string>
        {
            game is null ? "ゲーム未検出" : $"ゲーム検出: {game.Value.Title}",
        };
        if (!_ocr.IsAvailable) parts.Add("（注意: OCR 言語パック未導入）");
        if (!_template.IsAvailable) parts.Add("（注意: portraits ディレクトリ未検出）");
        _status.Text = string.Join("  ", parts);
    }

    void OnLoopUpdated(CaptureLoop.Result result)
    {
        if (IsDisposed) return;
        try { BeginInvoke(() => ApplyResult(result)); }
        catch { /* フォーム破棄中 */ }
    }

    void ApplyResult(CaptureLoop.Result result)
    {
        _status.Text = result.Status;

        // キャプチャプレビューは毎回差し替え（前の画像は Dispose）。
        var oldPreview = _capturePreview.Image;
        _capturePreview.Image = result.Preview;
        oldPreview?.Dispose();

        // 内容（カード＋OCR テキスト）が変わったときだけリスト更新（チラつき防止）。
        var signature = string.Join("|",
            result.Cards.Select(c => $"{c.CardId}:{c.Confidence:F2}"))
            + "##" + string.Join("|", result.TextSpans.Select(s => s.Text));
        if (signature == _lastSignature) return;
        _lastSignature = signature;

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var c in result.Cards)
        {
            var item = new ListViewItem(c.CardId);
            item.SubItems.Add(CardDatabaseService.GetName(c.CardId, japanese: false));
            item.SubItems.Add(CardDatabaseService.GetName(c.CardId, japanese: true));
            item.SubItems.Add(c.Confidence.ToString("F2"));
            item.SubItems.Add(c.Recognizer);
            item.Tag = c.CardId;
            _list.Items.Add(item);
        }
        _list.EndUpdate();

        _ocrList.BeginUpdate();
        _ocrList.Items.Clear();
        foreach (var s in result.TextSpans)
        {
            var item = new ListViewItem(s.Text);
            item.SubItems.Add(s.Source == "title" ? "帯" : "全");
            item.SubItems.Add(s.MatchedCardId ?? "(none)");
            item.SubItems.Add(s.Distance?.ToString() ?? "-");
            if (s.MatchedCardId is not null)
                item.BackColor = s.Source == "title"
                    ? Color.FromArgb(255, 235, 200)  // タイトル帯マッチ：橙
                    : Color.FromArgb(220, 245, 220); // 全画面マッチ：緑
            else if (s.Source == "title")
                item.BackColor = Color.FromArgb(245, 245, 245); // タイトル帯（未マッチ）
            _ocrList.Items.Add(item);
        }
        _ocrList.EndUpdate();
    }

    void SaveCapture()
    {
        Task.Run(() =>
        {
            var bmp = _loop.CaptureRawFrame();
            if (IsDisposed) { bmp?.Dispose(); return; }
            try { BeginInvoke(() => PromptSave(bmp)); }
            catch { bmp?.Dispose(); }
        });
    }

    void PromptSave(Bitmap? bmp)
    {
        if (bmp is null)
        {
            _status.Text = "キャプチャ保存：失敗（ゲーム未検出またはキャプチャ不可）";
            return;
        }
        try
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "PNG 画像 (*.png)|*.png",
                FileName = $"sts2_capture_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                bmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                _status.Text = $"キャプチャ保存：{dlg.FileName}（{bmp.Width}x{bmp.Height}）";
            }
        }
        catch (Exception ex) { _status.Text = $"キャプチャ保存エラー：{ex.Message}"; }
        finally { bmp.Dispose(); }
    }

    void UpdateThumbnail()
    {
        var old = _thumb.Image;
        Image? img = null;
        if (_list.SelectedItems.Count > 0 && _list.SelectedItems[0].Tag is string cardId && _portraitsDir is not null)
        {
            var path = CardImageService.GetSourcePath(_portraitsDir, cardId);
            if (path is not null && File.Exists(path))
            {
                try { img = Image.FromFile(path); } catch { img = null; }
            }
        }
        _thumb.Image = img;
        old?.Dispose();
    }

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
