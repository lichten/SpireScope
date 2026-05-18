using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using SkiaSharp;
using Spine;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISImage  = SixLabors.ImageSharp.Image;
using ISRgba32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

namespace StS2MonsterBrowser;

public record MonsterData(SkeletonData SkeletonData, Atlas Atlas, SKBitmap Texture, string[] Animations);

/// <summary>
/// モンスターディレクトリ（tools/extracted/animations/monsters/{name}/）から
/// Spine スケルトンデータとテクスチャを読み込む。
/// </summary>
static class SpineLoader
{
    static readonly Regex ImportPathRegex = new(@"path=""res://(.+?)""", RegexOptions.Compiled);

    /// <summary>
    /// モンスターディレクトリから MonsterData を生成する。
    /// toolsRoot = tools/extracted/ のフルパス
    /// </summary>
    public static MonsterData Load(string monsterDir, string toolsRoot)
    {
        // .skel.import → .spskel のパス解決
        var skelImport  = Directory.GetFiles(monsterDir, "*.skel.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.skel.import not found in {monsterDir}");
        var spskelPath  = ResolveImportPath(skelImport, toolsRoot);

        // .atlas.import → .spatlas のパス解決
        var atlasImport = Directory.GetFiles(monsterDir, "*.atlas.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.atlas.import not found in {monsterDir}");
        var spatlasPath = ResolveImportPath(atlasImport, toolsRoot);

        // .spatlas (JSON ラッパー) を読んで atlas テキストとテクスチャ名を取得
        using var spatlasDoc = JsonDocument.Parse(File.ReadAllText(spatlasPath));
        var atlasText     = spatlasDoc.RootElement.GetProperty("atlas_data").GetString()!;
        var textureFilename = atlasText.Split('\n')[0].Trim();   // 例: "axe_bot.png"

        // テクスチャ .ctex を探して SKBitmap に変換
        var pngImport = Directory.GetFiles(monsterDir, "*.png.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.png.import not found in {monsterDir}");
        var ctexPath  = ResolveImportPath(pngImport, toolsRoot);
        var texture   = LoadCtexAsSKBitmap(ctexPath);

        // Atlas を構築（TextureLoader でテクスチャを AtlasPage に紐付け）
        var textureLoader = new SkiaTextureLoader(texture);
        var atlas = new Atlas(new StringReader(atlasText), "", textureLoader);

        // Spine バイナリスケルトンを読み込む
        var attachmentLoader = new AtlasAttachmentLoader(atlas);
        var binary           = new SkeletonBinary(attachmentLoader);
        using var stream = File.OpenRead(spskelPath);
        var skeletonData = binary.ReadSkeletonData(stream);

        var animations = skeletonData.Animations.Select(a => a.Name).ToArray();
        return new MonsterData(skeletonData, atlas, texture, animations);
    }

    /// <summary>.import ファイルの path= エントリを実際のファイルパスに変換する。</summary>
    static string ResolveImportPath(string importFile, string toolsRoot)
    {
        var content = File.ReadAllText(importFile);
        var m = ImportPathRegex.Match(content);
        if (!m.Success)
            throw new InvalidDataException($"path= not found in {importFile}");
        // "res://.godot/imported/..." → toolsRoot + ".godot/imported/..."
        return Path.Combine(toolsRoot, m.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Godot の .ctex ファイル（BC7 または WebP）を SKBitmap に変換する。</summary>
    public static SKBitmap LoadCtexAsSKBitmap(string ctexPath)
    {
        var data = File.ReadAllBytes(ctexPath);
        if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "GST2")
            throw new InvalidDataException($"Not a GST2 ctex: {ctexPath}");

        var width      = (int)BitConverter.ToUInt32(data, 8);
        var height     = (int)BitConverter.ToUInt32(data, 12);
        var dataFormat = BitConverter.ToUInt32(data, 36);

        const int hdr = 52;
        if (dataFormat == 2)
        {
            // WebP
            var size = (int)BitConverter.ToUInt32(data, hdr);
            using var ms = new System.IO.MemoryStream(data, hdr + 4, size);
            using var isImg = ISImage.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(ms);
            return ToSKBitmap(isImg);
        }
        else
        {
            // BC7 → decode to raw RGBA
            var bc7Data  = new ReadOnlyMemory<byte>(data, hdr, data.Length - hdr);
            var decoder  = new BcDecoder();
            var pixels   = decoder.DecodeRaw(bc7Data.ToArray(), width, height, CompressionFormat.Bc7);
            var bytes    = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
            var bitmap   = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                bitmap.InstallPixels(bitmap.Info, handle.AddrOfPinnedObject(), bitmap.RowBytes);
                var safe = bitmap.Copy();
                bitmap.Dispose();
                return safe;
            }
            finally
            {
                handle.Free();
            }
        }
    }

    static SKBitmap ToSKBitmap(ISRgba32 img)
    {
        var bitmap = new SKBitmap(img.Width, img.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    bitmap.SetPixel(x, y, new SKColor(p.R, p.G, p.B, p.A));
                }
            }
        });
        return bitmap;
    }
}

/// <summary>
/// spine-csharp の TextureLoader 実装。SKBitmap を AtlasPage に紐付ける。
/// </summary>
class SkiaTextureLoader : TextureLoader
{
    readonly SKBitmap _bitmap;
    public SkiaTextureLoader(SKBitmap bitmap) => _bitmap = bitmap;

    public void Load(AtlasPage page, string path)
    {
        page.rendererObject = _bitmap;
        page.width  = _bitmap.Width;
        page.height = _bitmap.Height;
    }

    public void Unload(object texture) { }
}
