using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using StS2Shared.Services;

namespace SpireScope.Services;

/// <summary>
/// spire-codex.com のティアスコア（Codex Score、0-100）を保持し「S (92)」形式の表示文字列を返す。
/// データは「公開 API 取得 → %AppData% キャッシュ → 埋め込みスナップショット」の 3 段フォールバック
/// （詳細は docs/SpireScope-TierScores.md）。カードは現在ランのキャラ別スコアを優先し、
/// 未収載・ラン外は全体スコアへ落ちる。
/// </summary>
static class TierScoreService
{
    const string ApiBase = "https://spire-codex.com/api/runs/scores";

    // サイト側の更新周期（30分）の2倍。TTL 内は再取得しない。
    static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpireScope", "tier_scores_cache.json");

    static readonly HttpClient Http = CreateClient();

    sealed record ScoreBundle(DateTime FetchedAtUtc,
        IReadOnlyDictionary<string, int> Cards,
        IReadOnlyDictionary<string, int> Relics,
        IReadOnlyDictionary<string, int> Potions);

    sealed record CharScores(DateTime FetchedAtUtc, string CharId,
        IReadOnlyDictionary<string, int> Cards);

    // 差し替えのみで共有する immutable スナップショット（lock 不要で参照できる）
    static volatile ScoreBundle? _global;
    static volatile CharScores? _char;

    // キャッシュファイルへ書き戻すためのキャラ別スコア一式（アクセスは _saveLock で保護）
    static readonly Dictionary<string, CharScores> _cachedChars = new(StringComparer.OrdinalIgnoreCase);
    static readonly object _saveLock = new();
    static int _globalFetching, _charFetching;

