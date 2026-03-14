# Sky Harvest MVP Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-player survival-farming MVP where players generate a procedural sky island, farm crops affected by weather, process goods in workshops, scavenge debris, build structures, and save/load their progress.

**Architecture:** Unity project using a component-based entity system with ScriptableObjects for data definitions (crops, recipes, items, weather states). Event-driven architecture where systems communicate through a central EventBus. Game-time based (time only passes while playing). Hidden coarse grid with terrain-aware snapping for placement. Weather as a state machine broadcasting events to all listeners.

**Tech Stack:** Unity 2022 LTS (or latest LTS), C#, Unity Test Framework (NUnit), Universal Render Pipeline (URP), Cinemachine for camera, Unity Input System for input abstraction.

**Spec:** `docs/superpowers/specs/2026-03-14-sky-harvest-design.md`
**Pending Decisions (use "Leans" as defaults):** `docs/superpowers/specs/2026-03-14-sky-harvest-pending-decisions.md`

---

## File Structure

```
Assets/
  Scripts/
    Core/
      EventBus.cs                  — Static event pub/sub system
      GameManager.cs               — Game state, time management, scene flow
      GameTime.cs                  — Game clock (day/night cycle, tick system)
      InputManager.cs              — Input abstraction (keyboard + future touch)
      Constants.cs                 — Grid size, timing constants, layer masks
    Data/
      ItemDatabase.cs              — Runtime item lookup by ID
      ItemData.cs                  — ScriptableObject: item definition (name, icon, stack size, category)
      CropData.cs                  — ScriptableObject: crop definition (growth stages, needs, timings, yield)
      RecipeData.cs                — ScriptableObject: workshop recipe (inputs, output, duration, workshop type)
      WeatherStateData.cs          — ScriptableObject: weather state (effects, transitions, ambient cue settings)
      StructureData.cs             — ScriptableObject: structure definition (cost, size, properties)
      LootTableData.cs             — ScriptableObject: weighted loot table for debris
    Island/
      IslandGenerator.cs           — Procedural island mesh/terrain generation
      IslandData.cs                — Runtime island state (terrain map, soil map, placed objects)
      TerrainType.cs               — Enum + terrain type properties
      SoilPatch.cs                 — Component: soil quality, nutrients, water level per patch
      IslandExpansion.cs           — Scaffolding placement, edge extension logic
    Player/
      PlayerController.cs          — Isometric movement, animation state
      PlayerInventory.cs           — Item slots, hotbar, carry capacity
      InteractionSystem.cs         — Proximity detection, context-sensitive interact
      ToolSystem.cs                — Equipped tool, tool actions, durability
    Farming/
      CropInstance.cs              — Component: individual planted crop state
      CropGrowthSystem.cs          — Manages all crop growth ticks, evaluates needs
      FarmingActions.cs            — Plant, water, harvest, till actions
    Weather/
      WeatherManager.cs            — State machine, transition logic, event broadcasting
      WeatherEffects.cs            — Applies weather effects to world (rain particles, wind, lighting)
      WeatherAmbientCues.cs        — Sky colour, cloud movement, wind direction changes
    Building/
      BuildModeController.cs       — Enter/exit build mode, ghost preview, placement validation
      Structure.cs                 — Component: placed structure with properties
      StructureRegistry.cs         — Tracks all placed structures for save/load
    Workshop/
      WorkshopBase.cs              — Abstract base: input slots, output, processing timer
      DryingRack.cs                — Extends WorkshopBase: weather-dependent drying
      StoneMill.cs                 — Extends WorkshopBase: grain/seed processing
      Forge.cs                     — Extends WorkshopBase: fuel-consuming metal work
    Storage/
      StorageContainer.cs          — Component: stores items, capacity, type filtering
      StorageProximity.cs          — Detects nearby storage for workshop auto-pull
    Debris/
      DebrisSpawner.cs             — Spawns debris on timers, weather-affected frequency
      DebrisObject.cs              — Component: drifting debris, interaction, loot resolution
    UI/
      HUDController.cs             — Hotbar, equipped tool, time display, minimap toggle
      InventoryUI.cs               — Full inventory screen
      InspectorPanel.cs            — On-demand: crop health, soil quality, structure condition
      WorkshopUI.cs                — Recipe selection, progress bar, fuel gauge
      StorageUI.cs                 — Storage contents grid
      BuildMenuUI.cs               — Structure selection during build mode
    SaveLoad/
      SaveManager.cs               — Serialize/deserialize full game state
      IslandSaveData.cs            — Serializable island state
      PlayerSaveData.cs            — Serializable player state
      WorldSaveData.cs             — Top-level save container
  Data/
    Items/                         — ItemData ScriptableObject assets
    Crops/                         — CropData ScriptableObject assets
    Recipes/                       — RecipeData ScriptableObject assets
    Weather/                       — WeatherStateData ScriptableObject assets
    Structures/                    — StructureData ScriptableObject assets
    LootTables/                    — LootTableData ScriptableObject assets
  Prefabs/
    Player/
    Crops/
    Structures/
    Workshops/
    Debris/
    UI/
  Scenes/
    MainMenu.unity
    Game.unity
  Art/
    Terrain/
    Crops/
    Structures/
    Player/
    UI/
    VFX/
  Tests/
    EditMode/
      EventBusTests.cs
      InventoryTests.cs
      CropGrowthTests.cs
      WeatherStateMachineTests.cs
      RecipeTests.cs
      LootTableTests.cs
      SaveLoadTests.cs
      SoilTests.cs
    PlayMode/
      PlayerMovementTests.cs
      InteractionTests.cs
      BuildPlacementTests.cs
```

---

## Chunk 1: Project Foundation & Core Systems

### Task 1: Unity Project Setup

**Files:**
- Create: Unity project at repo root (or subfolder `SkyHarvest/`)
- Create: `.gitignore` for Unity
- Create: `Assets/Scenes/Game.unity`
- Create: `Assets/Scripts/Core/Constants.cs`

- [ ] **Step 1: Create Unity project**

Create a new Unity 3D (URP) project. Use Unity 2022 LTS or latest LTS.

```
Unity Hub → New Project → 3D (URP) → Name: SkyHarvest
```

- [ ] **Step 2: Configure project settings**

- Set camera to orthographic for isometric view
- Install packages: Input System, Cinemachine, Test Framework (should be included)
- Set target platform to PC Standalone
- Create folder structure as defined above

- [ ] **Step 3: Add Unity .gitignore**

Use the standard Unity `.gitignore` from GitHub's gitignore templates.

- [ ] **Step 4: Create Constants.cs**

```csharp
// Assets/Scripts/Core/Constants.cs
namespace SkyHarvest.Core
{
    public static class Constants
    {
        // Grid
        public const float GridCellSize = 1f;
        public const int DefaultIslandRadius = 12;

        // Time
        public const float SecondsPerGameMinute = 1f;
        public const float MinutesPerGameHour = 60f;
        public const float HoursPerGameDay = 24f;

        // Farming
        public const float StarterCropGrowthMinutes = 3f;
        public const float StapleCropGrowthMinutes = 15f;
        public const float MaxSoilNutrients = 100f;
        public const float MaxSoilWater = 100f;

        // Weather
        public const float MinWeatherDurationMinutes = 5f;
        public const float MaxWeatherDurationMinutes = 10f;

        // Debris
        public const float BaseDebrisIntervalSeconds = 45f;
        public const float GaleWindDebrisMultiplier = 0.4f;

        // Layers
        public const string InteractableLayer = "Interactable";
        public const string TerrainLayer = "Terrain";
        public const string StructureLayer = "Structure";
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: initialize Unity project with URP, folder structure, and constants"
```

---

### Task 2: Event Bus System

**Files:**
- Create: `Assets/Scripts/Core/EventBus.cs`
- Create: `Assets/Tests/EditMode/EventBusTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Assets/Tests/EditMode/EventBusTests.cs
using NUnit.Framework;
using SkyHarvest.Core;

public class EventBusTests
{
    struct TestEvent { public int Value; }
    struct OtherEvent { public string Name; }

    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Subscribe_And_Publish_Delivers_Event()
    {
        int received = 0;
        EventBus.Subscribe<TestEvent>(e => received = e.Value);
        EventBus.Publish(new TestEvent { Value = 42 });
        Assert.AreEqual(42, received);
    }

    [Test]
    public void Unsubscribe_Stops_Delivery()
    {
        int received = 0;
        void Handler(TestEvent e) => received = e.Value;
        EventBus.Subscribe<TestEvent>(Handler);
        EventBus.Unsubscribe<TestEvent>(Handler);
        EventBus.Publish(new TestEvent { Value = 99 });
        Assert.AreEqual(0, received);
    }

    [Test]
    public void Different_Event_Types_Are_Independent()
    {
        int intReceived = 0;
        string strReceived = null;
        EventBus.Subscribe<TestEvent>(e => intReceived = e.Value);
        EventBus.Subscribe<OtherEvent>(e => strReceived = e.Name);
        EventBus.Publish(new TestEvent { Value = 7 });
        Assert.AreEqual(7, intReceived);
        Assert.IsNull(strReceived);
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL**

Run: Unity Test Runner → EditMode → Run All
Expected: Compilation error — `EventBus` does not exist.

- [ ] **Step 3: Implement EventBus**

```csharp
// Assets/Scripts/Core/EventBus.cs
using System;
using System.Collections.Generic;

namespace SkyHarvest.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();
            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) return;
            foreach (var handler in _subscribers[type].ToArray())
                ((Action<T>)handler)(evt);
        }

        public static void Clear() => _subscribers.Clear();
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/EventBus.cs Assets/Tests/EditMode/EventBusTests.cs
git commit -m "feat: add static EventBus for decoupled system communication"
```

---

### Task 3: Game Time System

**Files:**
- Create: `Assets/Scripts/Core/GameTime.cs`
- Create: `Assets/Scripts/Core/GameEvents.cs`
- Create: `Assets/Tests/EditMode/GameTimeTests.cs`

- [ ] **Step 1: Create shared event definitions**

```csharp
// Assets/Scripts/Core/GameEvents.cs
namespace SkyHarvest.Core
{
    public struct GameTickEvent
    {
        public float DeltaMinutes;
        public float TotalGameMinutes;
    }

    public struct HourChangedEvent { public int Hour; }
    public struct DayChangedEvent { public int Day; }

    // Weather events (used later)
    public struct WeatherChangedEvent
    {
        public WeatherType Previous;
        public WeatherType Current;
    }

    public enum WeatherType
    {
        ClearSkies,
        LightRain,
        HeavyStorm,
        GaleWinds,
        FogBank,
        SkyDrought
    }
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// Assets/Tests/EditMode/GameTimeTests.cs
using NUnit.Framework;
using SkyHarvest.Core;

public class GameTimeTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Tick_Advances_TotalMinutes()
    {
        var time = new GameTimeClock();
        time.Tick(Constants.SecondsPerGameMinute * 5f);
        Assert.AreEqual(5f, time.TotalMinutes, 0.01f);
    }

    [Test]
    public void Tick_Publishes_GameTickEvent()
    {
        var time = new GameTimeClock();
        float received = 0;
        EventBus.Subscribe<GameTickEvent>(e => received = e.DeltaMinutes);
        time.Tick(Constants.SecondsPerGameMinute * 3f);
        Assert.AreEqual(3f, received, 0.01f);
    }

