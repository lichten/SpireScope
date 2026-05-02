namespace StS2Toys
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
            panelTop = new Panel();
            btnOpen = new Button();
            txtFilePath = new TextBox();
            panelInfo = new Panel();
            lblInfo = new Label();
            splitContainer = new SplitContainer();
            lblDeckTitle = new Label();
            listViewDeck = new ListView();
            colCardName = new ColumnHeader();
            colCardNameJa = new ColumnHeader();
            colCardCount = new ColumnHeader();
            lblRelicsTitle = new Label();
            listViewRelics = new ListView();
            colRelicName = new ColumnHeader();
            colRelicNameJa = new ColumnHeader();

            panelTop.SuspendLayout();
            panelInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            SuspendLayout();

            // panelTop
            panelTop.Controls.Add(txtFilePath);
            panelTop.Controls.Add(btnOpen);
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 44;
            panelTop.Padding = new Padding(8, 8, 8, 4);

            // btnOpen
            btnOpen.Text = "ファイルを開く";
            btnOpen.Width = 110;
            btnOpen.Dock = DockStyle.Left;
            btnOpen.Click += BtnOpen_Click;

            // txtFilePath
            txtFilePath.Dock = DockStyle.Fill;
            txtFilePath.ReadOnly = true;
            txtFilePath.Margin = new Padding(4, 0, 0, 0);

            // panelInfo
            panelInfo.Controls.Add(lblInfo);
            panelInfo.Dock = DockStyle.Top;
            panelInfo.Height = 52;
            panelInfo.Padding = new Padding(10, 6, 8, 4);
            panelInfo.BackColor = SystemColors.ControlLight;

            // lblInfo
            lblInfo.Dock = DockStyle.Fill;
            lblInfo.Font = new Font("Segoe UI", 10f);
            lblInfo.Text = "ファイルを開くと、ランの情報を表示します。";

            // splitContainer
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 420;

            // splitContainer.Panel1 (deck)
            splitContainer.Panel1.Controls.Add(listViewDeck);
            splitContainer.Panel1.Controls.Add(lblDeckTitle);

            // lblDeckTitle
            lblDeckTitle.Dock = DockStyle.Top;
            lblDeckTitle.Height = 26;
            lblDeckTitle.Padding = new Padding(4, 4, 0, 0);
            lblDeckTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblDeckTitle.Text = "デッキ";

            // listViewDeck
            listViewDeck.Dock = DockStyle.Fill;
            listViewDeck.View = View.Details;
            listViewDeck.FullRowSelect = true;
            listViewDeck.GridLines = true;
            listViewDeck.Columns.Add(colCardName);
            listViewDeck.Columns.Add(colCardNameJa);
            listViewDeck.Columns.Add(colCardCount);

            listViewDeck.ItemActivate += ListViewDeck_ItemActivate;

            colCardName.Text = "カード名 (EN)";
            colCardName.Width = 180;
            colCardNameJa.Text = "カード名 (JP)";
            colCardNameJa.Width = 160;
            colCardCount.Text = "枚数";
            colCardCount.Width = 55;
            colCardCount.TextAlign = HorizontalAlignment.Right;

            // splitContainer.Panel2 (relics)
            splitContainer.Panel2.Controls.Add(listViewRelics);
            splitContainer.Panel2.Controls.Add(lblRelicsTitle);

            // lblRelicsTitle
            lblRelicsTitle.Dock = DockStyle.Top;
            lblRelicsTitle.Height = 26;
            lblRelicsTitle.Padding = new Padding(4, 4, 0, 0);
            lblRelicsTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblRelicsTitle.Text = "レリック";

            // listViewRelics
            listViewRelics.Dock = DockStyle.Fill;
            listViewRelics.View = View.Details;
            listViewRelics.FullRowSelect = true;
            listViewRelics.GridLines = true;
            listViewRelics.Columns.Add(colRelicName);
            listViewRelics.Columns.Add(colRelicNameJa);

            listViewRelics.ItemActivate += ListViewRelics_ItemActivate;

            colRelicName.Text = "レリック名 (EN)";
            colRelicName.Width = 160;
            colRelicNameJa.Text = "レリック名 (JP)";
            colRelicNameJa.Width = 140;

            // Form1
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 520);
            Text = "StS2 Deck Viewer";
            Controls.Add(splitContainer);
            Controls.Add(panelInfo);
            Controls.Add(panelTop);

            panelTop.ResumeLayout();
            panelInfo.ResumeLayout();
            splitContainer.Panel1.ResumeLayout();
            splitContainer.Panel2.ResumeLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout();
            ResumeLayout();
        }

        private Panel panelTop;
        private Button btnOpen;
        private TextBox txtFilePath;
        private Panel panelInfo;
        private Label lblInfo;
        private SplitContainer splitContainer;
        private Label lblDeckTitle;
        private ListView listViewDeck;
        private ColumnHeader colCardName;
        private ColumnHeader colCardNameJa;
        private ColumnHeader colCardCount;
        private Label lblRelicsTitle;
        private ListView listViewRelics;
        private ColumnHeader colRelicName;
        private ColumnHeader colRelicNameJa;
    }
}
