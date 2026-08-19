import Foundation

/// バンドル内リソース（`data/v{version}/…` と手動管理 JSON）へのアクセス。
/// C# の `ResourceResolver`（StS2Shared/Services/ResourceResolver.cs）に相当し、
/// `data/v*` のうち最大バージョンを VersionKey（整数列ゼロ埋め連結の文字列比較）で選ぶ。
/// 同梱は最新版のみだが、解決ロジックは複数版が並んでも正しく動く。
public struct ResourceStore: Sendable {
    public let dataRoot: URL
    /// 最新バージョンフォルダ（例 `data/v0.111.0`）。v* が無ければ nil。
    public let versionDir: URL?
    /// 最新バージョン名（例 "v0.111.0"）。
    public let versionName: String?

    public init() throws {
        // .app 配布時は make-app.sh が data/ を Contents/Resources へ直接コピーする
        // （SwiftPM の Bundle.module アクセサは .app ルート直下しか探さないため）。
        // swift run / swift test では Bundle.module（SwiftPM リソースバンドル）を使う。
        if Bundle.main.url(forResource: "data", withExtension: nil) != nil {
            try self.init(bundle: .main)
        } else {
            try self.init(bundle: .module)
        }
    }

    public init(bundle: Bundle) throws {
        guard let root = bundle.url(forResource: "data", withExtension: nil) else {
            throw ResourceError.dataRootMissing
        }
        dataRoot = root
        let fm = FileManager.default
        let versions = ((try? fm.contentsOfDirectory(at: root, includingPropertiesForKeys: nil)) ?? [])
            .filter { $0.lastPathComponent.hasPrefix("v") && $0.hasDirectoryPath }
        let latest = versions.max { Self.versionKey($0.lastPathComponent) < Self.versionKey($1.lastPathComponent) }
        versionDir = latest
        versionName = latest?.lastPathComponent
    }

    /// バージョンフォルダ内の JSON を読む（例 `openJSON("card_costs.json")`、
    /// `openJSON("localization/jpn/relics.json")`）。無ければ nil。
    public func versionedData(_ relativePath: String) -> Data? {
        guard let versionDir else { return nil }
        return try? Data(contentsOf: versionDir.appending(path: relativePath))
    }

    /// バージョン非依存（手動管理）の JSON を読む（例 `monster_move_patterns.json`）。
    public func unversionedData(_ fileName: String) -> Data? {
        try? Data(contentsOf: dataRoot.appending(path: fileName))
    }

    public func versionedJSON<T: Decodable>(_ relativePath: String, as type: T.Type) throws -> T {
        guard let data = versionedData(relativePath) else {
            throw ResourceError.fileMissing(relativePath)
        }
        return try JSONDecoder().decode(type, from: data)
    }

    /// バージョン名（"v0.111.0" 等）内の整数列をゼロ埋め連結し、文字列比較で数値順になるキーを作る。
    /// C# `ResourceResolver.VersionKey` と同方針（セグメント数非依存）。
    static func versionKey(_ token: String) -> String {
        token.matches(of: /\d+/)
            .map { String(repeating: "0", count: max(0, 8 - $0.output.count)) + $0.output }
            .joined(separator: ".")
    }
}

public enum ResourceError: Error, CustomStringConvertible {
    case dataRootMissing
    case fileMissing(String)

    public var description: String {
        switch self {
        case .dataRootMissing:
            return "バンドル内リソース data/ が見つかりません（Scripts/sync-resources.sh 未実行の可能性）"
        case .fileMissing(let path):
            return "リソースが見つかりません: \(path)"
        }
    }
}
