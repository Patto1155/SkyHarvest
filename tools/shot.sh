#!/usr/bin/env bash
# Quiet single-image visual iteration shot for SkyHarvest.
#
# Launches the GUI editor, runs PlayModeContactSheet.Run (wide + detail framings
# stitched into ONE png), auto-dismisses the "running as administrator" modal, and
# prints ONLY the contact-sheet path + PASS/FAIL. Designed for a tight cozy-pass
# loop: edit StreamingAssets/visual.json, re-run this, Read the one image, repeat.
#
# Screenshots need a real GPU context, so this is a GUI launch (NOT -batchmode).
# A watchdog kills Unity if it hangs. Run with a generous tool timeout (~8 min).
#
# Usage: bash tools/shot.sh
# Env:   UNITY_PATH overrides the editor path.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY="${UNITY_PATH:-/d/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe}"
LOG="$ROOT/artifacts/contact.log"
SHEET="$ROOT/artifacts/screenshots/contact_sheet.png"
DISMISS="$ROOT/tools/dismiss-unity-admin-dialog.ps1"
WATCHDOG_SECS=330

mkdir -p "$ROOT/artifacts/screenshots"
rm -f "$SHEET" "$LOG"

# 1. Kill any stale Unity + clear the project lockfile so the launch isn't blocked.
powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1 || true
rm -f "$ROOT/Temp/UnityLockfile" 2>/dev/null || true

# 2. Background the admin-dialog dismisser (covers editor startup window).
powershell -NoProfile -File "$DISMISS" -TimeoutSeconds 200 > "$ROOT/artifacts/dismiss.log" 2>&1 &
DISMISS_PID=$!

# 3. Launch the GUI editor running the contact-sheet method.
"$UNITY" -projectPath "$ROOT" -executeMethod PlayModeContactSheet.Run -logFile "$LOG" &
UNITY_PID=$!

# 4. Watchdog: hard-kill Unity if it overruns.
( sleep "$WATCHDOG_SECS"
  powershell -NoProfile -Command "Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
) &
WATCHDOG_PID=$!

wait "$UNITY_PID" 2>/dev/null
UNITY_EXIT=$?
kill "$WATCHDOG_PID" 2>/dev/null || true
kill "$DISMISS_PID" 2>/dev/null || true

# 5. Quiet result — path + PASS/FAIL only.
if grep -q "error CS" "$LOG" 2>/dev/null; then
  echo "FAIL: script compile errors"
  grep "error CS" "$LOG" | head -5
  exit 1
fi
if [ -f "$SHEET" ]; then
  echo "PASS: $SHEET"
  exit 0
fi
echo "FAIL: no contact sheet produced (Unity exit $UNITY_EXIT)"
echo "--- tail $LOG ---"
tail -20 "$LOG" 2>/dev/null
exit 1
