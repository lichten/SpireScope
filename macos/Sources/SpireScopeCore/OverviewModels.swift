import Foundation

// SpireScope/DeckOverviewForm.cs 冒頭の DTO レコード群の移植（UI 非依存）。

public struct DeckCard: Hashable, Sendable {
    public let id: String
    public let nameEn: String
    public let nameJa: String
    public let cost: String
    public let type: String
    public let count: Int
    public let isUpgraded: Bool
    public let enchantmentId: String
    public let enchantmentAmount: Int

    public init(id: String, nameEn: String, nameJa: String, cost: String, type: String,
                count: Int, isUpgraded: Bool = false,
                enchantmentId: String = "", enchantmentAmount: Int = 0) {
        self.id = id
        self.nameEn = nameEn
        self.nameJa = nameJa
        self.cost = cost
        self.type = type
        self.count = count
        self.isUpgraded = isUpgraded
        self.enchantmentId = enchantmentId
        self.enchantmentAmount = enchantmentAmount
    }

    public func name(japanese: Bool) -> String { japanese ? nameJa : nameEn }
}

public struct RelicEntry: Hashable, Sendable {
    public let id: String
    public let nameEn: String
    public let nameJa: String

    public init(id: String, nameEn: String, nameJa: String) {
        self.id = id
        self.nameEn = nameEn
        self.nameJa = nameJa
    }

    public func name(japanese: Bool) -> String { japanese ? nameJa : nameEn }
}

public struct OverviewSection: Sendable {
    public let labelEn: String
    public let labelJa: String
    public let cards: [DeckCard]
    public let relics: [RelicEntry]

    public init(_ labelEn: String, _ labelJa: String, _ cards: [DeckCard], _ relics: [RelicEntry]) {
        self.labelEn = labelEn
        self.labelJa = labelJa
        self.cards = cards
        self.relics = relics
    }

    public func label(japanese: Bool) -> String { japanese ? labelJa : labelEn }
}

/// キーワードグループ（キャラクター概観）の 1 グループ。
public struct KeywordGroup: Sendable {
    public let label: String
    public let cards: [DeckCard]
    public let relics: [RelicEntry]
}
