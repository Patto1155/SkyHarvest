// UnityEngine stub — Rendering: Sprite, Texture2D, FilterMode, SpriteRenderer, Camera, Gizmos, Material
using System;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // FilterMode / TextureFormat / SpriteMeshType / SpriteDrawMode
    // -------------------------------------------------------------------------
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureFormat { RGBA32, RGB24, Alpha8, ARGB32, DXT5, ETC2_RGB, ETC2_RGBA8 }
    public enum SpriteMeshType { FullRect, Tight }
    public enum SpriteDrawMode { Simple, Sliced, Tiled }
    public enum SpriteTileMode { Continuous, Adaptive }
    public enum SpriteAlignment { Center, TopLeft, TopCenter, TopRight, LeftCenter, RightCenter, BottomLeft, BottomCenter, BottomRight, Custom }
    public enum TextureWrapMode { Repeat, Clamp, Mirror }
    public enum AnisotropicFiltering { Disable, Enable, ForceEnable }

    // -------------------------------------------------------------------------
    // Texture2D
    // -------------------------------------------------------------------------
    public partial class Texture2D : Object
    {
        private Color32[] _pixels;

        public int width { get; }
        public int height { get; }
        public FilterMode filterMode { get; set; } = FilterMode.Bilinear;
        public TextureWrapMode wrapMode { get; set; } = TextureWrapMode.Repeat;
        public bool isReadable => true;
        public TextureFormat format { get; }

        public Texture2D(int width, int height)
        {
            this.width = width; this.height = height; this.format = TextureFormat.RGBA32;
            _pixels = new Color32[width * height];
            name = $"Texture2D({width}x{height})";
        }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain)
            : this(width, height) { this.format = format; }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear)
            : this(width, height, format, mipChain) { }

        public void SetPixels32(Color32[] pixels) { _pixels = (Color32[])pixels.Clone(); }
        public void SetPixels32(Color32[] pixels, int x, int y, int w, int h, int mip = 0)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (x+i < width && y+j < height)
                        _pixels[(y+j)*width + (x+i)] = pixels[j*w+i];
        }
        public Color32[] GetPixels32(int mip = 0) => (Color32[])_pixels.Clone();
        public void SetPixels(Color[] pixels) { for (int i = 0; i < pixels.Length && i < _pixels.Length; i++) _pixels[i] = pixels[i]; }
        public Color[] GetPixels() { var r = new Color[_pixels.Length]; for (int i = 0; i < _pixels.Length; i++) r[i] = _pixels[i]; return r; }
        public void SetPixel(int x, int y, Color c) { if (x >= 0 && x < width && y >= 0 && y < height) _pixels[y*width+x] = c; }
        public Color GetPixel(int x, int y) => _pixels[Math.Clamp(y,0,height-1)*width + Math.Clamp(x,0,width-1)];
        public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false) { }
        public void Compress(bool highQuality) { }
        public byte[] EncodeToPNG() => Array.Empty<byte>();
        public byte[] EncodeToJPG(int quality = 75) => Array.Empty<byte>();
        public bool LoadImage(byte[] data) => data != null && data.Length > 0;
        public static Texture2D? whiteTexture { get; } = new Texture2D(4, 4);
        public static Texture2D? blackTexture { get; } = new Texture2D(4, 4);
    }

    // -------------------------------------------------------------------------
    // Sprite
    // -------------------------------------------------------------------------
    public partial class Sprite : Object
    {
        public Texture2D? texture { get; private set; }
        public Rect rect { get; private set; }
        public Vector2 pivot { get; private set; }
        public float pixelsPerUnit { get; private set; } = 100f;
        public Vector4 border { get; private set; }
        public Bounds bounds { get; private set; }

        private Sprite() { }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit = 100f,
            uint extrude = 0, SpriteMeshType meshType = SpriteMeshType.FullRect,
            Vector4 border = default, bool generateFallbackPhysicsShape = false)
        {
            return new Sprite
            {
                texture = texture, rect = rect, pivot = pivot,
                pixelsPerUnit = pixelsPerUnit, border = border,
                name = texture?.name ?? "Sprite",
                bounds = new Bounds(Vector3.zero, new Vector3(rect.width / pixelsPerUnit, rect.height / pixelsPerUnit, 0))
            };
        }
    }

    // -------------------------------------------------------------------------
    // SpriteRenderer
    // -------------------------------------------------------------------------
    public partial class SpriteRenderer : Renderer
    {
        public Sprite? sprite { get; set; }
        public new Color color { get; set; } = Color.white;
        public new int sortingOrder { get; set; }
        public string sortingLayerName { get; set; } = "Default";
        public int sortingLayerID { get; set; }
        public bool flipX { get; set; }
        public bool flipY { get; set; }
        public SpriteDrawMode drawMode { get; set; } = SpriteDrawMode.Simple;
        public Vector2 size { get; set; } = Vector2.one;
        public new Material? material { get; set; }
    }

    // -------------------------------------------------------------------------
    // Renderer (base)
    // -------------------------------------------------------------------------
    public partial class Renderer : Component
    {
        public bool enabled { get; set; } = true;
        public int sortingOrder { get; set; }
        public Color color { get; set; } = Color.white;
        public Material? material { get; set; }
        public Material[]? materials { get; set; }
        public Bounds bounds { get; set; }
        public bool isVisible { get; set; } = true;
        public bool shadowCastingMode { get; set; }
    }

    // -------------------------------------------------------------------------
    // Camera
    // -------------------------------------------------------------------------
    public partial class Camera : Behaviour
    {
        private static Camera? _main;

        public static Camera? main
        {
            get
            {
                if (_main == null)
                {
                    var go = new GameObject("Main Camera");
                    go.tag = "MainCamera";
                    _main = go.AddComponent<Camera>();
                }
                return _main;
            }
        }

        public static Camera? current => _main;

        public bool orthographic { get; set; } = true;
        public float orthographicSize { get; set; } = 5f;
        public float fieldOfView { get; set; } = 60f;
        public float nearClipPlane { get; set; } = 0.3f;
        public float farClipPlane { get; set; } = 1000f;
        public float aspect { get; set; } = 16f / 9f;
        public Color backgroundColor { get; set; } = new Color(0.19f, 0.3f, 0.47f);
        public int depth { get; set; }
        public int cullingMask { get; set; } = -1;
        public Rect rect { get; set; } = new Rect(0, 0, 1, 1);
        public bool clearFlags { get; set; } = true;

        public Vector3 ScreenToWorldPoint(Vector3 position)
        {
            // Orthographic unproject approximation
            float halfH = orthographicSize;
            float halfW = halfH * aspect;
            float wx = (position.x / Screen.width - 0.5f) * halfW * 2f;
            float wy = (position.y / Screen.height - 0.5f) * halfH * 2f;
            return transform.position + new Vector3(wx, wy, 0);
        }

        public Vector3 WorldToScreenPoint(Vector3 position) => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        public Vector3 WorldToViewportPoint(Vector3 position) => new Vector3(0.5f, 0.5f, 0);
        public Vector3 ViewportToWorldPoint(Vector3 position) => transform.position;
        public Ray ScreenPointToRay(Vector3 pos) => new Ray();
    }

    // -------------------------------------------------------------------------
    // Material
    // -------------------------------------------------------------------------
    public partial class Material : Object
    {
        public Shader? shader { get; set; }
        public Color color { get; set; } = Color.white;
        public Texture2D? mainTexture { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public Vector2 mainTextureScale { get; set; } = Vector2.one;

        public Material(Shader? shader) { this.shader = shader; }

        public void SetColor(string name, Color c) { }
        public void SetFloat(string name, float f) { }
        public void SetInt(string name, int i) { }
        public void SetVector(string name, Vector4 v) { }
        public void SetVector(string name, Vector3 v) { }
        public void SetTexture(string name, Texture2D? t) { }
        public Color GetColor(string name) => Color.white;
        public float GetFloat(string name) => 0f;
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
    }

    // -------------------------------------------------------------------------
    // Shader
    // -------------------------------------------------------------------------
    public partial class Shader : Object
    {
        public static Shader? Find(string name) => new Shader { name = name };
    }

    // -------------------------------------------------------------------------
    // Gizmos (no-op)
    // -------------------------------------------------------------------------
    public static partial class Gizmos
    {
        public static Color color { get; set; }
        public static void DrawLine(Vector3 from, Vector3 to) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawCube(Vector3 center, Vector3 size) { }
        public static void DrawRay(Ray r) { }
        public static void DrawRay(Vector3 from, Vector3 dir) { }
        public static void DrawIcon(Vector3 center, string name, bool allowScaling = true) { }
        public static void DrawMesh(Mesh? mesh, Vector3 pos, Quaternion rot, Vector3 scale) { }
    }

    // -------------------------------------------------------------------------
    // Ray / RaycastHit
    // -------------------------------------------------------------------------
    public struct Ray
    {
        public Vector3 origin, direction;
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
        public Vector3 GetPoint(float t) => origin + direction * t;
    }

    public struct RaycastHit
    {
        public Vector3 point, normal;
        public float distance;
        public Collider? collider;
        public Transform? transform => collider?.transform;
        public GameObject? gameObject => collider?.gameObject;
    }

    // -------------------------------------------------------------------------
    // Mesh (minimal)
    // -------------------------------------------------------------------------
    public partial class Mesh : Object
    {
        public Vector3[] vertices { get; set; } = Array.Empty<Vector3>();
        public Vector2[] uv { get; set; } = Array.Empty<Vector2>();
        public int[] triangles { get; set; } = Array.Empty<int>();
        public Color[] colors { get; set; } = Array.Empty<Color>();
        public Color32[] colors32 { get; set; } = Array.Empty<Color32>();
        public Vector3[] normals { get; set; } = Array.Empty<Vector3>();
        public Bounds bounds { get; set; }

        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void RecalculateTangents() { }
        public void Clear() { vertices = Array.Empty<Vector3>(); triangles = Array.Empty<int>(); }
        public void SetIndices(int[] idx, MeshTopology t, int sub) { triangles = idx; }
        public void MarkDynamic() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
    }

    public enum MeshTopology { Triangles, Quads, Lines, LineStrip, Points }
}
