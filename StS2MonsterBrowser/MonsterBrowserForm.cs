using SkiaSharp;

namespace StS2MonsterBrowser;

public class MonsterBrowserForm : Form
{
    readonly string _toolsRoot;
    readonly string _monstersDir;

    // Controls
    readonly ListBox _monsterList = new() { Dock = DockStyle.Fill, Font = new Font("Consolas", 9) };
    readonly PictureBox _view = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 35), SizeMode = PictureBoxSizeMode.Zoom };
    readonly ComboBox _animCombo = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TrackBar _timeSlider = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000, TickFrequency = 100 };
    readonly Button _playBtn = new() { Text = "▶ Play", Width = 80 };
    readonly Button _stopBtn = new() { Text = "■ Stop", Width = 80, Enabled = false };
    readonly Label _timeLbl = new() { Text = "0.00s", Width = 55, TextAlign = ContentAlignment.MiddleRight };
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };

    MonsterData? _current;
    float _animTime;
    float _animDuration;
    bool _playing;

    public MonsterBrowserForm()
    {
        Text = "StS2 Monster Browser";
        Size = new Size(1100, 750);
        MinimumSize = new Size(800, 550);

        _toolsRoot = FindToolsRoot();
        _monstersDir = Path.Combine(_toolsRoot, "animations", "monsters");

        BuildLayout();
        LoadMonsterList();
        _timer.Tick += OnTick;
    }

    void BuildLayout()
    {
        var splitter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 220,
        };
        splitter.Panel1.Controls.Add(_monsterList);

        // 右ペイン: ビュー(可変) + コントロール行(固定高)
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2, ColumnCount = 1,
            Margin = Padding.Empty,
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        // コントロール行: 列幅を固定し、スライダーだけを可変にして折り返しをなくす
        //   [Animation:] [Combo(175px)] [Slider(可変)] [0.00s(52px)] [▶Play(82px)] [■Stop(82px)]
        var ctrlBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1, ColumnCount = 6,
            Padding = new Padding(4, 2, 4, 2),
            Margin = Padding.Empty,
        };
        ctrlBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // "Animation:" label
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175)); // anim combo
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // time slider (可変)
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));  // 時刻ラベル
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));  // Play
        ctrlBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));  // Stop

        var animLabel = new Label
        {
            Text = "Animation:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 4, 0),
        };
        _animCombo.Dock = DockStyle.Fill;
        _timeSlider.Dock = DockStyle.Fill;
        _timeLbl.Dock = DockStyle.Fill;
        _timeLbl.TextAlign = ContentAlignment.MiddleRight;
        _timeLbl.Width = 0;   // Dock=Fill が効くよう既定幅をリセット
        _playBtn.Dock = DockStyle.Fill;
        _stopBtn.Dock = DockStyle.Fill;

        ctrlBar.Controls.Add(animLabel, 0, 0);
        ctrlBar.Controls.Add(_animCombo, 1, 0);
        ctrlBar.Controls.Add(_timeSlider, 2, 0);
        ctrlBar.Controls.Add(_timeLbl, 3, 0);
        ctrlBar.Controls.Add(_playBtn, 4, 0);
        ctrlBar.Controls.Add(_stopBtn, 5, 0);

        rightLayout.Controls.Add(_view, 0, 0);
        rightLayout.Controls.Add(ctrlBar, 0, 1);
        splitter.Panel2.Controls.Add(rightLayout);

        Controls.Add(splitter);

        _monsterList.SelectedIndexChanged += OnMonsterSelected;
        _animCombo.SelectedIndexChanged += OnAnimChanged;
        _timeSlider.ValueChanged += OnSliderChanged;
        _playBtn.Click += (_, _) => StartPlay();
        _stopBtn.Click += (_, _) => StopPlay();
        _view.SizeChanged += (_, _) => RefreshView();
    }

    void LoadMonsterList()
    {
        if (!Directory.Exists(_monstersDir)) return;
        var names = Directory.GetDirectories(_monstersDir)
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .OrderBy(n => n)
            .ToArray();
        _monsterList.Items.AddRange(names!);
    }

    void OnMonsterSelected(object? sender, EventArgs e)
    {
        if (_monsterList.SelectedItem is not string name) return;
        StopPlay();
        LoadMonster(name);
    }

    void LoadMonster(string name)
    {
        var monsterDir = Path.Combine(_monstersDir, name);
        try
        {
            _current?.Texture.Dispose();
            _current = SpineLoader.Load(monsterDir, _toolsRoot);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load {name}:\n{ex.Message}", "Load Error");
            _current = null;
            return;
        }

        _animCombo.Items.Clear();
        foreach (var anim in _current.Animations)
            _animCombo.Items.Add(anim);

        var preferred = _current.Animations.FirstOrDefault(a => a.StartsWith("idle", StringComparison.OrdinalIgnoreCase))
            ?? _current.Animations.FirstOrDefault() ?? "";
        _animCombo.SelectedItem = preferred;

        if (_animCombo.SelectedIndex < 0 && _animCombo.Items.Count > 0)
            _animCombo.SelectedIndex = 0;

        _animTime = 0;
        UpdateSlider();
        RefreshView();
    }

    void OnAnimChanged(object? sender, EventArgs e)
    {
        if (_current == null || _animCombo.SelectedItem is not string anim) return;
        var animData = _current.SkeletonData.FindAnimation(anim);
        _animDuration = animData?.Duration ?? 1f;
        _animTime = 0;
        UpdateSlider();
        RefreshView();
    }

    void OnSliderChanged(object? sender, EventArgs e)
    {
        if (_playing) return;
        _animTime = _timeSlider.Value / 1000f * _animDuration;
        _timeLbl.Text = $"{_animTime:F2}s";
        RefreshView();
    }

    void RefreshView()
    {
        if (_current == null || _animCombo.SelectedItem is not string anim) return;
        int w = Math.Max(_view.Width, 1), h = Math.Max(_view.Height, 1);
        var bmp = SpineRenderer.Render(_current, anim, _animTime, w, h);
        var prev = _view.Image;
        _view.Image = SkBitmapToGdi(bmp);
        bmp.Dispose();
        prev?.Dispose();
    }

    static System.Drawing.Bitmap SkBitmapToGdi(SkiaSharp.SKBitmap src)
    {
        // SKColorType.Rgba8888 → swap R↔B → GDI Format32bppArgb (BGRA)
        int len = src.Width * src.Height * 4;
        var buf = new byte[len];
        System.Runtime.InteropServices.Marshal.Copy(src.GetPixels(), buf, 0, len);
        for (int i = 0; i < len; i += 4)
            (buf[i], buf[i + 2]) = (buf[i + 2], buf[i]);   // R↔B swap
        var gdi = new System.Drawing.Bitmap(src.Width, src.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var bmpData = gdi.LockBits(new System.Drawing.Rectangle(0, 0, src.Width, src.Height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(buf, 0, bmpData.Scan0, len);
        gdi.UnlockBits(bmpData);
        return gdi;
    }

    void StartPlay()
    {
        if (_current == null) return;
        _playing = true;
        _playBtn.Enabled = false;
        _stopBtn.Enabled = true;
        _timer.Start();
    }

    void StopPlay()
    {
        _playing = false;
        _timer.Stop();
        _playBtn.Enabled = true;
        _stopBtn.Enabled = false;
    }

    void OnTick(object? sender, EventArgs e)
    {
        if (_current == null || _animCombo.SelectedItem is not string anim) return;
        _animTime += _timer.Interval / 1000f;
        if (_animDuration > 0)
            _animTime %= _animDuration;
        UpdateSlider();
        RefreshView();
    }

    void UpdateSlider()
    {
        _timeSlider.ValueChanged -= OnSliderChanged;
        _timeSlider.Value = _animDuration > 0
            ? (int)Math.Clamp(_animTime / _animDuration * 1000, 0, 1000)
            : 0;
        _timeLbl.Text = $"{_animTime:F2}s";
        _timeSlider.ValueChanged += OnSliderChanged;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _current?.Texture.Dispose();
        base.OnFormClosed(e);
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
