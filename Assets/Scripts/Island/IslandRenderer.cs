// Assets/Scripts/Island/IslandRenderer.cs
// Owned by: world/island agent
// Builds one GameObject per island cell with a SpriteRenderer for the terrain
// tile and optional child overlay sprites for tilled/wet/dry soil state.
// Spring cells get a SpriteAnimator cycling through their 3 strip frames.
// All sprite loading goes through SpriteLoader (UI/bootstrap agent owns it).
//
// Session 6 additions — procedural autotile terrain blending:
//   • Dark-earth underlay diamond (slightly oversized) under each tile to seal
//     sky-gap seams between tiles.
//   • 8-neighbour bitmask feather overlays at terrain type boundaries so terrain
//     types fade into each other instead of hard-cutting (TerrainAutotiler).
//   • RenderNewCells also refreshes blend overlays for all 8 neighbours of each
//     new cell so existing border cells update correctly.
using System.Collections.Generic;
using System.IO;
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

        // Shared underlay sprite — all cells use the same texture to save memory.
        // Lazily created on first Render().
        private Sprite? _underlaySprite;

        // Shared two-tier cliff-face sprites, keyed by (stair, rightSide). A face is
        // drawn on each diamond edge whose camera-facing neighbour is a lower tier:
        // +y (0,1) = the down-LEFT (SW) edge, +x (1,0) = the down-RIGHT (SE) edge.
        private readonly Dictionary<(bool stair, bool right), Sprite> _faceSprites = new();

        // Carved-stair: custom PNG block and/or procedural cliff face fallback.
        private Sprite? _carvedStairSw;
        private Sprite? _carvedStairSe;
        private Sprite? _stairTrimTowardY;
        private Sprite? _stairTrimTowardX;
        private Sprite? _carvedStairFaceCustom;
        private Sprite? _stairCutoutCustom;

        private const string CarvedStairFacePath = "Sprites/structures/carved_stair_face";
        private const string StairCutoutPath      = "Sprites/structures/stair_cutoutv2";
        private const string StairCutoutFallback  = "Sprites/structures/stair_cutout";
        private static readonly string[] StairCutoutStreamNames = { "stair_cutoutv2.png", "stair_cutout.png" };

        // Rim cliff sprites for void-facing outer edges (keyed by rightSide).
        private readonly Dictionary<bool, Sprite> _rimFaceSprites = new();

        // Correct face height: covers the full diagonal drop from one tier's edge to the
        // next.  The 64×32 diamond face has a half-height of 16px; the elevation step
        // contributes ElevationWorldStep*PPU = 32px; together = 48px of actual wall.
        private static readonly int TierFaceH = 16 + Mathf.RoundToInt(Constants.ElevationWorldStep * Constants.PixelsPerUnit);
        // Island underside depth: 1.5 world units of rocky cliff below the surface.
        private const int RimFaceH = 96;

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        private void OnEnable()  => EventBus.Subscribe<StairsCarvedEvent>(OnStairsCarved);
        private void OnDisable() => EventBus.Unsubscribe<StairsCarvedEvent>(OnStairsCarved);

        private void OnStairsCarved(StairsCarvedEvent _) => RefreshTierWalls();

        /// <summary>
        /// Build the full island visual from scratch.
        /// Called by Bootstrap / GameManager after Generate().
        /// </summary>
        public void Render(IslandData island)
        {
            _island = island;
            StairCutoutLayout.Load();
            DestroyAll();

            // Build underlay sprite once at Render time using current config.
            var cfg = VisualConfig.Current;
            _underlaySprite = ProcGfx.IsoDiamondUnderlay(
                cfg.TileUnderlayColor,
                w: 68, h: 34,
                scaleUp: cfg.tileUnderlayScale);

            foreach (var kvp in island.Cells)
                BuildCellVisuals(kvp.Value);

            // After all cells exist, compute + attach blend feathers for every cell.
            RefreshAllBlendOverlays();

            // Two-tier cliff walls (raised forge tier dropping to the lower farm).
            BuildTierWalls();

            // Atmospheric depth: soft shadow disc below the island's underside.
            AddIslandShadow(island);
        }

        // ---- Two-tier cliff walls ----

        /// <summary>
        /// Draw a cliff face on each diamond edge whose camera-facing neighbour is a
        /// lower tier, so the raised tier connects down to the lower one edge-to-edge.
        /// The high side of a registered stair edge gets the carved-stair face only
        /// after <see cref="IslandData.StairsCarved"/>; until then it shows as a solid wall.
        /// Also draws tall rocky rim faces on every void-facing outer edge so the
        /// island reads as a floating chunk of land.
        /// </summary>
        private void BuildTierWalls()
        {
            if (_island == null) return;

            foreach (var kvp in _island.Cells)
            {
                var cell = kvp.Value;
                Vector2Int pos = cell.GridPos;
                int myTier = _island.Tier(pos);

                // Down-LEFT (SW) edge toward the +y neighbour.
                var sw = pos + new Vector2Int(0, 1);
                if (_island.IsValidPosition(sw))
                {
                    if (myTier > _island.Tier(sw))
                    {
                        if (ShowCarvedStairFace(pos, sw))
                            AddCarvedStair(cell, rightSide: false);
                        else
                            AddTierFace(cell, stair: false, rightSide: false);
                    }
                }
                else
                {
                    AddRimFace(cell, rightSide: false);
                }

                // Down-RIGHT (SE) edge toward the +x neighbour.
                var se = pos + new Vector2Int(1, 0);
                if (_island.IsValidPosition(se))
                {
                    if (myTier > _island.Tier(se))
                    {
                        if (ShowCarvedStairFace(pos, se))
                            AddCarvedStair(cell, rightSide: true);
                        else
                            AddTierFace(cell, stair: false, rightSide: true);
                    }
                }
                else
                {
                    AddRimFace(cell, rightSide: true);
                }
            }

            EnsureStairCellMasked();
        }

        private bool ShowCarvedStairFace(Vector2Int high, Vector2Int low) =>
            _island != null && _island.StairsCarved && _island.IsStairEdge(high, low);

        /// <summary>Rebuild cliff/stair faces after the tutorial mine (wall → steps).</summary>
        public void RefreshTierWalls()
        {
            RestoreStairCellSurfaces();
            ClearTierFaces();
            _faceSprites.Clear();
            _rimFaceSprites.Clear();
            _carvedStairSw = null;
            _carvedStairSe = null;
            _stairTrimTowardY = null;
            _stairTrimTowardX = null;
            _carvedStairFaceCustom = null;
            _stairCutoutCustom = null;
            StairCutoutLayout.Load();
            BuildTierWalls();
        }

        /// <summary>Live cutout transform for the in-game layout editor.</summary>
        public Transform? GetStairCutoutTransform()
        {
            if (!_visuals.TryGetValue(StarterIsland.BackStairCell, out var vis)) return null;
            var t = vis.Root.transform.Find("StairCutout");
            return t != null ? t : null;
        }

        private void RestoreStairCellSurfaces()
        {
            if (_island == null) return;
            foreach (var kvp in _visuals)
            {
                var vis = kvp.Value;
                vis.Tile.enabled = true;
                vis.BlendRoot.gameObject.SetActive(true);
                var underlay = vis.Root.transform.Find("Underlay")?.GetComponent<SpriteRenderer>();
                if (underlay != null) underlay.enabled = true;
                var cell = _island.GetCell(kvp.Key);
                if (cell != null) UpdateOverlay(vis, cell);
            }
        }

        private void HideStairCellSurfaceImpl(Vector2Int pos)
        {
            if (!_visuals.TryGetValue(pos, out var vis)) return;
            vis.Tile.enabled = false;
            vis.Overlay.enabled = false;
            vis.BlendRoot.gameObject.SetActive(false);
            var underlay = vis.Root.transform.Find("Underlay")?.GetComponent<SpriteRenderer>();
            if (underlay != null) underlay.enabled = false;
        }

        /// <summary>Mask the upper stair cell whenever carved stairs use the PNG cutout.</summary>
        private void EnsureStairCellMasked()
        {
            if (_island == null || !_island.StairsCarved) return;
            if (LoadStairCutoutTexture() == null) return;
            HideStairCellSurfaceImpl(StarterIsland.BackStairCell);
        }

        private void ClearTierFaces()
        {
            foreach (var vis in _visuals.Values)
            {
                if (vis.Root == null) continue;
                var t = vis.Root.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                {
                    var child = t.GetChild(i);
                    var n = child.name;
                    if (n == "WallFace" || n == "StairFace" || n == "StairCutout" || n == "StairTileTrim" || n == "RimFace")
                        Destroy(child.gameObject);
                }
            }
        }

        private Sprite FaceSprite(bool rightSide)
        {
            var key = (false, rightSide);
            if (_faceSprites.TryGetValue(key, out var sp)) return sp;

            sp = ProcGfx.IsoTierFace(new Color(0.44f, 0.40f, 0.36f), new Color(0.16f, 0.14f, 0.12f), TierFaceH, false, rightSide);
            _faceSprites[key] = sp;
            return sp;
        }

        private Sprite? TryLoadStairCutout()
        {
            if (_stairCutoutCustom != null) return _stairCutoutCustom;

            var tex = LoadStairCutoutTexture();
            if (tex == null) return null;

            tex.filterMode = FilterMode.Point;
            float ppu = tex.height / StairCutoutLayout.Current.heightWorld;
            _stairCutoutCustom = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0f),
                ppu);
            return _stairCutoutCustom;
        }

        private static Texture2D? LoadStairCutoutTexture()
        {
            foreach (var name in StairCutoutStreamNames)
            {
                string streamPath = Path.Combine(Application.streamingAssetsPath, name);
                var tex = LoadCutoutFromFile(streamPath);
                if (tex != null) return tex;
            }

            return Resources.Load<Texture2D>(StairCutoutPath)
                ?? Resources.Load<Texture2D>(StairCutoutFallback);
        }

        private static Texture2D? LoadCutoutFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp,
                };
                return tex.LoadImage(bytes) ? tex : null;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[IslandRenderer] Cutout load failed ({path}): {e.Message}");
                return null;
            }
        }

        private Sprite? TryLoadCarvedStairFaceCustom(bool rightSide)
        {
            if (rightSide) return null;
            if (_carvedStairFaceCustom != null) return _carvedStairFaceCustom;

            var tex = LoadStairFaceTexture();
            if (tex == null) return null;

            tex.filterMode = FilterMode.Point;
            // SW tier face — same pivot as ProcGfx.IsoTierFace (rightSide: false).
            _carvedStairFaceCustom = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(1f, 1f),
                Constants.PixelsPerUnit);
            return _carvedStairFaceCustom;
        }

        /// <summary>StreamingAssets override (no rebuild) wins over Resources.</summary>
        private static Texture2D? LoadStairFaceTexture()
        {
            string streamPath = Path.Combine(Application.streamingAssetsPath, "carved_stair_face.png");
            if (File.Exists(streamPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(streamPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode   = TextureWrapMode.Clamp,
                    };
                    if (tex.LoadImage(bytes))
                        return tex;
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"[IslandRenderer] StreamingAssets stair face load failed: {e.Message}");
                }
            }

            return Resources.Load<Texture2D>(CarvedStairFacePath);
        }

        private Sprite CarvedStairFaceSprite(bool rightSide)
        {
            if (rightSide)
            {
                if (_carvedStairSe != null) return _carvedStairSe;
                _carvedStairSe = ProcGfx.IsoCarvedStairFace(
                    new Color(0.46f, 0.43f, 0.39f), new Color(0.18f, 0.16f, 0.14f),
                    new Color(0.55f, 0.51f, 0.46f), TierFaceH, rightSide: true);
                return _carvedStairSe;
            }

            if (_carvedStairSw != null) return _carvedStairSw;
            _carvedStairSw = ProcGfx.IsoCarvedStairFace(
                new Color(0.46f, 0.43f, 0.39f), new Color(0.18f, 0.16f, 0.14f),
                new Color(0.55f, 0.51f, 0.46f), TierFaceH, rightSide: false);
            return _carvedStairSw;
        }

        private Sprite StairTileTrimSprite(bool rightSide)
        {
            if (rightSide)
            {
                if (_stairTrimTowardX != null) return _stairTrimTowardX;
                _stairTrimTowardX = ProcGfx.IsoStairTileTrim(new Color(0.46f, 0.43f, 0.39f), towardPlusY: false);
                return _stairTrimTowardX;
            }

            if (_stairTrimTowardY != null) return _stairTrimTowardY;
            _stairTrimTowardY = ProcGfx.IsoStairTileTrim(new Color(0.46f, 0.43f, 0.39f), towardPlusY: true);
            return _stairTrimTowardY;
        }

        private Sprite RimFaceSprite(bool rightSide)
        {
            if (_rimFaceSprites.TryGetValue(rightSide, out var sp)) return sp;
            // Dark rocky cliff — lighter near the top, near-black at the base.
            sp = ProcGfx.IsoTierFace(
                new Color(0.30f, 0.27f, 0.24f),
                new Color(0.08f, 0.06f, 0.05f),
                RimFaceH, false, rightSide);
            _rimFaceSprites[rightSide] = sp;
            return sp;
        }

        // Y offset applied to both tier and rim faces so the face top aligns with the
        // camera-facing diamond edge (which sits 0.25 world units above the cell pivot).
        private static readonly float FaceYOffset = 16f / Constants.PixelsPerUnit;

        private void AddTierFace(IslandCell cell, bool stair, bool rightSide)
        {
            if (!_visuals.TryGetValue(cell.GridPos, out var vis)) return;

            var go = new GameObject("WallFace");
            go.transform.SetParent(vis.Root.transform);
            go.transform.localPosition = new Vector3(0f, FaceYOffset, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FaceSprite(rightSide);
            sr.sortingOrder = vis.SortBase + 1;
        }

        /// <summary>
        /// Carved stair on the middle upper cell: authored cutout PNG seated in the tier gap,
        /// flanked by normal cliff walls on (0,1) and (2,1). Falls back to procedural cliff face.
        /// </summary>
        private void AddCarvedStair(IslandCell cell, bool rightSide)
        {
            if (!_visuals.TryGetValue(cell.GridPos, out var vis)) return;

            if (!rightSide && TryLoadStairCutout() is Sprite cutout)
            {
                HideStairCellSurfaceImpl(cell.GridPos);

                var cutGo = new GameObject("StairCutout");
                cutGo.transform.SetParent(vis.Root.transform);
                var cutSr = cutGo.AddComponent<SpriteRenderer>();
                cutSr.sprite       = cutout;
                cutSr.sortingOrder = vis.SortBase + 1;
                StairCutoutLayout.ApplyTransform(cutGo.transform);
                return;
            }

            var customFace = TryLoadCarvedStairFaceCustom(rightSide);

            var faceGo = new GameObject("StairFace");
            faceGo.transform.SetParent(vis.Root.transform);
            faceGo.transform.localPosition = new Vector3(0f, FaceYOffset, 0f);
            var faceSr = faceGo.AddComponent<SpriteRenderer>();
            faceSr.sprite       = customFace ?? CarvedStairFaceSprite(rightSide);
            faceSr.sortingOrder = vis.SortBase + 1;

            if (customFace == null)
            {
                var trimGo = new GameObject("StairTileTrim");
                trimGo.transform.SetParent(vis.Root.transform);
                trimGo.transform.localPosition = Vector3.zero;
                var trimSr = trimGo.AddComponent<SpriteRenderer>();
                trimSr.sprite       = StairTileTrimSprite(rightSide);
                trimSr.sortingOrder = vis.SortBase + 2600;
            }
        }

        private void AddRimFace(IslandCell cell, bool rightSide)
        {
            if (!_visuals.TryGetValue(cell.GridPos, out var vis)) return;

            var go = new GameObject("RimFace");
            go.transform.SetParent(vis.Root.transform);
            go.transform.localPosition = new Vector3(0f, FaceYOffset, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RimFaceSprite(rightSide);
            sr.sortingOrder = vis.SortBase + 1;
        }

        private static void AddIslandShadow(IslandData island)
        {
            // Sum world positions to find centroid.
            var sum = Vector2.zero;
            int n = 0;
            float minY = float.MaxValue;
            foreach (var cell in island.Cells.Values)
            {
                var wp = GridMath.GridToWorld(cell.GridPos, cell.Elevation);
                sum += wp;
                if (wp.y < minY) minY = wp.y;
                n++;
            }
            if (n == 0) return;
            var centre = sum / n;

            var go = new GameObject("IslandShadow");
            // Position the shadow below the island's lowest visible point.
            go.transform.position = new Vector3(centre.x, minY - 0.8f, 0f);
            // Squash into a wide, flat oval that reads as the island's underbelly.
            go.transform.localScale = new Vector3(4.0f, 0.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProcGfx.SoftDisc(new Color(0.04f, 0.03f, 0.03f, 0.50f), 64, 2.0f);
            sr.sortingOrder = -20000;  // behind all terrain
        }

        /// <summary>
        /// Add visuals for cells added at runtime via <see cref="IslandExpansion.Expand"/>.
        /// Also refreshes blend overlays for all 8 neighbours of each new cell so
        /// existing border tiles update their feathers correctly.
        /// </summary>
        public void RenderNewCells(IEnumerable<IslandCell> newCells)
        {
            if (_island == null) return;

            // Ensure underlay sprite exists (may not if Render was never called, e.g. first boot)
            if (_underlaySprite == null)
            {
                var cfg = VisualConfig.Current;
                _underlaySprite = ProcGfx.IsoDiamondUnderlay(
                    cfg.TileUnderlayColor,
                    w: 68, h: 34,
                    scaleUp: cfg.tileUnderlayScale);
            }

            var newPositions = new List<Vector2Int>();
            foreach (var cell in newCells)
            {
                BuildCellVisuals(cell);
                newPositions.Add(cell.GridPos);
            }

            // Refresh blend overlays for every newly added cell and all their neighbours
            var toRefresh = new HashSet<Vector2Int>();
            foreach (var pos in newPositions)
                foreach (var affected in TerrainAutotiler.AffectedPositions(pos))
                    toRefresh.Add(affected);

            foreach (var pos in toRefresh)
                RefreshBlendOverlaysForCell(pos);
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

            // ---- dark-earth underlay (seam sealer) ----
            // Sorting order well BELOW terrain (-11000) so it sits under the tile.
            var underlayGo = new GameObject("Underlay");
            underlayGo.transform.SetParent(go.transform);
            underlayGo.transform.localPosition = Vector3.zero;
            var underlaySr = underlayGo.AddComponent<SpriteRenderer>();
            underlaySr.sortingOrder = sortBase - 1000;   // -10000-1000 = -11000
            underlaySr.sprite = _underlaySprite;
            // No tint — pure dark fill.

            // ---- terrain SpriteRenderer ----
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortBase;

            int variant  = CellHashVariant(cell.GridPos, TerrainProperties.VariantCount(cell.Terrain));
            string path  = TerrainProperties.TilePath(cell.Terrain);

            sr.color = WarmTint(cell.Terrain);

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

            // ---- blend overlay container (children added by RefreshBlendOverlaysForCell) ----
            var blendRoot = new GameObject("BlendOverlays");
            blendRoot.transform.SetParent(go.transform);
            blendRoot.transform.localPosition = Vector3.zero;

            var vis = new CellVisuals
            {
                Root       = go,
                Tile       = sr,
                Overlay    = overlaySr,
                BlendRoot  = blendRoot,
                SortBase   = sortBase,
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

        // ---- Autotile blend overlay management ----

        private void RefreshAllBlendOverlays()
        {
            if (_island == null) return;
            foreach (var pos in _visuals.Keys)
                RefreshBlendOverlaysForCell(pos);
        }

        private void RefreshBlendOverlaysForCell(Vector2Int pos)
        {
            if (_island == null) return;
            if (!_visuals.TryGetValue(pos, out var vis)) return;
            var cell = _island.GetCell(pos);
            if (cell == null) return;

            // Destroy existing blend overlay children
            var blendRoot = vis.BlendRoot;
            int childCount = blendRoot.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = blendRoot.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            var blendInfo = TerrainAutotiler.Compute(cell, _island);
            if (!blendInfo.HasAnyBlend) return;

            var cfg = VisualConfig.Current;
            float masterAlpha = cfg.blendFeatherAlpha;
            float falloff     = cfg.blendFeatherFalloff;

            // Blend overlays sort just ABOVE the terrain tile (-10000 + 500 = -9500)
            int blendSortBase = vis.SortBase + 500;

            for (int i = 0; i < blendInfo.Samples.Count; i++)
            {
                var sample = blendInfo.Samples[i];

                // Tint: lerp between the cell's own terrain colour and the neighbour's colour.
                // If the neighbour is absent (off-island), blend toward a very dark edge colour.
                Color neighbourColor = sample.NeighbourTerrain.HasValue
                    ? TerrainBlendColor(sample.NeighbourTerrain.Value)
                    : new Color(0.05f, 0.04f, 0.02f, 1f);

                Color featherColor = new Color(
                    neighbourColor.r,
                    neighbourColor.g,
                    neighbourColor.b,
                    masterAlpha * sample.Weight);

                var featherSprite = ProcGfx.IsoEdgeFeather(
                    featherColor,
                    sample.WorldDirection,
                    w: 64, h: 32,
                    falloff: falloff);

                var featherGo = new GameObject($"Blend_{i}");
                featherGo.transform.SetParent(blendRoot.transform);
                featherGo.transform.localPosition = Vector3.zero;

                var featherSr = featherGo.AddComponent<SpriteRenderer>();
                featherSr.sprite = featherSprite;
                featherSr.sortingOrder = blendSortBase + i;
                featherSr.color = Color.white; // colour is baked into the texture
            }
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
                // Soil overlays are flat 64×32 diamonds and must share the terrain FACE pivot
                // (0.5, 0 = bottom tip) so they sit ON the tile. LoadTile uses a center pivot
                // (TilePivot) meant for the tall 64×80 tiles, which floated the overlay up off-grid.
                return SpriteLoader.Load(path);
            }
            catch { return null; }
        }

        /// <summary>Per-terrain warm tint so the palette reads cozy earth, not cold grey.
        /// Fertile soil gets the full warm nudge; rock/cliff a gentler one; springs stay cool.</summary>
        private static Color WarmTint(TerrainType terrain)
        {
            var cfg = SkyHarvest.Core.VisualConfig.Current;
            float strength = cfg.earthTintStrength;
            switch (terrain)
            {
                case TerrainType.FertileValley: break;                 // full warmth
                case TerrainType.RockyPlateau:
                case TerrainType.WindCorridor:  strength *= 0.5f; break;
                case TerrainType.CliffEdge:     strength *= 0.65f; break;
                case TerrainType.NaturalSpring: strength *= 0.2f; break;
                default: strength *= 0.5f; break;
            }
            return Color.Lerp(Color.white, cfg.WarmEarthTint, strength);
        }

        /// <summary>
        /// Base blend colour for terrain transitions (mid-tone between the two terrain palettes).
        /// These are representative mid-tones of each terrain's visual palette.
        /// </summary>
        private static Color TerrainBlendColor(TerrainType terrain) => terrain switch
        {
            TerrainType.FertileValley => new Color(0.45f, 0.60f, 0.28f),   // warm green
            TerrainType.RockyPlateau  => new Color(0.52f, 0.47f, 0.38f),   // dusty stone
            TerrainType.CliffEdge     => new Color(0.35f, 0.30f, 0.25f),   // dark stone
            TerrainType.NaturalSpring => new Color(0.35f, 0.55f, 0.65f),   // cool aqua
            TerrainType.WindCorridor  => new Color(0.55f, 0.53f, 0.42f),   // pale straw
            TerrainType.Scaffold      => new Color(0.48f, 0.36f, 0.20f),   // timber
            _                         => new Color(0.40f, 0.38f, 0.30f),
        };

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
            public GameObject Root      = null!;
            public SpriteRenderer Tile  = null!;
            public SpriteRenderer Overlay = null!;
            public GameObject BlendRoot = null!;
            public int SortBase;
        }
    }
}
