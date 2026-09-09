// Tap / click to move and act (v2 spec §1 pillar 5: tap the tile, not the avatar).
// Tap a cell: if within reach, act on it now; otherwise walk there (BFS, tier-gated) and act on
// arrival. Ignored while a UI panel is open, while build mode owns the click, or when the tap
// landed on UI. Keyboard (WASD/E) keeps working as a fallback and cancels any auto-walk.
using UnityEngine;
using UnityEngine.EventSystems;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Player
{
    public class TapController : MonoBehaviour
    {
        private PlayerController? _player;
        private InteractionSystem? _interaction;
        private Camera? _cam;

        private Vector2Int? _pendingCell;

        private void Awake()
        {
            _player      = GetComponent<PlayerController>();
            _interaction = GetComponent<InteractionSystem>();
        }

        private void Update()
        {
            if (_player?.Island == null) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (AnyPanelOpen()) return;
            if (BuildModeController.Instance != null && BuildModeController.Instance.IsActive) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            _cam ??= Camera.main;
            if (_cam == null) return;

            var screen = Input.mousePosition;
            screen.z = Mathf.Abs(_cam.transform.position.z);
            Vector3 w3 = _cam.ScreenToWorldPoint(screen);
            var cell = TapTargeting.ResolveCell(_player.Island, new Vector2(w3.x, w3.y), _player.CurrentTier);
            if (cell == null) return;

            OnCellTapped(cell.Value);
        }

        private void OnCellTapped(Vector2Int cell)
        {
            var island = _player!.Island!;
            float elev = island.GetCell(cell)?.Elevation ?? 0f;

            if (TapTargeting.IsWithinReach(_player.transform.position, cell, elev))
            {
                _player.CancelWalk();
                Act(cell);
                return;
            }

            var from = GridMath.WorldToGrid(_player.transform.position, _player.CurrentTier);
            var path = GridPath.Find(island, from, cell);
            if (path == null)
            {
                // Unreachable (e.g. uncarved stairs): walk as close as we can along the tier and stop.
                var nearest = NearestReachable(island, from, cell);
                if (nearest == null) return;
                path = GridPath.Find(island, from, nearest.Value);
                if (path == null) return;
            }

            _pendingCell = cell;
            _player.WalkPath(path, OnArrived);
        }

        private void OnArrived()
        {
            if (_pendingCell == null) return;
            var cell = _pendingCell.Value;
            _pendingCell = null;

            var island = _player!.Island!;
            float elev = island.GetCell(cell)?.Elevation ?? 0f;
            if (TapTargeting.IsWithinReach(_player.transform.position, cell, elev))
                Act(cell);
        }

        private void Act(Vector2Int cell)
        {
            _player!.FaceCell(cell);
            _interaction?.TryActOnCell(cell);
        }

        private static Vector2Int? NearestReachable(IslandData island, Vector2Int from, Vector2Int goal)
        {
            Vector2Int? best = null;
            int bestDist = int.MaxValue;
            foreach (var kv in island.Cells)
            {
                var c = kv.Key;
                int d = Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y);
                if (d >= bestDist) continue;
                if (GridPath.Find(island, from, c) == null) continue;
                best = c; bestDist = d;
            }
            return best;
        }

        private bool AnyPanelOpen()
        {
            if (_player == null) return false;
            return (_player.InventoryUI?.IsOpen ?? false)
                || (_player.WorkshopUI?.IsOpen ?? false)
                || (_player.StorageUIRef?.IsOpen ?? false)
                || (_player.BuildMenuUI?.IsOpen ?? false)
                || (_player.PauseMenuUI?.IsOpen ?? false)
                || (GameManager.Instance?.IsPaused ?? false);
        }
    }
}
