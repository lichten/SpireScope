namespace SpireScope
{
    partial class Form1
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
            lblUpdateFlash = new Label();
            lblLastUpdated = new Label();
            lblPotionOdds = new Label();
            btnLang = new Button();
            btnToggleAuto = new Button();
            btnOpen = new Button();
            lblGroupFile = new Label();
            lblGroupCapture = new Label();
            btnCaptureSource = new Button();
            lblGroupSettings = new Label();
            btnAssetSetup = new Button();
            splitContainerOuter = new SplitContainer();
            panelSideButtons = new Panel();
            btnEncounterOverview = new Button();
            btnHpHistory = new Button();
            lblGroupOther = new Label();
            btnCharacterOverview = new Button();
            btnCombinedOverview = new Button();
            lblGroupOverview = new Label();
            _cbAuto = new CheckBox();
            _btnCapture = new Button();
            _btnLinks = new Button();
            _status = new Label();
            _captureResultView = new CaptureResultView();
            btnCaptureDiagnostics = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainerOuter).BeginInit();
            splitContainerOuter.Panel1.SuspendLayout();
            splitContainerOuter.Panel2.SuspendLayout();
            splitContainerOuter.SuspendLayout();
            panelSideButtons.SuspendLayout();
            SuspendLayout();
            //
            // splitContainerOuter
            //
            splitContainerOuter.Dock = DockStyle.Fill;
            splitContainerOuter.FixedPanel = FixedPanel.Panel1;
            splitContainerOuter.Location = new Point(0, 96);
            splitContainerOuter.Name = "splitContainerOuter";
            //
            // splitContainerOuter.Panel1
            //
            splitContainerOuter.Panel1.Controls.Add(panelSideButtons);
            splitContainerOuter.Panel1MinSize = 60;
            //
            // splitContainerOuter.Panel2
            //
            splitContainerOuter.Panel2.Controls.Add(_captureResultView);
            splitContainerOuter.Panel2.Controls.Add(_status);
            splitContainerOuter.Panel2MinSize = 200;
            splitContainerOuter.Size = new Size(800, 424);
            splitContainerOuter.SplitterDistance = 150;
            splitContainerOuter.TabIndex = 0;
            //
            // panelSideButtons
            //
            panelSideButtons.Controls.Add(btnAssetSetup);
            panelSideButtons.Controls.Add(_btnLinks);
            panelSideButtons.Controls.Add(btnLang);
            panelSideButtons.Controls.Add(lblGroupSettings);
            panelSideButtons.Controls.Add(btnEncounterOverview);
            panelSideButtons.Controls.Add(btnHpHistory);
            panelSideButtons.Controls.Add(lblGroupOther);
            panelSideButtons.Controls.Add(btnCharacterOverview);
            panelSideButtons.Controls.Add(btnCombinedOverview);
            panelSideButtons.Controls.Add(lblGroupOverview);
            panelSideButtons.Controls.Add(lblUpdateFlash);
            panelSideButtons.Controls.Add(lblLastUpdated);
            panelSideButtons.Controls.Add(lblPotionOdds);
            panelSideButtons.Controls.Add(btnToggleAuto);
            panelSideButtons.Controls.Add(btnOpen);
            panelSideButtons.Controls.Add(lblGroupFile);
            panelSideButtons.Controls.Add(btnCaptureDiagnostics);
            panelSideButtons.Controls.Add(_cbAuto);
            panelSideButtons.Controls.Add(_btnCapture);
            panelSideButtons.Controls.Add(btnCaptureSource);
            panelSideButtons.Controls.Add(lblGroupCapture);
            panelSideButtons.Dock = DockStyle.Fill;
            panelSideButtons.Location = new Point(0, 0);
            panelSideButtons.Name = "panelSideButtons";
            panelSideButtons.Size = new Size(150, 424);
            panelSideButtons.TabIndex = 0;
            //
            // btnEncounterOverview
            //
            btnEncounterOverview.Dock = DockStyle.Top;
            btnEncounterOverview.Location = new Point(0, 246);
            btnEncounterOverview.Name = "btnEncounterOverview";
            btnEncounterOverview.Size = new Size(150, 30);
            btnEncounterOverview.TabIndex = 6;
            btnEncounterOverview.Text = "○ 敵情報";
            btnEncounterOverview.Click += BtnEncounterOverview_Click;
            //
            // btnHpHistory
            //
            btnHpHistory.Dock = DockStyle.Top;
            btnHpHistory.Location = new Point(0, 216);
            btnHpHistory.Name = "btnHpHistory";
            btnHpHistory.Size = new Size(150, 30);
            btnHpHistory.TabIndex = 5;
            btnHpHistory.Text = "○ HP変動";
            btnHpHistory.Click += BtnHpHistory_Click;
            //
            // lblGroupOther
            //
            lblGroupOther.BackColor = SystemColors.ControlDark;
            lblGroupOther.Dock = DockStyle.Top;
            lblGroupOther.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblGroupOther.ForeColor = Color.White;
            lblGroupOther.Location = new Point(0, 198);
            lblGroupOther.Name = "lblGroupOther";
            lblGroupOther.Padding = new Padding(4, 0, 0, 0);
            lblGroupOther.Size = new Size(150, 18);
            lblGroupOther.TabIndex = 8;
            lblGroupOther.Text = "その他";
            lblGroupOther.TextAlign = ContentAlignment.MiddleLeft;
            //
            // btnCharacterOverview
            //
            btnCharacterOverview.Dock = DockStyle.Top;
            btnCharacterOverview.Location = new Point(0, 168);
            btnCharacterOverview.Name = "btnCharacterOverview";
            btnCharacterOverview.Size = new Size(150, 30);
            btnCharacterOverview.TabIndex = 8;
            btnCharacterOverview.Text = "○ キャラクター概観";
            btnCharacterOverview.Click += BtnCharacterOverview_Click;
            //
            // btnCombinedOverview
            //
            btnCombinedOverview.Dock = DockStyle.Top;
            btnCombinedOverview.Location = new Point(0, 108);
            btnCombinedOverview.Name = "btnCombinedOverview";
            btnCombinedOverview.Size = new Size(150, 30);
            btnCombinedOverview.TabIndex = 2;
            btnCombinedOverview.Text = "○ デッキ概観";
            btnCombinedOverview.Click += BtnCombinedOverview_Click;
            //
            // lblGroupOverview
            //
            lblGroupOverview.BackColor = SystemColors.ControlDark;
            lblGroupOverview.Dock = DockStyle.Top;
            lblGroupOverview.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblGroupOverview.ForeColor = Color.White;
            lblGroupOverview.Location = new Point(0, 90);
            lblGroupOverview.Name = "lblGroupOverview";
            lblGroupOverview.Padding = new Padding(4, 0, 0, 0);
            lblGroupOverview.Size = new Size(150, 18);
            lblGroupOverview.TabIndex = 14;
            lblGroupOverview.Text = "概観";
            lblGroupOverview.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblGroupFile
            //
            lblGroupFile.BackColor = SystemColors.ControlDark;
            lblGroupFile.Dock = DockStyle.Top;
            lblGroupFile.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblGroupFile.ForeColor = Color.White;
            lblGroupFile.Location = new Point(0, 0);
            lblGroupFile.Name = "lblGroupFile";
            lblGroupFile.Padding = new Padding(4, 0, 0, 0);
            lblGroupFile.Size = new Size(150, 18);
            lblGroupFile.TabIndex = 15;
            lblGroupFile.Text = "ファイル / 更新";
            lblGroupFile.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblGroupCapture
            //
            lblGroupCapture.BackColor = SystemColors.ControlDark;
            lblGroupCapture.Dock = DockStyle.Top;
            lblGroupCapture.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblGroupCapture.ForeColor = Color.White;
            lblGroupCapture.Location = new Point(0, 0);
            lblGroupCapture.Name = "lblGroupCapture";
            lblGroupCapture.Padding = new Padding(4, 0, 0, 0);
            lblGroupCapture.Size = new Size(150, 18);
            lblGroupCapture.TabIndex = 21;
            lblGroupCapture.Text = "キャプチャ";
            lblGroupCapture.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblGroupSettings
            //
            lblGroupSettings.BackColor = SystemColors.ControlDark;
            lblGroupSettings.Dock = DockStyle.Top;
            lblGroupSettings.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblGroupSettings.ForeColor = Color.White;
            lblGroupSettings.Location = new Point(0, 0);
            lblGroupSettings.Name = "lblGroupSettings";
            lblGroupSettings.Padding = new Padding(4, 0, 0, 0);
            lblGroupSettings.Size = new Size(150, 18);
            lblGroupSettings.TabIndex = 26;
            lblGroupSettings.Text = "設定";
            lblGroupSettings.TextAlign = ContentAlignment.MiddleLeft;
            //
            // btnAssetSetup
            //
            btnAssetSetup.Dock = DockStyle.Top;
            btnAssetSetup.Name = "btnAssetSetup";
            btnAssetSetup.Size = new Size(150, 30);
            btnAssetSetup.TabIndex = 27;
            btnAssetSetup.Text = "画像アセット設定";
            btnAssetSetup.Click += BtnAssetSetup_Click;
            //
            // btnCaptureSource
            //
            btnCaptureSource.Dock = DockStyle.Top;
            btnCaptureSource.Location = new Point(0, 18);
            btnCaptureSource.Name = "btnCaptureSource";
            btnCaptureSource.Size = new Size(150, 30);
            btnCaptureSource.TabIndex = 22;
            btnCaptureSource.Text = "取得: WGC";
            btnCaptureSource.Click += BtnCaptureSource_Click;
            //
            // btnOpen
            //
            btnOpen.Dock = DockStyle.Top;
            btnOpen.Location = new Point(0, 18);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(150, 30);
            btnOpen.TabIndex = 16;
            btnOpen.Text = "ファイルを開く";
            btnOpen.Click += BtnOpen_Click;
            //
            // btnToggleAuto
            //
            btnToggleAuto.Dock = DockStyle.Top;
            btnToggleAuto.Location = new Point(0, 48);
            btnToggleAuto.Name = "btnToggleAuto";
            btnToggleAuto.Size = new Size(150, 30);
            btnToggleAuto.TabIndex = 17;
            btnToggleAuto.Text = "○ 自動更新";
            btnToggleAuto.Click += BtnToggleAuto_Click;
            //
            // btnLang
            //
            btnLang.Dock = DockStyle.Top;
            btnLang.Location = new Point(0, 78);
            btnLang.Name = "btnLang";
            btnLang.Size = new Size(150, 30);
            btnLang.TabIndex = 18;
            btnLang.Text = "JP";
            btnLang.Click += BtnLang_Click;
            //
            // lblLastUpdated
            //
            lblLastUpdated.Dock = DockStyle.Top;
            lblLastUpdated.Location = new Point(0, 108);
            lblLastUpdated.Name = "lblLastUpdated";
            lblLastUpdated.Padding = new Padding(8, 0, 0, 0);
            lblLastUpdated.Size = new Size(150, 24);
            lblLastUpdated.TabIndex = 19;
            lblLastUpdated.Text = "最終更新: --:--:--";
            lblLastUpdated.TextAlign = ContentAlignment.MiddleLeft;
            //
            // lblPotionOdds
            //
            lblPotionOdds.AutoSize = false;
            lblPotionOdds.Dock = DockStyle.Top;
            lblPotionOdds.Location = new Point(0, 156);
            lblPotionOdds.Name = "lblPotionOdds";
            lblPotionOdds.Padding = new Padding(8, 2, 0, 2);
            lblPotionOdds.Size = new Size(150, 50);
            lblPotionOdds.TabIndex = 21;
            lblPotionOdds.Text = "ポーション: --";
            lblPotionOdds.TextAlign = ContentAlignment.TopLeft;
            //
            // lblUpdateFlash
            //
            lblUpdateFlash.Dock = DockStyle.Top;
            lblUpdateFlash.ForeColor = Color.ForestGreen;
            lblUpdateFlash.Location = new Point(0, 132);
            lblUpdateFlash.Name = "lblUpdateFlash";
            lblUpdateFlash.Padding = new Padding(8, 0, 0, 0);
            lblUpdateFlash.Size = new Size(150, 24);
            lblUpdateFlash.TabIndex = 20;
            lblUpdateFlash.TextAlign = ContentAlignment.MiddleLeft;
            //
            // _cbAuto
            //
            _cbAuto.AutoSize = false;
            _cbAuto.Dock = DockStyle.Top;
            _cbAuto.Location = new Point(0, 78);
            _cbAuto.Name = "_cbAuto";
            _cbAuto.Padding = new Padding(8, 0, 0, 0);
            _cbAuto.Size = new Size(150, 30);
            _cbAuto.TabIndex = 23;
            _cbAuto.Text = "自動監視";
            //
            // _btnCapture
            //
            _btnCapture.AutoSize = false;
            _btnCapture.Dock = DockStyle.Top;
            _btnCapture.Location = new Point(0, 48);
            _btnCapture.Name = "_btnCapture";
            _btnCapture.Size = new Size(150, 30);
            _btnCapture.TabIndex = 24;
            _btnCapture.Text = "手動キャプチャ";
            //
            // _btnLinks
            //
            _btnLinks.AutoSize = false;
            _btnLinks.Dock = DockStyle.Top;
            _btnLinks.Location = new Point(0, 30);
            _btnLinks.Name = "_btnLinks";
            _btnLinks.Size = new Size(150, 30);
            _btnLinks.TabIndex = 25;
            _btnLinks.Text = "リンク設定";
            //
            // _status
            //
            _status.Dock = DockStyle.Top;
            _status.Location = new Point(0, 53);
            _status.Name = "_status";
            _status.Padding = new Padding(8, 4, 8, 4);
            _status.Size = new Size(646, 26);
            _status.TabIndex = 1;
            _status.Text = "初期化中...";
            //
            // _captureResultView
            //
            _captureResultView.Dock = DockStyle.Fill;
            _captureResultView.Location = new Point(0, 79);
            _captureResultView.Name = "_captureResultView";
            _captureResultView.Size = new Size(646, 345);
            _captureResultView.TabIndex = 0;
            //
            // btnCaptureDiagnostics
            //
            btnCaptureDiagnostics.Dock = DockStyle.Top;
            btnCaptureDiagnostics.Location = new Point(0, 108);
            btnCaptureDiagnostics.Name = "btnCaptureDiagnostics";
            btnCaptureDiagnostics.Size = new Size(150, 30);
            btnCaptureDiagnostics.TabIndex = 28;
            btnCaptureDiagnostics.Text = "○ キャプチャ診断";
            btnCaptureDiagnostics.Click += BtnCaptureDiagnostics_Click;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 520);
            Controls.Add(splitContainerOuter);
            Name = "Form1";
            Text = "SpireScope";
            splitContainerOuter.Panel1.ResumeLayout(false);
            splitContainerOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerOuter).EndInit();
            splitContainerOuter.ResumeLayout(false);
            panelSideButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Button btnToggleAuto;
        private Button btnLang;
        private Label lblLastUpdated;
        private Label lblUpdateFlash;
        private Label lblPotionOdds;
        private Button btnOpen;
        private Label lblGroupFile;
        private SplitContainer splitContainerOuter;
        private Panel panelSideButtons;
        private Button btnCombinedOverview;
        private Button btnHpHistory;
        private Button btnEncounterOverview;
        private Button btnCharacterOverview;
        private Label lblGroupOverview;
        private Label lblGroupOther;
        private Label lblGroupCapture;
        private Button btnCaptureSource;
        private Label lblGroupSettings;
        private Button btnAssetSetup;

        private CheckBox _cbAuto;
        private Button _btnCapture;
        private Button _btnLinks;
        private Label _status;
        private CaptureResultView _captureResultView;
        private Button btnCaptureDiagnostics;
    }
}
