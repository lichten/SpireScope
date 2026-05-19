using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using SixLabors.ImageSharp.Formats.Png;
using StS2Shared.Services;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISImage = SixLabors.ImageSharp.Image;
using ISRgba32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using WinColor = System.Drawing.Color;
using WinImage = System.Drawing.Image;
using WinSize = System.Drawing.Size;

namespace StS2EventBrowser;

record EventOption(string Title, string Description);
record EventInfo(string Id, string Title, string Description, List<EventOption> Options);
record AncientInfo(string Id, string Title, string Epithet, Dictionary<string, string> Keys);
record TalkLine(string Char, int Visit, int Line, bool IsRandom, string Speaker, string Text);

// ListBox の各行を表す。IsHeader == true の行はグループ見出し（選択不可）
sealed record EventListItem(bool IsHeader, string Label, EventInfo? Event = null);

public partial class EventBrowserForm : Form
{
    readonly string _toolsRoot;
    List<EventInfo> _allEvents = [];
    List<EventListItem> _listItems = [];
    List<AncientInfo> _allAncients = [];
    bool _isJp = true;
    string? _selectedEventId;
    string? _selectedAncientId;

    public EventBrowserForm()
    {
        InitializeComponent();
        _toolsRoot = FindToolsRoot();
        _filterBox.TextChanged += (_, _) => PopulateList();
        _btnJp.Click += (_, _) => SwitchLang(true);
        _btnEn.Click += (_, _) => SwitchLang(false);
        _eventList.DrawItem += DrawEventListItem;
        _eventList.SelectedIndexChanged += (_, _) => ShowSelected();
        _ancientList.SelectedIndexChanged += (_, _) => ShowSelectedAncient();
        UpdateLangButtons();
        LoadEvents();
        LoadAncients();
        PopulateList();
        PopulateAncientList();
    }

    void SwitchLang(bool isJp)
    {
        _isJp = isJp;
        UpdateLangButtons();
        LoadEvents();
        LoadAncients();
        PopulateList();
        PopulateAncientList();
        if (_selectedAncientId != null)
        {
            var a = _allAncients.FirstOrDefault(x => x.Id == _selectedAncientId);
            if (a != null) ShowAncient(a);
        }
    }

    void UpdateLangButtons()
    {
        _btnJp.Font = new Font("Segoe UI", 9f, _isJp ? FontStyle.Bold : FontStyle.Regular);
        _btnEn.Font = new Font("Segoe UI", 9f, _isJp ? FontStyle.Regular : FontStyle.Bold);
        _btnJp.FlatAppearance.BorderSize = _isJp ? 2 : 1;
        _btnEn.FlatAppearance.BorderSize = _isJp ? 1 : 2;
    }

    // ── Event data loading ────────────────────────────────────────────

    void LoadEvents()
    {
        var lang = _isJp ? "jpn" : "eng";
        var jsonPath = Path.Combine(_toolsRoot, "localization", lang, "events.json");
        if (!File.Exists(jsonPath)) return;

        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var byId = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in root.EnumerateObject())
        {
            var dotIdx = prop.Name.IndexOf('.');
            if (dotIdx < 0) continue;
            var id = prop.Name[..dotIdx];
            var subKey = prop.Name[(dotIdx + 1)..];
            if (!byId.TryGetValue(id, out var sub))
                byId[id] = sub = [];
            sub[subKey] = prop.Value.GetString() ?? "";
        }

        static bool Skip(string id) => id is "DEPRECATED_EVENT" or "MOCK_EVENT_MODEL"
            or "ERROR" or "PROCEED";

