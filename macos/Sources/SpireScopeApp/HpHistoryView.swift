import SwiftUI
import Charts
import SpireScopeCore

/// フロアごとの HP 記録（Windows 版 HpHistoryForm の FloorPoint 相当）。
struct FloorPoint: Identifiable {
    let actIndex: Int
    let actFloor: Int
    let globalFloor: Int
    let floorType: String
    let currentHp: Int
    let maxHp: Int
    let damageTaken: Int
    let hpHealed: Int
    let maxHpGained: Int
    let maxHpLost: Int

    var id: Int { globalFloor }
    var netChange: Int { hpHealed - damageTaken + maxHpGained - maxHpLost }
}

/// HP 推移（Windows 版 HpHistoryForm の SwiftUI 版）。上段 Swift Charts、下段フロア一覧。
struct HpHistoryView: View {
    @Environment(AppModel.self) private var model
    @State private var hovered: FloorPoint?

    private var points: [FloorPoint] {
        Self.buildPoints(model.save?.mapPointHistory ?? [])
    }

    /// Windows 版 BuildPoints の移植。MaxHp=0 は直前値へフォールバック。
    static func buildPoints(_ history: [[MapPointHistoryEntry]]) -> [FloorPoint] {
        var result: [FloorPoint] = []
        var globalFloor = 0
        for (actIdx, act) in history.enumerated() {
            for (fi, entry) in act.enumerated() {
                guard let stat = entry.playerStats?.first else { continue }
                globalFloor += 1
                let maxHp = stat.maxHp > 0 ? stat.maxHp : (result.last?.maxHp ?? 1)
                result.append(FloorPoint(
                    actIndex: actIdx, actFloor: fi + 1, globalFloor: globalFloor,
                    floorType: entry.mapPointType,
                    currentHp: stat.currentHp, maxHp: maxHp,
                    damageTaken: stat.damageTaken, hpHealed: stat.hpHealed,
                    maxHpGained: stat.maxHpGained, maxHpLost: stat.maxHpLost))
            }
        }
        return result
    }

    var body: some View {
        let pts = points
        Group {
            if pts.isEmpty {
                ContentUnavailableView(
                    model.japanese ? "履歴なし" : "No history",
                    systemImage: "chart.line.uptrend.xyaxis")
            } else {
                VSplitView {
                    chart(pts)
                        .padding()
                        .frame(minHeight: 200)
                    floorList(pts)
                        .frame(minHeight: 140)
                }
            }
        }
        .navigationTitle(model.japanese ? "HP推移" : "HP History")
    }

    @ViewBuilder
    private func chart(_ pts: [FloorPoint]) -> some View {
        let actStarts = pts.filter { $0.actFloor == 1 && $0.actIndex > 0 }
        Chart {
            // HP 面＋線
            ForEach(pts) { p in
                AreaMark(x: .value("Floor", p.globalFloor),
                         y: .value("HP", p.currentHp))
                    .foregroundStyle(Color.blue.opacity(0.2))
                LineMark(x: .value("Floor", p.globalFloor),
                         y: .value("HP", p.currentHp))
                    .foregroundStyle(Color.blue)
            }
            // 最大 HP 点線（ステップ状）
            ForEach(pts) { p in
                LineMark(x: .value("Floor", p.globalFloor),
                         y: .value("MaxHP", p.maxHp),
                         series: .value("series", "max"))
                    .foregroundStyle(Color.gray)
                    .lineStyle(StrokeStyle(lineWidth: 1, dash: [3, 3]))
                    .interpolationMethod(.stepEnd)
            }
            // フロア種別マーク
            ForEach(pts) { p in
                PointMark(x: .value("Floor", p.globalFloor),
                          y: .value("HP", p.currentHp))
                    .foregroundStyle(Self.floorTypeColor(p.floorType))
                    .symbolSize(p.currentHp == 0 ? 100 : 36)
            }
            // アクト境界
            ForEach(actStarts) { p in
                RuleMark(x: .value("Floor", p.globalFloor))
                    .foregroundStyle(Color.gray.opacity(0.4))
                    .lineStyle(StrokeStyle(lineWidth: 1, dash: [4, 4]))
                    .annotation(position: .top, alignment: .leading) {
                        Text("Act\(p.actIndex + 1)")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                    }
            }
            if let hovered {
                RuleMark(x: .value("Floor", hovered.globalFloor))
                    .foregroundStyle(Color.secondary.opacity(0.5))
                    .annotation(position: .topTrailing) {
                        VStack(alignment: .leading, spacing: 2) {
                            Text("Act\(hovered.actIndex + 1) - \(hovered.actFloor) " +
                                 Self.localizeFloorType(hovered.floorType, japanese: model.japanese))
                            Text("HP \(hovered.currentHp)/\(hovered.maxHp)")
                        }
                        .font(.caption)
                        .padding(6)
                        .background(RoundedRectangle(cornerRadius: 6).fill(.background.opacity(0.9)))
                    }
            }
        }
        .chartYScale(domain: 0...(pts.map(\.maxHp).max() ?? 1))
        .chartOverlay { proxy in
            GeometryReader { _ in
                Rectangle().fill(.clear).contentShape(Rectangle())
                    .onContinuousHover { phase in
                        switch phase {
                        case .active(let location):
                            if let floor: Int = proxy.value(atX: location.x) {
                                hovered = pts.min { abs($0.globalFloor - floor) < abs($1.globalFloor - floor) }
                            }
                        case .ended:
                            hovered = nil
                        }
                    }
            }
        }
    }

