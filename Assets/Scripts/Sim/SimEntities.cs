// Pure-C# views of the island that IslandSim operates on. The Unity layer builds these from
// scene objects (SimBridge) and shares the underlying state objects (SoilState, CropState,
// Inventory, WorkshopProcess) so the sim mutates the live game directly.
using UnityEngine;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Workshop;

namespace SkyHarvest.Sim
{
    public sealed class SimPlot
    {
        public Vector2Int GridPos;
        public SoilState Soil = null!;
        public CropState? Crop;
        /// <summary>Crop last planted here — Tender's Posts prefer replanting the same thing.</summary>
        public string? LastCropId;
        /// <summary>Opaque back-reference for the Unity bridge (the CropPlot MonoBehaviour).</summary>
        public object? Tag;
    }

    public sealed class SimStorage
    {
        public Vector2Int GridPos;
        public Inventory Inventory = null!;
    }

    public sealed class SimWorkshop
    {
        public Vector2Int GridPos;
        public WorkshopProcess Process = null!;
        public object? Tag;
    }

    /// <summary>Per-step weather inputs. Offline is deliberately neutral (pillar 1: never punish absence).</summary>
    public sealed class SimEnvironment
    {
        public float SunExposure        = 1f;
        public float RainWaterPerMinute = 0f;
        public float WindDamage         = 0f;

        public static SimEnvironment Offline => new() { SunExposure = 0.75f };
    }
}
