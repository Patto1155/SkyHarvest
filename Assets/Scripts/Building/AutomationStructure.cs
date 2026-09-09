// A placed structure that owns a pure Sim.AutomationDevice. The device does all the work
// (driven by AutomationSystem online and IslandSim.Advance offline); this class is the scene
// anchor, prompt text, and the running/idle visual.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Sim;

namespace SkyHarvest.Building
{
    public class AutomationStructure : Structure
    {
        public AutomationDevice Device { get; private set; } = null!;

        private SpriteRenderer? _sr;
        private Sprite[]? _frames;
        private SpriteAnimator? _anim;
        private bool _running;
        private float _pollTimer;
        private const float PollInterval = 0.5f;

        public override void Initialize(StructureDef def, Vector2Int gridPos)
        {
            base.Initialize(def, gridPos);
            var island = Core.GameManager.Instance?.CurrentIsland;
            var terrain = island?.GetCell(gridPos)?.Terrain ?? TerrainType.RockyPlateau;
            Device = CreateDevice(def.Automation, gridPos, terrain);
            AutomationSystem.Instance?.MarkDirty();
            SetupVisual(def);
        }

        private void SetupVisual(StructureDef def)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (def.SpriteFrameWidth <= 0) return;
            try { _frames = SpriteLoader.LoadStrip($"Sprites/structures/{def.StructureId}", def.SpriteFrameWidth); }
            catch { _frames = null; }
            if (_frames == null || _frames.Length == 0) return;

            // Windmill sails always turn — wind never stops up here.
            if (Device is Windmill)
            {
                _anim = gameObject.AddComponent<SpriteAnimator>();
                _anim.Frames = _frames;
                _anim.Fps    = 6f;
                _anim.Loop   = true;
            }
            else if (_sr != null) _sr.sprite = _frames[0];
        }

        /// <summary>Two-frame devices show frame 1 while they actually have the resource to run.</summary>
        private void Update()
        {
            if (_frames == null || _frames.Length < 2 || _sr == null || _anim != null) return;
            _pollTimer -= Time.deltaTime;
            if (_pollTimer > 0f) return;
            _pollTimer = PollInterval;

            var sim = AutomationSystem.Instance;
            bool running = Device switch
            {
                Sprinkler   => sim != null && !sim.Water.IsEmpty,
                TendersPost => sim != null && !sim.Power.IsEmpty,
                _           => false
            };
            if (running == _running) return;
            _running = running;
            _sr.sprite = _frames[running ? 1 : 0];
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
