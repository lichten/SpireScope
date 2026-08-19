import Foundation

/// StS2Shared/Services/CardDatabaseService.cs の移植。
/// カード・レリック・ポーションの名称/コスト/タイプ解決と、説明文スキャンによるシナジー判定 Set 群。
/// C# 版は static クラスだが、Swift 版は ResourceStore から構築するインスタンス（immutable / Sendable）。
/// 判定 Set の構築ロジックは C# と同じ構造・同じ順序で並べ、diff 可能性を保つ。
/// 注: C# は OrdinalIgnoreCase 辞書だが、リソースのキーは一貫した大文字 ID（+ 小文字サフィックス）で
/// あり、セーブ由来の ID も大文字のため、Swift 版は exact 一致で引く。
public final class CardDatabase: Sendable {
    // ---- 基本辞書 ----
    let db: [String: (en: String, ja: String)]           // card_database.json（CARD./RELIC. 両方）
    let potionDb: [String: (en: String, ja: String)]     // potion_database.json
    let types: [String: String]                          // card_types.json
    let rarities: [String: String]                       // card_rarities.json
    let characters: [String: String]                     // card_characters.json
    let costs: [String: Int]                             // card_costs.json
    let upgradedCosts: [String: Int]                     // card_upgraded_costs.json

    // ---- localization ----
    let engRelics: [String: String]
    let jpnRelics: [String: String]
    let engEvents: [String: String]
    let jpnEvents: [String: String]
    let enchantEng: [String: String]
    let enchantJpn: [String: String]

    // カード説明文（card_descriptions.json）。"CARD.X" → (en, ja)。
    let cardDesc: [String: (en: String, ja: String)]
    // シナジー判定用の "{rawId}.description" → en 走査ソース。
    let engCardDesc: [String: String]

    // ---- シナジー判定 Set（C# の static フィールド群と同順） ----
    let blockGivers: Set<String>
    let blockRelicGivers: Set<String>
    let drawRelated: Set<String>
    let drawRelicRelated: Set<String>
    let necroOsty: Set<String>
    let necroSoul: Set<String>
    let necroDoom: Set<String>
    let necroSummon: Set<String>
    let ironcladStr: Set<String>
    let ironcladEx: Set<String>
    let exhaustAction: Set<String>
    let etherealRelated: Set<String>
    let cardExhaustKeyword: Set<String>
    let cardEtherealKeyword: Set<String>
    let upgradedKeywords: [String: Set<String>]
    let ironcladStrike: Set<String>
    let silentPoison: Set<String>
    let silentShiv: Set<String>
    let defectChannel: Set<String>
    let defectEvoke: Set<String>
    let defectFocus: Set<String>
    let weak: Set<String>
    let vulnerable: Set<String>
    let regentForge: Set<String>
    let regentBlade: Set<String>
    let regentCreate: Set<String>
    let regentStatusGen: Set<String>
    let regentTransform: Set<String>
    let regentStarGain: Set<String>
    let regentStarSpend: Set<String>
    let defectZeroEnergy: Set<String>
    let allEnemiesAttack: Set<String>
    let regentCreateExtra: Set<String> = ["METAMORPHOSIS", "SPECTRUM_SHIFT"]
    let exhaustGainingEnchantments: Set<String>

    // ---- stats / related / relic メタ ----
    let stats: [String: [String: Int]]                   // rawId → 変数
    let relatedCards: [String: [String]]
    let createdByCards: [String: [String]]
    let relicRarities: [String: String]
    let relicStats: [String: [String: Int]]

