import Foundation
import SpireScopeCore

/// 固定矩形レイアウト方式の画面認識器。C# ScreenRecognizer.cs の移植。
/// 画面ごとにイラスト矩形を固定座標で probe して中身を識別する。レリック／ポーションは
/// ShopItemRecognizer、エンシェントは AncientRelicRecognizer に委譲し、カードは本クラスが
/// キャラ別 portrait DB との HSV ヒストグラム照合で識別する。各カードスロットの外枠色から
/// キャラを推定し（FrameColorClassifier）、照合候補プールを絞り込む。
/// 画面種別は各レイアウトの固定スロットを試して確信一致数で判定する（probe-as-detector）。
final class ScreenRecognizer {
    private let shop = ShopItemRecognizer()
    private let ancient = AncientRelicRecognizer()

    // キャラ正規化キー（大文字、"" = ニュートラル）→ (CardId, 正規化 HSV ヒストグラム)。遅延構築。
    private var cardDb: [String: [(id: String, hist: [Float])]]?
    private var cardDbAll: [(id: String, hist: [Float])] = []

    /// potions_png 未抽出（旧セットアップ）の検出。UI がセットアップ再実行のヒントを出すために使う。
    var potionsMissing: Bool { shop.potionsMissing }

    func warmup(assets: AssetStore, db: CardDatabase) {
        _ = ensureCardDb(assets: assets, db: db)
        shop.warmup(assets: assets, db: db)
        ancient.warmup(assets: assets, db: db)
    }

    /// buffer は frame.image 全体のピクセルバッファ（枠色リング判定・クライアント推定と共用するため
    /// 呼び出し側で 1 回だけ作って渡す）。
    func recognize(frame: Frame, buffer frameBuffer: RGBAImage, client: RectI,
                   currentCharacterId: String?,
                   assets: AssetStore, db: CardDatabase) -> RecognitionResult {
        // 1) レリック／ポーションスロットを probe（ショップ判定の主信号）。
        let shopResult = shop.detect(frame: frame, client: client, assets: assets, db: db)
        if shopResult.isShop {
            let cards = probeCards(frame: frame, buffer: frameBuffer, client: client,
                                   slots: RecognitionConstants.shopCardSlots,
                                   currentCharacterId: currentCharacterId, assets: assets, db: db)
            return RecognitionResult(
                screen: .shop, cards: cards, shopItems: shopResult.items,
                cardBoxes: RecognitionConstants.shopCardSlots.map { $0.toPixels(client: client) })
        }

        // 2) エンシェントレリック選択画面（固定バンドのレリック名を OCR 照合）。
        let ancientResult = ancient.detect(frame: frame, client: client, assets: assets, db: db)
        if ancientResult.matched {
            return RecognitionResult(screen: .ancientSelect, shopItems: ancientResult.items,
                                     ocrTexts: ancientResult.ocrTexts)
        }

        // 3) いずれでもなければカード選択スロットを probe。相異なるカードが必要数あり、
        //    かつ確信カードも必要数ある場合のみ「カードを選択」とみなす。
        let cards = probeCards(frame: frame, buffer: frameBuffer, client: client,
                               slots: RecognitionConstants.cardSelectSlots,
                               currentCharacterId: currentCharacterId, assets: assets, db: db)
        let distinct = Set(cards.map(\.cardId)).count
        let confident = cards.count { $0.confidence >= RecognitionConstants.minSelectConfidence }
        if distinct >= RecognitionConstants.minCardsForSelect
            && confident >= RecognitionConstants.minCardsForSelect {
            return RecognitionResult(
                screen: .cardSelect, cards: cards,
                cardBoxes: RecognitionConstants.cardSelectSlots.map { $0.toPixels(client: client) })
        }
        return RecognitionResult(screen: .unknown)
    }

