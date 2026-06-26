namespace StS2Toys
{
    partial class HpHistoryForm
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            _splitContainer = new SplitContainer();
            _chartBox       = new PictureBox();
            _listView       = new ListView();
            colAct          = new ColumnHeader();
            colFloor        = new ColumnHeader();
            colType         = new ColumnHeader();
            colHp           = new ColumnHeader();
            colChange       = new ColumnHeader();
            colDamage       = new ColumnHeader();
            colHeal         = new ColumnHeader();

            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            SuspendLayout();

            // ---- _splitContainer ----
            _splitContainer.Dock             = DockStyle.Fill;
            _splitContainer.Orientation      = Orientation.Horizontal;
            _splitContainer.SplitterDistance = 200;
            _splitContainer.Panel1MinSize    = 80;
            _splitContainer.Panel2MinSize    = 60;

            // ---- _chartBox ----
            _chartBox.Dock     = DockStyle.Fill;
            _chartBox.SizeMode = PictureBoxSizeMode.Normal;
            _splitContainer.Panel1.Controls.Add(_chartBox);

            // ---- _listView ----
            _listView.Columns.AddRange(new ColumnHeader[] { colAct, colFloor, colType, colHp, colChange, colDamage, colHeal });
            _listView.Dock                            = DockStyle.Fill;
            _listView.FullRowSelect                   = true;
            _listView.GridLines                       = true;
            _listView.View                            = View.Details;
            _listView.UseCompatibleStateImageBehavior = false;
            _splitContainer.Panel2.Controls.Add(_listView);

            // ---- columns ----
            colAct.Text    = "Act";   colAct.Width    = 40;
            colFloor.Text  = "F";     colFloor.Width  = 36; colFloor.TextAlign  = HorizontalAlignment.Right;
            colType.Text   = "種別";   colType.Width   = 68;
            colHp.Text     = "HP";    colHp.Width     = 80; colHp.TextAlign     = HorizontalAlignment.Right;
            colChange.Text = "変動";   colChange.Width = 52; colChange.TextAlign = HorizontalAlignment.Right;
            colDamage.Text = "受ダメ"; colDamage.Width = 52; colDamage.TextAlign = HorizontalAlignment.Right;
            colHeal.Text   = "回復";   colHeal.Width   = 52; colHeal.TextAlign   = HorizontalAlignment.Right;

            _splitContainer.Panel1.ResumeLayout();
            _splitContainer.Panel2.ResumeLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout();

            // ---- Form ----
            AutoScaleMode   = AutoScaleMode.Font;
            ClientSize      = new Size(420, 480);
            MinimumSize     = new Size(300, 300);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            MaximizeBox     = true;
            MinimizeBox     = false;
            Text            = "HP変動";
            Controls.Add(_splitContainer);

            ResumeLayout();
        }

        private SplitContainer _splitContainer;
        private PictureBox     _chartBox;
        private ListView       _listView;
        private ColumnHeader   colAct;
        private ColumnHeader   colFloor;
        private ColumnHeader   colType;
        private ColumnHeader   colHp;
        private ColumnHeader   colChange;
        private ColumnHeader   colDamage;
        private ColumnHeader   colHeal;
    }
}
