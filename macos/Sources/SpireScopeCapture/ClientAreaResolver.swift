import Foundation

/// フレーム内の「ゲーム描画領域」（クライアント矩形）を推定する。
/// macOS では SCK のウィンドウキャプチャがそのまま描画内容なので原則フレーム全体だが、
/// フルスクリーン時にモニタのアスペクト比が 16:9 でないと上下（または左右）に黒帯が入る。
/// 純黒に近い帯を端から検出して切り詰める（Windows 版 WindowClientArea.Resolve の代替）。
public enum ClientAreaResolver {
    /// 黒帯とみなす輝度上限（max(R,G,B) がこの値以下）。ゲーム画面の暗部より厳しくする。
    static let blackMax = 6
    /// 行・列を黒帯と判定するのに必要な黒ピクセル比。
    static let blackRatio = 0.99
    /// 片側あたりの最大トリム比（誤検出の暴走防止）。
    static let maxTrimFrac = 0.25

    public static func resolve(_ buffer: RGBAImage) -> RectI {
        let w = buffer.width, h = buffer.height
        var top = 0, bottom = h, left = 0, right = w
        let maxV = Int(Double(h) * maxTrimFrac)
        let maxH = Int(Double(w) * maxTrimFrac)

        while top < maxV, isBlackRow(buffer, y: top) { top += 1 }
        while bottom > h - maxV, isBlackRow(buffer, y: bottom - 1) { bottom -= 1 }
        while left < maxH, isBlackCol(buffer, x: left, top: top, bottom: bottom) { left += 1 }
        while right > w - maxH, isBlackCol(buffer, x: right - 1, top: top, bottom: bottom) { right -= 1 }

        let rect = RectI.fromLTRB(left, top, right, bottom)
        return rect.isEmpty ? RectI(x: 0, y: 0, width: w, height: h) : rect
    }

    private static func isBlackRow(_ b: RGBAImage, y: Int) -> Bool {
        var black = 0, total = 0
        var x = 0
        while x < b.width {
            let (r, g, bl, _) = b.rgba(x, y)
            total += 1
            if max(r, g, bl) <= blackMax { black += 1 }
            x += 4 // 高速化のため 4px おきにサンプリング
        }
        return total > 0 && Double(black) / Double(total) >= blackRatio
    }

    private static func isBlackCol(_ b: RGBAImage, x: Int, top: Int, bottom: Int) -> Bool {
        var black = 0, total = 0
        var y = top
        while y < bottom {
            let (r, g, bl, _) = b.rgba(x, y)
            total += 1
            if max(r, g, bl) <= blackMax { black += 1 }
            y += 4
        }
        return total > 0 && Double(black) / Double(total) >= blackRatio
    }
}
