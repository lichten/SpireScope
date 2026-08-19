import Foundation

// StS2Shared/CharacterMechanics.cs の移植。キャラクター × メカニクスのフィルタ定義。

public struct MechanicDef: Sendable {
    public let enLabel: String
    public let jaLabel: String
    public let filter: @Sendable (String) -> Bool

    public init(_ enLabel: String, _ jaLabel: String, _ filter: @escaping @Sendable (String) -> Bool) {
        self.enLabel = enLabel
        self.jaLabel = jaLabel
        self.filter = filter
    }
}

public struct CharGroup: Sendable {
    public let enLabel: String
    public let jaLabel: String
    public let mechanics: [MechanicDef]

    public init(_ enLabel: String, _ jaLabel: String, _ mechanics: [MechanicDef]) {
        self.enLabel = enLabel
        self.jaLabel = jaLabel
        self.mechanics = mechanics
    }
}

public struct CharacterMechanics: Sendable {
    public static let characterLabels = ["Necrobinder", "Ironclad", "Silent", "Defect", "Regent"]

    public let all: [CharGroup]

    public init(db: CardDatabase) {
        all = [
            CharGroup("Necrobinder", "ネクロバインダー", [
                MechanicDef("Osty", "オスティ", { db.isNecroOsty($0) }),
                MechanicDef("Soul", "ソウル", { db.isNecroSoul($0) }),
                MechanicDef("Doom", "破滅", { db.isNecroDoom($0) }),
            ]),
            CharGroup("Ironclad", "アイアンクラッド", [
                MechanicDef("Strength", "筋力", { db.isIroncladStrength($0) }),
                MechanicDef("Exhaust", "廃棄", { db.isIroncladExhaust($0) }),
                MechanicDef("Strike", "ストライク", { db.isIroncladStrike($0) }),
            ]),
            CharGroup("Silent", "サイレント", [
                MechanicDef("Poison", "毒", { db.isSilentPoison($0) }),
                MechanicDef("Shiv", "ナイフ", { db.isSilentShiv($0) }),
            ]),
            CharGroup("Defect", "ディフェクト", [
                MechanicDef("Channel", "オーブ", { db.isDefectChannel($0) }),
                MechanicDef("FocusPower", "集中力", { db.isDefectFocus($0) }),
                MechanicDef("0 Energy", "0エナジー", { db.isDefectZeroEnergy($0) }),
            ]),
            CharGroup("Regent", "リージェント", [
                MechanicDef("Forge / Sovereign Blade", "鋳造 / ソヴリン・ブレード",
                            { db.isRegentForge($0) || db.isRegentBlade($0) }),
                MechanicDef("Card Creation", "カード作成", { db.isRegentCreate($0) }),
                MechanicDef("Star Gain", "Starを得る", { db.isRegentStarGain($0) }),
                MechanicDef("Star Spend", "Starを使用する", { db.isRegentStarSpend($0) }),
            ]),
            CharGroup("Other", "その他", []),
            CharGroup("Common", "共通", [
                MechanicDef("Weak", "脱力", { db.isWeak($0) }),
                MechanicDef("Vulnerable", "弱体", { db.isVulnerable($0) }),
            ]),
        ]
    }

    public func mechanicsFor(_ enLabel: String) -> [MechanicDef] {
        all.first { $0.enLabel == enLabel }?.mechanics ?? []
    }
}
