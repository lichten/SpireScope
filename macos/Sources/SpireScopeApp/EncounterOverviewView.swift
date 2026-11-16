import SwiftUI
import SpireScopeCore

/// 敵情報（Windows 版 EncounterOverviewForm の SwiftUI 版）。
/// アクト切替 → Weak/Normal/Elite/Boss 階層のエンカウンター一覧。
/// 現在 Act は遭遇済みに ✓＋ディム、セーブで確定済みの出現予定ボスに ★＋ハイライト。
struct EncounterOverviewView: View {
    @Environment(AppModel.self) private var model
    /// nil = 自動（現在 Act）。
    @State private var selectedActId: String?

    private var currentActId: String? {
        guard let save = model.save,
              save.currentActIndex >= 0, save.currentActIndex < save.acts.count
        else { return nil }
        return CardDatabase.rawId(save.acts[save.currentActIndex].id)
    }

    private var selectedGroup: ActEncounters? {
        if let selectedActId {
            return model.encounterActs.first { $0.id == selectedActId }
        }
        guard let currentActId else { return nil }
        return model.encounterActs.first { $0.id.caseInsensitiveCompare(currentActId) == .orderedSame }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Picker(model.japanese ? "アクト" : "Act", selection: $selectedActId) {
                    Text(model.japanese ? "自動（現在Act）" : "Auto (current Act)")
                        .tag(String?.none)
                    ForEach(model.encounterActs) { group in
                        Text(group.name(japanese: model.japanese)).tag(String?.some(group.id))
                    }
                }
                .frame(maxWidth: 340)
                Spacer()
            }
            .padding([.horizontal, .top])

            if let group = selectedGroup {
                encounterList(group)
            } else {
                ContentUnavailableView(
                    model.japanese ? "セーブを読み込むと現在Actを表示します。"
                                   : "Load a save to show the current Act.",
                    systemImage: "shield.lefthalf.filled")
            }
        }
        .navigationTitle(model.japanese ? "敵情報" : "Encounters")
    }

    @ViewBuilder
    private func encounterList(_ group: ActEncounters) -> some View {
        let isCurrentAct = currentActId.map {
            group.id.caseInsensitiveCompare($0) == .orderedSame
        } ?? false
        let visited = isCurrentAct ? visitedSet() : []
        let predicted = predictedBossSet(group)
        let boss = group.bossOrder.isEmpty ? group.boss : group.bossOrder
        let tiers: [(ja: String, en: String, ids: [String])] = [
            ("弱小", "Weak", group.weak),
            ("通常", "Normal", group.normal),
            ("エリート", "Elite", group.elite),
            ("ボス", "Boss", boss),
        ]

        List {
            Section {
                Text(group.name(japanese: model.japanese)
                     + (isCurrentAct ? (model.japanese ? "　【現在】" : "  [Current]") : ""))
                    .font(.headline)
                    .foregroundStyle(.blue)
            }
            ForEach(tiers, id: \.en) { tier in
                if !tier.ids.isEmpty {
                    Section(model.japanese ? tier.ja : tier.en) {
                        ForEach(tier.ids, id: \.self) { encId in
                            EncounterRowView(
                                encId: encId,
                                visited: visited.contains(CardDatabase.rawId(encId)),
                                predicted: predicted.contains(CardDatabase.rawId(encId)))
                        }
                    }
                }
            }
        }
    }

    /// 現在ラン Act で遭遇済みのエンカウンター ID 集合（接頭辞除去）。
    private func visitedSet() -> Set<String> {
        var set: Set<String> = []
        guard let save = model.save,
              save.currentActIndex >= 0, save.currentActIndex < save.acts.count,
              let rooms = save.acts[save.currentActIndex].rooms
        else { return set }
        for id in rooms.normalEncounterIds.prefix(rooms.normalEncountersVisited) {
            set.insert(CardDatabase.rawId(id))
        }
        for id in rooms.eliteEncounterIds.prefix(rooms.eliteEncountersVisited) {
            set.insert(CardDatabase.rawId(id))
        }
        if rooms.bossEncountersVisited > 0 {
            if let bossId = rooms.bossId, !bossId.isEmpty { set.insert(CardDatabase.rawId(bossId)) }
            if let second = rooms.secondBossId, !second.isEmpty { set.insert(CardDatabase.rawId(second)) }
        }
        return set
    }

    /// 表示中 Act でセーブが確定済みのボス ID 集合。Act 入場時に確定するため戦闘前でも出る。
    private func predictedBossSet(_ group: ActEncounters) -> Set<String> {
        var set: Set<String> = []
        guard let act = model.save?.acts.first(where: {
            CardDatabase.rawId($0.id).caseInsensitiveCompare(group.id) == .orderedSame
        }), let rooms = act.rooms else { return set }
        if let bossId = rooms.bossId, !bossId.isEmpty { set.insert(CardDatabase.rawId(bossId)) }
        if let second = rooms.secondBossId, !second.isEmpty { set.insert(CardDatabase.rawId(second)) }
        return set
    }
}

struct EncounterRowView: View {
    @Environment(AppModel.self) private var model
    let encId: String
    let visited: Bool
    let predicted: Bool

    private static let thumb: CGFloat = 40

    var body: some View {
        HStack(spacing: 6) {
            Text(visited ? "✓" : predicted ? "★" : "")
                .font(.body.bold())
                .foregroundStyle(visited ? Color.green : Color.orange)
                .frame(width: 18)

            Text(model.encounterDb.encounterName(encId, japanese: model.japanese))
                .font(predicted && !visited ? .body.bold() : .body)
                .foregroundStyle(visited ? Color.secondary : predicted ? Color.orange : Color.primary)
                .frame(width: 200, alignment: .leading)
                .lineLimit(1)

            ForEach(model.monsterDb.encounterMonsterDirs(CardDatabase.rawId(encId)) ?? [], id: \.self) { dir in
                if let image = ImageCache.image(at: model.assets.monsterImageURL(dir)) {
                    Image(nsImage: image)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: Self.thumb, height: Self.thumb)
                } else {
                    RoundedRectangle(cornerRadius: 4)
                        .stroke(Color.gray.opacity(0.5))
                        .frame(width: Self.thumb, height: Self.thumb)
                        .overlay(Text("?").foregroundStyle(.secondary))
                }
            }
            Spacer()
        }
        .opacity(visited ? 0.55 : 1)
        .listRowBackground(predicted ? Color.yellow.opacity(0.15) : nil)
        .help(tooltip)
        .contentShape(Rectangle())
        .onTapGesture { LinkOpener.open(model.links(kind: "encounter", id: encId)) }
        .contextMenu { LinkOpener.menu(model.links(kind: "encounter", id: encId)) }
    }

    private var tooltip: String {
        var lines = [model.encounterDb.encounterName(encId, japanese: model.japanese)]
        let monsters = model.monsterDb.encounterMonsters(CardDatabase.rawId(encId))
        if !monsters.isEmpty {
            lines.append(monsters.map { $0.label(japanese: model.japanese) }
                .joined(separator: model.japanese ? "、" : ", "))
        }
        return lines.joined(separator: "\n")
    }
}
