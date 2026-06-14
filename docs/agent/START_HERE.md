# Agent handoff — read this first

Low-context entry point for continuing SkyHarvest development. Read these 4 files (all short), then start:

1. **START_HERE.md** (this) — state + read order.
2. **WORKFLOW.md** — exact commands to compile, test, run the game, build, and screenshot-verify. The Unity admin-dialog gotcha lives here.
3. **NEXT_SESSION.md** — the prioritized task list to execute.
4. **MAP.md** — terse file/system map so you don't re-explore.

Also: **SCOPE_LEDGER.md** — spec/plan vs code gap list + keybind reconciliation table (kept current; update it when you close a gap).

Don't read the 100KB MVP plan or full design spec unless a task needs a specific detail. `docs/IMPLEMENTATION_NOTES.md` explains why this is headless-built (code-constructed scene, no prefabs/ScriptableObjects).

## Current state (2026-06-14, session 5 — debris → skynet → expansion loop)

- **Session 5 (branch `feat/debris-loop`):** audited and closed the debris → skynet →
  island-expansion payoff loop (NEXT_SESSION #3). The chain was **already wired** end-to-end
  (debris spawns every 45s on rim cells → falls → E-scavenge → loot; Forge recipe
  `scrap→skynet_frame`; build menu lists scaffolding/skynet/forge; scaffolding→`Expand`→
  `RenderNewCells`; skynet→live accrual→save/restore). Audit found **two real defects, both fixed:**
  - **Skynet offline accrual was DEAD** — `InitializeOfflineAccrual` had zero runtime callers (only
    the verify harness called it directly, which is exactly why it falsely "passed" before). The
    "checked like a mailbox" catch-up never ran in a real game. Now wired into the save-restore path
    (`Bootstrap.RestoreIslandContents`); math extracted to pure, tested `SkynetAccrual`.
  - **`IslandExpandedEvent` was double-published** (both `IslandExpansion.Expand` and
    `BuildModeController.PlaceStructure`). Removed the controller duplicate.
  - Proven **live in Play mode**: new `PlayModeVerify` expansion step (`newCells=2, fired=1`) +
    existing skynet/debris steps → **20/20 PASS**. New launcher `tools/verify.sh`.
  - Validated green: **103 EditMode + 103 NUnit** (11 new: `ExpansionTests`, `SkynetAccrualTests`),
    Unity compile clean.

## Current state (2026-06-13, session 4 — unified hotbar)

- **Session 4 (branch `feat/scope-cleanup`):** closed G6 with a **unified Stardew-style hotbar** —
  one bar holds 4 tool slots + 6 inventory item slots, number keys 1-9 then 0 select either a tool
  or an item, the selected slot is highlighted, and stack counts show. Removed the old split (a
  6-slot inventory strip + separate 1-4 tool keys + a redundant top-left tool icon). Core selection
  logic is the pure, tested `HotbarModel` (10 new unit tests); `Hotbar` MonoBehaviour wraps it and
  drives `ToolSystem`. A held seed now plants on interact (hoe no longer auto-sows).
  - **Also fixed latent G12** discovered en route: `FarmingActions.TryTill` had no runtime caller —
    only the verify harness tilled, so a player could never make a plot in-game. `InteractionSystem`
    now tills the faced bare cell when E is pressed with the Hoe selected. `PlayerController.Island`
    is now assigned at spawn (was never set).
  - Validated green: 92 EditMode + 92 NUnit (check.sh), Unity compile clean, contact-sheet verified
    (`artifacts/screenshots/contact_sheet.png` — bar shows Hoe/Can/Sickle/Hammer + 4 seed stacks +
    wood/scrap with counts and the selected slot highlighted).

## Current state (2026-06-13, session 3 — cozy/warm pass)

- **Session 3 (this branch `feat/cozy-warmth-pass`):** the warmth pass + a fast visual-iteration
  toolchain. Validate green (82 EditMode + 73 NUnit). See `screenshots/cozy_pass_after.png`.
  - **NEW tooling — `VisualConfig` + `visual.json` + `tools/shot.sh`** (read WORKFLOW.md "Fast
    cozy/visual iteration loop"). All look values are data; edit JSON, re-run shot.sh, no recompile.
  - Sky gradient + drifting clouds replace the black void (`SkyBackground`); warm earth tint on
    fertile cells (`IslandRenderer.WarmTint`); avatar drop shadow; **forge/shelter warm glow pool**
    (`StructureGlow`, attaches in `BuildModeController.PlaceStructure`); golden ripe-crop glow + sway
    (`CropPlot`). Starting island reshaped to a compact ~4×3 fertile core + cliff rim (radius 4 via
    `visual.json`, generator favours FertileValley now). Spawn island is natural/untilled — the
    player tills; the harness only seeds a forge+crops for the screenshot.
- PR #2 (terrain fix + build pipeline) MERGED. PR #3 (staged building, camera zoom, input fixes,
  verify harness) **MERGED** → main `443ee68`.
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
