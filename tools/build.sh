#!/usr/bin/env bash
# Build the Windows standalone player (batchmode — no admin dialog).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY="${UNITY_PATH:-/d/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe}"
LOG="$ROOT/artifacts/build.log"
EXE="$ROOT/Builds/Windows/SkyHarvest.exe"

mkdir -p "$ROOT/artifacts"

echo "=== SkyHarvest build.sh ==="
"$UNITY" -batchmode -quit \
  -projectPath "$ROOT" \
  -executeMethod BuildScript.BuildWindows \
  -logFile "$LOG"

if grep -q "\[BuildScript\] BUILD OK" "$LOG"; then
  echo "BUILD OK -> $EXE"
else
  echo "BUILD FAILED — see $LOG"
  grep -E "error CS|BUILD FAILED|Error" "$LOG" | tail -20 || true
  exit 1
fi
