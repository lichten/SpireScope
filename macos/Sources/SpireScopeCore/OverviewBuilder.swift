import Foundation

/// デッキ概観・キャラクター概観の分類ロジック（純関数）。
/// SpireScope/Form1.cs の DisplayDeck/DisplayRelics/RefreshCombinedOverview と、
/// SpireScope/DeckOverviewForm.cs の BuildKeywordGroups/IsDisposable/FormatCardCount/
/// BuildCharacterStats を移植したもの。
public struct OverviewBuilder: Sendable {
    public let db: CardDatabase
    public let mechanics: CharacterMechanics

    public init(db: CardDatabase) {
        self.db = db
        self.mechanics = CharacterMechanics(db: db)
    }

    // ---- Form1.DisplayDeck ----

    /// デッキを (Id, アップグレード, TinkerType, エンチャント) でまとめて DeckCard 化する。
    public func buildDeckCards(_ player: PlayerData, japanese: Bool) -> [DeckCard] {
        struct Key: Hashable {
            let id: String
            let isUpgraded: Bool
            let tinkerType: Int?
            let enchantmentId: String
            let enchantmentAmount: Int
        }
        var groups: [Key: Int] = [:]
        for c in player.deck {
            let key = Key(
                id: c.id,
                isUpgraded: (c.currentUpgradeLevel ?? 0) >= 1,
                tinkerType: c.propInt("TinkerTimeType"),
                enchantmentId: c.enchantment?.id ?? "",
                enchantmentAmount: c.enchantment?.amount ?? 0)
            groups[key, default: 0] += 1
        }
        return groups
            .map { key, count in
                let suffix = key.isUpgraded ? "+" : ""
                let runtimeType: String
                switch key.tinkerType {
                case 1: runtimeType = "Attack"
                case 2: runtimeType = "Skill"
                case 3: runtimeType = "Power"
                default: runtimeType = db.cardType(key.id)
                }
                return DeckCard(
                    id: key.id,
                    nameEn: db.name(key.id, japanese: false) + suffix,
                    nameJa: db.name(key.id, japanese: true) + suffix,
                    cost: db.cardCost(key.id),
                    type: runtimeType,
                    count: count,
                    isUpgraded: key.isUpgraded,
                    enchantmentId: key.enchantmentId,
                    enchantmentAmount: key.enchantmentAmount)
            }
            .sorted {
                let n0 = db.name($0.id, japanese: japanese)
                let n1 = db.name($1.id, japanese: japanese)
                if n0 != n1 { return n0 < n1 }
                if $0.isUpgraded != $1.isUpgraded { return !$0.isUpgraded }
                return $0.enchantmentId < $1.enchantmentId
            }
    }

    // ---- Form1.DisplayRelics ----

    public func buildRelics(_ player: PlayerData) -> [RelicEntry] {
        player.relics.map {
            RelicEntry(id: $0.id,
                       nameEn: db.name($0.id, japanese: false),
                       nameJa: db.name($0.id, japanese: true))
        }
    }

    // ---- Form1.RefreshCombinedOverview（デッキ概観のセクション構築） ----

