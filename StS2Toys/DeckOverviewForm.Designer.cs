namespace StS2Toys
{
    partial class DeckOverviewForm
    {
        private System.ComponentModel.IContainer components = null!;

        private void InitializeComponent()
        {
            _statsPanel = new Panel();
            _statsLabel = new Label();
            _scrollPanel = new Panel();
            _pictureBox = new PictureBox();
            _statsPanel.SuspendLayout();
            _scrollPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            SuspendLayout();
            //
            // _statsPanel
            //
            _statsPanel.Controls.Add(_statsLabel);
            _statsPanel.Dock = DockStyle.Top;
            _statsPanel.Name = "_statsPanel";
            _statsPanel.Size = new Size(640, 28);
            _statsPanel.TabIndex = 1;
            _statsPanel.Visible = false;
            //
            // _statsLabel
            //
            _statsLabel.Dock = DockStyle.Fill;
            _statsLabel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _statsLabel.Name = "_statsLabel";
            _statsLabel.TabIndex = 0;
            _statsLabel.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _scrollPanel
            //
            _scrollPanel.AutoScroll = true;
            _scrollPanel.Controls.Add(_pictureBox);
            _scrollPanel.Dock = DockStyle.Fill;
            _scrollPanel.Name = "_scrollPanel";
            _scrollPanel.TabIndex = 0;
            //
            // _pictureBox
            //
            _pictureBox.Location = new Point(0, 0);
            _pictureBox.Name = "_pictureBox";
            _pictureBox.SizeMode = PictureBoxSizeMode.Normal;
            _pictureBox.TabIndex = 0;
            _pictureBox.TabStop = false;
            //
            // DeckOverviewForm
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 500);
            Controls.Add(_scrollPanel);
            Controls.Add(_statsPanel);
            MinimumSize = new Size(300, 200);
            Name = "DeckOverviewForm";
            Text = "デッキ枚数理論値";
            _statsPanel.ResumeLayout(false);
            _scrollPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            ResumeLayout(false);
        }

        private Panel _statsPanel;
        private Label _statsLabel;
        private Panel _scrollPanel;
        private PictureBox _pictureBox;
    }
}
