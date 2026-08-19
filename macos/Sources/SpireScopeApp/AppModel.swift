import Foundation
import Observation
import SpireScopeCore

/// アプリ全体の状態。セーブの読み込み・監視・派生データ（概観セクション等）の再計算を担う。
/// Windows 版 Form1 のオーケストレーション部に相当する。
@Observable @MainActor
final class AppModel {
    // ---- 不変サービス ----
    @ObservationIgnored let store: ResourceStore
    @ObservationIgnored let db: CardDatabase
    @ObservationIgnored let builder: OverviewBuilder
    @ObservationIgnored let siteLinks: SiteLinkService
    @ObservationIgnored let encounterDb: EncounterDatabase
    @ObservationIgnored let monsterDb: MonsterDatabase
    @ObservationIgnored let encounterActs: [ActEncounters]
    /// 初回セットアップ完了を再起動なしで拾えるよう、reload のたびに再解決する
    /// （CLAUDE.md「アセットディレクトリ参照は遅延解決にする」の教訓）。
    private(set) var assets: AssetStore
    @ObservationIgnored private var watcher: SaveWatcher?

    // ---- 状態 ----
    private(set) var save: RunSaveData?
    private(set) var savePath: URL?
    private(set) var loadError: String?
    var japanese = true {
        didSet {
            rebuildDerived()
            persistSettings()
        }
    }
    /// セーブファイルの手動指定。nil なら自動検出。変更で再読込＋監視を張り直す。
    var savePathOverride: URL? {
        didSet {
            persistSettings()
            reload()
            startWatching()
        }
    }
    var urlTemplates: [UrlTemplate] {
        didSet { try? UrlTemplateStore.save(urlTemplates) }
    }

    // ---- 派生データ ----
    private(set) var deckCards: [DeckCard] = []
    private(set) var relics: [RelicEntry] = []
    private(set) var combinedSections: [OverviewSection] = []
    private(set) var potionOdds: PotionOdds?
    /// 現在ランのキャラクター raw ID（例 "DEFECT"）。
    private(set) var currentCharacterId: String?

    init() {
        do {
            let store = try ResourceStore()
            self.store = store
            let db = try CardDatabase(store: store)
            self.db = db
            builder = OverviewBuilder(db: db)
            assets = AssetStore(store: store)
            siteLinks = SiteLinkService(db: db)
            encounterDb = EncounterDatabase(store: store)
            monsterDb = MonsterDatabase(store: store)
            encounterActs = EncounterActService.load(store: store)
        } catch {
            fatalError("リソース初期化に失敗: \(error)")
        }
        urlTemplates = UrlTemplateStore.load()
        let settings = AppSettings.load()
        japanese = settings.japanese
        savePathOverride = settings.savePathOverride.map { URL(filePath: $0) }
        reload()
        startWatching()
    }

    private func persistSettings() {
        AppSettings(savePathOverride: savePathOverride?.path, japanese: japanese).save()
    }

    var isAssetConfigured: Bool { assets.isConfigured }

    func reload() {
        if !assets.isConfigured {
            assets = AssetStore(store: store)
        }
        guard let path = savePathOverride ?? SaveLocator.defaultSavePath() else {
            loadError = "セーブディレクトリが見つかりません（~/Library/Application Support/SlayTheSpire2/steam）"
            return
        }
        savePath = path
        guard FileManager.default.fileExists(atPath: path.path) else {
            save = nil
            loadError = "進行中のランがありません（current_run.save なし）"
            rebuildDerived()
            return
        }
        do {
            save = try SaveLocator.load(path)
            loadError = nil
        } catch {
            // 書き込み途中の読み込み失敗は前回の内容を保持（Windows 版と同方針）
            loadError = "セーブを読み込めませんでした: \(error.localizedDescription)"
        }
        rebuildDerived()
    }

    private func rebuildDerived() {
        guard let player = save?.players.first else {
            deckCards = []
            relics = []
            combinedSections = []
            potionOdds = nil
            currentCharacterId = nil
            return
        }
        currentCharacterId = SaveLocator.normalizeCharacterId(player.characterId)
        deckCards = builder.buildDeckCards(player, japanese: japanese)
        relics = builder.buildRelics(player)
        combinedSections = builder.combinedSections(
            cards: deckCards, relics: relics,
            characterId: player.characterId, japanese: japanese)
        potionOdds = PotionOddsService.compute(player)
    }

    private func startWatching() {
        watcher?.cancel()
        guard let dir = (savePathOverride ?? savePath ?? SaveLocator.defaultSavePath())?
            .deletingLastPathComponent() else { return }
        watcher = SaveWatcher(directory: dir) { [weak self] in
            Task { @MainActor in self?.reload() }
        }
    }

    // ---- リンク ----

    func links(kind: String, id: String) -> [SiteLinkService.Link] {
        siteLinks.buildLinks(templates: urlTemplates, kind: kind, id: id)
    }

    // ---- ツールチップ ----

    func cardTooltip(_ card: DeckCard) -> String {
        var lines: [String] = [card.name(japanese: japanese)]
        let cost = db.cardCost(card.id)
        if !cost.isEmpty {
            let upgraded = db.upgradedCost(card.id)
            let costText = card.isUpgraded && !upgraded.isEmpty ? upgraded : cost
            lines.append(japanese ? "コスト: \(costText)" : "Cost: \(costText)")
        }
        let desc = db.description(card.id)
        let resolved = DescriptionFormatter.resolve(
            japanese ? desc.ja : desc.en,
            stats: db.cardStats(card.id), japanese: japanese, upgraded: card.isUpgraded)
        if !resolved.isEmpty { lines.append(resolved) }
        if !card.enchantmentId.isEmpty {
            lines.append(db.formatEnchantmentLabel(
                card.enchantmentId, amount: card.enchantmentAmount, japanese: japanese))
            let enchDesc = db.enchantmentDescription(
                card.enchantmentId, amount: card.enchantmentAmount, japanese: japanese)
            if !enchDesc.isEmpty { lines.append(enchDesc) }
        }
        return lines.joined(separator: "\n")
    }

    func relicTooltip(_ relic: RelicEntry) -> String {
        var lines: [String] = [relic.name(japanese: japanese)]
        let desc = db.description(relic.id)
        let resolved = DescriptionFormatter.resolve(
            japanese ? desc.ja : desc.en,
            stats: db.relicStats(CardDatabase.rawId(relic.id)), japanese: japanese)
        if !resolved.isEmpty { lines.append(resolved) }
        if let flavor = db.flavor(relic.id) {
            lines.append(japanese ? flavor.ja : flavor.en)
        }
        return lines.joined(separator: "\n")
    }
}
