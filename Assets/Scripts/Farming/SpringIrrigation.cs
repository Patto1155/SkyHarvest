// Passive soil watering from NaturalSpring terrain neighbours (spec §4 irrigation MVP).
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Farming
{
    public static class SpringIrrigation
    {
        private const float WaterPerMinute = 4f;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        };

        /// <summary>Add moisture to plots orthogonally adjacent to any spring cell.</summary>
        public static void WaterAdjacentPlots(IslandData? island, float deltaMinutes)
        {
            if (island == null || deltaMinutes <= 0f) return;
            float amount = WaterPerMinute * deltaMinutes;
            if (amount <= 0f) return;

            foreach (var kvp in island.Cells)
            {
                if (!TerrainProperties.HasWaterSource(kvp.Value.Terrain)) continue;
                var springPos = kvp.Key;
                foreach (var off in NeighborOffsets)
                {
                    var neighbour = springPos + off;
                    var cell = island.GetCell(neighbour);
                    if (cell == null || !cell.IsTilled) continue;
                    cell.Soil.AddWater(amount);
                }
            }
        }

        /// <summary>True when <paramref name="plotPos"/> shares an edge with a spring cell.</summary>
        public static bool IsAdjacentToSpring(IslandData island, Vector2Int plotPos)
        {
            foreach (var off in NeighborOffsets)
            {
                var cell = island.GetCell(plotPos + off);
                if (cell != null && TerrainProperties.HasWaterSource(cell.Terrain))
                    return true;
            }
            return false;
        }
    }
}
