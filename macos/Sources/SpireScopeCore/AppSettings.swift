import Foundation

/// Mac 版アプリ設定の永続化（Windows 版 WindowSettingsService の簡易版）。
/// 保存先: ~/Library/Application Support/SpireScope/mac_settings.json
/// （Windows の window_settings.json とはファイルを分け、互いに上書きしない）
public struct AppSettings: Codable, Sendable, Equatable {
    /// セーブファイルの手動指定パス。nil なら自動検出。
    public var savePathOverride: String?
    /// 表示言語（true = 日本語）。
    public var japanese: Bool

    public init(savePathOverride: String? = nil, japanese: Bool = true) {
        self.savePathOverride = savePathOverride
        self.japanese = japanese
    }

    public static var path: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Library", "Application Support", "SpireScope", "mac_settings.json")
    }

    public static func load() -> AppSettings {
        guard let data = try? Data(contentsOf: path),
              let settings = try? JSONDecoder().decode(AppSettings.self, from: data)
        else { return AppSettings() }
        return settings
    }

    public func save() {
        do {
            try FileManager.default.createDirectory(
                at: Self.path.deletingLastPathComponent(), withIntermediateDirectories: true)
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted]
            try encoder.encode(self).write(to: Self.path)
        } catch {
            // 設定保存失敗は致命的でないため無視（次回起動が既定値に戻るだけ）
        }
    }
}
