# キャプチャ認識のフィクスチャ

ゴールデンテスト用のフレーム PNG を置く（大きいのでコミットは最小限）。

- `card_select.png` — カード選択画面のフルフレーム（Windows 版 `STS2_DUMP_DIR` ダンプ
  `live_CardSelect.png` か、Mac 版キャプチャビューの「フレームを保存」出力）
- `shop.png` — ショップ画面
- `ancient.png` — エンシェントレリック選択画面
- `*.expected.json` — 期待値（`{"screen": "cardSelect", "cards": ["CARD.X", ...]}` 形式）

ファイルが無い場合、対応するテストはスキップされる（CI では単体テストのみ走る）。
認識 DB はローカルの抽出済みアセット（`~/Library/Application Support/SpireScope/assets/v*`）を
使うため、ゴールデンテストはセットアップ済みのマシンでのみ有効。
