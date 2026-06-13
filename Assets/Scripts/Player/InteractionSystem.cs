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
        // Ground action — till the bare cell the player faces (Hoe selected).
        // This is the only path that creates a new CropPlot; the plot then
        // handles sow/water/harvest via its own Interact.
        // -----------------------------------------------------------------------
        private void TryTillFacingCell()
        {
            if (_player?.Island == null) return;
            if (!_player.TryGetComponent<ToolSystem>(out var tools)) return;
            if (tools.EquippedTool != ToolType.Hoe) return;

            var cell = _player.Island.GetCell(_player.CurrentFacingCell);
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
            Vector2 pos     = transform.position;
            float  closest  = float.MaxValue;
            IInteractable? best = null;

            foreach (var i in InteractableRegistry.All)
            {
                if (i is MonoBehaviour mb && mb != null)
                {
                    float dist = Vector2.Distance(pos, mb.transform.position);
                    if (dist < closest && dist <= InteractRadius)
                    {
                        closest = dist;
                        best    = i;
                    }
                }
            }

            CurrentTarget = best;
        }
    }
}
