import Foundation

/// エンカウンター・アクト名のローカライズ（StS2Shared/Services/EncounterDatabaseService.cs の移植）。
public struct EncounterDatabase: Sendable {
    let engEncounters: [String: String]
    let jpnEncounters: [String: String]
    let engActs: [String: String]
    let jpnActs: [String: String]

    public init(store: ResourceStore) {
        engEncounters = CardDatabase.loadStringDict(store, "localization/eng/encounters.json")
        jpnEncounters = CardDatabase.loadStringDict(store, "localization/jpn/encounters.json")
        engActs = CardDatabase.loadStringDict(store, "localization/eng/acts.json")
        jpnActs = CardDatabase.loadStringDict(store, "localization/jpn/acts.json")
    }

    static func toTitleCase(_ s: String) -> String {
        s.split(separator: "_", omittingEmptySubsequences: false)
            .map { w in w.isEmpty ? "" : w.prefix(1).uppercased() + w.dropFirst().lowercased() }
            .joined(separator: " ")
    }

    public func encounterName(_ id: String, japanese: Bool = false) -> String {
        let raw = CardDatabase.rawId(id)
        let dict = japanese ? jpnEncounters : engEncounters
        return dict["\(raw).title"] ?? Self.toTitleCase(raw)
    }

    public func actName(_ id: String, japanese: Bool = false) -> String {
        let raw = CardDatabase.rawId(id)
        let dict = japanese ? jpnActs : engActs
        return dict["\(raw).title"] ?? Self.toTitleCase(raw)
    }
}

/// アクト→戦闘エンカウンターの階層別プール（StS2Shared/Services/EncounterActService.cs の移植）。
public struct ActEncounters: Sendable, Identifiable {
    public let id: String
    public let nameJp: String
    public let nameEn: String
    public let weak: [String]
    public let normal: [String]
    public let elite: [String]
    public let boss: [String]
    public let bossOrder: [String]

    public func name(japanese: Bool) -> String { japanese ? nameJp : nameEn }
}

public enum EncounterActService {
    public static func load(store: ResourceStore) -> [ActEncounters] {
        guard let data = store.versionedData("encounter_acts.json"),
              let arr = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]]
        else { return [] }
        return arr.map { el in
            let id = el["id"] as? String ?? ""
            return ActEncounters(
                id: id,
                nameJp: el["nameJp"] as? String ?? id,
                nameEn: el["nameEn"] as? String ?? id,
                weak: el["weak"] as? [String] ?? [],
                normal: el["normal"] as? [String] ?? [],
                elite: el["elite"] as? [String] ?? [],
                boss: el["boss"] as? [String] ?? [],
                bossOrder: el["bossOrder"] as? [String] ?? [])
        }
    }
}

/// モンスター名とエンカウンター→モンスター対応（StS2Shared/Services/MonsterDatabaseService.cs の移植）。
public struct MonsterDef: Sendable {
    public let dirName: String
    public let enLabel: String
    public let jaLabel: String

    public func label(japanese: Bool) -> String { japanese ? jaLabel : enLabel }
}

public struct MonsterDatabase: Sendable {
    let monsters: [String: MonsterDef]
    let encounterMap: [String: [String]]

    public init(store: ResourceStore) {
        var m: [String: MonsterDef] = [:]
        if let data = store.versionedData("monster_names.json"),
           let arr = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]] {
            for el in arr {
                guard let dir = el["dirName"] as? String else { continue }
                let en = el["en"] as? String ?? dir
                let ja = el["ja"] as? String ?? en
                m[dir] = MonsterDef(dirName: dir, enLabel: en, jaLabel: ja)
            }
        }
        monsters = m

        var map: [String: [String]] = [:]
        if let data = store.versionedData("encounter_monsters.json"),
           let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            for (key, value) in obj {
                map[key] = value as? [String] ?? []
            }
        }
        encounterMap = map
    }

    public func monster(_ dirName: String) -> MonsterDef? { monsters[dirName] }

    public func monsterOrCreate(_ dirName: String) -> MonsterDef {
        if let m = monsters[dirName] { return m }
        let en = dirName.split(separator: "_")
            .map { $0.prefix(1).uppercased() + $0.dropFirst() }
            .joined(separator: " ")
        return MonsterDef(dirName: dirName, enLabel: en, jaLabel: en)
    }

    /// エンカウンター ID（接頭辞なし）→ モンスター dir 一覧。対応が無ければ nil。
    public func encounterMonsterDirs(_ encounterId: String) -> [String]? {
        encounterMap[encounterId]
    }

    public func encounterMonsters(_ encounterId: String) -> [MonsterDef] {
        (encounterMap[encounterId] ?? []).map(monsterOrCreate)
    }
}
