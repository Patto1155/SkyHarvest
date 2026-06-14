// Assets/Scripts/Player/Hotbar.cs
// Owned by: world/island agent
// Stardew-style unified hotbar: tools AND inventory items share one bar with a
// single selection cursor driven by the number keys.
//
// Layout (unified index): [tool slots 0..ToolCount-1][item slots, a view onto the
// first ItemSlots inventory stacks]. Selecting a tool slot equips that tool;
// selecting an item slot equips nothing and exposes HeldItemId (e.g. a seed to sow).
//
// The pure HotbarModel below holds the testable selection logic; the Hotbar
// MonoBehaviour wraps it, reads number-key input, and keeps ToolSystem in sync.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    // =========================================================================
    // Pure, testable selection model (no Unity input / lifecycle).
    // =========================================================================
    public class HotbarModel
    {
        public static readonly ToolType[] DefaultTools =
        {
            ToolType.Hoe, ToolType.WateringCan, ToolType.Sickle, ToolType.Hammer
        };

        private readonly ToolType[] _tools;
        private readonly Inventory _inventory;

        public int ItemSlots { get; }
        public int ToolCount => _tools.Length;
        public int SlotCount => ToolCount + ItemSlots;
        public int SelectedIndex { get; private set; }

        public HotbarModel(Inventory inventory, int itemSlots = 6, ToolType[]? tools = null)
        {
            _inventory = inventory;
            _tools     = tools ?? DefaultTools;
            ItemSlots  = Mathf.Max(0, itemSlots);
            SelectedIndex = 0;
        }

        public bool IsToolSlot(int index) => index >= 0 && index < ToolCount;
        public bool IsItemSlot(int index) => index >= ToolCount && index < SlotCount;

        public bool IsToolSelected => IsToolSlot(SelectedIndex);

        /// <summary>The tool a given unified slot maps to, or None for item slots.</summary>
        public ToolType ToolAt(int index) => IsToolSlot(index) ? _tools[index] : ToolType.None;

        public ToolType SelectedTool => ToolAt(SelectedIndex);

        /// <summary>The inventory slot a unified index maps to, or -1 if it is a tool slot.</summary>
        public int InventoryIndexFor(int index) => IsItemSlot(index) ? index - ToolCount : -1;

        /// <summary>The item shown in a unified slot (null for tool/empty slots).</summary>
        public string? ItemIdAt(int index)
        {
            int inv = InventoryIndexFor(index);
            if (inv < 0 || inv >= _inventory.Slots.Length) return null;
            var slot = _inventory.Slots[inv];
            return slot.IsEmpty ? null : slot.ItemId;
        }

        /// <summary>The stack count shown in a unified slot (0 for tool/empty slots).</summary>
        public int CountAt(int index)
        {
            int inv = InventoryIndexFor(index);
            if (inv < 0 || inv >= _inventory.Slots.Length) return 0;
            var slot = _inventory.Slots[inv];
            return slot.IsEmpty ? 0 : slot.Count;
        }

        /// <summary>The item currently held (null when a tool is selected or the slot is empty).</summary>
        public string? HeldItemId => ItemIdAt(SelectedIndex);

        /// <summary>Move the cursor. Returns true when the selection actually changed.</summary>
        public bool Select(int index)
        {
            if (index < 0 || index >= SlotCount) return false;
            if (index == SelectedIndex) return false;
            SelectedIndex = index;
            return true;
        }
    }

    // =========================================================================
    // MonoBehaviour wrapper — input + ToolSystem sync.
    // =========================================================================
    [RequireComponent(typeof(ToolSystem))]
    [RequireComponent(typeof(PlayerInventoryComponent))]
    public class Hotbar : MonoBehaviour
    {
        [SerializeField] private int _itemSlots = 6;

        public HotbarModel Model { get; private set; } = null!;

        private ToolSystem _tools = null!;
        private PlayerInventoryComponent _inv = null!;

        // Maps a unified slot index to its number key. Slots 0-8 → keys 1-9, slot 9 → key 0.
        private static KeyCode KeyForSlot(int index) =>
            index < 9 ? KeyCode.Alpha1 + index : KeyCode.Alpha0;

        public int SelectedIndex => Model.SelectedIndex;
        public string? HeldItemId => Model.HeldItemId;

        private void Awake()
        {
            _tools = GetComponent<ToolSystem>();
            _inv   = GetComponent<PlayerInventoryComponent>();
            Model  = new HotbarModel(_inv.Inventory, _itemSlots);
            ApplySelection();   // start on slot 0 (Hoe) with ToolSystem in sync
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnDestroy() => EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);

        private void Update()
        {
            int n = Mathf.Min(Model.SlotCount, 10);   // keys 1-9 then 0
            for (int i = 0; i < n; i++)
            {
                if (Input.GetKeyDown(KeyForSlot(i)))
                {
                    SelectSlot(i);
                    return;
                }
            }

            // Mouse wheel cycles the hotbar (Minecraft/Terraria style); wheel down → next slot.
            // Ctrl+scroll is reserved for camera zoom (CameraFollow), so ignore it here.
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!ctrl && Model.SlotCount > 0)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0f)
                {
                    int count = Model.SlotCount;
                    int dir   = scroll < 0f ? 1 : -1;
                    int next  = ((Model.SelectedIndex + dir) % count + count) % count;
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

        private void ApplySelection()
        {
            if (Model.IsToolSelected) _tools.EquipBySlot(Model.SelectedIndex);
            else                      _tools.Unequip();
        }

        // When a held stack runs out, keep the cursor (Stardew behaviour) but
        // republish so the HUD drops the now-empty icon/highlight.
        private void OnInventoryChanged(InventoryChangedEvent _) =>
            EventBus.Publish(new HotbarSelectionChangedEvent { SlotIndex = Model.SelectedIndex });
    }
}
