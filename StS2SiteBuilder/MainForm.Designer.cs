partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _statusStrip    = new StatusStrip();
        _statusLabel    = new ToolStripStatusLabel();
        _toolbar        = new FlowLayoutPanel();
        _buildButton    = new Button();
        _openDistButton = new Button();
        _logBox         = new TextBox();

        _statusStrip.SuspendLayout();
        _toolbar.SuspendLayout();
        SuspendLayout();

        // _statusLabel
        _statusLabel.Spring    = true;
        _statusLabel.Text      = "準備完了";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        // _statusStrip
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel });

        // _toolbar
        _toolbar.Dock    = DockStyle.Top;
        _toolbar.Height  = 44;
        _toolbar.Padding = new Padding(8, 6, 8, 6);
        _toolbar.Controls.AddRange(new Control[] { _buildButton, _openDistButton });

        // _buildButton
        _buildButton.AutoSize = true;
        _buildButton.Padding  = new Padding(12, 4, 12, 4);
        _buildButton.Text     = "サイト生成";

        // _openDistButton
        _openDistButton.AutoSize = true;
        _openDistButton.Padding  = new Padding(12, 4, 12, 4);
        _openDistButton.Text     = "dist を開く";

        // _logBox
        _logBox.BackColor  = Color.FromArgb(30, 30, 30);
        _logBox.Dock       = DockStyle.Fill;
        _logBox.Font       = new Font("Consolas", 9f);
        _logBox.ForeColor  = Color.FromArgb(220, 220, 220);
        _logBox.Multiline  = true;
        _logBox.ReadOnly   = true;
        _logBox.ScrollBars = ScrollBars.Vertical;

        // Form
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode       = AutoScaleMode.Font;
        ClientSize          = new Size(900, 600);
        MinimumSize         = new Size(600, 400);
        Name                = "MainForm";
        StartPosition       = FormStartPosition.CenterScreen;
        Text                = "StS2 Site Builder";
        Controls.Add(_logBox);
        Controls.Add(_toolbar);
        Controls.Add(_statusStrip);

        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        _toolbar.ResumeLayout(false);
        _toolbar.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private FlowLayoutPanel      _toolbar;
    private Button               _buildButton;
    private Button               _openDistButton;
    private TextBox              _logBox;
    private StatusStrip          _statusStrip;
    private ToolStripStatusLabel _statusLabel;
}
