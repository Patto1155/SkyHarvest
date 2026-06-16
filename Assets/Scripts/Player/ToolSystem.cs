// Selection is driven by the hotbar — equips whichever tool item is selected.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Player
{
    public enum ToolType { None, Hoe, WateringCan, Sickle, Hammer }

    public class ToolSystem : MonoBehaviour
    {
        public ToolType EquippedTool { get; private set; } = ToolType.None;

        public void EquipFromItemId(string? itemId)
        {
            var tool = ToolItems.GetToolType(itemId);
            if (tool == ToolType.None) Unequip();
            else                       EquipTool(tool);
        }

        public void EquipTool(ToolType tool)
        {
            if (EquippedTool == tool) return;
            EquippedTool = tool;
            EventBus.Publish(new ToolEquippedEvent { SlotIndex = (int)tool });
        }

        public void Unequip()
        {
            if (EquippedTool == ToolType.None) return;
            EquippedTool = ToolType.None;
            EventBus.Publish(new ToolEquippedEvent { SlotIndex = -1 });
        }

        public string EquippedToolId => EquippedTool.ToString();

        /// <summary>Save/load — maps legacy ToolType name to equipped state.</summary>
        public void EquipById(string toolId)
        {
            if (System.Enum.TryParse<ToolType>(toolId, out var t) && t != ToolType.None)
                EquipTool(t);
        }
    }
}
