#!/usr/bin/env bash
# Launch the Windows standalone build. Rebuilds when scripts are newer than the DLL.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXE="$ROOT/Builds/Windows/SkyHarvest.exe"
DLL="$ROOT/Builds/Windows/SkyHarvest_Data/Managed/SkyHarvest.dll"
BUILD_SH="$ROOT/tools/build.sh"

if [[ "${1:-}" == "--no-build" ]]; then
  shift
elif [[ ! -f "$DLL" ]] || find "$ROOT/Assets/Scripts" -name '*.cs' -newer "$DLL" -print -quit | grep -q .; then
  echo "Scripts newer than build (or no build) — rebuilding..."
  bash "$BUILD_SH"
fi

if [[ ! -f "$EXE" ]]; then
  echo "No build at $EXE — run: bash tools/build.sh"
  exit 1
fi

echo "Launching $EXE"
cd "$ROOT/Builds/Windows"
./SkyHarvest.exe "$@" &
