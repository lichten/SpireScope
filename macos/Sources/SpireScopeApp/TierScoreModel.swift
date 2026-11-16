import Foundation
import Observation
import SpireScopeCore

/// spire-codex.com のティアスコア保持（C# TierScoreService のオーケストレーション部）。
/// 「埋め込みスナップショット → キャッシュファイル → 公開 API」の 3 段フォールバック。
/// カードは現在ランのキャラ別スコアを優先し、未収載・ラン外は全体スコアへ落ちる。
/// フェッチは逐次（レート制限 300req/分に対して行儀よく）・失敗は無視（現状データで表示継続）。
@Observable @MainActor
final class TierScoreModel {
    private(set) var global: TierScores.Section?
    private var charId: String?
    private var charSection: TierScores.CharSection?
    @ObservationIgnored private var cachedChars: [String: TierScores.CharSection] = [:]
    @ObservationIgnored private var globalFetching = false
    @ObservationIgnored private var charFetching = false

    private static let userAgent: String = {
        let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "dev"
        return "SpireScope/\(version)"
    }()

    /// 起動時に 1 回呼ぶ。スナップショット→キャッシュを同期ロードし（この時点で表示可能）、
    /// TTL 切れなら裏で API から再取得する。
    func initialize(store: ResourceStore) {
        global = TierScores.loadSnapshot(store: store)
        if let cache = TierScores.loadCache() {
            if let g = cache.global, g.fetchedAt > (global?.fetchedAt ?? .distantPast) {
                global = g
            }
            cachedChars = cache.characters
        }
        if global == nil || Date().timeIntervalSince(global!.fetchedAt) > TierScores.cacheTtl {
            Task { await fetchGlobal() }
        }
    }

    /// 現在ランのキャラ別カードスコアを要求する（セーブ自動リロード毎に呼ばれる冪等メソッド）。
    /// nil・未知キャラならキャラ別を解除して全体スコアに戻す。
    func requestCharacterScores(_ normalizedCharId: String?) {
        guard let normalizedCharId = normalizedCharId?.uppercased(),
              TierScores.charApiNames[normalizedCharId] != nil else {
            charId = nil
            charSection = nil
            return
        }
        if charId == normalizedCharId, let cur = charSection,
           Date().timeIntervalSince(cur.fetchedAt) <= TierScores.cacheTtl {
            return
        }
        if let cached = cachedChars[normalizedCharId],
           Date().timeIntervalSince(cached.fetchedAt) <= TierScores.cacheTtl {
            charId = normalizedCharId
            charSection = cached
            return
        }
        Task { await fetchCharacter(normalizedCharId) }
    }

    // ---- 表示 ----

    func cardTier(_ cardId: String) -> String {
        let byChar = TierScores.format(charSection?.cards, cardId)
        return byChar.isEmpty ? TierScores.format(global?.cards, cardId) : byChar
    }

    func relicTier(_ relicId: String) -> String {
        TierScores.format(global?.relics, relicId)
    }

    func potionTier(_ potionId: String) -> String {
        TierScores.format(global?.potions, potionId)
    }

    // ---- 取得 ----

    private func fetchGlobal() async {
        guard !globalFetching else { return }
        globalFetching = true
        defer { globalFetching = false }
        // 逐次取得（並列にしない）。
        guard let cards = await fetch("\(TierScores.apiBase)/cards"),
              let relics = await fetch("\(TierScores.apiBase)/relics"),
              let potions = await fetch("\(TierScores.apiBase)/potions")
        else { return }
        global = TierScores.Section(fetchedAt: Date(), cards: cards, relics: relics, potions: potions)
        saveCache()
    }

    private func fetchCharacter(_ normalizedCharId: String) async {
        guard !charFetching, let apiName = TierScores.charApiNames[normalizedCharId] else { return }
        charFetching = true
        defer { charFetching = false }
        guard let cards = await fetch("\(TierScores.apiBase)/cards?character=\(apiName)")
        else { return }
        let section = TierScores.CharSection(fetchedAt: Date(), cards: cards)
        charId = normalizedCharId
        charSection = section
        cachedChars[normalizedCharId] = section
        saveCache()
    }

    private func fetch(_ urlString: String) async -> [String: Int]? {
        guard let url = URL(string: urlString) else { return nil }
        var request = URLRequest(url: url, timeoutInterval: 10)
        request.setValue(Self.userAgent, forHTTPHeaderField: "User-Agent")
        guard let (data, response) = try? await URLSession.shared.data(for: request),
              (response as? HTTPURLResponse)?.statusCode == 200
        else { return nil } // オフライン・429・5xx いずれも同じ扱い: 現状データで表示継続
        let scores = TierScores.parseScores(data)
        return scores.isEmpty ? nil : scores
    }

    private func saveCache() {
        TierScores.saveCache(TierScores.CacheFile(global: global, characters: cachedChars))
    }
}