        _allEvents = [];
        foreach (var (id, keys) in byId)
        {
            if (!keys.TryGetValue("title", out var title) || string.IsNullOrWhiteSpace(title)) continue;
            if (Skip(id)) continue;
            var desc = ResolveInitialDescription(keys);
            var options = ResolveInitialOptions(keys);
            _allEvents.Add(new EventInfo(id, title, desc, options));
        }
        _allEvents.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCulture));
    }

    static string ResolveInitialDescription(Dictionary<string, string> keys)
    {
        if (keys.TryGetValue("pages.INITIAL.description", out var d) && !string.IsNullOrEmpty(d))
            return StripTags(d);
        foreach (var k in keys.Keys.Where(k => k.EndsWith(".description") && k.StartsWith("pages.")))
        {
            if (!string.IsNullOrEmpty(keys[k])) return StripTags(keys[k]);
        }
        return "";
    }

    static List<EventOption> ResolveInitialOptions(Dictionary<string, string> keys)
    {
        var opts = new List<EventOption>();
        const string prefix = "pages.INITIAL.options.";
        foreach (var k in keys.Keys.Where(k => k.StartsWith(prefix) && k.EndsWith(".title")))
        {
            var base_ = k[..^".title".Length];
            var t = StripTags(keys[k]);
            var d = keys.TryGetValue(base_ + ".description", out var od) ? StripTags(od) : "";
            if (!string.IsNullOrWhiteSpace(t))
                opts.Add(new EventOption(t, d));
        }
        return opts;
    }

    // ── Ancient data loading ──────────────────────────────────────────

    void LoadAncients()
    {
        var lang = _isJp ? "jpn" : "eng";
        var jsonPath = Path.Combine(_toolsRoot, "localization", lang, "ancients.json");
        if (!File.Exists(jsonPath)) return;

        var json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var byId = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in root.EnumerateObject())
        {
            var dotIdx = prop.Name.IndexOf('.');
            if (dotIdx < 0) continue;
            var id = prop.Name[..dotIdx];
            if (id is "ERROR" or "PROCEED") continue;
            var subKey = prop.Name[(dotIdx + 1)..];
            if (!byId.TryGetValue(id, out var sub))
                byId[id] = sub = [];
            sub[subKey] = prop.Value.GetString() ?? "";
        }

        _allAncients = [];
        foreach (var (id, keys) in byId)
        {
            if (!keys.TryGetValue("title", out var title) || string.IsNullOrWhiteSpace(title)) continue;
            var epithet = keys.TryGetValue("epithet", out var ep) ? ep : "";
            _allAncients.Add(new AncientInfo(id, title, epithet, keys));
        }
        _allAncients.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCulture));
    }

    static List<TalkLine> ParseTalkLines(Dictionary<string, string> keys)
    {
        var result = new List<TalkLine>();
        var re = new Regex(@"^(\d+)-(\d+)(r?)$");
        foreach (var (key, text) in keys)
        {
            // key format: "talk.CHAR.VISIT-LINEr?.SPEAKER"
            var parts = key.Split('.');
            if (parts.Length != 4 || parts[0] != "talk") continue;
            var charName = parts[1];
            var speaker = parts[3];
            var m = re.Match(parts[2]);
            if (!m.Success) continue;
            var visit = int.Parse(m.Groups[1].Value);
            var line = int.Parse(m.Groups[2].Value);
            var isRandom = m.Groups[3].Value == "r";
            result.Add(new TalkLine(charName, visit, line, isRandom, speaker, text));
        }
        return result;
    }

    // ── Event list UI ─────────────────────────────────────────────────

    void PopulateList()
    {
        var filter = _filterBox.Text.Trim();
        var matched = string.IsNullOrEmpty(filter)
            ? _allEvents
            : _allEvents.Where(e => e.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                                 || e.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        var groups = StS2Shared.Services.EventActService.Groups;
        _listItems = BuildListItems(matched, groups);

        _eventList.BeginUpdate();
        _eventList.Items.Clear();
        foreach (var item in _listItems)
            _eventList.Items.Add(item.Label);
        _eventList.EndUpdate();

        _countLabel.Text = $"{matched.Count} / {_allEvents.Count} イベント";

        if (_selectedEventId != null)
        {
            var idx = _listItems.FindIndex(item => item.Event?.Id == _selectedEventId);
            if (idx >= 0) _eventList.SelectedIndex = idx;
        }
    }

    List<EventListItem> BuildListItems(
        IEnumerable<EventInfo> events,
        IReadOnlyList<StS2Shared.Services.EventActService.ActGroup> groups)
    {
        var eventList = events.ToList();
        var result = new List<EventListItem>();

        if (groups.Count == 0)
        {
            // event_acts.json が空の場合はグループなしでフラット表示
            foreach (var ev in eventList)
                result.Add(new EventListItem(false, ev.Title, ev));
            return result;
        }

        var remaining = eventList.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var inGroup = eventList
                .Where(e => group.Events.Contains(e.Id))
                .ToList();
            if (inGroup.Count == 0) continue;

            result.Add(new EventListItem(true, _isJp ? group.NameJp : group.NameEn));
            foreach (var ev in inGroup)
            {
                result.Add(new EventListItem(false, ev.Title, ev));
                remaining.Remove(ev.Id);
            }
        }

        // event_acts.json に未登録のイベントは末尾に追加（元の並び順を維持）
        var uncategorized = eventList.Where(e => remaining.ContainsKey(e.Id)).ToList();
        if (uncategorized.Count > 0)
        {
            result.Add(new EventListItem(true, _isJp ? "未分類" : "Uncategorized"));
            foreach (var ev in uncategorized)
                result.Add(new EventListItem(false, ev.Title, ev));
        }

        return result;
    }

    void ShowSelected()
    {
        var idx = _eventList.SelectedIndex;
        if (idx < 0 || idx >= _listItems.Count) return;
        var item = _listItems[idx];
        if (item.IsHeader) return;
        _selectedEventId = item.Event!.Id;
        ShowEvent(item.Event);
    }

    static readonly Color HeaderBg = Color.FromArgb(60, 70, 90);
    static readonly Color HeaderFg = Color.FromArgb(210, 220, 235);

    void DrawEventListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _listItems.Count) return;
        var item = _listItems[e.Index];

        if (item.IsHeader)
        {
            using var bg = new SolidBrush(HeaderBg);
            e.Graphics.FillRectangle(bg, e.Bounds);
            using var font = new Font(e.Font!, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, item.Label, font, e.Bounds, HeaderFg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
        else
        {
            e.DrawBackground();
            var indent = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, item.Label, e.Font, indent, e.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            e.DrawFocusRectangle();
        }
    }

    // ── Ancient list UI ───────────────────────────────────────────────

    void PopulateAncientList()
    {
        _ancientList.BeginUpdate();
        _ancientList.Items.Clear();
        foreach (var a in _allAncients)
            _ancientList.Items.Add($"{a.Title}  —  {a.Epithet}");
        _ancientList.EndUpdate();

        if (_selectedAncientId != null)
        {
            var idx = _allAncients.FindIndex(a => a.Id == _selectedAncientId);
            if (idx >= 0) _ancientList.SelectedIndex = idx;
        }
    }

    void ShowSelectedAncient()
    {
        var idx = _ancientList.SelectedIndex;
        if (idx < 0 || idx >= _allAncients.Count) return;
        var a = _allAncients[idx];
        _selectedAncientId = a.Id;
        ShowAncient(a);
    }

    // ── Event detail view ─────────────────────────────────────────────

    void ShowEvent(EventInfo ev)
    {
        var old = _pictureBox.Image;
        _pictureBox.Image = null;
        old?.Dispose();

        Task.Run(() => TryLoadEventImage(ev.Id)).ContinueWith(t =>
        {
            if (t.Result is WinImage img && _selectedEventId == ev.Id)
            {
                var prev = _pictureBox.Image;
                _pictureBox.Image = img;
                prev?.Dispose();
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());

        _textBox.SuspendLayout();
        _textBox.Clear();

        using (var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold))
        {
            _textBox.SelectionFont = titleFont;
            _textBox.SelectionColor = WinColor.FromArgb(20, 20, 60);
            _textBox.AppendText(ev.Title + "\n\n");
        }

        if (!string.IsNullOrEmpty(ev.Description))
        {
            _textBox.SelectionFont = _textBox.Font;
            _textBox.SelectionColor = SystemColors.ControlText;
            _textBox.AppendText(ev.Description + "\n");
        }

        if (ev.Options.Count > 0)
        {
            _textBox.AppendText("\n");
            using var boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _textBox.SelectionFont = boldFont;
            _textBox.SelectionColor = WinColor.FromArgb(40, 40, 120);
            _textBox.AppendText(_isJp ? "選択肢\n" : "Options\n");

            foreach (var opt in ev.Options)
            {
                _textBox.SelectionFont = boldFont;
                _textBox.SelectionColor = SystemColors.ControlText;
                _textBox.AppendText($"◆ {opt.Title}");

                if (!string.IsNullOrWhiteSpace(opt.Description))
                {
                    _textBox.SelectionFont = _textBox.Font;
                    _textBox.AppendText($": {opt.Description}");
                }
                _textBox.AppendText("\n");
            }
        }

        _textBox.SelectionStart = 0;
        _textBox.ScrollToCaret();
        _textBox.ResumeLayout();
    }

    // ── Ancient detail view ───────────────────────────────────────────

    static readonly WinColor ColorSection   = WinColor.FromArgb(80, 40, 0);
    static readonly WinColor ColorVisit     = WinColor.FromArgb(60, 60, 100);
    static readonly WinColor ColorAncient   = WinColor.FromArgb(140, 90, 0);
    static readonly WinColor ColorPlayer    = WinColor.FromArgb(20, 60, 140);
    static readonly WinColor ColorNext      = WinColor.FromArgb(100, 100, 100);

    void ShowAncient(AncientInfo a)
    {
        var old = _ancientPictureBox.Image;
        _ancientPictureBox.Image = null;
        old?.Dispose();

        Task.Run(() => TryLoadAncientImage(a.Id)).ContinueWith(t =>
        {
            if (t.Result is WinImage img && _selectedAncientId == a.Id)
            {
                var prev = _ancientPictureBox.Image;
                _ancientPictureBox.Image = img;
                prev?.Dispose();
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());

        var tb = _ancientTextBox;
        tb.SuspendLayout();
        tb.Clear();

        // Title
        using (var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold))
        {
            tb.SelectionFont = titleFont;
            tb.SelectionColor = WinColor.FromArgb(20, 20, 60);
            tb.AppendText(a.Title + "\n");
        }

        // Epithet
        if (!string.IsNullOrEmpty(a.Epithet))
        {
            using var epithetFont = new Font("Segoe UI", 10f, FontStyle.Italic);
            tb.SelectionFont = epithetFont;
            tb.SelectionColor = WinColor.FromArgb(80, 80, 120);
            tb.AppendText(a.Epithet + "\n");
        }
        tb.AppendText("\n");

        // ── Special sections ──────────────────────────────────────────

        // VAKUU: loss text
        if (a.Keys.TryGetValue("loss", out var loss) && !string.IsNullOrEmpty(loss))
        {
            AppendSectionHeader(tb, _isJp ? "敗北時" : "On Loss");
            AppendNormal(tb, loss + "\n\n");
        }

        // THE_ARCHITECT: top-level special keys
        if (a.Keys.TryGetValue("CONTINUE", out var cont) && !string.IsNullOrEmpty(cont))
        {
            AppendSectionHeader(tb, "Continue");
            AppendNormal(tb, cont + "\n\n");
        }
        if (a.Keys.TryGetValue("RESPOND", out var respond) && !string.IsNullOrEmpty(respond))
        {
            AppendSectionHeader(tb, "Respond");
            AppendNormal(tb, respond + "\n\n");
        }

        // pages.INITIAL.description
        if (a.Keys.TryGetValue("pages.INITIAL.description", out var pageDesc) && !string.IsNullOrEmpty(pageDesc))
        {
            AppendSectionHeader(tb, _isJp ? "説明" : "Description");
            AppendNormal(tb, StripTags(pageDesc) + "\n\n");
        }

        // Options (OROBAS pages.INITIAL.options.*)
        var optionKeys = a.Keys.Keys
            .Where(k => k.StartsWith("pages.INITIAL.options.") && k.EndsWith(".title"))
            .ToList();
        if (optionKeys.Count > 0)
        {
            AppendSectionHeader(tb, _isJp ? "選択肢" : "Options");
            using var boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            foreach (var titleKey in optionKeys.Order())
            {
                var optBase = titleKey[..^".title".Length];
                var optTitle = StripTags(a.Keys[titleKey]);
                tb.SelectionFont = boldFont;
                tb.SelectionColor = SystemColors.ControlText;
                tb.AppendText("◆ " + optTitle);
                if (a.Keys.TryGetValue(optBase + ".description", out var optDesc) && !string.IsNullOrEmpty(optDesc))
                {
                    tb.SelectionFont = tb.Font;
                    tb.AppendText(": " + StripTags(optDesc));
                }
                tb.AppendText("\n");
            }
            tb.AppendText("\n");
        }

        // ── Possible rewards (from ancient_options.json / DLL extraction) ──

        if (!AncientOptionService.IsDataAvailable)
        {
            AppendSectionHeader(tb, _isJp ? "報酬候補" : "Possible Rewards");
            using var warnFont = new Font("Segoe UI", 9f, FontStyle.Italic);
            tb.SelectionFont = warnFont;
            tb.SelectionColor = WinColor.Gray;
            tb.AppendText(_isJp
                ? "データ未生成: card-type-extractor を実行してください。\n\n"
                : "Data unavailable: run card-type-extractor to generate.\n\n");
        }
        else
        {
            var groups = AncientOptionService.GetGroups(a.Id);
            if (groups != null && groups.Count > 0)
            {
                AppendSectionHeader(tb, _isJp ? "報酬候補" : "Possible Rewards");
                using var groupFont  = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var normalFont = new Font("Segoe UI", 9f, FontStyle.Regular);
                foreach (var (groupName, items) in groups.OrderBy(g => g.Key))
                {
                    // グループ名表示 (複数アイテムのみ)
                    if (items.Count > 1)
                    {
                        tb.SelectionFont = groupFont;
                        tb.SelectionColor = ColorSection;
                        tb.AppendText("  " + FormatGroupName(groupName) + "\n");
                    }

                    foreach (var itemId in items)
                    {
                        var name = GetAncientItemName(itemId);
                        tb.SelectionFont = normalFont;
                        tb.SelectionColor = SystemColors.ControlText;
                        tb.AppendText(items.Count > 1 ? $"    · {name}\n" : $"  · {name}\n");
                    }
                }
                tb.AppendText("\n");
            }
        }

        // ── Dialogue sections ─────────────────────────────────────────

        var talkLines = ParseTalkLines(a.Keys);
        if (talkLines.Count == 0)
        {
            tb.SelectionStart = 0;
            tb.ScrollToCaret();
            tb.ResumeLayout();
            return;
        }

        AppendSectionHeader(tb, _isJp ? "会話" : "Dialogue");
        tb.AppendText("\n");

        var charOrder = new[] { "firstVisitEver", "ANY", "DEFECT", "IRONCLAD", "NECROBINDER", "REGENT", "SILENT" };
        var byChar = talkLines.GroupBy(t => t.Char).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var charName in charOrder)
        {
            if (!byChar.TryGetValue(charName, out var lines)) continue;

            // Character header
            using (var charFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                tb.SelectionFont = charFont;
                tb.SelectionColor = ColorSection;
                tb.AppendText(CharDisplayName(charName) + "\n");
            }

            // Group by visit, preserving random flag
            var visitGroups = lines
                .GroupBy(t => t.Visit)
                .OrderBy(g => g.Key);

            foreach (var vg in visitGroups)
            {
                var visitLines = vg.ToList();
                var anyRandom = visitLines.Any(l => l.IsRandom);

                // Visit header
                using (var visitFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    tb.SelectionFont = visitFont;
                    tb.SelectionColor = ColorVisit;
                    var visitLabel = _isJp
                        ? $"  訪問 {vg.Key}{(anyRandom ? " (r)" : "")}\n"
                        : $"  Visit {vg.Key}{(anyRandom ? " (r)" : "")}\n";
                    tb.AppendText(visitLabel);
                }

                // Lines sorted by (line number, speaker priority)
                foreach (var tl in visitLines.OrderBy(t => t.Line).ThenBy(t => SpeakerOrder(t.Speaker)))
                {
                    tb.SelectionFont = tb.Font;
                    switch (tl.Speaker)
                    {
                        case "ancient":
                            tb.SelectionColor = ColorAncient;
                            tb.AppendText($"    [{(_isJp ? "Ancient" : "Ancient")}] {tl.Text}\n");
                            break;
                        case "char":
                            tb.SelectionColor = ColorPlayer;
                            tb.AppendText($"    [{(_isJp ? "Player" : "Player")}]  {tl.Text}\n");
                            break;
                        case "next":
                            tb.SelectionColor = ColorNext;
                            tb.AppendText($"    → {tl.Text}\n");
                            break;
                        default:
                            tb.SelectionColor = SystemColors.ControlText;
                            tb.AppendText($"    [{tl.Speaker}] {tl.Text}\n");
                            break;
                    }
                }
                tb.AppendText("\n");
            }
        }

        tb.SelectionStart = 0;
        tb.ScrollToCaret();
        tb.ResumeLayout();
    }

    static void AppendSectionHeader(RichTextBox tb, string text)
    {
        using var font = new Font("Segoe UI", 10f, FontStyle.Bold | FontStyle.Underline);
        tb.SelectionFont = font;
        tb.SelectionColor = ColorSection;
        tb.AppendText(text + "\n");
        tb.SelectionFont = tb.Font;
        tb.SelectionColor = SystemColors.ControlText;
    }

    static void AppendNormal(RichTextBox tb, string text)
    {
        tb.SelectionFont = tb.Font;
        tb.SelectionColor = SystemColors.ControlText;
        tb.AppendText(text);
    }

    static string CharDisplayName(string charName) => charName switch
    {
        "firstVisitEver" => "初回訪問 (First Visit Ever)",
        "ANY"            => "全キャラ共通 (Any)",
        "DEFECT"         => "Defect",
        "IRONCLAD"       => "Ironclad",
        "NECROBINDER"    => "Necrobinder",
        "REGENT"         => "Regent",
        "SILENT"         => "Silent",
        _                => charName,
    };

    static int SpeakerOrder(string speaker) => speaker switch
    {
        "ancient" => 0,
        "char"    => 1,
        "next"    => 2,
        _         => 3,
    };

    // ── Image loading ─────────────────────────────────────────────────

    WinImage? TryLoadEventImage(string eventId)
    {
        try { return LoadEventImage(eventId); }
        catch { return null; }
    }

    WinImage? LoadEventImage(string eventId)
    {
        var pngCacheDir = Path.Combine(_toolsRoot, "images", "events_png");
        Directory.CreateDirectory(pngCacheDir);
        var pngPath = Path.Combine(pngCacheDir, eventId.ToLowerInvariant() + ".png");

        if (!File.Exists(pngPath))
        {
            var importPath = Path.Combine(_toolsRoot, "images", "events",
                eventId.ToLowerInvariant() + ".png.import");
            if (!File.Exists(importPath)) return null;

            var ctexRelPath = ParseCtexPath(importPath);
            if (ctexRelPath is null) return null;

            var ctexFull = Path.Combine(_toolsRoot,
                ctexRelPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(ctexFull)) return null;

            ConvertCtex(ctexFull, pngPath);
        }

        var bytes = File.ReadAllBytes(pngPath);
        using var ms = new MemoryStream(bytes);
        return new System.Drawing.Bitmap(ms);
    }

    WinImage? TryLoadAncientImage(string ancientId)
    {
        try { return LoadAncientImage(ancientId); }
        catch { return null; }
    }

    WinImage? LoadAncientImage(string ancientId)
    {
        var pngCacheDir = Path.Combine(_toolsRoot, "images", "ancients_png");
        Directory.CreateDirectory(pngCacheDir);
        var pngPath = Path.Combine(pngCacheDir, ancientId.ToLowerInvariant() + ".png");

        if (!File.Exists(pngPath))
        {
            var importPath = Path.Combine(_toolsRoot, "images", "ancients",
                ancientId.ToLowerInvariant() + "_placeholder.png.import");
            if (!File.Exists(importPath)) return null;

            var ctexRelPath = ParseCtexPath(importPath);
            if (ctexRelPath is null) return null;

            var ctexFull = Path.Combine(_toolsRoot,
                ctexRelPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(ctexFull)) return null;

            ConvertCtex(ctexFull, pngPath);
        }

        var bytes = File.ReadAllBytes(pngPath);
        using var ms = new MemoryStream(bytes);
        return new System.Drawing.Bitmap(ms);
    }

    static string? ParseCtexPath(string importPath)
    {
        var content = File.ReadAllText(importPath);
        var m = Regex.Match(content, @"^path(?:\.\w+)?=""res://(.+?\.ctex)""",
            RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value : null;
    }

    static void ConvertCtex(string srcPath, string outPath)
    {
        var data = File.ReadAllBytes(srcPath);
        if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "GST2")
            throw new InvalidDataException("Not a GST2 ctex file");

        var width      = (int)BitConverter.ToUInt32(data, 8);
        var height     = (int)BitConverter.ToUInt32(data, 12);
        var dataFormat = BitConverter.ToUInt32(data, 36);

        const int Hdr = 52;
        using var img = dataFormat == 2
            ? LoadWebP(data, Hdr)
            : DecodeBc7(data, Hdr, width, height);
        using var outStream = File.OpenWrite(outPath);
        img.Save(outStream, new PngEncoder());
    }

    static ISRgba32 LoadWebP(byte[] data, int hdr)
    {
        var size = (int)BitConverter.ToUInt32(data, hdr);
        using var ms = new MemoryStream(data, hdr + 4, size);
        return ISImage.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(ms);
    }

    static ISRgba32 DecodeBc7(byte[] data, int hdr, int w, int h)
    {
        var bc7Data = new ReadOnlyMemory<byte>(data, hdr, data.Length - hdr);
        var decoder = new BcDecoder();
        var pixels  = decoder.DecodeRaw(bc7Data.ToArray(), w, h, CompressionFormat.Bc7);
        var bytes   = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
        return ISImage.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes, w, h);
    }

    // ── Utilities ─────────────────────────────────────────────────────

    // "OptionPool1" → "Pool 1"、"BaseOptionPool" → "Base Option Pool" 等に整形
    static string FormatGroupName(string raw)
    {
        // キャメルケースをスペース区切りに変換
        var spaced = Regex.Replace(raw, @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        spaced = Regex.Replace(spaced, @"(?<=[a-zA-Z])(?=\d)", " ");
        return spaced;
    }

    // アイテム ID（CARD.XXX or リレックス ID）をローカライズ名に変換
    string GetAncientItemName(string itemId)
    {
        if (itemId.StartsWith("CARD.", StringComparison.OrdinalIgnoreCase))
            return CardDatabaseService.GetName(itemId, _isJp);
        return CardDatabaseService.GetRelicTitle(itemId, _isJp);
    }

    static string StripTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var s = Regex.Replace(text, @"\[[^\]]*\]", "");
        s = Regex.Replace(s, @"\{[^}]+\}", "?");
        return s.Trim();
    }

    static string FindToolsRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "tools", "extracted")))
                return Path.Combine(dir, "tools", "extracted");
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("tools/extracted が見つかりません");
    }
}