    public func combinedSections(
        cards: [DeckCard], relics: [RelicEntry], characterId: String, japanese: Bool
    ) -> [OverviewSection] {
        let ja = japanese
        let isNecro = characterId.uppercased().contains("NECRO")

        let drawRelics = relics.filter { db.isRelicDrawRelated($0.id) }
        let blockRelics = relics.filter { db.isRelicBlockGiver($0.id) }
        let otherRelics = relics.filter { !db.isRelicDrawRelated($0.id) && !db.isRelicBlockGiver($0.id) }

        func byName(_ c: [DeckCard]) -> [DeckCard] { c.sorted { $0.name(japanese: ja) < $1.name(japanese: ja) } }

        var sections: [OverviewSection] = []

        // 1. タイプ別
        let typeGroups = Dictionary(grouping: cards, by: \.type)
            .sorted { Self.combinedTypeOrder($0.key) < Self.combinedTypeOrder($1.key) }
        for (type, group) in typeGroups {
            sections.append(OverviewSection(
                Self.typeLabelEn(type), Self.typeLabelJa(type), byName(group), []))
        }

        // 2. ドロー関連
        sections.append(OverviewSection("Draw-related", "ドロー関連",
            byName(cards.filter { db.isDrawRelated($0.id) }), drawRelics))

        // 3. ブロック関連
        sections.append(OverviewSection("Block-related", "ブロック関連",
            byName(cards.filter { db.isBlockGiver($0.id) }), blockRelics))

        // 4. 全体攻撃
        sections.append(OverviewSection("Attacks All Enemies", "全体攻撃",
            byName(cards.filter { db.isAllEnemiesAttack($0.id) }), []))

        // 5. 召喚 (Necrobinder only)
        if isNecro {
            sections.append(OverviewSection("Summon", "召喚",
                byName(cards.filter { db.isNecroSummon($0.id) }), []))
        }

        // 6. 共通キーワード（レリックも振り分ける）
        var claimedRelics: Set<String> = []
        for m in mechanics.mechanicsFor("Common") {
            let kwRelics = relics.filter { m.filter($0.id) }
            for r in kwRelics { claimedRelics.insert(r.id) }
            sections.append(OverviewSection(m.enLabel, m.jaLabel,
                byName(cards.filter { m.filter($0.id) }), kwRelics))
        }

        // 7. その他のレリック（共通キーワードに振り分け済みのものは除外）
        let finalOtherRelics = otherRelics.filter { !claimedRelics.contains($0.id) }
        sections.append(OverviewSection("Other Relics", "その他のレリック", [], finalOtherRelics))

        return sections
    }

    static func combinedTypeOrder(_ type: String) -> Int {
        switch type {
        case "Attack": return 0
        case "Skill": return 1
        case "Power": return 2
        case "Curse": return 3
        case "Status": return 4
        case "Quest": return 5
        default: return 6
        }
    }

    public static func typeLabelEn(_ type: String) -> String { type.isEmpty ? "Other" : type }

    public static func typeLabelJa(_ type: String) -> String {
        switch type {
        case "Attack": return "アタック"
        case "Skill": return "スキル"
        case "Power": return "パワー"
        case "Curse": return "呪い"
        case "Status": return "状態異常"
        case "Quest": return "クエスト"
        default: return type.isEmpty ? "その他" : type
        }
    }

    // ---- DeckOverviewForm.IsDisposable / BuildKeywordGroups（キャラクター概観） ----

    // 戦闘中に消滅する（＝戦闘終了まで残らない）カードか。Power・廃棄・幽体・消滅付与エンチャント。
    public func isDisposable(_ c: DeckCard) -> Bool {
        c.type == "Power"
            || db.isExhaustKeyword(c.id, upgraded: c.isUpgraded)
            || db.isEtherealKeyword(c.id, upgraded: c.isUpgraded)
            || db.isExhaustGainingEnchantment(c.enchantmentId)
    }

