import Foundation

/// カード枠色の判定プロファイル。ピクセル (r,g,b) が枠色かを返す述語を持つ。
/// C# FrameColorProfile.cs の移植（実測 RGB しきい値は逐語一致）。
public struct FrameColorProfile: Sendable {
    public let name: String
    private let matches: @Sendable (Int, Int, Int) -> Bool

    public init(name: String, matches: @escaping @Sendable (Int, Int, Int) -> Bool) {
        self.name = name
        self.matches = matches
    }

    public func matches(_ r: Int, _ g: Int, _ b: Int) -> Bool { matches(r, g, b) }

    /// Defect の実測青枠（B が突出した暗いティール青）。
    public static let defectBlue = FrameColorProfile(name: "Defect") { r, g, b in
        b >= 95 && (b - r) >= 50 && (b - g) >= 14 && r <= 80
    }

    /// Ironclad の実測赤茶枠（≈ RGB(137,54,37)）。
    public static let ironcladRed = FrameColorProfile(name: "Ironclad") { r, g, b in
        r >= 90 && r <= 150 && g >= 38 && g <= 80 && b >= 22 && b <= 60 &&
        (r - g) >= 55 && (r - g) <= 105 && (r - b) >= 60
    }

    /// Silent の実測くすんだ緑枠（≈ RGB(68,116,33)）。
    public static let silentGreen = FrameColorProfile(name: "Silent") { r, g, b in
        (g - r) >= 30 && (g - b) >= 50 && g >= 80 && g <= 140 &&
        r >= 30 && r <= 95 && b >= 12 && b <= 55
    }

    /// Necrobinder の実測赤紫／ローズ枠（≈ RGB(142,66,94)、B > G が特徴）。
    public static let necrobinderRose = FrameColorProfile(name: "Necrobinder") { r, g, b in
        (r - g) >= 50 && (r - g) <= 95 && (b - g) >= 12 &&
        (r - b) >= 20 && (r - b) <= 70 &&
        r >= 95 && r <= 170 && g >= 40 && g <= 90 && b >= 55 && b <= 120
    }

    /// Regent の実測オレンジ枠（≈ RGB(171,90,0)、B がほぼ 0）。
    public static let regentOrange = FrameColorProfile(name: "Regent") { r, g, b in
        (r - g) >= 50 && (r - g) <= 120 && (g - b) >= 40 && b <= 35 &&
        r >= 130 && r <= 215 && g >= 50 && g <= 130
    }

    /// colorless の実測グレー枠（≈ RGB(92,92,92)、無彩色・中明度）。
    public static let colorlessGray = FrameColorProfile(name: "Colorless") { r, g, b in
        let mx = max(r, g, b), mn = min(r, g, b)
        return (mx - mn) <= 16 && mx >= 70 && mx <= 135
    }
}

/// 固定スロットのカード外枠リング帯をサンプリングし、最も枠色がヒットするキャラ
/// （正規化大文字、例 "DEFECT"）を推定する。識別の候補プールを絞り込むためにのみ使う。
/// colorless 枠は空文字 "" を返す。C# FrameColorClassifier.cs の移植。
public enum FrameColorClassifier {
    /// キャラ別実測プロファイル＋colorless（"" キー）。
    static let candidates: [(key: String, profile: FrameColorProfile)] = [
        ("DEFECT", .defectBlue),
        ("IRONCLAD", .ironcladRed),
        ("SILENT", .silentGreen),
        ("NECROBINDER", .necrobinderRose),
        ("REGENT", .regentOrange),
        ("", .colorlessGray),
    ]

    /// cardRect（フレーム座標のカード全体矩形）の外周リング帯を走査し、
    /// 各キャラプロファイルのヒット率最大のキャラキーを返す。閾値未満は nil。
    public static func classify(frame: RGBAImage, cardRect: RectI) -> String? {
        let rect = cardRect.intersect(RectI(x: 0, y: 0, width: frame.width, height: frame.height))
        guard rect.width >= 8, rect.height >= 8 else { return nil }

        let bx = max(1, Int(Double(rect.width) * RecognitionConstants.ringBorderFrac))
        let by = max(1, Int(Double(rect.height) * RecognitionConstants.ringBorderFrac))

        var hits = [Int](repeating: 0, count: candidates.count)
        var ring = 0
        for y in 0..<rect.height {
            let yEdge = y < by || y >= rect.height - by
            for x in 0..<rect.width {
                // 外周リング帯（上下 by / 左右 bx）のみ対象。内側のアートは無視する。
                if !yEdge && x >= bx && x < rect.width - bx { continue }
                let (r, g, b, _) = frame.rgba(rect.x + x, rect.y + y)
                ring += 1
                for c in candidates.indices where candidates[c].profile.matches(r, g, b) {
                    hits[c] += 1
                }
            }
        }
        guard ring > 0 else { return nil }

        var best = -1
        var bestRatio = 0.0
        for c in candidates.indices {
            let ratio = Double(hits[c]) / Double(ring)
            if ratio > bestRatio { bestRatio = ratio; best = c }
        }
        return bestRatio >= RecognitionConstants.ringMinHitRatio && best >= 0
            ? candidates[best].key : nil
    }
}
