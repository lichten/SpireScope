using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using StS2Shared.Assets;

// extract-pck: ユーザーの .pck からビューア用アセットを直接抽出する（配布セットアップの中核ロジックの検証用）。
//   dotnet run --project ctex-to-png -- extract-pck [<pck または ゲームフォルダ>] <出力ディレクトリ>
// 第1引数を省略すると Steam から自動検出する。tools/extracted に依存しないため先頭で処理する。
if (args.Length >= 1 && args[0] == "extract-pck")
{
    ExtractPck(args[1..]);
    return;
}

// dump-pck: .pck の全エントリを res:// 相対のレイアウトでそのまま書き出す（GodotPCKExplorer の代替。
// Windows 専用 exe を使えない macOS で tools/extracted を作るためのもの）。
//   dotnet run --project ctex-to-png -- dump-pck [<pck または ゲームフォルダ>] <出力ディレクトリ>
if (args.Length >= 1 && args[0] == "dump-pck")
{
    DumpPck(args[1..]);
    return;
}

// setup: 配布アセットセットアップ。SetupWizardForm と同じ AssetSetup.RunSetup（_staging → v{version}
// 原子 Move）を CLI から回す。Mac 版 SwiftUI アプリの初回セットアップはこれを実行する。
//   dotnet run --project ctex-to-png -- setup [<pck または ゲームフォルダ>]
// 出力先は AssetLocator.DistributionAssetsRoot（Windows: %LocalAppData%\SpireScope\assets、
// macOS: ~/Library/Application Support/SpireScope/assets）。tools/extracted に依存しない。
if (args.Length >= 1 && args[0] == "setup")
{
    RunAssetSetup(args[1..]);
    return;
}

var toolsRoot       = FindToolsRoot();
var portraitPngRoot = Path.Combine(toolsRoot, "images", "card_portraits_png");
var jpegOutRoot     = Path.GetFullPath(Path.Combine(toolsRoot, "..", "..", "card-images"));
var ctexImport      = Path.Combine(toolsRoot, ".godot", "imported");

const int JpegWidth   = 300;
const int JpegHeight  = 420;
const int JpegQuality = 40;

// Relic mode: convert relic .ctex → PNG into images/relics_png/ per relic_images.json
// Usage: dotnet run --project ctex-to-png -- relics
if (args.Length == 1 && args[0] == "relics")
{
    var repoRoot  = Path.GetFullPath(Path.Combine(toolsRoot, "..", ".."));
    var jsonPath  = LatestVersioned(Path.Combine(repoRoot, "StS2Shared", "Resources"), "relic_images.json");
    if (jsonPath is null) { Console.WriteLine("relic_images.json が見つかりません。"); return; }

    var relicsSrc = Path.Combine(toolsRoot, "images", "relics");
    var outRoot   = Path.Combine(toolsRoot, "images", "relics_png");
    Directory.CreateDirectory(outRoot);

    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    int relConverted = 0, relSkipped = 0, relMissing = 0;
    foreach (var prop in doc.RootElement.EnumerateObject())
    {
        var rel        = prop.Value.GetString()!;                 // "akabeko.png" / "beta/belt_buckle.png"
        var relOs      = rel.Replace('/', Path.DirectorySeparatorChar);
        var importPath = Path.Combine(relicsSrc, relOs + ".import");
        var ctexRel    = ParseImportCtexPath(importPath);
        if (ctexRel is null) { relMissing++; continue; }
        var ctexFull   = Path.Combine(toolsRoot, ctexRel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(ctexFull)) { relMissing++; continue; }
        var outPath    = Path.Combine(outRoot, relOs);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        if (File.Exists(outPath)) { relSkipped++; continue; }
        try { ConvertCtex(ctexFull, outPath, verbose: false); relConverted++; }
        catch (Exception ex) { Console.WriteLine($"  fail {rel}: {ex.Message}"); relMissing++; }
    }
    Console.WriteLine($"relics_png: converted={relConverted} skipped={relSkipped} missing={relMissing}");
    return;
}

