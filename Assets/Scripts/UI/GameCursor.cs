// Custom cursor and per-tile hover highlight.
// Hides the OS cursor and draws a small Terraria-style sprite instead.
// Draws a gold diamond outline around the hovered tile only when the
// player can interact with it given their current tool / held item.
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Building;

namespace SkyHarvest.UI
{
    public class GameCursor : MonoBehaviour
    {
        // ---- references wired by Bootstrap ----
        private Camera?           _cam;
        private Canvas?           _hudCanvas;
        private IslandData?       _island;
        private PlayerController? _player;
        private ToolSystem?       _tools;
        private Hotbar?           _hotbar;

        // ---- cursor UI ----
        private RectTransform? _cursorRT;

        // ---- tile highlight (world-space SpriteRenderer) ----
        private SpriteRenderer? _highlightSR;
        private Vector2Int      _lastCell   = new(int.MinValue, 0);
        private bool            _hlVisible;

        // -----------------------------------------------------------------------
        // Bootstrap wiring
        // -----------------------------------------------------------------------
        public void Initialize(Camera cam, Canvas hudCanvas)
        {
            _cam       = cam;
            _hudCanvas = hudCanvas;
            Cursor.visible = false;
            BuildCursorSprite();
            BuildHighlight();
        }

        public void SetGameRefs(IslandData island, PlayerController player,
                                ToolSystem tools, Hotbar hotbar)
        {
            _island  = island;
            _player  = player;
            _tools   = tools;
            _hotbar  = hotbar;
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Update()
        {
            MoveCursor();
            UpdateHighlight();
        }

        private void OnDestroy() => Cursor.visible = true;

        // -----------------------------------------------------------------------
        // Cursor sprite
        // -----------------------------------------------------------------------
        private void BuildCursorSprite()
        {
            var go = new GameObject("CustomCursor",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_hudCanvas!.transform, false);
            go.transform.SetAsLastSibling();

            _cursorRT = go.GetComponent<RectTransform>();
            _cursorRT.sizeDelta = new Vector2(18f, 18f);
            _cursorRT.pivot     = new Vector2(0f, 1f);  // top-left = cursor tip

            var img = go.GetComponent<Image>();
            // ProcGfx.CursorPointer draws a proper NW-pointing Terraria-style arrow
            // with white border and sky-blue fill, pivot pinned at the tip.
            img.sprite        = ProcGfx.CursorPointer(new Color(0.31f, 0.78f, 1f), 18);
            img.raycastTarget = false;
        }

        private void MoveCursor()
        {
            if (_cursorRT == null || _hudCanvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _hudCanvas.transform as RectTransform,
                Input.mousePosition, null, out var local);
            _cursorRT.anchoredPosition = local;
        }

        // -----------------------------------------------------------------------
        // Tile highlight
        // -----------------------------------------------------------------------
        private void BuildHighlight()
        {
            var go = new GameObject("TileHighlight");
            _highlightSR = go.AddComponent<SpriteRenderer>();
            // ProcGfx.IsoDiamondEdgeGlow is a 64×32 hollow diamond rim sprite that
            // fits exactly over one tile (1.0 × 0.5 world units, centre pivot).
            _highlightSR.sprite       = ProcGfx.IsoDiamondEdgeGlow(new Color(1f, 0.92f, 0.35f, 0.90f), 64, 32, 0.12f);
            _highlightSR.sortingOrder = 200;
            go.SetActive(false);
        }

        private void UpdateHighlight()
        {
            if (_highlightSR == null || _cam == null || _island == null || _player == null
                || _tools == null || _hotbar == null) return;

            // Screen → world → grid at the player's current elevation tier.
            var screenPos = Input.mousePosition;
            screenPos.z   = Mathf.Abs(_cam.transform.position.z);
            Vector3 world3 = _cam.ScreenToWorldPoint(screenPos);
            Vector2 world2 = new(world3.x, world3.y);

            var gridPos = GridMath.WorldToGrid(world2, _player.CurrentTier);

            bool canInteract = TileInteractability.CanInteractAt(
                gridPos, _island, _player, _tools, _hotbar,
                BuildModeController.Instance);

            if (!canInteract)
            {
                if (_hlVisible) { _highlightSR.gameObject.SetActive(false); _hlVisible = false; }
                return;
            }

            if (gridPos != _lastCell || !_hlVisible)
            {
                _lastCell = gridPos;
                Vector2 centre = GridMath.GridToWorld(gridPos, _player.CurrentTier);
                _highlightSR.transform.position = new Vector3(centre.x, centre.y, -0.1f);
            }

            if (!_hlVisible) { _highlightSR.gameObject.SetActive(true); _hlVisible = true; }
        }
    }
}
