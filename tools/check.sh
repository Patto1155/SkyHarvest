#!/usr/bin/env bash
# SkyHarvest headless verification harness.
# Compiles all C# under Assets/Scripts + Assets/Tests against UnityEngine stubs
# (.NET 8) and runs the NUnit test suite. Exits nonzero on any error or failure.
# Usage: ./tools/check.sh [--no-restore]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR/.."

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
