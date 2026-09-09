// The island as a pure simulation: crops, soil, storage, workshops and automation devices sharing
// one water network and one power grid. Advance() replays offline time in coarse steps; Step()
// with tickCrops=false runs just the devices during live play (CropGrowthSystem/WorkshopBase
// keep ticking their own state then).
using System;
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Sim
{
    public sealed class IslandSim
    {
        public const float DefaultStepSeconds = Constants.OfflineStepSeconds;
        public const float SpringInflowPerSecond = 0.2f;
        public const float RainCatcherInflowPerSecond = 0.05f;

        public readonly List<SimPlot>          Plots     = new();
        public readonly List<SimStorage>       Storages  = new();
        public readonly List<SimWorkshop>      Workshops = new();
        public readonly List<AutomationDevice> Devices   = new();

        public WaterNetwork Water { get; }
        public PowerGrid    Power { get; }
        public int SpringCells { get; set; }
        /// <summary>Rain catchers are open to the sky, so they trickle in even while away —
        /// the first water income a player can build without reaching the forge.</summary>
        public int RainCatchers { get; set; }
        public System.Random Rng { get; set; } = new System.Random();

        private readonly List<Sprinkler>   _sprinklers = new();
        private readonly List<WindTotem>   _totems     = new();
        private readonly List<TendersPost> _posts      = new();
        private readonly List<Composter>   _composters = new();
        private readonly List<Windmill>    _windmills  = new();

        public IslandSim(WaterNetwork water, PowerGrid power)
        {
            Water = water;
            Power = power;
        }

        /// <summary>Re-derive capacities and typed device lists after Devices changes.</summary>
        public void RebuildDeviceIndex()
        {
            _sprinklers.Clear(); _totems.Clear(); _posts.Clear(); _composters.Clear(); _windmills.Clear();
            float waterCap = WaterNetwork.BaseCapacity;
            float powerCap = PowerGrid.BaseCapacity;

            foreach (var d in Devices)
            {
                switch (d)
                {
                    case Sprinkler s:   _sprinklers.Add(s); break;
                    case WindTotem t:   _totems.Add(t); break;
                    case TendersPost p: _posts.Add(p); break;
                    case Composter c:   _composters.Add(c); break;
                    case Windmill w:    _windmills.Add(w); break;
                    case WaterTank:     waterCap += WaterTank.CapacityBonus; break;
                    case Battery:       powerCap += Battery.CapacityBonus; break;
                }
            }
            Water.SetCapacity(waterCap);
            Power.SetCapacity(powerCap);
        }

        /// <summary>True when a device can change which crop sits on a plot (Tender's Posts).</summary>
        public bool HasCropMutatingDevices => _posts.Count > 0;

        public bool IsWindProtected(Vector2Int pos)
        {
            foreach (var t in _totems) if (t.Covers(pos)) return true;
            return false;
        }

        public float GenerationPerSecond
        {
            get { float g = 0f; foreach (var w in _windmills) g += w.GenerationPerSecond; return g; }
        }

        /// <summary>
        /// One simulation step. Order matters: generate → water → grow → harvest/replant → compost → process.
        /// </summary>
        public void Step(float dtSeconds, SimEnvironment env, bool tickCrops, bool tickWorkshops, OfflineReport report)
        {
            if (dtSeconds <= 0f) return;
            float dtMinutes = dtSeconds / Constants.SecondsPerGameMinute;

            Power.Add(GenerationPerSecond * dtSeconds);
            Water.Add((SpringCells * SpringInflowPerSecond
                       + RainCatchers * RainCatcherInflowPerSecond) * dtSeconds);

            foreach (var s in _sprinklers) s.Step(dtSeconds, Plots, Water);
            if (Water.IsEmpty && _sprinklers.Count > 0) report.MarkWaterEmpty();

            if (tickCrops)
            {
                float rain = env.RainWaterPerMinute * dtMinutes;
                foreach (var plot in Plots)
                {
                    if (plot.Crop == null) continue;
                    bool wasRipe = plot.Crop.IsHarvestable;
                    if (rain > 0f) plot.Soil.AddWater(rain);
                    float wind = env.WindDamage > 0f && IsWindProtected(plot.GridPos) ? 0f : env.WindDamage;
                    plot.Crop.Tick(dtMinutes, plot.Soil, env.SunExposure, wind);
                    if (!wasRipe && plot.Crop.IsHarvestable) report.CropsRipened++;
                }
            }

            foreach (var p in _posts)
                if (!p.Step(dtSeconds, Plots, Storages, _composters, Power, Rng, report))
                    report.MarkPowerEmpty();

            foreach (var c in _composters) c.Step(Plots);

            if (tickWorkshops)
            {
                foreach (var w in Workshops)
                {
                    if (!w.Process.IsProcessing) continue;
                    if (w.Process.Tick(dtSeconds)) report.BatchesCompleted++;
                }
            }

            report.Clock += dtSeconds;
        }

        /// <summary>Replay <paramref name="elapsedSeconds"/> of offline time in coarse steps.</summary>
        public OfflineReport Advance(long elapsedSeconds, bool capped, float stepSeconds = DefaultStepSeconds)
        {
            var report = new OfflineReport { ElapsedSeconds = elapsedSeconds, CappedByOfflineLimit = capped };
            var env = SimEnvironment.Offline;
            var (steps, remainder) = OfflineCatchup.SplitSteps(elapsedSeconds, stepSeconds);
            for (int i = 0; i < steps; i++) Step(stepSeconds, env, tickCrops: true, tickWorkshops: true, report);
            if (remainder > 0f) Step(remainder, env, tickCrops: true, tickWorkshops: true, report);
            return report;
        }
    }
}