    [Test]
    public void Hour_Change_Publishes_Event()
    {
        var time = new GameTimeClock();
        int hourReceived = -1;
        EventBus.Subscribe<HourChangedEvent>(e => hourReceived = e.Hour);
        // Advance 60 game minutes
        time.Tick(Constants.SecondsPerGameMinute * Constants.MinutesPerGameHour);
        Assert.AreEqual(1, hourReceived);
    }

    [Test]
    public void CurrentHour_Wraps_At_24()
    {
        var time = new GameTimeClock();
        float fullDay = Constants.SecondsPerGameMinute * Constants.MinutesPerGameHour * Constants.HoursPerGameDay;
        time.Tick(fullDay);
        Assert.AreEqual(0, time.CurrentHour);
        Assert.AreEqual(1, time.CurrentDay);
    }
}
```

- [ ] **Step 3: Implement GameTimeClock**

```csharp
// Assets/Scripts/Core/GameTime.cs
namespace SkyHarvest.Core
{
    public class GameTimeClock
    {
        public float TotalMinutes { get; private set; }
        public int CurrentHour => (int)(TotalMinutes / Constants.MinutesPerGameHour) % (int)Constants.HoursPerGameDay;
        public int CurrentDay => (int)(TotalMinutes / (Constants.MinutesPerGameHour * Constants.HoursPerGameDay));

        private int _lastHour = -1;
        private int _lastDay = -1;

