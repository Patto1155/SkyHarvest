// Assets/Scripts/Player/TileInteractability.cs
// Decides whether a grid cell can be acted on with the current hotbar selection.
// Used by GameCursor to light up only actionable tiles under the pointer.
using UnityEngine;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Debris;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Skynet;
using SkyHarvest.Storage;
using SkyHarvest.Workshop;

namespace SkyHarvest.Player
{
    public static class TileInteractability
    {
        private const float DebrisCellRadius = 0.55f;

        public static bool CanInteractAt(
            Vector2Int gridPos,
            IslandData island,
            PlayerController player,
            ToolSystem tools,
            Hotbar hotbar,
            BuildModeController? buildMode)
        {
            if (island == null || player == null) return false;

            if (buildMode != null && buildMode.IsActive)
                return buildMode.CanPlaceAt(gridPos);

            var cell = island.GetCell(gridPos);
            if (cell == null) return false;
            if (Mathf.RoundToInt(cell.Elevation) != player.CurrentTier) return false;

            var tool = tools != null ? tools.EquippedTool : ToolType.None;
            string? heldItem = hotbar?.HeldItemId;
            var plot = FindPlotAt(gridPos);
            var structure = StructureRegistry.Instance?.GetStructureAt(gridPos);

            if (!string.IsNullOrEmpty(heldItem) && !ToolItems.IsTool(heldItem) && plot != null && plot.IsEmpty && IsSowableSeed(heldItem))
                return true;

            switch (tool)
            {
                case ToolType.Hoe:
                    if (plot == null && !cell.IsTilled && TerrainProperties.CanPlaceCrops(cell.Terrain))
                        return true;
                    if (plot != null && plot.Crop != null && plot.Crop.IsDead)
                        return true;
                    break;

                case ToolType.WateringCan:
                    if (plot != null && (plot.Crop == null || !plot.Crop.IsDead))
                        return true;
                    break;

                case ToolType.Sickle:
                    if (plot != null && plot.HasCrop && plot.Crop!.IsHarvestable)
                        return true;
                    break;

                case ToolType.Hammer:
                    if (structure != null)
                        return true;
                    break;

                case ToolType.None:
                    if (structure != null)
                        return CanUseStructure(structure, player);
                    break;
            }

            if (HasDebrisNearCell(gridPos, cell.Elevation))
                return true;

            return false;
        }

        private static bool CanUseStructure(Structure structure, PlayerController player)
        {
            switch (structure)
            {
                case ConstructionSite site:
                    return CanDeliverToSite(site, player);
                case WorkshopBase:
                case StorageContainer:
                    return true;
                case Skynet.Skynet skynet:
                    return skynet.GetBufferContents().Count > 0;
                default:
                    return false;
            }
        }

        private static bool CanDeliverToSite(ConstructionSite site, PlayerController player)
        {
            var inv = player.GetComponent<PlayerInventoryComponent>()?.Inventory;
            if (inv == null || site.Progress == null) return false;

            foreach (var (itemId, remaining) in site.Progress.RemainingCosts())
            {
                if (remaining > 0 && inv.GetCount(itemId) > 0)
                    return true;
            }
            return false;
        }

        private static CropPlot? FindPlotAt(Vector2Int pos)
        {
            foreach (var i in InteractableRegistry.All)
            {
                if (i is CropPlot plot && plot.GridPos == pos)
                    return plot;
            }
            return null;
        }

        private static bool HasDebrisNearCell(Vector2Int gridPos, float elevation)
        {
            Vector2 cellWorld = GridMath.GridToWorld(gridPos, elevation);
            foreach (var i in InteractableRegistry.All)
            {
                if (i is not DebrisObject debris) continue;
                if (debris is not MonoBehaviour mb) continue;
                if (Vector2.Distance(cellWorld, mb.transform.position) <= DebrisCellRadius)
                    return true;
            }
            return false;
        }

        private static bool IsSowableSeed(string seedItemId)
        {
            try { return GameDatabase.GetCropForSeed(seedItemId) != null; }
            catch { return false; }
        }
    }
}
