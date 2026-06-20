# Scope-gap ledger — design spec + MVP plan vs. code (2026-06-14)

> **Session 8 (2026-06-18, `main`, commit `11e9e51`) — carved stair cutout + tier corridor
> climbing + F3 dev debug panel.** Not in ledger before this pass:
> - Custom stair PNG (`stair_cutoutv2.png`) with `StairCutoutLayout` JSON + F8 in-game editor (`--dev`).
> - `StairWalkMath` diagonal walk corridor; `PlayerController.TryStep` clamp/exit logic; `GridMath.DiamondCentre`.
> - `Dev/DevDebugPanel` (F3), `DevDebugOverlay` (GL hitboxes), `DevDebugSettings`.
>
> **Session 8b (2026-06-18, same branch, uncommitted)** — automation pass without human playtest:
> - `PlayModeVerify.StepSkynet` falls back to any free edge cell (not only `CliffEdge`).
> - `tools/check.sh` works again: exclude `Dev/**` + editor-only scripts from CLR harness;
>   `SKYHARVEST_HEADLESS` shims for `StairCutoutEditor` / `DevDebugPanel`; `Texture2D.LoadImage` stub;
>   `IslandRenderer.BlendRoot.SetActive` fix (was invalid `.gameObject` on `GameObject`).
> - **G9 partial CLOSED** — `SpringIrrigation` passive drip from `NaturalSpring` neighbours +
>   manual water bonus; `SpringIrrigationTests` (3).
> - `StairWalkTests` end-to-end corridor walkability sample; `visual.json` cozy nudge; softer ambient gain.
> - Known test-harness gaps in `PlayModeVerify` **Till+Sow path already fixed** in source (seed hotbar
>   selection) — re-run `tools/verify.sh` to confirm 20/20.

> **Session 7 (2026-06-16, commit `6c98a7e`) — playtest bugfixes + PR1/2/4/6/7/9/10.** Full writeup in START_HERE.md.

> **Session 5 (2026-06-14, `feat/debris-loop`) — debris → skynet → expansion loop audited + closed.**
> The whole chain was already wired and is now **live-verified in Play mode** (PlayModeVerify,
> 20/20 PASS incl. new expansion + skynet-accrual checks). Two real defects found and fixed:
> - **G13 — Skynet offline accrual was DEAD** (`InitializeOfflineAccrual` had zero runtime callers;
>   only the verify harness called it directly, which is *why it falsely "passed" before*). The
>   spec's "checked like a mailbox" catch-up never ran in a real game. Now wired into the save-restore
>   path (`Bootstrap.RestoreIslandContents`). Math extracted to pure, tested `SkynetAccrual.OfflineRollCount`.
> - **G14 — `IslandExpandedEvent` double-published** by both `IslandExpansion.Expand` *and*
>   `BuildModeController.PlaceStructure` (the latter could also fire a bogus 0-count event). Removed
>   the controller duplicate; `Expand` is the canonical publisher. Verified `fired=1`.
> New coverage: `ExpansionTests` (4) + `SkynetAccrualTests` (7) → 103 NUnit; new `PlayModeVerify`
> expansion step; new `tools/verify.sh` launcher.

# Scope-gap ledger — design spec + MVP plan vs. code (2026-06-13)

Sources: `docs/superpowers/specs/2026-03-17-sky-harvest-design.md` (§12 MVP scope),
`docs/superpowers/plans/2026-03-14-sky-harvest-mvp.md` (Tasks 1–22). Status from reading
`Assets/Scripts/**` on `main` @ 030d5e8.

## MVP-scope items — present and working (per code read; live verification pending)

| MVP item | Code |
|---|---|
| Procedural island, varied terrain/elevation/soil | `IslandGenerator`, `TerrainType`, `SoilPatch`, `IslandData` |
| Debris landing + scavenge | `DebrisSpawner`, `DebrisObject`, loot tables in `GameDatabase` |
| Basic Skynet (tier 1) | `Skynet.cs`, real-time catch buffer w/ `LastCollectedUnixTime` |
| Plant/water/tend/harvest, starter+staple crops | `CropGrowthSystem`, `CropPlot`, `FarmingActions`; 4 crops (2 starter, 2 staple) |
| Weather that matters | `WeatherStateMachine/Manager/Effects/AmbientCues`; crops take wind damage |
| Build structures (ghost preview) | `BuildModeController` ghost w/ valid/invalid tint — but see gaps: instant build |
| 3 workshops + processing | `DryingRack`, `StoneMill`, `Forge`, 4 recipes, fuel, weather-sensitive drying |
| Storage crates + barrels | `StorageContainer`, `StorageUI`, workshop auto-pull via `StorageProximity` |
| Island expansion via scaffolding | `IslandExpansion.Expand` + renderer `RenderNewCells` |
| Save/load | `SaveManager`, `WorldSaveData` — structures, crops, storages, skynets, workshops, player |
| Audio/visual cues, no HUD notifications | `AudioCueSystem`, `WeatherAmbientCues`, `ContextualTooltipUI` first-time tips |
| Demolish (50% refund, no relocation) | `Structure.Demolish` via hammer |

## MVP-scope but MISSING / broken (the gap list)

