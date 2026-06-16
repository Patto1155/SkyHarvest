namespace SkyHarvest.Core
{
    // ---- Time ----
    public struct GameTickEvent
    {
        public float DeltaMinutes;
        public float TotalGameMinutes;
    }

    public struct HourChangedEvent { public int Hour; }
    public struct DayChangedEvent { public int Day; }

    // ---- Weather ----
    public enum WeatherType
    {
        ClearSkies,
        LightRain,
        HeavyStorm,
        GaleWinds,
        FogBank,
        SkyDrought
    }

    public struct WeatherChangedEvent
    {
        public WeatherType Previous;
        public WeatherType Current;
    }

    // ---- Farming ----
    public struct CropPlantedEvent { public string CropId; }
    public struct CropHarvestedEvent { public string CropId; public string YieldItemId; public int Amount; }
    public struct CropReadyEvent { public string CropId; }
    public struct CropDiedEvent { public string CropId; }

    // ---- Workshops ----
    public struct WorkshopStartedEvent { public string RecipeId; public string WorkshopId; }
    public struct WorkshopCompletedEvent { public string RecipeId; public string WorkshopId; }
    public struct WorkshopRuinedEvent { public string RecipeId; public string WorkshopId; }

    // ---- Debris ----
    public struct DebrisLandedEvent { public float X; public float Y; }
    public struct DebrisScavengedEvent { public float X; public float Y; }

    // ---- Building ----
    public struct StructurePlacedEvent { public string StructureId; }
    public struct ConstructionSitePlacedEvent { public string StructureId; }
    public struct ConstructionProgressEvent { public string StructureId; public bool Complete; }
    public struct StructureDemolishedEvent { public string StructureId; }
    public struct IslandExpandedEvent { public int NewCellCount; }

    /// <summary>Fired once when the tutorial staircase is mined, unlocking
    /// traversal between the lower and raised tiers. StairCell is the lower-tier
    /// cell the player mines into.</summary>
    public struct StairsCarvedEvent { public int StairX; public int StairY; }

    // ---- Player ----
    public struct ToolEquippedEvent { public int SlotIndex; }
    public struct InventoryChangedEvent { }
    public struct HotbarChangedEvent { }
    /// <summary>Fired when the active hotbar slot changes (number-key select).
    /// SlotIndex is the unified-bar index (tool slots first, then item slots).</summary>
    public struct HotbarSelectionChangedEvent { public int SlotIndex; }

    // ---- Game flow ----
    public struct GameStartedEvent { public bool LoadedFromSave; }
    public struct GameSavedEvent { }
}