        public void Tick(float realDeltaSeconds)
        {
            float deltaMinutes = realDeltaSeconds / Constants.SecondsPerGameMinute;
            TotalMinutes += deltaMinutes;

            EventBus.Publish(new GameTickEvent
            {
                DeltaMinutes = deltaMinutes,
                TotalGameMinutes = TotalMinutes
            });

            int hour = CurrentHour;
            if (hour != _lastHour && _lastHour != -1)
                EventBus.Publish(new HourChangedEvent { Hour = hour });
            _lastHour = hour;

            int day = CurrentDay;
            if (day != _lastDay && _lastDay != -1)
                EventBus.Publish(new DayChangedEvent { Day = day });
            _lastDay = day;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/GameTime.cs Assets/Scripts/Core/GameEvents.cs Assets/Tests/EditMode/GameTimeTests.cs
git commit -m "feat: add game time clock with event-driven hour/day changes"
```

---

### Task 4: Item System & Inventory

**Files:**
- Create: `Assets/Scripts/Data/ItemData.cs`
- Create: `Assets/Scripts/Data/ItemDatabase.cs`
- Create: `Assets/Scripts/Player/PlayerInventory.cs`
- Create: `Assets/Tests/EditMode/InventoryTests.cs`

- [ ] **Step 1: Create ItemData ScriptableObject**

```csharp
// Assets/Scripts/Data/ItemData.cs
using UnityEngine;

namespace SkyHarvest.Data
{
    public enum ItemCategory { Seed, Crop, Material, Tool, Processed, Fuel, Misc }

    [CreateAssetMenu(fileName = "NewItem", menuName = "SkyHarvest/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public ItemCategory Category;
        public int MaxStackSize = 99;
    }
}
```

- [ ] **Step 2: Write failing inventory tests**

```csharp
// Assets/Tests/EditMode/InventoryTests.cs
using NUnit.Framework;
using SkyHarvest.Player;
using UnityEngine;

public class InventoryTests
{
    private Inventory _inv;

    [SetUp]
    public void SetUp()
    {
        _inv = new Inventory(slotCount: 20);
    }

    [Test]
    public void AddItem_To_Empty_Inventory_Returns_True()
    {
        Assert.IsTrue(_inv.TryAdd("wheat_seed", 5));
        Assert.AreEqual(5, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void AddItem_Stacks_Same_Item()
    {
        _inv.TryAdd("wheat_seed", 5);
        _inv.TryAdd("wheat_seed", 3);
        Assert.AreEqual(8, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void RemoveItem_Reduces_Count()
    {
        _inv.TryAdd("wheat_seed", 10);
        Assert.IsTrue(_inv.TryRemove("wheat_seed", 4));
        Assert.AreEqual(6, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void RemoveItem_Fails_If_Not_Enough()
    {
        _inv.TryAdd("wheat_seed", 3);
        Assert.IsFalse(_inv.TryRemove("wheat_seed", 5));
        Assert.AreEqual(3, _inv.GetCount("wheat_seed"));
    }

    [Test]
    public void HasItems_Returns_Correctly()
    {
        _inv.TryAdd("scrap", 10);
        Assert.IsTrue(_inv.Has("scrap", 10));
        Assert.IsTrue(_inv.Has("scrap", 5));
        Assert.IsFalse(_inv.Has("scrap", 11));
    }

    [Test]
    public void Full_Inventory_Rejects_New_Item()
    {
        var small = new Inventory(slotCount: 2);
        small.TryAdd("item_a", 1);
        small.TryAdd("item_b", 1);
        Assert.IsFalse(small.TryAdd("item_c", 1));
    }
}
```

- [ ] **Step 3: Implement Inventory**

```csharp
// Assets/Scripts/Player/PlayerInventory.cs
using System.Collections.Generic;
using System.Linq;

namespace SkyHarvest.Player
{
    public class InventorySlot
    {
        public string ItemId;
        public int Count;
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    }

    public class Inventory
    {
        public InventorySlot[] Slots { get; }

        public Inventory(int slotCount)
        {
            Slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++)
                Slots[i] = new InventorySlot();
        }

        public bool TryAdd(string itemId, int count)
        {
            // Try stack into existing slot first
            var existing = Slots.FirstOrDefault(s => s.ItemId == itemId);
            if (existing != null)
            {
                existing.Count += count;
                return true;
            }

            // Try empty slot
            var empty = Slots.FirstOrDefault(s => s.IsEmpty);
            if (empty == null) return false;

            empty.ItemId = itemId;
            empty.Count = count;
            return true;
        }

        public bool TryRemove(string itemId, int count)
        {
            if (!Has(itemId, count)) return false;

            int remaining = count;
            foreach (var slot in Slots.Where(s => s.ItemId == itemId))
            {
                int take = System.Math.Min(slot.Count, remaining);
                slot.Count -= take;
                remaining -= take;
                if (slot.Count <= 0)
                {
                    slot.ItemId = null;
                    slot.Count = 0;
                }
                if (remaining <= 0) break;
            }
            return true;
        }

        public bool Has(string itemId, int count = 1)
        {
            return GetCount(itemId) >= count;
        }

        public int GetCount(string itemId)
        {
            return Slots.Where(s => s.ItemId == itemId).Sum(s => s.Count);
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Data/ItemData.cs Assets/Scripts/Player/PlayerInventory.cs Assets/Tests/EditMode/InventoryTests.cs
git commit -m "feat: add item data and inventory system with stacking and slot management"
```

---

### Task 5: Input Manager

**Files:**
- Create: `Assets/Scripts/Core/InputManager.cs`
- Create: `Assets/InputSystem/SkyHarvestInputActions.inputactions` (via Unity Input System asset)

- [ ] **Step 1: Create Input Actions asset**

In Unity: Create → Input Actions → name `SkyHarvestInputActions`.

Define action maps:
- **Gameplay**: Move (Vector2), Interact (Button), ToggleBuildMode (Button), OpenInventory (Button), Hotbar1-6 (Buttons), Cancel (Button)
- **BuildMode**: PlaceStructure (Button), RotateStructure (Button), Cancel (Button)
- **UI**: Navigate (Vector2), Submit (Button), Cancel (Button)

Bind Gameplay/Move to WASD + Arrow Keys. Interact to E. ToggleBuildMode to B. OpenInventory to Tab.

- [ ] **Step 2: Create InputManager wrapper**

```csharp
// Assets/Scripts/Core/InputManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyHarvest.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private SkyHarvestInputActions _actions;

        public Vector2 MoveInput => _actions.Gameplay.Move.ReadValue<Vector2>();
        public bool InteractPressed => _actions.Gameplay.Interact.WasPressedThisFrame();
        public bool BuildModeToggled => _actions.Gameplay.ToggleBuildMode.WasPressedThisFrame();
        public bool InventoryToggled => _actions.Gameplay.OpenInventory.WasPressedThisFrame();
        public bool CancelPressed => _actions.Gameplay.Cancel.WasPressedThisFrame();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _actions = new SkyHarvestInputActions();
        }

        private void OnEnable() => _actions.Gameplay.Enable();
        private void OnDisable() => _actions.Gameplay.Disable();

        public void SwitchToBuildMode()
        {
            _actions.Gameplay.Disable();
            _actions.BuildMode.Enable();
        }

        public void SwitchToGameplay()
        {
            _actions.BuildMode.Disable();
            _actions.Gameplay.Enable();
        }
    }
}
```

- [ ] **Step 3: Manual verification**

Enter Play mode. Verify WASD input registers via debug log in a temp test script. Verify E key and B key detected.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/InputManager.cs Assets/InputSystem/
git commit -m "feat: add input manager with gameplay and build mode action maps"
```

---

## Chunk 2: Island Generation & Player

### Task 6: Island Terrain Data Model

**Files:**
- Create: `Assets/Scripts/Island/TerrainType.cs`
- Create: `Assets/Scripts/Island/IslandData.cs`
- Create: `Assets/Scripts/Island/SoilPatch.cs`
- Create: `Assets/Tests/EditMode/SoilTests.cs`

- [ ] **Step 1: Define terrain types**

```csharp
// Assets/Scripts/Island/TerrainType.cs
namespace SkyHarvest.Island
{
    public enum TerrainType
    {
        FertileValley,   // Best soil, flood risk
        RockyPlateau,    // Safe, poor soil, good for workshops
        CliffEdge,       // Dangerous, best scavenging
        NaturalSpring,   // Rare, irrigation advantage
        WindCorridor     // Exposed, good for windmills
    }

    public static class TerrainProperties
    {
        public static float BaseSoilQuality(TerrainType type) => type switch
        {
            TerrainType.FertileValley => 80f,
            TerrainType.RockyPlateau => 20f,
            TerrainType.CliffEdge => 10f,
            TerrainType.NaturalSpring => 60f,
            TerrainType.WindCorridor => 30f,
            _ => 50f
        };

        public static float FloodRisk(TerrainType type) => type switch
        {
            TerrainType.FertileValley => 0.6f,
            TerrainType.RockyPlateau => 0.05f,
            TerrainType.CliffEdge => 0.1f,
            _ => 0.2f
        };

        public static float WindExposure(TerrainType type) => type switch
        {
            TerrainType.WindCorridor => 0.9f,
            TerrainType.CliffEdge => 0.7f,
            TerrainType.RockyPlateau => 0.4f,
            TerrainType.FertileValley => 0.2f,
            TerrainType.NaturalSpring => 0.3f,
            _ => 0.5f
        };

        public static bool CanPlaceCrops(TerrainType type) =>
            type != TerrainType.CliffEdge;

        public static bool HasWaterSource(TerrainType type) =>
            type == TerrainType.NaturalSpring;
    }
}
```

- [ ] **Step 2: Write soil tests**

```csharp
// Assets/Tests/EditMode/SoilTests.cs
using NUnit.Framework;
using SkyHarvest.Island;

public class SoilTests
{
    [Test]
    public void New_Soil_Has_Terrain_Based_Quality()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        Assert.AreEqual(80f, soil.Quality, 0.1f);
    }

    [Test]
    public void Watering_Increases_Water_Level()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(30f);
        Assert.AreEqual(30f, soil.WaterLevel, 0.1f);
    }

    [Test]
    public void Water_Clamped_To_Max()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(200f);
        Assert.AreEqual(100f, soil.WaterLevel, 0.1f);
    }

    [Test]
    public void Planting_Same_Crop_Depletes_Nutrients()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        soil.RecordHarvest("wheat");
        Assert.Less(soil.Nutrients, 100f);
    }

    [Test]
    public void Composting_Restores_Nutrients()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        soil.RecordHarvest("wheat");
        float depleted = soil.Nutrients;
        soil.ApplyCompost(20f);
        Assert.Greater(soil.Nutrients, depleted);
    }

    [Test]
    public void Crop_Rotation_Reduces_Depletion()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.RecordHarvest("wheat");
        float afterWheat = soil.Nutrients;
        soil.RecordHarvest("beans"); // different crop
        float afterBeans = soil.Nutrients;
        // Different crop should deplete less than same crop
        float wheatDepletion = 100f - afterWheat;
        float beansDepletion = afterWheat - afterBeans;
        Assert.Less(beansDepletion, wheatDepletion);
    }
}
```

- [ ] **Step 3: Implement SoilState**

```csharp
// Assets/Scripts/Island/SoilPatch.cs
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class SoilState
    {
        public float Quality { get; private set; }
        public float WaterLevel { get; private set; }
        public float Nutrients { get; private set; }
        public TerrainType Terrain { get; }

        private string _lastCropHarvested;
        private const float SameCropDepletion = 15f;
        private const float DifferentCropDepletion = 5f;

        public SoilState(TerrainType terrain)
        {
            Terrain = terrain;
            Quality = TerrainProperties.BaseSoilQuality(terrain);
            WaterLevel = 0f;
            Nutrients = Constants.MaxSoilNutrients;
        }

        public void AddWater(float amount)
        {
            WaterLevel = System.Math.Min(WaterLevel + amount, Constants.MaxSoilWater);
        }

        public void ConsumeWater(float amount)
        {
            WaterLevel = System.Math.Max(WaterLevel - amount, 0f);
        }

        public void RecordHarvest(string cropId)
        {
            float depletion = cropId == _lastCropHarvested
                ? SameCropDepletion
                : DifferentCropDepletion;
            Nutrients = System.Math.Max(0f, Nutrients - depletion);
            _lastCropHarvested = cropId;
        }

        public void ApplyCompost(float amount)
        {
            Nutrients = System.Math.Min(Nutrients + amount, Constants.MaxSoilNutrients);
        }

        public float GrowthMultiplier()
        {
            float waterFactor = WaterLevel > 10f ? 1f : WaterLevel / 10f;
            float nutrientFactor = Nutrients / Constants.MaxSoilNutrients;
            float qualityFactor = Quality / 100f;
            return waterFactor * nutrientFactor * qualityFactor;
        }
    }
}
```

- [ ] **Step 4: Create IslandData container**

```csharp
// Assets/Scripts/Island/IslandData.cs
using System.Collections.Generic;
using UnityEngine;

namespace SkyHarvest.Island
{
    public class IslandCell
    {
        public Vector2Int GridPos;
        public TerrainType Terrain;
        public float Elevation;
        public SoilState Soil;
        public bool IsEdge;
    }

    public class IslandData
    {
        public Dictionary<Vector2Int, IslandCell> Cells { get; } = new();
        public int Seed { get; }

        public IslandData(int seed) { Seed = seed; }

        public IslandCell GetCell(Vector2Int pos) =>
            Cells.TryGetValue(pos, out var cell) ? cell : null;

        public bool IsValidPosition(Vector2Int pos) => Cells.ContainsKey(pos);
    }
}
```

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Island/ Assets/Tests/EditMode/SoilTests.cs
git commit -m "feat: add terrain types, soil system with nutrient depletion and crop rotation"
```

---

### Task 7: Procedural Island Generator

**Files:**
- Create: `Assets/Scripts/Island/IslandGenerator.cs`
- Create: placeholder art (flat colored tiles per terrain type)

- [ ] **Step 1: Implement island generation**

```csharp
// Assets/Scripts/Island/IslandGenerator.cs
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class IslandGenerator : MonoBehaviour
    {
        [SerializeField] private int _seed = -1; // -1 = random
        [SerializeField] private int _radius = Constants.DefaultIslandRadius;
        [SerializeField] private float _noiseScale = 0.15f;
        [SerializeField] private float _elevationScale = 0.1f;

        public IslandData Generate()
        {
            int seed = _seed < 0 ? Random.Range(0, 999999) : _seed;
            var island = new IslandData(seed);
            var rng = new System.Random(seed);

            // Use Perlin noise offset by seed for shape
            float offsetX = (float)rng.NextDouble() * 1000f;
            float offsetY = (float)rng.NextDouble() * 1000f;

            // Determine if this island has a spring (30% chance)
            bool hasSpring = rng.NextDouble() < 0.3;
            Vector2Int springPos = new Vector2Int(
                rng.Next(-_radius / 3, _radius / 3),
                rng.Next(-_radius / 3, _radius / 3)
            );

            for (int x = -_radius; x <= _radius; x++)
            {
                for (int y = -_radius; y <= _radius; y++)
                {
                    var pos = new Vector2Int(x, y);
                    float distFromCenter = pos.magnitude / _radius;

                    // Island shape: use noise + distance falloff
                    float noise = Mathf.PerlinNoise(
                        (x + offsetX) * _noiseScale,
                        (y + offsetY) * _noiseScale
                    );
                    float threshold = 1f - distFromCenter + (noise - 0.5f) * 0.6f;

                    if (threshold <= 0.1f) continue; // Not part of island

                    bool isEdge = threshold < 0.25f;
                    float elevation = Mathf.PerlinNoise(
                        (x + offsetX + 500) * _elevationScale,
                        (y + offsetY + 500) * _elevationScale
                    ) * 3f;

                    // Determine terrain type
                    TerrainType terrain;
                    if (hasSpring && Vector2Int.Distance(pos, springPos) < 2)
                        terrain = TerrainType.NaturalSpring;
                    else if (isEdge)
                        terrain = TerrainType.CliffEdge;
                    else if (elevation > 2.0f && noise > 0.55f)
                        terrain = TerrainType.WindCorridor;
                    else if (elevation > 1.5f)
                        terrain = TerrainType.RockyPlateau;
                    else
                        terrain = TerrainType.FertileValley;

                    island.Cells[pos] = new IslandCell
                    {
                        GridPos = pos,
                        Terrain = terrain,
                        Elevation = elevation,
                        Soil = new SoilState(terrain),
                        IsEdge = isEdge
                    };
                }
            }

            return island;
        }
    }
}
```

- [ ] **Step 2: Create basic terrain visualization**

Create a MonoBehaviour `IslandRenderer` that instantiates a colored cube/quad per cell, tinted by terrain type. This is placeholder art — just enough to see the island shape.

Colors: FertileValley = dark green, RockyPlateau = grey, CliffEdge = brown, NaturalSpring = blue-green, WindCorridor = light grey.

Position cells in isometric space: `worldX = (gridX - gridY) * 0.5f`, `worldY = elevation * 0.25f + (gridX + gridY) * 0.25f`.

- [ ] **Step 3: Manual verification**

Enter Play mode. A procedural island shape should appear with varied terrain colors and visible elevation. Generate a few different seeds and verify islands look distinct.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Island/IslandGenerator.cs Assets/Scripts/Island/IslandRenderer.cs
git commit -m "feat: procedural island generation with terrain types and elevation"
```

---

### Task 8: Player Controller (Isometric Movement)

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Create: `Assets/Prefabs/Player/Player.prefab`

- [ ] **Step 1: Implement isometric movement**

```csharp
// Assets/Scripts/Player/PlayerController.cs
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;

        private Rigidbody _rb;

        // Isometric direction vectors
        private static readonly Vector3 IsoRight = new Vector3(1f, 0f, -1f).normalized;
        private static readonly Vector3 IsoUp = new Vector3(1f, 0f, 1f).normalized;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        private void FixedUpdate()
        {
            Vector2 input = InputManager.Instance.MoveInput;
            Vector3 direction = (IsoRight * input.x + IsoUp * input.y).normalized;
            _rb.linearVelocity = direction * _moveSpeed;
        }
    }
}
```

- [ ] **Step 2: Create Player prefab**

In Unity:
- Create capsule GameObject, name "Player"
- Add Rigidbody (freeze Y position, freeze all rotation, no gravity or use gravity with a ground plane)
- Add PlayerController script
- Create prefab in `Assets/Prefabs/Player/`

- [ ] **Step 3: Set up isometric camera**

Add a Camera (or use Main Camera) positioned above and angled for isometric view:
- Rotation: (30, 45, 0) for classic isometric
- Projection: Orthographic
- Size: ~8
- Add Cinemachine Virtual Camera following the player

- [ ] **Step 4: Manual verification**

Play mode. WASD moves the player character across the island in isometric directions. Camera follows smoothly.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs Assets/Prefabs/Player/ Assets/Scenes/Game.unity
git commit -m "feat: isometric player movement with Cinemachine camera follow"
```

---

### Task 9: Interaction System

**Files:**
- Create: `Assets/Scripts/Player/InteractionSystem.cs`
- Create: `Assets/Scripts/Player/IInteractable.cs`

- [ ] **Step 1: Define interactable interface**

```csharp
// Assets/Scripts/Player/IInteractable.cs
namespace SkyHarvest.Player
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerController player);
    }
}
```

- [ ] **Step 2: Implement interaction detection**

```csharp
// Assets/Scripts/Player/InteractionSystem.cs
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    public class InteractionSystem : MonoBehaviour
    {
        [SerializeField] private float _interactRadius = 1.5f;
        [SerializeField] private LayerMask _interactableMask;

        private IInteractable _currentTarget;
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            FindNearestInteractable();

            if (_currentTarget != null && InputManager.Instance.InteractPressed)
            {
                _currentTarget.Interact(_player);
            }
        }

        private void FindNearestInteractable()
        {
            var colliders = Physics.OverlapSphere(transform.position, _interactRadius, _interactableMask);
            _currentTarget = null;
            float closest = float.MaxValue;

            foreach (var col in colliders)
            {
                var interactable = col.GetComponent<IInteractable>();
                if (interactable == null) continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closest)
                {
                    closest = dist;
                    _currentTarget = interactable;
                }
            }
        }

        public IInteractable CurrentTarget => _currentTarget;
    }
}
```

- [ ] **Step 3: Manual verification**

Create a test interactable cube that logs to console on interact. Walk near it, press E, verify log appears. Walk away, press E, verify nothing happens.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/InteractionSystem.cs Assets/Scripts/Player/IInteractable.cs
git commit -m "feat: proximity-based interaction system with IInteractable interface"
```

---

### Task 10: Tool System

**Files:**
- Create: `Assets/Scripts/Player/ToolSystem.cs`

- [ ] **Step 1: Implement tool system**

```csharp
// Assets/Scripts/Player/ToolSystem.cs
using UnityEngine;

namespace SkyHarvest.Player
{
    public enum ToolType { None, Hoe, WateringCan, Sickle, Hammer }

    public class ToolSystem : MonoBehaviour
    {
        public ToolType EquippedTool { get; private set; } = ToolType.None;

        private readonly ToolType[] _toolSlots = new ToolType[]
        {
            ToolType.Hoe,
            ToolType.WateringCan,
            ToolType.Sickle,
            ToolType.Hammer
        };

        public void EquipTool(ToolType tool)
        {
            EquippedTool = tool;
            // EventBus.Publish(new ToolEquippedEvent { Tool = tool });
        }

        public void EquipBySlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _toolSlots.Length)
                EquipTool(_toolSlots[slotIndex]);
        }

        public void Unequip() => EquippedTool = ToolType.None;
    }
}
```

- [ ] **Step 2: Manual verification**

Hook up number keys 1-4 to equip tools. Verify equipped tool changes via debug UI text.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/ToolSystem.cs
git commit -m "feat: tool equip system with slot-based selection"
```

---

## Chunk 3: Weather & Farming Systems

### Task 11: Weather State Machine

**Files:**
- Create: `Assets/Scripts/Weather/WeatherManager.cs`
- Create: `Assets/Scripts/Data/WeatherStateData.cs`
- Create: `Assets/Tests/EditMode/WeatherStateMachineTests.cs`

- [ ] **Step 1: Create WeatherStateData ScriptableObject**

```csharp
// Assets/Scripts/Data/WeatherStateData.cs
using UnityEngine;

namespace SkyHarvest.Data
{
    [CreateAssetMenu(fileName = "NewWeatherState", menuName = "SkyHarvest/Weather State")]
    public class WeatherStateData : ScriptableObject
    {
        public Core.WeatherType Type;
        public string DisplayName;
        public float CropGrowthMultiplier = 1f;
        public float WaterPerMinute;          // Positive = rain, negative = evaporation
        public float WindDamageChance;         // 0-1, per crop per tick during this weather
        public float DebrisFrequencyMultiplier = 1f;
        public float FloodChancePerTick;       // For flood-prone terrain
        [Range(0, 1)] public float SunExposure = 1f;

