import Foundation
import Testing
@testable import SpireScopeCore

@Suite struct SaveDataTests {
    func fixture(_ name: String) throws -> URL {
        let url = Bundle.module.url(forResource: "Fixtures", withExtension: nil)?
            .appending(path: name)
        return try #require(url)
    }

    @Test func decodeCurrentRunSave() throws {
        let save = try SaveLocator.load(try fixture("current_run.save"))
        let player = try #require(save.players.first)
        #expect(!player.characterId.isEmpty)
        #expect(player.maxHp > 0)
        #expect(!player.deck.isEmpty)
        #expect(!save.acts.isEmpty)
        // acts[0].rooms はマップ生成済みなら必ず入る
        #expect(save.acts[0].rooms != nil)
    }

    @Test func normalizeCharacterId() {
        #expect(SaveLocator.normalizeCharacterId("CHARACTER.DEFECT") == "DEFECT")
        #expect(SaveLocator.normalizeCharacterId("ironclad") == "IRONCLAD")
        #expect(SaveLocator.normalizeCharacterId("") == nil)
        #expect(SaveLocator.normalizeCharacterId(nil) == nil)
    }
}

@Suite struct ResourceStoreTests {
    @Test func resolvesLatestVersion() throws {
        let store = try ResourceStore()
        let version = try #require(store.versionName)
        #expect(version.hasPrefix("v"))
        // 主要リソースが読めること
        #expect(store.versionedData("card_costs.json") != nil)
        #expect(store.versionedData("card_database.json") != nil)
        #expect(store.versionedData("localization/jpn/relics.json") != nil)
        #expect(store.unversionedData("monster_move_patterns.json") != nil)
    }

    @Test func versionKeyOrdersNumerically() {
        // 0.9 < 0.10 < 0.111（文字列比較では逆転する組み合わせ）
        #expect(ResourceStore.versionKey("v0.9.0") < ResourceStore.versionKey("v0.10.0"))
        #expect(ResourceStore.versionKey("v0.10.0") < ResourceStore.versionKey("v0.111.0"))
    }
}