    public init(store: ResourceStore) throws {
        db = Self.loadEntries(store, "card_database.json")
        potionDb = Self.loadEntries(store, "potion_database.json")
        types = Self.loadStringDict(store, "card_types.json")
        rarities = Self.loadStringDict(store, "card_rarities.json")
        characters = Self.loadStringDict(store, "card_characters.json")
        costs = Self.loadIntDict(store, "card_costs.json")
        upgradedCosts = Self.loadIntDict(store, "card_upgraded_costs.json")

        engRelics = Self.loadStringDict(store, "localization/eng/relics.json")
        jpnRelics = Self.loadStringDict(store, "localization/jpn/relics.json")
        engEvents = Self.loadStringDict(store, "localization/eng/events.json")
        jpnEvents = Self.loadStringDict(store, "localization/jpn/events.json")
        enchantEng = Self.loadStringDict(store, "localization/eng/enchantments.json")
        enchantJpn = Self.loadStringDict(store, "localization/jpn/enchantments.json")

        cardDesc = Self.loadEntries(store, "card_descriptions.json")
        // "CARD.ABRASIVE" → "ABRASIVE.description"（旧 _loc.EngCards の説明文キー形に合わせる）
        var engDesc: [String: String] = [:]
        for (id, v) in cardDesc { engDesc[Self.rawId(id) + ".description"] = v.en }
        engCardDesc = engDesc

        // ---- 判定 Set の構築（C# の初期化順を維持） ----
        let cardsAndRelics = [engDesc, engRelics]

        blockGivers = Self.computeBlockGivers(engDesc)
        blockRelicGivers = Self.computeBlockRelicGivers(engRelics)
        drawRelated = Self.computeDrawRelated(engDesc)
        drawRelicRelated = Self.computeDrawRelicRelated(engRelics)
        necroOsty = Self.computeByTag(cardsAndRelics, "[gold]Osty[/gold]", "[gold]Osty's[/gold]")
        necroSoul = Self.computeByTag(cardsAndRelics, "[gold]Soul[/gold]")
        necroDoom = Self.computeByTag(cardsAndRelics, "[gold]Doom[/gold]")
        necroSummon = Self.computeByTag(cardsAndRelics, "[gold]Summon[/gold]")
        ironcladStr = Self.computeByTag(cardsAndRelics, "[gold]Strength[/gold]")
        ironcladEx = Self.computeByTag(cardsAndRelics,
            "[gold]Exhaust[/gold]", "[gold]Exhausted[/gold]", "[gold]Exhaust Pile[/gold]")
        exhaustAction = Self.computeExhaustAction(engDesc)
        etherealRelated = Self.computeByTag(cardsAndRelics, "[gold]Ethereal[/gold]")
        cardExhaustKeyword = Self.loadKeywordSet(store, keyword: "EXHAUST")
        cardEtherealKeyword = Self.loadKeywordSet(store, keyword: "ETHEREAL")
        upgradedKeywords = Self.loadUpgradedKeywords(store)
        ironcladStrike = Self.computeByNameContaining(db, "Strike")
        silentPoison = Self.computeByTag(cardsAndRelics, "[gold]Poison[/gold]")
        silentShiv = Self.computeByGoldTagContaining(cardsAndRelics, "Shiv")
        defectChannel = Self.computeByTag(cardsAndRelics,
            "[gold]Channel[/gold]", "[gold]Channeled[/gold]", "[gold]Channels[/gold]")
        defectEvoke = Self.computeByTag(cardsAndRelics, "[gold]Evoke[/gold]")
        defectFocus = Self.computeByTag(cardsAndRelics, "[gold]Focus[/gold]")
        weak = Self.computeByTag(cardsAndRelics, "[gold]Weak[/gold]")
        vulnerable = Self.computeByTag(cardsAndRelics, "[gold]Vulnerable[/gold]")
        regentForge = Self.computeByTag(cardsAndRelics, "[gold]Forge[/gold]", "[gold]Forges[/gold]")
        regentBlade = Self.computeByTag(cardsAndRelics, "[gold]Sovereign Blade[/gold]")
        regentCreate = Self.computeByPlainText(cardsAndRelics,
            "Whenever you create", "created this combat")
        regentStatusGen = Self.computeStatusGenerators(types: types, engCardDesc: engDesc)
        regentTransform = Self.computeByTag(cardsAndRelics, "[gold]Transform[/gold]")

        // Star の増減に「反応」するだけのカードは gain/spend 両グループから除外する。
        let starReactive = Self.computeByPlainText(cardsAndRelics, "spend or gain {singleStarIcon}")
        var starGain = Self.computeByPlainText(cardsAndRelics, "starIcons()", "gain {singleStarIcon}")
        starGain.insert("ROYAL_GAMBLE") // "Gain {Stars:diff()} {singleStarIcon}" — 既存パターン非対応のため手動追加
        starGain.subtract(starReactive)
        regentStarGain = starGain
        var starSpend = Self.computeByPlainText(cardsAndRelics,
            "spend {singleStarIcon}", "{singleStarIcon} are used", "{singleStarIcon} cost", "{StarThreshold")
        starSpend.formUnion(Self.loadStarCostIds(store))
        starSpend.subtract(starReactive)
        regentStarSpend = starSpend

        defectZeroEnergy = Self.buildDefectZeroEnergy(costs: costs, characters: characters)
        allEnemiesAttack = Self.computeByPlainText(cardsAndRelics,
            "damage to ALL enemies", "hit ALL enemies")
        exhaustGainingEnchantments = Self.computeExhaustGainingEnchantments(enchantEng)

        stats = Self.loadNestedIntDict(store, "card_stats.json", rawKeys: true)
        relatedCards = Self.loadRelatedCards(store)
        createdByCards = Self.buildCreatedBy(relatedCards)
        relicRarities = Self.loadStringDict(store, "relic_rarities.json")
        relicStats = Self.loadNestedIntDict(store, "relic_stats.json", rawKeys: false)
    }

