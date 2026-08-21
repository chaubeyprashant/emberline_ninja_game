#!/usr/bin/env bash
# Emberline 3D — fully headless Unity project creation + Android build.
# Run after Unity Hub has installed a Unity 6 editor with Android Build Support.
set -euo pipefail

SRC="$(cd "$(dirname "$0")" && pwd)"
PROJ="$HOME/StudioProjects/EmberlineUnity"
LOGS="$SRC/logs"
mkdir -p "$LOGS"

EDITOR="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1 || true)"
if [ -z "$EDITOR" ]; then
  echo "ERROR: No Unity editor found under /Applications/Unity/Hub/Editor/."
  echo "Install Unity 6 LTS via Unity Hub (with Android Build Support) first."
  exit 1
fi
echo "Using editor: $EDITOR"

if [ ! -d "$PROJ/Assets" ]; then
  echo "Creating Unity project at $PROJ (first run takes a few minutes)…"
  "$EDITOR" -batchmode -quit -createProject "$PROJ" -logFile "$LOGS/create.log"
fi

echo "Syncing Emberline scripts into the project…"
mkdir -p "$PROJ/Assets/Scripts" "$PROJ/Assets/Editor" "$PROJ/Assets/Shaders"
rsync -a --delete "$SRC/Assets/Scripts/" "$PROJ/Assets/Scripts/"
rsync -a --delete "$SRC/Assets/Editor/" "$PROJ/Assets/Editor/"
rsync -a --delete "$SRC/Assets/Shaders/" "$PROJ/Assets/Shaders/"

echo "Generating scene + building APK (IL2CPP/ARM64 — expect 5–15 min on first build)…"
"$EDITOR" -batchmode -quit -projectPath "$PROJ" \
  -buildTarget Android \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.SetupAndBuild \
  -logFile "$LOGS/build.log"

APK="$PROJ/Builds/emberline3d.apk"
if [ -f "$APK" ]; then
  echo "SUCCESS: $APK"
else
  echo "Build finished but APK not found — check $LOGS/build.log"
  exit 1
fi
