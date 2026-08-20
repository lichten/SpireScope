import CoreGraphics
import Foundation
import ImageIO
import SpireScopeCapture
import SpireScopeCore
import UniformTypeIdentifiers

// 保存 PNG に対する認識の較正・検証 CLI（C# capture-verify 相当。TCC 権限不要）。
// 使い方:
//   swift run capture-verify <frame.png> [--client x,y,w,h] [--char DEFECT] [--crops dir]
// --client 省略時は黒帯トリムでクライアント矩形を推定する。
// --crops はカード／ショップ／エンシェントの各スロット切り出しを保存する（座標較正用）。

func fail(_ message: String) -> Never {
    FileHandle.standardError.write(Data((message + "\n").utf8))
    exit(1)
}

var pngPath: String?
var clientArg: RectI?
var characterId: String?
var cropsDir: String?

var args = Array(CommandLine.arguments.dropFirst())
while !args.isEmpty {
    let a = args.removeFirst()
    switch a {
    case "--client":
        guard !args.isEmpty else { fail("--client には x,y,w,h を指定") }
        let parts = args.removeFirst().split(separator: ",").compactMap { Int($0) }
        guard parts.count == 4 else { fail("--client には x,y,w,h を指定") }
        clientArg = RectI(x: parts[0], y: parts[1], width: parts[2], height: parts[3])
    case "--char":
        guard !args.isEmpty else { fail("--char にはキャラ ID を指定") }
        characterId = args.removeFirst()
    case "--crops":
        guard !args.isEmpty else { fail("--crops には出力ディレクトリを指定") }
        cropsDir = args.removeFirst()
    default:
        if pngPath == nil { pngPath = a } else { fail("不明な引数: \(a)") }
    }
}

guard let pngPath else {
    fail("使い方: capture-verify <frame.png> [--client x,y,w,h] [--char DEFECT] [--crops dir]")
}
guard let image = ImageLoader.load(URL(filePath: pngPath)) else {
    fail("画像を読み込めません: \(pngPath)")
}
let frame = Frame(image: image)

let store: ResourceStore
let db: CardDatabase
do {
    store = try ResourceStore()
    db = try CardDatabase(store: store)
} catch {
    fail("リソース初期化に失敗: \(error)")
}

@MainActor
func saveCrop(_ rect: RectI, name: String) {
    guard let cropsDir else { return }
    let clipped = rect.intersect(frame.rect)
    guard !clipped.isEmpty, let cropped = image.cropping(to: clipped.cgRect) else { return }
    let dir = URL(filePath: cropsDir)
    try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
    let url = dir.appending(path: name + ".png")
    guard let dest = CGImageDestinationCreateWithURL(
        url as CFURL, UTType.png.identifier as CFString, 1, nil) else { return }
    CGImageDestinationAddImage(dest, cropped, nil)
    CGImageDestinationFinalize(dest)
}

let engine = RecognitionEngine(store: store, db: db)
print("DB 構築中…（初回は十数秒かかる場合があります）")
let start = Date()
await engine.warmup()
print(String(format: "warmup: %.1fs", Date().timeIntervalSince(start)))

let t0 = Date()
let (result, client) = await engine.recognize(
    frame: frame, client: clientArg, currentCharacterId: characterId)
let elapsed = Date().timeIntervalSince(t0)

print("frame \(frame.width)x\(frame.height)  client \(client.x),\(client.y) \(client.width)x\(client.height)"
      + (clientArg == nil ? "（黒帯トリム推定）" : "（指定値）"))
print(String(format: "認識時間: %.2fs", elapsed))
print("画面種別: \(result.screen.rawValue)")

if !result.cards.isEmpty {
    print("カード:")
    for c in result.cards {
        let en = db.name(c.cardId, japanese: false)
        let ja = db.name(c.cardId, japanese: true)
        print(String(format: "  %@ / %@ (%@)  conf=%.2f  art=%d,%d %dx%d",
                     en, ja, c.cardId, c.confidence,
                     c.region.x, c.region.y, c.region.width, c.region.height))
    }
}
if !result.shopItems.isEmpty {
    print(result.screen == .ancientSelect ? "エンシェントレリック候補:" : "レリック／ポーションスロット:")
    for item in result.shopItems {
        let cand = item.candidates.map { c in
            let name = item.kind == .relic
                ? db.relicTitle(c.id, japanese: true)
                : db.potionTitle(c.id, japanese: true)
            return String(format: "%@(%@) d=%.2f", name, c.id, c.distance)
        }.joined(separator: ", ")
        print("  [\(item.kind.rawValue)] \(item.accepted ? "OK " : "-- ")\(cand.isEmpty ? "(不一致)" : cand)")
    }
}
if result.ocrTexts.contains(where: { !$0.isEmpty }) {
    print("OCR テキスト:")
    for (i, t) in result.ocrTexts.enumerated() {
        print("  band\(i): \(t)")
    }
}

// スロット切り出し（座標較正用）。認識結果に依らず全スロット表の矩形を保存する。
if cropsDir != nil {
    for (i, slot) in RecognitionConstants.cardSelectSlots.enumerated() {
        let rect = slot.toPixels(client: client)
        saveCrop(rect, name: String(format: "select_card_%02d", i))
        saveCrop(CardGeometry.artRegionOf(rect), name: String(format: "select_art_%02d", i))
    }
    for (i, slot) in RecognitionConstants.shopCardSlots.enumerated() {
        let rect = slot.toPixels(client: client)
        saveCrop(rect, name: String(format: "shop_card_%02d", i))
        saveCrop(CardGeometry.artRegionOf(rect), name: String(format: "shop_art_%02d", i))
    }
    let side = max(8, Int(Double(client.height) * RecognitionConstants.shopSlotSizeFrac))
    for (i, slot) in RecognitionConstants.shopSlots.enumerated() {
        let cx = client.x + Int(Double(client.width) * slot.cxFrac)
        let cy = client.y + Int(Double(client.height) * slot.cyFrac)
        saveCrop(RectI(x: cx - side / 2, y: cy - side / 2, width: side, height: side),
                 name: String(format: "shop_slot_%02d_%@", i, slot.kind.rawValue))
    }
    for (i, band) in RecognitionConstants.ancientNameBands.enumerated() {
        saveCrop(band.toPixels(client: client), name: String(format: "ancient_name_%02d", i))
    }
    print("スロット切り出しを保存: \(cropsDir!)")
}
