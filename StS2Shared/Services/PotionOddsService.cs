using StS2Shared.Models;

namespace StS2Shared.Services;

/// <summary>次戦闘のポーション報酬ドロップ確率（通常 / エリート）。<see cref="ForcedByRelic"/> なら確定100%。</summary>
public readonly record struct PotionOdds(bool ForcedByRelic, float Normal, float Elite);

/// <summary>
/// セーブの <c>players[].odds.potion_reward_odds_value</c>（＝現在の実確率そのもの）から、
/// 次戦闘のポーションドロップ確率を求める。式・定数の根拠は <c>docs/potion-drop-odds.md</c>
/// （sts2.dll の <c>PotionRewardOdds</c> をデコンパイルして確定）。
/// </summary>
public static class PotionOddsService
{
    /// <summary>エリート戦の加算補正（DLL の eliteBonus 0.25 の半分）。</summary>
    public const float EliteBonus = 0.125f;

    /// <summary>White Beast Statue（戦闘部屋でポーション報酬を確定させるレリック）の ID 末尾。</summary>
    const string WhiteBeastStatueIdSuffix = "WHITE_BEAST_STATUE";

    /// <summary>
    /// プレイヤーのポーションドロップ確率を計算する。<c>odds</c> 未収録（古い/欠損セーブ）なら null。
    /// </summary>
    public static PotionOdds? Compute(PlayerData player)
    {
        if (player.Odds is null) return null;

        // 接頭辞非依存の末尾一致（"RELIC.WHITE_BEAST_STATUE" や beta 変種も拾う）。
        bool forced = player.Relics.Any(r =>
            r.Id.EndsWith(WhiteBeastStatueIdSuffix, StringComparison.OrdinalIgnoreCase));

        float v = player.Odds.PotionRewardOddsValue;
        return new PotionOdds(forced, Clamp01(v), Clamp01(v + EliteBonus));
    }

    static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
}
