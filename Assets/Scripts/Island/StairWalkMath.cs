// Walkable volume for carved stair edges (links tier diamonds through the cliff channel).
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public static class StairWalkMath
    {
        private const float CorridorHalfWidth = 0.26f;
        private const float CorridorEndPad    = 0.32f;

        public static bool InCorridor(Vector2 world, Vector2Int a, Vector2Int b, IslandData island)
        {
            if (!island.IsStairEdge(a, b)) return false;

            ResolveEndsImpl(a, b, island, out var low, out int lowTier, out var high, out int highTier);

            if (GridMath.ContainsDiamond(world, low, lowTier)) return true;
            if (GridMath.ContainsDiamond(world, high, highTier)) return true;

            return TryProjectOntoCorridor(world, low, lowTier, high, highTier, out _, out var lateral) &&
                   lateral <= CorridorHalfWidth;
        }

        public static bool TryProjectOntoCorridor(Vector2 world,
            Vector2Int low, int lowTier, Vector2Int high, int highTier,
            out float t, out float lateralDistance)
        {
            CorridorSegment(low, lowTier, high, highTier, out var start, out var end);
            Vector2 axis = end - start;
            float len    = axis.magnitude;

            if (len <= Mathf.Epsilon)
            {
                t = 0f;
                lateralDistance = Vector2.Distance(world, start);
                return false;
            }

            Vector2 dir   = axis / len;
            float along   = Vector2.Dot(world - start, dir);
            t = Mathf.InverseLerp(0f, len, along);

            if (along < -CorridorEndPad * 0.5f || along > len + CorridorEndPad)
            {
                lateralDistance = float.MaxValue;
                return false;
            }

            Vector2 closest = start + dir * Mathf.Clamp(along, 0f, len);
            lateralDistance = Vector2.Distance(world, closest);
            return true;
        }

        public static void CorridorSegment(Vector2Int a, Vector2Int b, IslandData island,
            out Vector2 start, out Vector2 end)
        {
            ResolveEndsImpl(a, b, island, out var low, out int lowTier, out var high, out int highTier);
            CorridorSegment(low, lowTier, high, highTier, out start, out end);
        }

        public static void CorridorSegment(Vector2Int low, int lowTier, Vector2Int high, int highTier,
            out Vector2 start, out Vector2 end)
        {
            float hh = Constants.TileWorldHeight * 0.5f;
            Vector2 lowCentre  = GridMath.DiamondCentre(low, lowTier);
            Vector2 highCentre = GridMath.DiamondCentre(high, highTier);
            Vector2 axis = highCentre - lowCentre;
            Vector2 dir  = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector2.up;

            // Run from the back of the lower tile up through the upper tile surface.
            start = lowCentre - dir * CorridorEndPad;
            end   = highCentre + dir * (hh + CorridorEndPad);
        }

        public static Vector2 CorridorNormal(Vector2Int a, Vector2Int b, IslandData island)
        {
            CorridorSegment(a, b, island, out var start, out var end);
            Vector2 dir = (end - start).normalized;
            return new Vector2(-dir.y, dir.x);
        }

        public static int TierForProgress(float t, int lowTier, int highTier) =>
            t >= 0.42f ? highTier : lowTier;

        public static float HalfWidth => CorridorHalfWidth;

        public static void ResolveEnds(Vector2Int a, Vector2Int b, IslandData island,
            out Vector2Int low, out int lowTier, out Vector2Int high, out int highTier) =>
            ResolveEndsImpl(a, b, island, out low, out lowTier, out high, out highTier);

        /// <summary>Pull <paramref name="world"/> onto the corridor centreline when outside the band.</summary>
        public static Vector2 ClampToCorridor(Vector2 world,
            Vector2Int low, int lowTier, Vector2Int high, int highTier)
        {
            CorridorSegment(low, lowTier, high, highTier, out var start, out var end);
            Vector2 axis = end - start;
            float len    = axis.magnitude;
            if (len <= Mathf.Epsilon) return world;

            Vector2 dir   = axis / len;
            float along   = Mathf.Clamp(Vector2.Dot(world - start, dir), 0f, len);
            Vector2 onAxis  = start + dir * along;
            Vector2 offset  = world - onAxis;
            float dist      = offset.magnitude;
            if (dist <= CorridorHalfWidth || dist <= Mathf.Epsilon)
                return world;
            return onAxis + offset * (CorridorHalfWidth / dist);
        }

        private static void ResolveEndsImpl(Vector2Int a, Vector2Int b, IslandData island,
            out Vector2Int low, out int lowTier, out Vector2Int high, out int highTier)
        {
            int tierA = island.Tier(a);
            int tierB = island.Tier(b);
            if (tierA <= tierB)
            {
                low = a; lowTier = tierA;
                high = b; highTier = tierB;
            }
            else
            {
                low = b; lowTier = tierB;
                high = a; highTier = tierA;
            }
        }
    }
}
