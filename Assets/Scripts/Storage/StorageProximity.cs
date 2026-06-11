// Registry-distance based (no Physics2D) proximity lookup for StorageContainers.
// Iterates StructureRegistry.AllStructures, filters for StorageContainer within radius.
using UnityEngine;
using SkyHarvest.Building;

namespace SkyHarvest.Storage
{
    public static class StorageProximity
    {
        /// <summary>
        /// Find the nearest StorageContainer to worldPos within radius.
        /// Uses StructureRegistry — no physics overlap.
        /// </summary>
        public static StorageContainer FindNearest(Vector2 worldPos, float radius = 3f)
        {
            if (StructureRegistry.Instance == null) return null;

            StorageContainer nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var structure in StructureRegistry.Instance.AllStructures)
            {
                if (structure is not StorageContainer sc) continue;

                float wx = (structure.GridPosition.x - structure.GridPosition.y) * 0.5f;
                float wy = (structure.GridPosition.x + structure.GridPosition.y) * -0.25f;
                float dist = Vector2.Distance(worldPos, new Vector2(wx, wy));

                if (dist <= radius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = sc;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Try to pull itemId/amount from any nearby storage container.
        /// Returns true if the item was found and removed.
        /// </summary>
        public static bool TryPullItem(Vector2 worldPos, string itemId, int amount, float radius = 3f)
        {
            var sc = FindNearest(worldPos, radius);
            return sc != null && sc.Storage.TryRemove(itemId, amount);
        }
    }
}
