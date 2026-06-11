// Assets/Scripts/Farming/FarmingActions.cs
// Owned by: world/island agent
// Static helpers for till, sow, water, harvest, clear-dead.
// Uses GameDatabase API (owned by Data/UI agent).
// GameDatabase may not exist yet — code guards with null-checks.
using UnityEngine;
using SkyHarvest.Player;
using SkyHarvest.Island;
using SkyHarvest.Core;
using SkyHarvest.Data;

// NOTE FOR DATA/UI AGENT:
// This file calls:
//   SkyHarvest.Data.GameDatabase.GetCropForSeed(string seedItemId) -> CropDef
//   SkyHarvest.Data.GameDatabase.GetCrop(string cropId)            -> CropDef
// Those must exist for harvest yield to work.  Until GameDatabase.cs is created
// by the Data agent, null-returns degrade gracefully (yield = 1, cropId preserved).

namespace SkyHarvest.Farming
{
    public static class FarmingActions
    {
        // -----------------------------------------------------------------------
        // Till — called when player uses Hoe on an untilled valid cell
        // -----------------------------------------------------------------------

        /// <summary>
        /// Attempt to till the given island cell and create a CropPlot there.
        /// The returned plot is added to the world and registered with CropGrowthSystem.
        /// </summary>
        public static CropPlot? TryTill(IslandCell cell, IslandData island, IslandRenderer? renderer)
        {
            if (cell == null) return null;
            if (!TerrainProperties.CanPlaceCrops(cell.Terrain)) return null;
            if (cell.IsTilled) return null;   // already tilled

            cell.Soil.Till();
            cell.IsTilled = true;

            // Build world position for the plot object
            Vector2 worldPos = GridMath.GridToWorld(cell.GridPos, cell.Elevation);
            var go  = new GameObject($"CropPlot_{cell.GridPos.x}_{cell.GridPos.y}");
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            var plot = go.AddComponent<CropPlot>();
            plot.Soil    = cell.Soil;
            plot.GridPos = cell.GridPos;

            // Refresh soil overlay
            renderer?.RefreshCellOverlay(cell.GridPos);

            return plot;
        }

        // -----------------------------------------------------------------------
        // Sow — place a seed from inventory into a tilled empty plot
        // -----------------------------------------------------------------------
        public static void TrySow(CropPlot plot, PlayerController player)
        {
            var inv = player.GetComponent<PlayerInventoryComponent>()?.Inventory;
            if (inv == null) return;

            // Find first seed in inventory that maps to a known crop
            foreach (var slot in inv.Slots)
            {
                if (slot.IsEmpty) continue;

                CropDef? cropDef = TryGetCropForSeed(slot.ItemId);
                if (cropDef == null) continue;

                if (!inv.TryRemove(slot.ItemId, 1)) continue;

                plot.Crop = new CropState(
                    cropDef.CropId,
                    cropDef.GrowthTimeMinutes,
                    cropDef.GrowthStages,
                    cropDef.WaterConsumptionPerMinute);

                CropGrowthSystem.Instance?.Register(plot);
                EventBus.Publish(new CropPlantedEvent { CropId = cropDef.CropId });
                plot.RefreshVisuals();
                return;
            }
        }

        // -----------------------------------------------------------------------
        // Water — add 25 units to soil moisture
        // -----------------------------------------------------------------------
        public static void Water(CropPlot plot)
        {
            plot.Soil.AddWater(25f);
            plot.RefreshVisuals();
        }

        // -----------------------------------------------------------------------
        // Harvest — collect crop yield into player inventory
        // -----------------------------------------------------------------------
        public static void Harvest(CropPlot plot, PlayerController player)
        {
            if (plot.Crop == null || !plot.Crop.IsHarvestable) return;

            var inv = player.GetComponent<PlayerInventoryComponent>()?.Inventory;
            if (inv == null) return;

            // Resolve yield via GameDatabase (may be null until Data agent ships)
            CropDef? cropDef = TryGetCrop(plot.Crop.CropId);

            string yieldItem = cropDef?.HarvestYieldItemId ?? plot.Crop.CropId;
            int yieldMin     = cropDef?.HarvestYieldMin    ?? 1;
            int yieldMax     = cropDef?.HarvestYieldMax    ?? 1;
            int yield        = Random.Range(yieldMin, yieldMax + 1);

            if (!inv.TryAdd(yieldItem, yield))
                return;   // inventory full — don't destroy crop

            plot.Soil.RecordHarvest(plot.Crop.CropId);
            CropGrowthSystem.Instance?.Unregister(plot);

            EventBus.Publish(new CropHarvestedEvent
            {
                CropId      = plot.Crop.CropId,
                YieldItemId = yieldItem,
                Amount      = yield
            });

            plot.Crop = null;
            plot.RefreshVisuals();
        }

        // -----------------------------------------------------------------------
        // ClearDead — remove a dead crop so the plot can be replanted
        // -----------------------------------------------------------------------
        public static void ClearDead(CropPlot plot)
        {
            if (plot.Crop == null || !plot.Crop.IsDead) return;
            CropGrowthSystem.Instance?.Unregister(plot);
            plot.Crop = null;
            plot.RefreshVisuals();
        }

        // -----------------------------------------------------------------------
        // GameDatabase helpers (null-safe — works even if DB not yet shipped)
        // -----------------------------------------------------------------------
        private static CropDef? TryGetCropForSeed(string seedItemId)
        {
            try   { return GameDatabase.GetCropForSeed(seedItemId); }
            catch { return null; }
        }

        private static CropDef? TryGetCrop(string cropId)
        {
            try   { return GameDatabase.GetCrop(cropId); }
            catch { return null; }
        }
    }
}
