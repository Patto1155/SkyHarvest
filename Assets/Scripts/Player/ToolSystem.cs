// Assets/Scripts/Player/ToolSystem.cs
// Owned by: world/island agent
// Keys 1-4 equip Hoe, WateringCan, Sickle, Hammer.
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

        private void Update()
        {
            // 1-4 hotbar — PlayerController also calls EquipBySlot, but
            // we keep fallback input here in case PlayerController is absent.
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    EquipBySlot(i);
                    return;
                }
            }
        }

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
