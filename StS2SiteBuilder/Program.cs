using StS2Shared.Services;

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var distDir    = Path.Combine(projectDir, "dist");
Directory.CreateDirectory(distDir);

CharData[] chars =
[
    new("ironclad",    "Ironclad",    "アイアンクラッド", "#c0392b", "#fde8e8",
        "力と忍耐のキャラクター。StrengthとExhaustを活かした圧倒的な攻撃力が持ち味。"),
    new("silent",      "Silent",      "サイレント",       "#1a7a4a", "#e8f8f0",
        "素早さと策略のキャラクター。Poisonで敵を蝕みながら、Shivの連撃で圧倒する。"),
    new("defect",      "Defect",      "ディフェクト",     "#1a5799", "#e8f0fc",
        "論理と精密さのキャラクター。属性オーブのChannel・Evokeを駆使して戦う。"),
    new("necrobinder", "Necrobinder", "ネクロバインダー", "#6c3483", "#f4ecf7",
        "暗黒魔術と召喚のキャラクター。OstyとSoulを操り、Doomで敵を追い詰める。"),
    new("regent",      "Regent",      "リージェント",     "#7d6608", "#fdf8e8",
        "創造と王権のキャラクター。武器を鍛え、カードを生み出し、Starを消費して強力な効果を発動する。"),
];

var mechanicsMap = CharacterMechanics.All
    .ToDictionary(c => c.CharLabel,
                  c => c.Mechanics.Select(m => m.MecLabel).ToArray(),
                  StringComparer.OrdinalIgnoreCase);

PageEntry[] pages =
[
    ..chars.Select(ch => new PageEntry("キャラクター", $"{ch.Id}.html", ch.EnName, ch.JaName, ch.Desc, ch.Accent)),
    // 将来追加: new PageEntry("カード", ...), new PageEntry("レリック", ...) 等
];

File.WriteAllText(Path.Combine(distDir, "index.html"), BuildIndex(chars),          System.Text.Encoding.UTF8);
File.WriteAllText(Path.Combine(distDir, "pages.html"), BuildPageList(pages, chars), System.Text.Encoding.UTF8);
foreach (var ch in chars)
{
    mechanicsMap.TryGetValue(ch.EnName, out var mecs);
    File.WriteAllText(Path.Combine(distDir, $"{ch.Id}.html"),
        BuildCharPage(ch, chars, mecs ?? []), System.Text.Encoding.UTF8);
}

Console.WriteLine($"Generated {2 + chars.Length} files -> {distDir}");

// ── page builders ─────────────────────────────────────────────────────────────

static string BuildIndex(CharData[] chars)
{
    var cards = string.Concat(chars.Select(ch => $"""
              <a href="{ch.Id}.html" class="char-card">
                <div class="char-card-header" style="background:{ch.Accent}">
                  <div class="char-name-en">{ch.EnName}</div>
                  <div class="char-name-ja">{ch.JaName}</div>
                </div>
                <div class="char-card-body">
                  <p class="char-desc">{ch.Desc}</p>
                </div>
                <div class="char-card-footer">カードを見る &rarr;</div>
              </a>
        """));

    return Layout("トップ", "index", "#4a90d9", chars, $"""
        <div class="page-hero">
          <h1 class="hero-title">Slay the Spire 2</h1>
          <p class="hero-sub">カードリファレンス</p>
          <p class="hero-desc">5人のキャラクターのカード・メカニクスを確認できます。</p>
        </div>
        <div class="char-grid">
          {cards}
        </div>
        """);
}

static string BuildPageList(PageEntry[] pages, CharData[] chars)
{
    string[] allCategories = ["キャラクター", "カード", "レリック", "イベント", "エンカウンター"];

    var sections = string.Concat(allCategories.Select(cat =>
    {
        var catPages = pages.Where(p => p.Category == cat).ToArray();

        string content;
        if (catPages.Length == 0)
        {
            content = """<p class="placeholder">ページはまだ追加されていません。</p>""";
        }
        else
        {
            var cards = string.Concat(catPages.Select(p => $"""
                      <a href="{p.Path}" class="char-card">
                        <div class="char-card-header" style="background:{p.Color}">
                          <div class="char-name-en">{p.TitleEn}</div>
                          <div class="char-name-ja">{p.TitleJa}</div>
                        </div>
                        <div class="char-card-body">
                          <p class="char-desc">{p.Desc}</p>
                        </div>
                        <div class="char-card-footer">ページへ &rarr;</div>
                      </a>
                """));
            content = $"""<div class="char-grid">{cards}</div>""";
        }

        var pendingBadge  = catPages.Length == 0 ? """ <span class="pending-badge">準備中</span>""" : "";
        var sectionClass  = catPages.Length == 0 ? " section-pending" : "";

        return $"""
            <section class="section{sectionClass}">
              <h2 class="section-title">{cat}{pendingBadge}</h2>
              {content}
            </section>
            """;
    }));

    return Layout("ページ一覧", "pages", "#4a90d9", chars, $"""
        <div class="page-hero">
          <h1 class="hero-title">ページ一覧</h1>
          <p class="hero-sub">全ページの一覧</p>
        </div>
        {sections}
        """);
}

