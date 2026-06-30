using System.Drawing;
using StS2Capture.Recognition;

// キャプチャ静止画でエンシェントレリック（名前テキスト OCR）検出を実証・較正する CLI。
// 使い方: capture-verify <pngPath> [--client x,y,w,h] [--crops <dir>]
//   --client : 認識に渡す client 矩形（既定 = (0,45,W,H-45)。タイトルバー込み静止画用）
//   --crops  : 指定すると各名前バンドの二値化クロップを PNG 保存

if (args.Length == 0)
{
    Console.WriteLine("usage: capture-verify <pngPath> [--client x,y,w,h] [--crops <dir>]");
    return 1;
}

string pngPath = args[0];
if (!File.Exists(pngPath)) { Console.WriteLine($"file not found: {pngPath}"); return 1; }

Rectangle? clientOverride = null;
string? cropsDir = null;
double? dy = null, bandH = null, bandW = null, cx = null;
int? scale = null;
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--client" when i + 1 < args.Length:
            var p = args[++i].Split(',');
            if (p.Length == 4) clientOverride = new Rectangle(
                int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
            break;
        case "--crops" when i + 1 < args.Length: cropsDir = args[++i]; break;
        case "--dy" when i + 1 < args.Length: dy = double.Parse(args[++i]); break;
        case "--bandh" when i + 1 < args.Length: bandH = double.Parse(args[++i]); break;
        case "--bandw" when i + 1 < args.Length: bandW = double.Parse(args[++i]); break;
        case "--cx" when i + 1 < args.Length: cx = double.Parse(args[++i]); break;
        case "--scale" when i + 1 < args.Length: scale = int.Parse(args[++i]); break;
    }
}

using var bmp = new Bitmap(pngPath);
// 既定 client: タイトルバー（≈45px）を除いた領域。ライブ WGC（client のみ）相当。
var client = clientOverride ?? new Rectangle(0, 45, bmp.Width, bmp.Height - 45);

Console.WriteLine($"image: {pngPath}  size={bmp.Width}x{bmp.Height}");
Console.WriteLine($"client: {client.X},{client.Y},{client.Width},{client.Height}");

var ancient = new AncientRelicRecognizer();
if (cropsDir is not null) ancient.SaveCropsDir = cropsDir;
if (scale is int sc) ancient.TitleScale = sc;
if (dy is not null || bandH is not null || bandW is not null || cx is not null)
    ancient.NameBands = ancient.NameBands
        .Select(b => new AncientRelicRecognizer.NameBand(
            cx ?? b.CxFrac, b.CyFrac + (dy ?? 0), bandW ?? b.WFrac, bandH ?? b.HFrac))
        .ToList();
Console.WriteLine($"OCR engine available: {ancient.IsAvailable}");
Console.WriteLine();

// 1) 各名前バンドの OCR 生テキストと最良一致レリック。
var diag = ancient.Diagnose(bmp, client);
for (int s = 0; s < diag.Count; s++)
{
    var (rect, ocr, match) = diag[s];
    Console.WriteLine($"[band {s}] rect={rect.X},{rect.Y},{rect.Width},{rect.Height}");
    Console.WriteLine($"    OCR : \"{ocr}\"");
    Console.WriteLine(match is { } m
        ? $"    match: {m.Id} ({m.Name})  editDist={m.Distance:F0}"
        : "    match: (none)");
}
Console.WriteLine();

// 2) 実際の採否（Detect）。
var det = ancient.Detect(bmp, client);
Console.WriteLine($"Detect: matched(IsShop)={det.IsShop}  accepted={det.Items.Count(i => i.Accepted)}/{det.Items.Count}");
foreach (var it in det.Items)
{
    var label = it.Candidates.Count == 0 ? "(no match)" :
        string.Join(" / ", it.Candidates.Select(c => $"{c.Id} ({c.Name})"));
    Console.WriteLine($"    accepted={it.Accepted}  {label}");
}
return 0;
