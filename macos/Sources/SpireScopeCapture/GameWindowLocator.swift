import CoreGraphics
import Foundation
import ScreenCaptureKit

/// 画面共有可能なウィンドウ 1 件のスナップショット。SCWindow は不変スナップショットなので
/// @unchecked Sendable で包んで actor 間を渡す。
public struct GameWindowCandidate: @unchecked Sendable, Identifiable {
    public let window: SCWindow

    public var id: CGWindowID { window.windowID }
    public var title: String { window.title ?? "" }
    public var appName: String { window.owningApplication?.applicationName ?? "" }
    public var bundleID: String { window.owningApplication?.bundleIdentifier ?? "" }
    public var frame: CGRect { window.frame }

    /// STS2 のウィンドウか。Mac 版アプリは SlayTheSpire2.app（C# GameWindowLocator の
    /// プロセス名ヒントと同系）。空白・大文字小文字の揺れを正規化して部分一致で判定する。
    public var isGameWindow: Bool {
        func norm(_ s: String) -> String {
            s.lowercased().replacingOccurrences(of: " ", with: "")
        }
        let needle = "slaythespire"
        return norm(appName).contains(needle) || norm(title).contains(needle)
            || norm(bundleID).contains(needle)
    }
}

/// ゲームウィンドウの探索（C# GameWindowLocator の置き換え）。
public enum GameWindowLocator {
    /// 画面上の通常ウィンドウ一覧（診断ビューの候補一覧用）。
    public static func listWindows() async throws -> [GameWindowCandidate] {
        let content = try await SCShareableContent.excludingDesktopWindows(
            false, onScreenWindowsOnly: true)
        return content.windows
            .filter { $0.frame.width >= 200 && $0.frame.height >= 150 }
            .map { GameWindowCandidate(window: $0) }
    }

    /// STS2 のウィンドウを探す。見つからなければ nil。
    public static func findGameWindow() async throws -> GameWindowCandidate? {
        try await listWindows().first { $0.isGameWindow }
    }
}

/// ScreenCaptureKit による 1 フレームキャプチャ（C# WgcFrameSource の置き換え）。
/// 800ms ポーリングには SCStream の常設セッションではなく SCScreenshotManager の
/// ワンショット API（macOS 14+）で足りる。
public enum ScreenCapturer {
    /// 画面収録（TCC）権限があるか。
    public static var hasPermission: Bool { CGPreflightScreenCaptureAccess() }

    /// 権限をリクエストする（初回はシステムダイアログ。以後はシステム設定へ誘導が必要）。
    @discardableResult
    public static func requestPermission() -> Bool { CGRequestScreenCaptureAccess() }

    /// 対象ウィンドウの現在の内容を物理ピクセル解像度でキャプチャする。
    public static func captureFrame(of candidate: GameWindowCandidate) async throws -> Frame {
        let filter = SCContentFilter(desktopIndependentWindow: candidate.window)
        let config = SCStreamConfiguration()
        // contentRect はポイント、キャプチャは物理ピクセルで行う（Retina で 2 倍）。
        let scale = CGFloat(filter.pointPixelScale)
        config.width = Int(filter.contentRect.width * scale)
        config.height = Int(filter.contentRect.height * scale)
        config.showsCursor = false
        config.captureResolution = .best
        let image = try await SCScreenshotManager.captureImage(
            contentFilter: filter, configuration: config)
        return Frame(image: image)
    }
}
