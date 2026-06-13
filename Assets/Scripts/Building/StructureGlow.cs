// Assets/Scripts/Building/StructureGlow.cs
// Owned by: building agent
//
// Warm light pool for light-emitting structures (forge fire, shelter lantern).
// A soft amber disc parented at the structure's base with a gentle flicker — the
// single biggest "cozy" lever per the design's "dark earth + warm light" palette.
// Colour / alpha / radius / flicker all come from VisualConfig (visual.json).
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Building
{
    public class StructureGlow : MonoBehaviour
    {
        private SpriteRenderer _sr = null!;
        private float _baseAlpha;
        private float _pulse;
        private Color _rgb;
        private float _seed;

        /// <summary>Which structures glow, and how strongly (0 = none). Forge fire is brightest.</summary>
        public static float Intensity(string structureId) => structureId switch
        {
            "forge"       => 1.25f,
            "shelter"     => 0.8f,
            "drying_rack" => 0.45f,   // small embers / drying warmth
            _             => 0f,
        };

        public static void Attach(GameObject structureGo, int structureSorting, float intensity)
        {
            if (intensity <= 0f) return;
            var cfg = VisualConfig.Current;

            var go = new GameObject("Glow");
            go.transform.SetParent(structureGo.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.3f, 0f);   // pool around the base/body
            float r = cfg.glowRadius * intensity;
            go.transform.localScale = new Vector3(r, r * 0.8f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProcGfx.SoftDisc(cfg.GlowColor, 128, 1.8f);
            sr.sortingOrder = structureSorting - 1;   // sits just under the structure sprite

            var glow = go.AddComponent<StructureGlow>();
            glow._sr        = sr;
            glow._rgb       = cfg.GlowColor;
            glow._baseAlpha = cfg.GlowColor.a * intensity;
            glow._pulse     = cfg.glowPulse;
            glow._seed      = Random.value * 10f;
        }

        private void Update()
        {
            if (_sr == null) return;
            // Gentle Perlin flicker around the base alpha (fire-like, not strobing).
            float n = Mathf.PerlinNoise(_seed + Time.time * 3.5f, 0f) - 0.5f;
            float a = Mathf.Clamp01(_baseAlpha * (1f + _pulse * 2f * n));
            _sr.color = new Color(_rgb.r, _rgb.g, _rgb.b, a);
        }
    }
}
