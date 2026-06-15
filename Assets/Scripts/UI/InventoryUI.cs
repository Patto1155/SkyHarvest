using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public const int GridColumns = 5;
        public const int GridRows    = 4;
        public const int SlotCount   = GridColumns * GridRows;

        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private PlayerInventoryComponent? _playerInv;
        private Text[]? _slotCounts;
        private Image[]? _slotIcons;
        private Image[]? _slotBgs;
        private GameObject[]? _slotGOs;

        private static readonly Color SlotBg       = new Color(0.58f, 0.50f, 0.42f, 1f);

        public void Initialize(GameObject panel, PlayerInventoryComponent inv)
        {
            _panel     = panel;
            _playerInv = inv;
            _panel.SetActive(false);
            EventBus.Subscribe<InventoryChangedEvent>(_ => { if (IsOpen) Refresh(); });
        }

        private void OnDestroy() =>
            EventBus.Unsubscribe<InventoryChangedEvent>(_ => { });

        public void SetSlots(GameObject[] slotGOs, Image[] bgs, Image[] icons, Text[] counts)
        {
            _slotGOs    = slotGOs;
            _slotBgs    = bgs;
            _slotIcons  = icons;
            _slotCounts = counts;
        }

        public GameObject[]? SlotGameObjects => _slotGOs;

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            IsOpen = true;
            _panel?.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            _panel?.SetActive(false);
        }

        public void RefreshIfOpen() { if (IsOpen) Refresh(); }

        public void Refresh()
        {
            if (_playerInv == null || _slotCounts == null) return;
            var slots = _playerInv.Inventory.Slots;
            for (int i = 0; i < _slotCounts.Length && i < slots.Length; i++)
            {
                bool empty = slots[i].IsEmpty;

                if (_slotBgs != null && i < _slotBgs.Length && _slotBgs[i] != null)
                    _slotBgs[i].color = SlotBg;

                if (_slotCounts[i] != null)
                    _slotCounts[i].text = empty || slots[i].Count <= 1 ? "" : slots[i].Count.ToString();

                if (_slotIcons != null && i < _slotIcons.Length && _slotIcons[i] != null)
                {
                    if (empty)
                    {
                        _slotIcons[i].enabled = false;
                        _slotIcons[i].sprite  = null;
                    }
                    else
                    {
                        var spr = SpriteLoader.Load($"Sprites/items/icon_{slots[i].ItemId}");
                        _slotIcons[i].sprite  = spr;
                        _slotIcons[i].enabled = spr != null;
                    }
                }
            }
        }

        public static string ItemDisplayName(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            var def = GameDatabase.GetItem(itemId);
            return def?.DisplayName ?? itemId;
        }
    }
}
