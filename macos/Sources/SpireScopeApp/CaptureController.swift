import AppKit
import Foundation
import Observation
import SpireScopeCapture
import SpireScopeCore
import UniformTypeIdentifiers

/// ライブキャプチャのオーケストレーション（Windows 版 CaptureLoop + Form1 のキャプチャ部に相当）。
/// 800ms のポーリングループを所有し、ScreenCaptureKit で 1 フレーム取得 → RecognitionEngine（actor）で
/// 認識 → MainActor で結果を公開する。検出対象外の画面に切り替わっても最後の検出を保持し
/// 「（前回の検出）」注記を付ける（CaptureResultView の挙動を踏襲）。
@Observable @MainActor
final class CaptureController {
    @ObservationIgnored private let engine: RecognitionEngine
    /// 現在ランのキャラ ID の供給元（AppModel.currentCharacterId。照合プールの絞り込みに使う）。
    @ObservationIgnored var characterProvider: (() -> String?)?

    @ObservationIgnored private var loopTask: Task<Void, Never>?
    @ObservationIgnored private var lastSignature = ""
    @ObservationIgnored private var warmupStarted = false

    private(set) var isRunning = false
    private(set) var hasPermission = ScreenCapturer.hasPermission
    private(set) var isWarmingUp = false
    private(set) var statusText = ""
    /// ゲームウィンドウ未検出時の候補一覧（診断用。照合ルールの較正に使う）。
    private(set) var windowCandidates: [GameWindowCandidate] = []
    private(set) var latestFrame: Frame?
    private(set) var clientRect: RectI?
    /// 直近フレームの生の認識結果（診断ビュー用。毎フレーム更新）。
    private(set) var rawResult: RecognitionResult?
    /// 表示用の検出結果（検出があったフレームのみ差し替え、以後保持）。
    private(set) var displayResult: RecognitionResult?
    /// displayResult が「前回の検出」（現在の画面は検出対象外）かどうか。
    private(set) var isStale = false
    private(set) var lastElapsed: TimeInterval = 0
    /// potions_png 未抽出（旧セットアップ）の警告表示用。
    private(set) var potionsMissing = false

    init(store: ResourceStore, db: CardDatabase) {
        engine = RecognitionEngine(store: store, db: db)
    }

    /// キャプチャタブ初回表示時に照合 DB を裏で事前構築する（コールドビルドは十数秒かかる）。
    func warmupIfNeeded() {
        guard !warmupStarted else { return }
        warmupStarted = true
        isWarmingUp = true
        let engine = engine
        Task.detached(priority: .utility) {
            await engine.warmup()
            let missing = await engine.potionsMissing
            await MainActor.run { [weak self] in
                self?.isWarmingUp = false
                self?.potionsMissing = missing
            }
        }
    }

    func refreshPermission() {
        hasPermission = ScreenCapturer.hasPermission
    }

    func requestPermission() {
        ScreenCapturer.requestPermission()
        refreshPermission()
    }

    func start() {
        guard loopTask == nil else { return }
        isRunning = true
        loopTask = Task { [weak self] in
            while !Task.isCancelled {
                await self?.tick()
                try? await Task.sleep(for: .milliseconds(RecognitionConstants.captureIntervalMs))
            }
        }
    }

    func stop() {
        loopTask?.cancel()
        loopTask = nil
        isRunning = false
    }

    /// 手動 1 回キャプチャ。
    func captureOnce() async {
        await tick()
    }

    private func tick() async {
        refreshPermission()
        guard hasPermission else {
            statusText = "画面収録の権限がありません（システム設定 → プライバシーとセキュリティ）"
            return
        }

        // ウィンドウは毎回引き直す（クローズ・リサイズ・フルスクリーン切替に追従）。
        let candidates: [GameWindowCandidate]
        do {
            candidates = try await GameWindowLocator.listWindows()
        } catch {
            statusText = "ウィンドウ一覧を取得できません: \(error.localizedDescription)"
            return
        }
        windowCandidates = candidates
        guard let game = candidates.first(where: { $0.isGameWindow }) else {
            statusText = "Slay the Spire 2 のウィンドウが見つかりません"
            return
        }

        let frame: Frame
        do {
            frame = try await ScreenCapturer.captureFrame(of: game)
        } catch {
            statusText = "キャプチャ失敗: \(error.localizedDescription)"
            return
        }

        let started = Date()
        let character = characterProvider?()
        let (result, client) = await engine.recognize(frame: frame, currentCharacterId: character)
        lastElapsed = Date().timeIntervalSince(started)
        potionsMissing = await engine.potionsMissing

        latestFrame = frame
        clientRect = client
        rawResult = result
        statusText = Self.status(for: result)
            + "  [frame \(frame.width)x\(frame.height)"
            + " client \(client.x),\(client.y) \(client.width)x\(client.height)]"

        // 検出があったフレームのみ表示を差し替える（Unknown では前回の検出を保持）。
        if result.hasDetections {
            if result.signature != lastSignature {
                lastSignature = result.signature
                displayResult = result
            }
            isStale = false
        } else {
            isStale = displayResult != nil
        }
    }

    private static func status(for result: RecognitionResult) -> String {
        switch result.screen {
        case .cardSelect: return "カード選択画面: \(result.cards.count) 枚検出"
        case .shop:
            let n = result.shopItems.count(where: \.accepted)
            return "ショップ画面: アイテム \(n) 件・カード \(result.cards.count) 枚"
        case .ancientSelect:
            return "エンシェントレリック選択: \(result.shopItems.count(where: \.accepted)) 件"
        case .unknown: return "検出対象の画面ではありません"
        }
    }

    /// 現在のフレームを PNG で ~/Downloads に保存する（較正・フィクスチャ採取用）。
    func saveFrame() -> URL? {
        guard let frame = latestFrame else { return nil }
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyyMMdd_HHmmss"
        let name = "sts2_frame_\(formatter.string(from: Date())).png"
        let url = FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Downloads", name)
        guard let dest = CGImageDestinationCreateWithURL(
            url as CFURL, UTType.png.identifier as CFString, 1, nil) else { return nil }
        CGImageDestinationAddImage(dest, frame.image, nil)
        return CGImageDestinationFinalize(dest) ? url : nil
    }
}