    @ViewBuilder
    private func floorList(_ pts: [FloorPoint]) -> some View {
        let ja = model.japanese
        Table(pts.reversed()) {
            TableColumn(ja ? "アクト" : "Act") { p in Text("Act\(p.actIndex + 1)") }
                .width(50)
            TableColumn(ja ? "階" : "Floor") { p in Text("\(p.actFloor)") }
                .width(40)
            TableColumn(ja ? "種別" : "Type") { p in
                HStack(spacing: 4) {
                    Circle().fill(Self.floorTypeColor(p.floorType)).frame(width: 8, height: 8)
                    Text(Self.localizeFloorType(p.floorType, japanese: ja))
                }
            }
            TableColumn("HP") { p in
                Text("\(p.currentHp)/\(p.maxHp)")
                    .foregroundStyle(p.currentHp == 0 ? .red
                        : p.damageTaken > p.hpHealed ? .orange
                        : p.hpHealed > p.damageTaken ? .green : .primary)
            }
            TableColumn(ja ? "増減" : "Δ") { p in
                Text(p.netChange > 0 ? "+\(p.netChange)" : p.netChange < 0 ? "\(p.netChange)" : "-")
            }
            .width(50)
            TableColumn(ja ? "被ダメ" : "Dmg") { p in
                Text(p.damageTaken > 0 ? "\(p.damageTaken)" : "-")
            }
            .width(50)
            TableColumn(ja ? "回復" : "Heal") { p in
                Text(p.hpHealed > 0 ? "\(p.hpHealed)" : "-")
            }
            .width(50)
        }
    }

    static func floorTypeColor(_ type: String) -> Color {
        switch type.lowercased() {
        case "monster": return Color(red: 1, green: 0.43, blue: 0.43)
        case "elite": return Color(red: 0.82, green: 0.24, blue: 0.24)
        case "boss": return Color(red: 0.67, green: 0.08, blue: 0.08)
        case "rest": return Color(red: 0.39, green: 0.78, blue: 0.39)
        case "merchant": return Color(red: 1, green: 0.86, blue: 0.31)
        case "treasure": return Color(red: 1, green: 0.78, blue: 0.2)
        case "event": return Color(red: 0.59, green: 0.59, blue: 1)
        case "ancient": return Color(red: 0.78, green: 0.59, blue: 1)
        case "unknown": return Color(red: 0.71, green: 0.63, blue: 0.86)
        default: return Color.gray
        }
    }

    static func localizeFloorType(_ type: String, japanese: Bool) -> String {
        guard japanese else { return type.isEmpty ? "?" : type }
        switch type.lowercased() {
        case "monster": return "戦闘"
        case "elite": return "精鋭"
        case "boss": return "ボス"
        case "rest": return "休憩"
        case "merchant": return "商人"
        case "treasure": return "宝"
        case "event": return "イベント"
        case "ancient": return "古代の儀式"
        case "unknown": return "???"
        default: return type.isEmpty ? "不明" : type
        }
    }
}
