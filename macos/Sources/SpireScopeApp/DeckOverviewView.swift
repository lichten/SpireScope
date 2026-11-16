import SwiftUI
import SpireScopeCore

/// デッキ概観（Windows 版 DeckOverviewForm の ComposeFromSections モード相当）。
/// セクションごとに折りたたみ可能なグリッドでカード・レリックを表示する。
struct DeckOverviewView: View {
    @Environment(AppModel.self) private var model
    @State private var collapsed: Set<String> = []

    private let columns = [GridItem(.adaptive(minimum: CardTileView.tileWidth), spacing: 10)]

    var body: some View {
        let deckTotal = model.deckCards.reduce(0) { $0 + $1.count }
        ScrollView {
            LazyVStack(alignment: .leading, spacing: 12, pinnedViews: [.sectionHeaders]) {
                ForEach(Array(model.combinedSections.enumerated()), id: \.offset) { _, section in
                    if !section.cards.isEmpty || !section.relics.isEmpty {
                        sectionView(section, deckTotal: deckTotal)
                    }
                }
            }
            .padding()
        }
        .navigationTitle(model.japanese ? "デッキ概観" : "Deck Overview")
    }

    @ViewBuilder
    private func sectionView(_ section: OverviewSection, deckTotal: Int) -> some View {
        let label = section.label(japanese: model.japanese)
        let cardCount = section.cards.reduce(0) { $0 + $1.count }
        let isCollapsed = collapsed.contains(label)

        Section {
            if !isCollapsed {
                if !section.cards.isEmpty {
                    LazyVGrid(columns: columns, alignment: .leading, spacing: 10) {
                        ForEach(section.cards, id: \.self) { card in
                            CardTileView(card: card)
                        }
                    }
                }
                if !section.relics.isEmpty {
                    LazyVGrid(columns: columns, alignment: .leading, spacing: 10) {
                        ForEach(section.relics, id: \.self) { relic in
                            RelicTileView(relic: relic)
                        }
                    }
                }
            }
        } header: {
            Button {
                if isCollapsed { collapsed.remove(label) } else { collapsed.insert(label) }
            } label: {
                HStack(spacing: 6) {
                    Image(systemName: isCollapsed ? "chevron.right" : "chevron.down")
                        .font(.caption)
                    Text(label).font(.headline)
                    if cardCount > 0 {
                        Text(OverviewBuilder.formatCardCount(
                            cardCount, deckTotal: deckTotal, japanese: model.japanese))
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    if !section.relics.isEmpty {
                        Text(model.japanese
                             ? "レリック\(section.relics.count)個"
                             : "\(section.relics.count) relics")
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
