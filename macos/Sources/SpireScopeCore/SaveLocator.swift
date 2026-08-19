import Foundation

/// StS2Shared/Services/SaveDataService.cs の Swift 版。
/// macOS のセーブは `~/Library/Application Support/SlayTheSpire2/steam/{steamId}/profile1/saves/current_run.save`
/// （Windows の %AppData% 配下と同一構造。実機確認済み）。
public enum SaveLocator {
    /// 既定のセーブファイルパス。steam ID ディレクトリは最初に見つかったものを使う。無ければ nil。
    public static func defaultSavePath() -> URL? {
        let baseDir = FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Library", "Application Support", "SlayTheSpire2", "steam")
        guard let steamIdDir = firstSubdirectory(of: baseDir) else { return nil }
        return steamIdDir.appending(components: "profile1", "saves", "current_run.save")
    }

    public static func load(_ url: URL) throws -> RunSaveData {
        let data = try Data(contentsOf: url)
        return try JSONDecoder().decode(RunSaveData.self, from: data)
    }

    /// "CHARACTER.DEFECT" 等の生 ID を接頭辞なし大文字（"DEFECT"）に正規化する。空なら nil。
    public static func normalizeCharacterId(_ raw: String?) -> String? {
        guard let raw, !raw.trimmingCharacters(in: .whitespaces).isEmpty else { return nil }
        let id = raw.lastIndex(of: ".").map { String(raw[raw.index(after: $0)...]) } ?? raw
        return id.isEmpty ? nil : id.uppercased()
    }

    static func firstSubdirectory(of dir: URL) -> URL? {
        let fm = FileManager.default
        guard let entries = try? fm.contentsOfDirectory(
            at: dir, includingPropertiesForKeys: [.isDirectoryKey], options: [.skipsHiddenFiles])
        else { return nil }
        return entries
            .filter { (try? $0.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true }
            .sorted { $0.lastPathComponent < $1.lastPathComponent }
            .first
    }
}