    /// 各カードスロットのアート窓を識別し、採用できたカードを返す（枠色でプール絞り込み）。
    private func probeCards(frame: Frame, buffer: RGBAImage, client: RectI, slots: [CardSlot],
                            currentCharacterId: String?, assets: AssetStore,
                            db: CardDatabase) -> [RecognizedCard] {
        let cardDb = ensureCardDb(assets: assets, db: db)
        guard !cardDb.isEmpty else { return [] }

        var result: [RecognizedCard] = []
        for slot in slots {
            let cardRect = slot.toPixels(client: client).intersect(frame.rect)
            if cardRect.width < 16 || cardRect.height < 16 { continue }

            let art = CardGeometry.artRegionOf(cardRect).intersect(frame.rect)
            if art.width < 8 || art.height < 8 { continue }

            // 枠色 → キャラ推定 → 照合プールを絞る（推定不能なら現在キャラ／全件にフォールバック）。
            let ringChar = FrameColorClassifier.classify(frame: buffer, cardRect: cardRect)
            let pool = selectPool(cardDb: cardDb, ringChar: ringChar,
                                  currentCharacterId: currentCharacterId)

            if let m = Self.identify(frame: frame, art: art, pool: pool) {
                result.append(RecognizedCard(cardId: m.cardId, region: art, confidence: m.confidence))
            }
        }
        return result
    }

    /// 照合候補プールを選ぶ。枠色推定キャラ・現在キャラ・ニュートラル（colorless 等）を合算する。
    /// どちらの手掛かりも無い時のみ全件にフォールバックする。
    private func selectPool(cardDb: [String: [(id: String, hist: [Float])]],
                            ringChar: String?, currentCharacterId: String?)
        -> [(id: String, hist: [Float])] {
        let hasSignal = ringChar != nil || !(currentCharacterId ?? "").isEmpty
        guard hasSignal else { return cardDbAll }

        var keys: Set<String> = [""] // ニュートラルは常に含める
        if let ringChar { keys.insert(ringChar) }
        if let cur = currentCharacterId, !cur.isEmpty { keys.insert(cur.uppercased()) }

        var pool: [(id: String, hist: [Float])] = []
        for k in keys {
            if let bucket = cardDb[k] { pool.append(contentsOf: bucket) }
        }
        return pool.isEmpty ? cardDbAll : pool
    }

    private struct Match {
        let cardId: String
        let distance: Double
        let confidence: Double
    }

    /// アート窓のヒストグラムをプールと照合し最近傍を返す（しきい値・マージンで採否）。
    private static func identify(frame: Frame, art: RectI,
                                 pool: [(id: String, hist: [Float])]) -> Match? {
        let q = HsvHistogram.compute(frame.image, region: art)
        var bestId: String?
        var best = Double.greatestFiniteMagnitude
        var second = Double.greatestFiniteMagnitude
        for (id, h) in pool {
            let d = HsvHistogram.chiSquare(q, h)
            if d < best {
                second = best
                best = d
                bestId = id
            } else if d < second {
                second = d
            }
        }
        guard let bestId, best <= RecognitionConstants.cardMaxDistance,
              (second - best) >= RecognitionConstants.cardMinMargin
        else { return nil }
        let conf = min(1, max(0, (1.0 - best) * 0.5 + min(1.0, (second - best) / 0.3) * 0.5))
        return Match(cardId: bestId, distance: best, confidence: conf)
    }

    /// キャラ別 portrait DB を遅延構築する（下 15% トリムはアート窓ジオメトリと対）。
    /// アセット未セットアップの間はキャッシュせず次フレームで再試行する。
    private func ensureCardDb(assets: AssetStore, db: CardDatabase)
        -> [String: [(id: String, hist: [Float])]] {
        if let cardDb { return cardDb }
        guard assets.imagesDirURL("card_portraits_png") != nil else { return [:] }

        var built: [String: [(id: String, hist: [Float])]] = [:]
        var all: [(id: String, hist: [Float])] = []
        for id in db.allCardIds {
            guard let url = assets.cardImageURL(id),
                  FileManager.default.fileExists(atPath: url.path),
                  let img = ImageLoader.load(url)
            else { continue }
            let ph = max(1, Int(Double(img.height) * (1.0 - RecognitionConstants.portraitBottomTrim)))
            let hist = HsvHistogram.compute(img, region: RectI(x: 0, y: 0, width: img.width, height: ph))
            let key = db.cardCharacter(id).uppercased() // "" = ニュートラル
            built[key, default: []].append((id, hist))
            all.append((id, hist))
        }
        cardDbAll = all
        cardDb = built // アセット解決後に初めて確定キャッシュ
        return built
    }
}