    // ---- ローダ ----

    static func loadJSON(_ store: ResourceStore, _ path: String) -> [String: Any] {
        guard let data = store.versionedData(path),
              let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
        else { return [:] }
        return obj
    }

    static func loadEntries(_ store: ResourceStore, _ path: String) -> [String: (en: String, ja: String)] {
        var result: [String: (en: String, ja: String)] = [:]
        for (key, value) in loadJSON(store, path) {
            guard let obj = value as? [String: Any] else { continue }
            let en = obj["en"] as? String ?? ""
            let ja = obj["ja"] as? String ?? en
            result[key] = (en, ja)
        }
        return result
    }

    static func loadStringDict(_ store: ResourceStore, _ path: String) -> [String: String] {
        loadJSON(store, path).compactMapValues { $0 as? String }
    }

    static func loadIntDict(_ store: ResourceStore, _ path: String) -> [String: Int] {
        loadJSON(store, path).compactMapValues { $0 as? Int }
    }

    static func loadNestedIntDict(_ store: ResourceStore, _ path: String, rawKeys: Bool)
        -> [String: [String: Int]]
    {
        var result: [String: [String: Int]] = [:]
        for (key, value) in loadJSON(store, path) {
            guard let obj = value as? [String: Any] else { continue }
            result[rawKeys ? rawId(key) : key] = obj.compactMapValues { $0 as? Int }
        }
        return result
    }

    static func loadStarCostIds(_ store: ResourceStore) -> [String] {
        guard let data = store.versionedData("card_star_costs.json"),
              let arr = try? JSONSerialization.jsonObject(with: data) as? [String]
        else { return [] }
        return arr.map(rawId)
    }

    // card_keywords.json から指定キーワードを持つカードIDセットを構築
    static func loadKeywordSet(_ store: ResourceStore, keyword: String) -> Set<String> {
        var result: Set<String> = []
        for (key, value) in loadJSON(store, "card_keywords.json") {
            guard let arr = value as? [String] else { continue }
            if arr.contains(where: { $0.caseInsensitiveCompare(keyword) == .orderedSame }) {
                result.insert(rawId(key))
            }
        }
        return result
    }

