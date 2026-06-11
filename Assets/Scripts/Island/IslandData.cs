// Assets/Scripts/Island/IslandData.cs
// Owned by: world/island agent
using System.Collections.Generic;
using UnityEngine;

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
    }
}
