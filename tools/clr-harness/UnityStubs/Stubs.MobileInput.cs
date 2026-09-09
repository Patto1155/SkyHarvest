// Harness stub additions for the phone-first input/HUD work (tap-to-move, portrait scaler).
// CanvasScaler lives in the UnityEngine namespace in Stubs.UI.cs (not UnityEngine.UI), so this
// partial must match that namespace or the game code sees an ambiguous reference.
namespace UnityEngine
{
    public partial class CanvasScaler
    {
        public ScreenMatchMode screenMatchMode { get; set; }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
    }
}
