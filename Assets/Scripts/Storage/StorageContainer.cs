// StorageContainer — a placed Structure with an inventory of StructureDef.SlotCount.
// Interact opens StorageUI via event. Exposes public Inventory Storage.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.Storage
{
    public class StorageContainer : Building.Structure
    {
        /// <summary>Published when player interacts so StorageUI can open the panel.</summary>
        public struct OpenStorageEvent
        {
            public StorageContainer Container;
            public PlayerController Player;
        }

        public Inventory Storage { get; private set; }

        private void Awake()
        {
            // Default to 10 slots; overridden by Initialize once Def is set
            Storage = new Inventory(10);
        }

        public override void Initialize(Data.StructureDef def, Vector2Int gridPos)
        {
            base.Initialize(def, gridPos);

            // Use slot count from StructureDef (crate = 10, barrel = 8)
            int slots = def?.SlotCount > 0 ? def.SlotCount : 10;
            Storage = new Inventory(slots);
        }

        public override string InteractionPrompt => "Open " + (Def?.DisplayName ?? "Storage");

        public override void Interact(PlayerController player)
        {
            if (TryDemolishWithHammer(player)) return;
            EventBus.Publish(new OpenStorageEvent { Container = this, Player = player });
        }

        /// <summary>Restore stored items after save load.</summary>
        public void RestoreFromSave(System.Collections.Generic.IEnumerable<(string itemId, int count)> slots)
        {
            foreach (var slot in Storage.Slots)
            {
                slot.ItemId = null;
                slot.Count = 0;
            }

            if (slots == null) return;
            foreach (var (itemId, count) in slots)
            {
                if (string.IsNullOrEmpty(itemId) || count <= 0) continue;
                Storage.TryAdd(itemId, count);
            }
        }
    }
}
