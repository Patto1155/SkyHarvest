using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyHarvest.UI
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        private InventoryDragManager? _manager;
        private int _inventoryIndex;

        public void Setup(InventoryDragManager manager, int inventoryIndex)
        {
            _manager        = manager;
            _inventoryIndex = inventoryIndex;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _manager?.OnSlotClick(_inventoryIndex);
        }
    }
}
