#!/usr/bin/env bash
# Run the live PlayModeVerify harness (GUI editor + admin-dialog dismisser).
# Drives every feature loop in Play mode and writes artifacts/verify/verify_report.md.
# Mirrors shot.sh's launch pattern. Run with a generous tool timeout (~8 min).
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY="${UNITY_PATH:-/d/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe}"
LOG="$ROOT/artifacts/verify.log"
DISMISS="$ROOT/tools/dismiss-unity-admin-dialog.ps1"
REPORT="$ROOT/artifacts/verify/verify_report.md"
WATCHDOG_SECS=360

mkdir -p "$ROOT/artifacts/verify"
rm -f "$LOG" "$REPORT"

powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1 || true
rm -f "$ROOT/Temp/UnityLockfile" 2>/dev/null || true

powershell -NoProfile -File "$DISMISS" -TimeoutSeconds 300 > "$ROOT/artifacts/dismiss_verify.log" 2>&1 &
DISMISS_PID=$!

"$UNITY" -projectPath "$ROOT" -executeMethod PlayModeVerify.Run -logFile "$LOG" &
UNITY_PID=$!

( sleep "$WATCHDOG_SECS"
  powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
) &
WATCHDOG_PID=$!

wait "$UNITY_PID" 2>/dev/null
UNITY_EXIT=$?
kill "$WATCHDOG_PID" 2>/dev/null || true
kill "$DISMISS_PID" 2>/dev/null || true

if grep -q "error CS" "$LOG" 2>/dev/null; then
  echo "FAIL: script compile errors"
  grep "error CS" "$LOG" | head -5
  exit 1
fi

if [ -f "$REPORT" ]; then
  echo "PASS: $REPORT (Unity exit $UNITY_EXIT)"
  exit 0
fi
echo "FAIL: no report produced (Unity exit $UNITY_EXIT)"
tail -20 "$LOG" 2>/dev/null
exit 1
