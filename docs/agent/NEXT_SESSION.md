# Next session — task list

Execute in order. Branch off `main` (or continue on `fix/terrain-and-build-pipeline` if PR #2 is still open), validate, push to a branch, open/update a PR. Run `bash tools/check.sh` after every C# change; run the Play-mode screenshot verify (WORKFLOW.md) before claiming visual work done.

## 1. Camera zoom — make the avatar visible

- Problem: `Bootstrap.cs:75` sets `cam.orthographicSize = 4f`, which frames the whole island so the 1-unit avatar is tiny.
- Do: lower the default (try `2.5f`) AND add scroll-wheel zoom to `CameraFollow.cs` (currently follow-only, no zoom — spec wants zoom in/out, fixed rotation). Clamp e.g. `[2, 6]` via `Input.mouseScrollDelta.y`.
- Verify: screenshot run; avatar should read clearly. Haiku-review the frames.

## 2. Staged building (deliver materials → construct over time)

- Current: `BuildModeController.TryPlace` consumes materials and spawns the finished structure in one click.
- Spec (design §2 "Blueprint ghost system"): place a translucent ghost → deliver materials → construction completes.
- Do: add a `Constructing` state to `Structure`/`StructureRegistry` — place ghost at 0 materials, accept deliveries (reuse player inventory on interact `E`), swap to finished sprite when costs met. Keep instant-place behind a debug flag if useful for testing.
- Add EditMode tests for the construction state machine (mirror existing `Assets/Tests/EditMode` style).

## 3. Fix the stale `GameDatabase` comment

- `Assets/Scripts/Data/GameDatabase.cs` header still says "PLACEHOLDER … real implementation will replace this file." It IS the real data. Rewrite the header to describe it as the authoritative code-defined database. ~5 min, no logic change.

## 4. Verify ALL controls / features work in a live game

Goal: confirm each binding actually does its thing in Play mode, not just compiles. Extend `PlayModeScreenshots.cs` to simulate inputs (or drive systems directly in a Play-mode test) and screenshot before/after. Check off each:

| Control | Expected | Where |
|---|---|---|
| WASD / arrows | player walks, faces direction, walk anim | `PlayerController.cs` (`GetAxisRaw`) |
| `1`–`6` | select hotbar/tool slot | `ToolSystem.cs:30`, `PlayerController.cs:133` |
| `E` | interact with current target (harvest/scavenge/open workshop/collect Skynet) | `InteractionSystem.cs:48` |
| `Q` | open inspector panel on looked-at plot/structure | `InspectorPanel.cs:73` |
| `B` | toggle build mode; ghost follows mouse; left-click places if valid+affordable | `BuildModeController.cs:49/65` |
| `Tab` | OVERLOADED — closes storage, toggles tooltip, (inventory?). **Check for conflict.** | `StorageUI.cs:50`, `ContextualTooltipUI.cs:42`, `InventoryUI` |
| `Esc` | pause menu; also closes open menus | `Bootstrap.cs:453`, menu UIs |
| Build menu | Up/Down select, Enter confirm, Esc cancel | `BuildMenuUI.cs:56-60` |
| Mouse L | place structure in build mode | `BuildModeController.cs:65` |

Feature loops to exercise end-to-end (plant→water→grow→harvest; load workshop→process→collect; weather changes affect crops; debris lands→scavenge; Skynet catches; save→reload restores state). Report a pass/fail table; file the failures as the next task list.

## Done-criteria for the session

`tools/validate.sh` green, screenshots show steps 1–2 working, a controls pass/fail table exists, PR updated. Then refresh `docs/agent/START_HERE.md` "Current state" + this file so the *next* session starts clean.
