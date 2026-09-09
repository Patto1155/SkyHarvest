// Automation devices — the v2 spec's "one layer per crop need". Pure C#; each device is owned by
// an AutomationStructure in the scene and driven by IslandSim.Step both online and offline.
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Data;

namespace SkyHarvest.Sim
{
    public abstract class AutomationDevice
    {
        public Vector2Int GridPos { get; }
        public abstract AutomationKind Kind { get; }

        /// <summary>Chebyshev radius of effect; 1 = the 3×3 block around the device.</summary>
        public virtual int Radius => 1;

        protected AutomationDevice(Vector2Int gridPos) { GridPos = gridPos; }

        public bool Covers(Vector2Int pos) =>
            System.Math.Abs(pos.x - GridPos.x) <= Radius && System.Math.Abs(pos.y - GridPos.y) <= Radius;

        public bool IsAdjacent(Vector2Int pos) =>
            System.Math.Abs(pos.x - GridPos.x) <= 1 && System.Math.Abs(pos.y - GridPos.y) <= 1 && pos != GridPos;

        /// <summary>Short status line for the inspector / interaction prompt.</summary>
        public virtual string Status => Kind.ToString();
    }

    public sealed class WaterTank : AutomationDevice
    {
        public const float CapacityBonus = 200f;
        public WaterTank(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.WaterTank;
        public override string Status => $"Water Tank (+{CapacityBonus:0})";
    }

    public sealed class Battery : AutomationDevice
    {
        public const float CapacityBonus = 100f;
        public Battery(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.Battery;
        public override string Status => $"Battery (+{CapacityBonus:0})";
    }

    public sealed class Windmill : AutomationDevice
    {
        public const float BaseGenerationPerSecond = 0.2f;
        public const float WindCorridorMultiplier  = 2f;

        public float GenerationMultiplier { get; }

        public Windmill(Vector2Int pos, bool onWindCorridor) : base(pos)
        {
            GenerationMultiplier = onWindCorridor ? WindCorridorMultiplier : 1f;
        }

        public float GenerationPerSecond => BaseGenerationPerSecond * GenerationMultiplier;
        public override AutomationKind Kind => AutomationKind.Windmill;
        public override string Status => $"Windmill ({GenerationPerSecond:0.0}/s)";
    }

    public sealed class Sprinkler : AutomationDevice
    {
        public const float TargetSoilWater      = 40f;
        public const float MaxWaterPerSecond    = 2f;

        public Sprinkler(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.Sprinkler;

        /// <summary>Top covered plots up toward the target, limited by flow rate and network stock.</summary>
        public void Step(float dtSeconds, IReadOnlyList<SimPlot> plots, WaterNetwork water)
        {
            float perPlotCap = MaxWaterPerSecond * dtSeconds;
            foreach (var plot in plots)
            {
                if (!Covers(plot.GridPos)) continue;
                float need = TargetSoilWater - plot.Soil.WaterLevel;
                if (need <= 0f) continue;
                float drawn = water.Draw(System.Math.Min(need, perPlotCap));
                if (drawn > 0f) plot.Soil.AddWater(drawn);
                if (water.IsEmpty) break;
            }
        }
    }

    public sealed class WindTotem : AutomationDevice
    {
        public WindTotem(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.WindTotem;
        public override string Status => "Wind Totem (3×3 shelter)";
    }

    public sealed class Composter : AutomationDevice
    {
        public const float NutrientThreshold = 70f;
        public const float CompostPerUse     = 20f;
        public const int   MaxStock          = 50;

        public int Stock { get; private set; }

        public Composter(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.Composter;
        public override string Status => $"Composter ({Stock}/{MaxStock})";

        public void AddWaste(int amount = 1) => Stock = System.Math.Min(Stock + amount, MaxStock);
        public void Restore(int stock) => Stock = System.Math.Clamp(stock, 0, MaxStock);

        public void Step(IReadOnlyList<SimPlot> plots)
        {
            foreach (var plot in plots)
            {
                if (Stock <= 0) return;
                if (!Covers(plot.GridPos)) continue;
                if (plot.Soil.Nutrients >= NutrientThreshold) continue;
                plot.Soil.ApplyCompost(CompostPerUse);
                Stock--;
            }
        }
    }

