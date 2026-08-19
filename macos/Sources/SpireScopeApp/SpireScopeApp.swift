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
    }

    var body: some Scene {
        WindowGroup("SpireScope") {
            ContentView()
                .environment(model)
        }
    }
}
