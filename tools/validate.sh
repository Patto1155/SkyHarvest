#!/usr/bin/env bash
# SkyHarvest pre-handoff validation — run before returning work to the user.
#   bash tools/validate.sh
# Steps: (1) tools/check.sh via .NET 8 harness if SDK found, (2) Unity batch compile,
# (3) Unity EditMode tests (73 NUnit tests). Logs → artifacts/ (gitignored).
# Flags: --skip-check | --skip-unity-compile | --skip-unity-tests
# Env: UNITY_PATH, DOTNET_PATH (optional overrides)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS="$ROOT/artifacts"

SKIP_CHECK=0
SKIP_UNITY_COMPILE=0
SKIP_UNITY_TESTS=0
for arg in "$@"; do
  case "$arg" in
    --skip-check) SKIP_CHECK=1 ;;
    --skip-unity-compile) SKIP_UNITY_COMPILE=1 ;;
    --skip-unity-tests) SKIP_UNITY_TESTS=1 ;;
    -h|--help)
      sed -n '1,8p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown flag: $arg (try --help)" >&2
      exit 2
      ;;
  esac
done

mkdir -p "$ARTIFACTS"

FAILURES=0
step_ok() { echo "  ✓ $1"; }
step_fail() { echo "  ✗ $1" >&2; FAILURES=$((FAILURES + 1)); }

# ── .NET 8 discovery (PATH dotnet may be older than user-local install) ───────
find_dotnet8() {
  if [[ -n "${DOTNET_PATH:-}" && -x "$DOTNET_PATH" ]]; then
    echo "$DOTNET_PATH"
    return 0
  fi
  local candidates=()
  if command -v dotnet >/dev/null 2>&1; then
    candidates+=("$(command -v dotnet)")
  fi
  candidates+=(
    "${HOME}/.dotnet/dotnet"
    "${USERPROFILE:-}/.dotnet/dotnet"
    "/c/Program Files/dotnet/dotnet"
    "/usr/local/share/dotnet/dotnet"
  )
  local c
  for c in "${candidates[@]}"; do
    [[ -n "$c" && -x "$c" ]] || continue
    if "$c" --list-sdks 2>/dev/null | grep -qE '^8\.'; then
      echo "$c"
      return 0
    fi
  done
  return 1
}

# ── Unity discovery ───────────────────────────────────────────────────────────
find_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "$UNITY_PATH" ]]; then
    echo "$UNITY_PATH"
    return 0
  fi
  local candidates=(
    "/d/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe"
    "D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe"
    "/Applications/Unity/Hub/Editor/2022.3.45f1/Unity.app/Contents/MacOS/Unity"
  )
  local c
  for c in "${candidates[@]}"; do
    if [[ -x "$c" ]]; then
      echo "$c"
      return 0
    fi
  done
  return 1
}

unity_to_path() {
  # Unity accepts forward slashes on Windows; normalize for log readability.
  echo "$1" | sed 's|\\|/|g'
}

ROOT_UNITY="$(unity_to_path "$ROOT")"

echo "=== SkyHarvest validate.sh ==="
echo "Project: $ROOT_UNITY"
echo ""

# ── 1. CLR harness (tools/check.sh) ───────────────────────────────────────────
if [[ "$SKIP_CHECK" -eq 1 ]]; then
  echo "[1/3] CLR harness — SKIPPED (--skip-check)"
else
  echo "[1/3] CLR harness (tools/check.sh)..."
  if DOTNET8="$(find_dotnet8)"; then
    export PATH="$(dirname "$DOTNET8"):$PATH"
    echo "  Using dotnet: $DOTNET8 ($("$DOTNET8" --version))"
    if bash "$SCRIPT_DIR/check.sh"; then
      step_ok "CLR harness: compile + 73 NUnit tests"
    else
      step_fail "CLR harness failed (see output above)"
    fi
  else
    echo "  SKIP: .NET 8 SDK not found (install from https://dotnet.microsoft.com/download or set DOTNET_PATH)"
  fi
