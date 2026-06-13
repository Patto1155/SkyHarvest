// Assets/Scripts/Player/ToolSystem.cs
// Owned by: world/island agent
// Holds the currently equipped tool (Hoe/WateringCan/Sickle/Hammer).
// Selection is driven by the unified Hotbar (number keys) — see Hotbar.cs.
// Publishes ToolEquippedEvent on every equip change.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    public enum ToolType { None, Hoe, WateringCan, Sickle, Hammer }

    public class ToolSystem : MonoBehaviour
    {
        private static readonly ToolType[] Slots =
        {
            ToolType.Hoe,
            ToolType.WateringCan,
            ToolType.Sickle,
            ToolType.Hammer
        };

        public ToolType EquippedTool { get; private set; } = ToolType.None;

        public void EquipTool(ToolType tool)
        {
            if (EquippedTool == tool) return;
            EquippedTool = tool;
            int slotIdx = System.Array.IndexOf(Slots, tool);
            EventBus.Publish(new ToolEquippedEvent { SlotIndex = slotIdx });
        }

        public void EquipBySlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < Slots.Length)
                EquipTool(Slots[slotIndex]);
        }

        public void Unequip()
        {
            EquippedTool = ToolType.None;
            EventBus.Publish(new ToolEquippedEvent { SlotIndex = -1 });
        }

        public string EquippedToolId => EquippedTool.ToString();

        public void EquipById(string toolId)
        {
            if (System.Enum.TryParse<ToolType>(toolId, out var t))
                EquipTool(t);
        }
    }
}
