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

        // ---- tile highlight (world-space LineRenderer) ----
        private LineRenderer? _highlight;
        private Vector2Int    _lastCell  = new(int.MinValue, 0);
        private bool          _hlVisible;

        // Diamond vertices relative to cell centre (world units, 2:1 dimetric projection).
        // From GridToWorld: TileWorldWidth=1, TileWorldHeight=0.5; half-extents are ±0.5 x, ±0.25 y.
        private static readonly Vector2[] DiamondVerts =
        {
            new( 0f,    0.25f),   // top
            new( 0.5f,  0f   ),   // right
            new( 0f,   -0.25f),   // bottom
            new(-0.5f,  0f   ),   // left
        };

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
            _cursorRT.sizeDelta = new Vector2(20f, 20f);
            _cursorRT.pivot     = Vector2.up;   // top-left of sprite = cursor tip

            var img = go.GetComponent<Image>();
            img.sprite        = MakeCursorSprite();
            img.raycastTarget = false;
        }

        private static Sprite MakeCursorSprite()
        {
            const int S = 20;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var px = new Color32[S * S];
            var clear  = new Color32(0, 0, 0, 0);
            var white  = new Color32(255, 255, 255, 255);
            var fill   = new Color32(80, 200, 255, 220);   // sky-blue, matches the island palette

            for (int i = 0; i < px.Length; i++) px[i] = clear;

            // Arrow cursor pointing top-left.
            // Row indices count from the bottom of the texture; the top of the image
            // is row S-1. We define the arrow in "screen row" order (row 0 = topmost pixel)
            // then flip for Unity's bottom-up convention.
            // Arrow body: a right-triangle with the right-angle at top-left.
            int arrowH = 14;   // height of the arrow shaft
            for (int screenRow = 0; screenRow < arrowH; screenRow++)
            {
                int texRow = S - 1 - screenRow;
                int rowLen = arrowH - screenRow;   // gets shorter as we go down
                for (int col = 0; col < rowLen + 1 && col < S; col++)
                {
                    bool border = col == 0 || col == rowLen || screenRow == 0 ||
                                  (screenRow == arrowH - 1 && col <= 1);
                    px[texRow * S + col] = border ? white : fill;
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.up, 1f);
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
            _highlight = go.AddComponent<LineRenderer>();
            _highlight.positionCount  = 5;     // 4 corners + close the loop
            _highlight.loop           = false;
            _highlight.useWorldSpace  = true;
            _highlight.widthMultiplier = 0.032f;

            var mat = new Material(Shader.Find("Sprites/Default"));
            _highlight.material    = mat;
            _highlight.startColor  = new Color(1f, 0.92f, 0.35f, 0.90f);
            _highlight.endColor    = new Color(1f, 0.92f, 0.35f, 0.90f);
            _highlight.sortingOrder = 200;
            _highlight.gameObject.SetActive(false);
        }

        private void UpdateHighlight()
        {
            if (_highlight == null || _cam == null || _island == null || _player == null
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
                if (_hlVisible) { _highlight.gameObject.SetActive(false); _hlVisible = false; }
                return;
            }

            if (gridPos != _lastCell || !_hlVisible)
            {
                _lastCell = gridPos;
                Vector2 centre = GridMath.GridToWorld(gridPos, _player.CurrentTier);
                const float z  = -0.2f;   // in front of terrain

                for (int v = 0; v < 4; v++)
                    _highlight.SetPosition(v, new Vector3(
                        centre.x + DiamondVerts[v].x,
                        centre.y + DiamondVerts[v].y, z));
                _highlight.SetPosition(4, new Vector3(
                    centre.x + DiamondVerts[0].x,
                    centre.y + DiamondVerts[0].y, z));
            }

            if (!_hlVisible) { _highlight.gameObject.SetActive(true); _hlVisible = true; }
        }
    }
}
