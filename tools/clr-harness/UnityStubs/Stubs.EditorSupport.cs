// UnityEngine stub additions used by Assets/Editor scripts (compile-checked via
// clr-harness/EditorCode). Overloads only — the base declarations live in Stubs.Core.cs /
// Stubs.Rendering.cs and are not touched here (see CONVENTIONS §Verification harness).
namespace UnityEngine
{
    public partial class Object
    {
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object =>
            System.Array.Empty<T>();
    }

    public partial class Component
    {
        public T? GetComponentInChildren<T>(bool includeInactive) where T : Component =>
            GetComponentInChildren<T>();
    }

    public partial class GameObject
    {
        public T? GetComponentInChildren<T>(bool includeInactive) where T : Component =>
            GetComponentInChildren<T>();
    }

    public partial class Texture2D
    {
        /// <summary>Real API decodes PNG/JPG bytes; the harness only needs the signature.</summary>
        public bool LoadImage(byte[] data) => true;

        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors) { }
    }
}
