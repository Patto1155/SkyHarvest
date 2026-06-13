// Assets/Scripts/Core/ProcGfx.cs
// Owned by: UI/bootstrap agent
//
// Tiny procedural sprite generators for the cozy/warm pass — gradient sky, soft
// radial glow puffs (forge/lantern light, clouds), and the avatar drop shadow.
// Generating these in code keeps the warmth pass asset-free (nothing to import,
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
    }
}
