// Minecraft-style player storage: slots 0-9 = hotbar, 10+ = backpack (one array).
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.SaveLoad;

namespace SkyHarvest.Player
{
    public class PlayerInventoryComponent : MonoBehaviour
    {
        public const int HotbarSlots    = 10;
        public const int BackpackSlots  = 20;
        public const int TotalSlots     = HotbarSlots + BackpackSlots;

        public Inventory Inventory { get; private set; } = null!;

        public static bool IsHotbarIndex(int index) => index >= 0 && index < HotbarSlots;
        public static bool IsBackpackIndex(int index) => index >= HotbarSlots && index < TotalSlots;

        private void Awake()
        {
            Inventory = new Inventory(TotalSlots);
            AddToBackpack("sky_moss_seed",   4);
            AddToBackpack("cloud_root_seed", 3);
            AddToBackpack("wheat_seed",      2);
            AddToBackpack("herb_seed",       2);
            AddToBackpack("wood",            4);
            AddToBackpack("scrap",           2);
        }

        private void AddToBackpack(string itemId, int count)
        {
            for (int i = HotbarSlots; i < TotalSlots; i++)
            {
                if (!Inventory.Slots[i].IsEmpty) continue;
                Inventory.Slots[i].ItemId = itemId;
                Inventory.Slots[i].Count  = count;
                return;
            }
        }

        public void RestoreInventory(System.Collections.Generic.IEnumerable<(string itemId, int count)> slots,
                                     int firstIndex = 0)
        {
            Inventory = new Inventory(TotalSlots);
            int idx = firstIndex;
            foreach (var (id, count) in slots)
            {
                if (string.IsNullOrEmpty(id) || count <= 0) continue;
                while (idx < TotalSlots && !Inventory.Slots[idx].IsEmpty) idx++;
                if (idx >= TotalSlots) break;
                Inventory.Slots[idx].ItemId = id;
                Inventory.Slots[idx].Count  = count;
                idx++;
            }
            EventBus.Publish(new InventoryChangedEvent());
        }

        public void RestoreFromSave(SaveLoad.PlayerSaveData player)
        {
            Inventory = new Inventory(TotalSlots);

            bool legacy = player.HotbarSlots.Count > 0;
            if (legacy)
            {
                int hi = 0;
                foreach (var slot in player.HotbarSlots)
                {
                    if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0) continue;
                    if (hi >= HotbarSlots) break;
                    Inventory.Slots[hi].ItemId = slot.ItemId;
                    Inventory.Slots[hi].Count  = slot.Count;
                    hi++;
                }

                int bi = HotbarSlots;
                foreach (var slot in player.InventorySlots)
                {
                    if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0) continue;
                    while (bi < TotalSlots && !Inventory.Slots[bi].IsEmpty) bi++;
                    if (bi >= TotalSlots) break;
                    Inventory.Slots[bi].ItemId = slot.ItemId;
                    Inventory.Slots[bi].Count  = slot.Count;
                    bi++;
                }
            }
            else
            {
                foreach (var slot in player.InventorySlots)
                {
                    if (string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0) continue;
                    if (slot.SlotIndex < 0 || slot.SlotIndex >= TotalSlots) continue;
                    Inventory.Slots[slot.SlotIndex].ItemId = slot.ItemId;
                    Inventory.Slots[slot.SlotIndex].Count  = slot.Count;
                }
            }

            EventBus.Publish(new InventoryChangedEvent());
        }
    }
}
