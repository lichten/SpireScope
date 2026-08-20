import Foundation

/// カード全体矩形をクライアント相対で表す（中心 + 大きさ）。アート窓・枠帯はここから導出する。
public struct CardSlot: Sendable {
    public let cxFrac: Double
    public let cyFrac: Double
    public let wFrac: Double
    public let hFrac: Double

    public init(_ cxFrac: Double, _ cyFrac: Double, _ wFrac: Double, _ hFrac: Double) {
        self.cxFrac = cxFrac
        self.cyFrac = cyFrac
        self.wFrac = wFrac
        self.hFrac = hFrac
    }

    /// クライアント相対のスロットをフレームのピクセル矩形へ変換する。
    /// C# ScreenRecognizer.ToPixels と同じ切り捨て（(int) キャスト）で較正値の互換を保つ。
    public func toPixels(client: RectI) -> RectI {
        let w = max(1, Int(Double(client.width) * wFrac))
        let h = max(1, Int(Double(client.height) * hFrac))
        let cx = client.x + Int(Double(client.width) * cxFrac)
        let cy = client.y + Int(Double(client.height) * cyFrac)
        return RectI(x: cx - w / 2, y: cy - h / 2, width: w, height: h)
    }
}

/// 認識パイプラインの較正定数を 1 箇所に集約する（再較正はここだけ触る）。
/// 全て Windows 版（1287×765 ウィンドウ / 16:9 クライアント）で実測した値。
public enum RecognitionConstants {
    // ── ScreenRecognizer（カード照合）───────────────────────────────
    /// 採否しきい値（best がこの距離以内なら採用）。
    public static let cardMaxDistance = 0.85
    /// best と 2 位の最小マージン（タイは不採用）。
    public static let cardMinMargin = 0.04
    /// カード選択画面とみなす最小の確信一致カード数。
    public static let minCardsForSelect = 2
    /// カード選択判定に必要な 1 枚あたり最低確信度。
    public static let minSelectConfidence = 0.30
    /// portrait 下端トリム（種別装飾を除外。DB 構築とアート窓の両方で使い、対応を保つ）。
    public static let portraitBottomTrim = 0.15

    /// 「カードを選択」画面の 3 カードの全体矩形（クライアント相対）。
    public static let cardSelectSlots: [CardSlot] = [
        CardSlot(0.320, 0.597, 0.127, 0.263),
        CardSlot(0.500, 0.597, 0.127, 0.263),
        CardSlot(0.681, 0.597, 0.127, 0.263),
    ]

    /// ショップ画面の売り物カードの全体矩形（上段 5＋下段 2、クライアント相対）。
    public static let shopCardSlots: [CardSlot] = [
        CardSlot(0.230, 0.376, 0.103, 0.214), CardSlot(0.366, 0.374, 0.104, 0.218),
        CardSlot(0.501, 0.376, 0.104, 0.214), CardSlot(0.635, 0.376, 0.104, 0.214),
        CardSlot(0.772, 0.374, 0.104, 0.214),
        CardSlot(0.264, 0.721, 0.104, 0.214), CardSlot(0.406, 0.721, 0.104, 0.214),
    ]

    // ── ShopItemRecognizer（レリック／ポーション）──────────────────
    /// DB の透過部分を合成するショップ背景色（羊皮紙）。
    public static let shopBackground = HsvHistogram.RGB(78, 118, 106)
    public static let shopMaxDistance = 0.40
    /// best からこの距離差以内の候補は「同点」とみなし併せて列挙する（タイ窓）。
    public static let shopMinMargin = 0.02
    public static let shopMaxCandidates = 3
    /// この数以上のスロットが強一致したらショップ画面とみなす。
    public static let minMatchesForShop = 2
    /// スロット正方形の一辺（クライアント高さに対する比）。
    public static let shopSlotSizeFrac = 0.072
    /// ショップの HSV S/V ビン数（小アイコン識別のため既定 3 より細かく）。
    public static let shopSatBins = 4
    public static let shopValBins = 4

