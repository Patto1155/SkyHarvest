// Assets/Scripts/Island/IslandExpansion.cs
// Owned by: world/island agent
// Pure-static logic.  Called by Building agent's scaffolding placement code.
using System.Collections.Generic;
using UnityEngine;

namespace SkyHarvest.Island
{
    public static class IslandExpansion
    {
        private static readonly Vector2Int[] Neighbors = {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1)
        };

        /// <summary>
        /// Place scaffolding at <paramref name="scaffoldPos"/> and expand the island
        /// by adding new scaffold cells in any empty adjacent directions.
        /// Returns the list of newly created cells so the renderer can build them.
        /// Publishes <see cref="Core.IslandExpandedEvent"/>.
        /// </summary>
        public static List<IslandCell> Expand(IslandData island, Vector2Int scaffoldPos)
        {
            var newCells = new List<IslandCell>();

            // Reference elevation from the scaffold cell itself
            float baseElevation = island.GetCell(scaffoldPos)?.Elevation ?? 0f;

            foreach (var offset in Neighbors)
            {
                var neighborPos = scaffoldPos + offset;
                if (island.IsValidPosition(neighborPos)) continue;

                var newCell = new IslandCell
                {
                    GridPos   = neighborPos,
                    Terrain   = TerrainType.Scaffold,
                    Elevation = baseElevation,
                    Soil      = new SoilState(TerrainType.Scaffold),
                    IsEdge    = true
                };

                island.Cells[neighborPos] = newCell;
                newCells.Add(newCell);
            }

            // The scaffold cell is no longer the outermost edge
            var scaffoldCell = island.GetCell(scaffoldPos);
            if (scaffoldCell != null)
                scaffoldCell.IsEdge = false;

            if (newCells.Count > 0)
                Core.EventBus.Publish(new Core.IslandExpandedEvent
                    { NewCellCount = newCells.Count });

            return newCells;
        }
    }
}
