using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Farming;
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
        private InventoryDragManager? _dragManager;
        private Text[]? _slotCounts;
        private Image[]? _slotIcons;
        private Image[]? _slotBgs;
        private GameObject[]? _slotGOs;

        private static readonly Color SlotBg       = new Color(0.58f, 0.50f, 0.42f, 1f);

        public void Initialize(GameObject panel, PlayerInventoryComponent inv,
                               InventoryDragManager? dragManager = null)
        {
            _panel       = panel;
            _playerInv   = inv;
            _dragManager = dragManager;
            _panel.SetActive(false);
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnInventoryChanged(InventoryChangedEvent _) { if (IsOpen) Refresh(); }

        private void OnDestroy() =>
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);

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
            _dragManager?.OnInventoryClosed();
        }

        public void RefreshIfOpen() { if (IsOpen) Refresh(); }

        public void Refresh()
        {
            if (_playerInv == null || _slotCounts == null) return;
            var slots = _playerInv.Inventory.Slots;
            for (int i = 0; i < _slotCounts.Length; i++)
            {
                int invIdx = PlayerInventoryComponent.HotbarSlots + i;
                if (invIdx >= slots.Length) break;
                bool empty = slots[invIdx].IsEmpty;

                if (_slotBgs != null && i < _slotBgs.Length && _slotBgs[i] != null)
                    _slotBgs[i].color = SlotBg;

                if (_slotCounts[i] != null)
                    _slotCounts[i].text = empty || slots[invIdx].Count <= 1 ? "" : slots[invIdx].Count.ToString();

                if (_slotIcons != null && i < _slotIcons.Length && _slotIcons[i] != null)
                {
                    if (empty)
                    {
                        _slotIcons[i].enabled = false;
                        _slotIcons[i].sprite  = null;
                    }
                    else
                    {
                        var spr = SpriteLoader.Load(ItemIconPaths.For(slots[invIdx].ItemId));
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
