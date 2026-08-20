import CoreGraphics
import Foundation
import SpireScopeCore

/// 「エンシェントレリックを選択する」画面の検出器。C# AncientRelicRecognizer.cs の移植。
/// 各行のレリック名テキストを固定バンドで OCR（Vision）し、レリック名索引にファジー照合して候補を
/// 絞る。同一プレフィックスで末尾漢字だけが違うレリック群は OCR で一意に定まらないため、名前で
/// 絞った候補集合（family）の中をアイコンの色（HSV ヒストグラム）で見分けるハイブリッド方式。
///
/// Mac 固有の変更: Vision の .accurate は WinRT OCR より重いので、まず 3 バンドの中央位置のみ
/// OCR し、全バンド無テキストなら縦スキャンせず即棄却する（非エンシェント画面のコストを 3 回に
/// 抑える 2 段プローブ。判定セマンティクスは変えない）。
final class AncientRelicRecognizer {
    struct Result {
        let matched: Bool
        let items: [ShopItem]
        let ocrTexts: [String]
    }

    private struct BandCand {
        let id: String
        let nameDist: Int
        let iconChi: Double
    }

    private struct BandResult {
        var nameRect: RectI
        var iconRect: RectI
        var ocr: String
        var cands: [BandCand]
    }

    private var iconDb: [String: [Float]]?
    private var index: CardNameIndex?

    func warmup(assets: AssetStore, db: CardDatabase) {
        _ = ensureIconDb(assets: assets, db: db)
        _ = ensureIndex(db: db)
    }

    func detect(frame: Frame, client: RectI, assets: AssetStore, db: CardDatabase) -> Result {
        let iconDb = ensureIconDb(assets: assets, db: db)
        let index = ensureIndex(db: db)
        let bands = RecognitionConstants.ancientNameBands

        // 1 段目: 中央位置のみ OCR。全バンド無テキストなら非エンシェント画面として即棄却。
        var centers = bands.map { rankBand(frame: frame, client: client, band: $0, dy: 0,
                                           iconDb: iconDb, index: index) }
        if centers.allSatisfy({ $0.ocr.trimmingCharacters(in: .whitespaces).isEmpty }) {
            return Result(matched: false,
                          items: centers.map { Self.emptyItem($0) },
                          ocrTexts: centers.map(\.ocr))
        }

        // 2 段目: 各バンドを縦に ±scanRange スキャンし、名前一致が最良（最小編集距離）の位置を採る。
        for (i, band) in bands.enumerated() {
            centers[i] = scanBand(frame: frame, client: client, band: band,
                                  center: centers[i], iconDb: iconDb, index: index)
        }

        let chosen = Self.assignUnique(centers.map(\.cands))
        var items: [ShopItem] = []
        for (i, result) in centers.enumerated() {
            let accept = chosen[i].map(Self.isAccepted) ?? false
            let cands: [ShopCandidate] = accept
                ? [Self.toCandidate(chosen[i]!)] : []
            items.append(ShopItem(kind: .relic, region: result.nameRect,
                                  candidates: cands, accepted: accept))
        }
        let matched = items.count(where: \.accepted) >= RecognitionConstants.ancientMinMatches
        return Result(matched: matched, items: items, ocrTexts: centers.map(\.ocr))
    }

    /// 採否。名前 family に入っていれば採用。OCR は文字を返したが名前照合に失敗した時のみ、
    /// アイコン距離が十分近ければ採用する。
    private static func isAccepted(_ c: BandCand) -> Bool {
        c.nameDist <= RecognitionConstants.ancientMaxNameDist
            || c.iconChi <= RecognitionConstants.ancientIconMaxDistance
    }

    private static func toCandidate(_ c: BandCand) -> ShopCandidate {
        ShopCandidate(id: c.id, distance: c.nameDist == Int.max ? c.iconChi : Double(c.nameDist))
    }

    private static func emptyItem(_ r: BandResult) -> ShopItem {
        ShopItem(kind: .relic, region: r.nameRect, candidates: [], accepted: false)
    }

    /// バンドを縦スキャンし最小編集距離の位置を採る。中央（center 引数）は計算済みで、
    /// 完全一致（距離 0）が出れば以降のずれ位置は試さない。同点は中心寄りが残る。
    private func scanBand(frame: Frame, client: RectI, band: CardSlot, center: BandResult,
                          iconDb: [String: [Float]], index: CardNameIndex) -> BandResult {
        var best = center
        var bestScore = center.cands.map(\.nameDist).min() ?? Int.max
        if bestScore == 0 { return best }

        let half = max(0, RecognitionConstants.ancientBandScanSteps / 2)
        guard half > 0, RecognitionConstants.ancientBandScanRange > 0 else { return best }
        let unit = RecognitionConstants.ancientBandScanRange / Double(half)
        for k in 1...half {
            for dy in [Double(k) * unit, -Double(k) * unit] {
                let r = rankBand(frame: frame, client: client, band: band, dy: dy,
                                 iconDb: iconDb, index: index)
                let score = r.cands.map(\.nameDist).min() ?? Int.max
                if score < bestScore {
                    bestScore = score
                    best = r
                    if bestScore == 0 { return best }
                }
            }
        }
        return best
    }

