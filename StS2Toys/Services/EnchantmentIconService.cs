using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Pfim;

namespace StS2Toys.Services;

static class EnchantmentIconService
{
    const int CtexHeaderOffset = 52;

    static string? _extractedDir;
    static bool _dirSearched;
    static readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);

    static readonly Regex _ctexPathRe =
        new(@"path\.bptc=""(res://[^""]+)""", RegexOptions.Compiled);

    public static Bitmap? GetEnchantmentBitmap(string enchantmentId)
    {
        var name = ToName(enchantmentId);
        if (_cache.TryGetValue(name, out var cached)) return cached;
        return _cache[name] = LoadIcon(name);
    }

    static string ToName(string id)
    {
        var raw = id.Contains('.') ? id[(id.LastIndexOf('.') + 1)..] : id;
        return raw.ToLowerInvariant();
    }

    static Bitmap? LoadIcon(string name)
    {
        try
        {
            var dir = GetExtractedDir();
            if (dir is null) return null;

            var importPath = Path.Combine(dir, "images", "enchantments", $"{name}.png.import");
            if (!File.Exists(importPath)) return null;

            string? ctexRel = null;
            foreach (var line in File.ReadLines(importPath))
            {
                var m = _ctexPathRe.Match(line);
                if (m.Success) { ctexRel = m.Groups[1].Value; break; }
            }
            if (ctexRel is null) return null;

            // "res://.godot/imported/..." → 絶対パス
            var ctexPath = Path.Combine(dir,
                ctexRel["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(ctexPath)) return null;

            return DecodeCtex(ctexPath);
        }
        catch { return null; }
    }

    static Bitmap DecodeCtex(string ctexPath)
    {
        using var fs = File.OpenRead(ctexPath);
        using var br = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);

        fs.Seek(8, SeekOrigin.Begin);
        int width  = (int)br.ReadUInt32();
        int height = (int)br.ReadUInt32();

        fs.Seek(CtexHeaderOffset, SeekOrigin.Begin);
        var bc7 = new byte[fs.Length - CtexHeaderOffset];
        fs.ReadExactly(bc7);

        using var ms = new MemoryStream();
        ms.Write(BuildDdsHeader(width, height, bc7.Length));
        ms.Write(bc7);
        ms.Position = 0;

        using var image = Pfimage.FromStream(ms);
        var px = image.Data;
        for (int i = 0; i < px.Length; i += 4)
            (px[i], px[i + 2]) = (px[i + 2], px[i]);

        var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        var bd  = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                      ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int rowBytes = image.Width * 4;
            for (int row = 0; row < image.Height; row++)
                Marshal.Copy(px, row * image.Stride,
                    IntPtr.Add(bd.Scan0, row * bd.Stride), rowBytes);
        }
        finally { bmp.UnlockBits(bd); }

        return bmp;
    }

    static byte[] BuildDdsHeader(int width, int height, int dataSize)
    {
        using var ms = new MemoryStream(148);
        using var bw = new BinaryWriter(ms);
        bw.Write(0x20534444u);
        bw.Write(124u); bw.Write(0x81007u);
        bw.Write((uint)height); bw.Write((uint)width); bw.Write((uint)dataSize);
        bw.Write(0u); bw.Write(1u);
        for (int i = 0; i < 11; i++) bw.Write(0u);
        bw.Write(32u); bw.Write(0x4u); bw.Write(0x30315844u);
        bw.Write(0u); bw.Write(0u); bw.Write(0u); bw.Write(0u); bw.Write(0u);
        bw.Write(0x1000u);
        bw.Write(0u); bw.Write(0u); bw.Write(0u); bw.Write(0u);
        bw.Write(98u); bw.Write(3u); bw.Write(0u); bw.Write(1u); bw.Write(0u);
        return ms.ToArray();
    }

    static string? GetExtractedDir()
    {
        if (_dirSearched) return _extractedDir;
        _dirSearched = true;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "extracted");
            if (Directory.Exists(candidate)) return _extractedDir = candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
