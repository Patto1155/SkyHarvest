# Sky Harvest — Engineering Conventions (BINDING for all agents)

This document is the contract between parallel implementation agents. Follow it exactly.
The gameplay/system behavior spec is `docs/superpowers/specs/2026-03-17-sky-harvest-design.md`;
the task breakdown is `docs/superpowers/plans/2026-03-14-sky-harvest-mvp.md`. Where the plan's
sample code conflicts with this document (3D physics, URP, Input System, TMP, ScriptableObject
.asset files), THIS DOCUMENT WINS. See `docs/IMPLEMENTATION_NOTES.md` for rationale.

## Tech decisions

- Unity **2022.3 LTS**, **built-in render pipeline**, **2D sprite rendering** (SpriteRenderer), pixel art.
- **Legacy Input** (`UnityEngine.Input`): WASD/arrows move, E interact, B build mode, Tab inventory,
  Esc cancel/pause, 1-6 hotbar, mouse for build placement & UI. No Input System package.
- **uGUI with legacy `Text`** (font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`). No TextMeshPro.
- **Physics2D** only: `Collider2D`, `Physics2D.OverlapCircleAll`. Player uses transform movement with
  manual island-bounds clamping (NO Rigidbody — cleaner for grid world).
- **No prefabs, no .asset files, no Animator controllers, no hand-authored .meta GUIDs** (exceptions:
  `Assets/Scenes/Main.unity` + `Assets/Scripts/Core/Bootstrap.cs` whose .meta files already exist — do not touch).
  Everything is constructed in code at runtime from `Bootstrap`.
- **Sprites**: PNGs under `Assets/Resources/Sprites/...`, loaded via `SpriteLoader` (below) which does
  `Resources.Load<Texture2D>`, sets `filterMode = Point`, and slices strips in code. An editor script
  `Assets/Editor/PixelArtImporter.cs` (AssetPostprocessor) forces Point filter / no compression / readable
  off — owned by the UI/bootstrap agent.
- **Game data in code**: static `GameDatabase` (namespace `SkyHarvest.Data`) holds plain-C# definition
  classes `ItemDef`, `CropDef`, `RecipeDef`, `StructureDef`, `LootTableDef` with the IDs listed below.
  Keep definition classes UnityEngine-free so they compile in the harness. (We deliberately do NOT use
  ScriptableObjects.)
- **Saves**: JSON via `JsonUtility` to `Application.persistentDataPath/saves/save.json`, per plan Task 20.

## Namespaces / folders (per plan, all under `Assets/Scripts/`)

`SkyHarvest.Core`, `.Data`, `.Island`, `.Player`, `.Farming`, `.Weather`, `.Building`, `.Workshop`,
`.Storage`, `.Debris`, `.UI`, `.SaveLoad` — folder = namespace suffix. Tests in `Assets/Tests/EditMode/`
(NUnit, no UnityEngine APIs beyond what the harness stubs).

## Already-written shared contracts — build against these, do not modify

- `Core/Constants.cs`, `Core/EventBus.cs`, `Core/GameEvents.cs` (all events + `WeatherType` live here)
- `Player/IInteractable.cs` (`string InteractionPrompt { get; }`, `void Interact(PlayerController player)`)
- `Player/PlayerInventory.cs` (`Inventory`, `InventorySlot` POCOs; publishes `InventoryChangedEvent`)

If you need a new cross-system event, it already should be in `GameEvents.cs` — if genuinely missing,
add it there (keep it UnityEngine-free).

## Coordinates, rendering, sorting

Logical grid `Vector2Int` (x = "south-east", y = "south-west" axes). 2:1 dimetric projection into 2D world space:

```csharp
// GridMath (Core/GridMath.cs — owned by island/player agent, used by everyone)
worldX = (gx - gy) * 0.5f;
worldY = (gx + gy) * -0.25f + elevation * Constants.ElevationWorldStep; // +gx+gy goes DOWN-screen
Vector2Int WorldToGrid(Vector2 world, float elevation = 0) // inverse, rounds to nearest
```

- 1 world unit = 64 px (`Constants.PixelsPerUnit`). Terrain diamond = 1.0 × 0.5 world units.
- **Sorting**: all SpriteRenderers use `sortingOrder = Mathf.RoundToInt(-worldY * Constants.SortingOrderScale) + bias`
  (bias: terrain −10000 so it's always behind; flat overlays like tilled soil/paths −5000; everything else 0;
  UI prompts +10000). Camera orthographic, `orthographicSize` ~4, follows player (simple lerp script, no Cinemachine).
- Each placeable entity sits on one grid cell (footprint 1×1 for ALL MVP structures — simplification; the
  plan's `FootprintSize` stays in `StructureDef` but is always (1,1)).

## SpriteLoader (Core/SpriteLoader.cs — owned by UI/bootstrap agent; API fixed NOW)

```csharp
public static class SpriteLoader {
    // path relative to Resources, e.g. "Sprites/crops/crop_storm_wheat"
    public static Sprite Load(string path);                      // whole texture, pivot bottom-center
    public static Sprite[] LoadStrip(string path, int frameW);   // horizontal strip, pivot bottom-center
    public static Sprite[] LoadStrip(string path, int frameW, Vector2 pivot);
    public static Sprite LoadTile(string path);                  // terrain tile, pivot = diamond-top center: (0.5, (H-16)/H)
}
```

`Farming`/`Building`/etc. agents: reference sprites ONLY through these calls with manifest paths below.
A `SpriteAnimator : MonoBehaviour` (UI/bootstrap agent) cycles `Sprite[] Frames` at `Fps` (default 6), `Loop` flag.

## Sprite manifest (art agent generates EXACTLY these files; code agents reference EXACTLY these paths)

All under `Assets/Resources/Sprites/`. Strips are horizontal, left→right. Sizes in px.

| Path | Frames × WxH | Notes |
|---|---|---|
| `terrain/tile_fertile_valley.png` | 3 × 64x80 | 3 variants; diamond top face in top 32 rows, cliff skirt below |
| `terrain/tile_rocky_plateau.png` | 3 × 64x80 | |
| `terrain/tile_cliff_edge.png` | 3 × 64x80 | |
| `terrain/tile_natural_spring.png` | 3 × 64x80 | animated shimmer OK as variants |
| `terrain/tile_wind_corridor.png` | 3 × 64x80 | |
| `terrain/tile_scaffold.png` | 1 × 64x80 | expansion platform (timber planks) |
| `terrain/overlay_tilled.png` | 1 × 64x32 | diamond overlay |
| `terrain/overlay_wet.png` | 1 × 64x32 | dark moist soil diamond overlay |
| `terrain/overlay_dry.png` | 1 × 64x32 | cracked dry overlay |
| `player/player_idle_{s,n,e,w}.png` | 4 × 48x64 | breathing idle |
| `player/player_walk_{s,n,e,w}.png` | 6 × 48x64 | walk cycle |
| `player/player_action_{s,n,e,w}.png` | 4 × 48x64 | generic tool swing |
| `crops/crop_{sky_moss,cloud_root,storm_wheat,herb_plant}.png` | 5 × 64x64 | stages 0-3 + dead (frame 4) |
| `structures/shelter.png` | 1 × 128x128 | |
| `structures/rain_catcher.png` | 2 × 64x96 | empty / full |
| `structures/windbreak.png` | 1 × 64x80 | |
| `structures/path.png` | 1 × 64x32 | flat overlay |
| `structures/scaffolding.png` | 1 × 64x80 | |
| `structures/skynet.png` | 2 × 96x96 | empty / has-catch |
| `structures/drying_rack.png` | 2 × 96x80 | empty / drying |
| `structures/stone_mill.png` | 4 × 128x160 | sails turning |
| `structures/forge.png` | 4 × 128x128 | ember glow pulse |
| `structures/crate.png` | 1 × 48x48 | |
| `structures/barrel.png` | 1 × 48x64 | |
| `debris/debris_1.png` … `debris_3.png` | 1 × 48x48 each | salvage piles |
| `items/icon_<itemId>.png` | 1 × 32x32 | one per item ID below |
| `ui/icon_tool_{hoe,wateringcan,sickle,hammer}.png` | 1 × 32x32 | |
| `ui/panel.png` | 1 × 48x48 | 9-slice parchment/leather panel (code uses sliced Image) |
| `ui/button.png`, `ui/button_pressed.png` | 1 × 48x24 | 9-slice metal-framed |
| `ui/slot.png` | 1 × 40x40 | inventory slot, metallic frame |
| `ui/card_frame.png` | 1 × 40x40 | painterly card frame drawn OVER item icons |
| `ui/logo.png` | 1 × 256x96 | "SKY HARVEST" title |
| `fx/rain_drop.png` | 1 × 4x12 | |
| `fx/wind_streak.png` | 1 × 32x4 | |
| `fx/fog_blob.png` | 1 × 64x32 | soft alpha blob |
| `fx/sparkle.png` | 4 × 16x16 | harvest/collect pop |
| `bg/sky_far.png` | 1 × 512x256 | tileable moody cloud layer |
| `bg/sky_near.png` | 1 × 512x256 | tileable darker cloud wisps |

9-slice borders: panel 16px, button 8px, slot/card 6px (UI agent passes these to `Sprite.Create`).

**Palette (from concept art, binding):** base charcoal `#1d1a1d`, stone `#4a4a52`, timber `#6b4a2f`,
rust `#8a4a2a`; warm accents forge-orange `#e07b2a`, lantern amber `#ffb347`, crop gold `#d9a440`;
foliage `#5a7a3a`/`#7a9a4a`; sky storm-grey `#3a4150`, fog `#9aa0ab`; magic purple `#7a4fd0` (Skypillar
only — used on logo accent). Nothing clean or saturated; weather-beaten, hand-built look.

## Game data IDs (binding)

- **Items** — seeds: `sky_moss_seed`, `cloud_root_seed`, `wheat_seed`, `herb_seed`; crops: `sky_moss`,
  `cloud_root`, `wheat`, `herbs`; materials: `scrap`, `wood`, `stone`, `iron_ore`, `coal`, `rope`, `nails`,
  `skynet_frame`; processed: `flour`, `dried_herbs`.
- **Crops** (id, tier, growth min, stages=4, water/min, yield): `sky_moss` starter 2min 1.0 → `sky_moss` 1-2;
  `cloud_root` starter 3min 1.5 → `cloud_root` 1-3; `storm_wheat` staple 10min 2.0 → `wheat` 2-4
  (seed `wheat_seed`); `herb_plant` staple 8min 1.8 → `herbs` 1-3 (seed `herb_seed`).
- **Recipes**: `wheat_to_flour` (StoneMill: 3 wheat → 2 flour, 15s); `herbs_drying` (DryingRack: 2 herbs →
  2 dried_herbs, 20s, weather-sensitive — rain RUINS batch: publish `WorkshopRuinedEvent`, lose inputs);
  `ore_to_nails` (Forge: 2 iron_ore + fuel 1 coal → 4 nails, 25s); `scrap_to_skynet_frame` (Forge: 3 scrap +
  fuel 1 coal → 1 skynet_frame, 30s).
- **Structures** (id → build cost): `shelter` 5 wood+3 scrap; `rain_catcher` 3 scrap+2 rope; `windbreak`
  4 wood; `path` 2 stone; `scaffolding` 8 wood+5 scrap+3 nails (placed on edge cell → `IslandExpansion.Expand`);
  `skynet` 1 skynet_frame+2 rope (cliff-edge cells only; passively accrues loot on a real-time timer,
  collected on interact); `crate` 4 wood (10 slots); `barrel` 3 wood+1 scrap (8 slots); `drying_rack`
  4 wood+2 rope; `stone_mill` 6 stone+4 wood+2 nails; `forge` 8 stone+2 scrap+2 nails.
- **Debris loot table** (weight, min-max): scrap 30 1-3; wood 25 1-4; stone 20 1-3; iron_ore 10 1-2;
  coal 8 1-2; rope 5 1-2; wheat_seed 1.5 1; herb_seed 0.5 1. Storm/gale variant doubles iron_ore/coal/rope
  weights. Skynet table = storm variant.
- New game starting inventory: 4 sky_moss_seed, 3 cloud_root_seed, 2 wheat_seed, 2 herb_seed, 4 wood, 2 scrap.

## Behavioral conventions

- Time: `GameTimeClock` ticked from `GameManager.Update`; ALL simulation listens to `GameTickEvent`
  (never `Update` for sim logic; `Update` only for input/visuals). Pause = stop ticking the clock.
- Weather drives everything via `WeatherChangedEvent` + per-tick queries of `WeatherManager.Instance.CurrentWeather`.
  Audio/visual cues per spec §5 are events → `WeatherEffects`/`AudioCueSystem` listeners.
- Singletons: plan-style `public static X Instance` MonoBehaviours, created/wired ONLY by `Bootstrap`.
- All MonoBehaviours must tolerate sprites failing to load (null-check, magenta fallback square) so the
  game still runs if art regenerates.

## Verification harness (tools/clr-harness)

`tools/check.sh` compiles ALL of `Assets/Scripts` + `Assets/Tests` against **UnityEngine stub assemblies**
(`tools/clr-harness/UnityStubs/`) with .NET 8 and runs the NUnit tests. Stub classes are declared
`partial` — when you use a UnityEngine API the stubs lack, ADD it in a NEW file
`UnityStubs/Stubs.<YourArea>.cs` (never edit another agent's stub file). Stubs are minimal: correct
signatures, plausible no-op/math behavior. **Every agent must end with `tools/check.sh` passing for the
files they own.** Real Unity behavior (rendering, physics callbacks) is verified later in the editor;
the harness catches compile errors and logic bugs only.