// Event mode: convert event .ctex → PNG into images/events_png/ per event_images.json
// Usage: dotnet run --project ctex-to-png -- events
if (args.Length == 1 && args[0] == "events")
{
    var repoRoot  = Path.GetFullPath(Path.Combine(toolsRoot, "..", ".."));
    var jsonPath  = LatestVersioned(Path.Combine(repoRoot, "StS2Shared", "Resources"), "event_images.json");
    if (jsonPath is null) { Console.WriteLine("event_images.json が見つかりません。"); return; }

    var eventsSrc = Path.Combine(toolsRoot, "images", "events");
    var outRoot   = Path.Combine(toolsRoot, "images", "events_png");
    Directory.CreateDirectory(outRoot);

    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    int evConverted = 0, evSkipped = 0, evMissing = 0;
    foreach (var prop in doc.RootElement.EnumerateObject())
    {
        var rel        = prop.Value.GetString()!;                 // "abyssal_baths.png"
        var relOs      = rel.Replace('/', Path.DirectorySeparatorChar);
        var importPath = Path.Combine(eventsSrc, relOs + ".import");
        var ctexRel    = ParseImportCtexPath(importPath);
        if (ctexRel is null) { evMissing++; continue; }
        var ctexFull   = Path.Combine(toolsRoot, ctexRel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(ctexFull)) { evMissing++; continue; }
        var outPath    = Path.Combine(outRoot, relOs);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        if (File.Exists(outPath)) { evSkipped++; continue; }
        try { ConvertCtex(ctexFull, outPath, verbose: false); evConverted++; }
        catch (Exception ex) { Console.WriteLine($"  fail {rel}: {ex.Message}"); evMissing++; }
    }
    Console.WriteLine($"events_png: converted={evConverted} skipped={evSkipped} missing={evMissing}");
    return;
}

// Ancient mode: convert ancient .ctex → PNG into images/ancients_png/ per ancient_images.json
// Usage: dotnet run --project ctex-to-png -- ancients
if (args.Length == 1 && args[0] == "ancients")
{
    var repoRoot  = Path.GetFullPath(Path.Combine(toolsRoot, "..", ".."));
    var jsonPath  = LatestVersioned(Path.Combine(repoRoot, "StS2Shared", "Resources"), "ancient_images.json");
    if (jsonPath is null) { Console.WriteLine("ancient_images.json が見つかりません。"); return; }

    var ancientsSrc = Path.Combine(toolsRoot, "images", "ancients");
    var outRoot     = Path.Combine(toolsRoot, "images", "ancients_png");
    Directory.CreateDirectory(outRoot);

    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    int ancConverted = 0, ancSkipped = 0, ancMissing = 0;
    foreach (var prop in doc.RootElement.EnumerateObject())
    {
        var rel        = prop.Value.GetString()!;                 // "orobas_placeholder.png"
        var relOs      = rel.Replace('/', Path.DirectorySeparatorChar);
        var importPath = Path.Combine(ancientsSrc, relOs + ".import");
        var ctexRel    = ParseImportCtexPath(importPath);
        if (ctexRel is null) { ancMissing++; continue; }
        var ctexFull   = Path.Combine(toolsRoot, ctexRel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(ctexFull)) { ancMissing++; continue; }
        var outPath    = Path.Combine(outRoot, relOs);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        if (File.Exists(outPath)) { ancSkipped++; continue; }
        try { ConvertCtex(ctexFull, outPath, verbose: false); ancConverted++; }
        catch (Exception ex) { Console.WriteLine($"  fail {rel}: {ex.Message}"); ancMissing++; }
    }
    Console.WriteLine($"ancients_png: converted={ancConverted} skipped={ancSkipped} missing={ancMissing}");
    return;
}

