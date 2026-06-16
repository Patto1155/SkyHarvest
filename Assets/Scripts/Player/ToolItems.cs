namespace SkyHarvest.Player
{
    /// <summary>Item IDs for equippable tools (stack size 1, live in hotbar/backpack).</summary>
    public static class ToolItems
    {
        public const string Hoe          = "tool_hoe";
        public const string WateringCan  = "tool_watering_can";
        public const string Sickle       = "tool_sickle";
        public const string Hammer       = "tool_hammer";

        public static ToolType GetToolType(string? itemId) => itemId switch
        {
            Hoe         => ToolType.Hoe,
            WateringCan => ToolType.WateringCan,
            Sickle      => ToolType.Sickle,
            Hammer      => ToolType.Hammer,
            _           => ToolType.None
        };

        public static bool IsTool(string? itemId) => GetToolType(itemId) != ToolType.None;

        public static readonly string[] DefaultLoadout =
        {
            Hoe, WateringCan, Sickle, Hammer
        };
    }
}
