// Singleton registry of all placed structures.
// Used by workshop auto-pull (StorageProximity), save/load, and demolish.
using System.Collections.Generic;
using UnityEngine;

namespace SkyHarvest.Building
{
    public class StructureRegistry : MonoBehaviour
    {
        public static StructureRegistry Instance { get; private set; }

        private readonly Dictionary<Vector2Int, Structure> _byPos = new();
        private readonly List<Structure> _all = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(Structure s)
        {
            _byPos[s.GridPosition] = s;
            if (!_all.Contains(s)) _all.Add(s);
        }

        public void Unregister(Structure s)
        {
            _byPos.Remove(s.GridPosition);
            _all.Remove(s);
        }

        public bool HasStructureAt(Vector2Int pos) => _byPos.ContainsKey(pos);

        public Structure GetStructureAt(Vector2Int pos) =>
            _byPos.TryGetValue(pos, out var s) ? s : null;

        /// <summary>
        /// Snapshot list — safe to iterate while structures are added/removed.
        /// </summary>
        public IReadOnlyList<Structure> AllStructures => _all;
    }
}