fi
echo ""

# ── 2. Unity batch compile ────────────────────────────────────────────────────
if [[ "$SKIP_UNITY_COMPILE" -eq 1 ]]; then
  echo "[2/3] Unity compile — SKIPPED (--skip-unity-compile)"
else
  echo "[2/3] Unity batch compile..."
  COMPILE_LOG="$ARTIFACTS/unity-compile.log"
  if UNITY_BIN="$(find_unity)"; then
    echo "  Using Unity: $UNITY_BIN"
    set +e
    "$UNITY_BIN" \
      -batchmode \
      -nographics \
      -quit \
      -projectPath "$ROOT_UNITY" \
      -logFile "$COMPILE_LOG"
    UNITY_EXIT=$?
    set -e
    if [[ "$UNITY_EXIT" -ne 0 ]]; then
      step_fail "Unity exited with code $UNITY_EXIT (see $COMPILE_LOG)"
    elif grep -qE 'error CS[0-9]+:' "$COMPILE_LOG"; then
      step_fail "Unity script compile errors (grep 'error CS' in $COMPILE_LOG)"
      grep -E 'error CS[0-9]+:' "$COMPILE_LOG" | head -20 >&2 || true
    elif ! grep -q 'Exiting batchmode successfully' "$COMPILE_LOG"; then
      step_fail "Unity compile did not finish cleanly (see $COMPILE_LOG)"
    else
      step_ok "Unity compile (no CS errors)"
    fi
  else
    step_fail "Unity editor not found (set UNITY_PATH to Unity.exe)"
  fi
fi
echo ""

# ── 3. Unity EditMode tests ───────────────────────────────────────────────────
if [[ "$SKIP_UNITY_TESTS" -eq 1 ]]; then
  echo "[3/3] Unity EditMode tests — SKIPPED (--skip-unity-tests)"
else
  echo "[3/3] Unity EditMode tests..."
  TEST_LOG="$ARTIFACTS/unity-editmode.log"
  TEST_RESULTS="$ARTIFACTS/unity-editmode-results.xml"
  rm -f "$TEST_RESULTS"
  if UNITY_BIN="$(find_unity)"; then
    # Do NOT pass -quit with -runTests; it exits before the test runner starts.
    set +e
    "$UNITY_BIN" \
      -runTests \
      -batchmode \
      -nographics \
      -projectPath "$ROOT_UNITY" \
      -testResults "$TEST_RESULTS" \
      -testPlatform editmode \
      -assemblyNames "SkyHarvest.EditModeTests" \
      -logFile "$TEST_LOG"
    UNITY_EXIT=$?
    set -e
    if [[ "$UNITY_EXIT" -ne 0 ]]; then
      step_fail "Unity test run exited with code $UNITY_EXIT (see $TEST_LOG)"
    elif [[ ! -f "$TEST_RESULTS" ]]; then
      step_fail "Unity test results missing (see $TEST_LOG)"
    elif grep -qE 'failed="[1-9][0-9]*"' "$TEST_RESULTS"; then
      step_fail "Unity EditMode tests failed (see $TEST_RESULTS)"
    elif ! grep -q 'result="Passed"' "$TEST_RESULTS"; then
      step_fail "Unity EditMode tests did not pass (see $TEST_RESULTS)"
    else
      TOTAL="$(grep -oE 'testcasecount="[0-9]+"' "$TEST_RESULTS" | head -1 | grep -oE '[0-9]+' || echo "?")"
      step_ok "Unity EditMode tests ($TOTAL passed)"
    fi
  else
    step_fail "Unity editor not found (set UNITY_PATH to Unity.exe)"
  fi
fi
echo ""

if [[ "$FAILURES" -gt 0 ]]; then
  echo "=== validate.sh FAILED ($FAILURES step(s)) ===" >&2
  exit 1
fi

echo "=== validate.sh PASSED ==="
