// Hotbar = inventory slots 0-9 (same array as backpack; Minecraft layout).
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Player
{
    public class HotbarModel
    {
        private readonly Inventory _inventory;

        public int SlotCount => PlayerInventoryComponent.HotbarSlots;
        public int SelectedIndex { get; private set; }

        public HotbarModel(Inventory inventory)
        {
            _inventory    = inventory;
            SelectedIndex = 0;
        }

        public string? ItemIdAt(int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= SlotCount) return null;
            var slot = _inventory.Slots[hotbarIndex];
            return slot.IsEmpty ? null : slot.ItemId;
        }

        public int CountAt(int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= SlotCount) return 0;
            var slot = _inventory.Slots[hotbarIndex];
            return slot.IsEmpty ? 0 : slot.Count;
        }

        public string? HeldItemId => ItemIdAt(SelectedIndex);
        public ToolType SelectedTool => ToolItems.GetToolType(HeldItemId);
        public bool IsToolSelected => SelectedTool != ToolType.None;

        public bool Select(int index)
        {
            if (index < 0 || index >= SlotCount) return false;
            if (index == SelectedIndex) return false;
            SelectedIndex = index;
            return true;
        }

        public bool TryConsumeHeldItem(int count = 1)
        {
            if (ToolItems.IsTool(HeldItemId)) return false;
            if (SelectedIndex < 0 || SelectedIndex >= SlotCount) return false;

            var slot = _inventory.Slots[SelectedIndex];
            if (slot.IsEmpty || slot.Count < count) return false;

            slot.Count -= count;
            if (slot.Count <= 0)
            {
                slot.ItemId = null;
                slot.Count  = 0;
            }
            EventBus.Publish(new InventoryChangedEvent());
            return true;
        }
    }

    [RequireComponent(typeof(PlayerInventoryComponent))]
    public class Hotbar : MonoBehaviour
    {
        public HotbarModel Model { get; private set; } = null!;

        private ToolSystem _tools = null!;
        private PlayerInventoryComponent _inv = null!;

        private static KeyCode KeyForSlot(int index) =>
            index < 9 ? KeyCode.Alpha1 + index : KeyCode.Alpha0;

        public int SelectedIndex => Model.SelectedIndex;
        public string? HeldItemId => Model.HeldItemId;

        private void Awake()
        {
            _tools = GetComponent<ToolSystem>();
            _inv   = GetComponent<PlayerInventoryComponent>();
            Model  = new HotbarModel(_inv.Inventory);
            SeedDefaultToolsIfEmpty();
            ApplySelection();
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnDestroy() =>
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);

        private void SeedDefaultToolsIfEmpty()
        {
            for (int i = 0; i < PlayerInventoryComponent.HotbarSlots; i++)
                if (!_inv.Inventory.Slots[i].IsEmpty) return;

            for (int i = 0; i < ToolItems.DefaultLoadout.Length && i < PlayerInventoryComponent.HotbarSlots; i++)
            {
                _inv.Inventory.Slots[i].ItemId = ToolItems.DefaultLoadout[i];
                _inv.Inventory.Slots[i].Count  = 1;
            }
        }

        private void Update()
        {
            if (StairCutoutEditor.BlocksGameplayInput) return;

            int n = Model.SlotCount;
            for (int i = 0; i < n; i++)
            {
                if (Input.GetKeyDown(KeyForSlot(i)))
                {
                    SelectSlot(i);
                    return;
                }
            }

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrl && n > 0)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0f)
                {
                    int dir  = scroll < 0f ? 1 : -1;
                    int next = ((Model.SelectedIndex + dir) % n + n) % n;
                    SelectSlot(next);
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (!Model.Select(index)) return;
            ApplySelection();
            EventBus.Publish(new HotbarSelectionChangedEvent { SlotIndex = index });
        }

        public bool TryConsumeHeldItem(int count = 1) => Model.TryConsumeHeldItem(count);

        private void ApplySelection() => _tools.EquipFromItemId(Model.HeldItemId);

        private void OnInventoryChanged(InventoryChangedEvent _)
        {
            ApplySelection();
            EventBus.Publish(new HotbarSelectionChangedEvent { SlotIndex = Model.SelectedIndex });
        }
    }
}