        [Header("Transitions")]
        public WeatherTransition[] Transitions;
    }

    [System.Serializable]
    public class WeatherTransition
    {
        public WeatherStateData Target;
        [Range(0, 1)] public float Weight;
    }
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// Assets/Tests/EditMode/WeatherStateMachineTests.cs
using NUnit.Framework;
using SkyHarvest.Weather;
using SkyHarvest.Core;

public class WeatherStateMachineTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Initial_State_Is_ClearSkies()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        Assert.AreEqual(WeatherType.ClearSkies, sm.CurrentWeather);
    }

    [Test]
    public void ForceTransition_Changes_State()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        sm.ForceTransition(WeatherType.LightRain);
        Assert.AreEqual(WeatherType.LightRain, sm.CurrentWeather);
    }

    [Test]
    public void Transition_Publishes_WeatherChangedEvent()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        WeatherType received = WeatherType.ClearSkies;
        EventBus.Subscribe<WeatherChangedEvent>(e => received = e.Current);
        sm.ForceTransition(WeatherType.HeavyStorm);
        Assert.AreEqual(WeatherType.HeavyStorm, received);
    }

    [Test]
    public void TimeRemaining_Decreases_On_Tick()
    {
        var sm = new WeatherStateMachine(WeatherType.ClearSkies);
        float initial = sm.MinutesRemaining;
        sm.Tick(2f);
        Assert.Less(sm.MinutesRemaining, initial);
    }
}
```

- [ ] **Step 3: Implement WeatherStateMachine (pure logic, no MonoBehaviour)**

```csharp
// Assets/Scripts/Weather/WeatherStateMachine.cs
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherStateMachine
    {
        public WeatherType CurrentWeather { get; private set; }
        public float MinutesRemaining { get; private set; }

        private System.Random _rng;

        public WeatherStateMachine(WeatherType initial, int seed = 0)
        {
            _rng = new System.Random(seed);
            CurrentWeather = initial;
            MinutesRemaining = RollDuration();
        }

        public void Tick(float deltaMinutes)
        {
            MinutesRemaining -= deltaMinutes;
            if (MinutesRemaining <= 0f)
            {
                TransitionToNext();
            }
        }

        public void ForceTransition(WeatherType next)
        {
            var prev = CurrentWeather;
            CurrentWeather = next;
            MinutesRemaining = RollDuration();
            EventBus.Publish(new WeatherChangedEvent { Previous = prev, Current = next });
        }

        private void TransitionToNext()
        {
            // Weighted random from possible transitions
            // For MVP: simple cycle with randomness
            var options = GetTransitionWeights(CurrentWeather);
            float roll = (float)_rng.NextDouble();
            float cumulative = 0f;

            foreach (var (weather, weight) in options)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    ForceTransition(weather);
                    return;
                }
            }

            // Fallback
            ForceTransition(WeatherType.ClearSkies);
        }

        private (WeatherType, float)[] GetTransitionWeights(WeatherType current)
        {
            return current switch
            {
                WeatherType.ClearSkies => new[]
                {
                    (WeatherType.LightRain, 0.35f),
                    (WeatherType.ClearSkies, 0.30f),
                    (WeatherType.GaleWinds, 0.15f),
                    (WeatherType.FogBank, 0.10f),
                    (WeatherType.SkyDrought, 0.10f),
                },
                WeatherType.LightRain => new[]
                {
                    (WeatherType.ClearSkies, 0.30f),
                    (WeatherType.HeavyStorm, 0.25f),
                    (WeatherType.LightRain, 0.25f),
                    (WeatherType.FogBank, 0.20f),
                },
                WeatherType.HeavyStorm => new[]
                {
                    (WeatherType.LightRain, 0.40f),
                    (WeatherType.GaleWinds, 0.25f),
                    (WeatherType.ClearSkies, 0.20f),
                    (WeatherType.HeavyStorm, 0.15f),
                },
                WeatherType.GaleWinds => new[]
                {
                    (WeatherType.ClearSkies, 0.35f),
                    (WeatherType.HeavyStorm, 0.30f),
                    (WeatherType.LightRain, 0.20f),
                    (WeatherType.SkyDrought, 0.15f),
                },
                WeatherType.FogBank => new[]
                {
                    (WeatherType.ClearSkies, 0.40f),
                    (WeatherType.LightRain, 0.35f),
                    (WeatherType.FogBank, 0.25f),
                },
                WeatherType.SkyDrought => new[]
                {
                    (WeatherType.ClearSkies, 0.40f),
                    (WeatherType.GaleWinds, 0.25f),
                    (WeatherType.SkyDrought, 0.20f),
                    (WeatherType.HeavyStorm, 0.15f),
                },
                _ => new[] { (WeatherType.ClearSkies, 1f) }
            };
        }

        private float RollDuration()
        {
            return Constants.MinWeatherDurationMinutes +
                (float)_rng.NextDouble() * (Constants.MaxWeatherDurationMinutes - Constants.MinWeatherDurationMinutes);
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Weather/ Assets/Scripts/Data/WeatherStateData.cs Assets/Tests/EditMode/WeatherStateMachineTests.cs
git commit -m "feat: weather state machine with weighted random transitions and event broadcasting"
```

---

### Task 12: Weather Visual Effects

**Files:**
- Create: `Assets/Scripts/Weather/WeatherManager.cs` (MonoBehaviour wrapper)
- Create: `Assets/Scripts/Weather/WeatherEffects.cs`
- Create: `Assets/Scripts/Weather/WeatherAmbientCues.cs`

- [ ] **Step 1: Create WeatherManager MonoBehaviour**

```csharp
// Assets/Scripts/Weather/WeatherManager.cs (MonoBehaviour wrapper)
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        public WeatherStateMachine StateMachine { get; private set; }
        public WeatherType CurrentWeather => StateMachine.CurrentWeather;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            StateMachine = new WeatherStateMachine(WeatherType.ClearSkies);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameTickEvent>(OnGameTick);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameTickEvent>(OnGameTick);
        }

        private void OnGameTick(GameTickEvent e)
        {
            StateMachine.Tick(e.DeltaMinutes);
        }
    }
}
```

- [ ] **Step 2: Create WeatherEffects for particles**

Create `WeatherEffects.cs` that listens for `WeatherChangedEvent` and toggles particle systems:
- Rain particle system (light rain = few particles, heavy storm = dense + wind offset)
- Wind particle system (streaks for gale winds)
- Fog overlay (fog bank)
- Clear all particles for clear skies / drought

Use Unity's built-in particle system. Create child GameObjects under a WeatherEffects parent with pre-configured particle systems.

- [ ] **Step 3: Create WeatherAmbientCues**

`WeatherAmbientCues.cs` listens for `WeatherChangedEvent` and gradually shifts:
- Directional light colour and intensity (darker in storms, warm in clear)
- Skybox/background colour tint
- Wind direction indicator (a subtle particle or UI element showing wind bearing)

Use `Color.Lerp` over time for smooth transitions. The transition itself is the "ambient cue" — players see the sky darkening before the storm hits.

- [ ] **Step 4: Manual verification**

Play mode. Let weather cycle through states. Verify visual transitions look distinct and weather is readable from visuals alone.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Weather/
git commit -m "feat: weather visual effects and ambient cue system"
```

---

### Task 13: Crop Data & Growth System

**Files:**
- Create: `Assets/Scripts/Data/CropData.cs`
- Create: `Assets/Scripts/Farming/CropInstance.cs`
- Create: `Assets/Scripts/Farming/CropGrowthSystem.cs`
- Create: `Assets/Tests/EditMode/CropGrowthTests.cs`

- [ ] **Step 1: Create CropData ScriptableObject**

```csharp
// Assets/Scripts/Data/CropData.cs
using UnityEngine;

namespace SkyHarvest.Data
{
    [CreateAssetMenu(fileName = "NewCrop", menuName = "SkyHarvest/Crop Data")]
    public class CropData : ScriptableObject
    {
        public string CropId;
        public string DisplayName;
        public Sprite[] GrowthStageSprites; // Visual per stage
        public int GrowthStages = 4;
        public float GrowthTimeMinutes = 5f; // Total time in ideal conditions
        public float WaterConsumptionPerMinute = 2f;
        public float MinSoilQuality = 10f;
        public float IdealSunExposure = 0.7f;
        public float WindDamageVulnerability = 0.5f; // 0 = resistant, 1 = fragile
        public string HarvestYieldItemId;
        public int HarvestYieldMin = 1;
        public int HarvestYieldMax = 3;
        public string SeedItemId;

        [Header("Tier")]
        public CropTier Tier;
    }

    public enum CropTier { Starter, Staple, Specialty, Legendary }
}
```

- [ ] **Step 2: Write failing crop growth tests**

