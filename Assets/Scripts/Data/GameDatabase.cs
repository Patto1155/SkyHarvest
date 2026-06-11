// Assets/Scripts/Data/GameDatabase.cs
// PLACEHOLDER — Data/UI agent owns the real implementation.
// This stub exists ONLY so the Farming/Building/etc. namespaces compile
// before the Data agent delivers the full version.
// The real GameDatabase will replace this file entirely.
//
// API contract (world-agent uses these; Data agent must preserve signatures):
//   GameDatabase.GetItem(string id)           -> ItemDef?
//   GameDatabase.GetCropForSeed(string seedId) -> CropDef?
//   GameDatabase.GetCrop(string cropId)        -> CropDef?
//   GameDatabase.AllRecipes                    -> IReadOnlyList<RecipeDef>
//   GameDatabase.AllStructures                 -> IReadOnlyList<StructureDef>
//   GameDatabase.AllItems                      -> IReadOnlyList<ItemDef>

using System.Collections.Generic;

namespace SkyHarvest.Data
{
    public static class GameDatabase
    {
        // ---- stub backing data built from CONVENTIONS §Game data IDs ----
        private static readonly Dictionary<string, ItemDef> _items = BuildItems();
        private static readonly Dictionary<string, CropDef> _crops = BuildCrops();
        private static readonly Dictionary<string, CropDef> _cropBySeed = BuildCropBySeed();

        // ---- public API ----
        public static ItemDef? GetItem(string id) =>
            _items.TryGetValue(id, out var v) ? v : null;

        public static CropDef? GetCropForSeed(string seedItemId) =>
            _cropBySeed.TryGetValue(seedItemId, out var v) ? v : null;

        public static CropDef? GetCrop(string cropId) =>
            _crops.TryGetValue(cropId, out var v) ? v : null;

        public static IReadOnlyList<RecipeDef>    AllRecipes    { get; } = BuildRecipes();
        public static IReadOnlyList<StructureDef> AllStructures { get; } = BuildStructures();
        public static IReadOnlyList<ItemDef>      AllItems      => new List<ItemDef>(_items.Values);

        // =====================================================================
        // Static data — matches CONVENTIONS §Game data IDs
        // =====================================================================

        private static Dictionary<string, ItemDef> BuildItems()
        {
            var d = new Dictionary<string, ItemDef>();
            void Add(string id, string name, ItemCategory cat, int stack = 99) =>
                d[id] = new ItemDef { ItemId = id, DisplayName = name, Category = cat, MaxStackSize = stack };

            // Seeds
            Add("sky_moss_seed",   "Sky Moss Seed",   ItemCategory.Seed);
            Add("cloud_root_seed", "Cloud Root Seed", ItemCategory.Seed);
            Add("wheat_seed",      "Wheat Seed",      ItemCategory.Seed);
            Add("herb_seed",       "Herb Seed",       ItemCategory.Seed);

            // Crops
            Add("sky_moss",   "Sky Moss",   ItemCategory.Crop);
            Add("cloud_root", "Cloud Root", ItemCategory.Crop);
            Add("wheat",      "Wheat",      ItemCategory.Crop);
            Add("herbs",      "Herbs",      ItemCategory.Crop);

            // Materials
            Add("scrap",          "Scrap",         ItemCategory.Material);
            Add("wood",           "Wood",          ItemCategory.Material);
            Add("stone",          "Stone",         ItemCategory.Material);
            Add("iron_ore",       "Iron Ore",      ItemCategory.Material);
            Add("coal",           "Coal",          ItemCategory.Fuel);
            Add("rope",           "Rope",          ItemCategory.Material);
            Add("nails",          "Nails",         ItemCategory.Material);
            Add("skynet_frame",   "Skynet Frame",  ItemCategory.Material);

            // Processed
            Add("flour",       "Flour",       ItemCategory.Processed);
            Add("dried_herbs", "Dried Herbs", ItemCategory.Processed);

            return d;
        }

