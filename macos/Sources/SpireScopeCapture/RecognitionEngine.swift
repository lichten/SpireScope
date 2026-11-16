import Foundation
import SpireScopeCore

/// 認識パイプラインの入口。照合 DB（カード portrait・レリック×2背景・ポーション・名前索引）を
/// actor 内に持ち、フレームの認識をシリアライズする。
/// アセット未セットアップの間は AssetStore を毎回作り直して再解決する
/// （CLAUDE.md「アセットディレクトリ参照は遅延解決にする」のルール。初回セットアップ完了を
/// 再起動なしで拾う）。
public actor RecognitionEngine {
    private let store: ResourceStore
    private let db: CardDatabase
    private let assetsRoot: URL?
    private var assets: AssetStore
    private let recognizer = ScreenRecognizer()

    public init(store: ResourceStore, db: CardDatabase, assetsRoot: URL? = nil) {
        self.store = store
        self.db = db
        self.assetsRoot = assetsRoot
        self.assets = AssetStore(store: store, assetsRoot: assetsRoot)
    }

    /// 照合 DB を事前構築する（初回認識の重い遅延ビルド ≈ 十数秒 を前倒しして体感を改善する）。
    /// アセット未解決時は各ビルダが空で即返るため無害（次回以降に遅延構築される）。
    public func warmup() {
        refreshAssetsIfNeeded()
        recognizer.warmup(assets: assets, db: db)
    }

    /// potions_png 未抽出（旧バージョンのセットアップ）か。UI が再セットアップのヒントを出す。
    public var potionsMissing: Bool { recognizer.potionsMissing }

    /// 1 フレームを認識する。client が nil なら黒帯トリムでクライアント矩形を推定する。
    public func recognize(frame: Frame, client: RectI? = nil,
                          currentCharacterId: String?) -> (result: RecognitionResult, client: RectI) {
        refreshAssetsIfNeeded()
        guard let buffer = RGBAImage(cgImage: frame.image) else {
            return (RecognitionResult(screen: .unknown), frame.rect)
        }
        let resolved = client ?? ClientAreaResolver.resolve(buffer)
        let result = recognizer.recognize(frame: frame, buffer: buffer, client: resolved,
                                          currentCharacterId: currentCharacterId,
                                          assets: assets, db: db)
        return (result, resolved)
    }

    private func refreshAssetsIfNeeded() {
        if !assets.isConfigured {
            assets = AssetStore(store: store, assetsRoot: assetsRoot)
        }
    }
}
