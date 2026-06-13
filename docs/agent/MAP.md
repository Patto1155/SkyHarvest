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
- `IslandGenerator` (seeded proc-gen) → `IslandData` (`Cells` dict, `IsWalkable`) → `IslandRenderer` (tiles + overlays).
- `TerrainType.cs` — enum + `TerrainProperties` (CanPlaceCrops, BaseSoilQuality, tile paths).
- `SoilPatch.cs` — water/nutrients/quality; composting + rotation depletion live here.
- `IslandExpansion.cs` — scaffolding-triggered edge growth.

## Player (`Player/`)
- `PlayerController` — WASD/arrow movement (legacy axes), facing, walk anims, hotbar 1-4 passthrough.
- `InteractionSystem` — E key; nearest `IInteractable` via `InteractableRegistry`; `CurrentTarget/PromptText`.
- `ToolSystem` — slots 1-4 = Hoe/WateringCan/Sickle/Hammer.
- `PlayerInventory.cs` — `Inventory` POCO (TryAdd/TryRemove/GetCount/Has) + `PlayerInventoryComponent`.

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
- Keybinds: ALL routed through `Bootstrap.Update` — see SCOPE_LEDGER.md keybind table.

## Save/Load (`SaveLoad/`)
- `SaveManager` (`persistentDataPath/saves/save.json`, JsonUtility) + `WorldSaveData` DTOs.
  Construction sites persist via `StructureSaveData.Constructing` + `Delivered`.

## Editor harnesses (`Assets/Editor/`)
- `BuildScript.cs` — `BuildWindows` batchmode build → `Builds/Windows/SkyHarvest.exe`.
- `PlayModeScreenshots.cs` — unattended Play-mode screenshots (see WORKFLOW.md).
- `PlayModeVerify.cs` — drives every feature loop live, writes `artifacts/verify/verify_report.md`.

## Tests
- `Assets/Tests/EditMode/*` — run BOTH via `tools/check.sh` (fast, .NET against stubs) and Unity EditMode.
- `tools/validate.sh` — check.sh + Unity batch compile + EditMode tests. Always run before handoff.