| # | Gap | Spec/plan source | Code evidence | Action |
|---|---|---|---|---|
| G1 | ✅ FIXED 2026-06-13 — camera zoom: default ortho 2.5, smooth scroll zoom [2,6] | spec §7 "Fixed camera — zoom in/out only" | `CameraFollow.cs` | done |
| G2 | ✅ FIXED 2026-06-13 — staged building: `ConstructionSite` + `ConstructionProgress`, E delivers, saves persist sites; `InstantBuild` debug flag keeps old path | spec §2 blueprint ghost system | `Building/ConstructionSite.cs` | done |
| G3 | ✅ FIXED 2026-06-13 — `WireBuildMode` called on BOTH start paths (build mode was dead in New Game) | plan Task 15/21 wiring | `Bootstrap.WireBuildMode` | done |
| G4 | ✅ FIXED 2026-06-13 — `SetPlayer` wired (free-build hole closed); also `BuildMenuUI.Open` had NO caller — B never showed the menu, fixed via centralized B handler | plan Task 15 | `Bootstrap.Update` | done |
| G5 | ✅ FIXED 2026-06-13 — Tab toggles the pack (closes storage first if open); tooltip is no longer lying | plan Task 5 | `Bootstrap.Update` | done |
| G6 | ✅ FIXED 2026-06-13 — **unified Stardew-style hotbar**: one bar = 4 tool slots + 6 item slots, number keys 1-9/0 select either a tool or an inventory item, selected slot highlighted, stack counts shown. The old split (6-slot inventory strip + separate 1-4 tool keys + redundant top-left tool icon) is gone. Held seed plants on interact (was: hoe auto-sowed first seed). | spec §9 hotbar | `Player/Hotbar.cs` (+`HotbarModel`), `HUDController.RefreshHotbar`, `CropPlot.Interact`, `FarmingActions.TrySow(seedItemId)` | done — 10 HotbarModel unit tests |
| G12 | ✅ FIXED 2026-06-13 — **bare-ground tilling was unwired** (discovered via G6): `FarmingActions.TryTill` had NO runtime caller — only the verify harness tilled, so the player could never create a plot in-game. Now `InteractionSystem` tills the faced cell when E is pressed with the Hoe selected and no other target. Also set `PlayerController.Island` at spawn (was never assigned). | spec §4 farming loop | `InteractionSystem.TryTillFacingCell`, `Bootstrap.SpawnPlayer`/start paths | done |
| G7 | ✅ FIXED 2026-06-13 — all B/Tab/Esc hotkeys centralized in `Bootstrap.Update`; Esc closes topmost panel only, pauses when nothing open | spec: Esc = cancel/close | `Bootstrap.Update` | done |
| G8 | ✅ FIXED 2026-06-16 (session 7) — real top-down dot map + live player marker, no longer a static placeholder | spec §9 HUD "Minimap / island overview toggle" | `UI/MinimapController.cs` | done — M key also toggles it now |
| G9 | ~~Irrigation channels / springs-as-water-source not implemented~~ — **PARTIAL 2026-06-18**: `SpringIrrigation` waters tilled neighbours of `NaturalSpring` each tick; manual water bonus when adjacent. Carved channels still backlog. | spec §4 crop needs | `Farming/SpringIrrigation.cs`, `CropGrowthSystem` | partial — channels deferred |
| G10 | ~~Soil improvement missing~~ — WRONG on first pass: composting, rotation depletion, terrain-based quality ARE implemented (`SoilPatch`, proven by `SoilTests`) | spec §4 soil system | `SoilTests.Composting_Restores_Nutrients` etc. | ✅ no gap — verify a composting interaction path exists in-game |
| G11 | Sunlight/shade simulation absent | spec §4 | none | Deferred (not in §12 list) |

## Deferred by design (spec §12 "What's Not In" — correctly absent)

Hubs/multiplayer/Skypillar travel · automation (sprinklers/drones/feeders) · animals ·
brewhouse/loom/kitchen/alchemist · Skynet tiers 2–4 (only tier-1 buildable — correct) ·
battle pass/seasons · PvP/co-op/expeditions · pests/decay/debris-impact threats ·
Blackstorm weather state · UGC/AI modes · Commons hub.

Implementation deviations accepted by convention (see `docs/IMPLEMENTATION_NOTES.md`):
legacy `Input.*` instead of plan Task 5's new-Input-System `InputManager` + action maps;
code-constructed scene/UI instead of prefabs; `GameDatabase` static C# instead of ScriptableObjects.

## Keybind reconciliation — planned vs wired

| Input | Planned (plan Task 5 + spec §9) | Actually wired | Status |
|---|---|---|---|
| WASD / arrows | Move | `PlayerController.cs:75` `GetAxisRaw` | ✅ |
| E | Interact | `InteractionSystem.cs:48` | ✅ |
| B | Toggle build mode | `BuildModeController.cs:49` | ✅ |
| **Tab** | **Open inventory** | StorageUI-close (`StorageUI.cs:50`) + first-time tooltip (`ContextualTooltipUI.cs:42`); **inventory never opens** | ❌ G5 — bind Tab→`InventoryUI.Toggle` when no other panel open |
| 1-9, 0 | Unified hotbar slots (4 tools + 6 items) | `Hotbar.cs` Update → `SelectSlot` (single handler; ToolSystem/PlayerController input loops removed) | ✅ G6 fixed |
| Esc | Cancel / pause | Pause toggle `Bootstrap.cs:453` + StorageUI close + BuildMenu close + build-mode exit — all same frame | ⚠ G7 conflict |
| Mouse L | Place structure (build mode) | `BuildModeController.cs:65` | ✅ |
| ↑/↓ + Enter | Build menu navigate/confirm | `BuildMenuUI.cs:56-60` | ✅ |
| Q | (not planned — added) inspect plot/structure | `InspectorPanel.cs:73` | ✅ extra |
| R | RotateStructure (plan BuildMode map) | not implemented (structures are 1×1, nothing to rotate) | ✅ dropped by design |
| Mouse (Map button) / **M** | Minimap toggle | `Bootstrap.Update` (M key) + Map button, both toggle `MinimapPanel` | ✅ G8 fixed session 7 |
| **H** | (not planned — added) keybind help overlay | `Bootstrap.Update` + `KeybindPanel` | ✅ extra, session 7 |
