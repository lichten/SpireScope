import AppKit
import SwiftUI
import SpireScopeCore

@main
struct SpireScopeApp: App {
    @State private var model = AppModel()

    init() {
        // SwiftPM の executable 起動（swift run）でも通常アプリとして前面に出す。
        NSApplication.shared.setActivationPolicy(.regular)
        NSApplication.shared.activate(ignoringOtherApps: true)
        // バンドル外実行（swift run / Xcode）では Info.plist が効かないため実行時に設定する。
        if let icon = Self.findAppIcon() {
            NSApplication.shared.applicationIconImage = icon
        }
    }

    /// AppIcon.icns を .app（Contents/Resources）→ SwiftPM リソースバンドル（実行体の隣）の順で探す。
    /// Bundle.module は見つからないと fatalError するアクセサのため使わない（.app 配布時に
    /// 開発マシンのビルドパスへフォールバックしようとして落ちる恐れがある）。
    private static func findAppIcon() -> NSImage? {
        if let url = Bundle.main.url(forResource: "AppIcon", withExtension: "icns"),
           let icon = NSImage(contentsOf: url) {
            return icon
        }
        let bundleURL = Bundle.main.bundleURL
            .appendingPathComponent("SpireScopeMac_SpireScopeApp.bundle")
        if let bundle = Bundle(url: bundleURL),
           let url = bundle.url(forResource: "AppIcon", withExtension: "icns"),
           let icon = NSImage(contentsOf: url) {
            return icon
        }
        return nil
    }

    var body: some Scene {
        WindowGroup("SpireScope") {
            ContentView()
                .environment(model)
        }
    }
}