    /// <summary>正規化キャラ ID（接頭辞なし大文字）→ API の character パラメータ。未知キャラは全体スコアのみ。</summary>
    static readonly Dictionary<string, string> CharApiNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IRONCLAD"] = "ironclad",
        ["SILENT"] = "silent",
        ["DEFECT"] = "defect",
        ["NECROBINDER"] = "necrobinder",
        ["REGENT"] = "regent",
    };

    /// <summary>スコア更新完了時。バックグラウンドスレッドで発火するため UI 側は BeginInvoke すること。</summary>
    public static event Action? Updated;

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.TryParseAdd($"SpireScope/{AppVersion.Display}");
        return http;
    }

    /// <summary>
    /// 起動時に1回呼ぶ。埋め込みスナップショット→キャッシュを同期ロードし（この時点で表示可能になる）、
    /// TTL 切れなら裏で API から再取得する。
    /// </summary>
    public static void Initialize()
    {
        _global = LoadSnapshot();
        LoadCacheFile();
        if (_global is null || DateTime.UtcNow - _global.FetchedAtUtc > CacheTtl)
            _ = Task.Run(FetchGlobalAsync);
        RequestCharacterScores(SaveDataService.TryGetCurrentCharacterId());
    }

    /// <summary>
    /// 現在ランのキャラ別カードスコアを要求する（セーブ自動リロード毎に呼ばれる想定の冪等・軽量メソッド）。
    /// null・未知キャラならキャラ別を解除して全体スコアに戻す。
    /// </summary>
    public static void RequestCharacterScores(string? normalizedCharId)
    {
        if (normalizedCharId is null || !CharApiNames.ContainsKey(normalizedCharId))
        {
            if (_char is not null)
            {
                _char = null;
                Updated?.Invoke();
            }
            return;
        }

        if (_char is { } current &&
            current.CharId.Equals(normalizedCharId, StringComparison.OrdinalIgnoreCase) &&
            DateTime.UtcNow - current.FetchedAtUtc <= CacheTtl)
            return;

        CharScores? cached;
        lock (_saveLock)
            _cachedChars.TryGetValue(normalizedCharId, out cached);
        if (cached is not null && DateTime.UtcNow - cached.FetchedAtUtc <= CacheTtl)
        {
            _char = cached;
            Updated?.Invoke();
            return;
        }

        _ = Task.Run(() => FetchCharacterAsync(normalizedCharId));
    }

    /// <summary>カードのティア表示（例 "S (92)"）。"CARD.STRIKE" 形式可。未収載は空文字。</summary>
    public static string FormatCardTier(string cardId)
    {
        var byChar = Lookup(_char?.Cards, cardId);
        return byChar.Length > 0 ? byChar : Lookup(_global?.Cards, cardId);
    }

    /// <summary>レリックのティア表示。ID は接頭辞なし raw 形式（"AKABEKO"）・接頭辞付きも可。</summary>
    public static string FormatRelicTier(string relicId) => Lookup(_global?.Relics, relicId);

    /// <summary>ポーションのティア表示。</summary>
    public static string FormatPotionTier(string potionId) => Lookup(_global?.Potions, potionId);

    static string Lookup(IReadOnlyDictionary<string, int>? dict, string id) =>
        dict is not null && dict.TryGetValue(ToRaw(id), out var score)
            ? $"{TierLetter(score)} ({score})"
            : "";

    // spire-codex.com のティア境界（tier-list ページの定義に一致させる）
    static string TierLetter(int s) =>
        s >= 90 ? "S" : s >= 78 ? "A" : s >= 65 ? "B" : s >= 50 ? "C" : s >= 35 ? "D" : "F";

    static string ToRaw(string id)
    {
        int i = id.IndexOf('.');
        return i >= 0 ? id[(i + 1)..] : id;
    }

    // ---- 取得 ----

    static async Task FetchGlobalAsync()
    {
        if (Interlocked.Exchange(ref _globalFetching, 1) == 1) return;
        try
        {
            // 逐次取得（並列にしない）。レート制限 300req/分に対して行儀よく振る舞う。
            var cards   = ParseScores(await Http.GetStringAsync($"{ApiBase}/cards"));
            var relics  = ParseScores(await Http.GetStringAsync($"{ApiBase}/relics"));
            var potions = ParseScores(await Http.GetStringAsync($"{ApiBase}/potions"));
            _global = new ScoreBundle(DateTime.UtcNow, cards, relics, potions);
            SaveCacheFile();
            Updated?.Invoke();
        }
        catch (Exception ex)
        {
            // オフライン・429・5xx いずれも同じ扱い: 現状データ（キャッシュ/スナップショット）で表示継続
            Debug.WriteLine($"TierScoreService: 全体スコア取得失敗: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _globalFetching, 0);
        }
    }

    static async Task FetchCharacterAsync(string charId)
    {
        if (Interlocked.Exchange(ref _charFetching, 1) == 1) return;
        try
        {
            var json = await Http.GetStringAsync($"{ApiBase}/cards?character={CharApiNames[charId]}");
            var scores = new CharScores(DateTime.UtcNow, charId, ParseScores(json));
            _char = scores;
            lock (_saveLock)
                _cachedChars[charId] = scores;
            SaveCacheFile();
            Updated?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TierScoreService: キャラ別スコア取得失敗（{charId}）: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _charFetching, 0);
        }
    }

    /// <summary>API レスポンス {"ID": {"score": 92, ...}, ...} から score のみ抜き出す。壊れたエントリはスキップ。</summary>
    static IReadOnlyDictionary<string, int> ParseScores(string json)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("score", out var s) &&
                s.ValueKind == JsonValueKind.Number)
                dict[prop.Name] = (int)Math.Round(s.GetDouble());
        }
        return dict;
    }

    // ---- 永続化（スナップショット・キャッシュは同じセクション形式を共用） ----

    sealed class CacheFileDto
    {
        [JsonPropertyName("global")] public SectionDto? Global { get; set; }
        [JsonPropertyName("characters")] public Dictionary<string, CharSectionDto>? Characters { get; set; }
    }

    sealed class SectionDto
    {
        [JsonPropertyName("fetched_at")] public DateTime FetchedAt { get; set; }
        [JsonPropertyName("cards")] public Dictionary<string, int>? Cards { get; set; }
        [JsonPropertyName("relics")] public Dictionary<string, int>? Relics { get; set; }
        [JsonPropertyName("potions")] public Dictionary<string, int>? Potions { get; set; }
    }

    sealed class CharSectionDto
    {
        [JsonPropertyName("fetched_at")] public DateTime FetchedAt { get; set; }
        [JsonPropertyName("cards")] public Dictionary<string, int>? Cards { get; set; }
    }

    static ScoreBundle? LoadSnapshot()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames().FirstOrDefault(
                n => n.EndsWith(".Resources.tier_scores_snapshot.json", StringComparison.Ordinal));
            if (name is null) return null;
            using var stream = asm.GetManifestResourceStream(name)!;
            return ToBundle(JsonSerializer.Deserialize<SectionDto>(stream));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TierScoreService: スナップショット読込失敗: {ex.Message}");
            return null;
        }
    }

    static void LoadCacheFile()
    {
        try
        {
            if (!File.Exists(CachePath)) return;
            using var stream = File.OpenRead(CachePath);
            var file = JsonSerializer.Deserialize<CacheFileDto>(stream);
            if (file is null) return;

            var bundle = ToBundle(file.Global);
            if (bundle is not null && (_global is null || bundle.FetchedAtUtc > _global.FetchedAtUtc))
                _global = bundle;

            if (file.Characters is not null)
                lock (_saveLock)
                    foreach (var (id, sec) in file.Characters)
                        if (sec?.Cards is not null)
                            _cachedChars[id] = new CharScores(
                                sec.FetchedAt.ToUniversalTime(), id, Reindex(sec.Cards));
        }
        catch (Exception ex)
        {
            // 壊れたキャッシュは無視（スナップショット＋再取得で復旧する）
            Debug.WriteLine($"TierScoreService: キャッシュ読込失敗（無視）: {ex.Message}");
        }
    }

    static void SaveCacheFile()
    {
        try
        {
            lock (_saveLock)
            {
                var g = _global;
                var dto = new CacheFileDto
                {
                    Global = g is null ? null : new SectionDto
                    {
                        FetchedAt = g.FetchedAtUtc,
                        Cards = new(g.Cards),
                        Relics = new(g.Relics),
                        Potions = new(g.Potions),
                    },
                    Characters = _cachedChars.ToDictionary(kv => kv.Key, kv => new CharSectionDto
                    {
                        FetchedAt = kv.Value.FetchedAtUtc,
                        Cards = new(kv.Value.Cards),
                    }),
                };
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
                using var stream = File.Create(CachePath);
                JsonSerializer.Serialize(stream, dto);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TierScoreService: キャッシュ保存失敗: {ex.Message}");
        }
    }

    static ScoreBundle? ToBundle(SectionDto? dto) =>
        dto is null ? null : new ScoreBundle(
            dto.FetchedAt.ToUniversalTime(),
            Reindex(dto.Cards), Reindex(dto.Relics), Reindex(dto.Potions));

    static IReadOnlyDictionary<string, int> Reindex(Dictionary<string, int>? src) =>
        src is null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, int>(src, StringComparer.OrdinalIgnoreCase);
}
