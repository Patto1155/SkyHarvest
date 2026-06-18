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
        private Canvas?          _cursorCanvas;
        private RectTransform?   _cursorRT;

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

            // Own overlay canvas above MainMenuCanvas (sortingOrder 100) so the pointer
            // is visible on the title screen — the HUD canvas sits underneath the menu.
            var canvasGO = new GameObject("CursorCanvas");
            _cursorCanvas = canvasGO.AddComponent<Canvas>();
            _cursorCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _cursorCanvas.sortingOrder = 200;
            canvasGO.AddComponent<CanvasScaler>();

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
            go.transform.SetParent(_cursorCanvas!.transform, false);
            go.transform.SetAsLastSibling();

            _cursorRT = go.GetComponent<RectTransform>();
            _cursorRT.sizeDelta = new Vector2(24f, 24f);
            // Keep the drawn cursor centered on the same point used for tile picking.
            // This avoids "highlight one tile above/aside" feeling caused by a mismatched hotspot.
            _cursorRT.pivot     = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.sprite        = ProcGfx.CursorPointer(new Color(0.45f, 0.88f, 1f), 24);
            img.raycastTarget = false;
        }

        private void MoveCursor()
        {
            if (_cursorRT == null || _cursorCanvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cursorCanvas.transform as RectTransform,
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

            var gridPos = GridMath.ScreenToGrid(_cam, Input.mousePosition, _player.CurrentTier);

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
                float elev = _island.GetCell(gridPos)?.Elevation ?? _player.CurrentTier;
                Vector2 centre = GridMath.GridToWorld(gridPos, elev);
                _highlightSR.transform.position = new Vector3(centre.x, centre.y, -0.1f);
            }

            if (!_hlVisible) { _highlightSR.gameObject.SetActive(true); _hlVisible = true; }
        }
    }
}
