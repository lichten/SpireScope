import CoreGraphics

/// キャプチャした 1 フレーム。CGImage は不変オブジェクトなので actor 間を安全に渡せる。
public struct Frame: @unchecked Sendable {
    public let image: CGImage
    public var width: Int { image.width }
    public var height: Int { image.height }
    public var rect: RectI { RectI(x: 0, y: 0, width: width, height: height) }

    public init(image: CGImage) {
        self.image = image
    }
}

/// C# System.Drawing.Rectangle 相当の整数矩形。スロット座標の切り捨て・交差の挙動を
/// Windows 版の較正値と一致させるため、CGRect（浮動小数）ではなく整数で持つ。
public struct RectI: Sendable, Equatable {
    public var x: Int
    public var y: Int
    public var width: Int
    public var height: Int

    public init(x: Int, y: Int, width: Int, height: Int) {
        self.x = x
        self.y = y
        self.width = width
        self.height = height
    }

    public static func fromLTRB(_ left: Int, _ top: Int, _ right: Int, _ bottom: Int) -> RectI {
        RectI(x: left, y: top, width: right - left, height: bottom - top)
    }

    public var left: Int { x }
    public var top: Int { y }
    public var right: Int { x + width }
    public var bottom: Int { y + height }
    public var isEmpty: Bool { width <= 0 || height <= 0 }

    public func intersect(_ other: RectI) -> RectI {
        let l = max(left, other.left), t = max(top, other.top)
        let r = min(right, other.right), b = min(bottom, other.bottom)
        return r > l && b > t ? RectI.fromLTRB(l, t, r, b) : RectI(x: 0, y: 0, width: 0, height: 0)
    }

    public var cgRect: CGRect {
        CGRect(x: CGFloat(x), y: CGFloat(y), width: CGFloat(width), height: CGFloat(height))
    }
}
