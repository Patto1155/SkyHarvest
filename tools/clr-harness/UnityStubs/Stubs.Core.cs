// UnityEngine stub — Core: Object, GameObject, Component, Behaviour, MonoBehaviour, Transform,
// ScriptableObject, attributes, SceneManagement
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // Attributes
    // -------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeFieldAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type t) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute : Attribute { public string? fileName; public string? menuName; public int order; }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public float min; public float max; public RangeAttribute(float min, float max) { this.min = min; this.max = max; } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public string header; public HeaderAttribute(string h) { header = h; } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HideInInspectorAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponentAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class ExecuteInEditModeAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class ExecuteAlwaysAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)] public sealed class NonSerializedAttribute : Attribute { }

    // -------------------------------------------------------------------------
    // Object
    // -------------------------------------------------------------------------
    public partial class Object
    {
        public string name { get; set; } = string.Empty;
        public int GetInstanceID() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        public override string ToString() => name;

        public static void Destroy(Object? obj, float t = 0f) { /* no-op in harness */ }
        public static void DestroyImmediate(Object? obj) { }
        public static void DontDestroyOnLoad(Object? obj) { }

        public static T? Instantiate<T>(T original) where T : Object
        {
            // Shallow-clone via MemberwiseClone for harness
            if (original == null) return null;
            return (T)original.MemberwiseClone();
        }
        public static Object? Instantiate(Object original) => Instantiate<Object>(original);
        public static Object? Instantiate(Object original, Transform parent) => Instantiate<Object>(original);
        public static T? Instantiate<T>(T original, Transform parent) where T : Object => Instantiate<T>(original);
        public static T? Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => Instantiate<T>(original);
        public static T? Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => Instantiate<T>(original);

        public static implicit operator bool(Object? obj) => obj != null;

        public static T? FindObjectOfType<T>() where T : Object
        {
            // Unsupported in harness — returns null
            return null;
        }
        public static T[]? FindObjectsOfType<T>() where T : Object => Array.Empty<T>();
    }

    // -------------------------------------------------------------------------
    // Transform
    // -------------------------------------------------------------------------
    public partial class Transform : Component
    {
        private readonly List<Transform> _children = new();
        private Transform? _parent;

        public Vector3 position { get; set; } = Vector3.zero;
        public Vector3 localPosition { get; set; } = Vector3.zero;
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 lossyScale => localScale;
        public Vector3 eulerAngles { get; set; } = Vector3.zero;
        public Vector3 localEulerAngles { get; set; } = Vector3.zero;
        public Quaternion rotation { get; set; } = Quaternion.identity;
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Vector3 right => rotation * Vector3.right;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 forward => rotation * Vector3.forward;

        public Transform? parent
        {
            get => _parent;
            set => SetParent(value);
        }

        public int childCount => _children.Count;

        public void SetParent(Transform? p, bool worldPositionStays = true)
        {
            _parent?._children.Remove(this);
            _parent = p;
            p?._children.Add(this);
        }

        public Transform GetChild(int index) => _children[index];

        public Transform? Find(string name)
        {
            foreach (var c in _children)
                if (c.name == name) return c;
            return null;
        }

        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void SetSiblingIndex(int i) { }
        public int GetSiblingIndex() => 0;

        public void LookAt(Transform target) { }
        public void LookAt(Vector3 worldPoint) { }
        public void Rotate(Vector3 euler) { }
        public void Rotate(float x, float y, float z) { }
        public void Translate(Vector3 v) { position += v; }
        public void Translate(float x, float y, float z) { position += new Vector3(x, y, z); }

        public Vector3 TransformPoint(Vector3 point) => position + point;
        public Vector3 InverseTransformPoint(Vector3 point) => point - position;
        public Vector3 TransformDirection(Vector3 dir) => dir;
        public Vector3 InverseTransformDirection(Vector3 dir) => dir;
    }

    // -------------------------------------------------------------------------
    // Component
    // -------------------------------------------------------------------------
    public partial class Component : Object
    {
        public Transform transform { get; internal set; } = null!;
        public GameObject gameObject { get; internal set; } = null!;

        // Component graph delegation
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T GetComponentInChildren<T>() where T : Component => gameObject.GetComponentInChildren<T>();
        public T GetComponentInParent<T>() where T : Component => gameObject.GetComponentInParent<T>();
        public T[] GetComponents<T>() where T : Component => gameObject.GetComponents<T>();
        public T[] GetComponentsInChildren<T>() where T : Component => gameObject.GetComponentsInChildren<T>();

        public bool TryGetComponent<T>(out T component) where T : Component => gameObject.TryGetComponent(out component);

        public void SendMessage(string methodName, object? value = null) { }
        public void BroadcastMessage(string methodName, object? value = null) { }
    }

    // -------------------------------------------------------------------------
    // Behaviour
    // -------------------------------------------------------------------------
    public partial class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
        public bool isActiveAndEnabled => enabled && (gameObject?.activeSelf ?? false);
    }

    // -------------------------------------------------------------------------
    // MonoBehaviour
    // -------------------------------------------------------------------------
    public partial class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(System.Collections.IEnumerator routine) => new Coroutine();
        public void StopCoroutine(Coroutine coroutine) { }
        public void StopAllCoroutines() { }

        public T? GetOrAddComponent<T>() where T : Component, new()
        {
            var c = GetComponent<T>();
            if (c == null) c = gameObject.AddComponent<T>();
            return c;
        }

        public void Invoke(string method, float delay) { }
        public void InvokeRepeating(string method, float time, float repeat) { }
        public void CancelInvoke(string? method = null) { }
        public bool IsInvoking(string method) => false;

        public static void print(object msg) => Debug.Log(msg);
    }

    public class Coroutine { }

    // -------------------------------------------------------------------------
    // GameObject
    // -------------------------------------------------------------------------
    public partial class GameObject : Object
    {
        private readonly Dictionary<Type, Component> _components = new();
        private readonly List<Component> _componentList = new();

        public Transform transform { get; }
        public bool activeSelf { get; private set; } = true;
        public bool activeInHierarchy => activeSelf && (transform.parent?.gameObject.activeInHierarchy ?? true);
        public int layer { get; set; }
        public string tag { get; set; } = "Untagged";
        public Scene scene => default;

        public GameObject() : this(string.Empty) { }
        public GameObject(string name)
        {
            this.name = name;
            transform = new Transform();
            transform.gameObject = this;
            transform.transform = transform;
            _AddComponentInternal(transform);
        }
        public GameObject(string name, params Type[] components) : this(name)
        {
            foreach (var t in components)
                AddComponent(t);
        }

        private void _AddComponentInternal(Component c)
        {
            var t = c.GetType();
            _components[t] = c;
            _componentList.Add(c);
            c.gameObject = this;
            c.transform = transform;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T();
            _AddComponentInternal(c);
            return c;
        }

        public Component AddComponent(Type type)
        {
            var c = (Component)Activator.CreateInstance(type)!;
            _AddComponentInternal(c);
            return c;
        }

        public T GetComponent<T>() where T : Component
        {
            // exact type first
            if (_components.TryGetValue(typeof(T), out var exact)) return (T)exact;
            // inheritance walk
            foreach (var c in _componentList)
                if (c is T match) return match;
            return null!;
        }

        public T GetComponentInChildren<T>() where T : Component
        {
            var own = GetComponent<T>();
            if (own != null) return own;
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                var c = child.gameObject.GetComponent<T>();
                if (c != null) return c;
            }
            return null!;
        }

        public T GetComponentInParent<T>() where T : Component
        {
            var own = GetComponent<T>();
            if (own != null) return own;
            if (transform.parent != null)
                return transform.parent.gameObject.GetComponentInParent<T>();
            return null!;
        }

        public T[] GetComponents<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var c in _componentList)
                if (c is T match) result.Add(match);
            return result.ToArray();
        }

        public T[] GetComponentsInChildren<T>() where T : Component => GetComponents<T>();

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = GetComponent<T>();
            return component != null;
        }

        public void SetActive(bool value) { activeSelf = value; }

        public bool CompareTag(string t) => tag == t;

        public static GameObject? Find(string name) => null; // harness: not implemented
        public static GameObject? FindWithTag(string tag) => null;

        public void SendMessage(string method, object? value = null) { }
        public void BroadcastMessage(string method, object? value = null) { }
    }

    // -------------------------------------------------------------------------
    // ScriptableObject
    // -------------------------------------------------------------------------
    public partial class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
        public static ScriptableObject CreateInstance(Type type) => (ScriptableObject)Activator.CreateInstance(type)!;
        public static ScriptableObject CreateInstance(string className)
        {
            var t = Type.GetType(className) ?? typeof(ScriptableObject);
            return (ScriptableObject)Activator.CreateInstance(t)!;
        }
    }

    // -------------------------------------------------------------------------
    // Scene (struct, for completeness)
    // -------------------------------------------------------------------------
    public struct Scene { public bool IsValid() => false; }
}

namespace UnityEngine.SceneManagement
{
    public static partial class SceneManager
    {
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int index) { }
        public static void LoadSceneAsync(string sceneName) { }
        public static UnityEngine.Scene GetActiveScene() => default;
        public static int sceneCount => 1;
        public delegate void SceneLoaded(UnityEngine.Scene scene, LoadSceneMode mode);
        public static event SceneLoaded? sceneLoaded;
        public static event System.Action<UnityEngine.Scene>? sceneUnloaded;
    }

    public enum LoadSceneMode { Single, Additive }
}
