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
    }
}
