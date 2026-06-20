# Validation pipeline

SkyHarvest ships a single pre-handoff script that runs automated checks agents (and humans) should pass before claiming work is done.

## Quick start

From the repo root (Git Bash or WSL on Windows):

```bash
bash tools/validate.sh
```

Success ends with:

```
=== validate.sh PASSED ===
```

Failure exits with code `1` and prints which step failed. Logs are written under `artifacts/` (gitignored).

## What it runs

| Step | Script / tool | What it proves |
|------|----------------|----------------|
| 1 | `tools/check.sh` | Game C# compiles against Unity stubs on **.NET 8**; **134+** NUnit logic tests pass (no Unity editor required). |
| 2 | Unity batch mode | Project opens headlessly; **no `error CS` compile errors** in the real Unity compiler. |
| 3 | Unity EditMode tests | Same tests run inside Unity 2022.3.45f1 via `SkyHarvest.EditModeTests`. |

Agent guide to all tiers (verify, visual shot, dev mode): **`docs/agent/TESTING.md`**.

Step 1 is fast (~10s) and is what you want for tight edit/test loops. Steps 2–3 catch Unity-only issues (asmdefs, `#if UNITY_EDITOR`, real test runner) that the CLR harness cannot see.

## Requirements

| Requirement | Notes |
|-------------|--------|
| **.NET 8 SDK** | Required for step 1. Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download). The script auto-detects `~/.dotnet/dotnet` if PATH has an older SDK. |
| **Unity 2022.3.45f1** | Required for steps 2–3. Default path on this machine: `D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe`. |
| **Bash** | Git Bash or WSL on Windows; native bash on macOS/Linux. |

Override discovery with environment variables:

```bash
export DOTNET_PATH="/c/Users/you/.dotnet/dotnet"
export UNITY_PATH="D:/Unity/Hub/Editor/2022.3.45f1/Editor/Unity.exe"
bash tools/validate.sh
```

## Flags

```bash
bash tools/validate.sh --help
```

| Flag | Effect |
|------|--------|
| `--skip-check` | Skip CLR harness (steps 2–3 only). |
| `--skip-unity-compile` | Skip Unity batch compile. |
| `--skip-unity-tests` | Skip Unity EditMode tests. |

Examples:

```bash
# Unity-only (no .NET 8 installed)
bash tools/validate.sh --skip-check

# Fast compile check after a C# change
bash tools/validate.sh --skip-check --skip-unity-tests

# Logic tests only (no Unity installed)
bash tools/check.sh
```

## Logs and artifacts

| File | Contents |
|------|----------|
| `artifacts/unity-compile.log` | Full Unity log from batch compile. |
| `artifacts/unity-editmode.log` | Full Unity log from test run. |
| `artifacts/unity-editmode-results.xml` | NUnit XML results (pass/fail counts). |

On failure, grep compile errors:

```bash
grep 'error CS' artifacts/unity-compile.log
```

## What validation does **not** cover

Still verify manually in the Editor (**Play**):

- Sprites, terrain tiling, camera framing
- Main menu → New Game / Continue / seed input
- Input feel, tool hotkeys, build mode UX
- Audio levels and weather VFX
- Save/load in a real play session

The harness tests **logic** (inventory, crops, weather, saves, recipes, etc.), not rendering or UI layout.

## Troubleshooting

### `.NET 8 SDK not found` (step 1 skipped)

Install the SDK or set `DOTNET_PATH`. Validation continues with Unity steps; install .NET 8 before relying on step 1.

### `Unity editor not found`

Set `UNITY_PATH` to your `Unity.exe`. Version should match the project (**2022.3.45f1**).

### Unity lock / multiple instances

Close other Unity windows, or kill `Unity.exe` and remove `Temp/UnityLockfile`, then re-run.

### EditMode tests fail but `check.sh` passes

Usually an asmdef reference, `#if UNITY_EDITOR` guard, or Unity-specific API. Read `artifacts/unity-editmode.log` and fix the failing test or production code.

### Batch compile fails with exit code ≠ 0

Open `artifacts/unity-compile.log`. Script errors appear as `error CS####:`.

## When to run

- **Before opening a PR** or handing work to another person.
- **After changing C#** under `Assets/Scripts/` or `Assets/Tests/`.
- **After asmdef / package / Unity version changes** (always run full `validate.sh`).
- **During development** — use `tools/check.sh` alone for quick feedback; run full validation before merge.

## Related

- `tools/check.sh` — CLR harness only (documented in script header).
- `docs/IMPLEMENTATION_NOTES.md` — headless-dev context and plan deviations.
