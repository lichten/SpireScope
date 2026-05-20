using System.Diagnostics;
using Microsoft.Web.WebView2.Core;

public partial class MainForm : Form
{
    private string _currentFilePath = "";
    private string _savedReviewContent = "";
    private bool   _isDirty;

    public MainForm()
    {
        InitializeComponent();
        _buildButton.Click += BuildButton_Click;
        _openDistButton.Click += (_, _) =>
        {
            var d = SiteBuilderCore.GetDistDir();
            if (Directory.Exists(d)) Process.Start("explorer.exe", d);
        };
        _saveReviewButton.Click += SaveReview_Click;
        _revertReviewButton.Click += (_, _) => RevertReview();
        _changelogAddButton.Click += ChangelogAddButton_Click;
    }

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
            _tabControl.SelectedTab = _tabPreview;
            if (_webView2.CoreWebView2 != null && !string.IsNullOrEmpty(_currentFilePath))
                _webView2.CoreWebView2.Reload();
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

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        await _webView2.EnsureCoreWebView2Async(null);
        _webView2.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
        var indexPath = Path.Combine(SiteBuilderCore.GetDistDir(), "index.html");
        _webView2.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;
        var uri = _webView2.Source;
        if (uri == null || uri.Scheme != "file")
        {
            SetReviewPanel(null);
            return;
        }
        var filePath = uri.LocalPath;
        SetReviewPanel(filePath);
    }

    private void SetReviewPanel(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _previewSplit.Panel2Collapsed = true;
            _currentFilePath = "";
            return;
        }

        var distDir       = Path.GetFullPath(SiteBuilderCore.GetDistDir());
        var changelogPath = Path.Combine(distDir, "changelog.html");
        if (string.Equals(Path.GetFullPath(filePath), changelogPath, StringComparison.OrdinalIgnoreCase))
        {
            _currentFilePath        = "";
            _reviewPanel.Visible    = false;
            _changelogPanel.Visible = true;
            _previewSplit.Panel2Collapsed = false;
            return;
        }

        var content = SiteBuilderCore.ExtractReviewPublic(filePath);
        if (content == null)
        {
            _previewSplit.Panel2Collapsed = true;
            _currentFilePath = "";
            return;
        }
        _changelogPanel.Visible = false;
        _reviewPanel.Visible    = true;
        _currentFilePath = filePath;
        _savedReviewContent = content;
        _isDirty = false;
        _reviewEditor.TextChanged -= ReviewEditor_TextChanged;
        _reviewEditor.Text = content.Replace("\r\n", "\n").Replace("\n", "\r\n");
        _reviewEditor.TextChanged += ReviewEditor_TextChanged;
        _reviewLabel.Text = $"レビュー編集: {Path.GetFileName(filePath)}";
        _previewSplit.Panel2Collapsed = false;
        UpdateReviewButtons();
    }

    private void ChangelogAddButton_Click(object? sender, EventArgs e)
    {
        var text = _changelogEditor.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        SiteBuilderCore.AppendManualChangelogEntry(text);
        _changelogEditor.Clear();
        _statusLabel.Text = "更新履歴に追加しました";
        _webView2.CoreWebView2?.Reload();
    }

    private void ReviewEditor_TextChanged(object? sender, EventArgs e) => MarkDirty();

    private void MarkDirty()
    {
        if (_isDirty) return;
        _isDirty = true;
        UpdateReviewButtons();
    }

    private void UpdateReviewButtons()
    {
        _saveReviewButton.Enabled   = _isDirty;
        _revertReviewButton.Enabled = _isDirty;
    }

    private void SaveReview_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath)) return;
        SiteBuilderCore.SaveReview(_currentFilePath, _reviewEditor.Text.Replace("\r\n", "\n"));
        SiteBuilderCore.AppendChangelogEntry(_currentFilePath);
        _savedReviewContent = _reviewEditor.Text;
        _isDirty = false;
        UpdateReviewButtons();
        _statusLabel.Text = "保存しました";
        _webView2.CoreWebView2?.Reload();
    }

    private void RevertReview()
    {
        _reviewEditor.TextChanged -= ReviewEditor_TextChanged;
        _reviewEditor.Text = _savedReviewContent;
        _reviewEditor.TextChanged += ReviewEditor_TextChanged;
        _isDirty = false;
        UpdateReviewButtons();
    }
}
