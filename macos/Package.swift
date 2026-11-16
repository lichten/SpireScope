// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "SpireScopeMac",
    platforms: [.macOS(.v14)],
    targets: [
        // データ層（UI 非依存・テスト対象）。リソース JSON は Scripts/sync-resources.sh が
        // Resources/data/ へコピーしたものを git 管理し、ディレクトリ構造ごとバンドルする。
        .target(
            name: "SpireScopeCore",
            resources: [.copy("Resources/data")]
        ),
        // ライブキャプチャの認識層（StS2Capture.Core の移植）。CoreGraphics/Vision に依存するが
        // UI には依存しない。ScreenCaptureKit のフレーム供給は FrameSource プロトコル背後に置き、
        // テスト・CLI は PNG 供給源で動かす。
        .target(
            name: "SpireScopeCapture",
            dependencies: ["SpireScopeCore"]
        ),
        .executableTarget(
            name: "SpireScopeApp",
            dependencies: ["SpireScopeCore", "SpireScopeCapture"],
            resources: [.copy("Resources/AppIcon.icns")]
        ),
        // 保存 PNG に対する較正・検証 CLI（C# capture-verify 相当。TCC 権限不要）。
        .executableTarget(
            name: "capture-verify",
            dependencies: ["SpireScopeCore", "SpireScopeCapture"],
            path: "Sources/CaptureVerifyCLI"
        ),
        .testTarget(
            name: "SpireScopeCoreTests",
            dependencies: ["SpireScopeCore"],
            resources: [.copy("Fixtures")]
        ),
        .testTarget(
            name: "SpireScopeCaptureTests",
            dependencies: ["SpireScopeCapture"],
            resources: [.copy("Fixtures")]
        ),
    ]
)
