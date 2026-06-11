using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private PlayerInventoryComponent? _playerInv;
        private Text[]? _slotLabels;
        private Image[]? _slotIcons;

        public void Initialize(GameObject panel, PlayerInventoryComponent inv)
        {
            _panel     = panel;
            _playerInv = inv;
            _panel.SetActive(false);
            EventBus.Subscribe<InventoryChangedEvent>(_ => { if (IsOpen) Refresh(); });
        }

        private void OnDestroy() =>
            EventBus.Unsubscribe<InventoryChangedEvent>(_ => { });

        public void SetSlotDisplays(Text[] labels, Image[] icons) { _slotLabels = labels; _slotIcons = icons; }

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

        private void Refresh()
        {
            if (_playerInv == null || _slotLabels == null) return;
            var slots = _playerInv.Inventory.Slots;
            for (int i = 0; i < _slotLabels.Length && i < slots.Length; i++)
            {
                bool empty = slots[i].IsEmpty;
                _slotLabels[i].text = empty ? "" : $"{slots[i].ItemId}\n×{slots[i].Count}";
                if (_slotIcons != null && i < _slotIcons.Length)
                {
                    _slotIcons[i].enabled = !empty;
                    if (!empty)
                        _slotIcons[i].sprite = SpriteLoader.Load($"Sprites/items/icon_{slots[i].ItemId}");
                }
            }
        }
    }
}
