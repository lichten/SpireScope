import CoreGraphics
import Foundation

/// タイトル領域 OCR 用の画像前処理。C# ImageOps.cs の移植。
/// 色付きバナー背景・縁取りに弱い OCR を助けるため、領域を拡大→グレースケール→
/// Otsu 二値化して「黒文字／白背景」に正規化する。
enum ImageOps {
    /// region を切り出し scale 倍に拡大して Otsu 二値化したグレースケールバッファを返す。
    /// 戻り値は (輝度バイト列, 幅, 高さ)。文字は少数派ピクセルという仮定で多数派（背景）を白にする。
    static func cropUpscaleBinarize(_ image: CGImage, region: RectI, scale: Int)
        -> (pixels: [UInt8], width: Int, height: Int)? {
        let clipped = region.intersect(RectI(x: 0, y: 0, width: image.width, height: image.height))
        guard !clipped.isEmpty else { return nil }
        let w = max(1, clipped.width * scale)
        let h = max(1, clipped.height * scale)
        guard let scaled = RGBAImage.resampled(image, region: clipped, width: w, height: h)
        else { return nil }

        var gray = [UInt8](repeating: 0, count: w * h)
        for y in 0..<h {
            for x in 0..<w {
                let (r, g, b, _) = scaled.rgba(x, y)
                gray[y * w + x] = UInt8((r * 30 + g * 59 + b * 11) / 100)
            }
        }
        let threshold = otsuThreshold(gray)

        var below = 0
        for v in gray where Int(v) < threshold { below += 1 }
        let textIsDark = below <= gray.count - below // 暗い側が少数なら文字＝暗

        var out = [UInt8](repeating: 255, count: w * h)
        for i in gray.indices {
            let dark = Int(gray[i]) < threshold
            let ink = textIsDark ? dark : !dark
            out[i] = ink ? 0 : 255
        }
        return (out, w, h)
    }

    /// グレースケールバッファを CGImage 化する（Vision への入力・較正クロップ保存用）。
    static func grayImage(_ pixels: [UInt8], width: Int, height: Int) -> CGImage? {
        let data = Data(pixels)
        guard let provider = CGDataProvider(data: data as CFData) else { return nil }
        return CGImage(
            width: width, height: height,
            bitsPerComponent: 8, bitsPerPixel: 8, bytesPerRow: width,
            space: CGColorSpaceCreateDeviceGray(),
            bitmapInfo: CGBitmapInfo(rawValue: CGImageAlphaInfo.none.rawValue),
            provider: provider, decode: nil, shouldInterpolate: false,
            intent: .defaultIntent)
    }

    static func otsuThreshold(_ gray: [UInt8]) -> Int {
        var hist = [Int](repeating: 0, count: 256)
        for v in gray { hist[Int(v)] += 1 }
        let total = gray.count

        var sum = 0.0
        for t in 0..<256 { sum += Double(t * hist[t]) }

        var sumB = 0.0
        var wB = 0
        var maxVar = -1.0
        var threshold = 127
        for t in 0..<256 {
            wB += hist[t]
            if wB == 0 { continue }
            let wF = total - wB
            if wF == 0 { break }
            sumB += Double(t * hist[t])
            let mB = sumB / Double(wB)
            let mF = (sum - sumB) / Double(wF)
            let between = Double(wB) * Double(wF) * (mB - mF) * (mB - mF)
            if between > maxVar { maxVar = between; threshold = t }
        }
        return threshold
    }
}
