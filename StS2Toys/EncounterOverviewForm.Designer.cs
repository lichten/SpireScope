namespace StS2Toys
{
    partial class EncounterOverviewForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            _actPanel    = new FlowLayoutPanel();
            _actLabel    = new Label();
            _actSelector = new ComboBox();
            _scrollPanel = new Panel();
            _pictureBox  = new PictureBox();

            _actPanel.SuspendLayout();
            _scrollPanel.SuspendLayout();
            SuspendLayout();

            // ---- _actPanel ----
            _actPanel.Dock          = DockStyle.Top;
            _actPanel.AutoSize      = true;
            _actPanel.FlowDirection = FlowDirection.LeftToRight;
            _actPanel.Padding       = new Padding(6, 4, 6, 4);
            _actPanel.Controls.Add(_actLabel);
            _actPanel.Controls.Add(_actSelector);

            // ---- _actLabel ----
            _actLabel.AutoSize = true;
            _actLabel.Margin   = new Padding(0, 6, 4, 0);
            _actLabel.Text     = "Act:";

            // ---- _actSelector ----
            _actSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            _actSelector.Width         = 220;

            // ---- _scrollPanel ----
            _scrollPanel.Dock       = DockStyle.Fill;
            _scrollPanel.AutoScroll = true;
            _scrollPanel.Controls.Add(_pictureBox);

            // ---- _pictureBox ----
            _pictureBox.Location = new Point(0, 0);
            _pictureBox.SizeMode = PictureBoxSizeMode.Normal;

            // ---- Form ----
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(420, 460);
            MinimumSize     = new Size(320, 200);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            MaximizeBox     = true;
            MinimizeBox     = false;
            Text            = "エンカウンター情報";
            Controls.Add(_scrollPanel);
            Controls.Add(_actPanel);

            _actPanel.ResumeLayout(false);
            _actPanel.PerformLayout();
            _scrollPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private FlowLayoutPanel _actPanel;
        private Label           _actLabel;
        private ComboBox        _actSelector;
        private Panel           _scrollPanel;
        private PictureBox      _pictureBox;
    }
}
