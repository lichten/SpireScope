#!/bin/bash
# Resources/icon.svg から AppIcon.icns を再生成する（アイコンデザイン変更時のみ実行）。
#   1. Quick Look（qlmanage）で SVG → Resources/icon_1024.png
#   2. sips で各サイズへ縮小 → iconutil で .icns 化
# 出力: Sources/SpireScopeApp/Resources/AppIcon.icns（アプリ同梱＝原本。make-app.sh もここから使う）
set -euo pipefail

cd "$(dirname "$0")/.."
SVG="Resources/icon.svg"
PNG="Resources/icon_1024.png"
ICNS="Sources/SpireScopeApp/Resources/AppIcon.icns"

# SVG → 1024px PNG（qlmanage が失敗する環境ではコミット済み PNG をそのまま使う）
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
if qlmanage -t -s 1024 "$SVG" -o "$TMP" >/dev/null 2>&1 && [ -f "$TMP/icon.svg.png" ]; then
  cp "$TMP/icon.svg.png" "$PNG"
  echo "レンダリング: $SVG -> $PNG"
elif [ ! -f "$PNG" ]; then
  echo "エラー: SVG をレンダリングできず、$PNG も存在しません" >&2
  exit 1
fi

# iconset を組み立てて icns 化
ICONSET="$TMP/SpireScope.iconset"
mkdir -p "$ICONSET"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$PNG" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$PNG" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
mkdir -p "$(dirname "$ICNS")"
iconutil -c icns "$ICONSET" -o "$ICNS"

echo "生成: $ICNS"