    // card_upgraded_keywords.json から rawId → アップグレード後キーワード集合を構築
    static func loadUpgradedKeywords(_ store: ResourceStore) -> [String: Set<String>] {
        var result: [String: Set<String>] = [:]
        for (key, value) in loadJSON(store, "card_upgraded_keywords.json") {
            guard let arr = value as? [String] else { continue }
            result[rawId(key)] = Set(arr.filter { !$0.isEmpty })
        }
        return result
    }

    static func loadRelatedCards(_ store: ResourceStore) -> [String: [String]] {
        var result: [String: [String]] = [:]
        for (key, value) in loadJSON(store, "card_related.json") {
            guard let arr = value as? [String] else { continue }
            result[key] = arr.filter { !$0.isEmpty }
        }
        return result
    }

    static func buildCreatedBy(_ related: [String: [String]]) -> [String: [String]] {
        var map: [String: [String]] = [:]
        for (src, targets) in related {
            for t in targets { map[t, default: []].append(src) }
        }
        return map.mapValues { $0.sorted { $0.caseInsensitiveCompare($1) == .orderedAscending } }
    }

    // ---- 判定 Set の計算（C# の Compute* と同ロジック） ----

    static let descSuffix = ".description"

    /// description キーの集合から、条件を満たすキーの rawId 集合を作る共通ヘルパ。
    static func computeFromDescriptions(
        _ source: [String: String], _ predicate: (String) -> Bool
    ) -> Set<String> {
        var result: Set<String> = []
        for (key, desc) in source {
            guard key.hasSuffix(descSuffix) else { continue }
            if predicate(desc) { result.insert(String(key.dropLast(descSuffix.count))) }
        }
        return result
    }

    static func computeBlockGivers(_ engCardDesc: [String: String]) -> Set<String> {
        computeFromDescriptions(engCardDesc) { desc in
            let lower = desc.lowercased()
            // ブロック直接付与・倍増
            let isBlockGiver = desc.contains("[gold]Block[/gold]") &&
                (lower.contains("gain") || lower.contains("double"))
            // Frostオーブ生成（ディフェクトのブロック源）
            let isFrostChanneler = desc.contains("[gold]Channel[/gold]") &&
                desc.contains("[gold]Frost[/gold]")
            // プレート付与（ターン終了時にブロックを得るバフ）
            let isPlatingGiver = desc.contains("[gold]Plating[/gold]") && lower.contains("gain")
            return isBlockGiver || isFrostChanneler || isPlatingGiver
        }
    }

    static func computeBlockRelicGivers(_ engRelics: [String: String]) -> Set<String> {
        computeFromDescriptions(engRelics) { desc in
            let lower = desc.lowercased()
            // Rule 1: 直接ブロック付与 / Rule 2: 戦闘開始時付与 / Rule 3: ブロック倍増
            let isBlockGiver = desc.contains("[gold]Block[/gold]") &&
                (desc.contains("gain [blue]{") || lower.contains("each combat") || lower.contains("double"))
            // Rule 4: プレート付与（GORGET 等）
            let isPlatingGiver = desc.contains("[gold]Plating[/gold]") && lower.contains("gain")
            return isBlockGiver || isPlatingGiver
        }
    }

    static func computeDrawRelated(_ engCardDesc: [String: String]) -> Set<String> {
        computeFromDescriptions(engCardDesc) { desc in
            let stripped = desc.replacingOccurrences(of: "[gold]Draw Pile[/gold]", with: "")
            let isDrawCard = stripped.lowercased().contains("draw")
            // "Add ... Soul/Souls" パターン: Soulカードを生成してドローできるカード
            let lower = desc.lowercased()
            let isSoulGenerator = lower.contains("add") && lower.contains("soul")
            return isDrawCard || isSoulGenerator
        }
    }

    static func computeDrawRelicRelated(_ engRelics: [String: String]) -> Set<String> {
        computeFromDescriptions(engRelics) { desc in
            desc.replacingOccurrences(of: "[gold]Draw Pile[/gold]", with: "")
                .lowercased().contains("draw")
        }
    }

