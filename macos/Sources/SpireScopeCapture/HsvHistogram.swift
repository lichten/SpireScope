import CoreGraphics
import Foundation

/// HSV カラーヒストグラム（H12×S3×V3=108bin）による画像照合の共有ユーティリティ。
/// C# StS2Capture.Core/Recognition/HsvHistogram.cs の移植。カード絵・レリック・ポーションで共用。
/// 透過 PNG は compositeBg で背景色に合成してから集計できる（背景を共通定数化すると前景の差だけが
/// 効き、小アイコンでも識別が成立する）。
public enum HsvHistogram {
    public static let hueBins = 12
    public static let satBins = 3
    public static let valBins = 3
    public static let sampleN = 40

    public struct RGB: Sendable, Equatable {
        public let r: Int, g: Int, b: Int
        public init(_ r: Int, _ g: Int, _ b: Int) {
            self.r = r; self.g = g; self.b = b
        }
    }

    /// region を sampleN×sampleN に縮小して正規化 HSV ヒストグラムを返す。
    /// compositeBg 指定時は α 合成（透過部分を背景色に）してから集計する。
    public static func compute(_ image: CGImage, region: RectI, compositeBg: RGB? = nil,
                               sb: Int = satBins, vb: Int = valBins) -> [Float] {
        var hist = [Float](repeating: 0, count: hueBins * sb * vb)
        guard let small = RGBAImage.resampled(image, region: region, width: sampleN, height: sampleN)
        else { return hist }

        var count = 0
        for y in 0..<sampleN {
            for x in 0..<sampleN {
                var (r, g, b, a) = small.rgba(x, y)
                if let bg = compositeBg, a < 255 {
                    // premultiplied 値は既に r×α なので、背景×(1-α) を足すだけで
                    // C# の r*af + bg*(1-af)（非乗算）と等価になる。
                    let af = Double(a) / 255.0
                    r = min(255, r + Int(Double(bg.r) * (1 - af)))
                    g = min(255, g + Int(Double(bg.g) * (1 - af)))
                    b = min(255, b + Int(Double(bg.b) * (1 - af)))
                }
                let (h, s, v) = toHsv(r, g, b)
                let hi = min(hueBins - 1, Int(h * Double(hueBins)))
                let si = min(sb - 1, Int(s * Double(sb)))
                let vi = min(vb - 1, Int(v * Double(vb)))
                hist[(hi * sb + si) * vb + vi] += 1
                count += 1
            }
        }
        if count > 0 {
            for i in hist.indices { hist[i] /= Float(count) }
        }
        return hist
    }

    /// 2 つの正規化ヒストグラムのカイ二乗距離（0 に近いほど類似）。
    public static func chiSquare(_ u: [Float], _ v: [Float]) -> Double {
        var s = 0.0
        for i in u.indices {
            let d = Double(u[i]) - Double(v[i])
            let den = Double(u[i]) + Double(v[i])
            if den > 0 { s += d * d / den }
        }
        return s
    }

    /// RGB(0-255) → HSV(0..1)。C# 版と同じ max/min 手計算。
    public static func toHsv(_ r: Int, _ g: Int, _ b: Int) -> (h: Double, s: Double, v: Double) {
        let rf = Double(r) / 255.0, gf = Double(g) / 255.0, bf = Double(b) / 255.0
        let mx = max(rf, gf, bf), mn = min(rf, gf, bf)
        let delta = mx - mn
        let v = mx
        let s = mx <= 0 ? 0 : delta / mx
        guard delta > 0 else { return (0, s, v) }
        var hue: Double
        if mx == rf {
            hue = ((gf - bf) / delta).truncatingRemainder(dividingBy: 6)
        } else if mx == gf {
            hue = (bf - rf) / delta + 2
        } else {
            hue = (rf - gf) / delta + 4
        }
        hue /= 6
        if hue < 0 { hue += 1 }
        return (hue, s, v)
    }
}
