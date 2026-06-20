#!/usr/bin/env bash
# Auto-rebuild and restart the Windows build when scripts or stair sprites change.
#
# Usage:
#   bash tools/dev-watch.sh          # build once, launch, then watch
#   bash tools/dev-watch.sh --no-build   # skip initial build if DLL is fresh
#
# Not true hot-reload (the .exe must restart), but you edit → save → ~30s later
# the game comes back. Faster iteration than manually re-running tools/run.sh.
#
# For instant C# iteration, use the Unity Editor Play button instead (see WORKFLOW.md).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXE="$ROOT/Builds/Windows/SkyHarvest.exe"
DLL="$ROOT/Builds/Windows/SkyHarvest_Data/Managed/SkyHarvest.dll"
POLL_SECS="${POLL_SECS:-2}"
STAMP_FILE="$ROOT/artifacts/.dev-watch-stamp"
LOCK_FILE="$ROOT/artifacts/.dev-watch.lock"

mkdir -p "$ROOT/artifacts"

acquire_lock() {
  if [[ -f "$LOCK_FILE" ]]; then
    local old_pid
    old_pid="$(cat "$LOCK_FILE" 2>/dev/null || true)"
    if [[ -n "$old_pid" ]] && tasklist //FI "PID eq $old_pid" 2>/dev/null | grep -q "$old_pid"; then
      echo "[dev-watch] Already running (pid $old_pid). Stop it first:"
      echo "  taskkill //F //PID $old_pid"
      exit 1
    fi
  fi
  echo $$ > "$LOCK_FILE"
}

release_lock() {
  rm -f "$LOCK_FILE"
}

acquire_lock

kill_game() {
  taskkill //IM SkyHarvest.exe //F >/dev/null 2>&1 || true
  sleep 0.5
}

RES_ASSETS="$ROOT/Builds/Windows/SkyHarvest_Data/resources.assets"

needs_rebuild() {
  if [[ ! -f "$DLL" ]]; then return 0; fi
  if find "$ROOT/Assets/Scripts" -name '*.cs' -newer "$DLL" -print -quit | grep -q .; then return 0; fi
  if [[ ! -f "$RES_ASSETS" ]]; then return 0; fi
  if find "$ROOT/Assets/Resources" \( -name '*.png' -o -name '*.jpg' -o -name '*.json' \) -newer "$RES_ASSETS" -print -quit | grep -q .; then
    return 0
  fi
  if [[ -f "$ROOT/Assets/StreamingAssets/stair_cutoutv2.png" && "$ROOT/Assets/StreamingAssets/stair_cutoutv2.png" -nt "$RES_ASSETS" ]]; then
    return 0
  fi
  if [[ -f "$ROOT/Assets/StreamingAssets/stair_cutout.png" && "$ROOT/Assets/StreamingAssets/stair_cutout.png" -nt "$RES_ASSETS" ]]; then
    return 0
  fi
  return 1
}

rebuild_if_needed() {
  if needs_rebuild; then
    echo "[dev-watch] Assets changed — rebuilding..."
    bash "$ROOT/tools/build.sh"
    return 0
  fi
  return 1
}

launch_game() {
  if [[ ! -f "$EXE" ]]; then
    echo "[dev-watch] No build at $EXE — run: bash tools/build.sh"
    exit 1
  fi
  echo "[dev-watch] Launching $EXE --dev"
  cd "$ROOT/Builds/Windows"
  ./SkyHarvest.exe --dev "$@" &
}

touch_stamp() {
  : > "$STAMP_FILE"
  find "$ROOT/Assets/Scripts" -name '*.cs' -print0 2>/dev/null >> "$STAMP_FILE" || true
  find "$ROOT/Assets/Resources/Sprites" \( -name '*.png' -o -name '*.jpg' \) -print0 2>/dev/null >> "$STAMP_FILE" || true
}

files_changed() {
  [[ ! -f "$STAMP_FILE" ]] && return 0
  if find "$ROOT/Assets/Scripts" -name '*.cs' -newer "$STAMP_FILE" -print -quit | grep -q .; then
    return 0
  fi
  if find "$ROOT/Assets/Resources/Sprites" \( -name '*.png' -o -name '*.jpg' \) -newer "$STAMP_FILE" -print -quit | grep -q .; then
    return 0
  fi
  if [[ -f "$ROOT/Assets/StreamingAssets/stair_cutoutv2.png" && "$ROOT/Assets/StreamingAssets/stair_cutoutv2.png" -nt "$STAMP_FILE" ]]; then
    return 0
  fi
  if [[ -f "$ROOT/Assets/StreamingAssets/stair_cutout.png" && "$ROOT/Assets/StreamingAssets/stair_cutout.png" -nt "$STAMP_FILE" ]]; then
    return 0
  fi
  if [[ -f "$ROOT/Assets/StreamingAssets/carved_stair_face.png" && "$ROOT/Assets/StreamingAssets/carved_stair_face.png" -nt "$STAMP_FILE" ]]; then
    return 0
  fi
  return 1
}

if [[ "${1:-}" == "--no-build" ]]; then
  shift
else
  rebuild_if_needed || true
fi

kill_game
launch_game "$@"
touch_stamp

echo "[dev-watch] Watching Assets/Scripts and Assets/Resources/Sprites (poll every ${POLL_SECS}s). Ctrl+C to stop."

trap 'kill_game; release_lock; exit 0' INT TERM

while true; do
  sleep "$POLL_SECS"
  if files_changed; then
    echo "[dev-watch] Change detected — restarting..."
    kill_game
    rebuild_if_needed || true
    launch_game "$@"
    touch_stamp
  fi
done
