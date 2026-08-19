import Foundation

// StS2Shared/Models/SaveData.cs の 1:1 翻訳。current_run.save（平文 JSON）のモデル。

public struct RunSaveData: Codable, Sendable {
    public var ascension: Int
    public var currentActIndex: Int
    public var schemaVersion: Int
    public var players: [PlayerData]
    public var mapPointHistory: [[MapPointHistoryEntry]]
    public var acts: [ActData]

    enum CodingKeys: String, CodingKey {
        case ascension
        case currentActIndex = "current_act_index"
        case schemaVersion = "schema_version"
        case players
        case mapPointHistory = "map_point_history"
        case acts
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        ascension = try c.decodeIfPresent(Int.self, forKey: .ascension) ?? 0
        currentActIndex = try c.decodeIfPresent(Int.self, forKey: .currentActIndex) ?? 0
        schemaVersion = try c.decodeIfPresent(Int.self, forKey: .schemaVersion) ?? 0
        players = try c.decodeIfPresent([PlayerData].self, forKey: .players) ?? []
        mapPointHistory = try c.decodeIfPresent([[MapPointHistoryEntry]].self, forKey: .mapPointHistory) ?? []
        acts = try c.decodeIfPresent([ActData].self, forKey: .acts) ?? []
    }
}

public struct ActData: Codable, Sendable {
    public var id: String
    public var rooms: ActRooms?

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
        rooms = try c.decodeIfPresent(ActRooms.self, forKey: .rooms)
    }
}

public struct ActRooms: Codable, Sendable {
    public var bossId: String?
    public var secondBossId: String?
    public var eliteEncounterIds: [String]
    public var eliteEncountersVisited: Int
    // 通常戦闘（セーブは weak/normal 階層を normal_encounter_ids に統合）。
    public var normalEncounterIds: [String]
    public var normalEncountersVisited: Int
    public var bossEncountersVisited: Int

    enum CodingKeys: String, CodingKey {
        case bossId = "boss_id"
        case secondBossId = "second_boss_id"
        case eliteEncounterIds = "elite_encounter_ids"
        case eliteEncountersVisited = "elite_encounters_visited"
        case normalEncounterIds = "normal_encounter_ids"
        case normalEncountersVisited = "normal_encounters_visited"
        case bossEncountersVisited = "boss_encounters_visited"
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        bossId = try c.decodeIfPresent(String.self, forKey: .bossId)
        secondBossId = try c.decodeIfPresent(String.self, forKey: .secondBossId)
        eliteEncounterIds = try c.decodeIfPresent([String].self, forKey: .eliteEncounterIds) ?? []
        eliteEncountersVisited = try c.decodeIfPresent(Int.self, forKey: .eliteEncountersVisited) ?? 0
        normalEncounterIds = try c.decodeIfPresent([String].self, forKey: .normalEncounterIds) ?? []
        normalEncountersVisited = try c.decodeIfPresent(Int.self, forKey: .normalEncountersVisited) ?? 0
        bossEncountersVisited = try c.decodeIfPresent(Int.self, forKey: .bossEncountersVisited) ?? 0
    }
}

public struct MapPointHistoryEntry: Codable, Sendable {
    public var mapPointType: String
    public var playerStats: [PlayerFloorStats]?

    enum CodingKeys: String, CodingKey {
        case mapPointType = "map_point_type"
        case playerStats = "player_stats"
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        mapPointType = try c.decodeIfPresent(String.self, forKey: .mapPointType) ?? ""
        playerStats = try c.decodeIfPresent([PlayerFloorStats].self, forKey: .playerStats)
    }
}

public struct PlayerFloorStats: Codable, Sendable {
    public var currentHp: Int
    public var maxHp: Int
    public var damageTaken: Int
    public var hpHealed: Int
    public var maxHpGained: Int
    public var maxHpLost: Int
    public var currentGold: Int

    enum CodingKeys: String, CodingKey {
        case currentHp = "current_hp"
        case maxHp = "max_hp"
        case damageTaken = "damage_taken"
        case hpHealed = "hp_healed"
        case maxHpGained = "max_hp_gained"
        case maxHpLost = "max_hp_lost"
        case currentGold = "current_gold"
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        currentHp = try c.decodeIfPresent(Int.self, forKey: .currentHp) ?? 0
        maxHp = try c.decodeIfPresent(Int.self, forKey: .maxHp) ?? 0
        damageTaken = try c.decodeIfPresent(Int.self, forKey: .damageTaken) ?? 0
        hpHealed = try c.decodeIfPresent(Int.self, forKey: .hpHealed) ?? 0
        maxHpGained = try c.decodeIfPresent(Int.self, forKey: .maxHpGained) ?? 0
        maxHpLost = try c.decodeIfPresent(Int.self, forKey: .maxHpLost) ?? 0
        currentGold = try c.decodeIfPresent(Int.self, forKey: .currentGold) ?? 0
    }
}

