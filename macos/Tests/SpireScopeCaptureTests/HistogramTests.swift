import CoreGraphics
import Foundation
import Testing
@testable import SpireScopeCapture

/// 単色 CGImage を作る（alpha 0-255）。
private func solidImage(r: Int, g: Int, b: Int, a: Int = 255,
                        width: Int = 32, height: Int = 32) -> CGImage {
    let ctx = CGContext(
        data: nil, width: width, height: height,
        bitsPerComponent: 8, bytesPerRow: width * 4,
        space: CGColorSpace(name: CGColorSpace.sRGB)!,
        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    ctx.setFillColor(CGColor(srgbRed: CGFloat(r) / 255, green: CGFloat(g) / 255,
                             blue: CGFloat(b) / 255, alpha: CGFloat(a) / 255))
    ctx.fill(CGRect(x: 0, y: 0, width: width, height: height))
    return ctx.makeImage()!
}

@Suite struct HsvHistogramTests {
    @Test func toHsvPrimaries() {
        // 純赤: H=0, S=1, V=1
        let red = HsvHistogram.toHsv(255, 0, 0)
        #expect(abs(red.h - 0) < 1e-9 && abs(red.s - 1) < 1e-9 && abs(red.v - 1) < 1e-9)
        // 純緑: H=1/3
        let green = HsvHistogram.toHsv(0, 255, 0)
        #expect(abs(green.h - 1.0 / 3) < 1e-9)
        // 純青: H=2/3
        let blue = HsvHistogram.toHsv(0, 0, 255)
        #expect(abs(blue.h - 2.0 / 3) < 1e-9)
        // 無彩色: S=0
        let gray = HsvHistogram.toHsv(128, 128, 128)
        #expect(gray.s == 0)
    }

    @Test func solidColorFillsSingleBin() {
        let img = solidImage(r: 255, g: 0, b: 0)
        let hist = HsvHistogram.compute(img, region: RectI(x: 0, y: 0, width: 32, height: 32))
        // 純赤 → hue bin 0, s bin 2, v bin 2 → index (0*3+2)*3+2 = 8
        #expect(abs(hist[8] - 1.0) < 1e-6)
        #expect(hist.reduce(Float(0), +).isApproximatelyOne)
    }

    @Test func chiSquareProperties() {
        let img1 = solidImage(r: 255, g: 0, b: 0)
        let img2 = solidImage(r: 0, g: 0, b: 255)
        let h1 = HsvHistogram.compute(img1, region: RectI(x: 0, y: 0, width: 32, height: 32))
        let h2 = HsvHistogram.compute(img2, region: RectI(x: 0, y: 0, width: 32, height: 32))
        // 同一 → 0、異色 → 正、対称。
        #expect(HsvHistogram.chiSquare(h1, h1) == 0)
        #expect(HsvHistogram.chiSquare(h1, h2) > 1.0)
        #expect(abs(HsvHistogram.chiSquare(h1, h2) - HsvHistogram.chiSquare(h2, h1)) < 1e-12)
    }

    @Test func compositeBackgroundReplacesTransparency() {
        // 全透過画像を青背景に合成 → 純青のヒストグラムに一致する。
        let clear = solidImage(r: 0, g: 0, b: 0, a: 0)
        let hist = HsvHistogram.compute(
            clear, region: RectI(x: 0, y: 0, width: 32, height: 32),
            compositeBg: HsvHistogram.RGB(0, 0, 255))
        let blue = HsvHistogram.compute(
            solidImage(r: 0, g: 0, b: 255), region: RectI(x: 0, y: 0, width: 32, height: 32))
        #expect(HsvHistogram.chiSquare(hist, blue) < 0.01)
    }

    @Test func alphaBoundingBoxTrimsTransparentPadding() {
        // 中央 8×8 のみ不透明な 32×32。
        let ctx = CGContext(
            data: nil, width: 32, height: 32,
            bitsPerComponent: 8, bytesPerRow: 32 * 4,
            space: CGColorSpace(name: CGColorSpace.sRGB)!,
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
        ctx.setFillColor(CGColor(srgbRed: 1, green: 0, blue: 0, alpha: 1))
        ctx.fill(CGRect(x: 12, y: 12, width: 8, height: 8))
        let img = ctx.makeImage()!
        let buffer = RGBAImage(cgImage: img)!
        let bbox = buffer.alphaBoundingBox()
        #expect(bbox == RectI(x: 12, y: 12, width: 8, height: 8))
    }
}

extension Float {
    var isApproximatelyOne: Bool { abs(self - 1) < 1e-5 }
}
