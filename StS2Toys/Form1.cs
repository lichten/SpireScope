using StS2Toys.Models;
using StS2Toys.Services;

namespace StS2Toys
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
        }

        void Form1_Load(object? sender, EventArgs e)
        {
            var defaultPath = SaveDataService.GetDefaultSavePath();
            if (File.Exists(defaultPath))
                OpenFile(defaultPath);
        }

        void BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "セーブファイルを選択",
                Filter = "セーブファイル (*.save)|*.save|すべてのファイル (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(SaveDataService.GetDefaultSavePath()),
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                OpenFile(dialog.FileName);
        }

        void OpenFile(string path)
        {
            try
            {
                var data = SaveDataService.Load(path);
                txtFilePath.Text = path;
                DisplayData(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void DisplayData(RunSaveData data)
        {
            if (data.Players.Count == 0) return;
            var player = data.Players[0];

            var characterEn = CardDatabaseService.GetName(player.CharacterId, japanese: false);
            var characterJa = CardDatabaseService.GetName(player.CharacterId, japanese: true);
            lblInfo.Text =
                $"キャラクター: {characterJa} ({characterEn})　" +
                $"アセンション: {data.Ascension}　" +
                $"Act: {data.CurrentActIndex + 1}　　" +
                $"HP: {player.CurrentHp}/{player.MaxHp}　" +
                $"ゴールド: {player.Gold}　" +
                $"エネルギー: {player.MaxEnergy}";

            DisplayDeck(player);
            DisplayRelics(player);
        }

        void DisplayDeck(PlayerData player)
        {
            var grouped = player.Deck
                .GroupBy(c => c.Id)
                .OrderBy(g => CardDatabaseService.GetName(g.Key, japanese: true))
                .Select(g => (
                    Id:    g.Key,
                    En:    CardDatabaseService.GetName(g.Key, japanese: false),
                    Ja:    CardDatabaseService.GetName(g.Key, japanese: true),
                    Count: g.Count()))
                .ToList();

            lblDeckTitle.Text = $"デッキ ({player.Deck.Count}枚)";

            listViewDeck.BeginUpdate();
            listViewDeck.Items.Clear();
            foreach (var (id, en, ja, count) in grouped)
            {
                var item = new ListViewItem(en);
                item.SubItems.Add(ja);
                item.SubItems.Add(count.ToString());
                item.Tag = id;
                listViewDeck.Items.Add(item);
            }
            listViewDeck.EndUpdate();
        }

        void DisplayRelics(PlayerData player)
        {
            lblRelicsTitle.Text = $"レリック ({player.Relics.Count}個)";

            listViewRelics.BeginUpdate();
            listViewRelics.Items.Clear();
            foreach (var relic in player.Relics)
            {
                var item = new ListViewItem(CardDatabaseService.GetName(relic.Id, japanese: false));
                item.SubItems.Add(CardDatabaseService.GetName(relic.Id, japanese: true));
                item.Tag = relic.Id;
                listViewRelics.Items.Add(item);
            }
            listViewRelics.EndUpdate();
        }

        void ListViewDeck_ItemActivate(object? sender, EventArgs e)
        {
            if (listViewDeck.SelectedItems.Count == 0) return;
            if (listViewDeck.SelectedItems[0].Tag is not string id) return;
            using var dlg = new CardDetailForm(id, isRelic: false);
            dlg.ShowDialog(this);
        }

        void ListViewRelics_ItemActivate(object? sender, EventArgs e)
        {
            if (listViewRelics.SelectedItems.Count == 0) return;
            if (listViewRelics.SelectedItems[0].Tag is not string id) return;
            using var dlg = new CardDetailForm(id, isRelic: true);
            dlg.ShowDialog(this);
        }
    }
}