        private static Dictionary<string, CropDef> BuildCrops()
        {
            var d = new Dictionary<string, CropDef>();
            void Add(CropDef c) => d[c.CropId] = c;

            Add(new CropDef
            {
                CropId = "sky_moss", DisplayName = "Sky Moss", Tier = CropTier.Starter,
                GrowthTimeMinutes = 2f, GrowthStages = 4, WaterConsumptionPerMinute = 1.0f,
                WindDamageVulnerability = 0.2f, SeedItemId = "sky_moss_seed",
                HarvestYieldItemId = "sky_moss", HarvestYieldMin = 1, HarvestYieldMax = 2
            });
            Add(new CropDef
            {
                CropId = "cloud_root", DisplayName = "Cloud Root", Tier = CropTier.Starter,
                GrowthTimeMinutes = 3f, GrowthStages = 4, WaterConsumptionPerMinute = 1.5f,
                WindDamageVulnerability = 0.3f, SeedItemId = "cloud_root_seed",
                HarvestYieldItemId = "cloud_root", HarvestYieldMin = 1, HarvestYieldMax = 3
            });
            Add(new CropDef
            {
                CropId = "storm_wheat", DisplayName = "Storm Wheat", Tier = CropTier.Staple,
                GrowthTimeMinutes = 10f, GrowthStages = 4, WaterConsumptionPerMinute = 2.0f,
                WindDamageVulnerability = 0.5f, SeedItemId = "wheat_seed",
                HarvestYieldItemId = "wheat", HarvestYieldMin = 2, HarvestYieldMax = 4
            });
            Add(new CropDef
            {
                CropId = "herb_plant", DisplayName = "Herb Plant", Tier = CropTier.Staple,
                GrowthTimeMinutes = 8f, GrowthStages = 4, WaterConsumptionPerMinute = 1.8f,
                WindDamageVulnerability = 0.4f, SeedItemId = "herb_seed",
                HarvestYieldItemId = "herbs", HarvestYieldMin = 1, HarvestYieldMax = 3
            });

            return d;
        }

        private static Dictionary<string, CropDef> BuildCropBySeed()
        {
            var d = new Dictionary<string, CropDef>();
            foreach (var crop in BuildCrops().Values)
                if (!string.IsNullOrEmpty(crop.SeedItemId))
                    d[crop.SeedItemId] = crop;
            return d;
        }

        private static List<RecipeDef> BuildRecipes() => new()
        {
            new RecipeDef
            {
                RecipeId = "wheat_to_flour", DisplayName = "Wheat → Flour",
                RequiredWorkshop = WorkshopType.StoneMill,
                Inputs = new[] { new RecipeInput { ItemId = "wheat", Amount = 3 } },
                OutputItemId = "flour", OutputAmount = 2, ProcessingTimeSeconds = 15f
            },
            new RecipeDef
            {
                RecipeId = "herbs_drying", DisplayName = "Dry Herbs",
                RequiredWorkshop = WorkshopType.DryingRack,
                Inputs = new[] { new RecipeInput { ItemId = "herbs", Amount = 2 } },
                OutputItemId = "dried_herbs", OutputAmount = 2,
                ProcessingTimeSeconds = 20f, WeatherSensitive = true
            },
            new RecipeDef
            {
                RecipeId = "ore_to_nails", DisplayName = "Iron Ore → Nails",
                RequiredWorkshop = WorkshopType.Forge,
                Inputs = new[] { new RecipeInput { ItemId = "iron_ore", Amount = 2 } },
                FuelItemId = "coal", FuelAmount = 1,
                OutputItemId = "nails", OutputAmount = 4, ProcessingTimeSeconds = 25f
            },
            new RecipeDef
            {
                RecipeId = "scrap_to_skynet_frame", DisplayName = "Scrap → Skynet Frame",
                RequiredWorkshop = WorkshopType.Forge,
                Inputs = new[] { new RecipeInput { ItemId = "scrap", Amount = 3 } },
                FuelItemId = "coal", FuelAmount = 1,
                OutputItemId = "skynet_frame", OutputAmount = 1, ProcessingTimeSeconds = 30f
            }
        };

        // =====================================================================
        // Loot tables (Industry agent needs these — added per API contract in task brief)
        // =====================================================================

        public static LootTableDef DebrisLootTable { get; } = BuildDebrisLootTable(storm: false);
        public static LootTableDef StormDebrisLootTable { get; } = BuildDebrisLootTable(storm: true);

        /// <summary>
        /// Look up a structure definition by id.
        /// Returns null if not found (matches spec API: GetStructure(string id)).
        /// </summary>
        public static StructureDef? GetStructure(string id)
        {
            foreach (var s in AllStructures)
                if (s.StructureId == id) return s;
            return null;
        }

        /// <summary>
        /// Get all recipes valid for a given workshop type.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<RecipeDef> GetRecipesFor(WorkshopType workshop)
        {
            foreach (var r in AllRecipes)
                if (r.RequiredWorkshop == workshop) yield return r;
        }

