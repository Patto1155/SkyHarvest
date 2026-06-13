// Assets/Scripts/Island/IslandRenderer.cs
// Owned by: world/island agent
// Builds one GameObject per island cell with a SpriteRenderer for the terrain
// tile and optional child overlay sprites for tilled/wet/dry soil state.
// Spring cells get a SpriteAnimator cycling through their 3 strip frames.
// All sprite loading goes through SpriteLoader (UI/bootstrap agent owns it).
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class IslandRenderer : MonoBehaviour
    {
        // ---- sprite paths (from CONVENTIONS manifest) ----
        private const int TileFrameWidth    = 64;   // px per tile frame (64×80 strips)
        private const int OverlayFrameWidth = 64;   // overlay diamonds 64×32

        // ---- internal state ----
        private IslandData? _island;
        private readonly Dictionary<Vector2Int, CellVisuals> _visuals = new();

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Build the full island visual from scratch.
        /// Called by Bootstrap / GameManager after Generate().
        /// </summary>
        public void Render(IslandData island)
        {
            _island = island;
            DestroyAll();

            foreach (var kvp in island.Cells)
                BuildCellVisuals(kvp.Value);
        }

        /// <summary>
        /// Add visuals for cells added at runtime via <see cref="IslandExpansion.Expand"/>.
        /// </summary>
        public void RenderNewCells(IEnumerable<IslandCell> newCells)
        {
            foreach (var cell in newCells)
                BuildCellVisuals(cell);
        }

        /// <summary>
        /// Refresh the soil overlay for a single cell.
        /// Called by FarmingActions / CropGrowthSystem after tilling or watering.
        /// </summary>
        public void RefreshCellOverlay(Vector2Int gridPos)
        {
            if (!_visuals.TryGetValue(gridPos, out var vis)) return;
            if (_island == null) return;
            var cell = _island.GetCell(gridPos);
            if (cell == null) return;
            UpdateOverlay(vis, cell);
        }

        // -------------------------------------------------------------------------
        // Internal helpers
        // -------------------------------------------------------------------------

        private void BuildCellVisuals(IslandCell cell)
        {
            Vector2 worldPos = GridMath.GridToWorld(cell.GridPos, cell.Elevation);
            int sortBase = GridMath.SortingOrder(worldPos.y, bias: -10000);

            // ---- root cell object ----
            var go = new GameObject($"Cell_{cell.GridPos.x}_{cell.GridPos.y}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // ---- terrain SpriteRenderer ----
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortBase;

            int variant  = CellHashVariant(cell.GridPos, TerrainProperties.VariantCount(cell.Terrain));
            string path  = TerrainProperties.TilePath(cell.Terrain);

            Sprite[] strip = TryLoadTileStrip(path, TileFrameWidth);
            if (strip != null && strip.Length > 0)
            {
                sr.sprite = strip[variant % strip.Length];

                // Natural springs animate through their 3 frames
                if (cell.Terrain == TerrainType.NaturalSpring && strip.Length >= 2)
                {
                    var anim = go.AddComponent<SpriteAnimator>();
                    anim.Frames = strip;
                    anim.Fps    = 2f;
                    anim.Loop   = true;
                }
            }
            else
            {
                sr.sprite = MagentaFallback();
            }

            // ---- soil overlay child ----
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(go.transform);
            overlayGo.transform.localPosition = Vector3.zero;
            var overlaySr = overlayGo.AddComponent<SpriteRenderer>();
            overlaySr.sortingOrder = sortBase + 5000;   // -10000+5000 = -5000 (flat overlay layer)
            overlaySr.enabled = false;                  // hidden until tilled

            var vis = new CellVisuals
            {
                Root      = go,
                Tile      = sr,
                Overlay   = overlaySr
            };
            _visuals[cell.GridPos] = vis;
            UpdateOverlay(vis, cell);
        }

        private void UpdateOverlay(CellVisuals vis, IslandCell cell)
        {
            var soil = cell.Soil;
            if (!soil.IsTilled)
            {
                vis.Overlay.enabled = false;
                return;
            }

            // Choose overlay path by soil moisture state
            string overlayPath;
            if (soil.IsWet)
                overlayPath = "Sprites/terrain/overlay_wet";
            else if (soil.IsDry)
                overlayPath = "Sprites/terrain/overlay_dry";
            else
                overlayPath = "Sprites/terrain/overlay_tilled";

            Sprite? sp = TryLoadTileSprite(overlayPath);
            vis.Overlay.sprite  = sp ?? MagentaFallback();
            vis.Overlay.enabled = true;
        }

        private void DestroyAll()
        {
            foreach (var vis in _visuals.Values)
            {
                if (vis.Root != null) Destroy(vis.Root);
            }
            _visuals.Clear();
        }

        // ---- sprite helpers ----

        private static Sprite[]? TryLoadTileStrip(string path, int frameW)
        {
            try
            {
                // Crop to the 64×32 diamond face; full 80px frames overlap on the dimetric grid.
                return SpriteLoader.LoadTerrainStrip(path, frameW);
            }
            catch { return null; }
        }

        private static Sprite? TryLoadTileSprite(string path)
        {
            try
            {
                return SpriteLoader.LoadTile(path);
            }
            catch { return null; }
        }

        private static Sprite MagentaFallback()
        {
            // 1×1 magenta texture — harness-safe null fallback
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.magenta);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), Constants.PixelsPerUnit);
        }

        /// <summary>Stable per-cell hash to pick a tile variant deterministically.</summary>
        private static int CellHashVariant(Vector2Int pos, int count)
        {
            if (count <= 1) return 0;
            int h = pos.x * 73856093 ^ pos.y * 19349663;
            return Mathf.Abs(h) % count;
        }

        // -------------------------------------------------------------------------
        // Inner types
        // -------------------------------------------------------------------------
        private class CellVisuals
        {
            public GameObject Root    = null!;
            public SpriteRenderer Tile    = null!;
            public SpriteRenderer Overlay = null!;
        }
    }
}
