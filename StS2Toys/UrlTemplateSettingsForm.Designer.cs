namespace StS2Toys
{
    partial class UrlTemplateSettingsForm
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
            _grid = new DataGridView();
            _colLabel = new DataGridViewTextBoxColumn();
            _colType = new DataGridViewComboBoxColumn();
            _colTemplate = new DataGridViewTextBoxColumn();
            _bottom = new FlowLayoutPanel();
            _btnOk = new Button();
            _btnCancel = new Button();
            _lblHelp = new Label();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            _bottom.SuspendLayout();
            SuspendLayout();
            //
            // _grid
            //
            _grid.AllowUserToAddRows = true;
            _grid.AllowUserToDeleteRows = true;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.AddRange(_colLabel, _colType, _colTemplate);
            _grid.Dock = DockStyle.Fill;
            _grid.Name = "_grid";
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.TabIndex = 0;
            //
            // _colLabel
            //
            _colLabel.DataPropertyName = "Label";
            _colLabel.HeaderText = "名前";
            _colLabel.Name = "_colLabel";
            _colLabel.Width = 120;
            //
            // _colType
            //
            _colType.DataPropertyName = "Type";
            _colType.FlatStyle = FlatStyle.Flat;
            _colType.HeaderText = "種別";
            _colType.Items.AddRange("any", "card", "relic", "potion", "monster", "event", "encounter");
            _colType.Name = "_colType";
            _colType.Width = 110;
            //
            // _colTemplate
            //
            _colTemplate.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _colTemplate.DataPropertyName = "Template";
            _colTemplate.HeaderText = "テンプレート（{id} {en} {jp} {idraw} {idrawlower} {cardclass}）";
            _colTemplate.Name = "_colTemplate";
            //
            // _bottom
            //
            _bottom.AutoSize = true;
            _bottom.Controls.Add(_btnOk);
            _bottom.Controls.Add(_btnCancel);
            _bottom.Controls.Add(_lblHelp);
            _bottom.Dock = DockStyle.Bottom;
            _bottom.FlowDirection = FlowDirection.RightToLeft;
            _bottom.Name = "_bottom";
            _bottom.Padding = new Padding(8);
            _bottom.TabIndex = 1;
            //
            // _btnOk
            //
            _btnOk.AutoSize = true;
            _btnOk.DialogResult = DialogResult.OK;
            _btnOk.Name = "_btnOk";
            _btnOk.TabIndex = 0;
            _btnOk.Text = "OK";
            //
            // _btnCancel
            //
            _btnCancel.AutoSize = true;
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.Name = "_btnCancel";
            _btnCancel.TabIndex = 1;
            _btnCancel.Text = "キャンセル";
            //
            // _lblHelp
            //
            _lblHelp.AutoSize = true;
            _lblHelp.Margin = new Padding(8, 8, 8, 0);
            _lblHelp.Name = "_lblHelp";
            _lblHelp.Text = "種別 any は全エンティティに適用。日本語名などは自動 URL エンコードされます。";
            //
            // UrlTemplateSettingsForm
            //
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            Size = new Size(760, 420);
            Controls.Add(_grid);
            Controls.Add(_bottom);
            Name = "UrlTemplateSettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "URL テンプレート設定";
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            _bottom.ResumeLayout(false);
            _bottom.PerformLayout();
            ResumeLayout(false);
        }

        private DataGridView _grid;
        private DataGridViewTextBoxColumn _colLabel;
        private DataGridViewComboBoxColumn _colType;
        private DataGridViewTextBoxColumn _colTemplate;
        private FlowLayoutPanel _bottom;
        private Button _btnOk;
        private Button _btnCancel;
        private Label _lblHelp;
    }
}
