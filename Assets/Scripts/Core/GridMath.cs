// Assets/Scripts/Core/GridMath.cs
// Owned by: world/island agent
// 2:1 dimetric projection as specified in CONVENTIONS.md
//   worldX = (gx - gy) * 0.5
//   worldY = (gx + gy) * -0.25 + elevation * ElevationWorldStep
// Sorting: sortingOrder = Round(-worldY * SortingOrderScale) + bias
using UnityEngine;

namespace SkyHarvest.Core
{
    public static class GridMath
    {
        /// <summary>
        /// Convert a logical grid cell to 2-D world space.
        /// </summary>
        public static Vector2 GridToWorld(Vector2Int grid, float elevation = 0f)
        {
            float wx = (grid.x - grid.y) * 0.5f;
            float wy = (grid.x + grid.y) * -0.25f + elevation * Constants.ElevationWorldStep;
            return new Vector2(wx, wy);
        }

        /// <summary>
        /// Convert a 2-D world position back to the nearest grid cell.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector2 world, float elevation = 0f)
        {
            // Inverse of GridToWorld (assuming elevation = 0 for flat grid):
            //   wx = (gx - gy) * 0.5  => gx - gy = 2*wx
            //   wy = (gx + gy) * -0.25 + elev*step => gx + gy = (elev*step - wy) / 0.25
            float elevationOffset = elevation * Constants.ElevationWorldStep;
            float sum = (elevationOffset - world.y) / 0.25f;   // gx + gy
            float diff = world.x * 2f;                          // gx - gy
            float gx = (sum + diff) * 0.5f;
            float gy = (sum - diff) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
        }

        /// <summary>
        /// Compute the sprite SortingOrder for a world-Y position.
        /// bias: terrain = -10000; flat overlays = -5000; normal = 0; UI prompts = +10000.
        /// </summary>
        public static int SortingOrder(float worldY, int bias = 0) =>
            Mathf.RoundToInt(-worldY * Constants.SortingOrderScale) + bias;

        /// <summary>Convenience overload: compute from a grid cell + elevation.</summary>
        public static int SortingOrder(Vector2Int grid, float elevation = 0f, int bias = 0)
        {
            Vector2 world = GridToWorld(grid, elevation);
            return SortingOrder(world.y, bias);
        }
    }
}