static string BuildCharPage(CharData ch, CharData[] chars, string[] mecs)
{
    var mecHtml = mecs.Length > 0
        ? $"""<div class="mec-tags">{string.Concat(mecs.Select(m => $"""<span class="mec-tag">{m}</span>"""))}</div>"""
        : """<p class="placeholder">メカニクス情報なし</p>""";

    return Layout(ch.EnName, ch.Id, ch.Accent, chars, $"""
        <div class="char-header" style="border-left:5px solid {ch.Accent};background:{ch.LightBg}">
          <div class="char-header-body">
            <h1 class="char-title-en" style="color:{ch.Accent}">{ch.EnName}</h1>
            <div class="char-title-ja">{ch.JaName}</div>
            <p class="char-desc-full">{ch.Desc}</p>
          </div>
          <img src="images/characters/{ch.Id}.jpg" class="char-hero-img" alt="{ch.EnName}">
        </div>
        <section class="section">
          <h2 class="section-title">メカニクス / シナジー</h2>
          {mecHtml}
        </section>
        <section class="section">
          <h2 class="section-title">カード一覧</h2>
          <p class="placeholder">準備中...</p>
        </section>
        """);
}

static string Layout(string title, string activeId, string accent, CharData[] chars, string content)
{
    const string CSS = """
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
        body {
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', Arial,
                       'Hiragino Sans', 'Noto Sans JP', sans-serif;
          background: #f5f7fa;
          color: #2c2c2c;
          line-height: 1.6;
        }
        a { color: inherit; text-decoration: none; }
        .layout { display: flex; min-height: 100vh; }

        /* ── Sidebar ── */
        .sidebar {
          width: 240px;
          min-width: 240px;
          background: #fff;
          border-right: 1px solid #e8e8e8;
          display: flex;
          flex-direction: column;
          position: sticky;
          top: 0;
          height: 100vh;
          overflow-y: auto;
        }
        .sidebar-brand {
          padding: 18px 20px 16px;
          background: #1e2128;
          border-bottom: 1px solid #2c3040;
        }
        .brand-game { font-size: 13px; font-weight: 700; color: #f0f0f0; letter-spacing: 0.3px; }
        .brand-label { font-size: 11px; color: #8899aa; margin-top: 3px; }
        .nav-section { padding: 14px 0 6px; }
        .nav-group-label {
          font-size: 10px;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 1px;
          color: #c0c0c0;
          padding: 0 20px 8px;
        }
        .nav-link {
          display: flex;
          align-items: center;
          gap: 9px;
          padding: 8px 20px;
          font-size: 13.5px;
          color: #555;
          border-left: 3px solid transparent;
          transition: background 0.1s;
        }
        .nav-link:hover { background: #f7f8fa; color: #222; }
        .nav-link.active { background: #f3f4f6; color: #111; font-weight: 600; }
        .nav-dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
        .nav-home-icon { font-size: 15px; line-height: 1; }
        .nav-name-ja { font-size: 11px; color: #aaa; margin-left: auto; }
        .nav-link.active .nav-name-ja { color: #888; }

        /* ── Main ── */
        .main { flex: 1; padding: 40px 48px; min-width: 0; }

        /* ── Hero (index / pages) ── */
        .page-hero {
          margin-bottom: 32px;
          padding-bottom: 24px;
          border-bottom: 1px solid #e8e8e8;
        }
        .hero-title { font-size: 28px; font-weight: 800; color: #1a1a2e; letter-spacing: -0.5px; }
        .hero-sub { font-size: 15px; color: #666; margin-top: 4px; }
        .hero-desc { font-size: 13.5px; color: #999; margin-top: 10px; }

        /* ── Character grid (index / pages) ── */
        .char-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
          gap: 16px;
        }
        .char-card {
          display: flex;
          flex-direction: column;
          background: #fff;
          border-radius: 10px;
          overflow: hidden;
          box-shadow: 0 1px 4px rgba(0,0,0,0.08);
          transition: box-shadow 0.15s, transform 0.15s;
        }
        .char-card:hover { box-shadow: 0 6px 20px rgba(0,0,0,0.13); transform: translateY(-3px); }
        .char-card-header { padding: 20px 18px 14px; color: #fff; }
        .char-name-en { font-size: 19px; font-weight: 800; letter-spacing: -0.3px; }
        .char-name-ja { font-size: 11px; opacity: 0.82; margin-top: 3px; }
        .char-card-body { padding: 14px 18px; flex: 1; }
        .char-desc { font-size: 12.5px; color: #666; line-height: 1.65; }
        .char-card-footer {
          padding: 9px 18px 11px;
          font-size: 12px;
          color: #aaa;
          border-top: 1px solid #f0f0f0;
          font-weight: 500;
        }
        .char-card:hover .char-card-footer { color: #666; }

        /* ── Character page header ── */
        .char-header {
          border-radius: 10px;
          padding: 28px 32px;
          margin-bottom: 24px;
          display: flex;
          align-items: center;
          gap: 28px;
          overflow: hidden;
        }
        .char-header-body { flex: 1; min-width: 0; }
        .char-title-en { font-size: 30px; font-weight: 800; letter-spacing: -0.5px; }
        .char-title-ja { font-size: 13px; color: #777; margin-top: 5px; }
        .char-desc-full {
          font-size: 14px;
          color: #555;
          margin-top: 14px;
          max-width: 560px;
          line-height: 1.75;
        }
        .char-hero-img {
          width: 132px;
          height: 195px;
          object-fit: cover;
          border-radius: 8px;
          flex-shrink: 0;
          box-shadow: 0 2px 8px rgba(0,0,0,0.15);
          image-rendering: auto;
        }

        /* ── Sections ── */
        .section {
          background: #fff;
          border-radius: 10px;
          padding: 24px 28px;
          margin-bottom: 20px;
          box-shadow: 0 1px 3px rgba(0,0,0,0.06);
        }
        .section-title {
          font-size: 14px;
          font-weight: 700;
          color: #333;
          margin-bottom: 16px;
          padding-bottom: 10px;
          border-bottom: 1px solid #f0f0f0;
          letter-spacing: 0.2px;
        }
        .mec-tags { display: flex; flex-wrap: wrap; gap: 8px; }
        .mec-tag {
          padding: 5px 13px;
          background: #f5f6f8;
          border: 1px solid #e4e6ea;
          border-radius: 20px;
          font-size: 13px;
          color: #444;
        }
        .placeholder { font-size: 13.5px; color: #bbb; font-style: italic; }

        /* ── Page list ── */
        .section-pending { opacity: 0.55; }
        .pending-badge {
          display: inline-block;
          font-size: 10px;
          font-weight: 600;
          background: #f0f0f0;
          color: #aaa;
          border-radius: 10px;
          padding: 2px 8px;
          margin-left: 8px;
          vertical-align: middle;
          letter-spacing: 0.5px;
          font-style: normal;
        }
        """;

    var homeActive  = activeId == "index";
    var homeStyle   = homeActive  ? " style=\"border-left-color:#4a90d9\"" : "";
    var homeClass   = homeActive  ? " active" : "";
    var pagesActive = activeId == "pages";
    var pagesStyle  = pagesActive ? " style=\"border-left-color:#4a90d9\"" : "";
    var pagesClass  = pagesActive ? " active" : "";

    var navItems = string.Concat(chars.Select(ch => {
        var isActive    = ch.Id == activeId;
        var activeStyle = isActive ? $" style=\"border-left-color:{ch.Accent}\"" : "";
        var cls         = isActive ? " active" : "";
        return $"""
                  <a href="{ch.Id}.html" class="nav-link{cls}"{activeStyle}>
                    <span class="nav-dot" style="background:{ch.Accent}"></span>
                    {ch.EnName}
                    <span class="nav-name-ja">{ch.JaName}</span>
                  </a>
            """;
    }));

    return $"""
        <!DOCTYPE html>
        <html lang="ja">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{title} | StS2 カードリファレンス</title>
          <style>
        {CSS}
          </style>
        </head>
        <body>
          <div class="layout">
            <nav class="sidebar">
              <div class="sidebar-brand">
                <div class="brand-game">Slay the Spire 2</div>
                <div class="brand-label">カードリファレンス</div>
              </div>
              <div class="nav-section">
                <div class="nav-group-label">ページ</div>
                <a href="index.html" class="nav-link{homeClass}"{homeStyle}>
                  <span class="nav-home-icon">&#8962;</span>
                  トップ
                </a>
                <a href="pages.html" class="nav-link{pagesClass}"{pagesStyle}>
                  <span class="nav-home-icon">&#9776;</span>
                  ページ一覧
                </a>
              </div>
              <div class="nav-section">
                <div class="nav-group-label">キャラクター</div>
                {navItems}
              </div>
            </nav>
            <main class="main">
              {content}
            </main>
          </div>
        </body>
        </html>
        """;
}

record CharData(string Id, string EnName, string JaName, string Accent, string LightBg, string Desc);
record PageEntry(string Category, string Path, string TitleEn, string TitleJa, string Desc, string Color);