```csharp
// Assets/Tests/EditMode/CropGrowthTests.cs
using NUnit.Framework;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Core;

public class CropGrowthTests
{
    [SetUp]
    public void SetUp() => EventBus.Clear();

    [Test]
    public void Crop_Grows_Over_Time_In_Good_Soil()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(80f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(2.5f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.AreEqual(1, crop.CurrentStage); // Should be past stage 0
    }

    [Test]
    public void Crop_Does_Not_Grow_Without_Water()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        // No water added
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(5f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.AreEqual(0, crop.CurrentStage);
    }

    [Test]
    public void Crop_Reaches_Harvestable_State()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(100f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        // Tick enough time for full growth
        crop.Tick(6f, soil, sunExposure: 1f, windDamage: 0f);
        Assert.IsTrue(crop.IsHarvestable);
    }

    [Test]
    public void Wind_Damage_Reduces_Health()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        soil.AddWater(50f);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        crop.Tick(1f, soil, sunExposure: 1f, windDamage: 0.5f);
        Assert.Less(crop.Health, 1f);
    }

    [Test]
    public void Dead_Crop_Is_Not_Harvestable()
    {
        var soil = new SoilState(TerrainType.FertileValley);
        var crop = new CropState("wheat", growthTimeMinutes: 5f, stages: 4, waterPerMinute: 2f);
        // Apply massive wind damage
        for (int i = 0; i < 20; i++)
            crop.Tick(0.5f, soil, sunExposure: 0f, windDamage: 1f);
        Assert.IsTrue(crop.IsDead);
        Assert.IsFalse(crop.IsHarvestable);
    }
}
```

- [ ] **Step 3: Implement CropState (pure logic)**

```csharp
// Assets/Scripts/Farming/CropInstance.cs
using SkyHarvest.Island;
using SkyHarvest.Core;

namespace SkyHarvest.Farming
{
    public class CropState
    {
        public string CropId { get; }
        public int CurrentStage { get; private set; }
        public float GrowthProgress { get; private set; } // 0 to 1
        public float Health { get; private set; } = 1f;    // 0 to 1
        public bool IsHarvestable => CurrentStage >= _totalStages - 1 && !IsDead;
        public bool IsDead => Health <= 0f;

        private readonly float _growthTimeMinutes;
        private readonly int _totalStages;
        private readonly float _waterPerMinute;
        private float _accumulatedGrowth;

        public CropState(string cropId, float growthTimeMinutes, int stages, float waterPerMinute)
        {
            CropId = cropId;
            _growthTimeMinutes = growthTimeMinutes;
            _totalStages = stages;
            _waterPerMinute = waterPerMinute;
        }

        public void Tick(float deltaMinutes, SoilState soil, float sunExposure, float windDamage)
        {
            if (IsDead || IsHarvestable) return;

            // Consume water
            float waterNeeded = _waterPerMinute * deltaMinutes;
            float waterAvailable = soil.WaterLevel;
            float waterFactor = waterAvailable > waterNeeded ? 1f : waterAvailable / (waterNeeded + 0.01f);
            soil.ConsumeWater(System.Math.Min(waterNeeded, waterAvailable));

            // Growth based on conditions
            float growthRate = waterFactor * sunExposure * soil.GrowthMultiplier();
            _accumulatedGrowth += deltaMinutes * growthRate;
            GrowthProgress = _accumulatedGrowth / _growthTimeMinutes;
            CurrentStage = (int)(GrowthProgress * _totalStages);
            if (CurrentStage >= _totalStages) CurrentStage = _totalStages - 1;

            // Wind damage
            if (windDamage > 0f)
            {
                Health -= windDamage * deltaMinutes * 0.1f;
                if (Health < 0f) Health = 0f;
            }
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Farming/ Assets/Scripts/Data/CropData.cs Assets/Tests/EditMode/CropGrowthTests.cs
git commit -m "feat: crop growth system with water, sun, wind, and soil dependencies"
```

---

### Task 14: Crop Growth System Manager & Farming Actions

**Files:**
- Create: `Assets/Scripts/Farming/CropGrowthSystem.cs` (MonoBehaviour managing all crops)
- Create: `Assets/Scripts/Farming/FarmingActions.cs`
- Create: `Assets/Scripts/Farming/CropPlot.cs` (MonoBehaviour on world crop objects)

- [ ] **Step 1: Create CropPlot component**

```csharp
// Assets/Scripts/Farming/CropPlot.cs
using UnityEngine;
using SkyHarvest.Island;
using SkyHarvest.Player;

namespace SkyHarvest.Farming
{
    public class CropPlot : MonoBehaviour, IInteractable
    {
        public SoilState Soil { get; set; }
        public CropState Crop { get; set; }
        public bool HasCrop => Crop != null && !Crop.IsDead;
        public bool IsEmpty => Crop == null;

        public string InteractionPrompt
        {
            get
            {
                if (IsEmpty) return "Plant";
                if (Crop.IsHarvestable) return "Harvest";
                if (Crop.IsDead) return "Clear";
                return "Inspect";
            }
        }

        public void Interact(PlayerController player)
        {
            var tool = player.GetComponent<ToolSystem>();
            var inv = player.GetComponent<PlayerInventory>();

            if (IsEmpty && tool?.EquippedTool == ToolType.Hoe)
            {
                // Till soil - handled by FarmingActions
                FarmingActions.TrySow(this, player);
            }
            else if (HasCrop && Crop.IsHarvestable && tool?.EquippedTool == ToolType.Sickle)
            {
                FarmingActions.Harvest(this, player);
            }
            else if (HasCrop && tool?.EquippedTool == ToolType.WateringCan)
            {
                FarmingActions.Water(this);
            }
        }
    }
}
```

- [ ] **Step 2: Create FarmingActions**

```csharp
// Assets/Scripts/Farming/FarmingActions.cs
using SkyHarvest.Player;

namespace SkyHarvest.Farming
{
    public static class FarmingActions
    {
        public static void Water(CropPlot plot)
        {
            plot.Soil.AddWater(25f);
        }

        public static void TrySow(CropPlot plot, PlayerController player)
        {
            // Check player has a seed in selected hotbar slot
            // For MVP: check inventory for any seed item
            // Plant the crop
            // This will be data-driven via CropData ScriptableObjects
        }

        public static void Harvest(CropPlot plot, PlayerController player)
        {
            if (plot.Crop == null || !plot.Crop.IsHarvestable) return;

            var inv = player.GetComponent<PlayerInventory>();
            // Add harvest yield to inventory
            // Record harvest on soil for rotation tracking
            plot.Soil.RecordHarvest(plot.Crop.CropId);
            plot.Crop = null;
        }
    }
}
```

- [ ] **Step 3: Create CropGrowthSystem**

```csharp
// Assets/Scripts/Farming/CropGrowthSystem.cs
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Weather;

namespace SkyHarvest.Farming
{
    public class CropGrowthSystem : MonoBehaviour
    {
        public static CropGrowthSystem Instance { get; private set; }

        private readonly List<CropPlot> _activePlots = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() => EventBus.Subscribe<GameTickEvent>(OnTick);
        private void OnDisable() => EventBus.Unsubscribe<GameTickEvent>(OnTick);

        public void Register(CropPlot plot) => _activePlots.Add(plot);
        public void Unregister(CropPlot plot) => _activePlots.Remove(plot);

        private void OnTick(GameTickEvent e)
        {
            var weather = WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;

            foreach (var plot in _activePlots)
            {
                if (!plot.HasCrop) continue;

                float sun = GetSunExposure(weather);
                float wind = GetWindDamage(weather, plot);
                float rainWater = GetRainWater(weather, e.DeltaMinutes);

                if (rainWater > 0f)
                    plot.Soil.AddWater(rainWater);

                plot.Crop.Tick(e.DeltaMinutes, plot.Soil, sun, wind);
            }
        }

        private float GetSunExposure(WeatherType weather) => weather switch
        {
            WeatherType.ClearSkies => 1f,
            WeatherType.LightRain => 0.6f,
            WeatherType.HeavyStorm => 0.2f,
            WeatherType.GaleWinds => 0.7f,
            WeatherType.FogBank => 0.3f,
            WeatherType.SkyDrought => 1f,
            _ => 0.5f
        };

        private float GetWindDamage(WeatherType weather, CropPlot plot) => weather switch
        {
            WeatherType.GaleWinds => 0.8f,
            WeatherType.HeavyStorm => 0.5f,
            _ => 0f
        };

        private float GetRainWater(WeatherType weather, float deltaMinutes) => weather switch
        {
            WeatherType.LightRain => 5f * deltaMinutes,
            WeatherType.HeavyStorm => 15f * deltaMinutes,
            _ => 0f
        };
    }
}
```

- [ ] **Step 4: Manual verification**

Create a test scene with island, player, and a few CropPlot objects. Plant crops (via debug input), observe growth through stage changes, verify weather affects growth rate and wind damages crops.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Farming/
git commit -m "feat: crop growth manager with weather integration and farming actions"
```

---

## Chunk 4: Building, Workshops & Debris

### Task 15: Structure Data & Build Mode

**Files:**
- Create: `Assets/Scripts/Data/StructureData.cs`
- Create: `Assets/Scripts/Building/BuildModeController.cs`
- Create: `Assets/Scripts/Building/Structure.cs`
- Create: `Assets/Scripts/Building/StructureRegistry.cs`

- [ ] **Step 1: Create StructureData ScriptableObject**

```csharp
// Assets/Scripts/Data/StructureData.cs
using UnityEngine;
using System.Collections.Generic;

namespace SkyHarvest.Data
{
    public enum StructureCategory { Shelter, Farm, Workshop, Storage, Infrastructure, Expansion }

    [CreateAssetMenu(fileName = "NewStructure", menuName = "SkyHarvest/Structure Data")]
    public class StructureData : ScriptableObject
    {
        public string StructureId;
        public string DisplayName;
        public string Description;
        public StructureCategory Category;
        public GameObject Prefab;
        public GameObject GhostPrefab; // Semi-transparent preview
        public Vector2Int FootprintSize = Vector2Int.one;
        public CraftingCost[] BuildCosts;
        public bool RequiresFoundation = true;
    }

    [System.Serializable]
    public class CraftingCost
    {
        public string ItemId;
        public int Amount;
    }
}
```

- [ ] **Step 2: Implement BuildModeController**

```csharp
// Assets/Scripts/Building/BuildModeController.cs
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Island;

namespace SkyHarvest.Building
{
    public class BuildModeController : MonoBehaviour
    {
        public static BuildModeController Instance { get; private set; }

        [SerializeField] private LayerMask _terrainMask;
        public bool IsActive { get; private set; }

        private StructureData _selectedStructure;
        private GameObject _ghostInstance;
        private IslandData _island;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetIsland(IslandData island) => _island = island;

        public void EnterBuildMode(StructureData structure)
        {
            IsActive = true;
            _selectedStructure = structure;
            InputManager.Instance.SwitchToBuildMode();
            SpawnGhost();
        }

        public void ExitBuildMode()
        {
            IsActive = false;
            _selectedStructure = null;
            if (_ghostInstance != null) Destroy(_ghostInstance);
            InputManager.Instance.SwitchToGameplay();
        }

