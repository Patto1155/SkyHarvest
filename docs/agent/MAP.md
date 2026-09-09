# Code map — terse, so you don't re-explore

Everything is code-constructed at runtime from `Bootstrap` (no prefabs/ScriptableObjects;
see `docs/IMPLEMENTATION_NOTES.md`). One runtime asmdef: `Assets/Scripts/SkyHarvest.asmdef`.

## Entry + core (`Assets/Scripts/Core/`)
- `Bootstrap.cs` — builds EVERYTHING: managers, camera, island layer, all UI panels, main menu.
  Owns New Game / Continue flows, save-restore of structures+crops, and ALL centralized
  hotkeys (B/Tab/Esc) in its `Update`. UI widget factory helpers at the bottom.
- `GameManager.cs` — `Instance`, `CurrentIsland`, `Clock` (GameTime), Pause/Resume.
- `GameTime.cs` — publishes `GameTickEvent` (DeltaMinutes); game-time only advances in play.
- `EventBus.cs` / `GameEvents.cs` — static pub/sub; ALL cross-system events declared in GameEvents.
- `CameraFollow.cs` — smooth follow + scroll-wheel zoom (ortho size [2,6], default 2.5).
- `GridMath.cs` — dimetric grid↔world. `SpriteLoader/SpriteAnimator` — manifest-driven sprite strips.
- `Constants.cs` — island radius, sorting scale, SecondsPerGameMinute.

## Data (`Data/`)
- `GameDatabase.cs` — authoritative static DB: items, 4 crops, 4 recipes, 11 structures, debris loot tables.
- `Defs.cs` — ItemDef/CropDef/RecipeDef/StructureDef/BuildCost/LootTableDef + `WorkshopType` enum.

## Island (`Island/`)
- **New Game** uses `StarterIsland.Build(seed)` — a fixed, designed two-tier 3×4 island (farm
  tier 0 + raised forge tier 1, one mineable stair edge), NOT the procedural generator.
  `IslandGenerator` (seeded proc-gen) is still used as the fallback when **Continue** loads a
  save whose `IsStarterIsland` flag is false (i.e. an older/non-starter save).
- `IslandData` — `Cells` dict, `IsWalkable`, `IsStarter` flag, tier model: `Tier(pos)` (rounded
  elevation), `CanTraverse(from,to)` (free within a tier, blocked across tiers except a carved
  stair edge), `AddStairEdge`/`CarveStairs`/`StairsCarved`/`IsStairEdge`.
- `IslandRenderer` — tiles + overlays + `BuildTierWalls` (directional cliff faces: `AddTierFace`
  between tiers, `AddRimFace` on void-facing outer edges) + underside shadow disc for depth.
- `TerrainType.cs` — enum + `TerrainProperties` (CanPlaceCrops, BaseSoilQuality, tile paths).
- `SoilPatch.cs` — water/nutrients/quality; composting + rotation depletion live here.
- `IslandExpansion.cs` — scaffolding-triggered edge growth.