    /// キャラクター概観のキーワードグループを構築する。
    /// メカニクス群 → ブロック関連（クロス集計）→ 消滅（クロス集計）→ その他、の順。
    public func keywordGroups(
        characterLabel: String, cards: [DeckCard], relics: [RelicEntry], japanese: Bool,
        showBlockGroup: Bool = true, showDisposableGroup: Bool = true
    ) -> [KeywordGroup] {
        let ja = japanese
        func byName(_ c: [DeckCard]) -> [DeckCard] { c.sorted { $0.name(japanese: ja) < $1.name(japanese: ja) } }

        var assignedCards: Set<DeckCard> = []
        var assignedRelics: Set<RelicEntry> = []
        var result: [KeywordGroup] = []

        for m in mechanics.mechanicsFor(characterLabel) {
            let cardGroup = byName(cards.filter { m.filter($0.id) })
            let relicGroup = relics.filter { m.filter($0.id) }  // レリックは取得順を保持
            assignedCards.formUnion(cardGroup)
            assignedRelics.formUnion(relicGroup)
            result.append(KeywordGroup(label: ja ? m.jaLabel : m.enLabel,
                                       cards: cardGroup, relics: relicGroup))
        }

        // 「ブロック関連」クロス集計グループ。該当カードは assignedCards に登録して「その他」と重複させない
        // （メカニクス群・消滅群との重複表示は許容＝クロス集計）。
        if showBlockGroup {
            let block = byName(cards.filter { db.isBlockGiver($0.id) })
            assignedCards.formUnion(block)
            result.append(KeywordGroup(label: ja ? "ブロック関連" : "Block-related",
                                       cards: block, relics: []))
        }

        // 「戦闘中に消滅する」グループ。メカニクス群の後・「その他」の直前。
        if showDisposableGroup {
            let disposable = byName(cards.filter(isDisposable))
            assignedCards.formUnion(disposable)
            result.append(KeywordGroup(label: ja ? "プレイすると消滅する" : "Disappears in Battle",
                                       cards: disposable, relics: []))
        }

        let otherCards = byName(cards.filter { !assignedCards.contains($0) })
        let otherRelics = relics.filter { !assignedRelics.contains($0) }  // レリックは取得順を保持
        if !otherCards.isEmpty || !otherRelics.isEmpty {
            result.append(KeywordGroup(label: ja ? "その他" : "Other",
                                       cards: otherCards, relics: otherRelics))
        }
        return result
    }

    // ---- DeckOverviewForm.BuildCharacterStats / FormatCardCount ----

    public func characterStats(characterLabel: String, deck: [DeckCard], japanese: Bool) -> String {
        mechanics.mechanicsFor(characterLabel)
            .map { m in
                let count = deck.filter { m.filter($0.id) }.reduce(0) { $0 + $1.count }
                return japanese ? "\(m.jaLabel): \(count)枚" : "\(m.enLabel): \(count)"
            }
            .joined(separator: "  ")
    }

    public static func formatCardCount(_ groupCount: Int, deckTotal: Int, japanese: Bool) -> String {
        let pct = deckTotal > 0 ? 100.0 * Double(groupCount) / Double(deckTotal) : 0
        let pctStr = String(format: "%.0f", pct)
        return japanese
            ? "\(deckTotal)枚中\(groupCount)枚（\(pctStr)%）"
            : "\(groupCount) / \(deckTotal) (\(pctStr)%)"
    }
}

// ---- StS2Shared/Services/PotionOddsService.cs の移植 ----

/// 次戦闘のポーション報酬ドロップ確率（通常 / エリート）。forcedByRelic なら確定100%。
public struct PotionOdds: Sendable, Equatable {
    public let forcedByRelic: Bool
    public let normal: Double
    public let elite: Double
}

public enum PotionOddsService {
    /// エリート戦の加算補正（DLL の eliteBonus 0.25 の半分）。
    public static let eliteBonus = 0.125

    /// White Beast Statue（戦闘部屋でポーション報酬を確定させるレリック）の ID 末尾。
    static let whiteBeastStatueIdSuffix = "WHITE_BEAST_STATUE"

    /// セーブの odds.potion_reward_odds_value（＝現在の実確率そのもの）から計算する。
    /// odds 未収録（古い/欠損セーブ）なら nil。根拠は docs/potion-drop-odds.md。
    public static func compute(_ player: PlayerData) -> PotionOdds? {
        guard let odds = player.odds else { return nil }
        // 接頭辞非依存の末尾一致（"RELIC.WHITE_BEAST_STATUE" や beta 変種も拾う）。
        let forced = player.relics.contains {
            $0.id.uppercased().hasSuffix(whiteBeastStatueIdSuffix)
        }
        let v = odds.potionRewardOddsValue
        return PotionOdds(forcedByRelic: forced,
                          normal: min(max(v, 0), 1),
                          elite: min(max(v + eliteBonus, 0), 1))
    }
}
