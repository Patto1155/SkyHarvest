// Assets/Scripts/Player/InteractionSystem.cs
// Owned by: world/island agent
// Uses a static InteractableRegistry instead of Physics2D.OverlapCircleAll to
// avoid physics-layer setup dependencies.  E to interact with nearest target
// within 1.2 world units.
using System.Collections.Generic;
using UnityEngine;

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

            if (CurrentTarget != null && Input.GetKeyDown(KeyCode.E))
            {
                _player?.PlayActionAnimation();
                CurrentTarget.Interact(_player!);
            }
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
