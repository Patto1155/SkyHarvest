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
        /// Convert a logical grid cell to 2-D world space (diamond bottom-tip anchor;
        /// matches terrain sprite pivot 0.5,0 on the 64×32 face).
        /// </summary>
        public static Vector2 GridToWorld(Vector2Int grid, float elevation = 0f)
        {
            float wx = (grid.x - grid.y) * 0.5f;
            float wy = (grid.x + grid.y) * -0.25f + elevation * Constants.ElevationWorldStep;
            return new Vector2(wx, wy);
        }

        /// <summary>Geometric centre of the 1×0.5 walk / render diamond.</summary>
        public static Vector2 DiamondCentre(Vector2Int grid, float elevation = 0f) =>
            GridToWorld(grid, elevation) + new Vector2(0f, Constants.TileWorldHeight * 0.5f);

        private const float DiamondPickEpsilon = 0.001f;

        private static readonly Vector2Int[] DiamondNeighborOffsets =
        {
            new( 0,  0),
            new( 1,  0), new(-1,  0), new( 0,  1), new( 0, -1),
            new( 1,  1), new( 1, -1), new(-1,  1), new(-1, -1),
        };

        /// <summary>
        /// True when <paramref name="world"/> lies inside the isometric diamond for
        /// <paramref name="cell"/> at the given elevation tier.
        /// </summary>
        public static bool ContainsDiamond(Vector2 world, Vector2Int cell, float elevation = 0f)
        {
            Vector2 centre = DiamondCentre(cell, elevation);
            float dx = Mathf.Abs(world.x - centre.x);
            float dy = Mathf.Abs(world.y - centre.y);
            float hw = Constants.TileWorldWidth  * 0.5f;
            float hh = Constants.TileWorldHeight * 0.5f;
            return dx / hw + dy / hh <= 1f + DiamondPickEpsilon;
        }

        /// <summary>
        /// Convert a 2-D world position to the grid cell whose diamond contains it.
        /// Rounding alone mis-picks along shared diamond edges; this checks the
        /// rounded cell and its neighbours.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector2 world, float elevation = 0f)
        {
            float elevationOffset = elevation * Constants.ElevationWorldStep;
            float sum  = (elevationOffset - world.y) / 0.25f;   // gx + gy
            float diff = world.x * 2f;                            // gx - gy
            float gx   = (sum + diff) * 0.5f;
            float gy   = (sum - diff) * 0.5f;

            int cx = Mathf.RoundToInt(gx);
            int cy = Mathf.RoundToInt(gy);

            Vector2Int best     = new(cx, cy);
            float        bestDist = float.MaxValue;

            foreach (var offset in DiamondNeighborOffsets)
            {
                var candidate = new Vector2Int(cx + offset.x, cy + offset.y);
                if (!ContainsDiamond(world, candidate, elevation)) continue;

                Vector2 centre = DiamondCentre(candidate, elevation);
                float dist = (world - centre).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Convert a screen-space pointer position to a grid cell on the given tier.
        /// </summary>
        public static Vector2Int ScreenToGrid(Camera cam, Vector3 screenPos, float elevation = 0f)
        {
            screenPos.z = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(screenPos);
            return WorldToGrid(new Vector2(world.x, world.y), elevation);
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
