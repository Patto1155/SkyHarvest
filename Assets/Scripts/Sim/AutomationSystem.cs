// Unity-side owner of the island's WaterNetwork/PowerGrid and the live IslandSim view.
// - During play: ticks devices every GameTickEvent (crops/workshops keep their own tickers).
// - On Continue: RunOfflineCatchup() replays the time away and returns the report.
// The IslandSim is rebuilt lazily from scene objects whenever something is placed, removed,
// planted or harvested (MarkDirty), so it always mirrors the live world.
using System;
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Workshop;

namespace SkyHarvest.Sim
{
    public class AutomationSystem : MonoBehaviour
    {
        public static AutomationSystem? Instance { get; private set; }

        public WaterNetwork Water { get; } = new();
        public PowerGrid    Power { get; } = new();

        private IslandSim? _sim;
        private bool _dirty = true;
        private readonly OfflineReport _liveReport = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameTickEvent>(OnTick);
            EventBus.Subscribe<StructurePlacedEvent>(OnDirty);
            EventBus.Subscribe<StructureDemolishedEvent>(OnDirty);
            EventBus.Subscribe<CropPlantedEvent>(OnDirty);
            EventBus.Subscribe<CropHarvestedEvent>(OnDirty);
            EventBus.Subscribe<IslandExpandedEvent>(OnDirty);
            EventBus.Subscribe<GameStartedEvent>(OnDirty);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameTickEvent>(OnTick);
            EventBus.Unsubscribe<StructurePlacedEvent>(OnDirty);
            EventBus.Unsubscribe<StructureDemolishedEvent>(OnDirty);
            EventBus.Unsubscribe<CropPlantedEvent>(OnDirty);
            EventBus.Unsubscribe<CropHarvestedEvent>(OnDirty);
            EventBus.Unsubscribe<IslandExpandedEvent>(OnDirty);
            EventBus.Unsubscribe<GameStartedEvent>(OnDirty);
        }

        private void OnDirty(StructurePlacedEvent _)     => MarkDirty();
        private void OnDirty(StructureDemolishedEvent _) => MarkDirty();
        private void OnDirty(CropPlantedEvent _)         => MarkDirty();
        private void OnDirty(CropHarvestedEvent _)       => MarkDirty();
        private void OnDirty(IslandExpandedEvent _)      => MarkDirty();
        private void OnDirty(GameStartedEvent _)         => MarkDirty();

        public void MarkDirty() => _dirty = true;

        public IslandSim Sim
        {
            get
            {
                if (_sim == null || _dirty) { _sim = SimBridge.Build(Water, Power); _dirty = false; }
                return _sim;
            }
        }

        public bool IsWindProtected(Vector2Int pos) => Sim.IsWindProtected(pos);

        private void OnTick(GameTickEvent e)
        {
            if (GameManager.Instance?.CurrentIsland == null) return;
            var sim = Sim;
            if (sim.Devices.Count == 0 && sim.SpringCells == 0) return;

            float dt = e.DeltaMinutes * Constants.SecondsPerGameMinute;
            sim.Step(dt, SimEnvironment.Offline, tickCrops: false, tickWorkshops: false, _liveReport);
            if (sim.HasCropMutatingDevices) SimBridge.SyncPlotsToScene(sim);
        }

        /// <summary>Replay the time since <paramref name="lastSeenUnix"/>; null when nothing to show.</summary>
        public OfflineReport? RunOfflineCatchup(long lastSeenUnix)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var (elapsed, capped) = OfflineCatchup.ClampElapsed(lastSeenUnix, now, Constants.OfflineCapSeconds);
            if (elapsed <= 0) return null;

            MarkDirty();
            var sim = Sim;
            var report = sim.Advance(elapsed, capped, Constants.OfflineStepSeconds);
            SimBridge.SyncPlotsToScene(sim);
            SimBridge.SyncWorkshopsToScene(sim);
            MarkDirty();
            return report;
        }
    }

    /// <summary>Builds the pure IslandSim from live scene objects and pushes results back.</summary>
    public static class SimBridge
    {
        public static IslandSim Build(WaterNetwork water, PowerGrid power)
        {
            var sim = new IslandSim(water, power);
            var island = GameManager.Instance?.CurrentIsland;

            if (island != null)
                foreach (var cell in island.Cells.Values)
                    if (TerrainProperties.HasWaterSource(cell.Terrain)) sim.SpringCells++;

            foreach (var plot in UnityEngine.Object.FindObjectsOfType<CropPlot>() ?? Array.Empty<CropPlot>())
            {
                if (plot == null || plot.Soil == null) continue;
                sim.Plots.Add(new SimPlot
                {
                    GridPos = plot.GridPos, Soil = plot.Soil, Crop = plot.Crop,
                    LastCropId = plot.LastCropId, Tag = plot
                });
            }

            var registry = StructureRegistry.Instance;
            if (registry != null)
            {
                foreach (var s in registry.AllStructures)
                {
                    switch (s)
                    {
                        case Storage.StorageContainer sc:
                            sim.Storages.Add(new SimStorage { GridPos = sc.GridPosition, Inventory = sc.Storage });
                            break;
                        case AutomationStructure a when a.Device != null:
                            sim.Devices.Add(a.Device);
                            break;
                        case WorkshopBase wb:
                            sim.Workshops.Add(new SimWorkshop { GridPos = wb.GridPosition, Process = wb.Process, Tag = wb });
                            break;
                    }
                }
            }

            sim.RebuildDeviceIndex();
            return sim;
        }

        /// <summary>Write crop changes made by the sim back onto the CropPlot MonoBehaviours.</summary>
        public static void SyncPlotsToScene(IslandSim sim)
        {
            foreach (var p in sim.Plots)
            {
                if (p.Tag is not CropPlot plot || plot == null) continue;
                bool changed = !ReferenceEquals(plot.Crop, p.Crop);
                plot.Crop = p.Crop;
                plot.LastCropId = p.LastCropId;
                if (p.Crop != null) CropGrowthSystem.Instance?.Register(plot);
                else if (changed) CropGrowthSystem.Instance?.Unregister(plot);
                plot.RefreshVisuals();
            }
        }

        public static void SyncWorkshopsToScene(IslandSim sim)
        {
            foreach (var w in sim.Workshops)
                if (w.Tag is WorkshopBase wb && wb != null) wb.RefreshVisualFromState();
        }
    }
}
