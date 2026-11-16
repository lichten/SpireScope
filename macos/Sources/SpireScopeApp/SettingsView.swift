import SwiftUI
import SpireScopeCore

/// 設定（セーブパス・言語・URL テンプレート・アセット状態）。
struct SettingsView: View {
    @Environment(AppModel.self) private var model

    var body: some View {
        @Bindable var model = model
        Form {
            Section(model.japanese ? "表示" : "Display") {
                Picker(model.japanese ? "言語" : "Language", selection: $model.japanese) {
                    Text("日本語").tag(true)
                    Text("English").tag(false)
                }
                .pickerStyle(.segmented)
                .frame(maxWidth: 260)
            }

            Section(model.japanese ? "セーブファイル" : "Save file") {
                LabeledContent(model.japanese ? "現在のパス" : "Current path") {
                    Text(model.savePath?.path ?? "-")
                        .textSelection(.enabled)
                        .foregroundStyle(.secondary)
                        .lineLimit(2)
                }
                HStack {
                    Button(model.japanese ? "手動で選択…" : "Choose…") { chooseSaveFile() }
                    if model.savePathOverride != nil {
                        Button(model.japanese ? "自動検出に戻す" : "Reset to auto") {
                            model.savePathOverride = nil
                        }
                    }
                }
            }

            Section(model.japanese ? "ゲームアセット" : "Game assets") {
                LabeledContent(model.japanese ? "状態" : "Status") {
                    if let version = model.assets.installedVersion {
                        Text(model.japanese ? "抽出済み（\(version)）" : "Extracted (\(version))")
                    } else {
                        Text(model.japanese ? "未セットアップ" : "Not set up")
                            .foregroundStyle(.orange)
                    }
                }
                Text(model.japanese
                     ? "ゲーム更新後はターミナルで再抽出してください: dotnet run --project ctex-to-png -- setup"
                     : "After a game update, re-extract with: dotnet run --project ctex-to-png -- setup")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Section(model.japanese ? "リンク URL テンプレート" : "Link URL templates") {
                UrlTemplateEditor()
                Text(model.japanese
                     ? "トークン: {id} {en} {jp} {idraw} {idrawlower} {cardclass} / 種別: card, relic, potion, event, encounter, any"
                     : "Tokens: {id} {en} {jp} {idraw} {idrawlower} {cardclass} / Types: card, relic, potion, event, encounter, any")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .formStyle(.grouped)
        .navigationTitle(model.japanese ? "設定" : "Settings")
    }

    private func chooseSaveFile() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        panel.directoryURL = model.savePath?.deletingLastPathComponent()
        if panel.runModal() == .OK, let url = panel.url {
            model.savePathOverride = url
        }
    }
}

/// URL テンプレートの編集リスト（Windows 版 UrlTemplateSettingsForm 相当）。
struct UrlTemplateEditor: View {
    @Environment(AppModel.self) private var model

    var body: some View {
        @Bindable var model = model
        VStack(alignment: .leading, spacing: 6) {
            ForEach(model.urlTemplates.indices, id: \.self) { i in
                HStack {
                    TextField(model.japanese ? "ラベル" : "Label",
                              text: $model.urlTemplates[i].label)
                        .frame(width: 110)
                    TextField(model.japanese ? "種別" : "Type",
                              text: $model.urlTemplates[i].type)
                        .frame(width: 90)
                    TextField("URL", text: $model.urlTemplates[i].template)
                    Button {
                        model.urlTemplates.remove(at: i)
                    } label: {
                        Image(systemName: "minus.circle")
                    }
                    .buttonStyle(.plain)
                }
            }
            Button {
                model.urlTemplates.append(UrlTemplate(label: "", type: "any", template: ""))
            } label: {
                Label(model.japanese ? "追加" : "Add", systemImage: "plus.circle")
            }
            .buttonStyle(.plain)
        }
    }
}
