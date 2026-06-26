namespace StS2Toys
{
    partial class DeckOverviewForm
    {
        private System.ComponentModel.IContainer components = null!;

        private void InitializeComponent()
        {
            _charPanel = new FlowLayoutPanel();
            _charLabel = new Label();
            _charSelector = new ComboBox();
            _statsPanel = new Panel();
            _statsLabel = new Label();
            _scrollPanel = new Panel();
            _pictureBox = new PictureBox();
            _charPanel.SuspendLayout();
            _statsPanel.SuspendLayout();
            _scrollPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_pictureBox).BeginInit();
            SuspendLayout();
            //
            // _charPanel
            //
            _charPanel.AutoSize = true;
            _charPanel.Controls.Add(_charLabel);
            _charPanel.Controls.Add(_charSelector);
            _charPanel.Dock = DockStyle.Top;
            _charPanel.Name = "_charPanel";
            _charPanel.Padding = new Padding(6, 4, 6, 4);
            _charPanel.TabIndex = 2;
            _charPanel.Visible = false;
            //
            // _charLabel
            //
            _charLabel.AutoSize = true;
            _charLabel.Margin = new Padding(0, 6, 4, 0);
            _charLabel.Name = "_charLabel";
            _charLabel.TabIndex = 0;
            _charLabel.Text = "キャラ:";
            //
            // _charSelector
            //
            _charSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            _charSelector.Name = "_charSelector";
            _charSelector.TabIndex = 1;
            _charSelector.Width = 160;
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
            Controls.Add(_charPanel);
            MinimumSize = new Size(300, 200);
            Name = "DeckOverviewForm";
            Text = "デッキ枚数理論値";
            _charPanel.ResumeLayout(false);
            _charPanel.PerformLayout();
            _statsPanel.ResumeLayout(false);
            _scrollPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private FlowLayoutPanel _charPanel;
        private Label _charLabel;
        private ComboBox _charSelector;
        private Panel _statsPanel;
        private Label _statsLabel;
        private Panel _scrollPanel;
        private PictureBox _pictureBox;
    }
}
