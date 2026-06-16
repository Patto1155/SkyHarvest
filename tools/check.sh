#!/usr/bin/env bash
# SkyHarvest headless verification harness.
# Compiles all C# under Assets/Scripts + Assets/Tests against UnityEngine stubs
# (.NET 8) and runs the NUnit test suite. Exits nonzero on any error or failure.
# Usage: ./tools/check.sh [--no-restore]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR/.."

# Self-locate the .NET 8 SDK. The machine has BOTH 3.1.201 (C:\Program Files\dotnet,
# on PATH) and 8.0.x (~/.dotnet, NOT on PATH by default). The stub projects target
# net8.0, so 3.1 fails with MSB3644 — a past session wrongly concluded ".NET 8 isn't
# installed". Prefer ~/.dotnet so this works regardless of PATH.
DOTNET="$HOME/.dotnet/dotnet.exe"
if [[ ! -x "$DOTNET" ]]; then
  DOTNET="$(command -v dotnet || true)"
fi
if [[ -z "$DOTNET" ]]; then
  echo "ERROR: no dotnet found (looked for ~/.dotnet/dotnet.exe and PATH)." >&2
  exit 1
fi
dotnet() { "$DOTNET" "$@"; }

STUBS="$SCRIPT_DIR/clr-harness/UnityStubs/UnityStubs.csproj"
GAME="$SCRIPT_DIR/clr-harness/GameCode/GameCode.csproj"
TESTS="$SCRIPT_DIR/clr-harness/Tests/Tests.csproj"

RESTORE_FLAG=""
if [[ "${1-}" == "--no-restore" ]]; then
  RESTORE_FLAG="--no-restore"
fi

echo "=== SkyHarvest check.sh: building UnityStubs ==="
dotnet build "$STUBS" $RESTORE_FLAG -c Debug -v quiet

echo "=== Building GameCode ==="
dotnet build "$GAME" $RESTORE_FLAG -c Debug -v quiet

echo "=== Running tests ==="
dotnet test "$TESTS" $RESTORE_FLAG -c Debug --no-build --no-restore --logger "console;verbosity=normal"

echo ""
echo "=== check.sh PASSED ==="
