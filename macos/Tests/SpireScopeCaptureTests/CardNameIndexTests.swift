import Foundation
import Testing
@testable import SpireScopeCapture

@Suite struct CardNameIndexTests {
    @Test func normalizeKeepsKanaKanjiDropsSymbols() {
        #expect(CardNameIndex.normalize(" Bag of Marbles! ") == "bagofmarbles")
        #expect(CardNameIndex.normalize("パエルの涙") == "パエルの涙")
        #expect(CardNameIndex.normalize("ビー玉の袋（+1）") == "ビー玉の袋1")
    }

    @Test func levenshteinCapped() {
        #expect(CardNameIndex.levenshtein(Array("kitten"), Array("sitting"), max: 3) == 3)
        // 上限超過は max+1 を返す（早期打ち切り）。
        #expect(CardNameIndex.levenshtein(Array("abcdef"), Array("uvwxyz"), max: 2) == 3)
        #expect(CardNameIndex.levenshtein(Array("same"), Array("same"), max: 2) == 0)
    }

    @Test func findRankedOrdersByDistance() {
        let idx = CardNameIndex()
        idx.add("パエルの涙", id: "PAEL_TEAR")
        idx.add("パエルの盾", id: "PAEL_SHIELD")
        idx.add("アカベコ", id: "AKABEKO")

        // 完全一致は距離 0 で先頭。
        let exact = idx.findRanked("パエルの涙", maxDist: 2)
        #expect(exact.first?.id == "PAEL_TEAR")
        #expect(exact.first?.distance == 0)

        // 末尾 1 文字違い → 同一プレフィックスの 2 件が距離 1 以内に入る（family 形成）。
        let fuzzy = idx.findRanked("パエルの?", maxDist: 2)
        let ids = Set(fuzzy.map(\.id))
        #expect(ids.contains("PAEL_TEAR") && ids.contains("PAEL_SHIELD"))
        #expect(!ids.contains("AKABEKO"))
    }

    @Test func shortNamesRejected() {
        let idx = CardNameIndex()
        idx.add("ab", id: "TOO_SHORT") // 3 文字未満は登録されない
        #expect(idx.findRanked("ab", maxDist: 2).isEmpty)
    }
}