        private void Update()
        {
            if (!IsActive) return;
            UpdateGhostPosition();
            // Place on click/interact
        }

        private void SpawnGhost()
        {
            if (_selectedStructure.GhostPrefab != null)
                _ghostInstance = Instantiate(_selectedStructure.GhostPrefab);
        }

        private void UpdateGhostPosition()
        {
            // Raycast to terrain, snap to grid, validate placement
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, _terrainMask))
            {
                Vector2Int gridPos = WorldToGrid(hit.point);
                bool valid = CanPlaceAt(gridPos);
                _ghostInstance.transform.position = GridToWorld(gridPos);
                // Tint green/red based on validity
            }
        }

        public bool TryPlace(Vector2Int gridPos)
        {
            if (!CanPlaceAt(gridPos)) return false;

            var go = Instantiate(_selectedStructure.Prefab, GridToWorld(gridPos), Quaternion.identity);
            var structure = go.GetComponent<Structure>();
            structure.Initialize(_selectedStructure, gridPos);
            StructureRegistry.Instance.Register(structure);
            return true;
        }

        private bool CanPlaceAt(Vector2Int pos)
        {
            var cell = _island?.GetCell(pos);
            if (cell == null) return false;
            if (StructureRegistry.Instance.HasStructureAt(pos)) return false;
            return true;
        }

        private Vector2Int WorldToGrid(Vector3 world)
        {
            // Inverse isometric transform
            float gridX = (world.x + world.z);
            float gridY = (world.z - world.x);
            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        private Vector3 GridToWorld(Vector2Int grid)
        {
            float x = (grid.x - grid.y) * 0.5f;
            float z = (grid.x + grid.y) * 0.5f;
            return new Vector3(x, 0, z);
        }
    }
}
```

- [ ] **Step 3: Implement Structure and StructureRegistry**

```csharp
// Assets/Scripts/Building/Structure.cs
using UnityEngine;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Building
{
    public class Structure : MonoBehaviour, IInteractable
    {
        public StructureData Data { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        public string InteractionPrompt => Data?.DisplayName ?? "Structure";

        public void Initialize(StructureData data, Vector2Int gridPos)
        {
            Data = data;
            GridPosition = gridPos;
        }

        public virtual void Interact(PlayerController player)
        {
            // Base: inspect. Subclasses (workshops) override.
        }
    }
}
```

```csharp
// Assets/Scripts/Building/StructureRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace SkyHarvest.Building
{
    public class StructureRegistry : MonoBehaviour
    {
        public static StructureRegistry Instance { get; private set; }

        private readonly Dictionary<Vector2Int, Structure> _structures = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(Structure s) => _structures[s.GridPosition] = s;
        public void Unregister(Structure s) => _structures.Remove(s.GridPosition);
        public bool HasStructureAt(Vector2Int pos) => _structures.ContainsKey(pos);
        public Structure GetStructureAt(Vector2Int pos) =>
            _structures.TryGetValue(pos, out var s) ? s : null;
        public IEnumerable<Structure> AllStructures => _structures.Values;
    }
}
```

- [ ] **Step 4: Create ScriptableObject assets for MVP structures**

Create assets in `Assets/Data/Structures/`:
- `Shelter.asset` — basic shelter, costs 5 wood + 3 scrap
- `RainCatcher.asset` — collects rain water, costs 3 scrap + 2 rope
- `Windbreak.asset` — wall segment, costs 4 wood
- `Path.asset` — cosmetic path tile, costs 2 stone
- `Scaffolding.asset` — island expansion, costs 8 wood + 5 scrap + 3 nails

- [ ] **Step 5: Manual verification**

Press B to enter build mode. Select a structure. Move mouse to see ghost snap to valid positions. Click to place. Press B or Escape to exit build mode.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Building/ Assets/Scripts/Data/StructureData.cs Assets/Data/Structures/
git commit -m "feat: build mode with ghost preview, grid snapping, and structure registry"
```

---

### Task 16: Workshop System

**Files:**
- Create: `Assets/Scripts/Workshop/WorkshopBase.cs`
- Create: `Assets/Scripts/Workshop/DryingRack.cs`
- Create: `Assets/Scripts/Workshop/StoneMill.cs`
- Create: `Assets/Scripts/Workshop/Forge.cs`
- Create: `Assets/Scripts/Data/RecipeData.cs`
- Create: `Assets/Tests/EditMode/RecipeTests.cs`

- [ ] **Step 1: Create RecipeData ScriptableObject**

```csharp
// Assets/Scripts/Data/RecipeData.cs
using UnityEngine;

namespace SkyHarvest.Data
{
    public enum WorkshopType { DryingRack, StoneMill, Forge }

    [CreateAssetMenu(fileName = "NewRecipe", menuName = "SkyHarvest/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        public string RecipeId;
        public string DisplayName;
        public WorkshopType RequiredWorkshop;
        public CraftingCost[] Inputs;
        public string OutputItemId;
        public int OutputAmount = 1;
        public float ProcessingTimeSeconds = 10f;
        public string FuelItemId;        // null if no fuel needed
        public int FuelAmount;
        public bool WeatherSensitive;     // Drying rack: rain ruins batch
    }
}
```

- [ ] **Step 2: Write recipe validation tests**

```csharp
// Assets/Tests/EditMode/RecipeTests.cs
using NUnit.Framework;
using SkyHarvest.Workshop;
using SkyHarvest.Player;

public class RecipeTests
{
    [Test]
    public void CanCraft_Returns_True_When_Inventory_Has_Materials()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 3);
        var inputs = new[] { ("wheat", 2) };
        Assert.IsTrue(WorkshopLogic.CanCraft(inv, inputs));
    }

    [Test]
    public void CanCraft_Returns_False_When_Missing_Materials()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 1);
        var inputs = new[] { ("wheat", 2) };
        Assert.IsFalse(WorkshopLogic.CanCraft(inv, inputs));
    }

    [Test]
    public void ConsumeInputs_Removes_Items()
    {
        var inv = new Inventory(20);
        inv.TryAdd("wheat", 5);
        WorkshopLogic.ConsumeInputs(inv, new[] { ("wheat", 3) });
        Assert.AreEqual(2, inv.GetCount("wheat"));
    }
}
```

- [ ] **Step 3: Implement WorkshopLogic (pure logic)**

```csharp
// Assets/Scripts/Workshop/WorkshopLogic.cs
using SkyHarvest.Player;

namespace SkyHarvest.Workshop
{
    public static class WorkshopLogic
    {
        public static bool CanCraft(Inventory inv, (string itemId, int amount)[] inputs)
        {
            foreach (var (itemId, amount) in inputs)
                if (!inv.Has(itemId, amount)) return false;
            return true;
        }

        public static void ConsumeInputs(Inventory inv, (string itemId, int amount)[] inputs)
        {
            foreach (var (itemId, amount) in inputs)
                inv.TryRemove(itemId, amount);
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Implement WorkshopBase MonoBehaviour**

```csharp
// Assets/Scripts/Workshop/WorkshopBase.cs
using UnityEngine;
using SkyHarvest.Building;
using SkyHarvest.Data;
using SkyHarvest.Player;
using SkyHarvest.Core;

namespace SkyHarvest.Workshop
{
    public class WorkshopBase : Structure
    {
        [SerializeField] protected RecipeData[] _availableRecipes;

        public RecipeData CurrentRecipe { get; protected set; }
        public float ProcessingProgress { get; protected set; } // 0 to 1
        public bool IsProcessing => CurrentRecipe != null && ProcessingProgress < 1f;
        public bool IsComplete => CurrentRecipe != null && ProcessingProgress >= 1f;

        private float _elapsedTime;

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<GameTickEvent>(OnTick);
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<GameTickEvent>(OnTick);
        }

        private void OnTick(GameTickEvent e)
        {
            if (!IsProcessing) return;
            if (!CanContinueProcessing()) { CancelProcessing(); return; }

            _elapsedTime += e.DeltaMinutes * Constants.SecondsPerGameMinute;
            ProcessingProgress = _elapsedTime / CurrentRecipe.ProcessingTimeSeconds;
        }

        public virtual bool StartRecipe(RecipeData recipe, Inventory playerInv)
        {
            if (IsProcessing) return false;

            var inputs = new (string, int)[recipe.Inputs.Length];
            for (int i = 0; i < recipe.Inputs.Length; i++)
                inputs[i] = (recipe.Inputs[i].ItemId, recipe.Inputs[i].Amount);

            if (!WorkshopLogic.CanCraft(playerInv, inputs)) return false;

            WorkshopLogic.ConsumeInputs(playerInv, inputs);
            CurrentRecipe = recipe;
            ProcessingProgress = 0f;
            _elapsedTime = 0f;
            return true;
        }

        public virtual void CollectOutput(Inventory playerInv)
        {
            if (!IsComplete) return;
            playerInv.TryAdd(CurrentRecipe.OutputItemId, CurrentRecipe.OutputAmount);
            CurrentRecipe = null;
            ProcessingProgress = 0f;
        }

        protected virtual bool CanContinueProcessing() => true;

        protected virtual void CancelProcessing()
        {
            CurrentRecipe = null;
            ProcessingProgress = 0f;
            _elapsedTime = 0f;
        }

        public override void Interact(PlayerController player)
        {
            // Open workshop UI
            // UI handles recipe selection and output collection
        }
    }
}
```

- [ ] **Step 6: Implement DryingRack (weather-sensitive)**

```csharp
// Assets/Scripts/Workshop/DryingRack.cs
using SkyHarvest.Weather;

namespace SkyHarvest.Workshop
{
    public class DryingRack : WorkshopBase
    {
        protected override bool CanContinueProcessing()
        {
            var weather = WeatherManager.Instance?.CurrentWeather;
            // Rain ruins drying batches
            return weather != Core.WeatherType.LightRain
                && weather != Core.WeatherType.HeavyStorm;
        }
    }
}
```

- [ ] **Step 7: Implement StoneMill and Forge**

```csharp
// Assets/Scripts/Workshop/StoneMill.cs
namespace SkyHarvest.Workshop
{
    public class StoneMill : WorkshopBase
    {
        // No special behavior for MVP — indoor, reliable
    }
}
```

```csharp
// Assets/Scripts/Workshop/Forge.cs
using SkyHarvest.Player;
using SkyHarvest.Data;

namespace SkyHarvest.Workshop
{
    public class Forge : WorkshopBase
    {
        public override bool StartRecipe(RecipeData recipe, Inventory playerInv)
        {
            // Check fuel requirement
            if (!string.IsNullOrEmpty(recipe.FuelItemId))
            {
                if (!playerInv.Has(recipe.FuelItemId, recipe.FuelAmount))
                    return false;
                playerInv.TryRemove(recipe.FuelItemId, recipe.FuelAmount);
            }

            return base.StartRecipe(recipe, playerInv);
        }
    }
}
```

- [ ] **Step 8: Create recipe ScriptableObject assets**

Create in `Assets/Data/Recipes/`:
- `WheatToFlour.asset` — StoneMill, input: wheat x3, output: flour x2, 15s
- `HerbsDrying.asset` — DryingRack, input: herbs x2, output: dried_herbs x2, 20s, weather sensitive
- `ScrapToNails.asset` — Forge, input: iron_ore x2, fuel: coal x1, output: nails x4, 25s

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/Workshop/ Assets/Scripts/Data/RecipeData.cs Assets/Tests/EditMode/RecipeTests.cs Assets/Data/Recipes/
git commit -m "feat: workshop system with drying rack, stone mill, forge, and recipe processing"
```