    static func computeExhaustAction(_ engCardDesc: [String: String]) -> Set<String> {
        computeFromDescriptions(engCardDesc) { desc in
            desc.replacingOccurrences(of: "[gold]Exhaust Pile[/gold]", with: "")
                .replacingOccurrences(of: "[gold]Exhausted[/gold]", with: "")
                .contains("[gold]Exhaust[/gold]")
        }
    }

    static func computeByTag(_ sources: [[String: String]], _ tags: String...) -> Set<String> {
        var result: Set<String> = []
        for source in sources {
            result.formUnion(computeFromDescriptions(source) { desc in
                tags.contains { desc.contains($0) }
            })
        }
        return result
    }

    // [gold]...[/gold] 内に word が含まれるカード・レリックを収集する。
    // テンプレート形式（例: [gold]{...:plural:Shiv|Shivs}[/gold]）にも対応。
    static func computeByGoldTagContaining(_ sources: [[String: String]], _ word: String) -> Set<String> {
        let pattern = try! NSRegularExpression(
            pattern: "\\[gold\\][^\\[]*" + NSRegularExpression.escapedPattern(for: word) + "[^\\[]*\\[/gold\\]")
        var result: Set<String> = []
        for source in sources {
            result.formUnion(computeFromDescriptions(source) { desc in
                pattern.firstMatch(in: desc, range: NSRange(desc.startIndex..., in: desc)) != nil
            })
        }
        return result
    }

    static func computeByPlainText(_ sources: [[String: String]], _ phrases: String...) -> Set<String> {
        let lowers = phrases.map { $0.lowercased() }
        var result: Set<String> = []
        for source in sources {
            result.formUnion(computeFromDescriptions(source) { desc in
                let lower = desc.lowercased()
                return lowers.contains { lower.contains($0) }
            })
        }
        return result
    }

    static func computeByNameContaining(
        _ db: [String: (en: String, ja: String)], _ substring: String
    ) -> Set<String> {
        // カード名（タイトル）は card_database.json 由来の CARD.* の En から判定する。
        let lower = substring.lowercased()
        var result: Set<String> = []
        for (id, entry) in db where id.hasPrefix("CARD.") {
            if entry.en.lowercased().contains(lower) { result.insert(rawId(id)) }
        }
        return result
    }

    static func buildDefectZeroEnergy(costs: [String: Int], characters: [String: String]) -> Set<String> {
        // コスト0ではないがシナジーカード
        var set: Set<String> = ["SCRAPE", "ALL_FOR_ONE", "FERAL", "ADAPTIVE_STRIKE", "MOMENTUM_STRIKE"]
        for (key, cost) in costs where cost == 0 && characters[key] == "Defect" {
            set.insert(rawId(key))
        }
        return set
    }

    static func computeStatusGenerators(
        types: [String: String], engCardDesc: [String: String]
    ) -> Set<String> {
        // card_types.json の Status 型カード名から [gold]{Name}[/gold] タグを構築
        let statusTags = types
            .filter { $0.value.caseInsensitiveCompare("Status") == .orderedSame }
            .map { toTitleCase(rawId($0.key).replacingOccurrences(of: "_", with: " ")) }
            .flatMap { ["[gold]\($0)[/gold]".lowercased(), "[gold]\($0)s[/gold]".lowercased()] }

        return computeFromDescriptions(engCardDesc) { desc in
            guard desc.contains("Add") else { return false }
            let lower = desc.lowercased()
            return statusTags.contains { lower.contains($0) }
        }
    }

