namespace StS2Toys
{
    partial class CardDetailForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tableLayout = new TableLayoutPanel();
            lblTitle    = new Label();
            lblDescEn   = new Label();
            rtbDescEn   = new RichTextBox();
            lblDescJa   = new Label();
            rtbDescJa   = new RichTextBox();
            lblFlavor   = new Label();
            rtbFlavor   = new RichTextBox();
            pnlBottom   = new FlowLayoutPanel();
            btnClose    = new Button();

            tableLayout.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();

            // ---- tableLayout ----
            tableLayout.Dock        = DockStyle.Fill;
            tableLayout.Padding     = new Padding(8);
            tableLayout.ColumnCount = 1;
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tableLayout.RowCount    = 8;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 0: lblTitle
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 1: lblDescEn
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));    // 2: rtbDescEn
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 3: lblDescJa
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));    // 4: rtbDescJa
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0f));    // 5: lblFlavor (collapsed)
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0f));    // 6: rtbFlavor (collapsed)
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // 7: pnlBottom

            // ---- lblTitle ----
            lblTitle.Dock      = DockStyle.Fill;
            lblTitle.Font      = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblTitle.Margin    = new Padding(0, 0, 0, 6);
            lblTitle.AutoSize  = true;

            // ---- lblDescEn ----
            lblDescEn.Text   = "Description (EN)";
            lblDescEn.Dock   = DockStyle.Fill;
            lblDescEn.Font   = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblDescEn.Margin = new Padding(0, 4, 0, 2);
            lblDescEn.AutoSize = true;

            // ---- rtbDescEn ----
            rtbDescEn.Dock        = DockStyle.Fill;
            rtbDescEn.ReadOnly    = true;
            rtbDescEn.BackColor   = SystemColors.Control;
            rtbDescEn.BorderStyle = BorderStyle.None;
            rtbDescEn.ScrollBars  = RichTextBoxScrollBars.Vertical;
            rtbDescEn.WordWrap    = true;
            rtbDescEn.Font        = new Font("Segoe UI", 10f);
            rtbDescEn.Margin      = new Padding(0, 0, 0, 4);

            // ---- lblDescJa ----
            lblDescJa.Text   = "説明 (JP)";
            lblDescJa.Dock   = DockStyle.Fill;
            lblDescJa.Font   = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblDescJa.Margin = new Padding(0, 4, 0, 2);
            lblDescJa.AutoSize = true;

            // ---- rtbDescJa ----
            rtbDescJa.Dock        = DockStyle.Fill;
            rtbDescJa.ReadOnly    = true;
            rtbDescJa.BackColor   = SystemColors.Control;
            rtbDescJa.BorderStyle = BorderStyle.None;
            rtbDescJa.ScrollBars  = RichTextBoxScrollBars.Vertical;
            rtbDescJa.WordWrap    = true;
            rtbDescJa.Font        = new Font("Segoe UI", 10f);
            rtbDescJa.Margin      = new Padding(0, 0, 0, 4);

            // ---- lblFlavor (collapsed by default) ----
            lblFlavor.Text     = "Flavor";
            lblFlavor.Dock     = DockStyle.Fill;
            lblFlavor.Font     = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblFlavor.Margin   = new Padding(0, 4, 0, 2);
            lblFlavor.AutoSize = true;
            lblFlavor.Visible  = false;

            // ---- rtbFlavor (collapsed by default) ----
            rtbFlavor.Dock        = DockStyle.Fill;
            rtbFlavor.ReadOnly    = true;
            rtbFlavor.BackColor   = SystemColors.Control;
            rtbFlavor.BorderStyle = BorderStyle.None;
            rtbFlavor.ScrollBars  = RichTextBoxScrollBars.Vertical;
            rtbFlavor.WordWrap    = true;
            rtbFlavor.Font        = new Font("Segoe UI", 10f, FontStyle.Italic);
            rtbFlavor.ForeColor   = SystemColors.GrayText;
            rtbFlavor.Visible     = false;

            // ---- pnlBottom ----
            pnlBottom.Dock          = DockStyle.Fill;
            pnlBottom.FlowDirection = FlowDirection.RightToLeft;
            pnlBottom.AutoSize      = true;
            pnlBottom.Margin        = new Padding(0, 4, 0, 0);
            pnlBottom.Controls.Add(btnClose);

            // ---- btnClose ----
            btnClose.Text    = "閉じる";
            btnClose.Size    = new Size(88, 28);
            btnClose.Margin  = new Padding(0);

            // ---- wire up table ----
            tableLayout.Controls.Add(lblTitle,   0, 0);
            tableLayout.Controls.Add(lblDescEn,  0, 1);
            tableLayout.Controls.Add(rtbDescEn,  0, 2);
            tableLayout.Controls.Add(lblDescJa,  0, 3);
            tableLayout.Controls.Add(rtbDescJa,  0, 4);
            tableLayout.Controls.Add(lblFlavor,  0, 5);
            tableLayout.Controls.Add(rtbFlavor,  0, 6);
            tableLayout.Controls.Add(pnlBottom,  0, 7);

            // ---- Form ----
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(520, 480);
            MinimumSize     = new Size(400, 360);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.CenterParent;
            ShowInTaskbar   = false;
            MaximizeBox     = true;
            MinimizeBox     = false;
            Text            = "詳細";
            Controls.Add(tableLayout);

            tableLayout.ResumeLayout();
            pnlBottom.ResumeLayout();
            ResumeLayout();
        }

        private TableLayoutPanel tableLayout;
        private Label            lblTitle;
        private Label            lblDescEn;
        private RichTextBox      rtbDescEn;
        private Label            lblDescJa;
        private RichTextBox      rtbDescJa;
        private Label            lblFlavor;
        private RichTextBox      rtbFlavor;
        private FlowLayoutPanel  pnlBottom;
        private Button           btnClose;
    }
}
