import SwiftUI
import SpireScopeCore

/// キャラクター概観（Windows 版 DeckOverviewForm の EnableCharacterMode 相当）。
/// 上部でキャラを選択（自動＝セーブ追従）し、デッキをキーワードグループで分類表示する。
struct CharacterOverviewView: View {
    @Environment(AppModel.self) private var model
    /// nil = 自動（セーブ）。
    @State private var selectedLabel: String?
    @State private var collapsed: Set<String> = []

    private let columns = [GridItem(.adaptive(minimum: CardTileView.tileWidth), spacing: 10)]

    /// 表示対象キャラのラベル。自動選択時はセーブのキャラへ追従（未判定なら nil）。
    private var effectiveLabel: String? {
        if let selectedLabel { return selectedLabel }
        return CharacterMechanics.characterLabels.first {
            $0.uppercased() == model.currentCharacterId
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Picker(model.japanese ? "キャラクター" : "Character",
                       selection: $selectedLabel) {
                    Text(model.japanese ? "自動（セーブ）" : "Auto (save)")
                        .tag(String?.none)
                    ForEach(CharacterMechanics.characterLabels, id: \.self) { label in
                        Text(label).tag(String?.some(label))
                    }
                }
                .frame(maxWidth: 300)
                Spacer()
            }
            .padding([.horizontal, .top])

            if let label = effectiveLabel {
                // 統計バー（BuildCharacterStats 相当）
                Text(model.builder.characterStats(
                    characterLabel: label, deck: model.deckCards, japanese: model.japanese))
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .padding(.horizontal)
                    .padding(.top, 6)

                groupList(label: label)
            } else {
                ContentUnavailableView(
                    model.japanese ? "キャラクター未判定（手動で選択してください）"
                                   : "Character unknown (select manually)",
                    systemImage: "person.crop.circle.badge.questionmark")
            }
        }
        .navigationTitle(effectiveLabel.map {
            model.japanese ? "\($0)概観" : "\($0) Overview"
        } ?? (model.japanese ? "キャラクター概観" : "Character Overview"))
    }

    @ViewBuilder
    private func groupList(label: String) -> some View {
        let groups = model.builder.keywordGroups(
            characterLabel: label, cards: model.deckCards, relics: model.relics,
            japanese: model.japanese)
        let deckTotal = model.deckCards.reduce(0) { $0 + $1.count }

        ScrollView {
            LazyVStack(alignment: .leading, spacing: 12, pinnedViews: [.sectionHeaders]) {
                ForEach(Array(groups.enumerated()), id: \.offset) { _, group in
                    if !group.cards.isEmpty || !group.relics.isEmpty {
                        groupView(group, deckTotal: deckTotal)
                    }
                }
            }
            .padding()
        }
    }

    @ViewBuilder
    private func groupView(_ group: KeywordGroup, deckTotal: Int) -> some View {
        let cardCount = group.cards.reduce(0) { $0 + $1.count }
        let isCollapsed = collapsed.contains(group.label)

        Section {
            if !isCollapsed {
                if !group.cards.isEmpty {
                    LazyVGrid(columns: columns, alignment: .leading, spacing: 10) {
                        ForEach(group.cards, id: \.self) { card in
                            CardTileView(card: card)
                        }
                    }
                }
                if !group.relics.isEmpty {
                    LazyVGrid(columns: columns, alignment: .leading, spacing: 10) {
                        ForEach(group.relics, id: \.self) { relic in
                            RelicTileView(relic: relic)
                        }
                    }
                }
            }
        } header: {
            Button {
                if isCollapsed { collapsed.remove(group.label) } else { collapsed.insert(group.label) }
            } label: {
                HStack(spacing: 6) {
                    Image(systemName: isCollapsed ? "chevron.right" : "chevron.down")
                        .font(.caption)
                    Text(group.label).font(.headline)
                    if cardCount > 0 {
                        Text(OverviewBuilder.formatCardCount(
                            cardCount, deckTotal: deckTotal, japanese: model.japanese))
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                }
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
            .padding(.vertical, 4)
            .background(.background)
        }
    }
}
