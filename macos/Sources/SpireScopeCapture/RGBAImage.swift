import CoreGraphics
import Foundation
import ImageIO

/// CGImage から取り出した RGBA8（premultiplied last）のピクセルバッファ。
/// C# 版の LockBits（非乗算 BGRA）と違い premultiplied だが、ヒストグラムの背景合成は
/// premul 値 + 背景×(1-α) が非乗算式 r×α + 背景×(1-α) と数学的に等価なので結果は一致する。
public struct RGBAImage {
    public let width: Int
    public let height: Int
    /// 行優先・1 ピクセル 4 バイト（R,G,B,A）。stride = width*4 に正規化済み。
    public let pixels: [UInt8]

    /// CGImage 全体（または region）を 1:1 で描画してバッファ化する。
    public init?(cgImage: CGImage, region: RectI? = nil) {
        let src: CGImage
        if let region {
            let clipped = region.intersect(RectI(x: 0, y: 0, width: cgImage.width, height: cgImage.height))
            guard !clipped.isEmpty, let cropped = cgImage.cropping(to: clipped.cgRect) else { return nil }
            src = cropped
        } else {
            src = cgImage
        }
        guard let img = Self.render(src, width: src.width, height: src.height, interpolate: false)
        else { return nil }
        self = img
    }

    /// CGImage の region を width×height に縮小／拡大（高品質補間）してバッファ化する。
    /// C# HsvHistogram.Compute の HighQualityBicubic 縮小、ImageOps の拡大に相当する。
    public static func resampled(_ cgImage: CGImage, region: RectI, width: Int, height: Int) -> RGBAImage? {
        let clipped = region.intersect(RectI(x: 0, y: 0, width: cgImage.width, height: cgImage.height))
        guard !clipped.isEmpty, let cropped = cgImage.cropping(to: clipped.cgRect) else { return nil }
        return render(cropped, width: width, height: height, interpolate: true)
    }

    private init(width: Int, height: Int, pixels: [UInt8]) {
        self.width = width
        self.height = height
        self.pixels = pixels
    }

    private static func render(_ cgImage: CGImage, width: Int, height: Int, interpolate: Bool) -> RGBAImage? {
        guard width > 0, height > 0 else { return nil }
        var pixels = [UInt8](repeating: 0, count: width * height * 4)
        let ok = pixels.withUnsafeMutableBytes { buf -> Bool in
            guard let ctx = CGContext(
                data: buf.baseAddress,
                width: width, height: height,
                bitsPerComponent: 8, bytesPerRow: width * 4,
                space: CGColorSpace(name: CGColorSpace.sRGB)!,
                bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)
            else { return false }
            ctx.interpolationQuality = interpolate ? .high : .none
            ctx.draw(cgImage, in: CGRect(x: 0, y: 0, width: width, height: height))
            return true
        }
        return ok ? RGBAImage(width: width, height: height, pixels: pixels) : nil
    }

    /// (x, y) の RGBA。CGContext は上原点で描画するので y はそのまま画像上端基準。
    @inline(__always)
    public func rgba(_ x: Int, _ y: Int) -> (r: Int, g: Int, b: Int, a: Int) {
        let p = (y * width + x) * 4
        return (Int(pixels[p]), Int(pixels[p + 1]), Int(pixels[p + 2]), Int(pixels[p + 3]))
    }

    /// α>=16 のピクセルの外接矩形（透過余白を除いたアート部分）。全透過なら全体を返す。
    /// C# HsvHistogram.AlphaBoundingBox 相当。
    public func alphaBoundingBox() -> RectI {
        var minX = width, minY = height, maxX = -1, maxY = -1
        for y in 0..<height {
            let row = y * width * 4
            for x in 0..<width where pixels[row + x * 4 + 3] >= 16 {
                if x < minX { minX = x }
                if x > maxX { maxX = x }
                if y < minY { minY = y }
                if y > maxY { maxY = y }
            }
        }
        guard maxX >= 0 else { return RectI(x: 0, y: 0, width: width, height: height) }
        return RectI.fromLTRB(minX, minY, maxX + 1, maxY + 1)
    }
}

/// ファイルからの CGImage 読み込み（DB 用透過 PNG など）。
public enum ImageLoader {
    public static func load(_ url: URL) -> CGImage? {
        guard let source = CGImageSourceCreateWithURL(url as CFURL, nil) else { return nil }
        return CGImageSourceCreateImageAtIndex(source, 0, nil)
    }
}