    /// 1 バンドを名前 OCR で family に絞り、その中をアイコン色で順位付けした候補列を返す。
    /// 名前が全く読めなければアイコン全 DB をフォールバック候補にする。ただし OCR が 1 文字も
    /// 返さないバンド（＝レリック名が無い背景）は空にする（暗い背景の HSV が暗色レリックアイコンに
    /// 誤一致し、カード選択画面をエンシェント選択と誤判定するのを防ぐ重要ガード）。
    private func rankBand(frame: Frame, client: RectI, band: CardSlot, dy: Double,
                          iconDb: [String: [Float]], index: CardNameIndex) -> BandResult {
        let shifted = CardSlot(band.cxFrac, band.cyFrac + dy, band.wFrac, band.hFrac)
        let nameRect = shifted.toPixels(client: client).intersect(frame.rect)
        let iconRect = Self.iconRect(client: client, band: shifted, frame: frame)

        // 1) 名前 OCR → family（id → 最小編集距離）。
        var ocrText = ""
        var nameDist: [String: Int] = [:]
        if nameRect.width >= 8, nameRect.height >= 6,
           let bin = ImageOps.cropUpscaleBinarize(
               frame.image, region: nameRect,
               scale: max(1, RecognitionConstants.ancientTitleScale)),
           let binImage = ImageOps.grayImage(bin.pixels, width: bin.width, height: bin.height) {
            let lines = VisionOcr.recognizeLines(binImage)
            ocrText = lines.joined(separator: " ")
            for line in lines {
                for m in index.findRanked(line, maxDist: RecognitionConstants.ancientMaxNameDist)
                where nameDist[m.id].map({ m.distance < $0 }) ?? true {
                    nameDist[m.id] = m.distance
                }
            }
        }

        // 2) アイコン HSV（実背景上なので合成なし）。
        var qIcon: [Float]?
        if iconRect.width >= 6, iconRect.height >= 6 {
            qIcon = HsvHistogram.compute(frame.image, region: iconRect,
                                         sb: RecognitionConstants.ancientIconSatBins,
                                         vb: RecognitionConstants.ancientIconValBins)
        }

        // 3) 候補集合＝family。名前が読めなければ（OCR にテキストがある場合のみ）全 DB フォールバック。
        let ocrHasText = !ocrText.trimmingCharacters(in: .whitespaces).isEmpty
        let ids: [String]
        if !nameDist.isEmpty {
            ids = Array(nameDist.keys)
        } else if ocrHasText, qIcon != nil {
            ids = Array(iconDb.keys)
        } else {
            ids = []
        }

        var cands: [BandCand] = []
        for id in ids {
            let nd = nameDist[id] ?? Int.max
            let chi: Double
            if let qIcon, let h = iconDb[id] {
                chi = HsvHistogram.chiSquare(qIcon, h)
            } else {
                chi = .greatestFiniteMagnitude
            }
            cands.append(BandCand(id: id, nameDist: nd, iconChi: chi))
        }
        // 名前編集距離が主、アイコン距離が従。
        cands.sort { $0.nameDist != $1.nameDist ? $0.nameDist < $1.nameDist : $0.iconChi < $1.iconChi }
        return BandResult(nameRect: nameRect, iconRect: iconRect, ocr: ocrText, cands: cands)
    }

    /// 各バンドに相異なるレリックを (名前距離, アイコン距離) 昇順で貪欲割当する。
    /// 確信の高い行が先に id を確保し、同一プレフィックスの残り行は消去法で決まる。
    private static func assignUnique(_ perBand: [[BandCand]]) -> [BandCand?] {
        var assigned = [BandCand?](repeating: nil, count: perBand.count)
        var used = Set<String>()
        var all: [(band: Int, cand: BandCand)] = []
        for (bi, cands) in perBand.enumerated() {
            for c in cands { all.append((bi, c)) }
        }
        all.sort { a, b in
            a.cand.nameDist != b.cand.nameDist
                ? a.cand.nameDist < b.cand.nameDist
                : a.cand.iconChi < b.cand.iconChi
        }
        for (bi, c) in all {
            if assigned[bi] != nil || used.contains(c.id) { continue }
            assigned[bi] = c
            used.insert(c.id)
        }
        return assigned
    }

    private static func iconRect(client: RectI, band: CardSlot, frame: Frame) -> RectI {
        let side = max(1, Int(Double(client.height) * RecognitionConstants.ancientIconSizeFrac))
        let cx = client.x + Int(Double(client.width) * (band.cxFrac + RecognitionConstants.ancientIconDxFrac))
        let cy = client.y + Int(Double(client.height) * band.cyFrac)
        return RectI(x: cx - side / 2, y: cy - side / 2, width: side, height: side)
            .intersect(frame.rect)
    }

    private func ensureIconDb(assets: AssetStore, db: CardDatabase) -> [String: [Float]] {
        if let iconDb { return iconDb }
        guard assets.imagesDirURL("relics_png") != nil else { return [:] }
        let built = ShopItemRecognizer.buildDb(
            ids: db.allRelicIds,
            background: RecognitionConstants.ancientIconBackground,
            url: assets.relicImageURL)
        iconDb = built
        return built
    }

    private func ensureIndex(db: CardDatabase) -> CardNameIndex {
        if let index { return index }
        let built = CardNameIndex.buildRelics(db: db)
        index = built
        return built
    }
}
