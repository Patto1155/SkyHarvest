using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    /// <summary>
    /// Minecraft cursor — rearrange any slot while the inventory panel is open only.
    /// Hotbar (indices 0-9) and backpack (10-29) share one inventory array.
    /// </summary>
    public class InventoryDragManager : MonoBehaviour
    {
        private PlayerInventoryComponent? _playerInv;
        private InventoryUI? _inventoryUI;
        private HUDController? _hud;

        private Image? _ghostIcon;
        private Text? _ghostCount;
        private Canvas? _canvas;
        private GameObject? _ghostRoot;

        private CursorStack _cursor;
        private int _pickupIndex = -1;

        public bool IsHolding => !_cursor.IsEmpty;
        public bool IsInventoryOpen => _inventoryUI != null && _inventoryUI.IsOpen;

        public void Initialize(PlayerInventoryComponent playerInv, InventoryUI inventoryUI,
                               HUDController hud, Canvas canvas)
        {
            _playerInv    = playerInv;
            _inventoryUI = inventoryUI;
            _hud         = hud;
            _canvas      = canvas;
            BuildGhost();
        }

        public void RegisterSlot(GameObject slotGO, int inventoryIndex)
        {
            var slot = slotGO.GetComponent<InventorySlotUI>();
            if (slot == null) slot = slotGO.AddComponent<InventorySlotUI>();
            slot.Setup(this, inventoryIndex);
        }

        public void OnSlotClick(int inventoryIndex)
        {
            if (!IsInventoryOpen) return;

            var inv = _playerInv?.Inventory;
            if (inv == null || inventoryIndex < 0 || inventoryIndex >= inv.Slots.Length) return;

            bool wasEmpty = _cursor.IsEmpty;
            _cursor = InventoryCursorModel.ClickSlot(inv, _cursor, inventoryIndex);

            if (wasEmpty && !_cursor.IsEmpty)
                _pickupIndex = inventoryIndex;
            else if (_cursor.IsEmpty)
                _pickupIndex = -1;

            EventBus.Publish(new InventoryChangedEvent());
            SyncGhost();
            RefreshDisplays();
        }

        public void OnSlotShiftClick(int inventoryIndex)
        {
            if (!IsInventoryOpen || IsHolding) return;

            var inv = _playerInv?.Inventory;
            if (inv == null || inventoryIndex < 0 || inventoryIndex >= inv.Slots.Length) return;

            bool moved;
            if (PlayerInventoryComponent.IsHotbarIndex(inventoryIndex))
            {
                moved = inv.TryQuickMove(
                    inventoryIndex,
                    PlayerInventoryComponent.HotbarSlots,
                    PlayerInventoryComponent.TotalSlots);
            }
            else if (PlayerInventoryComponent.IsBackpackIndex(inventoryIndex))
            {
                moved = inv.TryQuickMove(
                    inventoryIndex,
                    0,
                    PlayerInventoryComponent.HotbarSlots);
            }
            else
                return;

            if (moved)
                RefreshDisplays();
        }

        public void OnInventoryClosed() => ReturnCursor();

        public void CancelHold() => ReturnCursor();

        private void ReturnCursor()
        {
            if (_cursor.IsEmpty) return;

            var inv = _playerInv?.Inventory;
            if (inv == null) { _cursor.Clear(); HideGhost(); return; }

            if (_pickupIndex >= 0 && _pickupIndex < inv.Slots.Length)
            {
                var src = inv.Slots[_pickupIndex];
                if (src.IsEmpty)
                {
                    _cursor.WriteTo(src);
                    _cursor.Clear();
                    _pickupIndex = -1;
                    EventBus.Publish(new InventoryChangedEvent());
                    SyncGhost();
                    RefreshDisplays();
                    return;
                }

                if (src.ItemId == _cursor.ItemId)
                {
                    src.Count += _cursor.Count;
                    _cursor.Clear();
                    _pickupIndex = -1;
                    EventBus.Publish(new InventoryChangedEvent());
                    SyncGhost();
                    RefreshDisplays();
                    return;
                }
            }

            for (int i = 0; i < inv.Slots.Length; i++)
            {
                if (!inv.Slots[i].IsEmpty) continue;
                _cursor.WriteTo(inv.Slots[i]);
                _cursor.Clear();
                _pickupIndex = -1;
                EventBus.Publish(new InventoryChangedEvent());
                SyncGhost();
                RefreshDisplays();
                return;
            }

            if (_pickupIndex >= 0)
                _cursor = InventoryCursorModel.ClickSlot(inv, _cursor, _pickupIndex);
            _pickupIndex = -1;
            EventBus.Publish(new InventoryChangedEvent());
            SyncGhost();
            RefreshDisplays();
        }

        private void BuildGhost()
        {
            _ghostRoot = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _ghostRoot.transform.SetParent(_canvas != null ? _canvas.transform : transform, false);
            var rt = _ghostRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 40f);
            var icon = _ghostRoot.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false;
            _ghostIcon = icon;

            var lblGO = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            lblGO.transform.SetParent(_ghostRoot.transform, false);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchoredPosition = new Vector2(14f, -14f);
            lblRT.sizeDelta        = new Vector2(28f, 16f);
            var lbl = lblGO.GetComponent<Text>();
            lbl.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize      = 11;
            lbl.color         = Color.white;
            lbl.alignment     = TextAnchor.LowerRight;
            lbl.raycastTarget = false;
            _ghostCount = lbl;

            _ghostRoot.transform.SetAsLastSibling();
            _ghostRoot.SetActive(false);
        }

        private void SyncGhost()
        {
            if (_cursor.IsEmpty || !IsInventoryOpen) { HideGhost(); return; }

            if (_ghostRoot != null)
            {
                _ghostRoot.SetActive(true);
                _ghostRoot.transform.SetAsLastSibling();
            }
            if (_ghostIcon != null)
            {
                _ghostIcon.sprite  = SpriteLoader.Load(ItemIconPaths.For(_cursor.ItemId!));
                _ghostIcon.enabled = _ghostIcon.sprite != null;
            }
            if (_ghostCount != null)
                _ghostCount.text = _cursor.Count > 1 ? _cursor.Count.ToString() : "";
            UpdateGhostPosition(Input.mousePosition);
        }

        private void HideGhost()
        {
            if (_ghostRoot != null) _ghostRoot.SetActive(false);
        }

        private void UpdateGhostPosition(Vector2 screenPos)
        {
            if (_ghostIcon == null || _canvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPos, null, out var local);
            _ghostIcon.rectTransform.anchoredPosition = local;
        }

        private void Update()
        {
            if (!IsInventoryOpen)
            {
                if (IsHolding) ReturnCursor();
                return;
            }

            if (!_cursor.IsEmpty)
            {
                if (Input.GetMouseButtonDown(1))
                    CancelHold();
                if (_ghostRoot != null && _ghostRoot.activeSelf)
                    UpdateGhostPosition(Input.mousePosition);
            }
        }

        private void RefreshDisplays()
        {
            _inventoryUI?.RefreshIfOpen();
            _hud?.RefreshHotbarPublic();
        }
    }
}
