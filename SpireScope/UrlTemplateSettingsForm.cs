using System.ComponentModel;
using StS2Shared.Services;
using SpireScope.Services;

namespace SpireScope;

/// <summary>
/// URL テンプレートの追加／編集／削除を行う簡易グリッド。OK で url_templates.json に保存する。
/// 使用可能トークン：{id} {en} {jp} {idraw} {idrawlower} {cardclass}。Type は
/// card/relic/potion/monster/event/encounter/any。
/// </summary>
public partial class UrlTemplateSettingsForm : Form
{
    sealed class Row
    {
        public string Label { get; set; } = "";
        public string Type { get; set; } = "any";
        public string Template { get; set; } = "";
    }

    readonly BindingList<Row> _rows;

    public UrlTemplateSettingsForm()
    {
        InitializeComponent();

        _rows = new BindingList<Row>(
            UrlTemplateService.Load().Select(t => new Row { Label = t.Label, Type = t.Type, Template = t.Template }).ToList());

        // ComboBox セルの未知値で例外を出さない。
        _grid.DataError += (_, e) => e.ThrowException = false;
        _grid.DataSource = _rows;

        _btnOk.Click += (_, _) => Save();
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
