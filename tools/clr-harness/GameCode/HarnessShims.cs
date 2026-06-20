// Headless-harness stubs for editor/dev-only types excluded from GameCode compile.
#if SKYHARVEST_HEADLESS
using UnityEngine;
using SkyHarvest.Island;
using SkyHarvest.Player;

namespace SkyHarvest.Island
{
    public class StairCutoutEditor : MonoBehaviour
    {
        public static bool IsEnabled => false;
        public static bool BlocksGameplayInput => false;
        public static void EnableIfRequested() { }
    }
}

namespace SkyHarvest.DevTools
{
    public class DevDebugPanel : MonoBehaviour
    {
        public static bool IsEnabled => false;
        public static void EnableIfRequested() { }
        public void Initialize(IslandData island, PlayerController player, Camera cam) { }
    }
}
#endif