// Potion mode: convert potion .ctex → PNG into images/potions_png/ per potion_images.json
// Usage: dotnet run --project ctex-to-png -- potions
if (args.Length == 1 && args[0] == "potions")
{
    var repoRoot  = Path.GetFullPath(Path.Combine(toolsRoot, "..", ".."));
    var jsonPath  = LatestVersioned(Path.Combine(repoRoot, "StS2Shared", "Resources"), "potion_images.json");
    if (jsonPath is null) { Console.WriteLine("potion_images.json が見つかりません。"); return; }

    var potionsSrc = Path.Combine(toolsRoot, "images", "potions");
    var outRoot    = Path.Combine(toolsRoot, "images", "potions_png");
    Directory.CreateDirectory(outRoot);

    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    int potConverted = 0, potSkipped = 0, potMissing = 0;
    foreach (var prop in doc.RootElement.EnumerateObject())
    {
        var rel        = prop.Value.GetString()!;                 // "fire_potion.png"
        var relOs      = rel.Replace('/', Path.DirectorySeparatorChar);
        var importPath = Path.Combine(potionsSrc, relOs + ".import");
        var ctexRel    = ParseImportCtexPath(importPath);
        if (ctexRel is null) { potMissing++; continue; }
        var ctexFull   = Path.Combine(toolsRoot, ctexRel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(ctexFull)) { potMissing++; continue; }
        var outPath    = Path.Combine(outRoot, relOs);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        if (File.Exists(outPath)) { potSkipped++; continue; }
        try { ConvertCtex(ctexFull, outPath, verbose: false); potConverted++; }
        catch (Exception ex) { Console.WriteLine($"  fail {rel}: {ex.Message}"); potMissing++; }
    }
    Console.WriteLine($"potions_png: converted={potConverted} skipped={potSkipped} missing={potMissing}");
    return;
}

// Monster mode: render Spine idle animations → {id}.gif / {id}.png into images/monsters/
// 出力は site3 が read-only マウントで直接参照する（クリーン展開後は必ず再実行すること）。
// Usage: dotnet run --project ctex-to-png -- monsters
if (args.Length == 1 && args[0] == "monsters")
{
    // 罠回避: RequireExtractedRoot は %LocalAppData% の配布キャッシュへフォールバックし得る。
    // site3 が参照するのはリポジトリの tools/extracted なので、開発モード解決（walk-up）を必須にする。
    if (!StS2Shared.Services.AssetLocator.HasDevExtracted())
    {
        Console.Error.WriteLine("リポジトリの tools/extracted が見つかりません。リポジトリ内から実行してください。");
        Environment.Exit(1);
        return;
    }
    var animationsDir = Path.Combine(toolsRoot, "animations", "monsters");
    if (!Directory.Exists(animationsDir))
    {
        Console.Error.WriteLine($"animations/monsters がありません（展開が不完全）: {animationsDir}");
        Environment.Exit(1);
        return;
    }
    var monsterOutDir = Path.Combine(toolsRoot, "images", "monsters");
    var withGif = MonsterGifGenerator.Generate(toolsRoot, monsterOutDir, Console.WriteLine);
    Console.WriteLine($"Generated monsters (GIF {withGif.Count}) -> {monsterOutDir}");
    return;
}

// Card JPEG mode: convert specified card IDs to web-sized JPEG for git tracking
// Usage: dotnet run --project ctex-to-png -- <id1> <id2> ...
// Example: dotnet run --project ctex-to-png -- bash defend_ironclad
if (args.Length > 0)
{
    Directory.CreateDirectory(jpegOutRoot);
    foreach (var id in args)
    {
        var matches = Directory.GetFiles(portraitPngRoot, $"{id}.png", SearchOption.AllDirectories);
        if (matches.Length == 0)
        {
            Console.WriteLine($"  not found: {id}.png  (run without args first to generate source PNGs)");
            continue;
        }
        foreach (var srcPath in matches)
        {
            var relDir  = Path.GetRelativePath(portraitPngRoot, Path.GetDirectoryName(srcPath)!);
            var outDir  = relDir == "." ? jpegOutRoot : Path.Combine(jpegOutRoot, relDir);
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, id + ".jpg");
            ConvertToJpeg(srcPath, outPath);
            var label = relDir == "." ? id : $"{relDir}/{id}";
            Console.WriteLine($"  {label}.jpg");
        }
    }
    return;
}

