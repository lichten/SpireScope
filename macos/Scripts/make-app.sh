#!/bin/bash
# release ビルドから SpireScope.app を組み立てる（ad-hoc 署名＝本人利用向け）。
# 出力: macos/out/SpireScope.app
set -euo pipefail

cd "$(dirname "$0")/.."

swift build -c release

OUT="out/SpireScope.app"
BIN=".build/release/SpireScopeApp"
BUNDLE=".build/release/SpireScopeMac_SpireScopeCore.bundle"

rm -rf "$OUT"
mkdir -p "$OUT/Contents/MacOS" "$OUT/Contents/Resources"
cp Resources/Info.plist "$OUT/Contents/Info.plist"
cp "$BIN" "$OUT/Contents/MacOS/SpireScopeApp"
# リソース（data/）は Contents/Resources へ直接コピーする。
# ResourceStore が Bundle.main → Bundle.module の順で解決する前提。
cp -R "$BUNDLE/data" "$OUT/Contents/Resources/data"
# アプリアイコン（原本は SpireScopeApp ターゲットのリソース。Scripts/make-icon.sh で再生成）
cp Sources/SpireScopeApp/Resources/AppIcon.icns "$OUT/Contents/Resources/AppIcon.icns"

codesign --force --sign - "$OUT"

echo "生成: $(pwd)/$OUT"
