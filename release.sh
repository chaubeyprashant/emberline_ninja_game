#!/usr/bin/env bash
# Emberline — Play Store release build: signed .aab, headless.
# Usage: ./release.sh [versionName]   (versionCode auto-increments)
set -euo pipefail

SRC="$(cd "$(dirname "$0")" && pwd)"
PROJ="$HOME/StudioProjects/EmberlineUnity"
LOGS="$SRC/logs"
SIGN="$SRC/signing"
mkdir -p "$LOGS"

EDITOR="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1 || true)"
[ -z "$EDITOR" ] && { echo "ERROR: no Unity editor found"; exit 1; }
[ -f "$SIGN/signing.properties" ] || { echo "ERROR: $SIGN/signing.properties missing"; exit 1; }

# Auto-increment version code.
CODE=$(cat "$SIGN/versioncode.txt" 2>/dev/null || echo 0)
CODE=$((CODE + 1))
echo "$CODE" > "$SIGN/versioncode.txt"

export EMBERLINE_SIGNING="$SIGN/signing.properties"
export EMBERLINE_VERSION_CODE="$CODE"
export EMBERLINE_VERSION_NAME="${1:-1.0.$CODE}"

echo "Building Emberline $EMBERLINE_VERSION_NAME (versionCode $CODE) — signed AAB…"

mkdir -p "$PROJ/Assets/Scripts" "$PROJ/Assets/Editor" "$PROJ/Assets/Shaders"
rsync -a --delete "$SRC/Assets/Scripts/" "$PROJ/Assets/Scripts/"
rsync -a --delete "$SRC/Assets/Editor/" "$PROJ/Assets/Editor/"
rsync -a --delete "$SRC/Assets/Shaders/" "$PROJ/Assets/Shaders/"

"$EDITOR" -batchmode -quit -projectPath "$PROJ" \
  -buildTarget Android \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.SetupAndBuildAab \
  -logFile "$LOGS/release.log" || true  # trust the AAB-exists check, not Unity's exit code

AAB="$PROJ/Builds/emberline.aab"
[ -f "$AAB" ] || { echo "Build finished but AAB not found — check $LOGS/release.log"; exit 1; }

# Re-sign with the upload key (Unity batch builds can't take scripted passwords).
mkdir -p "$SRC/Builds-release"
OUT="$SRC/Builds-release/emberline-v$CODE.aab"
cp "$AAB" "$OUT"
STOREPASS=$(grep '^storepass=' "$SIGN/signing.properties" | cut -d= -f2)
KEYPASS=$(grep '^keypass=' "$SIGN/signing.properties" | cut -d= -f2)
ALIAS=$(grep '^alias=' "$SIGN/signing.properties" | cut -d= -f2)
KS=$(grep '^keystore=' "$SIGN/signing.properties" | cut -d= -f2)
zip -q -d "$OUT" "META-INF/*" 2>/dev/null || true
jarsigner -keystore "$KS" -storepass "$STOREPASS" -keypass "$KEYPASS" \
  -digestalg SHA-256 -sigalg SHA256withRSA "$OUT" "$ALIAS" || { echo "ERROR: jarsigner failed"; exit 1; }
jarsigner -verify "$OUT" >/dev/null && echo "SIGNED+VERIFIED: $OUT (versionName $EMBERLINE_VERSION_NAME, versionCode $CODE)" || {
  echo "ERROR: signature verification failed"; exit 1; }