// --- 1. Card atlases (card_atlas_N.png) ---
Console.WriteLine("=== Card atlases ===");
var atlasOutDir = Path.Combine(toolsRoot, "images", "atlases");
Directory.CreateDirectory(atlasOutDir);

foreach (var path in Directory.GetFiles(ctexImport, "card_atlas_*.ctex").OrderBy(f => f))
{
    var outName = Path.GetFileNameWithoutExtension(path).Split('-')[0]; // "card_atlas_0.png"
    outName = Path.GetFileNameWithoutExtension(outName) + ".png";       // "card_atlas_0.png"
    var outPath = Path.Combine(atlasOutDir, outName);
    if (File.Exists(outPath)) { Console.WriteLine($"  skip {outName}"); continue; }
    ConvertCtex(path, outPath);
}

// --- 2. Individual card portraits (via .import files) ---
Console.WriteLine("\n=== Card portraits ===");
var portraitsRoot   = Path.Combine(toolsRoot, "images", "packed", "card_portraits");
var portraitOutRoot = Path.Combine(toolsRoot, "images", "card_portraits_png");
Directory.CreateDirectory(portraitOutRoot);

// beta/ は同名の本画像が親フォルダに無いときだけ採る（開発中キャラ＝v0.113.0 の Fool 等は beta/ にしか絵が無い）。
// AssetExtractor.ConvertPortraits(betaFallbackOnly) と同規則。
static bool IsBetaDir(string dir) => string.Equals(Path.GetFileName(dir), "beta", StringComparison.OrdinalIgnoreCase);
var importFiles = Directory.GetFiles(portraitsRoot, "*.png.import", SearchOption.AllDirectories)
    .Where(f =>
    {
        var dir = Path.GetDirectoryName(f)!;
        return !IsBetaDir(dir) || !File.Exists(Path.Combine(Path.GetDirectoryName(dir)!, Path.GetFileName(f)));
    })
    .OrderBy(f => f)
    .ToList();

Console.WriteLine($"Found {importFiles.Count} card portrait(s)");
int converted = 0, skipped = 0;

foreach (var importPath in importFiles)
{
    var ctexRelPath = ParseImportCtexPath(importPath);
    if (ctexRelPath is null) continue;

    var ctexFull = Path.Combine(toolsRoot, ctexRelPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(ctexFull)) continue;

    var relDir  = Path.GetRelativePath(portraitsRoot, Path.GetDirectoryName(importPath)!);
    var outDir  = Path.Combine(portraitOutRoot, relDir);
    Directory.CreateDirectory(outDir);

    var outName = Path.GetFileNameWithoutExtension(importPath); // e.g. "bash.png"
    var outPath = Path.Combine(outDir, outName);

    if (File.Exists(outPath)) { skipped++; continue; }

    ConvertCtex(ctexFull, outPath, verbose: false);
    converted++;
    if (converted % 50 == 0)
        Console.WriteLine($"  {converted}/{importFiles.Count}...");
}
Console.WriteLine($"  Done. converted={converted} skipped={skipped}");
Console.WriteLine("\nAll done.");

// ── helpers ──────────────────────────────────────────────────────────────────

static string FindToolsRoot() => StS2Shared.Services.AssetLocator.RequireExtractedRoot();

