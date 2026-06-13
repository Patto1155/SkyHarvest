# Agent handoff — read this first

Low-context entry point for continuing SkyHarvest development. Read these 4 files (all short), then start:

1. **START_HERE.md** (this) — state + read order.
2. **WORKFLOW.md** — exact commands to compile, test, run the game, build, and screenshot-verify. The Unity admin-dialog gotcha lives here.
3. **NEXT_SESSION.md** — the prioritized task list to execute.
4. **MAP.md** — terse file/system map so you don't re-explore.

Also: **SCOPE_LEDGER.md** — spec/plan vs code gap list + keybind reconciliation table (kept current; update it when you close a gap).

Don't read the 100KB MVP plan or full design spec unless a task needs a specific detail. `docs/IMPLEMENTATION_NOTES.md` explains why this is headless-built (code-constructed scene, no prefabs/ScriptableObjects).

## Current state (2026-06-13, session 2)

- PR #2 (terrain fix + build pipeline) MERGED. Session-2 branch: `feat/session-2026-06-13` (PR #3).
- **All four NEXT_SESSION tasks from last session are DONE:**
  - Camera: default ortho 2.5, smooth scroll-wheel zoom [2,6] (`CameraFollow`).
  - Staged building per spec §2: `ConstructionSite`/`ConstructionProgress` — free ghost placement,
    E delivers materials, completes into the real structure; hammer cancels w/ full refund;
    saves persist in-progress sites; `BuildModeController.InstantBuild` = debug instant mode.
  - `GameDatabase` header fixed (it IS the real database).
  - **Live verification harness** `Assets/Editor/PlayModeVerify.cs` — drives every feature loop
    in Play mode, 19/19 PASS → `artifacts/verify/verify_report.md` + screenshots.
- **Major wiring bugs found via scope ledger and FIXED**: build mode was completely broken
  (BMC never got island/player on New Game; BuildMenuUI.Open had no caller; Tab never opened
  inventory; Esc double-fired close+pause). All hotkeys (B/Tab/Esc) now centralized in `Bootstrap.Update`.
- `tools/check.sh` = 82 NUnit green; `tools/validate.sh` green.
- ⚠ check.sh gotcha: it runs `dotnet test --no-build` — if you add/change test files, build
  `tools/clr-harness/Tests/Tests.csproj` once first or counts will be stale.

## Direction from Patrick (2026-06-13)

Game should look **pretty and feel cozy/peaceful**. Haiku screenshot review verdict: no rendering
bugs, but tone reads "cold grey industrial compound". Next session = visual warmth pass
(see NEXT_SESSION.md).
