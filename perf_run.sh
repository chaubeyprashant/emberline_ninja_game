#!/usr/bin/env bash
# Record the perf baseline on a connected phone (docs/PERF_BASELINE.md).
#
#   ./perf_run.sh --build            build the development APK, then run
#   ./perf_run.sh                    run with the APK already in Builds/
#   ./perf_run.sh --fps 60           lift the 30 fps governor cap for the run
#   ./perf_run.sh --quick            short samples, to check the plumbing
#
# The game tours the main menu, every landmark and an eight-enemy fight on its
# own (Assets/Scripts/Debug/PerfRun.cs) and logs one JSON line per scenario.
# This script installs, launches, collects those lines from logcat into
# Logs/perf_<date>.jsonl and prints them as a table. The run restores the
# phone's explore save when it finishes. It never uninstalls anything.
set -euo pipefail
cd "$(dirname "$0")"

ADB="${ADB:-$HOME/Library/Android/sdk/platform-tools/adb}"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity}"
PKG=com.ergebins.emberline3d
ACT=com.unity3d.player.UnityPlayerGameActivity
APK=Builds/emberline-dev.apk

build=0; opts=1; limit=1500
while [ $# -gt 0 ]; do
  case "$1" in
    --build) build=1 ;;
    --fps) opts="$opts,fps=${2:?--fps needs a number}"; shift ;;
    --quick) opts="$opts,quick"; limit=600 ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
  shift
done

mkdir -p Logs
if [ "$build" = 1 ]; then
  echo "==> building $APK (development)"
  # Unity sometimes finishes and then hangs on exit, so it runs under a watchdog
  # and the log line decides success, not the exit code.
  perl -e 'alarm shift @ARGV; exec @ARGV' 3600 "$UNITY" -batchmode -quit -projectPath "$PWD" \
    -buildTarget Android -executeMethod EmberBuild.BuildAndroidDevelopment \
    -logFile Logs/perf_build.log || true
  grep -q "Build succeeded" Logs/perf_build.log || { echo "build failed; see Logs/perf_build.log" >&2; exit 1; }
fi
[ -f "$APK" ] || { echo "no $APK; run with --build" >&2; exit 1; }

[ "$("$ADB" get-state </dev/null 2>/dev/null || true)" = device ] || { echo "no phone attached (adb devices)" >&2; exit 1; }
if "$ADB" shell dumpsys trust </dev/null | grep -q "deviceLocked=1"; then
  echo "the phone is locked; unlock it and run again" >&2; exit 1
fi

echo "==> installing"
if ! "$ADB" install -r "$APK" </dev/null; then
  cat >&2 <<'EOF'
install failed. INSTALL_FAILED_UPDATE_INCOMPATIBLE means the phone holds a build
signed with another key, and a downgrade error means its versionCode is higher.
Uninstalling would delete the save on the phone, so that is left to you.
EOF
  exit 1
fi

stamp=$(date +%Y%m%d_%H%M)
raw="Logs/perf_${stamp}_logcat.txt"
out="Logs/perf_${stamp}.jsonl"
"$ADB" logcat -c </dev/null
"$ADB" logcat -v raw -s Unity:V >"$raw" </dev/null &
logpid=$!
trap 'kill $logpid 2>/dev/null || true' EXIT

echo "==> running (options: $opts); keep the phone awake and untouched"
"$ADB" shell input keyevent KEYCODE_WAKEUP </dev/null
"$ADB" shell am start -S -n "$PKG/$ACT" --es perfrun "$opts" </dev/null >/dev/null

waited=0
until grep -q '"kind":"done"' "$raw" 2>/dev/null; do
  [ "$waited" -ge "$limit" ] && { echo "timed out after ${limit}s; partial results kept" >&2; break; }
  sleep 10; waited=$((waited + 10))
  printf '\r    %3ds, %d scenarios' "$waited" "$(grep -c '"kind":"scenario"' "$raw" 2>/dev/null || true)"
done
echo

grep -o '\[PERFRUN\] {.*}' "$raw" | sed 's/^\[PERFRUN\] //' >"$out" || true
"$ADB" shell am force-stop "$PKG" </dev/null

python3 -I - "$out" <<'PY'
import json, sys
rows = [json.loads(l) for l in open(sys.argv[1]) if l.strip()]
meta = next((r for r in rows if r["kind"] == "meta"), {})
done = next((r for r in rows if r["kind"] == "done"), None)
print(f"\n{meta.get('device','?')} · {meta.get('gpu','?')} · {meta.get('api','?')} · v{meta.get('version','?')}"
      f" · tier {meta.get('gfx_tier','?')} · cap {meta.get('fps_override') or meta.get('gameplay_fps','?')} fps"
      f" · frame timing {meta.get('frame_timing','?')}\n")
cols = [("name", "Scenario"), ("fps", "FPS"), ("p50_ms", "p50 ms"), ("p95_ms", "p95 ms"), ("p99_ms", "p99 ms"),
        ("hitches", "Hitches"), ("cpu_p95_ms", "CPU p95"), ("gpu_p95_ms", "GPU p95"), ("draw", "Draws"),
        ("setpass", "SetPass"), ("tris_k", "Tris k"), ("gc_kb_frame", "GC KB/f"), ("sys_mb", "Sys MB"),
        ("arrive_worst_ms", "Arrive ms")]
print("| " + " | ".join(h for _, h in cols) + " |")
print("|" + "---|" * len(cols))
for r in rows:
    if r["kind"] == "scenario":
        print("| " + " | ".join(str(round(r[k]) if isinstance(r[k], float) and k not in ("p50_ms", "p95_ms", "p99_ms", "cpu_p95_ms", "gpu_p95_ms") else r[k]) for k, _ in cols) + " |")
print("\nrun " + ("finished" if done and done["code"] == 0 else f"ended early: {done}" if done else "did not finish"))
PY
echo "results: $out"
