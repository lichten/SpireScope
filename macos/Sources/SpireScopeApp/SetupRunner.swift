import Foundation
import Observation

/// アセット抽出 CLI（`dotnet run --project ctex-to-png -- setup`）の起動ヘルパ。
/// 開発リポジトリから実行しているとき（walk-up で ctex-to-png が見つかるとき）だけ
/// ワンクリック実行を提供する。見つからなければ手動実行の案内のみ。
@Observable @MainActor
final class SetupRunner {
    private(set) var isRunning = false
    private(set) var output: [String] = []
    private(set) var lastExitCode: Int32?

    /// dotnet 実行体の検出（PATH には無い前提で既知の場所を直接探す）。
    static func findDotnet() -> URL? {
        let candidates = [
            FileManager.default.homeDirectoryForCurrentUser.appending(components: ".dotnet", "dotnet"),
            URL(filePath: "/usr/local/share/dotnet/dotnet"),
            URL(filePath: "/opt/homebrew/bin/dotnet"),
            URL(filePath: "/usr/local/bin/dotnet"),
        ]
        return candidates.first { FileManager.default.isExecutableFile(atPath: $0.path) }
    }

    /// 実行体の位置から親を遡って ctex-to-png プロジェクトを含むリポジトリルートを探す。
    static func findRepoRoot() -> URL? {
        var dir = Bundle.main.bundleURL
        for _ in 0..<12 {
            let csproj = dir.appending(components: "ctex-to-png", "ctex-to-png.csproj")
            if FileManager.default.fileExists(atPath: csproj.path) { return dir }
            let parent = dir.deletingLastPathComponent()
            if parent == dir { break }
            dir = parent
        }
        return nil
    }

    var canRun: Bool { Self.findDotnet() != nil && Self.findRepoRoot() != nil }

    func run(onFinished: @escaping @MainActor () -> Void) {
        guard !isRunning,
              let dotnet = Self.findDotnet(),
              let repoRoot = Self.findRepoRoot() else { return }
        isRunning = true
        output = []
        lastExitCode = nil

        let process = Process()
        process.executableURL = dotnet
        process.arguments = ["run", "--project", "ctex-to-png", "--", "setup"]
        process.currentDirectoryURL = repoRoot
        var env = ProcessInfo.processInfo.environment
        env["DOTNET_ROOT"] = dotnet.deletingLastPathComponent().path
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        process.environment = env

        let pipe = Pipe()
        process.standardOutput = pipe
        process.standardError = pipe
        pipe.fileHandleForReading.readabilityHandler = { handle in
            let data = handle.availableData
            guard !data.isEmpty, let text = String(data: data, encoding: .utf8) else { return }
            Task { @MainActor [weak self] in
                guard let self else { return }
                for line in text.split(separator: "\n", omittingEmptySubsequences: true) {
                    self.output.append(String(line))
                }
                if self.output.count > 200 { self.output.removeFirst(self.output.count - 200) }
            }
        }
        process.terminationHandler = { proc in
            pipe.fileHandleForReading.readabilityHandler = nil
            Task { @MainActor [weak self] in
                self?.isRunning = false
                self?.lastExitCode = proc.terminationStatus
                onFinished()
            }
        }

        do {
            try process.run()
        } catch {
            isRunning = false
            output.append("起動に失敗しました: \(error.localizedDescription)")
        }
    }
}