        private static LootTableDef BuildDebrisLootTable(bool storm)
        {
            // CONVENTIONS §Debris loot table
            // Base weights: scrap 30, wood 25, stone 20, iron_ore 10, coal 8, rope 5, wheat_seed 1.5, herb_seed 0.5
            // Storm variant doubles iron_ore/coal/rope weights.
            float ironMult  = storm ? 2f : 1f;
            float coalMult  = storm ? 2f : 1f;
            float ropeMult  = storm ? 2f : 1f;

            return new LootTableDef
            {
                TableId = storm ? "storm_debris" : "debris",
                Entries = new[]
                {
                    new LootEntry { ItemId = "scrap",      Weight = 30f,             MinAmount = 1, MaxAmount = 3 },
                    new LootEntry { ItemId = "wood",       Weight = 25f,             MinAmount = 1, MaxAmount = 4 },
                    new LootEntry { ItemId = "stone",      Weight = 20f,             MinAmount = 1, MaxAmount = 3 },
                    new LootEntry { ItemId = "iron_ore",   Weight = 10f * ironMult,  MinAmount = 1, MaxAmount = 2 },
                    new LootEntry { ItemId = "coal",       Weight = 8f  * coalMult,  MinAmount = 1, MaxAmount = 2 },
                    new LootEntry { ItemId = "rope",       Weight = 5f  * ropeMult,  MinAmount = 1, MaxAmount = 2 },
                    new LootEntry { ItemId = "wheat_seed", Weight = 1.5f,            MinAmount = 1, MaxAmount = 1 },
                    new LootEntry { ItemId = "herb_seed",  Weight = 0.5f,            MinAmount = 1, MaxAmount = 1 },
                }
            };
        }

        private static List<StructureDef> BuildStructures() => new()
        {
            new StructureDef
            {
                StructureId = "shelter", DisplayName = "Shelter",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "wood",  Amount = 5 },
                    new BuildCost { ItemId = "scrap", Amount = 3 }
                }
            },
            new StructureDef
            {
                StructureId = "rain_catcher", DisplayName = "Rain Catcher",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "scrap", Amount = 3 },
                    new BuildCost { ItemId = "rope",  Amount = 2 }
                }
            },
            new StructureDef
            {
                StructureId = "windbreak", DisplayName = "Windbreak",
                BuildCosts = new[] { new BuildCost { ItemId = "wood", Amount = 4 } }
            },
            new StructureDef
            {
                StructureId = "path", DisplayName = "Path",
                BuildCosts = new[] { new BuildCost { ItemId = "stone", Amount = 2 } }
            },
            new StructureDef
            {
                StructureId = "scaffolding", DisplayName = "Scaffolding",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "wood",  Amount = 8 },
                    new BuildCost { ItemId = "scrap", Amount = 5 },
                    new BuildCost { ItemId = "nails", Amount = 3 }
                },
                PlacementRule = PlacementRule.EdgeCellOnly
            },
            new StructureDef
            {
                StructureId = "skynet", DisplayName = "Skynet",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "skynet_frame", Amount = 1 },
                    new BuildCost { ItemId = "rope",         Amount = 2 }
                },
                PlacementRule = PlacementRule.CliffEdgeOnly
            },
            new StructureDef
            {
                StructureId = "crate", DisplayName = "Crate",
                SlotCount = 10,
                BuildCosts = new[] { new BuildCost { ItemId = "wood", Amount = 4 } }
            },
            new StructureDef
            {
                StructureId = "barrel", DisplayName = "Barrel",
                SlotCount = 8,
                BuildCosts = new[] {
                    new BuildCost { ItemId = "wood",  Amount = 3 },
                    new BuildCost { ItemId = "scrap", Amount = 1 }
                }
            },
            new StructureDef
            {
                StructureId = "drying_rack", DisplayName = "Drying Rack",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "wood", Amount = 4 },
                    new BuildCost { ItemId = "rope", Amount = 2 }
                }
            },
            new StructureDef
            {
                StructureId = "stone_mill", DisplayName = "Stone Mill",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "stone", Amount = 6 },
                    new BuildCost { ItemId = "wood",  Amount = 4 },
                    new BuildCost { ItemId = "nails", Amount = 2 }
                }
            },
            new StructureDef
            {
                StructureId = "forge", DisplayName = "Forge",
                BuildCosts = new[] {
                    new BuildCost { ItemId = "stone", Amount = 8 },
                    new BuildCost { ItemId = "scrap", Amount = 2 },
                    new BuildCost { ItemId = "nails", Amount = 2 }
                }
            }
        };
    }
}