    // "gains [gold]Exhaust[/gold]" を含むエンチャントID（= そのカードに廃棄を付与するエンチャント）
    static func computeExhaustGainingEnchantments(_ enchantEng: [String: String]) -> Set<String> {
        computeFromDescriptions(enchantEng) { desc in
            guard let markerRange = desc.range(of: "[gold]Exhaust[/gold]") else { return false }
            // "gains [gold]Exhaust[/gold]" のみ（"loses" は除外）
            let gainStr = "gains "
            let prefix = desc[..<markerRange.lowerBound]
            return prefix.count >= gainStr.count &&
                prefix.suffix(gainStr.count).lowercased() == gainStr
        }
    }

    // ---- 公開 API（C# の public メソッドに対応） ----

    public func cardType(_ id: String) -> String { types[id] ?? "" }
    public func cardRarity(_ id: String) -> String { rarities[id] ?? "" }
    public func cardCharacter(_ id: String) -> String { characters[id] ?? "" }

    public func cardCost(_ id: String) -> String {
        guard let cost = costs[id] else { return "" }
        return formatCost(id, cost)
    }

    /// アップグレードでコストが変わるカードのアップグレード後コスト値。変わらない場合は nil。
    public func upgradedCostValue(_ id: String) -> Int? { upgradedCosts[id] }

    /// アップグレード後コストの整形文字列。コストが変わらないカードは ""。
    public func upgradedCost(_ id: String) -> String {
        upgradedCosts[id].map { formatCost(id, $0) } ?? ""
    }

    func formatCost(_ id: String, _ cost: Int) -> String {
        if cost != -1 { return String(cost) }
        let type = cardType(id)
        return ["Attack", "Skill", "Power"].contains(type) ? "X" : "-"
    }

    public func name(_ id: String, japanese: Bool = false) -> String {
        if let entry = db[id] { return japanese ? entry.ja : entry.en }
        // フォールバック: CARD./RELIC. を除いてタイトルケースで返す
        return Self.toTitleCase(Self.rawId(id).replacingOccurrences(of: "_", with: " "))
    }

    /// レリック ID のローカライズタイトル。見つからない場合はタイトルケースにフォールバック。
    public func relicTitle(_ id: String, japanese: Bool = false) -> String {
        let loc = japanese ? jpnRelics : engRelics
        // ローカライズのキーは接頭辞なし（"AKABEKO.title"）。両対応で引く。
        let raw = Self.rawId(id)
        if let title = loc[id + ".title"], !title.trimmingCharacters(in: .whitespaces).isEmpty {
            return title
        }
        if let title = loc[raw + ".title"], !title.trimmingCharacters(in: .whitespaces).isEmpty {
            return title
        }
        return Self.toTitleCase(raw.replacingOccurrences(of: "_", with: " "))
    }

    /// ポーション ID の表示名。接頭辞 "POTION." は付いていても無くても可。
    public func potionTitle(_ id: String, japanese: Bool = false) -> String {
        let raw = Self.rawId(id)
        if let e = potionDb[id] ?? potionDb["POTION." + raw] {
            return japanese ? e.ja : e.en
        }
        return Self.toTitleCase(raw.replacingOccurrences(of: "_", with: " "))
    }

    // ---- 判定 API ----

