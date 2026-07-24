namespace SpireScope
{
    partial class CaptureDiagnosticsForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            _diagStatus = new Label();
            _outer = new SplitContainer();
            _left = new SplitContainer();
            _list = new ListView();
            _colCardId = new ColumnHeader();
            _colEn = new ColumnHeader();
            _colJp = new ColumnHeader();
            _colConf = new ColumnHeader();
            _colRecog = new ColumnHeader();
            _colTier = new ColumnHeader();
            _ocrHeaderPanel = new Panel();
            _ocrList = new ListView();
            _colOcrText = new ColumnHeader();
            _colOcrKind = new ColumnHeader();
            _colOcrMatch = new ColumnHeader();
            _colOcrDist = new ColumnHeader();
            _colOcrTier = new ColumnHeader();
            _ocrHeaderLabel = new Label();
            _previewHeaderPanel = new Panel();
            _capturePreview = new PictureBox();
            _previewHeaderLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)_outer).BeginInit();
            _outer.Panel1.SuspendLayout();
            _outer.Panel2.SuspendLayout();
            _outer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_left).BeginInit();
            _left.Panel1.SuspendLayout();
            _left.Panel2.SuspendLayout();
            _left.SuspendLayout();
            _ocrHeaderPanel.SuspendLayout();
            _previewHeaderPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_capturePreview).BeginInit();
            SuspendLayout();
            //
            // _diagStatus
            //
            _diagStatus.Dock = DockStyle.Top;
            _diagStatus.Location = new Point(0, 0);
            _diagStatus.Name = "_diagStatus";
            _diagStatus.Padding = new Padding(8, 4, 8, 4);
            _diagStatus.Size = new Size(900, 26);
            _diagStatus.TabIndex = 1;
            _diagStatus.Text = "キャプチャ待機中...";
            //
            // _outer
            //
            _outer.Dock = DockStyle.Fill;
            _outer.Location = new Point(0, 26);
            _outer.Name = "_outer";
            //
            // _outer.Panel1
            //
            _outer.Panel1.Controls.Add(_left);
            //
            // _outer.Panel2
            //
            _outer.Panel2.Controls.Add(_previewHeaderPanel);
            _outer.Size = new Size(900, 474);
            _outer.SplitterDistance = 495;
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
            _left.Size = new Size(495, 474);
            _left.SplitterDistance = 237;
            _left.TabIndex = 0;
            //
            // _list
            //
            _list.Columns.AddRange(new ColumnHeader[] { _colCardId, _colEn, _colJp, _colConf, _colRecog, _colTier });
            _list.Dock = DockStyle.Fill;
            _list.FullRowSelect = true;
            _list.Location = new Point(0, 0);
            _list.Name = "_list";
            _list.Size = new Size(495, 237);
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
            // _colTier
            //
            _colTier.Text = "Tier";
            _colTier.Width = 60;
            //
            // _ocrHeaderPanel
            //
            _ocrHeaderPanel.Controls.Add(_ocrList);
            _ocrHeaderPanel.Controls.Add(_ocrHeaderLabel);
            _ocrHeaderPanel.Dock = DockStyle.Fill;
            _ocrHeaderPanel.Location = new Point(0, 0);
            _ocrHeaderPanel.Name = "_ocrHeaderPanel";
            _ocrHeaderPanel.Size = new Size(495, 233);
            _ocrHeaderPanel.TabIndex = 0;
            //
            // _ocrList
            //
            _ocrList.Columns.AddRange(new ColumnHeader[] { _colOcrText, _colOcrKind, _colOcrMatch, _colOcrDist, _colOcrTier });
            _ocrList.Dock = DockStyle.Fill;
            _ocrList.FullRowSelect = true;
            _ocrList.Location = new Point(0, 18);
            _ocrList.Name = "_ocrList";
            _ocrList.Size = new Size(495, 215);
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
            // _colOcrTier
            //
            _colOcrTier.Text = "Tier";
            _colOcrTier.Width = 90;
            //
            // _ocrHeaderLabel
            //
            _ocrHeaderLabel.BackColor = SystemColors.ControlDark;
            _ocrHeaderLabel.Dock = DockStyle.Top;
            _ocrHeaderLabel.ForeColor = SystemColors.ControlLightLight;
            _ocrHeaderLabel.Location = new Point(0, 0);
            _ocrHeaderLabel.Name = "_ocrHeaderLabel";
            _ocrHeaderLabel.Padding = new Padding(4, 0, 0, 0);
            _ocrHeaderLabel.Size = new Size(495, 18);
            _ocrHeaderLabel.TabIndex = 1;
            _ocrHeaderLabel.Text = "ショップ候補（レリック／ポーション）";
            _ocrHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _previewHeaderPanel
            //
            _previewHeaderPanel.Controls.Add(_capturePreview);
            _previewHeaderPanel.Controls.Add(_previewHeaderLabel);
            _previewHeaderPanel.Dock = DockStyle.Fill;
            _previewHeaderPanel.Location = new Point(0, 0);
            _previewHeaderPanel.Name = "_previewHeaderPanel";
            _previewHeaderPanel.Size = new Size(401, 474);
            _previewHeaderPanel.TabIndex = 0;
            //
            // _capturePreview
            //
            _capturePreview.BackColor = SystemColors.ControlDarkDark;
            _capturePreview.Dock = DockStyle.Fill;
            _capturePreview.Location = new Point(0, 18);
            _capturePreview.Name = "_capturePreview";
            _capturePreview.Size = new Size(401, 456);
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
            _previewHeaderLabel.Size = new Size(401, 18);
            _previewHeaderLabel.TabIndex = 1;
            _previewHeaderLabel.Text = "キャプチャ画像（縮小プレビュー）";
            _previewHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // CaptureDiagnosticsForm
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 500);
            Controls.Add(_outer);
            Controls.Add(_diagStatus);
            Name = "CaptureDiagnosticsForm";
            Text = "キャプチャ診断";
            _outer.Panel1.ResumeLayout(false);
            _outer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_outer).EndInit();
            _outer.ResumeLayout(false);
            _left.Panel1.ResumeLayout(false);
            _left.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_left).EndInit();
            _left.ResumeLayout(false);
            _ocrHeaderPanel.ResumeLayout(false);
            _previewHeaderPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_capturePreview).EndInit();
            ResumeLayout(false);
        }

        private Label _diagStatus;
        private SplitContainer _outer;
        private SplitContainer _left;
        private ListView _list;
        private ColumnHeader _colCardId;
        private ColumnHeader _colEn;
        private ColumnHeader _colJp;
        private ColumnHeader _colConf;
        private ColumnHeader _colRecog;
        private ColumnHeader _colTier;
        private Panel _ocrHeaderPanel;
        private ListView _ocrList;
        private ColumnHeader _colOcrText;
        private ColumnHeader _colOcrKind;
        private ColumnHeader _colOcrMatch;
        private ColumnHeader _colOcrDist;
        private ColumnHeader _colOcrTier;
        private Label _ocrHeaderLabel;
        private Panel _previewHeaderPanel;
        private PictureBox _capturePreview;
        private Label _previewHeaderLabel;
    }
}
