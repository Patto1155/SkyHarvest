#!/usr/bin/env bash
# Capture a single in-game screenshot of the carved stair boundary.
# Uses Unity Play mode (GUI) — run with ~8 min timeout.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY="${UNITY_PATH:-/d/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe}"
LOG="$ROOT/artifacts/stair_shot.log"
OUT="$ROOT/artifacts/screenshots/stair_verify.png"
DISMISS="$ROOT/tools/dismiss-unity-admin-dialog.ps1"

mkdir -p "$ROOT/artifacts/screenshots"
rm -f "$OUT" "$LOG"

powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1 || true
rm -f "$ROOT/Temp/UnityLockfile" 2>/dev/null || true

powershell -NoProfile -File "$DISMISS" -TimeoutSeconds 200 > "$ROOT/artifacts/dismiss.log" 2>&1 &
DISMISS_PID=$!

"$UNITY" -projectPath "$ROOT" -executeMethod PlayModeStairShot.Run -logFile "$LOG" &
UNITY_PID=$!

( sleep 330
  powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
) &
WATCHDOG_PID=$!

wait "$UNITY_PID" 2>/dev/null
UNITY_EXIT=$?
kill "$WATCHDOG_PID" 2>/dev/null || true
kill "$DISMISS_PID" 2>/dev/null || true

if grep -q "error CS" "$LOG" 2>/dev/null; then
  echo "FAIL: compile error — see $LOG"
  exit 1
fi

if [[ -f "$OUT" ]]; then
  echo "PASS: $OUT"
  exit 0
fi

echo "FAIL: no screenshot — see $LOG (exit $UNITY_EXIT)"
grep -E "\[StairShot\]|error|Error" "$LOG" 2>/dev/null | tail -20 || true
exit 1
