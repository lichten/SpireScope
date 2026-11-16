import SwiftUI
import SpireScopeCapture
import SpireScopeCore

/// ライブキャプチャタブ（Windows 版 CaptureResultView + CaptureDiagnosticsForm の SwiftUI 版）。
/// 権限ゲート → 操作列 → 検出結果（タイル＋ティアバッジ）→ 診断ディスクロージャの構成。
struct CaptureView: View {
    @Environment(AppModel.self) private var model

    var body: some View {
        let capture = model.capture
        ScrollView {
            VStack(alignment: .leading, spacing: 12) {
                if !capture.hasPermission {
                    permissionGate
                } else {
                    controls
                    Text(capture.statusText.isEmpty
                         ? (model.japanese ? "「自動監視」を有効にするか手動キャプチャしてください"
                                           : "Enable auto capture or capture once")
                         : capture.statusText)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    if capture.potionsMissing {
                        Label(model.japanese
                              ? "ポーション画像が未抽出です。認識精度のためセットアップ（dotnet run --project ctex-to-png -- setup）を再実行してください"
                              : "Potion images missing — re-run setup (dotnet run --project ctex-to-png -- setup)",
                              systemImage: "exclamationmark.triangle")
                            .font(.caption)
                            .foregroundStyle(.orange)
                    }
                    resultSection
                    diagnostics
                }
            }
            .padding()
            .frame(maxWidth: .infinity, alignment: .leading)
        }
        .onAppear {
            capture.refreshPermission()
            capture.warmupIfNeeded()
        }
    }

    // ---- 権限ゲート ----

