# SpireScope ティアスコア表示（spire-codex.com）

ライブキャプチャの検出結果（カード選択・ショップ・エンシェント選択）の各行に、
spire-codex.com の **Codex Score**（コミュニティ勝率ベースの 0-100 評価）を
「S (92)」形式の Tier 列として表示する。実装は `SpireScope/Services/TierScoreService.cs`。

## データソース（公開 API）

サイトの利用規約は「文書化された公開 API」経由の利用を許可している（スクレイピングは禁止）。
API ドキュメント: https://spire-codex.com/developers

```
GET https://spire-codex.com/api/runs/scores/{cards|relics|potions}
GET https://spire-codex.com/api/runs/scores/cards?character={ironclad|silent|defect|necrobinder|regent}
```

- 認証不要（キーなしは IP あたり 300 req/分）。SpireScope の発行数は起動時最大 3＋キャラ変化時 1 で、
  User-Agent に `SpireScope/{version}` を付ける。
- レスポンス: `{"BURST": {"score": 92, "elo": ..., "picks": ..., "wins": ..., "win_rate": ...}, ...}`。
  ID は接頭辞なし大文字スネークケース＝本リポジトリの raw ID（`ToRawId`）と同一。
  score のみ使用し他フィールドは捨てる。
- サイト側のスコア更新周期は 30 分。

## 3 段フォールバックと TTL

表示データの解決順（`TierScoreService.Initialize`）:

1. **埋め込みスナップショット** `SpireScope/Resources/tier_scores_snapshot.json` を同期ロード
   （オフライン初回起動でも Tier 列が埋まる）
2. **キャッシュ** `%AppData%\SpireScope\tier_scores_cache.json` がスナップショットより新しければ上書き
3. 手持ちデータが **TTL（1 時間）** を超えていたら裏で API 再取得 → 成功時にキャッシュ保存＋
   `Updated` イベントで再描画。失敗（オフライン・429・5xx）は現状データのまま表示継続、リトライなし
   （次回起動・次回キャラ変化が再試行機会）

カードは**現在ランのキャラ別スコアを優先**し、ラン外・未取得・未収載カードは全体スコアへ落ちる。
キャラ別の取得はセーブ読込（`Form1.DisplayData`）をトリガーに `RequestCharacterScores` が冪等に行う。
スターターカード等の非ドラフトカード（約 140 件）は API 未収載のため空欄になる。

## ティア境界

サイトの tier-list ページの定義と一致させている（`TierScoreService.TierLetter`）:

| Tier | スコア |
|---|---|
| S | 90+ |
| A | 78–89 |
| B | 65–77 |
| C | 50–64 |
| D | 35–49 |
| F | 0–34 |

## 埋め込みスナップショットの更新手順

**リリース前に 1 回**、リポジトリルートで以下を実行して埋め込みスナップショットを更新する
（要ネットワーク。実行後は通常のビルドで埋め込みに反映される）:

```powershell
$base = "https://spire-codex.com/api/runs/scores"
$out = [ordered]@{ fetched_at = (Get-Date).ToUniversalTime().ToString("o") }
foreach ($k in "cards","relics","potions") {
  $r = Invoke-RestMethod "$base/$k" -Headers @{ "User-Agent" = "SpireScope-SnapshotUpdate" }
  $d = [ordered]@{}
  $r.PSObject.Properties | Sort-Object Name | ForEach-Object { $d[$_.Name] = [int][math]::Round($_.Value.score) }
  $out[$k] = $d
}
$out | ConvertTo-Json -Depth 4 | Set-Content SpireScope\Resources\tier_scores_snapshot.json -Encoding utf8
```

スナップショットの形式はキャッシュファイルの `global` セクションと同一で、パーサを共用している
（`TierScoreService.SectionDto`）。
