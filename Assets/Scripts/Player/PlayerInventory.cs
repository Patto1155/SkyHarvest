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

        /// <summary>Lift the full stack out of a slot (Terraria pick-up).</summary>
        public (string? itemId, int count) TakeFromSlot(int index)
        {
            if (index < 0 || index >= Slots.Length) return (null, 0);
            var slot = Slots[index];
            if (slot.IsEmpty) return (null, 0);

            var itemId = slot.ItemId;
            var count  = slot.Count;
            slot.ItemId = null;
            slot.Count  = 0;
            Core.EventBus.Publish(new Core.InventoryChangedEvent());
            return (itemId, count);
        }

        /// <summary>
        /// Place a held stack onto a slot. Merges same items; swaps when different.
        /// Returns the stack displaced by a swap (null/0 when placement finished).
        /// </summary>
        public (string? itemId, int count) PlaceOnSlot(int index, string itemId, int count)
        {
            if (index < 0 || index >= Slots.Length || string.IsNullOrEmpty(itemId) || count <= 0)
                return (itemId, count);

            var dst = Slots[index];
            if (dst.IsEmpty)
            {
                dst.ItemId = itemId;
                dst.Count  = count;
                Core.EventBus.Publish(new Core.InventoryChangedEvent());
                return (null, 0);
            }

            if (dst.ItemId == itemId)
            {
                dst.Count += count;
                Core.EventBus.Publish(new Core.InventoryChangedEvent());
                return (null, 0);
            }

            var swapId    = dst.ItemId;
            var swapCount = dst.Count;
            dst.ItemId = itemId;
            dst.Count  = count;
            Core.EventBus.Publish(new Core.InventoryChangedEvent());
            return (swapId, swapCount);
        }

        /// <summary>Move or swap stacks between two slots without using a cursor.</summary>
        public void SwapSlots(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= Slots.Length || to >= Slots.Length)
                return;

            var a = Slots[from];
            var b = Slots[to];

            if (a.IsEmpty && b.IsEmpty) return;

            if (a.IsEmpty)
            {
                a.ItemId = b.ItemId; a.Count = b.Count;
                b.ItemId = null;     b.Count = 0;
            }
            else if (b.IsEmpty)
            {
                b.ItemId = a.ItemId; b.Count = a.Count;
                a.ItemId = null;     a.Count = 0;
            }
            else if (a.ItemId == b.ItemId)
            {
                b.Count += a.Count;
                a.ItemId = null;
                a.Count  = 0;
            }
            else
            {
                (a.ItemId, b.ItemId) = (b.ItemId, a.ItemId);
                (a.Count,  b.Count)  = (b.Count,  a.Count);
            }

            Core.EventBus.Publish(new Core.InventoryChangedEvent());
        }
    }
}
