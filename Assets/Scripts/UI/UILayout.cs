// Portrait-first UI layout helpers (v2 spec §1 pillar 5).
// Bootstrap builds every panel in code, so anchoring lives here rather than in a scene file.
// The canvas reference is portrait 720×1280; HUD elements anchor to screen edges so they stay
// put on any aspect, and panels stay centred.
using UnityEngine;

namespace SkyHarvest.UI
{
    public static class UILayout
    {
        public const float RefWidth  = 720f;
        public const float RefHeight = 1280f;

        /// <summary>Side margin kept clear of the screen edge (thumb reach + rounded corners).</summary>
        public const float Margin = 24f;

        /// <summary>
        /// Slot pitch that fits <paramref name="count"/> slots across <paramref name="availableWidth"/>.
        /// Prefers slotSize+gap, shrinking toward slotSize (overlap) only when it cannot fit.
        /// </summary>
        public static float HotbarSpacing(int count, float slotSize, float gap, float availableWidth)
        {
            if (count <= 1) return slotSize + gap;
            float preferred = slotSize + gap;
            float maxPitch  = (availableWidth - slotSize) / (count - 1);
            return Mathf.Max(Mathf.Min(preferred, maxPitch), 1f);
        }

        /// <summary>Centred X offset of slot <paramref name="index"/> for a given pitch.</summary>
        public static float SlotOffsetX(int index, int count, float spacing) =>
            (index - (count - 1) / 2f) * spacing;

        // ---- anchoring helpers -------------------------------------------------
        // Each sets anchor+pivot so the offset is measured from that screen edge.

        public static void AnchorBottomCenter(RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        public static void AnchorTopLeft(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public static void AnchorTopRight(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-x, -y);
        }

        public static void AnchorTopCenter(RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
        }

        public static void AnchorCenter(RectTransform rt, float x = 0f, float y = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Stretch to fill the parent (backgrounds).</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
