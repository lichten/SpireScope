import Foundation
import SpireScopeCore

/// 正規化した名前（EN/JP）→ ID の索引。OCR 結果のファジー照合に使う。
/// C# CardNameIndex.cs の移植（エンシェント画面ではレリック名索引として使う）。
public final class CardNameIndex {
    public struct Match: Sendable {
        public let id: String
        public let canonicalName: String
        public let distance: Int
        public let confidence: Double
    }

    // 正規化名 → ID。EN/JP 双方を登録。
    private var exact: [String: String] = [:]
    // ファジー照合用：(正規化名, ID) の一覧（長さでバケット化）。
    private var byLen: [Int: [(norm: String, id: String)]] = [:]

    /// 全レリック名（EN/JP）→ レリック ID の索引（エンシェントレリック選択画面用）。
    public static func buildRelics(db: CardDatabase) -> CardNameIndex {
        let idx = CardNameIndex()
        for id in db.allRelicIds {
            idx.add(db.relicTitle(id, japanese: false), id: id)
            idx.add(db.relicTitle(id, japanese: true), id: id)
        }
        return idx
    }

    func add(_ name: String, id: String) {
        let norm = Self.normalize(name)
        guard norm.count >= 3 else { return } // 短すぎる名前は誤検出源なので除外
        exact[norm] = id
        byLen[norm.count, default: []].append((norm, id))
    }

    /// text に近い ID を距離昇順で列挙する（各 ID は最良距離のみ、maxDist 以内）。
    public func findRanked(_ text: String, maxDist: Int) -> [Match] {
        let norm = Self.normalize(text)
        guard norm.count >= 3, maxDist >= 0 else { return [] }

        var best: [String: Match] = [:]
        func consider(_ cand: String, _ id: String, _ d: Int) {
            guard d <= maxDist else { return }
            let conf = 1.0 - Double(d) / Double(max(norm.count, cand.count))
            if best[id] == nil || d < best[id]!.distance {
                best[id] = Match(id: id, canonicalName: cand, distance: d, confidence: conf)
            }
        }

        if let exactId = exact[norm] { consider(norm, exactId, 0) }
        let normChars = Array(norm)
        for len in max(3, norm.count - maxDist)...(norm.count + maxDist) {
            guard let list = byLen[len] else { continue }
            for (cand, id) in list {
                consider(cand, id, Self.levenshtein(normChars, Array(cand), max: maxDist))
            }
        }
        return best.values.sorted {
            $0.distance != $1.distance ? $0.distance < $1.distance : $0.confidence > $1.confidence
        }
    }

    /// 小文字化し、英数字・かな・漢字以外（空白・記号）を除去する。
    static func normalize(_ s: String) -> String {
        String(s.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
            .filter { $0.isLetter || $0.isNumber })
    }

    /// 上限付き Levenshtein 距離（上限超過なら max+1 を返す）。
    static func levenshtein(_ a: [Character], _ b: [Character], max maxDist: Int) -> Int {
        let n = a.count, m = b.count
        if abs(n - m) > maxDist { return maxDist + 1 }
        if n == 0 { return m }
        if m == 0 { return n }

        var prev = Array(0...m)
        var cur = [Int](repeating: 0, count: m + 1)
        for i in 1...n {
            cur[0] = i
            var rowMin = cur[0]
            for j in 1...m {
                let cost = a[i - 1] == b[j - 1] ? 0 : 1
                cur[j] = min(min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost)
                if cur[j] < rowMin { rowMin = cur[j] }
            }
            if rowMin > maxDist { return maxDist + 1 } // 早期打ち切り
            swap(&prev, &cur)
        }
        return prev[m]
    }
}
