import Foundation

/// セーブファイルの自動リロード監視。
/// Windows 版（Form1 の FileSystemWatcher + 500ms デバウンス）に相当する。
/// Godot がファイルを rename で置き換える可能性があるため、ファイル本体ではなく
/// **親ディレクトリ**を DispatchSource（kqueue）で監視する。
public final class SaveWatcher: @unchecked Sendable {
    private let queue = DispatchQueue(label: "SpireScope.SaveWatcher")
    private var source: DispatchSourceFileSystemObject?
    private var fd: CInt = -1
    private var pending: DispatchWorkItem?
    private let debounce: TimeInterval
    private let onChange: @Sendable () -> Void

    /// - Parameters:
    ///   - directory: 監視するディレクトリ（セーブファイルの親）。
    ///   - debounce: 変更イベントのデバウンス秒（既定 0.5 = Windows 版と同じ）。
    ///   - onChange: デバウンス後に呼ばれるコールバック（監視キュー上）。
    public init?(directory: URL, debounce: TimeInterval = 0.5,
                 onChange: @escaping @Sendable () -> Void) {
        self.debounce = debounce
        self.onChange = onChange
        fd = open(directory.path, O_EVTONLY)
        guard fd >= 0 else { return nil }
        let source = DispatchSource.makeFileSystemObjectSource(
            fileDescriptor: fd,
            eventMask: [.write, .extend, .rename, .delete, .attrib],
            queue: queue)
        source.setEventHandler { [weak self] in self?.scheduleReload() }
        source.setCancelHandler { [fd] in close(fd) }
        source.resume()
        self.source = source
    }

    private func scheduleReload() {
        pending?.cancel()
        let item = DispatchWorkItem { [onChange] in onChange() }
        pending = item
        queue.asyncAfter(deadline: .now() + debounce, execute: item)
    }

    public func cancel() {
        source?.cancel()
        source = nil
    }

    deinit { cancel() }
}