// extract-pck サブコマンド本体。
//   extract-pck <出力ディレクトリ>                     … Steam から .pck を自動検出
//   extract-pck <pck または ゲームフォルダ> <出力ディレクトリ>
static void ExtractPck(string[] a)
{
    Sts2Install? install;
    string outDir;

    if (a.Length == 1)
    {
        outDir = a[0];
        install = SteamLocator.Locate();
        if (install is null)
        {
            Console.Error.WriteLine("Slay the Spire 2 を Steam から自動検出できませんでした。" +
                ".pck かゲームフォルダのパスを第1引数で指定してください。");
            Environment.Exit(1);
            return;
        }
    }
    else if (a.Length == 2)
    {
        install = SteamLocator.FromPath(a[0]);
        outDir = a[1];
        if (install is null)
        {
            Console.Error.WriteLine($"指定パスから SlayTheSpire2.pck が見つかりません: {a[0]}");
            Environment.Exit(1);
            return;
        }
    }
    else
    {
        Console.Error.WriteLine("使い方: extract-pck [<pck または ゲームフォルダ>] <出力ディレクトリ>");
        Environment.Exit(2);
        return;
    }

    Console.WriteLine($"検出: {install.PckPath}");
    Console.WriteLine($"バージョン: {install.Version ?? "(不明)"}");
    Console.WriteLine($"出力先: {outDir}");

    using var pck = new PckReader(install.PckPath);
    Console.WriteLine($"PCK: format=v{pck.FormatVersion} engine={pck.EngineVersion} files={pck.Index.Count}");

    var extractor = new AssetExtractor(pck, outDir);
    var lastGroup = "";
    var progress = new SyncProgress<ExtractProgress>(p =>
    {
        if (p.Group != lastGroup)
        {
            if (lastGroup.Length > 0) Console.WriteLine();
            Console.Write($"  {p.Group}: ");
            lastGroup = p.Group;
        }
        if (p.Done == p.Total || p.Done % 50 == 0)
            Console.Write($"{p.Done}/{p.Total} ");
    });

    extractor.ExtractViewerAssets(progress);
    Console.WriteLine("\n完了。");
}

// dump-pck サブコマンド本体。引数の解決は extract-pck と同じ。
static void DumpPck(string[] a)
{
    Sts2Install? install = a.Length switch
    {
        1 => SteamLocator.Locate(),
        2 => SteamLocator.FromPath(a[0]),
        _ => null,
    };
    if (a.Length is not (1 or 2))
    {
        Console.Error.WriteLine("使い方: dump-pck [<pck または ゲームフォルダ>] <出力ディレクトリ>");
        Environment.Exit(2);
        return;
    }
    if (install is null)
    {
        Console.Error.WriteLine("Slay the Spire 2 の .pck が見つかりません。.pck かゲームフォルダのパスを第1引数で指定してください。");
        Environment.Exit(1);
        return;
    }
    var outDir = Path.GetFullPath(a[^1]);
    Console.WriteLine($"検出: {install.PckPath}");
    Console.WriteLine($"バージョン: {install.Version ?? "(不明)"}");
    Console.WriteLine($"出力先: {outDir}");

    using var pck = new PckReader(install.PckPath);
    var entries = pck.Index.OrderBy(e => e.ResPath, StringComparer.Ordinal).ToList();
    Console.WriteLine($"PCK: format=v{pck.FormatVersion} engine={pck.EngineVersion} files={entries.Count}");

    int written = 0, encrypted = 0, unsafePath = 0;
    long bytes = 0;
    foreach (var e in entries)
    {
        if (e.Encrypted) { encrypted++; continue; }
        var dest = Path.GetFullPath(Path.Combine(outDir, e.ResPath.Replace('/', Path.DirectorySeparatorChar)));
        // パス走査（"../" 等）で出力先の外へ書かないようにする
        if (!dest.StartsWith(outDir + Path.DirectorySeparatorChar, StringComparison.Ordinal)) { unsafePath++; continue; }
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var data = pck.Read(e.ResPath);
        File.WriteAllBytes(dest, data);
        written++;
        bytes += data.Length;
        if (written % 2000 == 0) Console.WriteLine($"  {written}/{entries.Count}");
    }
    Console.WriteLine($"完了: written={written} ({bytes / 1048576} MB) encrypted(skip)={encrypted} unsafePath(skip)={unsafePath}");
    if (encrypted > 0) Console.Error.WriteLine("警告: 暗号化エントリをスキップしました。");
}

