using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

public class MainForm : Form
{
    // ── Build tab ────────────────────────────────────────────────────────────────
    private readonly Button  _buildButton;
    private readonly Button  _openDistButton;
    private readonly TextBox _logBox;

    // ── Preview tab ───────────────────────────────────────────────────────────────
    private readonly WebView2        _webView;
    private readonly Button          _backButton;
    private readonly Button          _forwardButton;
    private readonly Button          _homeButton;
    private readonly Button          _reloadButton;
    private readonly SplitContainer  _previewSplit;
    private readonly Panel           _reviewPanel;
    private readonly TextBox         _reviewEditor;
    private readonly Button          _saveReviewButton;
    private readonly Button          _revertReviewButton;
    private readonly Label           _reviewLabel;

    // ── Shared ───────────────────────────────────────────────────────────────────
    private readonly TabControl            _tabs;
    private readonly ToolStripStatusLabel  _statusLabel;
    private readonly StatusStrip           _statusStrip;

    private string? _currentFilePath;
    private string  _savedReviewContent = "";
    private bool    _isDirty;
    private bool    _webViewReady;
    private bool    _webViewInitializing;

    public MainForm()
    {
        Text          = "StS2 Site Builder";
        Width         = 1100;
        Height        = 720;
        MinimumSize   = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;

        // ── StatusStrip ──────────────────────────────────────────────────────────
        _statusLabel = new ToolStripStatusLabel("準備完了") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip = new StatusStrip();
        _statusStrip.Items.Add(_statusLabel);

        // ── TabControl ───────────────────────────────────────────────────────────
        _tabs = new TabControl { Dock = DockStyle.Fill };

        // ═══ ビルドタブ ══════════════════════════════════════════════════════════
        var buildTab = new TabPage("ビルド");

        var buildToolbar = new FlowLayoutPanel
        {
            Dock    = DockStyle.Top,
            Height  = 44,
            Padding = new Padding(8, 6, 8, 6),
        };

        _buildButton = new Button { Text = "サイト生成", AutoSize = true, Padding = new Padding(12, 4, 12, 4) };
        _buildButton.Click += BuildButton_Click;

        _openDistButton = new Button { Text = "dist を開く", AutoSize = true, Padding = new Padding(12, 4, 12, 4) };
        _openDistButton.Click += (_, _) =>
        {
            var d = SiteBuilderCore.GetDistDir();
            if (Directory.Exists(d)) Process.Start("explorer.exe", d);
        };

        buildToolbar.Controls.AddRange([_buildButton, _openDistButton]);

        _logBox = new TextBox
        {
            Dock       = DockStyle.Fill,
            Multiline  = true,
            ReadOnly   = true,
            ScrollBars = ScrollBars.Vertical,
            Font       = new Font("Consolas", 9f),
            BackColor  = Color.FromArgb(30, 30, 30),
            ForeColor  = Color.FromArgb(220, 220, 220),
        };

        buildTab.Controls.Add(_logBox);
        buildTab.Controls.Add(buildToolbar);

        // ═══ プレビュータブ ═══════════════════════════════════════════════════════
        var previewTab = new TabPage("プレビュー");

        var navBar = new FlowLayoutPanel
        {
            Dock    = DockStyle.Top,
            Height  = 38,
            Padding = new Padding(6, 4, 6, 4),
        };

        _backButton    = NavButton("◀", "戻る");
        _forwardButton = NavButton("▶", "進む");
        _homeButton    = NavButton("⌂", "トップ");
        _reloadButton  = NavButton("↻", "再読込");

        _backButton.Click    += (_, _) => { if (_webViewReady) _webView.GoBack();    };
        _forwardButton.Click += (_, _) => { if (_webViewReady) _webView.GoForward(); };
        _homeButton.Click    += (_, _) => { if (_webViewReady) NavigateToIndex(); };
        _reloadButton.Click  += (_, _) => { if (_webViewReady) _webView.Reload();    };

        navBar.Controls.AddRange([_backButton, _forwardButton, _homeButton, _reloadButton]);

        // WebView2
        _webView = new WebView2 { Dock = DockStyle.Fill };

        // レビューパネル（下ペイン）
        _reviewLabel = new Label
        {
            Text      = "レビュー編集（保存してもビルド不要）",
            Dock      = DockStyle.Top,
            Height    = 22,
            Font      = new Font(Font.FontFamily, 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Padding   = new Padding(4, 4, 0, 0),
        };

        _reviewEditor = new TextBox
        {
            Dock        = DockStyle.Fill,
            Multiline   = true,
            ScrollBars  = ScrollBars.Both,
            Font        = new Font("Consolas", 9.5f),
            WordWrap    = false,
            AcceptsTab  = true,
        };
        _reviewEditor.TextChanged += (_, _) => MarkDirty();

        var reviewButtons = new FlowLayoutPanel
        {
            Dock    = DockStyle.Bottom,
            Height  = 34,
            Padding = new Padding(4, 3, 4, 3),
        };

        _saveReviewButton   = new Button { Text = "保存",     AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
        _revertReviewButton = new Button { Text = "元に戻す", AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
        _saveReviewButton.Click   += SaveReview_Click;
        _revertReviewButton.Click += (_, _) => RevertReview();

        reviewButtons.Controls.AddRange([_saveReviewButton, _revertReviewButton]);

        _reviewPanel = new Panel { Dock = DockStyle.Fill };
        _reviewPanel.Controls.Add(_reviewEditor);
        _reviewPanel.Controls.Add(reviewButtons);
        _reviewPanel.Controls.Add(_reviewLabel);

        _previewSplit = new SplitContainer
        {
            Dock           = DockStyle.Fill,
            Orientation    = Orientation.Horizontal,
            SplitterDistance = 460,
            Panel2MinSize  = 120,
        };
        _previewSplit.Panel1.Controls.Add(_webView);
        _previewSplit.Panel2.Controls.Add(_reviewPanel);
        _previewSplit.Panel2Collapsed = true;

        previewTab.Controls.Add(_previewSplit);
        previewTab.Controls.Add(navBar);

        _tabs.TabPages.AddRange([buildTab, previewTab]);
        _tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;

        Controls.Add(_tabs);
        Controls.Add(_statusStrip);
    }

    // ── Build tab logic ───────────────────────────────────────────────────────────

    private async void BuildButton_Click(object? sender, EventArgs e)
    {
        _buildButton.Enabled    = false;
        _openDistButton.Enabled = false;
        _logBox.Clear();
        _statusLabel.Text = "生成中...";
        try
        {
            var distDir = SiteBuilderCore.GetDistDir();
            await Task.Run(() => SiteBuilderCore.Build(distDir, AppendLog));
            _statusLabel.Text = "完了";

            // プレビュータブが開いていれば現在のページをリロード
            if (_tabs.SelectedIndex == 1 && _webViewReady)
                _webView.Reload();
        }
        catch (Exception ex)
        {
            AppendLog($"エラー: {ex.Message}");
            _statusLabel.Text = "エラー";
        }
        finally
        {
            _buildButton.Enabled    = true;
            _openDistButton.Enabled = true;
        }
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(message)); return; }
        _logBox.AppendText(message + Environment.NewLine);
    }

    // ── Preview tab logic ─────────────────────────────────────────────────────────

    private void Tabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_tabs.SelectedIndex != 1) return;