    public func isWeak(_ id: String) -> Bool { weak.contains(Self.rawId(id)) }
    public func isVulnerable(_ id: String) -> Bool { vulnerable.contains(Self.rawId(id)) }
    public func isNecroOsty(_ id: String) -> Bool { necroOsty.contains(Self.rawId(id)) }
    public func isNecroSoul(_ id: String) -> Bool { necroSoul.contains(Self.rawId(id)) }
    public func isNecroDoom(_ id: String) -> Bool { necroDoom.contains(Self.rawId(id)) }
    public func isNecroSummon(_ id: String) -> Bool { necroSummon.contains(Self.rawId(id)) }
    public func isIroncladStrength(_ id: String) -> Bool { ironcladStr.contains(Self.rawId(id)) }
    public func isIroncladExhaust(_ id: String) -> Bool { ironcladEx.contains(Self.rawId(id)) }
    public func isExhaustAction(_ id: String) -> Bool { exhaustAction.contains(Self.rawId(id)) }
    public func isEtherealRelated(_ id: String) -> Bool { etherealRelated.contains(Self.rawId(id)) }
    public func isIroncladStrike(_ id: String) -> Bool { ironcladStrike.contains(Self.rawId(id)) }
    public func isSilentPoison(_ id: String) -> Bool { silentPoison.contains(Self.rawId(id)) }
    public func isSilentShiv(_ id: String) -> Bool { silentShiv.contains(Self.rawId(id)) }
    public func isDefectChannel(_ id: String) -> Bool { defectChannel.contains(Self.rawId(id)) }
    public func isDefectEvoke(_ id: String) -> Bool { defectEvoke.contains(Self.rawId(id)) }
    public func isDefectFocus(_ id: String) -> Bool { defectFocus.contains(Self.rawId(id)) }
    public func isDefectZeroEnergy(_ id: String) -> Bool { defectZeroEnergy.contains(Self.rawId(id)) }
    public func isRegentForge(_ id: String) -> Bool { regentForge.contains(Self.rawId(id)) }
    public func isRegentBlade(_ id: String) -> Bool { regentBlade.contains(Self.rawId(id)) }
    public func isRegentStarGain(_ id: String) -> Bool { regentStarGain.contains(Self.rawId(id)) }
    public func isRegentStarSpend(_ id: String) -> Bool { regentStarSpend.contains(Self.rawId(id)) }
    public func isRegentCreate(_ id: String) -> Bool {
        let raw = Self.rawId(id)
        return regentCreate.contains(raw) || regentStatusGen.contains(raw)
            || regentTransform.contains(raw) || regentCreateExtra.contains(raw)
    }

    public func isBlockGiver(_ id: String) -> Bool { blockGivers.contains(Self.rawId(id)) }
    public func isRelicBlockGiver(_ id: String) -> Bool { blockRelicGivers.contains(Self.rawId(id)) }
    public func isDrawRelated(_ id: String) -> Bool { drawRelated.contains(Self.rawId(id)) }
    public func isRelicDrawRelated(_ id: String) -> Bool { drawRelicRelated.contains(Self.rawId(id)) }
    public func isAllEnemiesAttack(_ id: String) -> Bool { allEnemiesAttack.contains(Self.rawId(id)) }

    public func isExhaustGainingEnchantment(_ enchantmentId: String) -> Bool {
        !enchantmentId.isEmpty && exhaustGainingEnchantments.contains(Self.rawId(enchantmentId))
    }

    // DLL の get_CanonicalKeywords から抽出した正確なキーワード判定
    public func isExhaustKeyword(_ id: String) -> Bool { cardExhaustKeyword.contains(Self.rawId(id)) }
    public func isEtherealKeyword(_ id: String) -> Bool { cardEtherealKeyword.contains(Self.rawId(id)) }

    // アップグレード状態を考慮したキーワード判定。
    public func isExhaustKeyword(_ id: String, upgraded: Bool) -> Bool {
        if upgraded, let s = upgradedKeywords[Self.rawId(id)] { return s.contains("EXHAUST") }
        return isExhaustKeyword(id)
    }

    public func isEtherealKeyword(_ id: String, upgraded: Bool) -> Bool {
        if upgraded, let s = upgradedKeywords[Self.rawId(id)] { return s.contains("ETHEREAL") }
        return isEtherealKeyword(id)
    }

    // ---- stats / related / relic ----

    public func cardStats(_ id: String) -> [String: Int]? { stats[Self.rawId(id)] }

    public func relatedCards(_ id: String) -> [String] { relatedCards[Self.normalizeCardId(id)] ?? [] }
    public func createdByCards(_ id: String) -> [String] { createdByCards[Self.normalizeCardId(id)] ?? [] }

    public var allCardIds: [String] { Array(types.keys) }