## Input — phone-first (v2)
- `Player/TapController` — the primary input. Left-click/tap → `TapTargeting.ResolveCell` (tries the
  player's tier first, then others, since elevation shifts the projection) → in reach? act now :
  `GridPath.Find` a route and `PlayerController.WalkPath(path, onArrive)`. Suppressed while any
  panel is open, during build mode, or when the tap hit UI.
- `Island/GridPath` — BFS honouring `IslandData.CanTraverse` (tiers + carved stairs).
- `Player/PlayerController.Move(h, v, maxStep)` — one movement path shared by keyboard and
  auto-walk, so tier rules can't diverge. `WalkPath`/`CancelWalk`/`IsAutoWalking`/`FaceCell`.
  Keyboard input cancels an auto-walk.
- `Player/InteractionSystem.TryActOnCell(cell)` — tap equivalent of E: stairs → interactable on
  that cell → till. The E key still uses the facing cell.
- `CameraFollow` — Ctrl+scroll or two-finger pinch to zoom.
- `UI/UILayout` — portrait 720×1280 reference + edge-anchor helpers; `Bootstrap.ConfigureScaler`
  applies it to both canvases. HUD chrome anchors to screen edges; panels stay centred.

## Player (`Player/`)
- `PlayerController` — WASD/arrow movement (legacy axes), facing, walk anims, `CurrentTier`
  (which elevation tier the player is standing on — see Island section), hotbar passthrough.
  **`FacingOffset()` N/S is intentionally inverted vs an abstract cardinal grid** — see the
  comment in the method; the dimetric worldY formula means "up" on screen is lower `gy`.
- `InteractionSystem` — E key; nearest `IInteractable` via `InteractableRegistry`
  (debris preferred over crop plots on a distance tie); `CurrentTarget`/`PromptText`;
  `CanCarveStairs` (drives the HUD "press E to carve passage" hint) checked at highest
  priority so it can't be stolen by a nearby interactable.
- `ToolSystem` — slots 1-4 = Hoe/WateringCan/Sickle/Hammer. `ToolItems.cs` — tool-vs-item id
  helpers used by the hotbar/drag system.
- `PlayerInventory.cs` — `Inventory` POCO (TryAdd enforces `GameDatabase` `MaxStackSize`,
  all-or-nothing/TryRemove/GetCount/Has) + `PlayerInventoryComponent`.
- `InventoryCursorModel.cs` — pickup/drop cursor state for inventory drag-and-drop
  (`InventoryDragManager` in UI/ owns the MonoBehaviour side).

## Building (`Building/`)
- `BuildModeController` — singleton; Bootstrap calls Enter/ExitBuildMode; ghost follows mouse;
  left-click → `PlaceConstructionSite` (staged) or `PlaceStructure` (finished; also save-restore).
  `InstantBuild` static flag = old consume-and-spawn for debug. `AttachStructureComponent`
  switch maps StructureId→component type.
- `ConstructionProgress.cs` — pure-C# deliver/remaining/complete state (tested in check.sh).
- `ConstructionSite.cs` — translucent placed site; E delivers from inventory; completes → real structure;
  hammer cancels w/ 100% refund.
- `Structure.cs` — base: registry registration, hammer demolish (50% refund). `StructureRegistry` — by-pos lookup.
- `RainCatcher.cs` — fills in rain, water source.

## Farming (`Farming/`)
- `FarmingActions` — static TryTill/TrySow/Water/Harvest/ClearDead (the E/tool verbs).
- `CropGrowthSystem` — ticks all `CropPlot`s on GameTickEvent; weather-aware (sun/wind via bridge).
- `CropPlot` + `CropInstance.cs` (`CropState`: GrowthProgress, Health, IsHarvestable).

## Workshops (`Workshop/`)
- `WorkshopBase` — StartRecipe (pulls player inv + adjacent storage ≤1.5u via `StorageProximity`),
  ticks on GameTickEvent, CollectOutput. Subclasses: `DryingRack` (rain ruins), `StoneMill`, `Forge` (fuel).

## Debris & Skynet
- `DebrisSpawner` — interval spawns on edge cells, weather-weighted. `DebrisObject` — falls ~1.5s,
  then interactable scavenge (1-3 loot rolls), despawns after a while.
- `Skynet/Skynet.cs` — cliff-edge structure; REAL-time accrual (90-180s rolls, buffer 6 stacks);
  offline accrual on load; E collects.

## Weather (`Weather/`)
- `WeatherStateMachine` (weighted transitions, SetState for restore) ← `WeatherManager.Instance`.
- `WeatherEffects` / `WeatherAmbientCues` — rain/storm visuals + audio; crops react via CropGrowthSystem.

## UI (`UI/`) — all panels built by Bootstrap, components on the HUD canvas
- `HUDController` (time/weather/prompt/hotbar/tool icon), `InventoryUI` (pack, Tab),
  `StorageUI` (two-sided transfer), `WorkshopUI` (recipes/progress/start/collect),
  `BuildMenuUI` (↑/↓/Enter select; B opens with build mode), `InspectorPanel` (Q),
  `PauseMenuUI` (Esc; save/quit), `MainMenuUI` (seed input/new/continue), `ContextualTooltipUI`
  (one-time hints via PlayerPrefs).
- `GameCursor.cs` — hides the OS cursor, draws a custom `ProcGfx.CursorPointer()` sprite, and
  highlights the hovered tile with `ProcGfx.IsoDiamondEdgeGlow()` only when
  `TileInteractability.CanInteractAt` says the current tool/held item can act on it.
- `MinimapController.cs` — real top-down dot map (`RawImage` on a **child** of `MinimapPanel`,
  not the panel itself — see "gotcha" below) + a live player-position marker. Wired from
  `Bootstrap.WireGameCursor` after New Game / Continue.
- `ItemIconPaths.cs` — itemId → icon sprite path lookup, used by hotbar/inventory/drag UI.
- **Gotcha:** `Bootstrap.MakePanel()` already adds an `Image` (a `Graphic`) to every panel it
  creates. Unity does not allow two `Graphic` components on one GameObject — if you want to
  add a `RawImage` or another `Image` to dress up an existing panel, parent it to a **new
  child GameObject**, never `AddComponent` it directly onto the panel.
- Keybinds: ALL routed through `Bootstrap.Update` — see SCOPE_LEDGER.md keybind table.

## Sim — offline / automation layer (`Sim/`) — v2 idle pivot (spec `2026-09-09-sky-harvest-idle-pivot.md`)
- `IslandSim` — pure-C# island: `Plots`/`Storages`/`Workshops`/`Devices` sharing one `WaterNetwork`
  + `PowerGrid`. `Step(dt, env, tickCrops, tickWorkshops, report)` order: windmills→power,
  springs→water, sprinklers, crops, Tender's Posts (harvest→adjacent storage, replant from its
  seeds), composters, workshops. `Advance(elapsed)` replays offline time in
  `Constants.OfflineStepSeconds` (10s) steps with `SimEnvironment.Offline` (no rain/wind).
