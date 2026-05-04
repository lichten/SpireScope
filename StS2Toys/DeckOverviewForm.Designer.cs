namespace StS2Toys
{
    partial class DeckOverviewForm
    {
        private System.ComponentModel.IContainer components = null!;

        private void InitializeComponent()
        {
            _scrollPanel = new Panel();
            _pictureBox = new PictureBox();
            _scrollPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            SuspendLayout();
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
            MinimumSize = new Size(300, 200);
            Name = "DeckOverviewForm";
            Text = "デッキ概観";
            _scrollPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            ResumeLayout(false);
        }

        private Panel _scrollPanel;
        private PictureBox _pictureBox;
    }
}
