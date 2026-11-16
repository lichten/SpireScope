import Foundation

/// spire-codex.com のティアスコア（Codex Score、0-100）の共通ロジック。
/// C# SpireScope/Services/TierScoreService.cs のデータ層の移植で、
/// キャッシュファイル・スナップショットは Windows 版と同一の JSON 形式を共用する
/// （fetched_at / cards / relics / potions。詳細は docs/SpireScope-TierScores.md）。
/// フェッチのオーケストレーション（TTL・フォールバック）はアプリ側 TierScoreModel が担う。
public enum TierScores {
    public static let apiBase = "https://spire-codex.com/api/runs/scores"
    /// サイト側の更新周期（30 分）の 2 倍。TTL 内は再取得しない。
    public static let cacheTtl: TimeInterval = 3600

    /// 正規化キャラ ID（接頭辞なし大文字）→ API の character パラメータ。
    public static let charApiNames: [String: String] = [
        "IRONCLAD": "ironclad",
        "SILENT": "silent",
        "DEFECT": "defect",
        "NECROBINDER": "necrobinder",
        "REGENT": "regent",
    ]

    public struct Section: Sendable {
        public var fetchedAt: Date
        public var cards: [String: Int]
        public var relics: [String: Int]
        public var potions: [String: Int]

        public init(fetchedAt: Date, cards: [String: Int] = [:],
                    relics: [String: Int] = [:], potions: [String: Int] = [:]) {
            self.fetchedAt = fetchedAt
            self.cards = cards
            self.relics = relics
            self.potions = potions
        }
    }

    public struct CharSection: Sendable {
        public var fetchedAt: Date
        public var cards: [String: Int]

        public init(fetchedAt: Date, cards: [String: Int]) {
            self.fetchedAt = fetchedAt
            self.cards = cards
        }
    }

    public struct CacheFile: Sendable {
        public var global: Section?
        public var characters: [String: CharSection]

        public init(global: Section?, characters: [String: CharSection]) {
            self.global = global
            self.characters = characters
        }
    }

    public static var cachePath: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Library", "Application Support", "SpireScope",
                       "tier_scores_cache.json")
    }

    // ---- 表示 ----

    /// spire-codex.com のティア境界（tier-list ページの定義に一致させる）。
    public static func tierLetter(_ s: Int) -> String {
        s >= 90 ? "S" : s >= 78 ? "A" : s >= 65 ? "B" : s >= 50 ? "C" : s >= 35 ? "D" : "F"
    }

    /// 例 "S (92)"。未収載は空文字。ID は "CARD.STRIKE" 形式・raw 形式どちらも可。
    public static func format(_ dict: [String: Int]?, _ id: String) -> String {
        guard let score = dict?[toRaw(id).uppercased()] else { return "" }
        return "\(tierLetter(score)) (\(score))"
    }

    static func toRaw(_ id: String) -> String {
        guard let dot = id.firstIndex(of: ".") else { return id }
        return String(id[id.index(after: dot)...])
    }

    // ---- パース ----

    /// API レスポンス {"ID": {"score": 92, ...}, ...} から score のみ抜き出す。壊れたエントリはスキップ。
    /// キーは大文字に正規化する（照合側も大文字化して引く）。
    public static func parseScores(_ data: Data) -> [String: Int] {
        guard let root = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any]
        else { return [:] }
        var dict: [String: Int] = [:]
        for (key, value) in root {
            if let obj = value as? [String: Any], let score = obj["score"] as? Double {
                dict[key.uppercased()] = Int(score.rounded())
            } else if let obj = value as? [String: Any], let score = obj["score"] as? Int {
                dict[key.uppercased()] = score
            }
        }
        return dict
    }

    // ---- 永続化（スナップショット・キャッシュは同じセクション形式を共用）----

    /// バンドル済みスナップショット（sync-resources.sh がコピーする tier_scores_snapshot.json）。
    public static func loadSnapshot(store: ResourceStore) -> Section? {
        guard let data = store.unversionedData("tier_scores_snapshot.json") else { return nil }
        return parseSection(try? JSONSerialization.jsonObject(with: data))
    }

    public static func loadCache() -> CacheFile? {
        guard let data = try? Data(contentsOf: cachePath),
              let root = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any]
        else { return nil }
        let global = parseSection(root["global"])
        var characters: [String: CharSection] = [:]
        if let chars = root["characters"] as? [String: Any] {
            for (id, value) in chars {
                guard let obj = value as? [String: Any],
                      let cards = intDict(obj["cards"]) else { continue }
                characters[id.uppercased()] = CharSection(
                    fetchedAt: parseDate(obj["fetched_at"]), cards: cards)
            }
        }
        return CacheFile(global: global, characters: characters)
    }

    public static func saveCache(_ file: CacheFile) {
        var root: [String: Any] = [:]
        if let g = file.global {
            root["global"] = [
                "fetched_at": formatDate(g.fetchedAt),
                "cards": g.cards, "relics": g.relics, "potions": g.potions,
            ] as [String: Any]
        }
        root["characters"] = file.characters.mapValues {
            ["fetched_at": formatDate($0.fetchedAt), "cards": $0.cards] as [String: Any]
        }
        guard let data = try? JSONSerialization.data(withJSONObject: root) else { return }
        let dir = cachePath.deletingLastPathComponent()
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        try? data.write(to: cachePath)
    }

    static func parseSection(_ value: Any?) -> Section? {
        guard let obj = value as? [String: Any] else { return nil }
        return Section(
            fetchedAt: parseDate(obj["fetched_at"]),
            cards: intDict(obj["cards"]) ?? [:],
            relics: intDict(obj["relics"]) ?? [:],
            potions: intDict(obj["potions"]) ?? [:])
    }

    private static func intDict(_ value: Any?) -> [String: Int]? {
        guard let obj = value as? [String: Any] else { return nil }
        var dict: [String: Int] = [:]
        for (k, v) in obj {
            if let i = v as? Int { dict[k.uppercased()] = i }
            else if let d = v as? Double { dict[k.uppercased()] = Int(d.rounded()) }
        }
        return dict
    }

    /// C# JsonSerializer の DateTime（小数 7 桁の ISO8601）も読めるよう、小数部を捨てて秒精度で
    /// パースする（用途は 1 時間 TTL 判定のみ）。読めなければ distantPast（=期限切れ扱い）。
    static func parseDate(_ value: Any?) -> Date {
        guard var s = value as? String else { return .distantPast }
        if let dot = s.firstIndex(of: ".") {
            let tail = s[dot...]
            let suffix = tail.hasSuffix("Z") ? "Z" : ""
            s = String(s[..<dot]) + suffix
        }
        if !s.hasSuffix("Z") && !s.contains("+") { s += "Z" }
        let fmt = ISO8601DateFormatter()
        return fmt.date(from: s) ?? .distantPast
    }

    static func formatDate(_ date: Date) -> String {
        ISO8601DateFormatter().string(from: date)
    }
}