---

### Task 17: Debris System

**Files:**
- Create: `Assets/Scripts/Data/LootTableData.cs`
- Create: `Assets/Scripts/Debris/DebrisSpawner.cs`
- Create: `Assets/Scripts/Debris/DebrisObject.cs`
- Create: `Assets/Tests/EditMode/LootTableTests.cs`

- [ ] **Step 1: Create LootTableData ScriptableObject**

```csharp
// Assets/Scripts/Data/LootTableData.cs
using UnityEngine;

namespace SkyHarvest.Data
{
    [CreateAssetMenu(fileName = "NewLootTable", menuName = "SkyHarvest/Loot Table")]
    public class LootTableData : ScriptableObject
    {
        public LootEntry[] Entries;

        public (string itemId, int amount) Roll(System.Random rng)
        {
            float totalWeight = 0f;
            foreach (var e in Entries) totalWeight += e.Weight;

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var e in Entries)
            {
                cumulative += e.Weight;
                if (roll <= cumulative)
                {
                    int amount = rng.Next(e.MinAmount, e.MaxAmount + 1);
                    return (e.ItemId, amount);
                }
            }

            return (Entries[0].ItemId, Entries[0].MinAmount);
        }
    }

    [System.Serializable]
    public class LootEntry
    {
        public string ItemId;
        public float Weight = 1f;
        public int MinAmount = 1;
        public int MaxAmount = 3;
    }
}
```

- [ ] **Step 2: Write loot table tests**

```csharp
// Assets/Tests/EditMode/LootTableTests.cs
using NUnit.Framework;
using SkyHarvest.Data;

public class LootTableTests
{
    [Test]
    public void Roll_Returns_Valid_Item()
    {
        var table = UnityEngine.ScriptableObject.CreateInstance<LootTableData>();
        table.Entries = new[]
        {
            new LootEntry { ItemId = "scrap", Weight = 1f, MinAmount = 1, MaxAmount = 3 }
        };

        var rng = new System.Random(42);
        var (itemId, amount) = table.Roll(rng);
        Assert.AreEqual("scrap", itemId);
        Assert.GreaterOrEqual(amount, 1);
        Assert.LessOrEqual(amount, 3);
    }

    [Test]
    public void Roll_Respects_Weights()
    {
        var table = UnityEngine.ScriptableObject.CreateInstance<LootTableData>();
        table.Entries = new[]
        {
            new LootEntry { ItemId = "common", Weight = 90f, MinAmount = 1, MaxAmount = 1 },
            new LootEntry { ItemId = "rare", Weight = 10f, MinAmount = 1, MaxAmount = 1 }
        };

        var rng = new System.Random(42);
        int commonCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var (itemId, _) = table.Roll(rng);
            if (itemId == "common") commonCount++;
        }

        // Should be roughly 90% common
        Assert.Greater(commonCount, 800);
        Assert.Less(commonCount, 950);
    }
}
```

- [ ] **Step 3: Run tests — expect PASS**

- [ ] **Step 4: Implement DebrisSpawner and DebrisObject**

```csharp
// Assets/Scripts/Debris/DebrisSpawner.cs
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Weather;

namespace SkyHarvest.Debris
{
    public class DebrisSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _debrisPrefab;
        [SerializeField] private LootTableData _defaultLootTable;
        [SerializeField] private float _islandRadius = 15f;

        private float _timer;
        private System.Random _rng = new();

        private void Update()
        {
            float interval = GetSpawnInterval();
            _timer += Time.deltaTime;

            if (_timer >= interval)
            {
                _timer = 0f;
                SpawnDebris();
            }
        }

        private float GetSpawnInterval()
        {
            float baseInterval = Constants.BaseDebrisIntervalSeconds;
            var weather = WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;

            return weather switch
            {
                WeatherType.GaleWinds => baseInterval * Constants.GaleWindDebrisMultiplier,
                WeatherType.HeavyStorm => baseInterval * 0.6f,
                _ => baseInterval
            };
        }

        private void SpawnDebris()
        {
            // Spawn at random edge position
            float angle = (float)_rng.NextDouble() * Mathf.PI * 2f;
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * (_islandRadius + 5f),
                1f,
                Mathf.Sin(angle) * (_islandRadius + 5f)
            );

            var go = Instantiate(_debrisPrefab, spawnPos, Quaternion.identity);
            var debris = go.GetComponent<DebrisObject>();
            debris.Initialize(_defaultLootTable, _rng);
        }
    }
}
```

```csharp
// Assets/Scripts/Debris/DebrisObject.cs
using UnityEngine;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Debris
{
    public class DebrisObject : MonoBehaviour, IInteractable
    {
        private LootTableData _lootTable;
        private System.Random _rng;
        private float _lifetime = 30f; // Seconds before drifting away
        private Vector3 _driftDirection;

        public string InteractionPrompt => "Scavenge";

        public void Initialize(LootTableData lootTable, System.Random rng)
        {
            _lootTable = lootTable;
            _rng = rng;
            // Drift toward center of island
            _driftDirection = (-transform.position).normalized * 0.5f;
            _driftDirection.y = 0f;
        }

        private void Update()
        {
            transform.position += _driftDirection * Time.deltaTime;
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f) Destroy(gameObject);
        }

        public void Interact(PlayerController player)
        {
            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null) return;

            // Roll 1-3 items from loot table
            int rolls = _rng.Next(1, 4);
            for (int i = 0; i < rolls; i++)
            {
                var (itemId, amount) = _lootTable.Roll(_rng);
                inv.TryAdd(itemId, amount);
            }

            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 5: Create loot table assets**

Create `Assets/Data/LootTables/DefaultDebris.asset`:
- scrap: weight 30, 1-3
- wood: weight 25, 1-4
- stone: weight 20, 1-3
- iron_ore: weight 10, 1-2
- coal: weight 8, 1-2
- rope: weight 5, 1-2
- wheat_seed: weight 1.5, 1-1
- herb_seed: weight 0.5, 1-1

- [ ] **Step 6: Manual verification**

Play mode. Debris objects spawn at island edges and drift. Walk up to one and press E. Verify items appear in inventory (debug log). Verify debris despawns if not collected. Verify faster spawning during gale winds.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Debris/ Assets/Scripts/Data/LootTableData.cs Assets/Tests/EditMode/LootTableTests.cs Assets/Data/LootTables/
git commit -m "feat: debris spawning with weather-affected frequency and weighted loot tables"
```

---

## Chunk 5: Storage, UI, Save/Load & Integration

### Task 18: Storage System

**Files:**
- Create: `Assets/Scripts/Storage/StorageContainer.cs`
- Create: `Assets/Scripts/Storage/StorageProximity.cs`

- [ ] **Step 1: Implement StorageContainer**

```csharp
// Assets/Scripts/Storage/StorageContainer.cs
using SkyHarvest.Player;
using SkyHarvest.Building;
using UnityEngine;

namespace SkyHarvest.Storage
{
    public enum StorageType { Crate, Barrel }

    public class StorageContainer : Structure
    {
        [SerializeField] private StorageType _storageType;
        [SerializeField] private int _slotCount = 10;

        public Inventory Storage { get; private set; }
        public StorageType Type => _storageType;

        private void Awake()
        {
            Storage = new Inventory(_slotCount);
        }

        public override void Interact(PlayerController player)
        {
            // Open storage UI showing both player inventory and this storage
        }
    }
}
```

- [ ] **Step 2: Implement StorageProximity for workshop auto-pull**

```csharp
// Assets/Scripts/Storage/StorageProximity.cs
using UnityEngine;
using SkyHarvest.Player;

namespace SkyHarvest.Storage
{
    public static class StorageProximity
    {
        public static StorageContainer FindNearby(Vector3 position, float radius = 3f)
        {
            var colliders = Physics.OverlapSphere(position, radius);
            StorageContainer closest = null;
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var storage = col.GetComponent<StorageContainer>();
                if (storage == null) continue;
                float dist = Vector3.Distance(position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = storage;
                }
            }

            return closest;
        }

        public static bool TryPullItem(Vector3 position, string itemId, int amount, float radius = 3f)
        {
            var storage = FindNearby(position, radius);
            return storage != null && storage.Storage.TryRemove(itemId, amount);
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Storage/
git commit -m "feat: storage containers with proximity-based workshop auto-pull"
```

---

### Task 19: Core UI

**Files:**
- Create: `Assets/Scripts/UI/HUDController.cs`
- Create: `Assets/Scripts/UI/InventoryUI.cs`
- Create: `Assets/Scripts/UI/InspectorPanel.cs`
- Create: `Assets/Scripts/UI/WorkshopUI.cs`
- Create: `Assets/Scripts/UI/StorageUI.cs`
- Create: `Assets/Scripts/UI/BuildMenuUI.cs`

- [ ] **Step 1: Create HUD Canvas**

In Unity, create a Canvas (Screen Space - Overlay) with:
- Bottom bar: hotbar slots (6 slots showing items/tools)
- Top-left: equipped tool icon
- Top-right: time of day display (sun/moon icon + hour text)
- Bottom-right: minimap toggle button

- [ ] **Step 2: Implement HUDController**

```csharp
// Assets/Scripts/UI/HUDController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Image _toolIcon;
        [SerializeField] private Transform _hotbarContainer;

        private void OnEnable()
        {
            EventBus.Subscribe<HourChangedEvent>(OnHourChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
        }

        private void OnHourChanged(HourChangedEvent e)
        {
            _timeText.text = $"{e.Hour:D2}:00";
        }
    }
}
```

- [ ] **Step 3: Implement InventoryUI**

Grid layout panel that opens on Tab. Shows player inventory slots. Click to select item for hotbar. Close with Tab or Escape.

- [ ] **Step 4: Implement InspectorPanel**

Floating panel that appears when player looks at a crop plot, workshop, or structure. Shows contextual info:
- Crop: growth stage bar, health bar, water level, soil quality
- Workshop: current recipe, progress bar, fuel level
- Structure: name, condition

- [ ] **Step 5: Implement WorkshopUI**

Panel that opens when interacting with a workshop. Shows:
- Available recipes list
- Required materials (greyed out if insufficient)
- "Start" button
- Progress bar for active recipe
- "Collect" button when done

- [ ] **Step 6: Implement StorageUI**

Split panel: player inventory on left, storage contents on right. Click items to transfer between them.

- [ ] **Step 7: Implement BuildMenuUI**

Panel showing available structures when in build mode. Filter by category. Shows cost, description. Click to select for placement.

- [ ] **Step 8: Manual verification**

Test each UI panel opens and closes correctly. Verify HUD updates with game time. Verify inventory shows items after scavenging debris. Verify workshop UI starts recipes and shows progress.

- [ ] **Step 9: Commit**

```bash
git add Assets/Scripts/UI/ Assets/Prefabs/UI/
git commit -m "feat: core UI — HUD, inventory, inspector, workshop, storage, and build menu"
```

