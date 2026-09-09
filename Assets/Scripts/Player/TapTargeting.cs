// Pure helpers for tap-to-move: which cell a world point means, and whether the player is
// already close enough to act on it without walking. Testable under check.sh.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Player
{
    public static class TapTargeting
    {
        public const float InteractRadius = 1.2f;
        public const int MaxTier = 3;

        /// <summary>
        /// Resolve a world point to a grid cell. The same screen point maps to different cells on
        /// different tiers (elevation shifts the projection), so try the player's own tier first,
        /// then the others, accepting only a cell that really sits on the tier it was resolved for.
        /// </summary>
        public static Vector2Int? ResolveCell(IslandData island, Vector2 world, int preferredTier)
        {
            if (island == null) return null;

            var pos = GridMath.WorldToGrid(world, preferredTier);
            if (island.IsValidPosition(pos) && island.Tier(pos) == preferredTier) return pos;

            for (int tier = 0; tier <= MaxTier; tier++)
            {
                if (tier == preferredTier) continue;
                pos = GridMath.WorldToGrid(world, tier);
                if (island.IsValidPosition(pos) && island.Tier(pos) == tier) return pos;
            }
            return null;
        }

        /// <summary>True when the player can act on the cell from where they stand.</summary>
        public static bool IsWithinReach(Vector2 playerWorld, Vector2Int cell, float cellElevation) =>
            Vector2.Distance(playerWorld, GridMath.GridToWorld(cell, cellElevation)) <= InteractRadius;

        /// <summary>
        /// Convert a world-space direction into the (h, v) axes PlayerController.Move expects, so
        /// auto-walk goes through exactly the same tier-aware stepping as keyboard input.
        /// </summary>
        public static (float h, float v) WorldDirToAxes(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-6f) return (0f, 0f);
            float h = dir.x / 0.5f;
            float v = dir.y / 0.25f;
            float mag = Mathf.Max(Mathf.Abs(h), Mathf.Abs(v));
            return (h / mag, v / mag);
        }
    }
}
