using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using SkiaSharp;
using Spine;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISImage  = SixLabors.ImageSharp.Image;
using ISRgba32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;

public record MonsterData(SkeletonData SkeletonData, Atlas Atlas, SKBitmap Texture, string[] Animations);

static class SpineLoader
{
    static readonly Regex ImportPathRegex = new(@"path=""res://(.+?)""", RegexOptions.Compiled);

    public static MonsterData Load(string monsterDir, string toolsRoot)
    {
        var skelImport  = Directory.GetFiles(monsterDir, "*.skel.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.skel.import not found in {monsterDir}");
        var atlasImport = Directory.GetFiles(monsterDir, "*.atlas.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.atlas.import not found in {monsterDir}");
        return LoadFromImports(skelImport, atlasImport, toolsRoot);
    }

    /// <summary>
    /// 明示的な .skel.import / .atlas.import を指定してロードする
    /// （creature_visuals の .tscn → .tres で解決した特定リグ用）。
    /// テクスチャは atlas_data 先頭行のページ名から同フォルダの .png.import を引く。
    /// </summary>
    public static MonsterData LoadFromImports(string skelImport, string atlasImport, string toolsRoot)
    {
        var spskelPath  = ResolveImportPath(skelImport, toolsRoot);
        var spatlasPath = ResolveImportPath(atlasImport, toolsRoot);

        using var spatlasDoc = JsonDocument.Parse(File.ReadAllText(spatlasPath));
        var atlasText     = spatlasDoc.RootElement.GetProperty("atlas_data").GetString()!;

        // atlas_data 先頭の非空行 = ページ画像名（例 "bowlbug.png"）
        var atlasDir = Path.GetDirectoryName(atlasImport)!;
        var pageName = atlasText.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        string? pngImport = null;
        if (pageName is not null)
        {
            var candidate = Path.Combine(atlasDir, pageName + ".import");
            if (File.Exists(candidate)) pngImport = candidate;
        }
        pngImport ??= Directory.GetFiles(atlasDir, "*.png.import").FirstOrDefault()
            ?? throw new FileNotFoundException($"*.png.import not found in {atlasDir}");
        var ctexPath  = ResolveImportPath(pngImport, toolsRoot);
        var texture   = LoadCtexAsSKBitmap(ctexPath);

        var textureLoader = new SkiaTextureLoader(texture);
        var atlas = new Atlas(new StringReader(atlasText), "", textureLoader);

        var attachmentLoader = new AtlasAttachmentLoader(atlas);
        var binary           = new SkeletonBinary(attachmentLoader);
        using var stream = File.OpenRead(spskelPath);
        var skeletonData = binary.ReadSkeletonData(stream);

        var animations = skeletonData.Animations.Select(a => a.Name).ToArray();
        return new MonsterData(skeletonData, atlas, texture, animations);
    }

    public static string ResolveImportPath(string importFile, string toolsRoot)
    {
        var content = File.ReadAllText(importFile);
        var m = ImportPathRegex.Match(content);
        if (!m.Success)
            throw new InvalidDataException($"path= not found in {importFile}");
        return Path.Combine(toolsRoot, m.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar));
    }

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
            var size = (int)BitConverter.ToUInt32(data, hdr);
            using var ms = new System.IO.MemoryStream(data, hdr + 4, size);
            using var isImg = ISImage.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(ms);
            return ToSKBitmap(isImg);
        }
        else
        {
            var bc7Data  = new ReadOnlyMemory<byte>(data, hdr, data.Length - hdr);
            var decoder  = new BcDecoder();
            var pixels   = decoder.DecodeRaw(bc7Data.ToArray(), width, height, CompressionFormat.Bc7);
            var bytes    = MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray();
            var bitmap   = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
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