// setup サブコマンド本体。
//   setup                              … Steam から .pck を自動検出
//   setup <pck または ゲームフォルダ>
static void RunAssetSetup(string[] a)
{
    Sts2Install? install;
    if (a.Length == 0)
    {
        install = SteamLocator.Locate();
        if (install is null)
        {
            Console.Error.WriteLine("Slay the Spire 2 を Steam から自動検出できませんでした。" +
                ".pck かゲームフォルダのパスを第1引数で指定してください。");
            Environment.Exit(1);
            return;
        }
    }
    else if (a.Length == 1)
    {
        install = SteamLocator.FromPath(a[0]);
        if (install is null)
        {
            Console.Error.WriteLine($"指定パスから .pck が見つかりません: {a[0]}");
            Environment.Exit(1);
            return;
        }
    }
    else
    {
        Console.Error.WriteLine("使い方: setup [<pck または ゲームフォルダ>]");
        Environment.Exit(2);
        return;
    }

    Console.WriteLine($"検出: {install.PckPath}");
    Console.WriteLine($"バージョン: {install.Version ?? "(不明)"}");

    var lastGroup = "";
    var progress = new SyncProgress<ExtractProgress>(p =>
    {
        if (p.Group != lastGroup)
        {
            if (lastGroup.Length > 0) Console.WriteLine();
            Console.Write($"  {p.Group}: ");
            lastGroup = p.Group;
        }
        if (p.Done == p.Total || p.Done % 50 == 0)
            Console.Write($"{p.Done}/{p.Total} ");
    });

    var final = AssetSetup.RunSetup(install, progress);
    Console.WriteLine($"\n完了: {final}");
}

// GST2 (.ctex) のデコードは StS2Shared.Assets.CtexDecoder に一元化。以下は従来の呼び出し名を保つ薄いラッパー。
static void ConvertCtex(string srcPath, string outPath, bool verbose = true)
{
    if (verbose)
        Console.Write($"  {Path.GetFileName(outPath)} ... ");
    CtexDecoder.ConvertToPng(srcPath, outPath);
    if (verbose)
        Console.WriteLine("ok");
}

static string? ParseImportCtexPath(string importPath) => CtexDecoder.ParseImportCtexPath(importPath);

// Resources/v*/{fileName} のうち最大バージョンの実ファイルパスを返す（無ければ null）。
// バージョン token 内の整数列を 0 埋め連結して文字列比較で数値順にする
// （StS2Shared/Services/ResourceResolver.cs の VersionKey と同方針）。
static string? LatestVersioned(string resourcesDir, string fileName)
{
    if (!Directory.Exists(resourcesDir)) return null;
    return Directory.GetDirectories(resourcesDir, "v*")
        .Select(d => Path.Combine(d, fileName))
        .Where(File.Exists)
        .OrderByDescending(p => string.Join('.',
            Regex.Matches(Path.GetFileName(Path.GetDirectoryName(p)!), @"\d+")
                 .Select(x => x.Value.PadLeft(8, '0'))), StringComparer.Ordinal)
        .FirstOrDefault();
}

static void ConvertToJpeg(string srcPath, string outPath)
{
    using var image = Image.Load<Rgba32>(srcPath);
    image.Mutate(x => x
        .Resize(JpegWidth, JpegHeight)
        .BackgroundColor(Color.White));
    image.SaveAsJpeg(outPath, new JpegEncoder { Quality = JpegQuality });
}

// 同期的に Report を実行する IProgress（Progress<T> と違いスレッドプールへ marshal しないので
// CLI のコンソール出力が順序どおりになる）。
sealed class SyncProgress<T>(Action<T> onReport) : IProgress<T>
{
    public void Report(T value) => onReport(value);
}
