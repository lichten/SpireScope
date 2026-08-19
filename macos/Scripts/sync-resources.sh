#!/bin/bash
# StS2Shared/Resources の最新バージョンフォルダ（v* の最大）と手動管理 JSON を
# Sources/SpireScopeCore/Resources/data/ へ同期する。ゲーム更新で新しい
# Resources/v{version}/ がコミットされたら再実行してコミットすること。
set -euo pipefail

cd "$(dirname "$0")/.."
SRC="../StS2Shared/Resources"
DST="Sources/SpireScopeCore/Resources/data"

# バージョンフォルダの最大を選ぶ（整数列をゼロ埋めして比較。ResourceResolver.VersionKey と同方針）
latest=$(ls -d "$SRC"/v*/ | while read -r d; do
  name=$(basename "$d")
  key=$(echo "$name" | grep -oE '[0-9]+' | while read -r n; do printf "%08d." "$n"; done)
  echo "$key $name"
done | sort | tail -1 | awk '{print $2}')

if [ -z "$latest" ]; then
  echo "エラー: $SRC に v* フォルダが見つかりません" >&2
  exit 1
fi

echo "同期: $SRC/$latest -> $DST/$latest"
rm -rf "$DST"
mkdir -p "$DST/$latest"
rsync -a --delete "$SRC/$latest/" "$DST/$latest/"

# 手動管理（バージョン非依存）の JSON
for f in "$SRC"/*.json; do
  cp "$f" "$DST/"
done

echo "完了:"
du -sh "$DST"
