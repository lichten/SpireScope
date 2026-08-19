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
        .executableTarget(
            name: "SpireScopeApp",
            dependencies: ["SpireScopeCore"],
            resources: [.copy("Resources/AppIcon.icns")]
        ),
        .testTarget(
            name: "SpireScopeCoreTests",
            dependencies: ["SpireScopeCore"],
            resources: [.copy("Fixtures")]
        ),
    ]
)