public struct PlayerData: Codable, Sendable {
    public var characterId: String
    public var currentHp: Int
    public var maxHp: Int
    public var gold: Int
    public var maxEnergy: Int
    public var deck: [CardData]
    public var relics: [RelicData]
    public var odds: PlayerOddsData?

    enum CodingKeys: String, CodingKey {
        case characterId = "character_id"
        case currentHp = "current_hp"
        case maxHp = "max_hp"
        case gold
        case maxEnergy = "max_energy"
        case deck
        case relics
        case odds
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        characterId = try c.decodeIfPresent(String.self, forKey: .characterId) ?? ""
        currentHp = try c.decodeIfPresent(Int.self, forKey: .currentHp) ?? 0
        maxHp = try c.decodeIfPresent(Int.self, forKey: .maxHp) ?? 0
        gold = try c.decodeIfPresent(Int.self, forKey: .gold) ?? 0
        maxEnergy = try c.decodeIfPresent(Int.self, forKey: .maxEnergy) ?? 0
        deck = try c.decodeIfPresent([CardData].self, forKey: .deck) ?? []
        relics = try c.decodeIfPresent([RelicData].self, forKey: .relics) ?? []
        odds = try c.decodeIfPresent(PlayerOddsData.self, forKey: .odds)
    }
}

public struct PlayerOddsData: Codable, Sendable {
    // ポーション報酬ドロップの現在確率そのもの（ベースへの加算値ではない）。docs/potion-drop-odds.md 参照。
    public var potionRewardOddsValue: Double
    public var cardRarityOddsValue: Double

    enum CodingKeys: String, CodingKey {
        case potionRewardOddsValue = "potion_reward_odds_value"
        case cardRarityOddsValue = "card_rarity_odds_value"
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        potionRewardOddsValue = try c.decodeIfPresent(Double.self, forKey: .potionRewardOddsValue) ?? 0
        cardRarityOddsValue = try c.decodeIfPresent(Double.self, forKey: .cardRarityOddsValue) ?? 0
    }
}

public struct EnchantmentData: Codable, Sendable {
    public var id: String
    public var amount: Int

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
        amount = try c.decodeIfPresent(Int.self, forKey: .amount) ?? 0
    }
}

public struct CardData: Codable, Sendable {
    public var id: String
    public var floorAddedToDeck: Int
    public var currentUpgradeLevel: Int?
    public var enchantment: EnchantmentData?
    public var props: CardProps?

    enum CodingKeys: String, CodingKey {
        case id
        case floorAddedToDeck = "floor_added_to_deck"
        case currentUpgradeLevel = "current_upgrade_level"
        case enchantment
        case props
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
        floorAddedToDeck = try c.decodeIfPresent(Int.self, forKey: .floorAddedToDeck) ?? 0
        currentUpgradeLevel = try c.decodeIfPresent(Int.self, forKey: .currentUpgradeLevel)
        enchantment = try c.decodeIfPresent(EnchantmentData.self, forKey: .enchantment)
        props = try c.decodeIfPresent(CardProps.self, forKey: .props)
    }

    public func propInt(_ name: String) -> Int? {
        props?.ints.first { $0.name == name }?.value
    }
}

public struct CardProps: Codable, Sendable {
    public var ints: [NamedInt]

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        ints = try c.decodeIfPresent([NamedInt].self, forKey: .ints) ?? []
    }
}

public struct NamedInt: Codable, Sendable {
    public var name: String
    public var value: Int

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        name = try c.decodeIfPresent(String.self, forKey: .name) ?? ""
        value = try c.decodeIfPresent(Int.self, forKey: .value) ?? 0
    }
}

public struct RelicData: Codable, Sendable {
    public var id: String
    public var floorAddedToDeck: Int

    enum CodingKeys: String, CodingKey {
        case id
        case floorAddedToDeck = "floor_added_to_deck"
    }

    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
        floorAddedToDeck = try c.decodeIfPresent(Int.self, forKey: .floorAddedToDeck) ?? 0
    }
}
