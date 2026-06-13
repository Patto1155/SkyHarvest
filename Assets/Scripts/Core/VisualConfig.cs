// Assets/Scripts/Core/VisualConfig.cs
// Owned by: UI/bootstrap agent
//
// Runtime-tunable visual parameters loaded from StreamingAssets/visual.json.
// The whole point: every "cozy pass" knob (sky colours, glow, earth tint, island
// size) lives in DATA, not compiled C#. Tweaking a value here costs no script
// recompile and no domain reload — edit the JSON, re-run tools/shot.sh, look.
//
// Colours are stored as "#RRGGBB" / "#RRGGBBAA" hex strings (human-editable) and
// parsed via ColorUtility. Missing file or missing keys fall back to the cozy
// defaults baked in below, so the game always runs.
using System.IO;
using UnityEngine;

namespace SkyHarvest.Core
{
    [System.Serializable]
    public class VisualConfig
    {
        // ---- island ----
        public int islandRadius = 4;            // ~4 → compact starting island (4×3-ish fertile core + cliff rim)

        // ---- sky (replaces the flat black void) ----
        public string skyTop      = "#2B3A5C";  // moody dusk blue (top of frame)
        public string skyBottom   = "#0F0E16";  // deep charcoal (bottom)
        public string cloudColor  = "#3A4559";   // soft slate cloud tint
        public float  cloudSpeed  = 0.15f;       // parallax drift, world units / sec
        public int    cloudCount  = 6;
        public float  cloudAlpha  = 0.10f;

        // ---- warm light pooling around structures ----
        public string glowColor   = "#FFB24A";   // forge / lantern amber
        public float  glowAlpha   = 0.45f;
        public float  glowRadius   = 1.6f;        // world units
        public float  glowPulse    = 0.12f;       // flicker amplitude (0 = steady)

        // ---- golden ripe-crop glow ----
        public string cropGlowColor = "#FFD66B";
        public float  cropGlowAlpha = 0.5f;
        public float  cropSwayDeg   = 3.0f;       // gentle mature-crop sway amplitude (degrees)

        // ---- terrain warmth ----
        public string warmEarthTint    = "#FFE8C8"; // multiply tint nudged onto fertile cells
        public float  earthTintStrength = 0.22f;     // 0 = no tint, 1 = full tint colour

        // ---- avatar grounding ----
        public float avatarShadowAlpha = 0.40f;

        // ─────────────────────────────────────────────────────────────────────
        // Loading
        // ─────────────────────────────────────────────────────────────────────

        private static VisualConfig? _current;

        /// <summary>Lazily-loaded singleton. First access reads visual.json.</summary>
        public static VisualConfig Current => _current ??= Load();

        /// <summary>Force a fresh read (used by the screenshot harness between shots).</summary>
        public static VisualConfig Reload() => _current = Load();

        private static string ConfigPath =>
            Path.Combine(Application.streamingAssetsPath, "visual.json");

        private static VisualConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var cfg = JsonUtility.FromJson<VisualConfig>(json);
                    if (cfg != null)
                    {
                        Debug.Log($"[VisualConfig] loaded {ConfigPath}");
                        return cfg;
                    }
                }
                else
                {
                    Debug.Log($"[VisualConfig] no {ConfigPath}, using defaults");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VisualConfig] failed to load, using defaults: {e.Message}");
            }
            return new VisualConfig();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Parsed colour accessors
        // ─────────────────────────────────────────────────────────────────────

        public Color SkyTop       => Parse(skyTop,        new Color(0.17f, 0.23f, 0.36f));
        public Color SkyBottom    => Parse(skyBottom,     new Color(0.06f, 0.05f, 0.09f));
        public Color CloudColor   => WithAlpha(Parse(cloudColor, new Color(0.23f, 0.27f, 0.36f)), cloudAlpha);
        public Color GlowColor    => WithAlpha(Parse(glowColor,  new Color(1f, 0.70f, 0.29f)), glowAlpha);
        public Color CropGlow     => WithAlpha(Parse(cropGlowColor, new Color(1f, 0.84f, 0.42f)), cropGlowAlpha);
        public Color WarmEarthTint => Parse(warmEarthTint, new Color(1f, 0.91f, 0.78f));

        /// <summary>Parse "#RRGGBB" or "#RRGGBBAA" → Color. Falls back on any malformed input.
        /// Hand-rolled (not ColorUtility) so it compiles identically under the headless CLR harness.</summary>
        private static Color Parse(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            string h = hex.TrimStart('#');
            if (h.Length != 6 && h.Length != 8) return fallback;
            try
            {
                float r = System.Convert.ToInt32(h.Substring(0, 2), 16) / 255f;
                float g = System.Convert.ToInt32(h.Substring(2, 2), 16) / 255f;
                float b = System.Convert.ToInt32(h.Substring(4, 2), 16) / 255f;
                float a = h.Length == 8 ? System.Convert.ToInt32(h.Substring(6, 2), 16) / 255f : 1f;
                return new Color(r, g, b, a);
            }
            catch { return fallback; }
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
