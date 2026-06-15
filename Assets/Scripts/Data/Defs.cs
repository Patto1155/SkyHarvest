// Assets/Scripts/Data/Defs.cs
// Owned by: world/island agent
// Plain-C# definition classes — NO UnityEngine dependency.
// These are the data records that GameDatabase (owned by the Data/UI agent)
// populates at runtime.  Agent-assigned fields match CONVENTIONS §Game data IDs.
using System;

namespace SkyHarvest.Data
{
    // =========================================================================
    // Item
    // =========================================================================
    public enum ItemCategory { Seed, Crop, Material, Tool, Processed, Fuel, Misc }

    public class ItemDef
    {
        public string ItemId      = string.Empty;
        public string DisplayName = string.Empty;
        public string Description = string.Empty;
        public ItemCategory Category;
        public int MaxStackSize = 99;
    }

    // =========================================================================
    // Crop
    // =========================================================================
    public enum CropTier { Starter, Staple, Specialty, Legendary }

    public class CropDef
    {
        public string CropId              = string.Empty;
        public string DisplayName         = string.Empty;
        public CropTier Tier              = CropTier.Starter;

        // --- growth ---
        public float GrowthTimeMinutes    = 5f;
        public int   GrowthStages         = 4;
        public float WaterConsumptionPerMinute = 1f;
        public float WindDamageVulnerability   = 0.5f;   // 0=resistant, 1=fragile

        // --- seed / harvest ---
        public string SeedItemId          = string.Empty;
        public string HarvestYieldItemId  = string.Empty;
        public int    HarvestYieldMin     = 1;
        public int    HarvestYieldMax     = 3;
    }

    // =========================================================================
    // Recipe
    // =========================================================================
    public struct RecipeInput
    {
        public string ItemId;
        public int    Amount;
    }

    public enum WorkshopType { DryingRack, StoneMill, Forge }

    public class RecipeDef
    {
        public string RecipeId             = string.Empty;
        public string DisplayName          = string.Empty;
        public WorkshopType RequiredWorkshop;
        public RecipeInput[] Inputs        = Array.Empty<RecipeInput>();
        public string OutputItemId         = string.Empty;
        public int    OutputAmount         = 1;
        public float  ProcessingTimeSeconds = 10f;
        public string FuelItemId           = string.Empty;   // empty = no fuel needed
        public int    FuelAmount;
        public bool   WeatherSensitive;   // DryingRack: rain ruins the batch
    }

    // =========================================================================
    // Structure
    // =========================================================================
    public enum PlacementRule { Any, CliffEdgeOnly, EdgeCellOnly }

    public struct BuildCost
    {
        public string ItemId;
        public int    Amount;
    }

    public class StructureDef
    {
        public string StructureId = string.Empty;
        public string DisplayName = string.Empty;
        public BuildCost[] BuildCosts = Array.Empty<BuildCost>();
        public int SlotCount          = 0;            // storage slots (0 = not storage)
        public PlacementRule PlacementRule = PlacementRule.Any;
        // 0 = load whole texture as one sprite; >0 = animated strip, preview uses frame 0
        public int SpriteFrameWidth   = 0;
        // FootprintSize is always (1,1) for MVP per CONVENTIONS.
    }

    // =========================================================================
    // Loot table
    // =========================================================================
    public class LootEntry
    {
        public string ItemId   = string.Empty;
        public float  Weight   = 1f;
        public int    MinAmount = 1;
        public int    MaxAmount = 3;
    }

    public class LootTableDef
    {
        public string TableId = string.Empty;
        public LootEntry[] Entries = Array.Empty<LootEntry>();

        /// <summary>Roll one item from the table using the provided RNG.</summary>
        public (string itemId, int amount) Roll(Random rng)
        {
            if (Entries == null || Entries.Length == 0)
                return (string.Empty, 0);

            float total = 0f;
            foreach (var e in Entries) total += e.Weight;

            float roll = (float)rng.NextDouble() * total;
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

            // Fallback: first entry
            return (Entries[0].ItemId, Entries[0].MinAmount);
        }
    }
}
