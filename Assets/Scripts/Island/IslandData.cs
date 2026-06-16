// Assets/Scripts/Island/IslandData.cs
// Owned by: world/island agent
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class IslandCell
    {
        public Vector2Int GridPos;
        public TerrainType Terrain;
        public float Elevation;
        public SoilState Soil  = null!;
        public bool IsEdge;

        /// <summary>True when this cell has a tilled CropPlot on it.</summary>
        public bool IsTilled;
    }

    public class IslandData
    {
        public Dictionary<Vector2Int, IslandCell> Cells { get; } = new();
        public int Seed { get; }
        public int Radius { get; }

        /// <summary>True when this island was built by StarterIsland (not IslandGenerator).</summary>
        public bool IsStarter { get; private set; }
        internal void MarkAsStarter() => IsStarter = true;

        public IslandData(int seed, int radius = 0)
        {
            Seed   = seed;
            Radius = radius;
        }

        public IslandCell? GetCell(Vector2Int pos) =>
            Cells.TryGetValue(pos, out var cell) ? cell : null;

        public bool IsValidPosition(Vector2Int pos) => Cells.ContainsKey(pos);

        /// <summary>
        /// Returns true when the cell exists and is walkable (not a cliff edge
        /// that drops off — for MVP, all cells that exist are walkable).
        /// </summary>
        public bool IsWalkable(Vector2Int pos) => IsValidPosition(pos);

        // -------------------------------------------------------------------
        // Two-tier traversal + mineable-stair gate.
        //
        // Cells carry an integer Tier (= rounded Elevation). Movement within a
        // tier is free; crossing to a different tier is blocked everywhere
        // except across a registered "stair edge", and even there only after
        // the stairs have been carved (the tutorial mining beat).
        // -------------------------------------------------------------------

        /// <summary>Registered cell-pairs that may be traversed across a tier
        /// boundary once the stairs are carved. Stored order-independently.</summary>
        private readonly HashSet<(Vector2Int, Vector2Int)> _stairEdges = new();

        /// <summary>True once the tutorial staircase has been mined.</summary>
        public bool StairsCarved { get; private set; }

        /// <summary>The integer tier of a cell (rounded elevation); 0 if absent.</summary>
        public int Tier(Vector2Int pos) =>
            Cells.TryGetValue(pos, out var c) ? Mathf.RoundToInt(c.Elevation) : 0;

        private static (Vector2Int, Vector2Int) Edge(Vector2Int a, Vector2Int b) =>
            (a.x < b.x || (a.x == b.x && a.y <= b.y)) ? (a, b) : (b, a);

        /// <summary>Register a pair of cells as the carve-able stair connection
        /// between two tiers.</summary>
        public void AddStairEdge(Vector2Int a, Vector2Int b) => _stairEdges.Add(Edge(a, b));

        public bool IsStairEdge(Vector2Int a, Vector2Int b) => _stairEdges.Contains(Edge(a, b));

        /// <summary>Carve the staircase: permanently unlock tier traversal across
        /// the registered stair edges. Idempotent; fires StairsCarvedEvent once.</summary>
        public void CarveStairs(Vector2Int stairCell)
        {
            if (StairsCarved) return;
            StairsCarved = true;
            EventBus.Publish(new StairsCarvedEvent { StairX = stairCell.x, StairY = stairCell.y });
        }

        /// <summary>True when the player may step from one cell to an adjacent
        /// one: the target must exist, and any change of tier must go through a
        /// carved stair edge.</summary>
        public bool CanTraverse(Vector2Int from, Vector2Int to)
        {
            if (!IsWalkable(to) || !IsValidPosition(from)) return false;
            if (Tier(from) == Tier(to)) return true;
            return StairsCarved && IsStairEdge(from, to);
        }
    }
}
