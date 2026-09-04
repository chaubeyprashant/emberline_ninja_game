#!/usr/bin/env bash
# Build and sign a Play-ready App Bundle.
#
#   ./release.sh 7 1.2.0
#
# Unity's batch builds reject scripted keystore passwords, so the AAB comes out
# debug-signed and is re-signed here with the upload key. Credentials are read
# from a properties file and never echoed.
set -euo pipefail

CODE="${1:?usage: release.sh <versionCode> <versionName>}"
NAME="${2:?usage: release.sh <versionCode> <versionName>}"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity}"
SIGNING="${EMBERLINE_SIGNING:-/Users/prashant/StudioProjects/emberline-unity/signing/signing.properties}"
OUT="Builds/emberline-${NAME}-code${CODE}.aab"

[ -f "$SIGNING" ] || { echo "no signing properties at $SIGNING" >&2; exit 1; }
prop() { grep "^$1=" "$SIGNING" | cut -d= -f2- | tr -d '\r\n'; }
KEYSTORE=$(prop keystore); ALIAS=$(prop alias)
STOREPASS=$(prop storepass); KEYPASS=$(prop keypass)
[ -f "$KEYSTORE" ] || { echo "keystore missing: $KEYSTORE" >&2; exit 1; }

echo "==> building $NAME (code $CODE)"
EMBERLINE_VERSION_CODE="$CODE" EMBERLINE_VERSION_NAME="$NAME" \
  "$UNITY" -batchmode -quit -projectPath "$PWD" \
  -executeMethod Emberline.EditorTools.EmberlineBootstrap.BuildPlayStoreAab \
  -logFile Logs/aab.log
grep -q "AAB build SUCCEEDED" Logs/aab.log || { echo "build failed; see Logs/aab.log" >&2; exit 1; }

echo "==> signing"
cp Builds/emberline.aab "$OUT"
# Strip Unity's debug signature so the upload key is the only signer.
zip -q -d "$OUT" 'META-INF/*.SF' 'META-INF/*.RSA' 'META-INF/*.DSA' 2>/dev/null || true
jarsigner -keystore "$KEYSTORE" -storepass "$STOREPASS" -keypass "$KEYPASS" \
  -sigalg SHA256withRSA -digestalg SHA-256 "$OUT" "$ALIAS" >/dev/null

echo "==> verifying"
jarsigner -verify "$OUT" | grep -q "jar verified" || { echo "SIGNATURE INVALID" >&2; exit 1; }
echo "signer: $(keytool -printcert -jarfile "$OUT" 2>/dev/null | grep -m1 'SHA256:' | tr -d '\t ')"
ls -lh "$OUT"
echo "==> done. Confirm $CODE exceeds the highest code already on Play before uploading."