    /// レリック行 y≈0.6225・ポーション行 y≈0.744、列 x≈0.518/0.5925/0.6693（クライアント相対）。
    public static let shopSlots: [(kind: ShopItemKind, cxFrac: Double, cyFrac: Double)] = [
        (.relic, 0.518, 0.6225), (.relic, 0.5925, 0.6225), (.relic, 0.6693, 0.6225),
        (.potion, 0.518, 0.744), (.potion, 0.5925, 0.744), (.potion, 0.6693, 0.744),
    ]

    // ── FrameColorClassifier ──────────────────────────────────────
    /// 最尤キャラのヒット率がこの値未満なら判定不能（nil）。
    public static let ringMinHitRatio = 0.16
    /// 外枠リング帯の太さ（カード辺長に対する比）。
    public static let ringBorderFrac = 0.07

    // ── AncientRelicRecognizer ────────────────────────────────────
    /// 名前バンド（テスカタラ 1287×765 実測較正値）。アイコン右の名前テキストを囲む。
    public static let ancientNameBands: [CardSlot] = [
        CardSlot(0.382, 0.719, 0.21, 0.042),
        CardSlot(0.382, 0.809, 0.21, 0.042),
        CardSlot(0.382, 0.899, 0.21, 0.042),
    ]
    /// タイトル帯の拡大倍率（二値化 OCR 前処理）。
    public static let ancientTitleScale = 4
    /// この数以上の行が一致したらエンシェントレリック選択画面とみなす。
    public static let ancientMinMatches = 2
    /// 名前照合の許容編集距離（family を緩めに作りアイコンで一意化）。
    public static let ancientMaxNameDist = 2
    /// 名前バンドの縦スキャン範囲（クライアント高さ比）と試行数（中心＋上下対称）。
    public static let ancientBandScanRange = 0.035
    public static let ancientBandScanSteps = 7
    /// アイコン中心の名前バンド中心からの水平オフセット（クライアント幅比。負＝左）。
    public static let ancientIconDxFrac = -0.112
    /// アイコン正方形の一辺（クライアント高さ比）。
    public static let ancientIconSizeFrac = 0.052
    /// アイコン照合の採否しきい値（カイ二乗、これ以下で採用）。
    public static let ancientIconMaxDistance = 0.7
    /// DB アイコンを合成する背景色（エンシェント画面の暗い半透明バー）。
    public static let ancientIconBackground = HsvHistogram.RGB(38, 38, 38)
    public static let ancientIconSatBins = 4
    public static let ancientIconValBins = 4

    // ── CaptureLoop ───────────────────────────────────────────────
    /// ポーリング間隔（ミリ秒）。
    public static let captureIntervalMs = 800
}

/// カード矩形からアート窓・タイトル帯を導出するジオメトリ（C# CardRegionDetector の該当部の移植）。
/// 縦の単位にカード「幅」を使い解像度不変にする。下 15% トリムは portrait DB 側と対で、
/// 片方を変えたら必ずもう片方も合わせること。
public enum CardGeometry {
    /// カード矩形から絵（portrait）の窓を算出する。上 +0.05×幅・高さ 0.55×0.85×幅・左右 0.04×幅。
    public static func artRegionOf(_ card: RectI) -> RectI {
        let topFrac = 0.05, heightFrac = 0.55 * 0.85, sideInset = 0.04
        let x1 = card.left + Int(Double(card.width) * sideInset)
        let x2 = card.right - Int(Double(card.width) * sideInset)
        let y1 = card.top + Int(Double(card.width) * topFrac)
        let y2 = y1 + Int(Double(card.width) * heightFrac)
        return RectI.fromLTRB(x1, y1, max(x1 + 4, x2), max(y1 + 4, y2))
    }
}
