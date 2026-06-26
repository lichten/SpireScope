namespace StS2Toys
{
    partial class LiveCaptureForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _top = new FlowLayoutPanel();
            _radioGroup = new FlowLayoutPanel();
            _lblCapture = new Label();
            _rbWgc = new RadioButton();
            _rbGdi = new RadioButton();
            _cbAuto = new CheckBox();
            _btnCapture = new Button();
            _btnLinks = new Button();
            _lblCharacter = new Label();
            _cbCharacter = new ComboBox();
            _status = new Label();
            _mainSplit = new SplitContainer();
            _statePanel = new Panel();
            _relicHeaderPanel = new Panel();
            _relicList = new ListView();
            _colRelic = new ColumnHeader();
            _colFloor = new ColumnHeader();
            _relicHeaderLabel = new Label();
            _stateText = new Label();
            _stateHeaderLabel = new Label();
            _outer = new SplitContainer();
            _left = new SplitContainer();
            _list = new ListView();
            _colCardId = new ColumnHeader();
            _colEn = new ColumnHeader();
            _colJp = new ColumnHeader();
            _colConf = new ColumnHeader();
            _colRecog = new ColumnHeader();
            _ocrHeaderPanel = new Panel();
            _ocrList = new ListView();
            _colOcrText = new ColumnHeader();
            _colOcrKind = new ColumnHeader();
            _colOcrMatch = new ColumnHeader();
            _colOcrDist = new ColumnHeader();
            _ocrHeaderLabel = new Label();
            _right = new SplitContainer();
            _previewHeaderPanel = new Panel();
            _capturePreview = new PictureBox();
            _previewHeaderLabel = new Label();
            _thumbHeaderPanel = new Panel();
            _thumb = new PictureBox();
            _thumbHeaderLabel = new Label();
            _top.SuspendLayout();
            _radioGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_mainSplit).BeginInit();
            _mainSplit.Panel1.SuspendLayout();
            _mainSplit.Panel2.SuspendLayout();
            _mainSplit.SuspendLayout();
            _statePanel.SuspendLayout();
            _relicHeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_outer).BeginInit();
            _outer.Panel1.SuspendLayout();
            _outer.Panel2.SuspendLayout();
            _outer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_left).BeginInit();
            _left.Panel1.SuspendLayout();
            _left.Panel2.SuspendLayout();
            _left.SuspendLayout();
            _ocrHeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_right).BeginInit();
            _right.Panel1.SuspendLayout();
            _right.Panel2.SuspendLayout();
            _right.SuspendLayout();
            _previewHeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_capturePreview).BeginInit();
            _thumbHeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_thumb).BeginInit();
            SuspendLayout();
            // 
            // _top
            // 
            _top.AutoSize = true;
            _top.Controls.Add(_radioGroup);
            _top.Controls.Add(_cbAuto);
            _top.Controls.Add(_btnCapture);
            _top.Controls.Add(_btnLinks);
            _top.Controls.Add(_lblCharacter);
            _top.Controls.Add(_cbCharacter);
            _top.Dock = DockStyle.Top;
            _top.Location = new Point(0, 0);
            _top.Name = "_top";
            _top.Padding = new Padding(8, 8, 8, 4);
            _top.Size = new Size(1119, 53);
            _top.TabIndex = 2;
            // 
            // _radioGroup
            // 
            _radioGroup.AutoSize = true;
            _radioGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _radioGroup.Controls.Add(_lblCapture);
            _radioGroup.Controls.Add(_rbWgc);
            _radioGroup.Controls.Add(_rbGdi);
            _radioGroup.Location = new Point(8, 8);
            _radioGroup.Margin = new Padding(0);
            _radioGroup.Name = "_radioGroup";
            _radioGroup.Size = new Size(210, 35);
            _radioGroup.TabIndex = 0;
            _radioGroup.WrapContents = false;
            // 
            // _lblCapture
            // 
            _lblCapture.AutoSize = true;
            _lblCapture.Location = new Point(0, 6);
            _lblCapture.Margin = new Padding(0, 6, 2, 0);
            _lblCapture.Name = "_lblCapture";
            _lblCapture.Size = new Size(52, 25);
            _lblCapture.TabIndex = 0;
            _lblCapture.Text = "取得:";
            // 
            // _rbWgc
            // 
            _rbWgc.AutoSize = true;
            _rbWgc.Location = new Point(57, 3);
            _rbWgc.Name = "_rbWgc";
            _rbWgc.Size = new Size(77, 29);
            _rbWgc.TabIndex = 1;
            _rbWgc.Text = "WGC";
            // 
            // _rbGdi
            // 
            _rbGdi.AutoSize = true;
            _rbGdi.Location = new Point(140, 3);
            _rbGdi.Name = "_rbGdi";
            _rbGdi.Size = new Size(67, 29);
            _rbGdi.TabIndex = 2;
            _rbGdi.Text = "GDI";
            // 
            // _cbAuto
            // 
            _cbAuto.AutoSize = true;
            _cbAuto.Location = new Point(221, 11);
            _cbAuto.Name = "_cbAuto";
            _cbAuto.Size = new Size(110, 29);
            _cbAuto.TabIndex = 1;
            _cbAuto.Text = "自動監視";
            // 
            // _btnCapture
            // 
            _btnCapture.AutoSize = true;
            _btnCapture.Location = new Point(337, 11);
            _btnCapture.Name = "_btnCapture";
            _btnCapture.Size = new Size(123, 35);
            _btnCapture.TabIndex = 2;
            _btnCapture.Text = "手動キャプチャ";
            // 
            // _btnLinks
            // 
            _btnLinks.AutoSize = true;
            _btnLinks.Location = new Point(466, 11);
            _btnLinks.Name = "_btnLinks";
            _btnLinks.Size = new Size(98, 35);
            _btnLinks.TabIndex = 3;
            _btnLinks.Text = "リンク設定";
            // 
            // _lblCharacter
            // 
            _lblCharacter.AutoSize = true;
            _lblCharacter.Location = new Point(575, 14);
            _lblCharacter.Margin = new Padding(8, 6, 2, 0);
            _lblCharacter.Name = "_lblCharacter";
            _lblCharacter.Size = new Size(82, 25);
            _lblCharacter.TabIndex = 4;
            _lblCharacter.Text = "  枠キャラ:";
            // 
            // _cbCharacter
            // 
            _cbCharacter.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbCharacter.Location = new Point(667, 11);
            _cbCharacter.Margin = new Padding(8, 3, 0, 0);
            _cbCharacter.Name = "_cbCharacter";
            _cbCharacter.Size = new Size(150, 33);
            _cbCharacter.TabIndex = 5;
            // 
            // _status
            // 
            _status.Dock = DockStyle.Top;
            _status.Location = new Point(0, 53);
            _status.Name = "_status";
            _status.Padding = new Padding(8, 4, 8, 4);
            _status.Size = new Size(1119, 26);
            _status.TabIndex = 1;
            _status.Text = "初期化中...";
            // 
            // _mainSplit
            // 
            _mainSplit.Dock = DockStyle.Fill;
            _mainSplit.Location = new Point(0, 79);
            _mainSplit.Name = "_mainSplit";
            // 
            // _mainSplit.Panel1
            // 
            _mainSplit.Panel1.Controls.Add(_statePanel);
            // 
            // _mainSplit.Panel2
            // 
            _mainSplit.Panel2.Controls.Add(_outer);
            _mainSplit.Size = new Size(1119, 445);
            _mainSplit.SplitterDistance = 373;
            _mainSplit.TabIndex = 0;
            // 
            // _statePanel
            // 
            _statePanel.Controls.Add(_relicHeaderPanel);
            _statePanel.Controls.Add(_stateText);
            _statePanel.Controls.Add(_stateHeaderLabel);
            _statePanel.Dock = DockStyle.Fill;
            _statePanel.Location = new Point(0, 0);
            _statePanel.Name = "_statePanel";
            _statePanel.Size = new Size(373, 445);
            _statePanel.TabIndex = 0;
            // 
            // _relicHeaderPanel
            // 
            _relicHeaderPanel.Controls.Add(_relicList);
            _relicHeaderPanel.Controls.Add(_relicHeaderLabel);
            _relicHeaderPanel.Dock = DockStyle.Fill;
            _relicHeaderPanel.Location = new Point(0, 168);
            _relicHeaderPanel.Name = "_relicHeaderPanel";
            _relicHeaderPanel.Size = new Size(373, 277);
            _relicHeaderPanel.TabIndex = 0;
            // 
            // _relicList
            // 
            _relicList.Columns.AddRange(new ColumnHeader[] { _colRelic, _colFloor });
            _relicList.Dock = DockStyle.Fill;
            _relicList.FullRowSelect = true;
            _relicList.Location = new Point(0, 18);
            _relicList.Name = "_relicList";
            _relicList.Size = new Size(373, 259);
            _relicList.TabIndex = 0;
            _relicList.UseCompatibleStateImageBehavior = false;
            _relicList.View = View.Details;
            //
            // _colRelic
            //
            _colRelic.Text = "所有レリック";
            _colRelic.Width = 200;
            //
            // _colFloor
            //
            _colFloor.Text = "床";
            _colFloor.Width = 45;
            //
            // _relicHeaderLabel
            // 
            _relicHeaderLabel.BackColor = SystemColors.ControlDark;
            _relicHeaderLabel.Dock = DockStyle.Top;
            _relicHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _relicHeaderLabel.Location = new Point(0, 0);
            _relicHeaderLabel.Name = "_relicHeaderLabel";
            _relicHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _relicHeaderLabel.Size = new Size(373, 18);
            _relicHeaderLabel.TabIndex = 1;
            _relicHeaderLabel.Text = "所有レリック";
            _relicHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _stateText
            // 
            _stateText.Dock = DockStyle.Top;
            _stateText.Font = new Font("Microsoft Sans Serif", 9F);
            _stateText.Location = new Point(0, 18);
            _stateText.Name = "_stateText";
            _stateText.Padding = new Padding(6, 4, 4, 4);
            _stateText.Size = new Size(373, 150);
            _stateText.TabIndex = 1;
            _stateText.Text = "セーブ読込待ち...";
            // 
            // _stateHeaderLabel
            // 
            _stateHeaderLabel.BackColor = SystemColors.ControlDark;
            _stateHeaderLabel.Dock = DockStyle.Top;
            _stateHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _stateHeaderLabel.Location = new Point(0, 0);
            _stateHeaderLabel.Name = "_stateHeaderLabel";
            _stateHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _stateHeaderLabel.Size = new Size(373, 18);
            _stateHeaderLabel.TabIndex = 2;
            _stateHeaderLabel.Text = "現在状態（current_run.save）";
            _stateHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _outer
            // 
            _outer.Dock = DockStyle.Fill;
            _outer.Location = new Point(0, 0);
            _outer.Name = "_outer";
            // 
            // _outer.Panel1
            // 
            _outer.Panel1.Controls.Add(_left);
            // 
            // _outer.Panel2
            // 
            _outer.Panel2.Controls.Add(_right);
            _outer.Size = new Size(742, 445);
            _outer.SplitterDistance = 247;
            _outer.TabIndex = 0;
            // 
            // _left
            // 
            _left.Dock = DockStyle.Fill;
            _left.Location = new Point(0, 0);
            _left.Name = "_left";
            _left.Orientation = Orientation.Horizontal;
            // 
            // _left.Panel1
            // 
            _left.Panel1.Controls.Add(_list);
            // 
            // _left.Panel2
            // 
            _left.Panel2.Controls.Add(_ocrHeaderPanel);
            _left.Size = new Size(247, 445);
            _left.SplitterDistance = 222;
            _left.TabIndex = 0;
            // 
            // _list
            // 
            _list.Columns.AddRange(new ColumnHeader[] { _colCardId, _colEn, _colJp, _colConf, _colRecog });
            _list.Dock = DockStyle.Fill;
            _list.FullRowSelect = true;
            _list.Location = new Point(0, 0);
            _list.Name = "_list";
            _list.Size = new Size(247, 222);
            _list.TabIndex = 0;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;
            //
            // _colCardId
            //
            _colCardId.Text = "CardId";
            _colCardId.Width = 180;
            //
            // _colEn
            //
            _colEn.Text = "EN";
            _colEn.Width = 130;
            //
            // _colJp
            //
            _colJp.Text = "JP";
            _colJp.Width = 120;
            //
            // _colConf
            //
            _colConf.Text = "確信度";
            _colConf.Width = 60;
            //
            // _colRecog
            //
            _colRecog.Text = "認識器";
            _colRecog.Width = 70;
            //
            // _ocrHeaderPanel
            // 
            _ocrHeaderPanel.Controls.Add(_ocrList);
            _ocrHeaderPanel.Controls.Add(_ocrHeaderLabel);
            _ocrHeaderPanel.Dock = DockStyle.Fill;
            _ocrHeaderPanel.Location = new Point(0, 0);
            _ocrHeaderPanel.Name = "_ocrHeaderPanel";
            _ocrHeaderPanel.Size = new Size(247, 219);
            _ocrHeaderPanel.TabIndex = 0;
            // 
            // _ocrList
            // 
            _ocrList.Columns.AddRange(new ColumnHeader[] { _colOcrText, _colOcrKind, _colOcrMatch, _colOcrDist });
            _ocrList.Dock = DockStyle.Fill;
            _ocrList.FullRowSelect = true;
            _ocrList.Location = new Point(0, 18);
            _ocrList.Name = "_ocrList";
            _ocrList.Size = new Size(247, 201);
            _ocrList.TabIndex = 0;
            _ocrList.UseCompatibleStateImageBehavior = false;
            _ocrList.View = View.Details;
            //
            // _colOcrText
            //
            _colOcrText.Text = "検出テキスト／候補";
            _colOcrText.Width = 240;
            //
            // _colOcrKind
            //
            _colOcrKind.Text = "種別";
            _colOcrKind.Width = 45;
            //
            // _colOcrMatch
            //
            _colOcrMatch.Text = "一致";
            _colOcrMatch.Width = 80;
            //
            // _colOcrDist
            //
            _colOcrDist.Text = "距離";
            _colOcrDist.Width = 90;
            //
            // _ocrHeaderLabel
            // 
            _ocrHeaderLabel.BackColor = SystemColors.ControlDark;
            _ocrHeaderLabel.Dock = DockStyle.Top;
            _ocrHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _ocrHeaderLabel.Location = new Point(0, 0);
            _ocrHeaderLabel.Name = "_ocrHeaderLabel";
            _ocrHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _ocrHeaderLabel.Size = new Size(247, 18);
            _ocrHeaderLabel.TabIndex = 1;
            _ocrHeaderLabel.Text = "ショップ候補（レリック／ポーション）";
            _ocrHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _right
            // 
            _right.Dock = DockStyle.Fill;
            _right.Location = new Point(0, 0);
            _right.Name = "_right";
            _right.Orientation = Orientation.Horizontal;
            // 
            // _right.Panel1
            // 
            _right.Panel1.Controls.Add(_previewHeaderPanel);
            // 
            // _right.Panel2
            // 
            _right.Panel2.Controls.Add(_thumbHeaderPanel);
            _right.Size = new Size(491, 445);
            _right.SplitterDistance = 222;
            _right.TabIndex = 0;
            // 
            // _previewHeaderPanel
            // 
            _previewHeaderPanel.Controls.Add(_capturePreview);
            _previewHeaderPanel.Controls.Add(_previewHeaderLabel);
            _previewHeaderPanel.Dock = DockStyle.Fill;
            _previewHeaderPanel.Location = new Point(0, 0);
            _previewHeaderPanel.Name = "_previewHeaderPanel";
            _previewHeaderPanel.Size = new Size(491, 222);
            _previewHeaderPanel.TabIndex = 0;
            // 
            // _capturePreview
            // 
            _capturePreview.BackColor = SystemColors.ControlDarkDark;
            _capturePreview.Dock = DockStyle.Fill;
            _capturePreview.Location = new Point(0, 18);
            _capturePreview.Name = "_capturePreview";
            _capturePreview.Size = new Size(491, 204);
            _capturePreview.SizeMode = PictureBoxSizeMode.Zoom;
            _capturePreview.TabIndex = 0;
            _capturePreview.TabStop = false;
            // 
            // _previewHeaderLabel
            // 
            _previewHeaderLabel.BackColor = SystemColors.ControlDark;
            _previewHeaderLabel.Dock = DockStyle.Top;
            _previewHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _previewHeaderLabel.Location = new Point(0, 0);
            _previewHeaderLabel.Name = "_previewHeaderLabel";
            _previewHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _previewHeaderLabel.Size = new Size(491, 18);
            _previewHeaderLabel.TabIndex = 1;
            _previewHeaderLabel.Text = "キャプチャ画像（縮小プレビュー）";
            _previewHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _thumbHeaderPanel
            // 
            _thumbHeaderPanel.Controls.Add(_thumb);
            _thumbHeaderPanel.Controls.Add(_thumbHeaderLabel);
            _thumbHeaderPanel.Dock = DockStyle.Fill;
            _thumbHeaderPanel.Location = new Point(0, 0);
            _thumbHeaderPanel.Name = "_thumbHeaderPanel";
            _thumbHeaderPanel.Size = new Size(491, 219);
            _thumbHeaderPanel.TabIndex = 0;
            // 
            // _thumb
            // 
            _thumb.BackColor = SystemColors.ControlLight;
            _thumb.Dock = DockStyle.Fill;
            _thumb.Location = new Point(0, 18);
            _thumb.Name = "_thumb";
            _thumb.Size = new Size(491, 201);
            _thumb.SizeMode = PictureBoxSizeMode.Zoom;
            _thumb.TabIndex = 0;
            _thumb.TabStop = false;
            // 
            // _thumbHeaderLabel
            // 
            _thumbHeaderLabel.BackColor = SystemColors.ControlDark;
            _thumbHeaderLabel.Dock = DockStyle.Top;
            _thumbHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _thumbHeaderLabel.Location = new Point(0, 0);
            _thumbHeaderLabel.Name = "_thumbHeaderLabel";
            _thumbHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _thumbHeaderLabel.Size = new Size(491, 18);
            _thumbHeaderLabel.TabIndex = 1;
            _thumbHeaderLabel.Text = "選択カードの portrait";
            _thumbHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LiveCaptureForm
            // 
            ClientSize = new Size(1119, 524);
            Controls.Add(_mainSplit);
            Controls.Add(_status);
            Controls.Add(_top);
            Name = "LiveCaptureForm";
            StartPosition = FormStartPosition.Manual;
            Text = "ライブキャプチャ（カード／ショップ検出）";
            _top.ResumeLayout(false);
            _top.PerformLayout();
            _radioGroup.ResumeLayout(false);
            _radioGroup.PerformLayout();
            _mainSplit.Panel1.ResumeLayout(false);
            _mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_mainSplit).EndInit();
            _mainSplit.ResumeLayout(false);
            _statePanel.ResumeLayout(false);
            _relicHeaderPanel.ResumeLayout(false);
            _outer.Panel1.ResumeLayout(false);
            _outer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_outer).EndInit();
            _outer.ResumeLayout(false);
            _left.Panel1.ResumeLayout(false);
            _left.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_left).EndInit();
            _left.ResumeLayout(false);
            _ocrHeaderPanel.ResumeLayout(false);
            _right.Panel1.ResumeLayout(false);
            _right.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_right).EndInit();
            _right.ResumeLayout(false);
            _previewHeaderPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_capturePreview).EndInit();
            _thumbHeaderPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_thumb).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private FlowLayoutPanel _top;
        private FlowLayoutPanel _radioGroup;
        private Label _lblCapture;
        private RadioButton _rbWgc;
        private RadioButton _rbGdi;
        private CheckBox _cbAuto;
        private Button _btnCapture;
        private Button _btnLinks;
        private Label _lblCharacter;
        private ComboBox _cbCharacter;
        private Label _status;
        private SplitContainer _mainSplit;
        private Panel _statePanel;
        private Panel _relicHeaderPanel;
        private ListView _relicList;
        private ColumnHeader _colRelic;
        private ColumnHeader _colFloor;
        private Label _relicHeaderLabel;
        private Label _stateText;
        private Label _stateHeaderLabel;
        private SplitContainer _outer;
        private SplitContainer _left;
        private ListView _list;
        private ColumnHeader _colCardId;
        private ColumnHeader _colEn;
        private ColumnHeader _colJp;
        private ColumnHeader _colConf;
        private ColumnHeader _colRecog;
        private Panel _ocrHeaderPanel;
        private ListView _ocrList;
        private ColumnHeader _colOcrText;
        private ColumnHeader _colOcrKind;
        private ColumnHeader _colOcrMatch;
        private ColumnHeader _colOcrDist;
        private Label _ocrHeaderLabel;
        private SplitContainer _right;
        private Panel _previewHeaderPanel;
        private PictureBox _capturePreview;
        private Label _previewHeaderLabel;
        private Panel _thumbHeaderPanel;
        private PictureBox _thumb;
        private Label _thumbHeaderLabel;
    }
}
