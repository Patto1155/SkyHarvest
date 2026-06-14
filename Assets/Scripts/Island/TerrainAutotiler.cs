// Assets/Scripts/Island/TerrainAutotiler.cs
// Owned by: world/island agent
//
// Pure-static procedural autotile bitmask logic — no MonoBehaviour, no Unity
// imports beyond the math types. Tested by AutotileTests via tools/check.sh.
//
// The autotiler answers: for a given cell at position P with terrain T, which
// of its 8 neighbours are "different" (different terrain or absent/off-island)?
// It encodes this as a bitmask (bit 0 = N, 1 = NE, 2 = E, 3 = SE, 4 = S,
// 5 = SW, 6 = W, 7 = NW — isometric compass, NOT screen-space) and produces
// a list of BlendSample descriptors for the renderer to materialise.
//
// Note: "north" in the iso dimetric grid is gx+1,gy-1 (upper-right on screen).
// For the purpose of this class we simply define neighbours by their grid offsets
// and do not care about screen direction — the renderer places overlays at the
// correct world positions.
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Island;

namespace SkyHarvest.Island
{
    /// <summary>
    /// Describes one edge/corner blend overlay that should be rendered on a cell.
    /// </summary>
    public struct BlendSample
    {
        /// <summary>Grid-space offset from cell centre toward the neighbour (may be diagonal).</summary>
        public Vector2Int NeighbourOffset;

        /// <summary>Normalised direction in world space toward the blended edge.</summary>
        public Vector2 WorldDirection;

        /// <summary>
        /// The neighbour's terrain type; used to tint the feather toward that colour.
        /// Null = off-island (blend toward void/sky).
        /// </summary>
        public TerrainType? NeighbourTerrain;

        /// <summary>Alpha weight for this blend: 1.0 = full edge, 0.7 = corner.</summary>
        public float Weight;
    }

    /// <summary>
    /// All blend information for one island cell.
    /// </summary>
    public readonly struct CellBlendInfo
    {
        /// <summary>8-bit bitmask; bit i set = neighbour i is different/absent.</summary>
        public readonly int Bitmask;

        /// <summary>Ordered blend overlays to render. Empty when bitmask == 0.</summary>
        public readonly IReadOnlyList<BlendSample> Samples;

        public CellBlendInfo(int bitmask, List<BlendSample> samples)
        {
            Bitmask = bitmask;
            Samples = samples;
        }

        public bool HasAnyBlend => Bitmask != 0;
    }

    /// <summary>
    /// Pure static autotile bitmask calculator.
    /// </summary>
    public static class TerrainAutotiler
    {
        // Neighbour offsets in grid space. Index maps to bit position 0..7.
        // Layout: N, NE, E, SE, S, SW, W, NW (grid-compass, starting north).
        // In the iso dimetric grid:
        //   N  = (+1, -1)  upper-right on screen
        //   E  = (+1, +0)  right on screen but down-right
        //   S  = (+0, +1)  lower-left on screen
        //   W  = (-1, +0)  left on screen
        //   NE = (+1, +0)  -- cardinal only for 4-dir; diagonals:
        // We use 8 neighbours:
        //   0: (0,-1)  iso-NW
        //   1: (+1,-1) iso-N
        //   2: (+1,0)  iso-NE
        //   3: (+1,+1) iso-E
        //   4: (0,+1)  iso-SE
        //   5: (-1,+1) iso-S
        //   6: (-1,0)  iso-SW
        //   7: (-1,-1) iso-W
        //
        // (We intentionally use screen-adjacent offsets, not rotated iso-compass,
        //  so the 4 cardinal directions are the orthogonal iso neighbours.)
        public static readonly Vector2Int[] NeighbourOffsets = new Vector2Int[8]
        {
            new Vector2Int( 0, -1),  // 0: gx same, gy-1  → upper-right pair
            new Vector2Int(+1, -1),  // 1: NE diagonal
            new Vector2Int(+1,  0),  // 2: gx+1  → right
            new Vector2Int(+1, +1),  // 3: SE diagonal
            new Vector2Int( 0, +1),  // 4: gx same, gy+1 → lower-left pair
            new Vector2Int(-1, +1),  // 5: SW diagonal
            new Vector2Int(-1,  0),  // 6: gx-1  → left
            new Vector2Int(-1, -1),  // 7: NW diagonal
        };

