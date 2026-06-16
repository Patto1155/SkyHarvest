// Assets/Scripts/Island/StarterIsland.cs
// Owned by: world/island agent
//
// The designed hero starter island — a fixed two-tier piece (not procedurally
// generated). 3 wide (gx 0..2) x 4 deep (gy 0..3):
//
//   BACK / raised tier  (FORGE) : gy in {0,1}, elevation = 1  (renders +0.5 world)
//   FRONT / lower tier  (FARM)  : gy in {2,3}, elevation = 0
//
// A stone wall runs along the gy1|gy2 boundary. The staircase is carved in the
// tutorial at the MIDDLE boundary column (gx = StairColumn): the back tier is
// unreachable until the player mines from FrontStairCell up into BackStairCell.
//
// Pure construction (no Unity scene types) so it is unit-testable and the same
// data drives both the placeholder-diamond render and the final painted art.
using UnityEngine;

namespace SkyHarvest.Island
{
    public static class StarterIsland
    {
        public const int Width = 3;   // gx 0..2
        public const int Depth = 4;   // gy 0..3
        public const int StairColumn = 1;            // middle of the 3-wide boundary

        public static readonly Vector2Int FrontStairCell = new Vector2Int(StairColumn, 2);
        public static readonly Vector2Int BackStairCell  = new Vector2Int(StairColumn, 1);

        private static bool IsBackTier(int gy) => gy <= 1;   // raised forge tier

        public static IslandData Build(int seed = 0)
        {
            var island = new IslandData(seed);

            for (int gx = 0; gx < Width; gx++)
            for (int gy = 0; gy < Depth; gy++)
            {
                bool back = IsBackTier(gy);
                // Front-row outer corners are the dangerous cliff lip: debris tumbles in
                // here and Skynet can be planted to catch it. Kept off the (1,3) spawn cell
                // and off every cell referenced by TierGateTests so the tier gate is untouched.
                bool frontCliff = !back && gy == Depth - 1 && (gx == 0 || gx == Width - 1);
                var terrain = back        ? TerrainType.RockyPlateau
                            : frontCliff  ? TerrainType.CliffEdge
                                          : TerrainType.FertileValley;
                var pos = new Vector2Int(gx, gy);
                island.Cells[pos] = new IslandCell
                {
                    GridPos   = pos,
                    Terrain   = terrain,
                    Elevation = back ? 1f : 0f,
                    Soil      = new SoilState(terrain),
                    IsEdge    = gx == 0 || gx == Width - 1 || gy == 0 || gy == Depth - 1,
                };
            }

            // The one place the player can cross between tiers — locked until mined.
            island.AddStairEdge(FrontStairCell, BackStairCell);
            island.MarkAsStarter();
            return island;
        }
    }
}