---

### Task 20: Save/Load System

**Files:**
- Create: `Assets/Scripts/SaveLoad/SaveManager.cs`
- Create: `Assets/Scripts/SaveLoad/WorldSaveData.cs`
- Create: `Assets/Scripts/SaveLoad/IslandSaveData.cs`
- Create: `Assets/Scripts/SaveLoad/PlayerSaveData.cs`
- Create: `Assets/Tests/EditMode/SaveLoadTests.cs`

- [ ] **Step 1: Define serializable save data classes**

```csharp
// Assets/Scripts/SaveLoad/WorldSaveData.cs
using System;
using System.Collections.Generic;

namespace SkyHarvest.SaveLoad
{
    [Serializable]
    public class WorldSaveData
    {
        public int Version = 1;
        public float GameTimeMinutes;
        public string WeatherState;
        public float WeatherTimeRemaining;
        public IslandSaveData Island;
        public PlayerSaveData Player;
    }
}
```

```csharp
// Assets/Scripts/SaveLoad/IslandSaveData.cs
using System;
using System.Collections.Generic;

namespace SkyHarvest.SaveLoad
{
    [Serializable]
    public class IslandSaveData
    {
        public int Seed;
        public List<CellSaveData> ModifiedCells = new();
        public List<StructureSaveData> Structures = new();
        public List<CropSaveData> Crops = new();
        public List<StorageSaveData> Storages = new();
    }

    [Serializable]
    public class CellSaveData
    {
        public int X, Y;
        public float SoilQuality, WaterLevel, Nutrients;
        public string LastCrop;
    }

    [Serializable]
    public class StructureSaveData
    {
        public string StructureId;
        public int GridX, GridY;
    }

    [Serializable]
    public class CropSaveData
    {
        public string CropId;
        public int GridX, GridY;
        public float GrowthProgress, Health;
    }

    [Serializable]
    public class StorageSaveData
    {
        public int GridX, GridY;
        public List<SlotSaveData> Slots = new();
    }

    [Serializable]
    public class SlotSaveData
    {
        public string ItemId;
        public int Count;
    }
}
```

```csharp
// Assets/Scripts/SaveLoad/PlayerSaveData.cs
using System;
using System.Collections.Generic;

namespace SkyHarvest.SaveLoad
{
    [Serializable]
    public class PlayerSaveData
    {
        public float PosX, PosY, PosZ;
        public string EquippedTool;
        public List<SlotSaveData> InventorySlots = new();
    }
}
```

- [ ] **Step 2: Write save/load tests**

```csharp
// Assets/Tests/EditMode/SaveLoadTests.cs
using NUnit.Framework;
using SkyHarvest.SaveLoad;
using UnityEngine;

public class SaveLoadTests
{
    [Test]
    public void WorldSaveData_Serializes_To_Json_And_Back()
    {
        var save = new WorldSaveData
        {
            GameTimeMinutes = 123.5f,
            WeatherState = "ClearSkies",
            WeatherTimeRemaining = 4.2f,
            Island = new IslandSaveData { Seed = 42 },
            Player = new PlayerSaveData { PosX = 1f, PosY = 2f, PosZ = 3f }
        };

        string json = JsonUtility.ToJson(save);
        var loaded = JsonUtility.FromJson<WorldSaveData>(json);

        Assert.AreEqual(123.5f, loaded.GameTimeMinutes, 0.01f);
        Assert.AreEqual(42, loaded.Island.Seed);
        Assert.AreEqual(1f, loaded.Player.PosX, 0.01f);
    }

    [Test]
    public void CropSaveData_Preserves_Growth()
    {
        var crop = new CropSaveData
        {
            CropId = "wheat",
            GrowthProgress = 0.75f,
            Health = 0.9f,
            GridX = 3,
            GridY = -2
        };

        string json = JsonUtility.ToJson(crop);
        var loaded = JsonUtility.FromJson<CropSaveData>(json);

        Assert.AreEqual("wheat", loaded.CropId);
        Assert.AreEqual(0.75f, loaded.GrowthProgress, 0.01f);
    }
}
```

- [ ] **Step 3: Run tests — expect PASS**

- [ ] **Step 4: Implement SaveManager**

```csharp
// Assets/Scripts/SaveLoad/SaveManager.cs
using UnityEngine;
using System.IO;
using SkyHarvest.Core;

namespace SkyHarvest.SaveLoad
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, "saves");
        private const string SaveFileName = "save.json";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Directory.CreateDirectory(SavePath);
        }

        public void Save()
        {
            var data = BuildSaveData();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(SavePath, SaveFileName), json);
            Debug.Log($"Game saved to {SavePath}");
        }

        public WorldSaveData Load()
        {
            string path = Path.Combine(SavePath, SaveFileName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<WorldSaveData>(json);
        }

        public bool HasSave() => File.Exists(Path.Combine(SavePath, SaveFileName));

        private WorldSaveData BuildSaveData()
        {
            // Collect state from all managers
            // This will reference GameManager, WeatherManager,
            // StructureRegistry, CropGrowthSystem, PlayerController, etc.
            var data = new WorldSaveData();
            // Implementation depends on manager references
            return data;
        }

        public void ApplySaveData(WorldSaveData data)
        {
            // Restore state to all managers
            // Re-generate island from seed, then apply modified cells
            // Place structures, crops, storage contents
            // Set player position and inventory
            // Set weather state and time
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/SaveLoad/ Assets/Tests/EditMode/SaveLoadTests.cs
git commit -m "feat: save/load system with JSON serialization for full game state"
```

---

### Task 21: Game Manager & Scene Integration

**Files:**
- Create: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Scenes/Game.unity` — wire all systems together

- [ ] **Step 1: Create GameManager**

```csharp
// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using SkyHarvest.Island;
using SkyHarvest.Weather;
using SkyHarvest.SaveLoad;

namespace SkyHarvest.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private IslandGenerator _islandGenerator;
        [SerializeField] private GameObject _playerPrefab;

        public GameTimeClock Clock { get; private set; }
        public IslandData CurrentIsland { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Clock = new GameTimeClock();
        }

        private void Start()
        {
            if (SaveManager.Instance.HasSave())
                LoadGame();
            else
                NewGame();
        }

        private void Update()
        {
            Clock.Tick(Time.deltaTime);
        }

        public void NewGame()
        {
            CurrentIsland = _islandGenerator.Generate();
            // Render island
            // Spawn player at center
            var player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
        }

        public void LoadGame()
        {
            var data = SaveManager.Instance.Load();
            if (data == null) { NewGame(); return; }
            SaveManager.Instance.ApplySaveData(data);
        }

        public void SaveGame()
        {
            SaveManager.Instance.Save();
        }
    }
}
```

- [ ] **Step 2: Wire up Game scene**

In `Game.unity`, create GameObjects with:
- GameManager (with IslandGenerator, player prefab reference)
- InputManager
- WeatherManager
- CropGrowthSystem
- StructureRegistry
- BuildModeController
- DebrisSpawner
- SaveManager
- HUD Canvas

- [ ] **Step 3: Create ScriptableObject data assets**

Create in `Assets/Data/Items/` the MVP item set:
- Seeds: wheat_seed, herb_seed, cloud_root_seed, sky_moss_seed
- Crops: wheat, herbs, cloud_root, sky_moss
- Materials: scrap, wood, stone, iron_ore, coal, rope, nails
- Processed: flour, dried_herbs

Create in `Assets/Data/Crops/` the MVP crops:
- SkyMoss: starter, 2 min growth, low water, wind resistant
- CloudRoot: starter, 3 min growth, medium water
- StormWheat: staple, 10 min growth, needs decent soil
- HerbPlant: staple, 8 min growth, needs shade

- [ ] **Step 4: End-to-end playtest**

Full gameplay loop verification:
1. Game starts, island generates with varied terrain
2. Player moves around island with WASD
3. Debris spawns and drifts, player scavenges for materials
4. Player tills soil, plants seeds, waters crops
5. Weather changes affect crops (rain waters, storms damage)
6. Crops grow and become harvestable
7. Player harvests and carries to workshops
8. Workshops process goods (drying rack fails in rain)
9. Player builds structures (shelter, rain catcher, windbreak)
10. Save and reload preserves all state

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/GameManager.cs Assets/Scenes/ Assets/Data/
git commit -m "feat: game manager with scene integration, data assets, and full gameplay loop"
```

---

### Task 22: Main Menu & Polish

**Files:**
- Create: `Assets/Scenes/MainMenu.unity`
- Create: `Assets/Scripts/UI/MainMenuUI.cs`

- [ ] **Step 1: Create main menu scene**

Simple menu with:
- "Sky Harvest" title
- "New Game" button (generates new island)
- "Continue" button (loads save, greyed out if no save)
- Seed input field (optional, for sharing islands)
- Background: slowly rotating sky with clouds

- [ ] **Step 2: Implement scene transitions**

```csharp
// Assets/Scripts/UI/MainMenuUI.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace SkyHarvest.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _seedInput;
        [SerializeField] private GameObject _continueButton;

        private void Start()
        {
            _continueButton.SetActive(PlayerPrefs.HasKey("HasSave"));
        }

        public void OnNewGame()
        {
            string seedText = _seedInput.text;
            if (int.TryParse(seedText, out int seed))
                PlayerPrefs.SetInt("IslandSeed", seed);
            else
                PlayerPrefs.DeleteKey("IslandSeed");

            PlayerPrefs.SetInt("NewGame", 1);
            SceneManager.LoadScene("Game");
        }

        public void OnContinue()
        {
            PlayerPrefs.SetInt("NewGame", 0);
            SceneManager.LoadScene("Game");
        }
    }
}
```

- [ ] **Step 3: Add pause menu**

In-game Escape key opens pause overlay with:
- "Resume"
- "Save" (calls GameManager.SaveGame())
- "Save & Quit" (saves, returns to main menu)

- [ ] **Step 4: Final commit**

```bash
git add Assets/Scenes/MainMenu.unity Assets/Scripts/UI/MainMenuUI.cs
git commit -m "feat: main menu with new game, continue, and seed input"
```

---

## Task Summary

| Task | System | Chunk |
|------|--------|-------|
| 1 | Unity project setup | 1 |
| 2 | EventBus | 1 |
| 3 | Game time clock | 1 |
| 4 | Item data & inventory | 1 |
| 5 | Input manager | 1 |
| 6 | Island terrain data & soil | 2 |
| 7 | Procedural island generator | 2 |
| 8 | Player controller (isometric movement) | 2 |
| 9 | Interaction system | 2 |
| 10 | Tool system | 2 |
| 11 | Weather state machine | 3 |
| 12 | Weather visual effects | 3 |
| 13 | Crop data & growth logic | 3 |
| 14 | Crop growth manager & farming actions | 3 |
| 15 | Structure data & build mode | 4 |
| 16 | Workshop system (3 workshops) | 4 |
| 17 | Debris system | 4 |
| 18 | Storage system | 5 |
| 19 | Core UI (HUD, inventory, inspector, etc.) | 5 |
| 20 | Save/load system | 5 |
| 21 | Game manager & scene integration | 5 |
| 22 | Main menu & polish | 5 |
