using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Player;
using SkyHarvest.Storage;

namespace SkyHarvest.UI
{
    public class StorageUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private GameObject? _panel;
        private StorageContainer? _container;
        private PlayerInventoryComponent? _playerInv;
        private Text[]? _playerLabels;
        private Text[]? _storageLabels;

        public void Initialize(GameObject panel, PlayerInventoryComponent inv)
        {
            _panel = panel;
            _playerInv = inv;
            _panel.SetActive(false);
            EventBus.Subscribe<InventoryChangedEvent>(_ => { if (IsOpen) Refresh(); });
        }

        public void SetDisplays(Text[] playerLabels, Text[] storageLabels)
        {
            _playerLabels  = playerLabels;
            _storageLabels = storageLabels;
        }

        public void Open(StorageContainer container)
        {
            _container = container;
            IsOpen = true;
            _panel?.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            _container = null;
            _panel?.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Tab)) Close();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void Refresh()
        {
            RefreshSide(_playerLabels,  _playerInv?.Inventory);
            RefreshSide(_storageLabels, _container?.Storage);
        }

        private static void RefreshSide(Text[]? labels, Player.Inventory? inv)
        {
            if (labels == null || inv == null) return;
            var slots = inv.Slots;
            for (int i = 0; i < labels.Length && i < slots.Length; i++)
                labels[i].text = slots[i].IsEmpty ? "—" : $"{slots[i].ItemId} ×{slots[i].Count}";
        }
    }
}