    public var allRelicIds: [String] {
        engRelics.keys
            .filter { $0.hasSuffix(".title") }
            .map { String($0.dropLast(".title".count)) }
            .sorted { $0.caseInsensitiveCompare($1) == .orderedAscending }
    }

    public func relicRarity(_ id: String) -> String { relicRarities[id] ?? "" }
    public func relicStats(_ id: String) -> [String: Int]? { relicStats[id] }

    // ---- enchantment ----

    public func enchantmentName(_ id: String, japanese: Bool) -> String {
        let raw = Self.rawId(id)
        let dict = japanese ? enchantJpn : enchantEng
        return dict["\(raw).title"]
            ?? Self.toTitleCase(raw.replacingOccurrences(of: "_", with: " "))
    }

    public func formatEnchantmentLabel(_ id: String, amount: Int, japanese: Bool) -> String {
        if id.isEmpty { return "" }
        let name = enchantmentName(id, japanese: japanese)
        return amount > 0 ? "\(name) +\(amount)" : name
    }

    public func enchantmentDescription(_ id: String, amount: Int, japanese: Bool) -> String {
        if id.isEmpty { return "" }
        let raw = Self.rawId(id)
        let dict = japanese ? enchantJpn : enchantEng
        guard let desc = dict["\(raw).description"], !desc.isEmpty else { return "" }
        return DescriptionFormatter.cleanWithAmount(desc, amount: amount, japanese: japanese)
    }

    // ---- description / flavor / event ----

    public func description(_ id: String) -> (en: String, ja: String) {
        let isRelic = id.hasPrefix("RELIC.")
        if !isRelic {
            // カード説明文はバージョン管理の card_descriptions.json から
            return cardDesc["CARD." + Self.rawId(id)] ?? ("", "")
        }
        let key = Self.rawId(id) + ".description"
        let en = engRelics[key] ?? ""
        let ja = jpnRelics[key] ?? en
        return (en, ja)
    }

    public func flavor(_ id: String) -> (en: String, ja: String)? {
        guard id.hasPrefix("RELIC.") else { return nil }
        let key = Self.rawId(id) + ".flavor"
        guard let en = engRelics[key], !en.isEmpty else { return nil }
        let enClean = DescriptionFormatter.clean(en)
        if enClean.lowercased().contains("revealed") { return nil }
        let ja = jpnRelics[key] ?? en
        return (enClean, DescriptionFormatter.clean(ja, japanese: true))
    }

    static let excludedEvents: Set<String> = ["DEPRECATED_EVENT", "ERROR", "MOCK_EVENT_MODEL", "PROCEED"]

    public var allEventIds: [String] {
        engEvents.keys
            .filter { $0.hasSuffix(".title") }
            .map { String($0.dropLast(".title".count)) }
            .filter { !$0.contains(".") && !Self.excludedEvents.contains($0) }
            .sorted { $0.caseInsensitiveCompare($1) == .orderedAscending }
    }

    public func eventTitle(_ id: String, japanese: Bool = false) -> String {
        let loc = japanese ? jpnEvents : engEvents
        let raw = Self.rawId(id)
        if let title = loc[id + ".title"], !title.trimmingCharacters(in: .whitespaces).isEmpty {
            return title
        }
        if let title = loc[raw + ".title"], !title.trimmingCharacters(in: .whitespaces).isEmpty {
            return title
        }
        return Self.toTitleCase(raw.replacingOccurrences(of: "_", with: " "))
    }

    // ---- helpers ----

    static func toTitleCase(_ s: String) -> String {
        s.split(separator: " ", omittingEmptySubsequences: false)
            .map { w in w.isEmpty ? "" : w.prefix(1).uppercased() + w.dropFirst().lowercased() }
            .joined(separator: " ")
    }

    public static func rawId(_ id: String) -> String {
        guard let dot = id.firstIndex(of: ".") else { return id }
        return String(id[id.index(after: dot)...])
    }

    static func normalizeCardId(_ id: String) -> String {
        id.contains(".") ? id : "CARD." + id
    }
}
