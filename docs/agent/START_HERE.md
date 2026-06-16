# Agent handoff — read this first

Low-context entry point for continuing SkyHarvest development. Read these 4 files (all short), then start:

1. **START_HERE.md** (this) — state + read order.
2. **WORKFLOW.md** — exact commands to compile, test, run the game, build, and screenshot-verify. The Unity admin-dialog gotcha lives here.
3. **NEXT_SESSION.md** — the prioritized task list to execute.
4. **MAP.md** — terse file/system map so you don't re-explore.

Also: **SCOPE_LEDGER.md** — spec/plan vs code gap list + keybind reconciliation table (kept current; update it when you close a gap).

Don't read the 100KB MVP plan or full design spec unless a task needs a specific detail. `docs/IMPLEMENTATION_NOTES.md` explains why this is headless-built (code-constructed scene, no prefabs/ScriptableObjects).

## Current state (2026-06-16, session 7 — playtest bugfixes + 7 polish PRs)

- **Branch `feat/bugfixes-and-session-prs` (commit `6c98a7e`, NOT yet merged/pushed)** — two
  batches of work landed in one session, on top of the session-6 starter-island work below:
  1. **Patrick's playtest bug list**, all fixed:
     - `PlayerController.FacingOffset` had N/S **inverted** for the dimetric projection
       (pressing UP moved toward the wrong `gy`) — root cause of *both* "can't reach the
       stairs" and "can only till 2-3 tiles". One-line fix, big payoff.
     - Stair-carve check moved to the **highest E-key priority** so a nearby CropPlot can't
       steal the press before the player mines up to the forge tier.
     - Cross-tier tilling blocked (must stand on the same tier as the target cell).
     - Debris-on-tilled-tile fixed: `FindNearestInteractable` now prefers `DebrisObject` over
       `CropPlot` on a distance tie (`TileInteractability.HasDebrisNearCell` also had a broken
       C# negated-pattern that never bound `mb` — fixed).
     - Inventory/hotbar drag-and-drop reworked (new `InventoryCursorModel`, `ToolItems`,
       `ItemIconPaths`) — this was the "cursor-agent rewrite" Patrick flagged as buggy.
     - New: Terraria-style custom cursor + per-tile interactable glow (`GameCursor.cs`).
  2. **7 of 10 proposed PRs**, approved by Patrick (3/5/8 explicitly rejected — don't revisit
     without him asking):
     - **PR1** stair discoverability — `InteractionSystem.CanCarveStairs` + HUD prompt.
     - **PR2** Continue restores the **designed StarterIsland** (not procedural) when that's
       what was saved — `IslandData.IsStarter`, new save fields `IsStarterIsland`/
       `StairsCarved`/player `Tier`. **G8 from the ledger below is effectively superseded** —
       Continue no longer silently swaps the island shape.
     - **PR4** `Inventory.TryAdd` now enforces `MaxStackSize` (99), all-or-nothing.
     - **PR6** soft underside shadow disc for floating-island depth (the rim-cliff side faces
       were already done in session 6 — this was the missing "floats in the sky" read).
     - **PR7** `AudioCueSystem` also starts the ambient drone on `GameStartedEvent` (it could
       previously launch silent if `WeatherChangedEvent` fired before the system subscribed).
     - **PR9** real minimap — `UI/MinimapController.cs` renders a top-down dot map + live
       player marker, replacing the static "(MVP)" placeholder text. **Closes ledger gap G8.**
     - **PR10** keybind help overlay (H key) + M toggles minimap; both wired into the
       centralized Esc-close chain in `Bootstrap.Update`.
  - GameCursor refactored mid-session onto the existing `ProcGfx.CursorPointer()` /
    `IsoDiamondEdgeGlow()` helpers instead of its original hand-rolled texture/LineRenderer.
  - **Bug caught by the verify harness, fixed before commit:** `MinimapController.Initialize`
    tried to `AddComponent<RawImage>` directly onto `MinimapPanel`, which already carries an
    `Image` from `MakePanel` — Unity disallows two `Graphic`s on one GameObject, so every
    New Game threw a NullReferenceException loop. Fixed by parenting the map image to a
    child GameObject instead. **If you add more dynamic UI to an existing `MakePanel()`
    panel, always use a child object, never `AddComponent` a second Graphic onto the panel.**
  - Validated: `tools/build.sh` clean; `tools/verify.sh` **zero exceptions**, 19/19 applicable
    checks PASS. 3 remaining FAILs (Till+Sow, Grow, Harvest, Skynet) are **pre-existing
    `PlayModeVerify.cs` / StarterIsland mismatches**, not regressions — see SCOPE_LEDGER.md
    "Known test-harness gaps" for the root cause and exact fix needed; nobody has fixed the
    harness itself since the starter island replaced the procedural one in session 6.
  - `tools/check.sh` could not be exercised this session — the sandboxed shell didn't have
    `dotnet` on PATH by default (it's at `C:\Program Files\dotnet\dotnet.exe`); even with it
    added, the local SDK is 3.1.201 and the `UnityStubs.csproj` now targets net8.0, so it
    fails with MSB3644 (missing .NET 8 reference assemblies). **Needs a .NET 8 SDK install
    (or retarget the stub project) before `check.sh` is usable again** — flagged, not fixed.
  - Not pushed to remote; no PR opened. Patrick said "commit this", nothing about pushing.

## Current state (2026-06-15, session 6 — two-tier hero starter island)

> Landed straight on `main` (commit `ab7401b`); never written up here until session 7 — if you
> hit references to "starter island" / "tier 0/1" elsewhere and wonder where the writeup is,
> this is it.

- Replaced the procedural diamond as the **New Game** starting piece with a fixed, designed
  **two-tier 3×4 island**: `Island/StarterIsland.cs` — front farm tier (`gy∈{2,3}`, elevation 0,
  `FertileValley`) + raised back forge tier (`gy∈{0,1}`, elevation 1, `RockyPlateau`), separated
  by a stone wall with one mineable staircase at the middle column (`StairColumn=1`,
  `FrontStairCell`↔`BackStairCell`). The player carves the stairs in the tutorial (E while
  facing the uncarved edge) to unlock the forge tier — see `IslandData.AddStairEdge` /
  `CarveStairs` / `IsStairEdge`.
- New tier-aware movement gate: `IslandData.Tier(pos)` (rounded elevation) +
  `IslandData.CanTraverse(from, to)` — free within a tier, blocked across tiers everywhere
  except a carved stair edge. `PlayerController.CurrentTier` tracks which tier the player is
  standing on; `TryStep` snaps the player to the new tier's world position when they cross.
- `ElevationWorldStep = 0.5` world units per tier (`Constants.cs`); `GridMath.GridToWorld` adds
  `elevation * ElevationWorldStep` to worldY.
- Directional cliff-face rendering in `IslandRenderer.BuildTierWalls`: a face is drawn on every
  diamond edge whose camera-facing neighbour (+x or +y) is a lower tier or off-island —
  `AddTierFace` (tier-to-tier, `TierFaceH=48px`) vs `AddRimFace` (void-facing outer edge,
  `RimFaceH=96px`, the "floating island" cliff skirt). Sprites built once via
  `ProcGfx.IsoTierFace` and cached.
- 4 playtest bugs found and fixed in this session: walk-over-void, build-on-gap (both via the
  `CanTraverse`/tier gate above), invisible clouds, elevation-doubling in the renderer.
- 123/123 tests green at the time.
- **Known limitation carried forward into session 7 (now fixed by PR2 above): Continue/Load
  only ever rebuilt via `IslandGenerator.Generate` (procedural)**, even if the save was made on
  the starter island — loading silently swapped the player onto a different-shaped island with
  no stairs gate. "New Game" was the only path that used `StarterIsland.Build`.
- Build freshness gotcha (still true): the exe is only fresh if
  `SkyHarvest_Data/Managed/SkyHarvest.dll` mtime is recent — checking the `.exe` mtime alone is
  misleading (Unity doesn't always touch it on incremental builds).

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
