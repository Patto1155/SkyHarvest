using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyHarvest.UI
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryDragManager? _manager;
        private int _inventoryIndex;

        public void Setup(InventoryDragManager manager, int inventoryIndex)
        {
            _manager        = manager;
            _inventoryIndex = inventoryIndex;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            _manager?.OnSlotHoverEnter(_inventoryIndex);

        public void OnPointerExit(PointerEventData eventData) =>
            _manager?.OnSlotHoverExit(_inventoryIndex);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift)
                _manager?.OnSlotShiftClick(_inventoryIndex);
            else
                _manager?.OnSlotClick(_inventoryIndex);
        }
    }
}
