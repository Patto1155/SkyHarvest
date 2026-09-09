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
        // Tap-to-act — the tapped cell replaces "facing cell" as the target.
        // Priority mirrors the E key: stairs → interactable on the cell → till.
        // -----------------------------------------------------------------------
        public bool TryActOnCell(Vector2Int cell)
        {
            var island = _player?.Island;
            if (island == null) return false;

            var cur = SkyHarvest.Core.GridMath.WorldToGrid(_player!.transform.position, _player.CurrentTier);
            if (!island.StairsCarved && island.IsStairEdge(cur, cell))
            {
                island.CarveStairs(cur);
                _player.PlayActionAnimation();
                return true;
            }

            var target = FindInteractableAt(cell, island);
            if (target != null)
            {
                _player.PlayActionAnimation();
                target.Interact(_player);
                return true;
            }

            return TryTillCell(cell);
        }

        private IInteractable? FindInteractableAt(Vector2Int cell, IslandData island)
        {
            float elev = island.GetCell(cell)?.Elevation ?? 0f;
            Vector2 centre = SkyHarvest.Core.GridMath.GridToWorld(cell, elev);
            IInteractable? best = null; IInteractable? bestDebris = null;
            float closest = 0.6f, closestDebris = 0.6f;

            foreach (var i in InteractableRegistry.All)
            {
                if (i is not MonoBehaviour mb || mb == null) continue;
                float dist = Vector2.Distance(centre, mb.transform.position);
                if (i is SkyHarvest.Debris.DebrisObject) { if (dist < closestDebris) { closestDebris = dist; bestDebris = i; } }
                else if (dist < closest) { closest = dist; best = i; }
            }
            return bestDebris ?? best;
        }

        private bool TryTillCell(Vector2Int cellPos)
        {
            if (_player?.Island == null) return false;
            if (!_player.TryGetComponent<ToolSystem>(out var tools)) return false;
            if (tools.EquippedTool != ToolType.Hoe) return false;
            if (_player.Island.Tier(cellPos) != _player.CurrentTier) return false;

            var cell = _player.Island.GetCell(cellPos);
            if (cell == null || cell.IsTilled) return false;
            if (!TerrainProperties.CanPlaceCrops(cell.Terrain)) return false;

            if (_renderer == null) _renderer = Object.FindObjectOfType<IslandRenderer>();
            if (FarmingActions.TryTill(cell, _player.Island, _renderer) == null) return false;
            _player.PlayActionAnimation();
            return true;
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
            if (_player == null) return;
            TryTillCell(_player.CurrentFacingCell);
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
