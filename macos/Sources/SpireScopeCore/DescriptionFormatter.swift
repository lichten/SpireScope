import Foundation

/// StS2Shared/Services/DescriptionFormatter.cs の 1:1 移植。
/// `[gold]...[/gold]` 等の BB タグ除去と `{Var:format}` テンプレートの解決を行う。
public enum DescriptionFormatter {
    // Regex は Sendable 非準拠だが、リテラル構築後は読み取り専用のため共有しても安全。
    nonisolated(unsafe) static let tagRegex = /\[\/?[a-zA-Z]+\]/
    nonisolated(unsafe) static let templateRegex = /\{([^}]+)\}/
    nonisolated(unsafe) static let energyArgRegex = /energyIcons\((\d+)\)/
    nonisolated(unsafe) static let amountTemplate = /\{[Aa][Mm][Oo][Uu][Nn][Tt][^}]*\}/

    // 引数なし版: card_stats.json が未整備の変数は [VarName] で表示（案C）
    public static func clean(_ raw: String, japanese: Bool = false) -> String {
        resolve(raw, stats: nil, japanese: japanese)
    }

    // card_stats.json の値を渡すと実際の数値で置換する（案A）
    // preserveTags: true で [gold] 等の色タグを除去せず残す。
    // combineDiff:  true（既定）で diff() を "base(upgraded)" 結合表示、false で純 base 値のみ。
    public static func resolve(
        _ raw: String, stats: [String: Int]?, japanese: Bool = false, upgraded: Bool = false,
        preserveTags: Bool = false, combineDiff: Bool = true
    ) -> String {
        if raw.isEmpty { return raw }
        var s = stripInCombat(raw)
        if !preserveTags {
            s = s.replacing(tagRegex, with: "")
        }
        // C# 同様、置換前の文字列上の位置で前後の空白を判定しながらテンプレートを解決する。
        var out = ""
        var cursor = s.startIndex
        for m in s.matches(of: templateRegex) {
            out += s[cursor..<m.range.lowerBound]
            var r = resolveTemplate(String(m.output.1), stats: stats, japanese: japanese,
                                    upgraded: upgraded, combineDiff: combineDiff)
            if isEnergyWord(r, japanese: japanese) {
                if m.range.lowerBound > s.startIndex,
                   s[s.index(before: m.range.lowerBound)] != " " {
                    r = " " + r
                }
                if m.range.upperBound < s.endIndex, s[m.range.upperBound] != " " {
                    r += " "
                }
            }
            out += r
            cursor = m.range.upperBound
        }
        out += s[cursor...]
        return out.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    static func isEnergyWord(_ text: String, japanese: Bool) -> Bool {
        japanese ? text.hasSuffix("エナジー") : text.hasSuffix("Energy")
    }

    // エンチャント説明文用（Amount テンプレートのみ数値置換、残りは Clean）
    public static func cleanWithAmount(_ raw: String, amount: Int, japanese: Bool = false) -> String {
        clean(raw.replacing(amountTemplate, with: String(amount)), japanese: japanese)
    }

    // {InCombat:...|} ブロックをネスト深度で正確に除去
    static func stripInCombat(_ s: String) -> String {
        guard let start = s.range(of: "{InCombat:") else { return s }
        var pos = s.index(after: start.lowerBound)
        var depth = 1
        while pos < s.endIndex && depth > 0 {
            if s[pos] == "{" { depth += 1 } else if s[pos] == "}" { depth -= 1 }
            pos = s.index(after: pos)
        }
        return String(s[..<start.lowerBound]) + stripInCombat(String(s[pos...]))
    }

    static func resolveTemplate(
        _ content: String, stats: [String: Int]?, japanese: Bool, upgraded: Bool, combineDiff: Bool
    ) -> String {
        let colonIdx = content.firstIndex(of: ":")
        let varName = colonIdx.map { String(content[..<$0]) } ?? content
        let rest = colonIdx.map { String(content[content.index(after: $0)...]) } ?? ""

        // {InCombat:...} — 除去済みのはずだが念のため
        if varName == "InCombat" { return "" }

        // {IfUpgraded:show:UpgradedText|BaseText}
        if varName == "IfUpgraded", rest.hasPrefix("show:") {
            let afterShow = String(rest.dropFirst(5))
            let pipe = afterShow.firstIndex(of: "|")
            if upgraded {
                return pipe.map { String(afterShow[..<$0]) } ?? afterShow
            }
            return pipe.map { String(afterShow[afterShow.index(after: $0)...]) } ?? afterShow
        }

        // {VarName:plural:singular|plural} → singular（値が不明なので単数形を返す）
        if rest.hasPrefix("plural:") {
            let afterPlural = String(rest.dropFirst(7))
            // stats に値があれば正しい形を返す
            if let count = findStat(stats, varName), let pipe = afterPlural.firstIndex(of: "|") {
                return count == 1
                    ? String(afterPlural[..<pipe])
                    : String(afterPlural[afterPlural.index(after: pipe)...])
            }
            let pipe = afterPlural.firstIndex(of: "|")
            return pipe.map { String(afterPlural[..<$0]) } ?? afterPlural
        }

        // {prefix:energyIcons(N)} → "Energy" / "エナジー"（N=1）or "N Energy" / "Nエナジー"（N>1）
        if let energyMatch = rest.firstMatch(of: energyArgRegex) {
            let n = Int(energyMatch.output.1) ?? 0
            if japanese { return n <= 1 ? "エナジー" : "\(n)エナジー" }
            return n <= 1 ? "Energy" : "\(n) Energy"
        }

        // {VarName:diff()} など — stats を参照
        let (baseVal, upgVal) = findStatPair(stats, varName)
        if upgraded, let upgVal { return String(upgVal) }
        if let baseVal {
            if !upgraded, let upgVal, combineDiff { return "\(baseVal)(\(upgVal))" }
            return String(baseVal)
        }

        // 案C: 値不明の変数名を [VarName] で表示
        return "[\(varName)]"
    }

    // case-insensitive + 先頭アンダースコア除去でフィールド名を検索
    // "Power" サフィックスも除去してリトライ（例: DexterityPower → Dexterity）
    static func findStat(_ stats: [String: Int]?, _ varName: String) -> Int? {
        guard let stats else { return nil }
        if let v = findStatExact(stats, varName) { return v }
        if varName.lowercased().hasSuffix("power") {
            return findStatExact(stats, String(varName.dropLast(5)))
        }
        return nil
    }

    static func findStatExact(_ stats: [String: Int], _ varName: String) -> Int? {
        if let v = stats[varName] { return v }
        let lower = varName.lowercased()
        for (k, v) in stats {
            if k.drop(while: { $0 == "_" }).lowercased() == lower { return v }
        }
        return nil
    }

    // base + upgraded の両値を取得（diff() 表示用）
    static func findStatPair(_ stats: [String: Int]?, _ varName: String) -> (base: Int?, upgraded: Int?) {
        guard let stats else { return (nil, nil) }
        let result = findStatPairExact(stats, varName)
        if result.base != nil { return result }
        if varName.lowercased().hasSuffix("power") {
            return findStatPairExact(stats, String(varName.dropLast(5)))
        }
        return result
    }

    static func findStatPairExact(_ stats: [String: Int], _ varName: String) -> (base: Int?, upgraded: Int?) {
        var baseVal: Int?
        var upgVal: Int?
        let lower = varName.lowercased()
        let upgLower = "upgraded" + lower
        for (k, v) in stats {
            let norm = k.drop(while: { $0 == "_" }).lowercased()
            if norm == lower { baseVal = v } else if norm == upgLower { upgVal = v }
        }
        return (baseVal, upgVal)
    }
}
