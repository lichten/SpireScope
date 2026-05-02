using StS2Toys.Services;

namespace StS2Toys;

public partial class CardDetailForm : Form
{
    public CardDetailForm(string id, bool isRelic)
    {
        InitializeComponent();

        var en = CardDatabaseService.GetName(id, japanese: false);
        var ja = CardDatabaseService.GetName(id, japanese: true);
        lblTitle.Text = $"{en}  /  {ja}";
        Text = isRelic ? $"レリック: {ja}" : $"カード: {ja}";

        var (descEn, descJa) = CardDatabaseService.GetDescription(id);
        rtbDescEn.Text = DescriptionFormatter.Clean(descEn);
        rtbDescJa.Text = DescriptionFormatter.Clean(descJa);

        var flavor = CardDatabaseService.GetFlavor(id);
        if (flavor is { } f)
        {
            lblFlavor.Visible = true;
            rtbFlavor.Visible = true;
            rtbFlavor.Text    = $"{f.En}\n\n{f.Ja}";

            // 3つの RichTextBox を等分に再配分
            tableLayout.RowStyles[2].Height = 33.3f;
            tableLayout.RowStyles[4].Height = 33.3f;
            tableLayout.RowStyles[5].SizeType = SizeType.AutoSize;
            tableLayout.RowStyles[5].Height = 0f;
            tableLayout.RowStyles[6].SizeType = SizeType.Percent;
            tableLayout.RowStyles[6].Height = 33.4f;
        }

        btnClose.Click += (_, _) => Close();
    }
}
