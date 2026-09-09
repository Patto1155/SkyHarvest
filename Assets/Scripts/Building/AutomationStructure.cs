// A placed structure that owns a pure Sim.AutomationDevice. The device does all the work
// (driven by AutomationSystem online and IslandSim.Advance offline); this class is just the
// scene anchor, prompt text, and save/restore of device state.
using UnityEngine;
using SkyHarvest.Data;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Sim;

namespace SkyHarvest.Building
{
    public class AutomationStructure : Structure
    {
        public AutomationDevice Device { get; private set; } = null!;

        public override void Initialize(StructureDef def, Vector2Int gridPos)
        {
            base.Initialize(def, gridPos);
            var island = Core.GameManager.Instance?.CurrentIsland;
            var terrain = island?.GetCell(gridPos)?.Terrain ?? TerrainType.RockyPlateau;
            Device = CreateDevice(def.Automation, gridPos, terrain);
            AutomationSystem.Instance?.MarkDirty();
        }

        public static AutomationDevice CreateDevice(AutomationKind kind, Vector2Int pos, TerrainType terrain) => kind switch
        {
            AutomationKind.WaterTank   => new WaterTank(pos),
            AutomationKind.Sprinkler   => new Sprinkler(pos),
            AutomationKind.WindTotem   => new WindTotem(pos),
            AutomationKind.TendersPost => new TendersPost(pos),
            AutomationKind.Composter   => new Composter(pos),
            AutomationKind.Windmill    => new Windmill(pos, terrain == TerrainType.WindCorridor),
            AutomationKind.Battery     => new Battery(pos),
            _ => throw new System.ArgumentException($"Structure has no automation kind: {kind}")
        };

        public override string InteractionPrompt => Device?.Status ?? base.InteractionPrompt;

        public override void Interact(PlayerController player)
        {
            if (TryDemolishWithHammer(player))
                AutomationSystem.Instance?.MarkDirty();
        }
    }
}
