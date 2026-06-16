// Assets/Scripts/Player/InteractionSystem.cs
// Owned by: world/island agent
// Uses a static InteractableRegistry instead of Physics2D.OverlapCircleAll to
// avoid physics-layer setup dependencies.  E to interact with nearest target
// within 1.2 world units.  When nothing is targeted, E with the Hoe selected
// tills the bare cell the player is facing (the only way to create new plots).
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Farming;
using SkyHarvest.Island;

namespace SkyHarvest.Player
{
    // =========================================================================
    // Registry — any IInteractable component registers itself here
    // =========================================================================
    public static class InteractableRegistry
    {
        private static readonly List<IInteractable> _all = new();

        public static void Register(IInteractable i)   => _all.Add(i);
        public static void Unregister(IInteractable i) => _all.Remove(i);
        public static IReadOnlyList<IInteractable> All  => _all;
    }

    // =========================================================================
    // System
    // =========================================================================
    public class InteractionSystem : MonoBehaviour
    {
        private const float InteractRadius = 1.2f;

        private PlayerController? _player;
        private IslandRenderer? _renderer;

        // ---- public read API (for UI agent) ----
        public IInteractable? CurrentTarget { get; private set; }
        public string PromptText => CurrentTarget?.InteractionPrompt ?? string.Empty;

        /// <summary>True when the player is facing the uncarved stair edge — used by HUD to show a prompt.</summary>
        public bool CanCarveStairs
        {
            get
            {
                var island = _player?.Island;
                if (island == null || island.StairsCarved) return false;
                var cur = SkyHarvest.Core.GridMath.WorldToGrid(_player!.transform.position, _player.CurrentTier);
                return island.IsStairEdge(cur, _player.CurrentFacingCell);
            }
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            FindNearestInteractable();

            if (!Input.GetKeyDown(KeyCode.E)) return;

            // Stair carve has highest priority so a nearby CropPlot cannot steal the E press
            // before the player mines their way up to the forge tier.
            if (TryCarveStairs()) return;

            if (CurrentTarget != null)
            {
                _player?.PlayActionAnimation();
                CurrentTarget.Interact(_player!);
            }
            else
            {
                TryTillFacingCell();
            }
        }

        // -----------------------------------------------------------------------
        // Tutorial mining — carve the staircase the player is facing, unlocking
        // traversal to the raised tier. One-shot; idempotent via IslandData.
        // -----------------------------------------------------------------------
        private bool TryCarveStairs()
        {
            var island = _player?.Island;
            if (island == null || island.StairsCarved) return false;

            var cur = SkyHarvest.Core.GridMath.WorldToGrid(_player!.transform.position, _player.CurrentTier);
            if (!island.IsStairEdge(cur, _player.CurrentFacingCell)) return false;

            island.CarveStairs(cur);
            _player.PlayActionAnimation();
            return true;
        }

        // -----------------------------------------------------------------------
        // Ground action — till the bare cell the player faces (Hoe selected).
        // This is the only path that creates a new CropPlot; the plot then
        // handles sow/water/harvest via its own Interact.
        // -----------------------------------------------------------------------
        private void TryTillFacingCell()
        {
            if (_player?.Island == null) return;
            if (!_player.TryGetComponent<ToolSystem>(out var tools)) return;
            if (tools.EquippedTool != ToolType.Hoe) return;

            var facingCell = _player.CurrentFacingCell;

            // Block cross-tier tilling: the player must be on the same tier as the target cell.
            if (_player.Island.Tier(facingCell) != _player.CurrentTier) return;

            var cell = _player.Island.GetCell(facingCell);
            if (cell == null || cell.IsTilled) return;
            if (!TerrainProperties.CanPlaceCrops(cell.Terrain)) return;

            if (_renderer == null) _renderer = Object.FindObjectOfType<IslandRenderer>();
            if (FarmingActions.TryTill(cell, _player.Island, _renderer) != null)
                _player.PlayActionAnimation();
        }

        // -----------------------------------------------------------------------
        // Nearest-target scan
        // -----------------------------------------------------------------------
        private void FindNearestInteractable()
        {
            Vector2 pos          = transform.position;
            float   closest      = float.MaxValue;
            float   closestDebris = float.MaxValue;
            IInteractable? best       = null;
            IInteractable? bestDebris = null;

            foreach (var i in InteractableRegistry.All)
            {
                if (i is not MonoBehaviour mb || mb == null) continue;
                float dist = Vector2.Distance(pos, mb.transform.position);
                if (dist > InteractRadius) continue;

                if (i is SkyHarvest.Debris.DebrisObject)
                {
                    if (dist < closestDebris) { closestDebris = dist; bestDebris = i; }
                }
                else
                {
                    if (dist < closest) { closest = dist; best = i; }
                }
            }

            // Prefer debris over other interactables when it is at least as close — this
            // prevents a CropPlot at the same world position from swallowing the scavenge.
            CurrentTarget = (bestDebris != null && closestDebris <= closest) ? bestDebris : (best ?? bestDebris);
        }
    }
}