    public sealed class TendersPost : AutomationDevice
    {
        public const float PowerPerSecond = 0.1f;

        public TendersPost(Vector2Int pos) : base(pos) { }
        public override AutomationKind Kind => AutomationKind.TendersPost;
        public override string Status => "Tender's Post (3×3 harvest + replant)";

        /// <summary>
        /// Harvest ripe covered crops into adjacent storage, then replant empty covered plots
        /// from seeds found there. Returns false when the post could not run for lack of power.
        /// </summary>
        public bool Step(float dtSeconds, IReadOnlyList<SimPlot> plots, IReadOnlyList<SimStorage> storages,
                         IReadOnlyList<Composter> composters, PowerGrid power, System.Random rng, OfflineReport report)
        {
            if (!power.TryDraw(PowerPerSecond * dtSeconds)) return false;

            var adjacent = new List<SimStorage>();
            foreach (var s in storages)
                if (IsAdjacent(s.GridPos)) adjacent.Add(s);
            if (adjacent.Count == 0) return true;

            foreach (var plot in plots)
            {
                if (!Covers(plot.GridPos)) continue;
                if (plot.Crop != null && plot.Crop.IsHarvestable)
                    Harvest(plot, adjacent, composters, rng, report);
            }

            foreach (var plot in plots)
            {
                if (!Covers(plot.GridPos)) continue;
                if (plot.Crop == null && plot.Soil.IsTilled)
                    Replant(plot, adjacent, report);
            }
            return true;
        }

        private static void Harvest(SimPlot plot, List<SimStorage> adjacent, IReadOnlyList<Composter> composters,
                                    System.Random rng, OfflineReport report)
        {
            var crop = plot.Crop!;
            var def  = GameDatabase.GetCrop(crop.CropId);
            string yieldItem = def?.HarvestYieldItemId ?? crop.CropId;
            int min = def?.HarvestYieldMin ?? 1;
            int max = def?.HarvestYieldMax ?? 1;
            int amount = rng.Next(min, max + 1);

            bool stored = false;
            foreach (var s in adjacent)
                if (s.Inventory.TryAdd(yieldItem, amount)) { stored = true; break; }

            if (!stored)
            {
                report.MarkStorageFull();
                return;   // leave it ripe for the player
            }

            plot.Soil.RecordHarvest(crop.CropId);
            plot.LastCropId = crop.CropId;
            plot.Crop = null;
            report.RecordHarvest(yieldItem, amount);
            Core.EventBus.Publish(new Core.CropHarvestedEvent { CropId = crop.CropId, YieldItemId = yieldItem, Amount = amount });

            foreach (var c in composters)
                if (c.Covers(plot.GridPos)) c.AddWaste();
        }

        private static void Replant(SimPlot plot, List<SimStorage> adjacent, OfflineReport report)
        {
            // Prefer the seed for whatever grew here last, then any seed in reach.
            CropDef? chosen = null;
            SimStorage? source = null;

            if (plot.LastCropId != null)
            {
                var last = GameDatabase.GetCrop(plot.LastCropId);
                if (last != null)
                    foreach (var s in adjacent)
                        if (s.Inventory.Has(last.SeedItemId)) { chosen = last; source = s; break; }
            }

            if (chosen == null)
            {
                foreach (var s in adjacent)
                {
                    foreach (var slot in s.Inventory.Slots)
                    {
                        if (slot.IsEmpty) continue;
                        var def = GameDatabase.GetCropForSeed(slot.ItemId);
                        if (def == null) continue;
                        chosen = def; source = s; break;
                    }
                    if (chosen != null) break;
                }
            }

            if (chosen == null || source == null) return;
            if (!source.Inventory.TryRemove(chosen.SeedItemId, 1)) return;

            plot.Crop = new Farming.CropState(chosen.CropId, chosen.GrowthTimeMinutes,
                                              chosen.GrowthStages, chosen.WaterConsumptionPerMinute);
            plot.LastCropId = chosen.CropId;
            report.Replanted++;
            Core.EventBus.Publish(new Core.CropPlantedEvent { CropId = chosen.CropId });
        }
    }
}
