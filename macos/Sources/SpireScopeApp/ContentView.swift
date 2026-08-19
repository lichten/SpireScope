import SwiftUI
import SpireScopeCore

enum SidebarItem: String, CaseIterable, Identifiable {
    case deck
    case character
    case encounters
    case hpHistory
    case settings

    var id: String { rawValue }

    func label(japanese: Bool) -> String {
        switch self {
        case .deck: return japanese ? "デッキ概観" : "Deck Overview"
        case .character: return japanese ? "キャラクター概観" : "Character Overview"
        case .encounters: return japanese ? "敵情報" : "Encounters"
        case .hpHistory: return japanese ? "HP推移" : "HP History"
        case .settings: return japanese ? "設定" : "Settings"
        }
    }

    var systemImage: String {
        switch self {
        case .deck: return "rectangle.grid.3x2"
        case .character: return "person.crop.rectangle.stack"
        case .encounters: return "shield.lefthalf.filled"
        case .hpHistory: return "chart.line.uptrend.xyaxis"
        case .settings: return "gearshape"
        }
    }
}

struct ContentView: View {
    @Environment(AppModel.self) private var model
    @State private var selection: SidebarItem = .deck

    var body: some View {
        NavigationSplitView {
            VStack(spacing: 0) {
                List(SidebarItem.allCases, selection: $selection) { item in
                    Label(item.label(japanese: model.japanese), systemImage: item.systemImage)
                        .tag(item)
                }
                Divider()
                runStatusPanel
            }
            .navigationSplitViewColumnWidth(min: 190, ideal: 210)
        } detail: {
            detailView
        }
        .toolbar {
            ToolbarItem {
                Button(model.japanese ? "EN" : "JA") { model.japanese.toggle() }
                    .help(model.japanese ? "英語表示に切り替え" : "日本語表示に切り替え")
            }
            ToolbarItem {
                Button("再読み込み", systemImage: "arrow.clockwise") { model.reload() }
            }
        }
    }

    @ViewBuilder private var detailView: some View {
        if model.save == nil {
            ContentUnavailableView(
                model.loadError ?? (model.japanese ? "セーブデータなし" : "No save data"),
                systemImage: "doc.questionmark")
        } else if !model.isAssetConfigured && selection != .settings {
            SetupGateView()
        } else {
            switch selection {
            case .deck: DeckOverviewView()
            case .character: CharacterOverviewView()
            case .encounters: EncounterOverviewView()
            case .hpHistory: HpHistoryView()
            case .settings: SettingsView()
            }
        }
    }

    /// サイドバー下部の常時表示パネル（ラン情報＋ポーション報酬確率 = Windows 版サイドパネル相当）。
    @ViewBuilder private var runStatusPanel: some View {
        VStack(alignment: .leading, spacing: 4) {
            if let save = model.save, let player = save.players.first {
                Text(model.currentCharacterId ?? "?")
                    .font(.caption.bold())
                Text("HP \(player.currentHp)/\(player.maxHp)   \(player.gold) G")
                    .font(.caption)
                potionOddsText
            } else {
                Text(model.japanese ? "ランなし" : "No run")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(10)
    }

    @ViewBuilder private var potionOddsText: some View {
        if let odds = model.potionOdds {
            if odds.forcedByRelic {
                Text(model.japanese
                     ? "ポーション報酬\n確定100%（白き獣の像）"
                     : "Potion reward\n100% (White Beast Statue)")
                    .font(.caption)
            } else {
                Text(model.japanese
                     ? "ポーション報酬\n通常 \(Self.pct(odds.normal)) / エリート \(Self.pct(odds.elite))"
                     : "Potion reward\nNormal \(Self.pct(odds.normal)) / Elite \(Self.pct(odds.elite))")
                    .font(.caption)
            }
        }
    }

    // ±10% 刻み＋エリート +12.5% なので最大小数第1位。整数なら小数点は付けない。
    static func pct(_ v: Double) -> String {
        let p = (v * 1000).rounded() / 10
        return p == p.rounded() ? String(format: "%.0f%%", p) : String(format: "%.1f%%", p)
    }
}

struct PlaceholderView: View {
    let text: String
    var body: some View {
        ContentUnavailableView(text, systemImage: "hammer")
    }
}

/// アセット未セットアップ時のゲート画面。開発リポジトリ＋dotnet 検出時はワンクリック実行できる。
struct SetupGateView: View {
    @Environment(AppModel.self) private var model
    @State private var runner = SetupRunner()

    var body: some View {
        VStack(spacing: 16) {
            Image(systemName: "shippingbox")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)
            Text(model.japanese ? "ゲームアセットが未抽出です" : "Game assets not extracted")
                .font(.title2)
            Text(model.japanese
                 ? "ゲームの .pck から画像・テキストを抽出します（初回のみ・数分かかります）。"
                 : "Extracts images and text from the game's .pck (first run only, takes a few minutes).")

            if runner.canRun {
                Button {
                    runner.run { model.reload() }
                } label: {
                    if runner.isRunning {
                        Label(model.japanese ? "抽出中…" : "Extracting…",
                              systemImage: "gearshape.arrow.trianglehead.2.clockwise.rotate.90")
                    } else {
                        Label(model.japanese ? "抽出を実行" : "Run extraction",
                              systemImage: "play.fill")
                    }
                }
                .disabled(runner.isRunning)
                if let code = runner.lastExitCode, code != 0 {
                    Text(model.japanese ? "抽出に失敗しました（exit \(code)）" : "Extraction failed (exit \(code))")
                        .foregroundStyle(.red)
                }
                if !runner.output.isEmpty {
                    ScrollView {
                        Text(runner.output.suffix(12).joined(separator: "\n"))
                            .font(.system(.caption, design: .monospaced))
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }
                    .frame(maxWidth: 560, maxHeight: 160)
                    .padding(8)
                    .background(RoundedRectangle(cornerRadius: 6).fill(Color.gray.opacity(0.12)))
                }
            } else {
                Text(model.japanese
                     ? "ターミナルでリポジトリのルートから以下を実行してください:"
                     : "Run this from the repository root in Terminal:")
                Text(verbatim: "dotnet run --project ctex-to-png -- setup")
                    .font(.system(.body, design: .monospaced))
                    .textSelection(.enabled)
                    .padding(8)
                    .background(RoundedRectangle(cornerRadius: 6).fill(Color.gray.opacity(0.15)))
            }

            Button(model.japanese ? "再チェック" : "Re-check") { model.reload() }
        }
        .padding()
    }
}