    @ViewBuilder private var permissionGate: some View {
        VStack(spacing: 16) {
            Image(systemName: "camera.viewfinder")
                .font(.system(size: 48))
                .foregroundStyle(.secondary)
            Text(model.japanese ? "画面収録の権限が必要です" : "Screen Recording permission required")
                .font(.title2)
            Text(model.japanese
                 ? "ゲーム画面のキャプチャには macOS の画面収録権限が必要です。\n許可後、システム設定でオンになっていることを確認してください。\n（swift run ではなく out/SpireScope.app から起動した場合のみアプリに権限が付きます）"
                 : "Capturing the game window requires the Screen Recording permission.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
            HStack {
                Button(model.japanese ? "権限をリクエスト" : "Request permission") {
                    model.capture.requestPermission()
                }
                Button(model.japanese ? "システム設定を開く" : "Open System Settings") {
                    if let url = URL(string:
                        "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture") {
                        NSWorkspace.shared.open(url)
                    }
                }
                Button(model.japanese ? "再チェック" : "Re-check") {
                    model.capture.refreshPermission()
                }
            }
        }
        .frame(maxWidth: .infinity)
        .padding(.top, 60)
    }

    // ---- 操作列 ----

    @ViewBuilder private var controls: some View {
        let capture = model.capture
        HStack(spacing: 12) {
            Toggle(model.japanese ? "自動監視" : "Auto capture", isOn: Binding(
                get: { capture.isRunning },
                set: { $0 ? capture.start() : capture.stop() }))
                .toggleStyle(.switch)
            Button(model.japanese ? "1回キャプチャ" : "Capture once") {
                Task { await capture.captureOnce() }
            }
            Button(model.japanese ? "フレームを保存" : "Save frame") {
                if let url = capture.saveFrame() {
                    NSWorkspace.shared.activateFileViewerSelecting([url])
                }
            }
            .disabled(capture.latestFrame == nil)
            if capture.isWarmingUp {
                ProgressView()
                    .controlSize(.small)
                Text(model.japanese ? "照合DB構築中…" : "Building DB…")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
    }

    // ---- 検出結果 ----

    @ViewBuilder private var resultSection: some View {
        let capture = model.capture
        if let result = capture.displayResult {
            VStack(alignment: .leading, spacing: 8) {
                HStack(spacing: 6) {
                    Text(result.screen.label(japanese: model.japanese))
                        .font(.headline)
                    if capture.isStale {
                        Text(model.japanese ? "（前回の検出）" : "(previous)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }

                if !result.cards.isEmpty {
                    LazyVGrid(columns: [GridItem(.adaptive(minimum: CardTileView.tileWidth),
                                                 spacing: 8, alignment: .top)],
                              alignment: .leading, spacing: 10) {
                        ForEach(Array(result.cards.enumerated()), id: \.offset) { _, card in
                            CaptureCardTile(card: card)
                        }
                    }
                }

                let accepted = result.shopItems.filter { $0.accepted || !$0.candidates.isEmpty }
                if !accepted.isEmpty {
                    LazyVGrid(columns: [GridItem(.adaptive(minimum: 90),
                                                 spacing: 8, alignment: .top)],
                              alignment: .leading, spacing: 10) {
                        ForEach(Array(accepted.enumerated()), id: \.offset) { _, item in
                            CaptureItemTile(item: item)
                        }
                    }
                }
            }
            .padding(10)
            .background(RoundedRectangle(cornerRadius: 8).fill(Color.gray.opacity(0.08)))
            .opacity(capture.isStale ? 0.6 : 1.0)
        } else {
            ContentUnavailableView(
                model.japanese ? "検出待ち" : "Waiting for detections",
                systemImage: "rectangle.dashed.badge.record")
        }
    }

    // ---- 診断 ----

    @ViewBuilder private var diagnostics: some View {
        let capture = model.capture
        DisclosureGroup(model.japanese ? "キャプチャ診断" : "Diagnostics") {
            VStack(alignment: .leading, spacing: 8) {
                Text(String(format: model.japanese ? "認識時間: %.2f 秒" : "Recognition: %.2fs",
                            capture.lastElapsed))
                    .font(.caption)

                if let frame = capture.latestFrame {
                    CapturePreview(frame: frame, result: capture.rawResult)
                        .frame(maxWidth: 480)
                }

                if let raw = capture.rawResult {
                    rawList(raw)
                }

                if !capture.windowCandidates.isEmpty {
                    Text(model.japanese ? "ウィンドウ候補:" : "Window candidates:")
                        .font(.caption.bold())
                    ForEach(capture.windowCandidates) { w in
                        Text("\(w.isGameWindow ? "→" : "・") \(w.appName) — \(w.title)  (\(Int(w.frame.width))×\(Int(w.frame.height)))  \(w.bundleID)")
                            .font(.system(.caption2, design: .monospaced))
                            .foregroundStyle(w.isGameWindow ? .primary : .secondary)
                    }
                }
            }
            .padding(.top, 6)
        }
        .font(.callout)
    }

    @ViewBuilder private func rawList(_ raw: RecognitionResult) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            ForEach(Array(raw.cards.enumerated()), id: \.offset) { _, c in
                Text(String(format: "card  %@ / %@  conf=%.2f",
                            c.cardId, model.db.name(c.cardId, japanese: true), c.confidence))
                    .font(.system(.caption2, design: .monospaced))
            }
            ForEach(Array(raw.shopItems.enumerated()), id: \.offset) { _, item in
                let cands = item.candidates.map {
                    String(format: "%@ d=%.2f", $0.id, $0.distance)
                }.joined(separator: ", ")
                Text("\(item.kind.rawValue)  \(item.accepted ? "OK" : "--")  \(cands.isEmpty ? "(none)" : cands)")
                    .font(.system(.caption2, design: .monospaced))
                    .foregroundStyle(item.accepted ? .primary : .secondary)
            }
            ForEach(Array(raw.ocrTexts.enumerated()), id: \.offset) { i, t in
                if !t.isEmpty {
                    Text("ocr[\(i)]  \(t)")
                        .font(.system(.caption2, design: .monospaced))
                }
            }
        }
    }
}

/// キャプチャプレビュー（スロット矩形オーバーレイ付き。Windows 版 MakePreview 相当）。
struct CapturePreview: View {
    let frame: Frame
    let result: RecognitionResult?

    var body: some View {
        GeometryReader { geo in
            let scale = min(geo.size.width / CGFloat(frame.width),
                            geo.size.height / CGFloat(frame.height))
            ZStack(alignment: .topLeading) {
                Image(nsImage: NSImage(cgImage: frame.image, size: .zero))
                    .resizable()
                    .frame(width: CGFloat(frame.width) * scale,
                           height: CGFloat(frame.height) * scale)
                if let result {
                    Canvas { ctx, _ in
                        func draw(_ r: RectI, color: Color) {
                            let rect = CGRect(x: CGFloat(r.x) * scale, y: CGFloat(r.y) * scale,
                                              width: CGFloat(r.width) * scale,
                                              height: CGFloat(r.height) * scale)
                            ctx.stroke(Path(rect), with: .color(color), lineWidth: 1.5)
                        }
                        for box in result.cardBoxes { draw(box, color: .green) }
                        for card in result.cards { draw(card.region, color: .mint) }
                        for item in result.shopItems {
                            draw(item.region, color: item.accepted ? .green : .red)
                        }
                    }
                    .frame(width: CGFloat(frame.width) * scale,
                           height: CGFloat(frame.height) * scale)
                }
            }
        }
        .aspectRatio(CGFloat(frame.width) / CGFloat(frame.height), contentMode: .fit)
    }
}

/// 検出カードのタイル（画像＋名前＋確信度＋ティアバッジ）。クリックで外部リンク。
struct CaptureCardTile: View {
    @Environment(AppModel.self) private var model
    let card: RecognizedCard

    var body: some View {
        VStack(spacing: 2) {
            ZStack(alignment: .topTrailing) {
                if let image = ImageCache.image(at: model.assets.cardImageURL(card.cardId)) {
                    Image(nsImage: image)
                        .resizable()
                        .aspectRatio(contentMode: .fill)
                        .frame(width: CardTileView.tileWidth, height: CardTileView.imageHeight)
                        .clipShape(RoundedRectangle(cornerRadius: 6))
                        .overlay(RoundedRectangle(cornerRadius: 6)
                            .stroke(Color.gray.opacity(0.4), lineWidth: 1))
                } else {
                    RoundedRectangle(cornerRadius: 6)
                        .fill(Color.gray.opacity(0.15))
                        .frame(width: CardTileView.tileWidth, height: CardTileView.imageHeight)
                }
                TierBadge(text: model.tiers.cardTier(card.cardId))
            }
            Text(model.db.name(card.cardId, japanese: model.japanese))
                .font(.caption2)
                .lineLimit(2)
                .multilineTextAlignment(.center)
            Text(String(format: "%.0f%%", card.confidence * 100))
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
        .frame(width: CardTileView.tileWidth)
        .onTapGesture { LinkOpener.open(model.links(kind: "card", id: card.cardId)) }
        .contextMenu { LinkOpener.menu(model.links(kind: "card", id: card.cardId)) }
    }
}

/// 検出レリック／ポーションのタイル。候補が複数ある場合は +N バッジで示す。
struct CaptureItemTile: View {
    @Environment(AppModel.self) private var model
    let item: ShopItem

    private var bestId: String? { item.candidates.first?.id }
    private var kindString: String { item.kind == .relic ? "relic" : "potion" }

    var body: some View {
        if let id = bestId {
            VStack(spacing: 2) {
                ZStack(alignment: .topTrailing) {
                    ZStack {
                        RoundedRectangle(cornerRadius: 6)
                            .fill(Color.gray.opacity(0.15))
                        if let image = ImageCache.image(at: imageURL(id)) {
                            Image(nsImage: image)
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .padding(4)
                        }
                    }
                    .frame(width: 56, height: 56)
                    TierBadge(text: tier(id))
                }
                .overlay(alignment: .bottomTrailing) {
                    if item.candidates.count > 1 {
                        Text("+\(item.candidates.count - 1)")
                            .font(.caption2.bold())
                            .foregroundStyle(.white)
                            .padding(.horizontal, 4)
                            .background(Capsule().fill(Color.black.opacity(0.7)))
                            .padding(2)
                    }
                }
                Text(name(id))
                    .font(.caption2)
                    .lineLimit(2)
                    .multilineTextAlignment(.center)
            }
            .frame(width: 90)
            .opacity(item.accepted ? 1.0 : 0.5)
            .help(item.candidates.map { "\(name($0.id)) (\(String(format: "%.2f", $0.distance)))" }
                .joined(separator: "\n"))
            .onTapGesture { LinkOpener.open(model.links(kind: kindString, id: id)) }
            .contextMenu { LinkOpener.menu(model.links(kind: kindString, id: id)) }
        }
    }

    private func imageURL(_ id: String) -> URL? {
        item.kind == .relic ? model.assets.relicImageURL(id) : model.assets.potionImageURL(id)
    }

    private func name(_ id: String) -> String {
        item.kind == .relic
            ? model.db.relicTitle(id, japanese: model.japanese)
            : model.db.potionTitle(id, japanese: model.japanese)
    }

    private func tier(_ id: String) -> String {
        item.kind == .relic ? model.tiers.relicTier(id) : model.tiers.potionTier(id)
    }
}

/// ティアバッジ（例 "S (92)"）。Windows 版 CaptureResultView のバッジ配色を踏襲。
struct TierBadge: View {
    let text: String

    var body: some View {
        if !text.isEmpty {
            Text(text)
                .font(.caption2.bold())
                .foregroundStyle(.white)
                .padding(.horizontal, 4)
                .padding(.vertical, 1)
                .background(RoundedRectangle(cornerRadius: 4).fill(color.opacity(0.82)))
                .padding(2)
        }
    }

    private var color: Color {
        switch text.first {
        case "S": return Color(red: 190 / 255, green: 120 / 255, blue: 20 / 255)
        case "A": return Color(red: 60 / 255, green: 130 / 255, blue: 50 / 255)
        case "B": return Color(red: 50 / 255, green: 100 / 255, blue: 170 / 255)
        case "C": return Color(red: 110 / 255, green: 110 / 255, blue: 110 / 255)
        default: return Color(red: 140 / 255, green: 80 / 255, blue: 60 / 255)
        }
    }
}
