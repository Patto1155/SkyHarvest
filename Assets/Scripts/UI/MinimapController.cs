// Minimap panel: renders the island shape as a top-down dot map and tracks the
// player's position with a small yellow marker that updates every frame.
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.Core;

namespace SkyHarvest.UI
{
    public class MinimapController : MonoBehaviour
    {
        private IslandData?       _island;
        private PlayerController? _player;
        private RectTransform?    _playerMarker;

        // Island grid bounds cached at initialize time.
        private int   _minGx, _maxGx, _minGy, _maxGy;
        private float _cellW, _cellH;
        private const float PanelW = 156f, PanelH = 116f;   // inner area (4px margin each side)

        public void Initialize(GameObject panel, IslandData island, PlayerController player)
        {
            _island = island;
            _player = player;

            // Re-initializing (e.g. New Game after a prior session) — clear old children.
            var oldMap = panel.transform.Find("MinimapImage");
            if (oldMap != null) Destroy(oldMap.gameObject);
            var oldDot = panel.transform.Find("MinimapPlayer");
            if (oldDot != null) Destroy(oldDot.gameObject);

            ComputeBounds(island);

            // The panel itself already has an Image (from MakePanel) — a GameObject can
            // only hold one Graphic, so the map goes on its own child RawImage instead.
            var tex = BuildTexture(island);
            var mapGO = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
            mapGO.transform.SetParent(panel.transform, false);
            var raw = mapGO.GetComponent<RawImage>();
            raw.texture = tex;
            var rawRT = raw.rectTransform;
            rawRT.anchoredPosition = Vector2.zero;
            rawRT.sizeDelta        = new Vector2(PanelW, PanelH);

            // Player dot — 5×5 yellow square, always on top.
            var dotGO  = new GameObject("MinimapPlayer", typeof(RectTransform), typeof(Image));
            dotGO.transform.SetParent(panel.transform, false);
            var dotImg = dotGO.GetComponent<Image>();
            dotImg.color = new Color(1f, 0.95f, 0.2f, 1f);
            _playerMarker = dotGO.GetComponent<RectTransform>();
            _playerMarker.sizeDelta = new Vector2(5f, 5f);
            dotGO.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (_player == null || _playerMarker == null || _island == null) return;

            var gridPos = GridMath.WorldToGrid(_player.transform.position, _player.CurrentTier);
            _playerMarker.anchoredPosition = GridToPanel(gridPos.x, gridPos.y);
        }

        // -----------------------------------------------------------------------

        private void ComputeBounds(IslandData island)
        {
            _minGx = _minGy = int.MaxValue;
            _maxGx = _maxGy = int.MinValue;
            foreach (var pos in island.Cells.Keys)
            {
                if (pos.x < _minGx) _minGx = pos.x;
                if (pos.x > _maxGx) _maxGx = pos.x;
                if (pos.y < _minGy) _minGy = pos.y;
                if (pos.y > _maxGy) _maxGy = pos.y;
            }
            int spanX = _maxGx - _minGx + 1;
            int spanY = _maxGy - _minGy + 1;
            _cellW = PanelW / spanX;
            _cellH = PanelH / spanY;
        }

        private Vector2 GridToPanel(int gx, int gy)
        {
            // Panel center = (0,0). gy=0 (forge back) → top of minimap.
            float x = (gx - _minGx + 0.5f) * _cellW - PanelW * 0.5f;
            float y = (_maxGy - gy + 0.5f) * _cellH - PanelH * 0.5f;
            return new Vector2(x, y);
        }

        private Texture2D BuildTexture(IslandData island)
        {
            int spanX = _maxGx - _minGx + 1;
            int spanY = _maxGy - _minGy + 1;
            int cellPx = Mathf.Max(4, Mathf.FloorToInt(Mathf.Min(PanelW / spanX, PanelH / spanY)));
            int texW   = spanX * cellPx;
            int texH   = spanY * cellPx;

            var pixels = new Color32[texW * texH];
            // Transparent background so the panel bg shows through for empty areas.
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

            foreach (var kvp in island.Cells)
            {
                var pos  = kvp.Key;
                var cell = kvp.Value;
                // Forge (elevated) = stone tan; Farm = green.
                Color32 fill = cell.Elevation > 0.5f
                    ? new Color32(128, 108, 84, 245)
                    : new Color32(72, 128, 58, 245);
                Color32 border = new Color32(20, 16, 12, 220);

                int px = (pos.x - _minGx) * cellPx;
                // Flip vertical: gy=0 at top of texture (texH - offset - height)
                int py = (_maxGy - pos.y) * cellPx;

                for (int dy = 0; dy < cellPx; dy++)
                for (int dx = 0; dx < cellPx; dx++)
                {
                    bool edge = dx == 0 || dy == 0 || dx == cellPx - 1 || dy == cellPx - 1;
                    // Unity textures are bottom-up, so flip Y within texture.
                    int ty = texH - 1 - (py + dy);
                    pixels[ty * texW + (px + dx)] = edge ? border : fill;
                }
            }

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
