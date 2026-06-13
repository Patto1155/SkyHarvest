# Agent handoff — read this first

Low-context entry point for continuing SkyHarvest development. Read these 4 files (all short), then start:

1. **START_HERE.md** (this) — state + read order.
2. **WORKFLOW.md** — exact commands to compile, test, run the game, build, and screenshot-verify. The Unity admin-dialog gotcha lives here.
3. **NEXT_SESSION.md** — the prioritized task list to execute.
4. **MAP.md** — terse file/system map so you don't re-explore.

Don't read the 100KB MVP plan or full design spec unless a task needs a specific detail. `docs/IMPLEMENTATION_NOTES.md` explains why this is headless-built (code-constructed scene, no prefabs/ScriptableObjects).

## Current state (2026-06-13)

- Game is functional: main menu → New Game generates a varied procedural island; player, HUD, hotbar render. All MVP systems exist in code (farming loop, 3 workshops, weather, debris+Skynet, storage, save/load, expansion).
- **Terrain-gap bug: FIXED** (PR #2). Was sprite import settings (NPOT/mips/compression) corrupting `Sprite.Create` rects. 71 `.meta` files fixed + `SpriteImportPostprocessor` enforces it.
- **Windows build works**: `BuildScript.BuildWindows` → `Builds/Windows/SkyHarvest.exe` (v1.0.0), launches clean.
- **Verification harness works**: Play-mode screenshots + admin-dialog auto-dismiss + Haiku review (see WORKFLOW).
- Open PR: **#2** `fix/terrain-and-build-pipeline`. Push goes to a branch + PR, never direct to `main`.

## Known gaps (tracked in NEXT_SESSION)

- Camera has no zoom; `orthographicSize = 4f` shows the whole island so the avatar looks tiny.
- Building is instant (ghost → consume materials → spawn finished), not the spec's staged "deliver materials → construct over time".
- `GameDatabase.cs` header says "placeholder" but it IS the real populated data — just fix the comment.
- Controls/features not yet end-to-end verified in a live session.
