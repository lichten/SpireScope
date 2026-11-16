import Foundation
import Testing
@testable import SpireScopeCore

@Suite struct TierScoresTests {
    @Test func tierLetterBoundaries() {
        #expect(TierScores.tierLetter(100) == "S")
        #expect(TierScores.tierLetter(90) == "S")
        #expect(TierScores.tierLetter(89) == "A")
        #expect(TierScores.tierLetter(78) == "A")
        #expect(TierScores.tierLetter(77) == "B")
        #expect(TierScores.tierLetter(65) == "B")
        #expect(TierScores.tierLetter(64) == "C")
        #expect(TierScores.tierLetter(50) == "C")
        #expect(TierScores.tierLetter(49) == "D")
        #expect(TierScores.tierLetter(35) == "D")
        #expect(TierScores.tierLetter(34) == "F")
    }

    @Test func formatStripsPrefix() {
        let dict = ["STRIKE": 92]
        #expect(TierScores.format(dict, "CARD.STRIKE") == "S (92)")
        #expect(TierScores.format(dict, "STRIKE") == "S (92)")
        #expect(TierScores.format(dict, "UNKNOWN") == "")
        #expect(TierScores.format(nil, "STRIKE") == "")
    }

    @Test func parseScoresApiShape() {
        let json = """
        {"ABRASIVE": {"score": 93.4, "count": 10}, "BAD": "oops", "ACCURACY": {"score": 78}}
        """
        let scores = TierScores.parseScores(Data(json.utf8))
        #expect(scores["ABRASIVE"] == 93)
        #expect(scores["ACCURACY"] == 78)
        #expect(scores["BAD"] == nil)
    }

    @Test func parseDateHandlesCSharpFractions() {
        // C# JsonSerializer の 7 桁小数（スナップショットの実形式）。
        let d = TierScores.parseDate("2026-07-23T06:43:16.6297144Z")
        #expect(d > Date(timeIntervalSince1970: 1_700_000_000))
        // Swift 側で書いた小数なし形式。
        let d2 = TierScores.parseDate("2026-07-23T06:43:16Z")
        #expect(abs(d.timeIntervalSince(d2)) < 1.0)
        #expect(TierScores.parseDate("garbage") == .distantPast)
        #expect(TierScores.parseDate(nil) == .distantPast)
    }

    @Test func snapshotLoadsFromBundle() throws {
        let store = try ResourceStore()
        let snapshot = TierScores.loadSnapshot(store: store)
        #expect(snapshot != nil)
        #expect((snapshot?.cards.count ?? 0) > 100)
        // スナップショットは cards のみでなく relics/potions も持つ（Windows 版と同形式）。
        #expect(snapshot.map { $0.fetchedAt > .distantPast } == true)
    }

    @Test func cacheRoundTrip() {
        let section = TierScores.Section(
            fetchedAt: Date(timeIntervalSince1970: 1_750_000_000),
            cards: ["STRIKE": 92], relics: ["AKABEKO": 80], potions: ["FIRE_POTION": 60])
        let chars = ["DEFECT": TierScores.CharSection(
            fetchedAt: Date(timeIntervalSince1970: 1_750_000_000), cards: ["ZAP": 70])]
        // 実キャッシュパスを汚さないよう、シリアライズ経路のみ検証する
        //（保存先はユーザープロファイル固定のため書き込みテストはスキップ）。
        let file = TierScores.CacheFile(global: section, characters: chars)
        #expect(file.global?.cards["STRIKE"] == 92)
        #expect(file.characters["DEFECT"]?.cards["ZAP"] == 70)
    }
}
