import Foundation

/// 情報ページ URL テンプレート（StS2Shared/Services/SiteLinkService.cs の移植）。
/// JSON のキーは C# 版の url_templates.json（PascalCase）と互換にし、
/// Windows からコピーした設定ファイルをそのまま読めるようにする。
public struct UrlTemplate: Codable, Sendable, Equatable {
    public var label: String
    public var type: String      // card/relic/potion/monster/event/encounter/any
    public var template: String  // {id}/{en}/{jp}/{idraw}/{idrawlower}/{cardclass} を含む URL

    enum CodingKeys: String, CodingKey {
        case label = "Label"
        case type = "Type"
        case template = "Template"
    }

    public init(label: String, type: String, template: String) {
        self.label = label
        self.type = type
        self.template = template
    }
}

/// url_templates.json の読み書き（SpireScope/Services/UrlTemplateService.cs の移植）。
/// 保存先: ~/Library/Application Support/SpireScope/url_templates.json
public enum UrlTemplateStore {
    public static var path: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appending(components: "Library", "Application Support", "SpireScope", "url_templates.json")
    }

    public static func load() -> [UrlTemplate] {
        guard let data = try? Data(contentsOf: path),
              let templates = try? JSONDecoder().decode([UrlTemplate].self, from: data)
        else { return defaults() }
        return templates
    }

    public static func save(_ templates: [UrlTemplate]) throws {
        try FileManager.default.createDirectory(
            at: path.deletingLastPathComponent(), withIntermediateDirectories: true)
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted]
        try encoder.encode(templates).write(to: path)
    }

    /// 初期テンプレート（外部 Wiki のみ）。
    public static func defaults() -> [UrlTemplate] {
        [UrlTemplate(label: "Wiki", type: "any", template: "https://wikiwiki.jp/sts2/{jp}")]
    }
}

/// 検出エンティティ（種別＋ID）から URL テンプレートを展開してリンクを作る。
public struct SiteLinkService: Sendable {
    public struct Link: Sendable, Equatable {
        public let label: String
        public let url: String
    }

    static let cardClasses: Set<String> = ["ironclad", "silent", "defect", "necrobinder", "regent", "fool"]

    let db: CardDatabase

    public init(db: CardDatabase) { self.db = db }

    /// 種別が一致（または "any"）するテンプレートを展開したリンク一覧。
    public func buildLinks(templates: [UrlTemplate], kind: String, id: String) -> [Link] {
        let t = resolveTokens(kind: kind, id: id)
        var links: [Link] = []
        for tmpl in templates {
            let type = tmpl.type.trimmingCharacters(in: .whitespaces)
            if !type.isEmpty,
               type.caseInsensitiveCompare("any") != .orderedSame,
               type.caseInsensitiveCompare(kind) != .orderedSame {
                continue
            }
            let url = Self.expand(tmpl.template, t)
            if !url.trimmingCharacters(in: .whitespaces).isEmpty {
                links.append(Link(label: tmpl.label, url: url))
            }
        }
        return links
    }

    struct Tokens {
        let id: String
        let idRaw: String
        let idRawLower: String
        let en: String
        let jp: String
        let cardClass: String
    }

    func resolveTokens(kind: String, id: String) -> Tokens {
        let idRaw = CardDatabase.rawId(id)
        let en: String
        let jp: String
        switch kind.lowercased() {
        case "card":
            en = db.name(id, japanese: false)
            jp = db.name(id, japanese: true)
        case "relic":
            en = db.relicTitle(id, japanese: false)
            jp = db.relicTitle(id, japanese: true)
        case "event":
            en = db.eventTitle(id, japanese: false)
            jp = db.eventTitle(id, japanese: true)
        case "potion":
            en = db.potionTitle(id, japanese: false)
            jp = db.potionTitle(id, japanese: true)
        default:
            en = idRaw
            jp = idRaw
        }

        var cardClass = ""
        if kind.caseInsensitiveCompare("card") == .orderedSame {
            let c = db.cardCharacter(id).lowercased()
            cardClass = Self.cardClasses.contains(c) ? c : "shared"
        }

        return Tokens(id: id, idRaw: idRaw, idRawLower: idRaw.lowercased(),
                      en: en, jp: jp, cardClass: cardClass)
    }

    static func expand(_ template: String, _ t: Tokens) -> String {
        // 長いトークンから置換（部分一致を避ける）。値は URL エンコード。
        template
            .replacingOccurrences(of: "{idrawlower}", with: enc(t.idRawLower))
            .replacingOccurrences(of: "{idraw}", with: enc(t.idRaw))
            .replacingOccurrences(of: "{cardclass}", with: enc(t.cardClass))
            .replacingOccurrences(of: "{id}", with: enc(t.id))
            .replacingOccurrences(of: "{en}", with: enc(t.en))
            .replacingOccurrences(of: "{jp}", with: enc(t.jp))
    }

    static func enc(_ s: String) -> String {
        s.addingPercentEncoding(withAllowedCharacters: .alphanumerics.union(.init(charactersIn: "-._~"))) ?? s
    }
}
