// Pure Terraria / Minecraft left-click cursor logic (no Unity, no events).
namespace SkyHarvest.Player
{
    public struct CursorStack
    {
        public string? ItemId;
        public int Count;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        public static CursorStack FromSlot(InventorySlot slot) =>
            slot.IsEmpty ? default : new CursorStack { ItemId = slot.ItemId, Count = slot.Count };

        public void WriteTo(InventorySlot slot)
        {
            slot.ItemId = ItemId;
            slot.Count  = Count;
        }

        public void Clear()
        {
            ItemId = null;
            Count  = 0;
        }
    }

    public static class InventoryCursorModel
    {
        /// <summary>
        /// One left-click on a slot: pick up the full stack, place/swap/merge from cursor.
        /// Mutates inventory slots in place; does not publish events.
        /// </summary>
        public static CursorStack ClickSlot(Inventory inv, CursorStack cursor, int index)
        {
            if (inv == null || index < 0 || index >= inv.Slots.Length)
                return cursor;

            var slot = inv.Slots[index];

            if (cursor.IsEmpty)
            {
                if (slot.IsEmpty) return cursor;
                var picked = CursorStack.FromSlot(slot);
                slot.ItemId = null;
                slot.Count  = 0;
                return picked;
            }

            if (slot.IsEmpty)
            {
                cursor.WriteTo(slot);
                cursor.Clear();
                return cursor;
            }

            if (slot.ItemId == cursor.ItemId)
            {
                slot.Count += cursor.Count;
                cursor.Clear();
                return cursor;
            }

            var displaced = CursorStack.FromSlot(slot);
            cursor.WriteTo(slot);
            return displaced;
        }
    }
}
