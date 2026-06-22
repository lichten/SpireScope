namespace StS2Capture.Recognition;

/// <summary>
/// カード枠色の判定プロファイル。ピクセル (r,g,b) が枠色かを返す述語と表示名を持つ。
/// キャラごとに実測した色（RGB しきい値）や、色相非依存の彩度リングなどを差し替えられる。
/// 矩形検出はカード内容（=キャラ）を知る前段なので、プロファイルは current_run.save から
/// 解決した現在キャラで選ぶ（<see cref="ForCharacter"/>）。
/// </summary>
public sealed class FrameColorProfile
{
    public string Name { get; }
    readonly Func<int, int, int, bool> _matches;

    public FrameColorProfile(string name, Func<int, int, int, bool> matches)
    {
        Name = name;
        _matches = matches;
    }

    /// <summary>(r,g,b) が枠色か。</summary>
    public bool Matches(int r, int g, int b) => _matches(r, g, b);

    /// <summary>
    /// Defect の実機計測に合わせた青枠プロファイル（検証済み挙動）。
    /// 実測枠は B が突出した暗いティール青（例 RGB(18,91,129)〜(31,108,148)）。
    /// </summary>
    public static FrameColorProfile DefectBlue { get; } = new(
        "Defect(青・実測)",
        static (r, g, b) => b >= 95 && (b - r) >= 50 && (b - g) >= 14 && r <= 80);

    /// <summary>
    /// 色相非依存の彩度リング。彩度 S が高く・明度 V が中程度以上のピクセルを枠候補にする。
    /// 未実測キャラ／セーブ未検出時のベストエフォート。枠は細い均一色リング（低 fill）で
    /// 形状フィルタを通り、塗り潰しの絵（高 fill）や暗い背景（低 S/V）は除外される前提。
    /// HSV は手計算（<see cref="ImageOps.RowSaturation"/> と同じ max/min 式）。
    /// </summary>
    public static FrameColorProfile SaturatedRing { get; } = new(
        "汎用(彩度リング)",
        static (r, g, b) =>
        {
            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(r, Math.Min(g, b));
            if (max == 0) return false;
            double s = (double)(max - min) / max;
            double v = max / 255.0;
            return s >= 0.30 && v >= 0.18;
        });

    /// <summary>キャラ正規化 ID（大文字、例 "DEFECT"）→ 実測プロファイル。未登録は彩度リングに落ちる。</summary>
    static readonly Dictionary<string, FrameColorProfile> Measured = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DEFECT"] = DefectBlue,
    };

    /// <summary>実測プロファイルを持つキャラ ID の一覧（UI の手動上書き候補に使う）。</summary>
    public static IReadOnlyCollection<string> MeasuredCharacters => Measured.Keys;

    /// <summary>
    /// キャラ ID に対応するプロファイルを返す。null/未登録キャラは彩度リング・フォールバック。
    /// </summary>
    public static FrameColorProfile ForCharacter(string? characterId)
    {
        if (characterId is not null && Measured.TryGetValue(characterId, out var p))
            return p;
        return SaturatedRing;
    }
}
