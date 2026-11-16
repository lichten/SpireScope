import AppKit

/// ファイル URL → NSImage のキャッシュ（Windows 版 DeckOverviewForm._imageCache に相当）。
@MainActor
enum ImageCache {
    private static let cache = NSCache<NSString, NSImage>()

    static func image(at url: URL?) -> NSImage? {
        guard let url else { return nil }
        let key = url.path as NSString
        if let cached = cache.object(forKey: key) { return cached }
        guard let image = NSImage(contentsOf: url) else { return nil }
        cache.setObject(image, forKey: key)
        return image
    }
}
