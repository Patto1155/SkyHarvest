namespace SkyHarvest.Core
{
    /// <summary>Maps item IDs to Resources sprite paths (tools use ui/ icons).</summary>
    public static class ItemIconPaths
    {
        public static string For(string itemId) => itemId switch
        {
            Player.ToolItems.Hoe         => "Sprites/ui/icon_tool_hoe",
            Player.ToolItems.WateringCan => "Sprites/ui/icon_tool_wateringcan",
            Player.ToolItems.Sickle      => "Sprites/ui/icon_tool_sickle",
            Player.ToolItems.Hammer      => "Sprites/ui/icon_tool_hammer",
            _                            => $"Sprites/items/icon_{itemId}"
        };
    }
}
