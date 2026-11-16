import Foundation

/// 認識対象の画面種別。
public enum ScreenType: String, Sendable {
    case unknown
    case cardSelect
    case shop
    case ancientSelect

    public func label(japanese: Bool) -> String {
        switch self {
        case .unknown: return japanese ? "検出結果" : "Detections"
        case .cardSelect: return japanese ? "カード選択" : "Card Select"
        case .shop: return japanese ? "ショップ" : "Shop"
        case .ancientSelect: return japanese ? "エンシェントレリック候補" : "Ancient Relics"
        }
    }
}

public enum ShopItemKind: String, Sendable {
    case relic
    case potion
}

/// 識別できたカード 1 枚。
public struct RecognizedCard: Sendable, Equatable {
    public let cardId: String
    public let region: RectI
    public let confidence: Double

    public init(cardId: String, region: RectI, confidence: Double) {
        self.cardId = cardId
        self.region = region
        self.confidence = confidence
    }
}

/// レリック／ポーションスロットの識別候補（距離が近いほど確からしい）。
public struct ShopCandidate: Sendable, Equatable {
    public let id: String
    public let distance: Double

    public init(id: String, distance: Double) {
        self.id = id
        self.distance = distance
    }
}

/// 1 スロットの結果。candidates は距離昇順。best が圏内なら採用し、タイ窓内の近接候補も併記する。
public struct ShopItem: Sendable, Equatable {
    public let kind: ShopItemKind
    public let region: RectI
    public let candidates: [ShopCandidate]
    public let accepted: Bool

    public init(kind: ShopItemKind, region: RectI, candidates: [ShopCandidate], accepted: Bool) {
        self.kind = kind
        self.region = region
        self.candidates = candidates
        self.accepted = accepted
    }
}

/// 1 フレームの認識結果（C# ScreenRecognizer.ScreenResult ＋ CaptureLoop.Result の統合）。
public struct RecognitionResult: Sendable {
    public let screen: ScreenType
    public let cards: [RecognizedCard]
    /// ショップ（relic/potion）またはエンシェント（relic のみ）のスロット結果。
    public let shopItems: [ShopItem]
    /// probe したカードスロットの矩形（診断オーバーレイ用）。
    public let cardBoxes: [RectI]
    /// エンシェント各バンドの OCR 生テキスト（診断・シグネチャ用）。
    public let ocrTexts: [String]

    public init(screen: ScreenType, cards: [RecognizedCard] = [], shopItems: [ShopItem] = [],
                cardBoxes: [RectI] = [], ocrTexts: [String] = []) {
        self.screen = screen
        self.cards = cards
        self.shopItems = shopItems
        self.cardBoxes = cardBoxes
        self.ocrTexts = ocrTexts
    }

    public var hasDetections: Bool {
        !cards.isEmpty || shopItems.contains { $0.accepted }
    }

    /// 表示更新の抑制に使う内容シグネチャ（C# Form1.ApplyResult のシグネチャ相当）。
    public var signature: String {
        let cardSig = cards.map { "\($0.cardId):\(String(format: "%.2f", $0.confidence))" }
            .joined(separator: "|")
        let shopSig = shopItems.map {
            "\($0.kind.rawValue):\($0.candidates.map(\.id).joined(separator: ","))"
        }.joined(separator: "|")
        return "\(screen.rawValue)##\(cardSig)##\(shopSig)##\(ocrTexts.joined(separator: "|"))"
    }
}
