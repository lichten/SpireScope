import Foundation
import Testing
@testable import SpireScopeCapture

/// スロット矩形の切り捨て演算が C# 版（System.Drawing.Rectangle + (int) キャスト）と
/// 一致することを、Windows 較正解像度（クライアント 1287×720）の期待値で固定する。
@Suite struct GeometryTests {
    let client = RectI(x: 0, y: 0, width: 1287, height: 720)

    @Test func cardSelectSlotPixels() {
        let rects = RecognitionConstants.cardSelectSlots.map { $0.toPixels(client: client) }
        #expect(rects[0] == RectI(x: 330, y: 335, width: 163, height: 189))
        #expect(rects[1] == RectI(x: 562, y: 335, width: 163, height: 189))
        #expect(rects[2] == RectI(x: 795, y: 335, width: 163, height: 189))
    }

    @Test func artRegionMatchesCSharp() {
        let card = RectI(x: 330, y: 335, width: 163, height: 189)
        let art = CardGeometry.artRegionOf(card)
        // x1=330+int(163*0.04)=336, x2=493-6=487, y1=335+int(163*0.05)=343, y2=343+int(163*0.4675)=419
        #expect(art == RectI.fromLTRB(336, 343, 487, 419))
    }

    @Test func shopSlotWithClientOffset() {
        // クライアント原点 (0,45)（Windows 実測時のタイトルバー分）でのレリックスロット 0。
        let offsetClient = RectI(x: 0, y: 45, width: 1287, height: 720)
        let slot = RecognitionConstants.shopSlots[0]
        let side = max(8, Int(Double(offsetClient.height) * RecognitionConstants.shopSlotSizeFrac))
        let cx = offsetClient.x + Int(Double(offsetClient.width) * slot.cxFrac)
        let cy = offsetClient.y + Int(Double(offsetClient.height) * slot.cyFrac)
        let rect = RectI(x: cx - side / 2, y: cy - side / 2, width: side, height: side)
        #expect(side == 51)
        #expect(rect == RectI(x: 641, y: 468, width: 51, height: 51))
    }

    @Test func intersectClampsToFrame() {
        let r = RectI(x: -10, y: -10, width: 50, height: 50)
            .intersect(RectI(x: 0, y: 0, width: 100, height: 100))
        #expect(r == RectI(x: 0, y: 0, width: 40, height: 40))
        let empty = RectI(x: 200, y: 0, width: 10, height: 10)
            .intersect(RectI(x: 0, y: 0, width: 100, height: 100))
        #expect(empty.isEmpty)
    }
}
