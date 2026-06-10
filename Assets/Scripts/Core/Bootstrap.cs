using UnityEngine;

namespace SkyHarvest.Core
{
    /// <summary>
    /// Single scene entry point. Constructs the entire game at runtime:
    /// managers, camera, lighting, UI, and the main menu overlay.
    /// The only component referenced by Main.unity.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Filled in by the integration pass: creates GameManager, InputManager-less
            // (legacy input), WeatherManager, CropGrowthSystem, StructureRegistry,
            // BuildModeController, DebrisSpawner, SaveManager, UI root, camera, menu.
            Debug.Log("SkyHarvest Bootstrap awake");
        }
    }
}
