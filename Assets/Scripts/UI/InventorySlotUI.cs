using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyHarvest.UI
{
    /// <summary>Pointer/drag target for one inventory-backed slot (panel or hotbar item slot).</summary>
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventoryDragManager? _manager;
        private int _inventoryIndex = -1;

        public void Setup(InventoryDragManager manager, int inventoryIndex)
        {
            _manager        = manager;
            _inventoryIndex = inventoryIndex;
        }

        public void OnPointerClick(PointerEventData eventData) =>
            _manager?.OnSlotClick(_inventoryIndex);

        public void OnBeginDrag(PointerEventData eventData) =>
            _manager?.OnSlotBeginDrag(_inventoryIndex, eventData);

        public void OnDrag(PointerEventData eventData) =>
            _manager?.OnSlotDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) { }

        public void OnDrop(PointerEventData eventData) =>
            _manager?.OnSlotDrop(_inventoryIndex);
    }
}
