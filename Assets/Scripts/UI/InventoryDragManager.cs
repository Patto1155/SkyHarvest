using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    /// <summary>
    /// Terraria-style inventory cursor: pick up stacks, drag a ghost icon, place/swap/merge.
    /// Works on inventory-panel slots and hotbar item slots (which map onto inventory indices).
    /// </summary>
    public class InventoryDragManager : MonoBehaviour
    {
        private Inventory? _inventory;
        private Hotbar? _hotbar;
        private InventoryUI? _inventoryUI;
        private HUDController? _hud;

        private Image? _ghostIcon;
        private Text? _ghostCount;
        private Canvas? _canvas;

        private int _heldFromIndex = -1;
        private string? _heldItemId;
        private int _heldCount;
        private bool _suppressClick;

        public bool IsHolding => !string.IsNullOrEmpty(_heldItemId) && _heldCount > 0;

        public void Initialize(Inventory inventory, Hotbar hotbar, InventoryUI inventoryUI,
                               HUDController hud, Canvas canvas)
        {
            _inventory   = inventory;
            _hotbar      = hotbar;
            _inventoryUI = inventoryUI;
            _hud         = hud;
            _canvas      = canvas;
            BuildGhost();
            EventBus.Subscribe<InventoryChangedEvent>(_ => RefreshDisplays());
        }

        private void OnDestroy() =>
            EventBus.Unsubscribe<InventoryChangedEvent>(_ => RefreshDisplays());

        private void BuildGhost()
        {
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40f, 40f);
            var icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.enabled = false;
            _ghostIcon = icon;

            var lblGO = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchoredPosition = new Vector2(14f, -14f);
            lblRT.sizeDelta        = new Vector2(28f, 16f);
            var lbl = lblGO.GetComponent<Text>();
            lbl.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize  = 11;
            lbl.color     = Color.white;
            lbl.alignment = TextAnchor.LowerRight;
            lbl.raycastTarget = false;
            _ghostCount = lbl;

            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        public void RegisterSlot(GameObject slotGO, int inventoryIndex)
        {
            var slot = slotGO.GetComponent<InventorySlotUI>();
            if (slot == null) slot = slotGO.AddComponent<InventorySlotUI>();
            slot.Setup(this, inventoryIndex);
        }

        /// <summary>Single click — pick up or place (Terraria).</summary>
        public void OnSlotClick(int inventoryIndex)
        {
            if (_suppressClick)
            {
                _suppressClick = false;
                return;
            }
            if (_inventory == null) return;

            if (!IsHolding)
            {
                TryPickUp(inventoryIndex);
                return;
            }

            if (inventoryIndex == _heldFromIndex)
                ReturnHeldToSource();
            else
                PlaceHeldOn(inventoryIndex);
        }

        /// <summary>Drag start — pick up if needed and show ghost.</summary>
        public void OnSlotBeginDrag(int inventoryIndex, PointerEventData eventData)
        {
            if (_inventory == null) return;
            _suppressClick = true;

            if (!IsHolding)
                TryPickUp(inventoryIndex);

            if (IsHolding)
                ShowGhost(eventData.position, _heldItemId!, _heldCount);
        }

        public void OnSlotDrag(PointerEventData eventData)
        {
            if (!IsHolding || _ghostIcon == null) return;
            UpdateGhostPosition(eventData.position);
        }

        public void OnSlotDrop(int inventoryIndex)
        {
            if (!IsHolding) return;
            if (inventoryIndex == _heldFromIndex)
                ReturnHeldToSource();
            else
                PlaceHeldOn(inventoryIndex);
        }

        public void CancelHold()
        {
            if (!IsHolding) return;
            ReturnHeldToSource();
        }

        private void TryPickUp(int inventoryIndex)
        {
            if (_inventory == null) return;
            var (id, count) = _inventory.TakeFromSlot(inventoryIndex);
            if (string.IsNullOrEmpty(id) || count <= 0) return;
            _heldFromIndex = inventoryIndex;
            _heldItemId    = id;
            _heldCount     = count;
            ShowGhost(Input.mousePosition, id, count);
            RefreshDisplays();
        }

        private void PlaceHeldOn(int inventoryIndex)
        {
            if (_inventory == null || !IsHolding) return;

            var (swapId, swapCount) = _inventory.PlaceOnSlot(inventoryIndex, _heldItemId!, _heldCount);
            if (string.IsNullOrEmpty(swapId) || swapCount <= 0)
                ClearHeld();
            else
            {
                _heldFromIndex = inventoryIndex;
                _heldItemId    = swapId;
                _heldCount     = swapCount;
                if (_ghostIcon != null && _ghostIcon.enabled)
                    _ghostIcon.sprite = SpriteLoader.Load($"Sprites/items/icon_{swapId}");
                if (_ghostCount != null)
                    _ghostCount.text = swapCount > 1 ? swapCount.ToString() : "";
            }
            RefreshDisplays();
        }

        private void ReturnHeldToSource()
        {
            if (_inventory == null || !IsHolding) return;

            if (_heldFromIndex >= 0)
            {
                var (swapId, swapCount) = _inventory.PlaceOnSlot(_heldFromIndex, _heldItemId!, _heldCount);
                // If source was filled while we held (shouldn't happen), carry the displaced stack.
                if (!string.IsNullOrEmpty(swapId) && swapCount > 0)
                {
                    _heldItemId = swapId;
                    _heldCount  = swapCount;
                    RefreshDisplays();
                    return;
                }
            }
            ClearHeld();
            RefreshDisplays();
        }

        private void ClearHeld()
        {
            _heldFromIndex = -1;
            _heldItemId    = null;
            _heldCount     = 0;
            HideGhost();
        }

        private void ShowGhost(Vector2 screenPos, string itemId, int count)
        {
            if (_ghostIcon == null) return;
            _ghostIcon.transform.parent.gameObject.SetActive(true);
            _ghostIcon.sprite  = SpriteLoader.Load($"Sprites/items/icon_{itemId}");
            _ghostIcon.enabled = _ghostIcon.sprite != null;
            if (_ghostCount != null)
                _ghostCount.text = count > 1 ? count.ToString() : "";
            UpdateGhostPosition(screenPos);
        }

        private void HideGhost()
        {
            if (_ghostIcon != null)
                _ghostIcon.transform.parent.gameObject.SetActive(false);
        }

        private void UpdateGhostPosition(Vector2 screenPos)
        {
            if (_ghostIcon == null || _canvas == null) return;
            var rt = _ghostIcon.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screenPos,
                _canvas.worldCamera, out var local);
            rt.anchoredPosition = local;
        }

        private void Update()
        {
            if (!IsHolding) return;

            // Right-click or Escape returns the held stack (Terraria cancel).
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelHold();

            if (_ghostIcon != null && _ghostIcon.transform.parent.gameObject.activeSelf)
                UpdateGhostPosition(Input.mousePosition);
        }

        private void RefreshDisplays()
        {
            _inventoryUI?.RefreshIfOpen();
            _hud?.RefreshHotbarPublic();
        }
    }
}
