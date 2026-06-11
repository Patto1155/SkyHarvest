using System.Collections.Generic;
using System.Linq;

namespace SkyHarvest.Player
{
    public class InventorySlot
    {
        public string ItemId;
        public int Count;
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    }

    public class Inventory
    {
        public InventorySlot[] Slots { get; }

        public Inventory(int slotCount)
        {
            Slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++)
                Slots[i] = new InventorySlot();
        }

        public bool TryAdd(string itemId, int count)
        {
            var existing = Slots.FirstOrDefault(s => s.ItemId == itemId && !s.IsEmpty);
            if (existing != null)
            {
                existing.Count += count;
                Core.EventBus.Publish(new Core.InventoryChangedEvent());
                return true;
            }

            var empty = Slots.FirstOrDefault(s => s.IsEmpty);
            if (empty == null) return false;

            empty.ItemId = itemId;
            empty.Count = count;
            Core.EventBus.Publish(new Core.InventoryChangedEvent());
            return true;
        }

        public bool TryRemove(string itemId, int count)
        {
            if (!Has(itemId, count)) return false;

            int remaining = count;
            foreach (var slot in Slots.Where(s => s.ItemId == itemId))
            {
                int take = System.Math.Min(slot.Count, remaining);
                slot.Count -= take;
                remaining -= take;
                if (slot.Count <= 0)
                {
                    slot.ItemId = null;
                    slot.Count = 0;
                }
                if (remaining <= 0) break;
            }
            Core.EventBus.Publish(new Core.InventoryChangedEvent());
            return true;
        }

        public bool Has(string itemId, int count = 1) => GetCount(itemId) >= count;

        public int GetCount(string itemId) =>
            Slots.Where(s => s.ItemId == itemId).Sum(s => s.Count);
    }
}
