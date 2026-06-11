// Assets/Scripts/Player/PlayerInventoryComponent.cs
// Owned by: world/island agent
// MonoBehaviour wrapper around the pure-C# Inventory POCO.
// Attach to the Player GameObject so other systems can GetComponent<PlayerInventoryComponent>().
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    public class PlayerInventoryComponent : MonoBehaviour
    {
        [SerializeField] private int _slotCount = 20;

        public Inventory Inventory { get; private set; } = null!;

        private void Awake()
        {
            Inventory = new Inventory(_slotCount);

            // Populate new-game starting items (CONVENTIONS §Game data IDs)
            Inventory.TryAdd("sky_moss_seed",   4);
            Inventory.TryAdd("cloud_root_seed", 3);
            Inventory.TryAdd("wheat_seed",      2);
            Inventory.TryAdd("herb_seed",       2);
            Inventory.TryAdd("wood",            4);
            Inventory.TryAdd("scrap",           2);
        }

        /// <summary>
        /// Replace the inventory contents wholesale (used by save/load restore).
        /// </summary>
        public void RestoreInventory(System.Collections.Generic.IEnumerable<(string itemId, int count)> slots)
        {
            Inventory = new Inventory(_slotCount);
            foreach (var (id, count) in slots)
                Inventory.TryAdd(id, count);
        }
    }
}
