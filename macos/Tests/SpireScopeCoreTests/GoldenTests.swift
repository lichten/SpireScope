import Foundation
import Testing
@testable import SpireScopeCore

/// C# 実装（StS2Shared）の出力ダンプ（Fixtures/golden.json）と Swift 移植を突き合わせる。
/// ダンプの再生成はリポジトリ外の一時プロジェクト（StS2Shared を参照する小さな
/// コンソールアプリで CardDatabaseService / DescriptionFormatter を全 ID 分呼ぶ）で行う。
@Suite struct GoldenTests {
    // テストからの読み取り専用のため共有安全。
    nonisolated(unsafe) static let golden: [String: Any] = {
        guard let url = Bundle.module.url(forResource: "Fixtures", withExtension: nil)?
            .appending(path: "golden.json"),
            let data = try? Data(contentsOf: url),
            let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
        else { return [:] }
        return obj
    }()

    static let db = try! CardDatabase(store: ResourceStore())

    @Test func cardsMatchCSharp() throws {
        let cards = try #require(Self.golden["cards"] as? [String: Any])
        #expect(cards.count > 500)
        let db = Self.db
        var mismatches: [String] = []

        func check(_ id: String, _ field: String, _ actual: some Equatable & Sendable, _ expected: Any?) {
            // JSON からの期待値と比較（Bool/String のみ）
            if let e = expected as? Bool, let a = actual as? Bool {
                if a != e { mismatches.append("\(id).\(field): swift=\(a) csharp=\(e)") }
            } else if let e = expected as? String, let a = actual as? String {
                if a != e { mismatches.append("\(id).\(field): swift=\(a) csharp=\(e)") }
            } else {
                mismatches.append("\(id).\(field): 型不一致")
            }
        }

        for (id, value) in cards {
            guard let g = value as? [String: Any] else { continue }
            check(id, "nameEn", db.name(id, japanese: false), g["nameEn"])
            check(id, "nameJa", db.name(id, japanese: true), g["nameJa"])
            check(id, "cost", db.cardCost(id), g["cost"])
            check(id, "upgradedCost", db.upgradedCost(id), g["upgradedCost"])
            check(id, "type", db.cardType(id), g["type"])
            check(id, "rarity", db.cardRarity(id), g["rarity"])
            check(id, "character", db.cardCharacter(id), g["character"])
            check(id, "blockGiver", db.isBlockGiver(id), g["blockGiver"])
            check(id, "drawRelated", db.isDrawRelated(id), g["drawRelated"])
            check(id, "allEnemiesAttack", db.isAllEnemiesAttack(id), g["allEnemiesAttack"])
            check(id, "weak", db.isWeak(id), g["weak"])
            check(id, "vulnerable", db.isVulnerable(id), g["vulnerable"])
            check(id, "necroOsty", db.isNecroOsty(id), g["necroOsty"])
            check(id, "necroSoul", db.isNecroSoul(id), g["necroSoul"])
            check(id, "necroDoom", db.isNecroDoom(id), g["necroDoom"])
            check(id, "necroSummon", db.isNecroSummon(id), g["necroSummon"])
            check(id, "ironcladStrength", db.isIroncladStrength(id), g["ironcladStrength"])
            check(id, "ironcladExhaust", db.isIroncladExhaust(id), g["ironcladExhaust"])
            check(id, "ironcladStrike", db.isIroncladStrike(id), g["ironcladStrike"])
            check(id, "silentPoison", db.isSilentPoison(id), g["silentPoison"])
            check(id, "silentShiv", db.isSilentShiv(id), g["silentShiv"])
            check(id, "defectChannel", db.isDefectChannel(id), g["defectChannel"])
            check(id, "defectFocus", db.isDefectFocus(id), g["defectFocus"])
            check(id, "defectZeroEnergy", db.isDefectZeroEnergy(id), g["defectZeroEnergy"])
            check(id, "regentForge", db.isRegentForge(id), g["regentForge"])
            check(id, "regentBlade", db.isRegentBlade(id), g["regentBlade"])
            check(id, "regentCreate", db.isRegentCreate(id), g["regentCreate"])
            check(id, "regentStarGain", db.isRegentStarGain(id), g["regentStarGain"])
            check(id, "regentStarSpend", db.isRegentStarSpend(id), g["regentStarSpend"])
            check(id, "exhaustKeyword", db.isExhaustKeyword(id), g["exhaustKeyword"])
            check(id, "exhaustKeywordUpgraded", db.isExhaustKeyword(id, upgraded: true),
                  g["exhaustKeywordUpgraded"])
            check(id, "etherealKeyword", db.isEtherealKeyword(id), g["etherealKeyword"])
            check(id, "etherealKeywordUpgraded", db.isEtherealKeyword(id, upgraded: true),
                  g["etherealKeywordUpgraded"])
            check(id, "exhaustAction", db.isExhaustAction(id), g["exhaustAction"])

            let desc = db.description(id)
            let stats = db.cardStats(id)
            check(id, "descResolvedEn",
                  DescriptionFormatter.resolve(desc.en, stats: stats), g["descResolvedEn"])
            check(id, "descResolvedJa",
                  DescriptionFormatter.resolve(desc.ja, stats: stats, japanese: true), g["descResolvedJa"])
            check(id, "descResolvedEnUpgraded",
                  DescriptionFormatter.resolve(desc.en, stats: stats, upgraded: true),
                  g["descResolvedEnUpgraded"])
        }

        #expect(mismatches.isEmpty, Comment(rawValue: "\(mismatches.count) 件の不一致:\n" +
            mismatches.prefix(30).joined(separator: "\n")))
    }

    @Test func relicsMatchCSharp() throws {
        let relics = try #require(Self.golden["relics"] as? [String: Any])
        #expect(relics.count > 250)
        let db = Self.db
        var mismatches: [String] = []

        func check(_ id: String, _ field: String, _ actual: String, _ expected: Any?) {
            if actual != (expected as? String) {
                mismatches.append("\(id).\(field): swift=\(actual) csharp=\(expected ?? "nil")")
            }
        }
        func checkBool(_ id: String, _ field: String, _ actual: Bool, _ expected: Any?) {
            if actual != (expected as? Bool) {
                mismatches.append("\(id).\(field): swift=\(actual) csharp=\(expected ?? "nil")")
            }
        }

        for (raw, value) in relics {
            guard let g = value as? [String: Any] else { continue }
            let id = "RELIC." + raw
            check(raw, "nameEn", db.name(id, japanese: false), g["nameEn"])
            check(raw, "nameJa", db.name(id, japanese: true), g["nameJa"])
            check(raw, "titleEn", db.relicTitle(id, japanese: false), g["titleEn"])
            check(raw, "titleJa", db.relicTitle(id, japanese: true), g["titleJa"])
            check(raw, "rarity", db.relicRarity(raw), g["rarity"])
            checkBool(raw, "relicBlockGiver", db.isRelicBlockGiver(id), g["relicBlockGiver"])
            checkBool(raw, "relicDrawRelated", db.isRelicDrawRelated(id), g["relicDrawRelated"])
            checkBool(raw, "weak", db.isWeak(id), g["weak"])
            checkBool(raw, "vulnerable", db.isVulnerable(id), g["vulnerable"])

            let desc = db.description(id)
            let stats = db.relicStats(raw)
            check(raw, "descResolvedEn",
                  DescriptionFormatter.resolve(desc.en, stats: stats), g["descResolvedEn"])
            check(raw, "descResolvedJa",
                  DescriptionFormatter.resolve(desc.ja, stats: stats, japanese: true), g["descResolvedJa"])
        }

        #expect(mismatches.isEmpty, Comment(rawValue: "\(mismatches.count) 件の不一致:\n" +
            mismatches.prefix(30).joined(separator: "\n")))
    }

    @Test func potionOddsFromFixture() throws {
        let url = try #require(Bundle.module.url(forResource: "Fixtures", withExtension: nil)?
            .appending(path: "current_run.save"))
        let save = try SaveLocator.load(url)
        let player = try #require(save.players.first)
        if player.odds != nil {
            let odds = try #require(PotionOddsService.compute(player))
            #expect(odds.normal >= 0 && odds.normal <= 1)
            #expect(odds.elite >= odds.normal)
        }
    }
}
