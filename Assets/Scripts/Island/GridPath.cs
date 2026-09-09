// Pure BFS over the island grid, honouring IslandData.CanTraverse (tiers + stair gate).
// Used by tap-to-move; small islands make BFS plenty fast.
using System.Collections.Generic;
using UnityEngine;

namespace SkyHarvest.Island
{
    public static class GridPath
    {
        private static readonly Vector2Int[] Neighbours =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        /// <summary>
        /// Shortest 4-neighbour path from <paramref name="from"/> to <paramref name="to"/>,
        /// excluding the start cell and including the goal. Null when unreachable.
        /// </summary>
        public static List<Vector2Int>? Find(IslandData island, Vector2Int from, Vector2Int to, int maxNodes = 4096)
        {
            if (island == null || !island.IsValidPosition(from) || !island.IsValidPosition(to)) return null;
            if (from == to) return new List<Vector2Int>();

            var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);

            while (queue.Count > 0 && cameFrom.Count <= maxNodes)
            {
                var cur = queue.Dequeue();
                foreach (var d in Neighbours)
                {
                    var next = cur + d;
                    if (cameFrom.ContainsKey(next)) continue;
                    if (!island.CanTraverse(cur, next)) continue;
                    cameFrom[next] = cur;
                    if (next == to) return Reconstruct(cameFrom, from, to);
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int>();
            var cur = to;
            while (cur != from)
            {
                path.Add(cur);
                cur = cameFrom[cur];
            }
            path.Reverse();
            return path;
        }
    }
}