        // 初回タブ選択時のみ初期化（この時点でコントロールの HWND が確実に作成されている）
        if (!_webViewReady && !_webViewInitializing)
        {
            _webViewInitializing = true;
            _ = InitWebViewAsync();
        }
    }

    private async Task InitWebViewAsync()
    {
        _statusLabel.Text = "WebView2 初期化中...";
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webViewReady = true;

            _webView.CoreWebView2.NewWindowRequested  += WebView_NewWindowRequested;
            _webView.CoreWebView2.NavigationStarting  += WebView_NavigationStarting;
            _webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            _webView.CoreWebView2.SourceChanged       += (_, _) => UpdateNavButtons();

            var distDir = SiteBuilderCore.GetDistDir();
            if (Directory.Exists(distDir))
                NavigateToIndex();
            else
                ShowNoDistMessage();
        }
        catch (Exception ex)
        {
            _webViewInitializing = false;
            ShowWebViewError(ex.Message);
        }
    }

    private void NavigateToIndex()
    {
        var distDir = SiteBuilderCore.GetDistDir();
        var index   = Path.Combine(distDir, "index.html");
        if (!File.Exists(index))
        {
            _statusLabel.Text = $"index.html が見つかりません: {index}";
            return;
        }
        // Source プロパティ経由ではなく CoreWebView2.Navigate() で直接指定する
        var fileUri = new Uri(index).AbsoluteUri;   // → "file:///C:/..."
        _webView.CoreWebView2.Navigate(fileUri);
    }

    private void WebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // 新しいウィンドウを開こうとしたらデフォルトブラウザで開く
        e.Handled = true;
        Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
    }

    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // dist 外の URL はデフォルトブラウザで開く
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme == "file")
        {
            _statusLabel.Text = $"読込中: {uri.LocalPath}";
            return;
        }

        e.Cancel = true;
        Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _statusLabel.Text = $"ナビゲーション失敗 (WebErrorStatus={e.WebErrorStatus})";
            SetReviewPanel(null);
            return;
        }

        if (_isDirty && !ConfirmLeave()) return;

        UpdateNavButtons();

        var sourceStr = _webView.CoreWebView2.Source;
        if (!Uri.TryCreate(sourceStr, UriKind.Absolute, out var uri) || uri.Scheme != "file")
        {
            SetReviewPanel(null);
            return;
        }

        var filePath = Uri.UnescapeDataString(uri.LocalPath);
        SetReviewPanel(filePath);
    }

    private void SetReviewPanel(string? filePath)
    {
        _currentFilePath = filePath;

        if (filePath is null || !File.Exists(filePath))
        {
            _previewSplit.Panel2Collapsed = true;
            _statusLabel.Text = "";
            return;
        }

        var fileContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        if (!fileContent.Contains("<!-- REVIEW_START -->"))
        {
            _previewSplit.Panel2Collapsed = true;
            _statusLabel.Text = "";
            return;
        }

        const string START = "<!-- REVIEW_START -->";
        const string END   = "<!-- REVIEW_END -->";
        var s   = fileContent.IndexOf(START, StringComparison.Ordinal);
        var e   = fileContent.IndexOf(END,   StringComparison.Ordinal);
        var raw = (s >= 0 && e > s) ? fileContent[(s + START.Length)..e] : "";

        // テンプレートコメントのみの場合は空扱い
        var trimmed = raw.Trim();
        var text    = (trimmed.StartsWith("<!--") && trimmed.EndsWith("-->")) ? "" : raw;

        _savedReviewContent = text;
        _reviewEditor.TextChanged -= ReviewEditorChanged_ForDirty;
        _reviewEditor.Text = text;
        _reviewEditor.TextChanged += ReviewEditorChanged_ForDirty;
        _isDirty = false;
        UpdateReviewButtons();

        _previewSplit.Panel2Collapsed = false;
        _statusLabel.Text = "レビュー編集可";
    }

    private void SaveReview_Click(object? sender, EventArgs e)
    {
        if (_currentFilePath is null) return;
        try
        {
            SiteBuilderCore.SaveReview(_currentFilePath, _reviewEditor.Text);
            _savedReviewContent = _reviewEditor.Text;
            _isDirty = false;
            UpdateReviewButtons();
            _statusLabel.Text = "保存しました";
            _webView.Reload();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存エラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RevertReview()
    {
        _reviewEditor.TextChanged -= ReviewEditorChanged_ForDirty;
        _reviewEditor.Text = _savedReviewContent;
        _reviewEditor.TextChanged += ReviewEditorChanged_ForDirty;
        _isDirty = false;
        UpdateReviewButtons();
    }

    private void MarkDirty() { _isDirty = true; UpdateReviewButtons(); }

    private void ReviewEditorChanged_ForDirty(object? sender, EventArgs e) => MarkDirty();

    private void UpdateReviewButtons()
    {
        _saveReviewButton.Enabled   = _isDirty;
        _revertReviewButton.Enabled = _isDirty;
    }

    private void UpdateNavButtons()
    {
        _backButton.Enabled    = _webView.CanGoBack;
        _forwardButton.Enabled = _webView.CanGoForward;
    }

    private bool ConfirmLeave()
    {
        if (!_isDirty) return true;
        var r = MessageBox.Show("未保存のレビューがあります。ページを離れますか？",
            "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r == DialogResult.Yes) { _isDirty = false; return true; }
        return false;
    }

    private void ShowNoDistMessage()
    {
        _statusLabel.Text = "dist フォルダがありません。先にビルドを実行してください。";
    }

    private void ShowWebViewError(string message)
    {
        _statusLabel.Text = $"WebView2 初期化エラー: {message}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static Button NavButton(string text, string tooltip)
    {
        var btn = new Button
        {
            Text    = text,
            Width   = 36,
            Height  = 26,
            Padding = new Padding(0),
            Font    = new Font("Segoe UI", 10f),
        };
        new ToolTip().SetToolTip(btn, tooltip);
        return btn;
    }
}
