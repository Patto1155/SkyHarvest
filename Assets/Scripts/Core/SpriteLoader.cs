using UnityEngine;

namespace SkyHarvest.Core
{
    public static class SpriteLoader
    {
        private static Sprite? _fallback;

        private static Sprite Fallback()
        {
            if (_fallback != null) return _fallback;
            var tex = new Texture2D(1, 1);
            tex.SetPixels32(new[] { new Color32(255, 0, 255, 255) });
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _fallback = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f),
                Constants.PixelsPerUnit);
            return _fallback;
        }

        public static Sprite Load(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return Fallback();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f),
                Constants.PixelsPerUnit);
        }

        public static Sprite[] LoadStrip(string path, int frameW) =>
            LoadStrip(path, frameW, new Vector2(0.5f, 0f));

        public static Sprite[] LoadStrip(string path, int frameW, Vector2 pivot)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return new[] { Fallback() };
            tex.filterMode = FilterMode.Point;

            int frames = tex.width / frameW;
            if (frames < 1) frames = 1;
            var sprites = new Sprite[frames];
            for (int i = 0; i < frames; i++)
                sprites[i] = Sprite.Create(tex,
                    new Rect(i * frameW, 0, frameW, tex.height), pivot, Constants.PixelsPerUnit);
            return sprites;
        }

        public static Sprite LoadTile(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return Fallback();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                TilePivot(tex.height), Constants.PixelsPerUnit);
        }

        /// <summary>Diamond-top-center pivot for isometric terrain tiles.</summary>
        public static Vector2 TilePivot(int height) =>
            new Vector2(0.5f, height > 16 ? (height - 16f) / height : 0.5f);

        /// <summary>
        /// Horizontal strip of terrain frames cropped to the top diamond face only (64×32 px).
        /// Full 64×80 art includes a cliff skirt; grid spacing matches the 1.0×0.5 world-unit
        /// diamond, so only the face is used for per-cell tiling. Pivot = bottom tip of diamond.
        /// </summary>
        public static Sprite[] LoadTerrainStrip(string path, int frameW)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return new[] { Fallback() };
            tex.filterMode = FilterMode.Point;

            int faceH = 32;
            int y0 = Mathf.Max(0, tex.height - faceH);
            int frames = tex.width / frameW;
            if (frames < 1) frames = 1;

            var pivot = new Vector2(0.5f, 0f);
            var sprites = new Sprite[frames];
            for (int i = 0; i < frames; i++)
                sprites[i] = Sprite.Create(tex,
                    new Rect(i * frameW, y0, frameW, faceH), pivot, Constants.PixelsPerUnit);
            return sprites;
        }
    }
}
