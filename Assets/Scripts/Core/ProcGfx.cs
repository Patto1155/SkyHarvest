// Assets/Scripts/Core/ProcGfx.cs
// Owned by: UI/bootstrap agent
//
// Tiny procedural sprite generators for the cozy/warm pass — gradient sky, soft
// radial glow puffs (forge/lantern light, clouds), the avatar drop shadow, and
// autotile terrain blend feathers (IsoEdgeFeather / IsoDiamondUnderlay).
// Generating these in code keeps the visual pass asset-free (nothing to import,
// nothing to commit but C#) and fully driven by VisualConfig colours.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Core
{
    public static class ProcGfx
    {
        /// <summary>Vertical two-stop gradient sprite (top → bottom). Used for the sky backdrop.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom, int height = 256)
        {
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);          // 0 bottom → 1 top
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Soft radial puff: opaque-ish core fading to fully transparent at the rim.
        /// One sprite serves glows (additive amber), clouds (low-alpha slate) and shadows (dark).</summary>
        public static Sprite SoftDisc(Color color, int size = 128, float falloff = 2.2f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - r) / r;
                float dy = (y + 0.5f - r) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);        // 0 centre → 1 rim
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, falloff);                       // soft edge
                px[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Autotile terrain helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A solid-fill isometric diamond at pixel size w×h.
        /// Used as the dark-earth underlay rendered UNDER every terrain tile to
        /// plug the sky-gap seams that appear between tiles.
        /// The pivot is (0.5, 0) = bottom tip, matching the terrain tile convention.
        /// <paramref name="scaleUp"/> makes the diamond slightly larger than 1:1 so
        /// adjacent underlays overlap and fully seal the gap (recommend ~1.05).
        /// </summary>
        public static Sprite IsoDiamondUnderlay(Color fill, int w = 68, int h = 34, float scaleUp = 1.0f)
        {
            int tw = Mathf.RoundToInt(w * scaleUp);
            int th = Mathf.RoundToInt(h * scaleUp);
            // Ensure even dimensions for clean diamond edges
            if (tw % 2 != 0) tw++;
            if (th % 2 != 0) th++;

            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[tw * th];
            float cx = tw * 0.5f;
            float cy = th * 0.5f;

            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                // Normalised position in diamond space: |u|+|v| ≤ 1 is inside
                float u = (x + 0.5f - cx) / (tw * 0.5f);
                float v = (y + 0.5f - cy) / (th * 0.5f);
                bool inside = (Mathf.Abs(u) + Mathf.Abs(v)) <= 1.0f;
                px[y * tw + x] = inside ? fill : Color.clear;
            }
            tex.SetPixels(px);
            tex.Apply();
            // Pivot (0.5, 0) = bottom tip
            return Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0f), Constants.PixelsPerUnit);
        }

        /// <summary>
        /// A directional feather overlay for terrain blending.
        /// The sprite is a diamond-shaped mask (w×h iso pixel size) where pixels
        /// fade from <paramref name="featherColor"/> at the edge in direction
        /// <paramref name="worldDir"/> to transparent at the centre.  The renderer
        /// tints this toward the neighbour terrain colour.
        ///
        /// worldDir is the normalised 2-D world-space direction toward the neighbour.
        /// The falloff sharpness is controlled by <paramref name="falloff"/>.
        /// </summary>
        public static Sprite IsoEdgeFeather(
            Color featherColor,
            Vector2 worldDir,
            int w = 64, int h = 32,
            float falloff = 1.8f)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[w * h];
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            // Normalise direction (safety)
            float dmag = Mathf.Sqrt(worldDir.x * worldDir.x + worldDir.y * worldDir.y);
            if (dmag < 1e-5f) worldDir = new Vector2(1, 0);
            else { worldDir.x /= dmag; worldDir.y /= dmag; }

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Normalised iso-diamond position
                float u = (x + 0.5f - cx) / (w * 0.5f);   // -1..1 horizontal
                float v = (y + 0.5f - cy) / (h * 0.5f);   // -1..1 vertical

                // Only paint inside the diamond mask
                if (Mathf.Abs(u) + Mathf.Abs(v) > 1.0f)
                {
                    px[y * w + x] = Color.clear;
                    continue;
                }

                // Dot product of iso-pixel position with the world direction gives
                // how far this pixel is "toward" the neighbour edge.
                // u is horizontal in world-ish space; v needs halving for iso aspect.
                float dot = u * worldDir.x + v * 0.5f * worldDir.y;

                // dot: -1 = far side, +1 = near edge toward neighbour
                // We want alpha to be 1 at the near edge and 0 at the centre
                float t = Mathf.Clamp01((dot + 1f) * 0.5f);   // remap to [0,1]
                float a = Mathf.Pow(t, falloff);

                px[y * w + x] = new Color(featherColor.r, featherColor.g, featherColor.b, featherColor.a * a);
            }
            tex.SetPixels(px);
            tex.Apply();
            // Pivot (0.5, 0) = bottom tip, matching terrain tile convention
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), Constants.PixelsPerUnit);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Two-tier cliff wall
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// One directional cliff face on a single diamond edge — the wall between a
        /// raised tile and the lower tile on one of its two camera-facing sides.
        /// <paramref name="rightSide"/> false = the down-LEFT (SW) edge toward the +y
        /// neighbour; true = the down-RIGHT (SE) edge toward the +x neighbour.
        ///
        /// The face is a parallelogram: its top edge is the diamond's lower edge
        /// (sloping half a tile over the half-width) and it extrudes straight down by
        /// <paramref name="faceHeightPx"/> = ElevationWorldStep × PixelsPerUnit, which
        /// makes it land exactly on the neighbouring lower tile's matching edge.
        /// Pivot is the cell centre: (1,1) top-right for SW, (0,1) top-left for SE.
        /// </summary>
        public static Sprite IsoTierFace(Color faceTop, Color faceBottom, int faceHeightPx, bool stair, bool rightSide)
        {
            const int half = 16;
            const int halfW = 32;
            int H = half + faceHeightPx;
            var tex = new Texture2D(halfW, H, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var px = new Color[halfW * H];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            for (int x = 0; x < halfW; x++)
            {
                float frac = (float)x / (halfW - 1);
                float dTop = (rightSide ? (1f - frac) : frac) * half;
                for (int b = 0; b < faceHeightPx; b++)
                {
                    int yTex = (H - 1) - Mathf.RoundToInt(dTop + b);
                    if (yTex < 0 || yTex >= H) continue;
                    float t = (float)b / Mathf.Max(1, faceHeightPx - 1);
                    Color c = Color.Lerp(faceTop, faceBottom, t);

                    if (stair)
                    {
                        int stepH = Mathf.Max(5, faceHeightPx / 4);
                        int within = b % stepH;
                        if (within == 0)              c = Color.Lerp(c, new Color(0.72f, 0.62f, 0.48f), 0.55f);
                        else if (within == stepH - 1) c = Color.Lerp(c, new Color(0.12f, 0.10f, 0.08f), 0.45f);
                        // Vertical timber plank seams on the tread face
                        if (x % 6 == 0) c = Color.Lerp(c, Color.black, 0.12f);
                    }
                    else
                    {
                        // Rock texture: value noise + strata lines + edge shadow
                        float n = RockNoise(x, b, seed: 7);               // –1..1 cell-level noise
                        float fine = RockNoise(x * 3, b * 3, seed: 13);   // finer grain
                        float noise = n * 0.10f + fine * 0.05f;
                        c = new Color(
                            Mathf.Clamp01(c.r + noise),
                            Mathf.Clamp01(c.g + noise),
                            Mathf.Clamp01(c.b + noise), 1f);

                        // Horizontal rock strata — subtle dark lines every ~8px
                        if (b % 8 == 0)
                            c = Color.Lerp(c, Color.black, 0.18f);

                        // Occasional lighter surface crack highlight
                        if (b % 13 == 0 && x % 5 == 2)
                            c = Color.Lerp(c, Color.white, 0.12f);

                        // Left/right edge receive a directional shadow so face reads as 3D
                        float edgeShadow = 1f - Mathf.Pow(Mathf.Abs(frac - 0.5f) * 2f, 3f) * 0.3f;
                        c = new Color(c.r * edgeShadow, c.g * edgeShadow, c.b * edgeShadow, 1f);
                    }

                    px[yTex * halfW + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var pivot = rightSide ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            return Sprite.Create(tex, new Rect(0, 0, halfW, H), pivot, Constants.PixelsPerUnit);
        }

        // Cheap deterministic value noise in [–1, 1] for rock texture detail.
        private static float RockNoise(int x, int y, int seed)
        {
            int h = x * 374761393 ^ y * 668265263 ^ seed * unchecked((int)2246822519);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFF) / 32767.5f - 1f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cursor + tile highlight
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Terraria-style pointer: coloured fill with a white outline.
        /// Pivot is the tip (top-left corner) so the hot-spot sits on the click point.
        /// </summary>
        public static Sprite CursorPointer(Color fill, int size = 18)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            void StampArrow(int ox, int oy, Color inside, Color edge)
            {
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int sx = x - ox, sy = y - oy;
                    if (!IsCursorArrowPixel(sx, sy, size)) continue;
                    bool border =
                        !IsCursorArrowPixel(sx - 1, sy, size) ||
                        !IsCursorArrowPixel(sx + 1, sy, size) ||
                        !IsCursorArrowPixel(sx, sy - 1, size) ||
                        !IsCursorArrowPixel(sx, sy + 1, size);
                    px[y * size + x] = border ? edge : inside;
                }
            }

            // Drop shadow so the pointer reads on dark menu backgrounds.
            StampArrow(1, -1, new Color(0f, 0f, 0f, 0.85f), new Color(0f, 0f, 0f, 0.95f));
            StampArrow(0, 0, fill, Color.white);

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 1f), 100f);
        }

        private static bool IsCursorArrowPixel(int x, int y, int size)
        {
            return
                (x <= 1 && y >= size - 2) ||
                (x <= 2 && y >= size - 4 && y <= size - 2) ||
                (x <= 3 && y >= size - 6 && y <= size - 3) ||
                (x <= 4 && y >= size - 8 && y <= size - 4) ||
                (x <= 5 && y >= size - 10 && y <= size - 5) ||
                (x <= 6 && y >= size - 12 && y <= size - 6) ||
                (x <= 7 && y >= size - 14 && y <= size - 7) ||
                (x <= 8 && y >= size - 16 && y <= size - 8);
        }

        /// <summary>
        /// Soft edge glow for an isometric tile — only the diamond rim is lit,
        /// centre stays transparent so the terrain reads through.
        /// </summary>
        public static Sprite IsoDiamondEdgeGlow(Color glowColor, int w = 64, int h = 32, float edgeThickness = 0.12f)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[w * h];
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f - cx) / (w * 0.5f);
                float v = (y + 0.5f - cy) / (h * 0.5f);
                float manhattan = Mathf.Abs(u) + Mathf.Abs(v);

                if (manhattan > 1.0f)
                {
                    px[y * w + x] = Color.clear;
                    continue;
                }

                float distToEdge = 1.0f - manhattan;
                float t = Mathf.Clamp01(distToEdge / edgeThickness);
                float a = glowColor.a * Mathf.Pow(t, 1.4f);
                px[y * w + x] = new Color(glowColor.r, glowColor.g, glowColor.b, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), Constants.PixelsPerUnit);
        }
    }
}