- `AutomationDevice` + subclasses `WaterTank`/`Battery`/`Windmill`/`Sprinkler`/`WindTotem`/
  `Composter`/`TendersPost` — Chebyshev radius 1 (3×3). Balance constants live on each class.
- `OfflineCatchup` — clamp elapsed to `Constants.OfflineCapSeconds` (4h), split into steps.
- `OfflineReport` — harvested/replanted/batches + first `StorageFullAt`/`WaterEmptyAt`/`PowerEmptyAt`.
- `AutomationSystem` (MonoBehaviour, built by Bootstrap) — owns the live `WaterNetwork`/`PowerGrid`,
  lazily rebuilds the `IslandSim` from scene objects via `SimBridge` (dirty on place/demolish/
  plant/harvest/expand), ticks devices on `GameTickEvent`, `RunOfflineCatchup(lastSeenUnix)` on
  Continue. `SimBridge.SyncPlotsToScene` writes crop changes back to `CropPlot`s.
- `Building/AutomationStructure` — the placed structure for any `StructureDef.Automation != None`;
  holds the device. `BuildModeController.AttachStructureComponent` routes by that field first.
- `UI/WelcomeBackUI` — "While you were away" panel shown from `Bootstrap.StartFromSave`.
  `BuildBody(report)` is static and unit-tested.
- `CropPlot.LastCropId` — what to replant. Empty tilled plots are now saved (`EmptyPlots`).
- **Water comes from** springs (`TerrainProperties.HasWaterSource`) and rain catchers
  (`IslandSim.RainCatchers`, a trickle while away — the only pre-forge offline income).
- **`OfflineReport.CropsRipened`** is the day-one payoff with no automation built; without it the
  panel wrongly read "the island waited quietly".
- Balance constants live on the device classes (`Sprinkler.TargetSoilWater`,
  `TendersPost.PowerPerSecond`, `Windmill.BaseGenerationPerSecond`, …) and `Constants`
  (`OfflineCapSeconds` 4h, `OfflineStepSeconds` 10s). **None of it is tuned yet.**

## Save/Load (`SaveLoad/`)
- `SaveManager` (`persistentDataPath/saves/save.json`, JsonUtility) + `WorldSaveData` DTOs.
  **v2 fields:** `LastSeenUnixTime` (drives offline catch-up), `WaterStored`/`PowerStored`,
  `Island.Devices` (composter stock), `Island.EmptyPlots`. `ApplyAutomationState(data)` restores
  buffers after structures exist (capacities derive from placed tanks/batteries).
  Construction sites persist via `StructureSaveData.Constructing` + `Delivered`.
  `IslandSaveData.IsStarterIsland`/`StairsCarved` + `PlayerSaveData.Tier` (added session 7) let
  `Bootstrap.StartFromSave` rebuild via `StarterIsland.Build` instead of `IslandGenerator`
  when that's what was actually played, and restore which elevation tier the player was on.

## Editor harnesses (`Assets/Editor/`) — compile-checked by `tools/check.sh` since session 8
> `tools/clr-harness/EditorCode` builds these against `EditorStubs/Stubs.Editor.cs` (a minimal
> UnityEditor surface). If you use a UnityEditor API the stubs lack, add it there; if it's a
> UnityEngine API, add it to a new `UnityStubs/Stubs.<Area>.cs` (never edit another agent's file).

- `BuildScript.cs` — `BuildWindows` batchmode build → `Builds/Windows/SkyHarvest.exe`.
- `PlayModeScreenshots.cs` — unattended Play-mode screenshots (see WORKFLOW.md).
- `PlayModeVerify.cs` — drives every feature loop live, writes `artifacts/verify/verify_report.md`.

## Tests
- `Assets/Tests/EditMode/*` — run BOTH via `tools/check.sh` (fast, .NET against stubs) and Unity EditMode.
- `tools/validate.sh` — check.sh + Unity batch compile + EditMode tests. Always run before handoff.
