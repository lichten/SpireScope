import Foundation
import SpireScopeCore

/// ショップ画面の固定スロット（レリック行・ポーション行）を probe して、販売中のレリック／ポーションを
/// 識別する。C# ShopItemRecognizer.cs の移植。座標はクライアント領域基準の相対値。
/// 識別は背景合成済み DB との HSV ヒストグラム照合。強一致スロットが一定数あればショップ画面と
/// みなす（probe-as-detector）。所有レリック（HUD 左上）はスロット座標に含めないため自然に除外される。
final class ShopItemRecognizer {
    struct Result {
        let isShop: Bool
        let items: [ShopItem]
    }

    // アセット未セットアップの間はキャッシュせず次フレームで再試行（CLAUDE.md の遅延解決ルール）。
    private var relicDb: [String: [Float]]?
    private var potionDb: [String: [Float]]?

    func warmup(assets: AssetStore, db: CardDatabase) {
        _ = ensureRelicDb(assets: assets, db: db)
        _ = ensurePotionDb(assets: assets)
    }

    /// potions_png 未抽出（旧セットアップ）の検出用。レリック DB はあるがポーション DB が空。
    var potionsMissing: Bool {
        relicDb?.isEmpty == false && (potionDb?.isEmpty ?? true)
    }

    func detect(frame: Frame, client: RectI, assets: AssetStore, db: CardDatabase) -> Result {
        let relicDb = ensureRelicDb(assets: assets, db: db)
        let potionDb = ensurePotionDb(assets: assets)

        let side = max(8, Int(Double(client.height) * RecognitionConstants.shopSlotSizeFrac))
        var items: [ShopItem] = []
        for slot in RecognitionConstants.shopSlots {
            let cx = client.x + Int(Double(client.width) * slot.cxFrac)
            let cy = client.y + Int(Double(client.height) * slot.cyFrac)
            let rect = RectI(x: cx - side / 2, y: cy - side / 2, width: side, height: side)
                .intersect(frame.rect)
            if rect.width < 8 || rect.height < 8 { continue }

            // クエリは実背景上にあるので合成なし。DB は背景合成済み。
            let q = HsvHistogram.compute(frame.image, region: rect,
                                         sb: RecognitionConstants.shopSatBins,
                                         vb: RecognitionConstants.shopValBins)
            let dbForKind = slot.kind == .relic ? relicDb : potionDb
            let cands = Self.nearestCandidates(q, db: dbForKind)
            items.append(ShopItem(kind: slot.kind, region: rect, candidates: cands,
                                  accepted: !cands.isEmpty))
        }
        let isShop = items.count(where: \.accepted) >= RecognitionConstants.minMatchesForShop
        return Result(isShop: isShop, items: items)
    }

    /// best が shopMaxDistance 以内なら、best とタイ窓（shopMinMargin）以内の近接候補を
    /// 距離昇順で最大 shopMaxCandidates 件返す。圏外なら空（不採用）。
    static func nearestCandidates(_ q: [Float], db: [String: [Float]]) -> [ShopCandidate] {
        var scored = db.map { (id: $0.key, d: HsvHistogram.chiSquare(q, $0.value)) }
        scored.sort { $0.d < $1.d }

        guard let first = scored.first, first.d <= RecognitionConstants.shopMaxDistance
        else { return [] }
        var result: [ShopCandidate] = []
        for (id, d) in scored {
            if d - first.d >= RecognitionConstants.shopMinMargin
                || result.count >= RecognitionConstants.shopMaxCandidates { break }
            result.append(ShopCandidate(id: id, distance: d))
        }
        return result
    }

    private func ensureRelicDb(assets: AssetStore, db: CardDatabase) -> [String: [Float]] {
        if let relicDb { return relicDb }
        guard assets.imagesDirURL("relics_png") != nil else { return [:] }
        let built = Self.buildDb(ids: db.allRelicIds, background: RecognitionConstants.shopBackground,
                                 url: assets.relicImageURL)
        relicDb = built
        return built
    }

    private func ensurePotionDb(assets: AssetStore) -> [String: [Float]] {
        if let potionDb { return potionDb }
        guard assets.imagesDirURL("potions_png") != nil else { return [:] }
        let built = Self.buildDb(ids: assets.allPotionImageIds,
                                 background: RecognitionConstants.shopBackground,
                                 url: assets.potionImageURL)
        potionDb = built
        return built
    }

    /// 透過余白をトリムしてアートを枠いっぱいに → 画面背景色で合成してから集計（照合成立の鍵）。
    static func buildDb(ids: [String], background: HsvHistogram.RGB,
                        url: (String) -> URL?) -> [String: [Float]] {
        var built: [String: [Float]] = [:]
        for id in ids {
            guard let u = url(id), FileManager.default.fileExists(atPath: u.path),
                  let img = ImageLoader.load(u),
                  let full = RGBAImage(cgImage: img)
            else { continue }
            built[id] = HsvHistogram.compute(img, region: full.alphaBoundingBox(),
                                             compositeBg: background,
                                             sb: RecognitionConstants.shopSatBins,
                                             vb: RecognitionConstants.shopValBins)
        }
        return built
    }
}
