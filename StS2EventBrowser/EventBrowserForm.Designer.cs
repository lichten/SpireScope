namespace StS2EventBrowser;

partial class EventBrowserForm
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
        _topPanel           = new Panel();
        _filterLabel        = new Label();
        _filterBox          = new TextBox();
        _btnJp              = new Button();
        _btnEn              = new Button();
        _statusLabel        = new Label();
        _statusBar          = new Panel();
        _countLabel         = new Label();
        _tabControl         = new TabControl();
        _eventTab           = new TabPage();
        _mainSplit          = new SplitContainer();
        _eventList          = new ListBox();
        _detailSplit        = new SplitContainer();
        _pictureBox         = new PictureBox();
        _textBox            = new RichTextBox();
        _ancientTab         = new TabPage();
        _ancientSplit       = new SplitContainer();
        _ancientList        = new ListBox();
        _ancientDetailSplit = new SplitContainer();
        _ancientPictureBox  = new PictureBox();
        _ancientTextBox     = new RichTextBox();

        _topPanel.SuspendLayout();
        _statusBar.SuspendLayout();
        _tabControl.SuspendLayout();
        _eventTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_mainSplit).BeginInit();
        _mainSplit.Panel1.SuspendLayout();
        _mainSplit.Panel2.SuspendLayout();
        _mainSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_detailSplit).BeginInit();
        _detailSplit.Panel1.SuspendLayout();
        _detailSplit.Panel2.SuspendLayout();
        _detailSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
        _ancientTab.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_ancientSplit).BeginInit();
        _ancientSplit.Panel1.SuspendLayout();
        _ancientSplit.Panel2.SuspendLayout();
        _ancientSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_ancientDetailSplit).BeginInit();
        _ancientDetailSplit.Panel1.SuspendLayout();
        _ancientDetailSplit.Panel2.SuspendLayout();
        _ancientDetailSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_ancientPictureBox).BeginInit();
        SuspendLayout();

        // _topPanel
        _topPanel.Dock = DockStyle.Top;
        _topPanel.Height = 36;
        _topPanel.Controls.AddRange(new Control[] { _filterLabel, _filterBox, _btnJp, _btnEn, _statusLabel });

        // _filterLabel
        _filterLabel.AutoSize = true;
        _filterLabel.Location = new Point(8, 9);
        _filterLabel.Text = "検索:";

        // _filterBox
        _filterBox.Location = new Point(48, 6);
        _filterBox.PlaceholderText = "イベント名でフィルタ...";
        _filterBox.Size = new Size(260, 23);

        // _btnJp
        _btnJp.FlatStyle = FlatStyle.Flat;
        _btnJp.Location = new Point(318, 5);
        _btnJp.Size = new Size(40, 26);
        _btnJp.Text = "JP";

        // _btnEn
        _btnEn.FlatStyle = FlatStyle.Flat;
        _btnEn.Location = new Point(362, 5);
        _btnEn.Size = new Size(40, 26);
        _btnEn.Text = "EN";

        // _statusLabel
        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = SystemColors.GrayText;
        _statusLabel.Location = new Point(420, 9);
        _statusLabel.Size = new Size(300, 15);

        // _statusBar
        _statusBar.BackColor = SystemColors.ControlLight;
        _statusBar.Dock = DockStyle.Bottom;
        _statusBar.Height = 24;
        _statusBar.Controls.Add(_countLabel);

        // _countLabel
        _countLabel.AutoSize = true;
        _countLabel.Font = new Font("Segoe UI", 8f);
        _countLabel.ForeColor = SystemColors.GrayText;
        _countLabel.Location = new Point(4, 4);

        // _tabControl
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.TabPages.AddRange(new TabPage[] { _eventTab, _ancientTab });

        // _eventTab
        _eventTab.Padding = new Padding(0);
        _eventTab.Text = "イベント";
        _eventTab.Controls.Add(_mainSplit);

        // _mainSplit
        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.FixedPanel = FixedPanel.Panel1;
        _mainSplit.SplitterDistance = 260;
        _mainSplit.Panel1.Controls.Add(_eventList);
        _mainSplit.Panel2.Controls.Add(_detailSplit);

        // _eventList
        _eventList.Dock = DockStyle.Fill;
        _eventList.DrawMode = DrawMode.OwnerDrawFixed;
        _eventList.Font = new Font("Segoe UI", 9.5f);
        _eventList.IntegralHeight = false;

        // _detailSplit
        _detailSplit.Dock = DockStyle.Fill;
        _detailSplit.Orientation = Orientation.Horizontal;
        _detailSplit.SplitterDistance = 320;
        _detailSplit.Panel1.Controls.Add(_pictureBox);
        _detailSplit.Panel2.Controls.Add(_textBox);

        // _pictureBox
        _pictureBox.BackColor = Color.FromArgb(24, 24, 28);
        _pictureBox.Dock = DockStyle.Fill;
        _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        // _textBox
        _textBox.BackColor = SystemColors.Window;
        _textBox.BorderStyle = BorderStyle.None;
        _textBox.Dock = DockStyle.Fill;
        _textBox.Font = new Font("Segoe UI", 9.5f);
        _textBox.ReadOnly = true;
        _textBox.ScrollBars = RichTextBoxScrollBars.Vertical;

        // _ancientTab
        _ancientTab.Padding = new Padding(0);
        _ancientTab.Text = "Ancient";
        _ancientTab.Controls.Add(_ancientSplit);

        // _ancientSplit
        _ancientSplit.Dock = DockStyle.Fill;
        _ancientSplit.FixedPanel = FixedPanel.Panel1;
        _ancientSplit.SplitterDistance = 280;
        _ancientSplit.Panel1.Controls.Add(_ancientList);
        _ancientSplit.Panel2.Controls.Add(_ancientDetailSplit);

        // _ancientList
        _ancientList.Dock = DockStyle.Fill;
        _ancientList.Font = new Font("Segoe UI", 9.5f);
        _ancientList.IntegralHeight = false;

        // _ancientDetailSplit
        _ancientDetailSplit.Dock = DockStyle.Fill;
        _ancientDetailSplit.Orientation = Orientation.Horizontal;
        _ancientDetailSplit.SplitterDistance = 320;
        _ancientDetailSplit.Panel1.Controls.Add(_ancientPictureBox);
        _ancientDetailSplit.Panel2.Controls.Add(_ancientTextBox);

        // _ancientPictureBox
        _ancientPictureBox.BackColor = Color.FromArgb(24, 24, 28);
        _ancientPictureBox.Dock = DockStyle.Fill;
        _ancientPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        // _ancientTextBox
        _ancientTextBox.BackColor = SystemColors.Window;
        _ancientTextBox.BorderStyle = BorderStyle.None;
        _ancientTextBox.Dock = DockStyle.Fill;
        _ancientTextBox.Font = new Font("Segoe UI", 9.5f);
        _ancientTextBox.ReadOnly = true;
        _ancientTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;

        // Form
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(900, 600);
        Name = "EventBrowserForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "StS2 Event Browser";
        Controls.Add(_tabControl);
        Controls.Add(_statusBar);
        Controls.Add(_topPanel);

        _topPanel.ResumeLayout(false);
        _topPanel.PerformLayout();
        _statusBar.ResumeLayout(false);
        _statusBar.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
        _detailSplit.Panel1.ResumeLayout(false);
        _detailSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_detailSplit).EndInit();
        _detailSplit.ResumeLayout(false);
        _mainSplit.Panel1.ResumeLayout(false);
        _mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_mainSplit).EndInit();
        _mainSplit.ResumeLayout(false);
        _eventTab.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_ancientPictureBox).EndInit();
        _ancientDetailSplit.Panel1.ResumeLayout(false);
        _ancientDetailSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_ancientDetailSplit).EndInit();
        _ancientDetailSplit.ResumeLayout(false);
        _ancientSplit.Panel1.ResumeLayout(false);
        _ancientSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_ancientSplit).EndInit();
        _ancientSplit.ResumeLayout(false);
        _ancientTab.ResumeLayout(false);
        _tabControl.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    // ── Control fields ────────────────────────────────────────────────────────────
    private Panel          _topPanel;
    private Label          _filterLabel;
    private TextBox        _filterBox;
    private Button         _btnJp;
    private Button         _btnEn;
    private Label          _statusLabel;
    private Panel          _statusBar;
    private Label          _countLabel;
    private TabControl     _tabControl;
    private TabPage        _eventTab;
    private SplitContainer _mainSplit;
    private ListBox        _eventList;
    private SplitContainer _detailSplit;
    private PictureBox     _pictureBox;
    private RichTextBox    _textBox;
    private TabPage        _ancientTab;
    private SplitContainer _ancientSplit;
    private ListBox        _ancientList;
    private SplitContainer _ancientDetailSplit;
    private PictureBox     _ancientPictureBox;
    private RichTextBox    _ancientTextBox;
}
