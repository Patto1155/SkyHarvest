#!/usr/bin/env bash
# Launch the Windows standalone build. Rebuilds when scripts are newer than the DLL.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXE="$ROOT/Builds/Windows/SkyHarvest.exe"
DLL="$ROOT/Builds/Windows/SkyHarvest_Data/Managed/SkyHarvest.dll"
BUILD_SH="$ROOT/tools/build.sh"

RES_ASSETS="$ROOT/Builds/Windows/SkyHarvest_Data/resources.assets"

needs_rebuild() {
  if [[ ! -f "$DLL" ]]; then return 0; fi
  if find "$ROOT/Assets/Scripts" -name '*.cs' -newer "$DLL" -print -quit | grep -q .; then return 0; fi
  if [[ ! -f "$RES_ASSETS" ]]; then return 0; fi
  if find "$ROOT/Assets/Resources" \( -name '*.png' -o -name '*.jpg' \) -newer "$RES_ASSETS" -print -quit | grep -q .; then
    return 0
  fi
  return 1
}

if [[ "${1:-}" == "--no-build" ]]; then
  shift
elif needs_rebuild; then
  echo "Assets newer than build (or no build) — rebuilding..."
  bash "$BUILD_SH"
fi

if [[ ! -f "$EXE" ]]; then
  echo "No build at $EXE — run: bash tools/build.sh"
  exit 1
fi

echo "Launching $EXE"
cd "$ROOT/Builds/Windows"
./SkyHarvest.exe "$@" &
