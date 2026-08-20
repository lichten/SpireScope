import Foundation
import Testing
@testable import SpireScopeCapture
import SpireScopeCore

/// フィクスチャ PNG（Fixtures/README.md 参照）に対するエンドツーエンドのゴールデンテスト。
/// フィクスチャまたは抽出済みアセットが無い環境ではスキップ（early return）する。
@Suite struct RecognitionGoldenTests {
    struct Expected: Decodable {
        let screen: String
        let cards: [String]?
        let relics: [String]?
        let potions: [String]?
    }

    static func fixture(_ name: String) -> URL? {
        Bundle.module.url(forResource: "Fixtures", withExtension: nil)?
            .appending(path: name)
    }

    static func loadCase(_ base: String) -> (frame: Frame, expected: Expected)? {
        guard let pngURL = fixture("\(base).png"),
              let jsonURL = fixture("\(base).expected.json"),
              FileManager.default.fileExists(atPath: pngURL.path),
              FileManager.default.fileExists(atPath: jsonURL.path),
              let image = ImageLoader.load(pngURL),
              let data = try? Data(contentsOf: jsonURL),
              let expected = try? JSONDecoder().decode(Expected.self, from: data)
        else { return nil }
        return (Frame(image: image), expected)
    }

    static var assetsAvailable: Bool {
        FileManager.default.fileExists(atPath: AssetStore.distributionAssetsRoot.path)
    }

    func run(_ base: String) async throws {
        guard Self.assetsAvailable, let (frame, expected) = Self.loadCase(base) else { return }
        let store = try ResourceStore()
        let db = try CardDatabase(store: store)
        let engine = RecognitionEngine(store: store, db: db)
        let (result, _) = await engine.recognize(frame: frame, currentCharacterId: nil)

        #expect(result.screen.rawValue == expected.screen,
                "screen: got \(result.screen.rawValue), want \(expected.screen)")
        if let cards = expected.cards {
            #expect(Set(result.cards.map(\.cardId)) == Set(cards),
                    "cards: got \(result.cards.map(\.cardId))")
        }
        let accepted = result.shopItems.filter(\.accepted)
        if let relics = expected.relics {
            let got = accepted.filter { $0.kind == .relic }.compactMap(\.candidates.first?.id)
            #expect(Set(got) == Set(relics), "relics: got \(got)")
        }
        if let potions = expected.potions {
            let got = accepted.filter { $0.kind == .potion }.compactMap(\.candidates.first?.id)
            #expect(Set(got) == Set(potions), "potions: got \(got)")
        }
    }

    @Test func cardSelectFrame() async throws { try await run("card_select") }
    @Test func shopFrame() async throws { try await run("shop") }
    @Test func ancientFrame() async throws { try await run("ancient") }

    /// 負例: カード選択画面がエンシェント選択と誤判定されないこと（OCR 空ガードの回帰テスト）。
    @Test func cardSelectIsNotAncient() async throws {
        guard Self.assetsAvailable, let (frame, _) = Self.loadCase("card_select") else { return }
        let store = try ResourceStore()
        let db = try CardDatabase(store: store)
        let engine = RecognitionEngine(store: store, db: db)
        let (result, _) = await engine.recognize(frame: frame, currentCharacterId: nil)
        #expect(result.screen != .ancientSelect)
    }
}
