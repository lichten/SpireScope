using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace StS2Capture.Recognition;

/// <summary>
/// Windows 標準 OCR（<see cref="OcrEngine"/>）の生成と実行を共有するヘルパー。
/// カード名（<see cref="OcrCardRecognizer"/>）とエンシェントレリック名（<see cref="AncientRelicRecognizer"/>）で共用する。
/// </summary>
public static class OcrEngineHelper
{
    /// <summary>ユーザのプロファイル言語（多くは日本語）でエンジンを作る。無ければ英語。失敗時 null。</summary>
    public static OcrEngine? CreateEngine()
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is not null) return engine;
        try { return OcrEngine.TryCreateFromLanguage(new Language("en")); }
        catch { return null; }
    }

    /// <summary>Bitmap を OCR して結果を返す（失敗時 null）。</summary>
    public static OcrResult? RunOcr(OcrEngine engine, Bitmap bmp)
    {
        SoftwareBitmap? sb = null;
        try { sb = ToSoftwareBitmap(bmp); }
        catch { return null; }
        try { return engine.RecognizeAsync(sb).AsTask().GetAwaiter().GetResult(); }
        catch { return null; }
        finally { sb?.Dispose(); }
    }

    /// <summary>System.Drawing.Bitmap → SoftwareBitmap(Bgra8)。</summary>
    public static SoftwareBitmap ToSoftwareBitmap(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        var bytes = new byte[w * 4 * h];
        try
        {
            for (int y = 0; y < h; y++)
                Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * w * 4, w * 4);
        }
        finally { bmp.UnlockBits(data); }

        var sb = new SoftwareBitmap(BitmapPixelFormat.Bgra8, w, h, BitmapAlphaMode.Premultiplied);
        using var dw = new DataWriter();
        dw.WriteBytes(bytes);
        sb.CopyFromBuffer(dw.DetachBuffer());
        return sb;
    }
}