        // World-space direction for each neighbour offset (normalized, iso projection).
        // wx=(gx-gy)*0.5, wy=(gx+gy)*-0.25 → for offset (dx,dy):
        //   wdx = (dx-dy)*0.5, wdy = (dx+dy)*-0.25
        private static readonly Vector2[] _worldDirs;

        static TerrainAutotiler()
        {
            _worldDirs = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                var off = NeighbourOffsets[i];
                float wx = (off.x - off.y) * 0.5f;
                float wy = (off.x + off.y) * -0.25f;
                float mag = Mathf.Sqrt(wx * wx + wy * wy);
                _worldDirs[i] = mag > 1e-5f ? new Vector2(wx / mag, wy / mag) : Vector2.zero;
            }
        }

        /// <summary>
        /// Compute the blend information for a single cell.
        /// </summary>
        /// <param name="cell">The cell to evaluate.</param>
        /// <param name="island">The island data for neighbour lookups.</param>
        /// <returns>Bitmask + blend samples (empty list when no blending needed).</returns>
        public static CellBlendInfo Compute(IslandCell cell, IslandData island)
        {
            int bitmask = 0;
            var samples = new List<BlendSample>(8);

            for (int i = 0; i < 8; i++)
            {
                var neighbourPos = cell.GridPos + NeighbourOffsets[i];
                var neighbour = island.GetCell(neighbourPos);

                bool isDifferent = neighbour == null || neighbour.Terrain != cell.Terrain;
                if (!isDifferent) continue;

                bitmask |= (1 << i);

                // Diagonals only contribute if at least one of their orthogonal
                // neighbours also differs — this prevents spurious corner blends
                // when two like-terrain cells share only a diagonal.
                bool isDiagonal = (i % 2) == 1;
                if (isDiagonal && !DiagonalNeedsBlend(i, cell, island))
                    continue;

                float weight = isDiagonal ? 0.55f : 1.0f;

                samples.Add(new BlendSample
                {
                    NeighbourOffset  = NeighbourOffsets[i],
                    WorldDirection   = _worldDirs[i],
                    NeighbourTerrain = neighbour?.Terrain,
                    Weight           = weight,
                });
            }

            return new CellBlendInfo(bitmask, samples);
        }

        /// <summary>
        /// Refresh blend info for a cell and all of its existing neighbours.
        /// Used after island expansion adds new cells so existing border cells
        /// get their blend overlays updated.
        /// </summary>
        public static IEnumerable<Vector2Int> AffectedPositions(Vector2Int pos)
        {
            yield return pos;
            foreach (var off in NeighbourOffsets)
                yield return pos + off;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// For a diagonal neighbour at index i (odd indices), return true if
        /// at least one of its two flanking orthogonal neighbours is also different
        /// from the current cell's terrain.  This avoids corner blends that look
        /// wrong when two matching-terrain tiles share only a diagonal.
        /// </summary>
        private static bool DiagonalNeedsBlend(int diagIdx, IslandCell cell, IslandData island)
        {
            // Flanking orthogonals: neighbour before and after in the 8-ring.
            int prevIdx = (diagIdx + 7) % 8;
            int nextIdx = (diagIdx + 1) % 8;

            var prevPos = cell.GridPos + NeighbourOffsets[prevIdx];
            var nextPos = cell.GridPos + NeighbourOffsets[nextIdx];

            var prevN = island.GetCell(prevPos);
            var nextN = island.GetCell(nextPos);

            bool prevDiff = prevN == null || prevN.Terrain != cell.Terrain;
            bool nextDiff = nextN == null || nextN.Terrain != cell.Terrain;

            return prevDiff || nextDiff;
        }
    }
}
