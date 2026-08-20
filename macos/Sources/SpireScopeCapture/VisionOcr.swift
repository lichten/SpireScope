import CoreGraphics
import Foundation
import Vision

/// Vision による OCR（Windows.Media.Ocr の置き換え）。エンシェントレリック名の読み取り専用。
/// 入力は Otsu 二値化済みの拡大クロップなので言語補正は切る（生のグリフを読ませる）。
enum VisionOcr {
    /// 画像内のテキスト行を返す（失敗・無検出は空配列）。
    static func recognizeLines(_ image: CGImage) -> [String] {
        let request = VNRecognizeTextRequest()
        request.recognitionLevel = .accurate
        request.recognitionLanguages = ["ja-JP", "en-US"]
        request.usesLanguageCorrection = false

        let handler = VNImageRequestHandler(cgImage: image, options: [:])
        do {
            try handler.perform([request])
        } catch {
            return []
        }
        return (request.results ?? []).compactMap { $0.topCandidates(1).first?.string }
    }
}
