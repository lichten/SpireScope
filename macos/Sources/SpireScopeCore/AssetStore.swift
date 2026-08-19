import Foundation

/// 抽出済みゲームアセット（画像）の解決。
/// C# の `AssetLocator`（配布モード）+ `CardImageService` / `RelicImageService` に相当する。
/// ルートは `~/Library/Application Support/SpireScope/assets/v{最大}`
/// （`dotnet run --project ctex-to-png -- setup` が生成。レイアウトは Windows 配布モードと同一）。
public struct AssetStore: Sendable {
    /// 配布アセット格納ベース。
    public static var distributionAssetsRoot: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Library", "Application Support", "SpireScope", "assets")
    }

    /// 最新バージョンディレクトリ（例 `assets/v0.111.0`）。未セットアップなら nil。
    public let versionDir: URL?
    /// カード ID → 相対パス（card_images.json、"CARD.X" → "silent/abrasive.png"）。
    let cardImagePaths: [String: String]
    /// レリック raw ID → 相対パス（relic_images.json、"AKABEKO" → "akabeko.png"）。
    let relicImagePaths: [String: String]

    public init(store: ResourceStore, assetsRoot: URL? = nil) {
        let root = assetsRoot ?? Self.distributionAssetsRoot
        let fm = FileManager.default
        let versions = ((try? fm.contentsOfDirectory(at: root, includingPropertiesForKeys: nil)) ?? [])
            .filter { $0.lastPathComponent.hasPrefix("v") && $0.hasDirectoryPath }
        versionDir = versions.max {
            ResourceStore.versionKey($0.lastPathComponent) < ResourceStore.versionKey($1.lastPathComponent)
        }
        cardImagePaths = CardDatabase.loadStringDict(store, "card_images.json")
        relicImagePaths = CardDatabase.loadStringDict(store, "relic_images.json")
    }

    /// アセットが解決可能か（初回セットアップ済みか）。
    public var isConfigured: Bool { versionDir != nil }

    /// セットアップ済みバージョン名（例 "v0.111.0"）。未セットアップなら nil。
    public var installedVersion: String? { versionDir?.lastPathComponent }

    /// カード画像の絶対パス。CARD. 接頭辞の有無どちらでも引ける。画像が無ければ nil。
    public func cardImageURL(_ cardId: String) -> URL? {
        guard let versionDir, !cardId.isEmpty else { return nil }
        let rel = cardImagePaths[cardId]
            ?? (cardId.contains(".") ? nil : cardImagePaths["CARD." + cardId])
        guard let rel else { return nil }
        return versionDir.appending(components: "images", "card_portraits_png")
            .appending(path: rel)
    }

    /// レリック画像の絶対パス。RELIC. 接頭辞の有無どちらでも引ける。
    public func relicImageURL(_ relicId: String) -> URL? {
        guard let versionDir else { return nil }
        guard let rel = relicImagePaths[CardDatabase.rawId(relicId)] else { return nil }
        return versionDir.appending(components: "images", "relics_png").appending(path: rel)
    }

    /// エンチャントアイコン（enchantments_png/{rawId 小文字}.png）。
    public func enchantmentIconURL(_ enchantmentId: String) -> URL? {
        guard let versionDir, !enchantmentId.isEmpty else { return nil }
        let name = CardDatabase.rawId(enchantmentId).lowercased() + ".png"
        let url = versionDir.appending(components: "images", "enchantments_png").appending(path: name)
        return FileManager.default.fileExists(atPath: url.path) ? url : nil
    }

    /// モンスター画像（images/monsters/{id}.png）。
    public func monsterImageURL(_ monsterId: String) -> URL? {
        guard let versionDir else { return nil }
        let url = versionDir.appending(components: "images", "monsters")
            .appending(path: monsterId + ".png")
        return FileManager.default.fileExists(atPath: url.path) ? url : nil
    }
}
