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
            _fallback = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f));
            return _fallback;
        }

        public static Sprite Load(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return Fallback();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f));
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
                    new Rect(i * frameW, 0, frameW, tex.height), pivot);
            return sprites;
        }

        public static Sprite LoadTile(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return Fallback();
            tex.filterMode = FilterMode.Point;
            // Pivot at diamond-top-center: x=0.5, y=(height-16)/height
            float py = tex.height > 16 ? (tex.height - 16f) / tex.height : 0.5f;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, py));
        }
    }
}
