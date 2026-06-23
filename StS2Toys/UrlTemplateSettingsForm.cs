using System.ComponentModel;
using StS2Shared.Services;
using StS2Toys.Services;

namespace StS2Toys;

/// <summary>
/// URL テンプレートの追加／編集／削除を行う簡易グリッド。OK で url_templates.json に保存する。
/// 使用可能トークン：{id} {en} {jp} {idraw} {idrawlower} {cardclass}。Type は
/// card/relic/potion/monster/event/encounter/any。
/// </summary>
public sealed class UrlTemplateSettingsForm : Form
{
    sealed class Row
    {
        public string Label { get; set; } = "";
        public string Type { get; set; } = "any";
        public string Template { get; set; } = "";
    }

    readonly BindingList<Row> _rows;
    readonly DataGridView _grid = new();

    public UrlTemplateSettingsForm()
    {
        Text = "URL テンプレート設定";
        Width = 760;
        Height = 420;
        StartPosition = FormStartPosition.CenterParent;

        _rows = new BindingList<Row>(
            UrlTemplateService.Load().Select(t => new Row { Label = t.Label, Type = t.Type, Template = t.Template }).ToList());

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名前", DataPropertyName = nameof(Row.Label), Width = 120 });
        var typeCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "種別",
            DataPropertyName = nameof(Row.Type),
            Width = 110,
            FlatStyle = FlatStyle.Flat,
        };
        typeCol.Items.AddRange("any", "card", "relic", "potion", "monster", "event", "encounter");
        _grid.Columns.Add(typeCol);
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "テンプレート（{id} {en} {jp} {idraw} {idrawlower} {cardclass}）",
            DataPropertyName = nameof(Row.Template),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        // ComboBox セルの未知値で例外を出さない。
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.DataSource = _rows;

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8),
        };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, AutoSize = true };
        var help = new Label
        {
            Text = "種別 any は全エンティティに適用。日本語名などは自動 URL エンコードされます。",
            AutoSize = true,
            Margin = new Padding(8, 8, 8, 0),
        };
        bottom.Controls.Add(ok);
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(help);

        Controls.Add(_grid);
        Controls.Add(bottom);
        AcceptButton = ok;
        CancelButton = cancel;

        ok.Click += (_, _) => Save();
    }

    void Save()
    {
        var list = _rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Template))
            .Select(r => new UrlTemplate(
                string.IsNullOrWhiteSpace(r.Label) ? "リンク" : r.Label.Trim(),
                string.IsNullOrWhiteSpace(r.Type) ? "any" : r.Type.Trim(),
                r.Template.Trim()))
            .ToList();
        UrlTemplateService.Save(list);
    }
}
