import SwiftUI
import SpireScopeCore

/// カードタイル（Windows 版 DrawCard の SwiftUI 版）。
/// サムネ 120×91 + コストバッジ（左上）・枚数バッジ（右下）・エンチャントバッジ（下辺）。
/// アップグレードは名前の "+" と枠色で表現。ホバーで説明ツールチップ、クリックで外部リンク。
struct CardTileView: View {
    @Environment(AppModel.self) private var model
    let card: DeckCard

    static let tileWidth: CGFloat = 120
    static let imageHeight: CGFloat = 91

    var body: some View {
        VStack(spacing: 2) {
            ZStack(alignment: .topLeading) {
                tileImage
                costBadge
            }
            .overlay(alignment: .bottomTrailing) { countBadge }
            .overlay(alignment: .bottomLeading) { enchantBadge }

            Text(card.name(japanese: model.japanese))
                .font(.caption2)
                .lineLimit(2)
                .multilineTextAlignment(.center)
                .foregroundStyle(card.isUpgraded ? .green : .primary)
        }
        .frame(width: Self.tileWidth)
        .help(model.cardTooltip(card))
        .onTapGesture { LinkOpener.open(model.links(kind: "card", id: card.id)) }
        .contextMenu { LinkOpener.menu(model.links(kind: "card", id: card.id)) }
    }

    @ViewBuilder private var tileImage: some View {
        if let image = ImageCache.image(at: model.assets.cardImageURL(card.id)) {
            Image(nsImage: image)
                .resizable()
                .aspectRatio(contentMode: .fill)
                .frame(width: Self.tileWidth, height: Self.imageHeight)
                .clipShape(RoundedRectangle(cornerRadius: 6))
                .overlay(RoundedRectangle(cornerRadius: 6)
                    .stroke(card.isUpgraded ? Color.green : Color.gray.opacity(0.4),
                            lineWidth: card.isUpgraded ? 2 : 1))
        } else {
            RoundedRectangle(cornerRadius: 6)
                .fill(Color.gray.opacity(0.15))
                .frame(width: Self.tileWidth, height: Self.imageHeight)
                .overlay(Text(card.name(japanese: model.japanese))
                    .font(.caption)
                    .multilineTextAlignment(.center)
                    .padding(4))
        }
    }

    @ViewBuilder private var costBadge: some View {
        let cost = card.isUpgraded && !model.db.upgradedCost(card.id).isEmpty
            ? model.db.upgradedCost(card.id) : card.cost
        if !cost.isEmpty {
            Text(cost)
                .font(.caption.bold())
                .foregroundStyle(.white)
                .frame(width: 20, height: 20)
                .background(Circle().fill(Color.blue.opacity(0.85)))
                .padding(2)
        }
    }

    @ViewBuilder private var countBadge: some View {
        if card.count > 1 {
            Text("×\(card.count)")
                .font(.caption.bold())
                .foregroundStyle(.white)
                .padding(.horizontal, 5)
                .padding(.vertical, 1)
                .background(Capsule().fill(Color.black.opacity(0.7)))
                .padding(3)
        }
    }

    @ViewBuilder private var enchantBadge: some View {
        if !card.enchantmentId.isEmpty {
            if let icon = ImageCache.image(at: model.assets.enchantmentIconURL(card.enchantmentId)) {
                Image(nsImage: icon)
                    .resizable()
                    .frame(width: 18, height: 18)
                    .padding(3)
            } else {
                Circle()
                    .fill(Color.purple.opacity(0.85))
                    .frame(width: 10, height: 10)
                    .padding(5)
            }
        }
    }
}

/// レリックタイル（Windows 版 DrawRelicTile の SwiftUI 版）。レアリティ色の背景に画像を内接。
struct RelicTileView: View {
    @Environment(AppModel.self) private var model
    let relic: RelicEntry

    static let tileSize: CGFloat = 56

    var body: some View {
        VStack(spacing: 2) {
            ZStack {
                RoundedRectangle(cornerRadius: 6)
                    .fill(rarityColor.opacity(0.25))
                if let image = ImageCache.image(at: model.assets.relicImageURL(relic.id)) {
                    Image(nsImage: image)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .padding(4)
                } else {
                    Text(relic.name(japanese: model.japanese))
                        .font(.system(size: 8))
                        .multilineTextAlignment(.center)
                        .padding(2)
                }
            }
            .frame(width: Self.tileSize, height: Self.tileSize)

            Text(relic.name(japanese: model.japanese))
                .font(.caption2)
                .lineLimit(2)
                .multilineTextAlignment(.center)
        }
        .frame(width: CardTileView.tileWidth * 0.75)
        .help(model.relicTooltip(relic))
        .onTapGesture { LinkOpener.open(model.links(kind: "relic", id: relic.id)) }
        .contextMenu { LinkOpener.menu(model.links(kind: "relic", id: relic.id)) }
    }

    private var rarityColor: Color {
        switch model.db.relicRarity(CardDatabase.rawId(relic.id)) {
        case "Common": return .gray
        case "Uncommon": return .teal
        case "Rare": return .yellow
        case "Boss": return .red
        case "Shop": return .purple
        case "Ancient": return .orange
        default: return .gray
        }
    }
}

/// 外部リンクを開く（1件なら即オープン、複数はコンテキストメニューで選択）。
enum LinkOpener {
    static func open(_ links: [SiteLinkService.Link]) {
        guard let first = links.first, let url = URL(string: first.url) else { return }
        NSWorkspace.shared.open(url)
    }

    @ViewBuilder static func menu(_ links: [SiteLinkService.Link]) -> some View {
        ForEach(links, id: \.url) { link in
            Button(link.label) {
                if let url = URL(string: link.url) { NSWorkspace.shared.open(url) }
            }
        }
    }
}
