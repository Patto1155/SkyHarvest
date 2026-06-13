// Assets/Scripts/Core/SkyBackground.cs
// Owned by: UI/bootstrap agent
//
// Replaces the flat near-black camera clear with a moody dusk gradient + a few
// slow-drifting cloud puffs, so the island reads as floating in a SKY rather than
// a void. Parented to the camera and sized for the widest zoom, so it always fills
// the frame. All look values come from VisualConfig (StreamingAssets/visual.json).
using UnityEngine;

namespace SkyHarvest.Core
{
    public class SkyBackground : MonoBehaviour
    {
        private const int   SkySortingOrder   = -32000;  // behind everything
        private const int   CloudSortingOrder  = -20000;  // behind terrain (~-10000), in front of sky
        private const float SkyZ      = 20f;              // positive Z → behind sprites at z=0 (cam looks +Z)
        private const float SkyWidth  = 40f;              // covers max ortho zoom (size 6 → ~21 wide) + margin
        private const float SkyHeight = 24f;

        private struct Cloud { public Transform T; public float Speed; public float HalfSpan; }
        private Cloud[] _clouds = System.Array.Empty<Cloud>();

        /// <summary>Build the sky under the given camera. Call once after the camera exists.</summary>
        public static SkyBackground Attach(Camera cam, VisualConfig cfg)
        {
            var go = new GameObject("SkyBackground");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, SkyZ);
            var sky = go.AddComponent<SkyBackground>();
            sky.Build(cfg);
            return sky;
        }

        private void Build(VisualConfig cfg)
        {
            // ---- gradient backdrop ----
            var gradGo = new GameObject("SkyGradient");
            gradGo.transform.SetParent(transform, false);
            gradGo.transform.localPosition = Vector3.zero;
            var gsr = gradGo.AddComponent<SpriteRenderer>();
            gsr.sprite = ProcGfx.VerticalGradient(cfg.SkyTop, cfg.SkyBottom);
            gsr.sortingOrder = SkySortingOrder;
            // Sprite is 4×256 px @100ppu = 0.04×2.56 units → scale to cover the view.
            var sp = gsr.sprite;
            gradGo.transform.localScale = new Vector3(
                SkyWidth / sp.bounds.size.x,
                SkyHeight / sp.bounds.size.y, 1f);

            // ---- drifting clouds ----
            int n = Mathf.Clamp(cfg.cloudCount, 0, 24);
            _clouds = new Cloud[n];
            var rng = new System.Random(1234);
            var cloudSprite = ProcGfx.SoftDisc(cfg.CloudColor, 128, 1.4f);
            for (int i = 0; i < n; i++)
            {
                var cgo = new GameObject($"Cloud{i}");
                cgo.transform.SetParent(transform, false);
                float scaleX = 3f + (float)rng.NextDouble() * 4f;   // wide wispy puffs
                float scaleY = scaleX * 0.45f;
                // Spread across the upper 2/3 of the sky.
                float px = (float)(rng.NextDouble() * SkyWidth - SkyWidth / 2f);
                float py = (float)(rng.NextDouble() * (SkyHeight * 0.4f) + SkyHeight * 0.08f);
                cgo.transform.localPosition = new Vector3(px, py, -1f);
                cgo.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                var csr = cgo.AddComponent<SpriteRenderer>();
                csr.sprite = cloudSprite;
                csr.sortingOrder = CloudSortingOrder + i;
                _clouds[i] = new Cloud
                {
                    T = cgo.transform,
                    Speed = cfg.cloudSpeed * (0.6f + (float)rng.NextDouble() * 0.8f),
                    HalfSpan = SkyWidth / 2f + scaleX
                };
            }
        }

        private void Update()
        {
            for (int i = 0; i < _clouds.Length; i++)
            {
                var c = _clouds[i];
                if (c.T == null) continue;
                var p = c.T.localPosition;
                p.x += c.Speed * Time.deltaTime;
                if (p.x > c.HalfSpan) p.x = -c.HalfSpan;     // wrap
                c.T.localPosition = p;
            }
        }
    }
}
