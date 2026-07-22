# 次の戦闘のポーションドロップ確率 — 算出方法の調査

SpireScope に「次の戦闘でポーションがドロップする確率」を表示する機能を追加するための調査記録。
**結論：セーブデータ（`current_run.save`）に現在の実確率がそのまま保存されているため、
Mod も手入力も不要で、セーブ読み込みだけで正確に算出できる。**

- 調査日: 2026-07-06
- 対象ゲームバージョン: v0.108.0（セーブ `schema_version: 18`）
- 一次情報: `sts2.dll` を ilspycmd でデコンパイルして確認（下記に抜粋）。
  有志解析（[slaythespire.wiki.gg の Potions ページ](https://slaythespire.wiki.gg/wiki/Slay_the_Spire_2:Potions)）とも完全に一致した。
  なお `tools/extracted/src/**` の逆コンパイル資料は空スタブのため一次情報にならない。

## 1. ゲーム内実装の仕様

### 1.1 確率の本体 — `MegaCrit.Sts2.Core.Odds.PotionRewardOdds`

```csharp
public class PotionRewardOdds : AbstractOdds
{
    public const float targetOdds = 0.5f;          // 設計上の収束目標（コメント用）
    public const float eliteBonus = 0.25f;         // ※実際は半分の +0.125 が効く（後述）
    private const float _basePotionRewardOdds = 0.4f;

    public PotionRewardOdds(Rng rng) : base(0.4f, rng) { }                 // ラン開始時
    public PotionRewardOdds(float initialValue, Rng rng)                   // セーブ復元時
        : base(initialValue, rng) { }

    public bool Roll(Player player, RoomType roomType)
    {
        float currentValue = base.CurrentValue;
        if (Hook.ShouldForcePotionReward(player.RunState, player, roomType))
            return true;                                   // 強制ドロップ（オッズ変動なし）
        float num = ((roomType != RoomType.Elite) ? 0f : 0.25f);
        float num3 = currentValue + num * 0.5f;            // エリートは +0.125
        if (_rng.NextFloat() < num3)
        {
            base.CurrentValue -= 0.1f;                     // ドロップ → 次回 -10%
            return true;
        }
        base.CurrentValue += 0.1f;                         // 外れ → 次回 +10%
        return false;
    }
}
```

要点：

| 項目 | 値・挙動 |
|---|---|
| 初期値 | **40%**（ラン開始時） |
| pity 方式 | ドロップで **−10%** / 外れで **+10%** |
| エリート補正 | **+12.5%**（`eliteBonus` 定数 0.25 の**半分**が加算される） |
| クランプ | なし。数値上は 0 未満・1 超になり得るが、`NextFloat()`（[0,1)）との比較なので実効確率は 0〜100% に自然飽和 |
| 強制ドロップ | `Hook.ShouldForcePotionReward` が true なら 100%、かつ**オッズは変動しない** |
| 乱数 | `PlayerRngSet.Rewards` ストリーム（`CardRarityOdds` と共用） |

`AbstractOdds.CurrentValue` は**実確率そのもの**（ベースへの加算修正値ではない）。
セーブ復元用コンストラクタが `CurrentValue = initialValue` とするだけであることからも確定。

### 1.2 いつロールされるか — `RewardsSet.WithRewardsFromRoom`

- **Monster / Elite / Boss** の戦闘報酬生成時に `RollForPotionAndAddTo` でロールされる。
- 最終アクトのボスは報酬自体が無いため対象外。
- チュートリアル（初回ラン Ironclad 序盤）は固定報酬でロールしない。
- `RollForPotionAndAddTo` 内の `AscensionManager` 取得は未使用（死に変数）で、
  アセンションはドロップ確率に影響しない（v0.108.0 時点）。

### 1.3 強制ドロップの実装 — White Beast Statue

```csharp
public sealed class WhiteBeastStatue : RelicModel
{
    public override bool ShouldForcePotionReward(Player player, RoomType roomType)
    {
        if (player != base.Owner) return false;
        if (!roomType.IsCombatRoom()) return false;
        return true;
    }
}
```

フックは `AbstractModel` 起点なので、将来ほかのレリック・エポック等が実装する可能性がある。
v0.108.0 で `ShouldForcePotionReward` を実装するのは `WhiteBeastStatue` のみ（sts2.xml で確認）。

## 2. セーブデータの所在とフィールド

`%AppData%\SlayTheSpire2\steam\{steamId}\profile1\saves\current_run.save`
（**平文の pretty-print JSON**。暗号化・圧縮なし。SpireScope の `SaveDataService` が既に読んでいるファイル）

### 2.1 本命フィールド

```jsonc
"players": [
  {
    // ...
    "odds": {
      "card_rarity_odds_value": -0.05,
      "potion_reward_odds_value": 0.3     // ← 現在の実確率そのもの（この例: 次の通常戦 30%）
    }
  }
]
```

DLL 側のシリアライズは `SerializablePlayerOddsSet`（`ToSerializable` が
`PotionReward.CurrentValue` をそのまま書き出す）。つまり**このフィールドを読むだけで、
pity の履歴を辿ることなく現在の確率が正確に分かる**。

### 2.2 併読すると有用なフィールド

| フィールド | 用途 |
|---|---|
| `players[].relics[]` | White Beast Statue（`RELIC.WHITE_BEAST_STATUE` 系 ID）所持なら 100% 表示に切替 |
| `players[].potions[]` / `max_potion_slot_count` | ポーション枠あふれの注記（ドロップしても取れない状況の表示） |
| `rng.counters.combat_potion_generation` / `rng.seed` | 「どのポーションが出るか」の生成用（ドロップ判定とは別ストリーム。確率表示には不要） |

## 3. 算出方法（結論）

```
value = players[i].odds.potion_reward_odds_value

P(通常戦闘)   = clamp01(value)
P(エリート)   = clamp01(value + 0.125)
P(ボス)       = clamp01(value)            // 最終アクトのボスを除く
WBS 所持時    = 100%（部屋種別を問わず戦闘部屋。オッズ非消費）
```

例：調査時の実セーブは `value = 0.3` → 次の通常戦闘 **30%**・エリート **42.5%**。

表示後の変動も決定的に予告できる：ドロップすれば次は `value − 0.1`、外れれば `value + 0.1`
（WBS 強制ドロップ時は変動なし）。

## 4. 代替案の比較と不採用理由

| 方式 | 評価 |
|---|---|
| **セーブ読み込み（採用）** | 正確・自動・手間ゼロ。実装はモデル拡張＋表示のみ |
| 手入力 | 不要（セーブに実確率がある）。初期値 40% から戦闘結果ごとに ±10% を手で辿る UI は煩雑で、エリート補正・WBS・入力漏れで容易に狂う |
| RNG 完全再現（次ロールの成否まで確定予測） | `rng.seed`＋rewards カウンタから理論上可能だが、ゲームの乱数実装の移植が必要。しかもドロップ判定はカードレアリティと**同一の rewards ストリームを共用**するため、消費順の完全再現が壊れやすい。高コスト・保守困難で非推奨（確率表示で十分） |

## 5. 将来の実装方針スケッチ

1. **モデル拡張**（`StS2Shared/Models/SaveData.cs`）：`PlayerData` に
   `odds`（`potion_reward_odds_value` / `card_rarity_odds_value`）を追加。
   必要なら `potions[]` / `max_potion_slot_count` も。
2. **サービス**：確率計算（§3 の式＋WBS 判定）は小さな静的ヘルパで足りる
   （`CardDatabaseService` 等と同様に `StS2Shared` へ）。
3. **表示**：`Form1.DisplayData` から表示（ステータスバー or サイドパネル）。
   `EncounterOverviewForm` にはボス予測表示の前例があり、エリート行に 42.5% 等の
   行内表示を足す拡張も自然。ファイル監視の自動リロードが既にあるため、
   戦闘後にセーブが書き換われば表示も自動更新される。
4. **ゲーム更新時の再検証**：バージョンアップ時は
   `ilspycmd -t "MegaCrit.Sts2.Core.Odds.PotionRewardOdds" "<sts2.dll>"`
   で定数（0.4 / ±0.1 / 0.25×0.5）とロール式の変化を確認する
   （手順は card-type-extractor 系と同じ。`ilspycmd -l c` で型一覧）。
