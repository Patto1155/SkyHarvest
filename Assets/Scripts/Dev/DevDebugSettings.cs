// Shared toggles for the dev debug overlay (F3 panel, --dev only).
using UnityEngine;

namespace SkyHarvest.DevTools
{
    public static class DevDebugSettings
    {
        public static bool PanelOpen;
        public static bool ShowDiamondBounds = true;
        public static bool ShowTerrainFill   = true;
        public static bool ShowCellLabels;
        public static bool ShowPlayerProbe   = true;
        public static bool ShowHoverProbe    = true;
        public static bool ShowStairCorridor  = true;
        public static bool ShowBothTiers     = true;
    }
}
