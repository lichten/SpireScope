namespace SpireScope
{
    partial class SetupWizardForm
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
            _root = new TableLayoutPanel();
            _lblTitle = new Label();
            _lblDesc = new Label();
            _lblStatus = new Label();
            _btnBrowse = new Button();
            _lblProgress = new Label();
            _progress = new ProgressBar();
            _bottom = new FlowLayoutPanel();
            _btnStart = new Button();
            _btnCancel = new Button();
            _btnSkip = new Button();
            _root.SuspendLayout();
            _bottom.SuspendLayout();
            SuspendLayout();
            //
            // _root
            //
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.Dock = DockStyle.Fill;
            _root.Name = "_root";
            _root.Padding = new Padding(12);
            _root.RowCount = 8;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // title
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // desc
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // status
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // browse
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // spacer
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // progress label
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // progress bar
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // buttons
            _root.Controls.Add(_lblTitle, 0, 0);
            _root.Controls.Add(_lblDesc, 0, 1);
            _root.Controls.Add(_lblStatus, 0, 2);
            _root.Controls.Add(_btnBrowse, 0, 3);
            _root.Controls.Add(_lblProgress, 0, 5);
            _root.Controls.Add(_progress, 0, 6);
            _root.Controls.Add(_bottom, 0, 7);
            //
            // _lblTitle
            //
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new Font("Yu Gothic UI", 12F, FontStyle.Bold);
            _lblTitle.Margin = new Padding(3, 3, 3, 8);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Text = "画像アセットのセットアップ";
            //
            // _lblDesc
            //
            _lblDesc.AutoSize = true;
            _lblDesc.MaximumSize = new Size(460, 0);
            _lblDesc.Margin = new Padding(3, 3, 3, 8);
            _lblDesc.Name = "_lblDesc";
            _lblDesc.Text = "説明";
            //
            // _lblStatus
            //
            _lblStatus.AutoSize = true;
            _lblStatus.MaximumSize = new Size(460, 0);
            _lblStatus.Margin = new Padding(3, 3, 3, 6);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Text = "検出中...";
            //
            // _btnBrowse
            //
            _btnBrowse.AutoSize = true;
            _btnBrowse.Name = "_btnBrowse";
            _btnBrowse.Text = "参照...";
            //
            // _lblProgress
            //
            _lblProgress.AutoSize = true;
            _lblProgress.Margin = new Padding(3, 6, 3, 3);
            _lblProgress.Name = "_lblProgress";
            _lblProgress.Text = "";
            //
            // _progress
            //
            _progress.Dock = DockStyle.Fill;
            _progress.Margin = new Padding(3);
            _progress.Name = "_progress";
            _progress.Height = 20;
            //
            // _bottom
            //
            _bottom.AutoSize = true;
            _bottom.Controls.Add(_btnStart);
            _bottom.Controls.Add(_btnCancel);
            _bottom.Controls.Add(_btnSkip);
            _bottom.Dock = DockStyle.Fill;
            _bottom.FlowDirection = FlowDirection.RightToLeft;
            _bottom.Margin = new Padding(0, 6, 0, 0);
            _bottom.Name = "_bottom";
            //
            // _btnStart
            //
            _btnStart.AutoSize = true;
            _btnStart.Enabled = false;
            _btnStart.Name = "_btnStart";
            _btnStart.Text = "開始";
            //
            // _btnCancel
            //
            _btnCancel.AutoSize = true;
            _btnCancel.Enabled = false;
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Text = "キャンセル";
            //
            // _btnSkip
            //
            _btnSkip.AutoSize = true;
            _btnSkip.Name = "_btnSkip";
            _btnSkip.Text = "スキップ";
            //
            // SetupWizardForm
            //
            AcceptButton = _btnStart;
            CancelButton = _btnSkip;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(500, 320);
            Controls.Add(_root);
            MinimumSize = new Size(460, 320);
            Name = "SetupWizardForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SpireScope セットアップ";
            _root.ResumeLayout(false);
            _root.PerformLayout();
            _bottom.ResumeLayout(false);
            _bottom.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel _root;
        private Label _lblTitle;
        private Label _lblDesc;
        private Label _lblStatus;
        private Button _btnBrowse;
        private Label _lblProgress;
        private ProgressBar _progress;
        private FlowLayoutPanel _bottom;
        private Button _btnStart;
        private Button _btnCancel;
        private Button _btnSkip;
    }
}
