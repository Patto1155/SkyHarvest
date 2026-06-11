// UnityEngine stub — Physics2D: Collider2D, BoxCollider2D, CircleCollider2D, Rigidbody2D, Physics2D
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // LayerMask
    // -------------------------------------------------------------------------
    public struct LayerMask
    {
        public int value;
        public static implicit operator int(LayerMask m) => m.value;
        public static implicit operator LayerMask(int v) => new LayerMask { value = v };
        public static int GetMask(params string[] layerNames) => -1;
        public static int NameToLayer(string name) => 0;
        public static string LayerToName(int layer) => string.Empty;
    }

    // -------------------------------------------------------------------------
    // Collider2D (base)
    // -------------------------------------------------------------------------
    public partial class Collider2D : Behaviour
    {
        public bool isTrigger { get; set; }
        public PhysicsMaterial2D? sharedMaterial { get; set; }
        public Bounds bounds { get; set; }
        public Vector2 offset { get; set; }
        public Rigidbody2D? attachedRigidbody { get; set; }

        public bool OverlapPoint(Vector2 point) => false;
        public bool IsTouching(Collider2D other) => false;
        public ContactFilter2D CreateContactFilter() => default;
    }

    // -------------------------------------------------------------------------
    // BoxCollider2D
    // -------------------------------------------------------------------------
    public partial class BoxCollider2D : Collider2D
    {
        public Vector2 size { get; set; } = Vector2.one;
        public float edgeRadius { get; set; }
        public bool autoTiling { get; set; }
    }

    // -------------------------------------------------------------------------
    // CircleCollider2D
    // -------------------------------------------------------------------------
    public partial class CircleCollider2D : Collider2D
    {
        public float radius { get; set; } = 0.5f;
    }

    // -------------------------------------------------------------------------
    // PolygonCollider2D
    // -------------------------------------------------------------------------
    public partial class PolygonCollider2D : Collider2D
    {
        public Vector2[][] paths { get; set; } = Array.Empty<Vector2[]>();
        public int pathCount => paths.Length;
        public void SetPath(int index, Vector2[] points) { }
    }

    // -------------------------------------------------------------------------
    // Rigidbody2D
    // -------------------------------------------------------------------------
    public partial class Rigidbody2D : Component
    {
        public Vector2 velocity { get; set; }
        public float angularVelocity { get; set; }
        public float mass { get; set; } = 1f;
        public float drag { get; set; }
        public float angularDrag { get; set; } = 0.05f;
        public float gravityScale { get; set; } = 1f;
        public bool isKinematic { get; set; }
        public bool freezeRotation { get; set; }
        public RigidbodyType2D bodyType { get; set; } = RigidbodyType2D.Dynamic;
        public CollisionDetectionMode2D collisionDetectionMode { get; set; }
        public RigidbodyConstraints2D constraints { get; set; }
        public Vector2 position { get => new Vector2(transform.position.x, transform.position.y); set => transform.position = new Vector3(value.x, value.y, transform.position.z); }
        public float rotation { get => transform.eulerAngles.z; set { var e = transform.eulerAngles; e.z = value; transform.eulerAngles = e; } }

        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force) { }
        public void AddTorque(float torque, ForceMode2D mode = ForceMode2D.Force) { }
        public void MovePosition(Vector2 position) { this.position = position; }
        public void MoveRotation(float angle) { rotation = angle; }
        public void Sleep() { }
        public void WakeUp() { }
        public bool IsSleeping() => false;
        public void SetRotation(float angle) { rotation = angle; }
    }

    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum CollisionDetectionMode2D { Discrete, Continuous }
    [Flags] public enum RigidbodyConstraints2D { None = 0, FreezePositionX = 1, FreezePositionY = 2, FreezeRotation = 4, FreezeAll = 7 }
    public enum ForceMode2D { Force, Impulse }

    // -------------------------------------------------------------------------
    // PhysicsMaterial2D
    // -------------------------------------------------------------------------
    public partial class PhysicsMaterial2D : Object
    {
        public float bounciness { get; set; }
        public float friction { get; set; }
    }

    // -------------------------------------------------------------------------
    // ContactFilter2D
    // -------------------------------------------------------------------------
    public struct ContactFilter2D
    {
        public bool useTriggers;
        public bool useLayerMask;
        public LayerMask layerMask;
        public bool useDepth, useNormalAngle;
        public void NoFilter() { }
        public void SetLayerMask(LayerMask mask) { layerMask = mask; useLayerMask = true; }
    }

    // -------------------------------------------------------------------------
    // Collision2D / ContactPoint2D
    // -------------------------------------------------------------------------
    public class Collision2D
    {
        public Collider2D? collider { get; set; }
        public Rigidbody2D? rigidbody { get; set; }
        public GameObject? gameObject => collider?.gameObject;
        public ContactPoint2D[] contacts { get; set; } = Array.Empty<ContactPoint2D>();
        public Vector2 relativeVelocity { get; set; }
    }

    public struct ContactPoint2D
    {
        public Vector2 point, normal;
        public float separation;
        public Collider2D? collider;
        public Collider2D? otherCollider;
    }

    // -------------------------------------------------------------------------
    // Physics2D
    // -------------------------------------------------------------------------
    public static partial class Physics2D
    {
        public static float gravity { get; set; } = -9.81f;

        // Harness: OverlapCircleAll / OverlapPointAll return empty — logic tests should
        // use dependency injection rather than calling Physics2D directly.
        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask = -1) => Array.Empty<Collider2D>();
        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask = -1) => Array.Empty<Collider2D>();
        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask = -1) => Array.Empty<Collider2D>();
        public static Collider2D? OverlapCircle(Vector2 point, float radius, int layerMask = -1) => null;
        public static Collider2D? OverlapPoint(Vector2 point, int layerMask = -1) => null;
        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask = -1) => default;
        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance = float.MaxValue, int layerMask = -1) => default;
        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance = float.MaxValue, int layerMask = -1) => Array.Empty<RaycastHit2D>();
        public static bool GetIgnoreLayerCollision(int layer1, int layer2) => false;
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore = true) { }
        public static void IgnoreCollision(Collider2D a, Collider2D b, bool ignore = true) { }

        public const int DefaultRaycastLayers = -5;
        public static int solverIterations { get; set; } = 6;
        public static Vector2 Gravity { get => new(0, -9.81f); set { } }
    }

    public struct RaycastHit2D
    {
        public Vector2 point, normal;
        public float distance, fraction;
        public Collider2D? collider;
        public Rigidbody2D? rigidbody;
        public Transform? transform => collider?.transform;
        public static implicit operator bool(RaycastHit2D h) => h.collider != null;
    }

    // -------------------------------------------------------------------------
    // Rigidbody (3D — referenced by old plan code, stub so it compiles)
    // -------------------------------------------------------------------------
    public partial class Rigidbody : Component
    {
        public Vector3 velocity { get; set; }
        public float mass { get; set; } = 1f;
        public bool isKinematic { get; set; }
        public bool freezeRotation { get; set; }
        public RigidbodyConstraints constraints { get; set; }
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) { }
        public Vector3 linearVelocity { get => velocity; set => velocity = value; }
    }

    [Flags] public enum RigidbodyConstraints { None = 0, FreezePositionX = 2, FreezePositionY = 4, FreezePositionZ = 8, FreezeRotationX = 16, FreezeRotationY = 32, FreezeRotationZ = 64, FreezePosition = 14, FreezeRotation = 112, FreezeAll = 126 }
    public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }

    // -------------------------------------------------------------------------
    // Collider (3D) — also referenced by old plan code
    // -------------------------------------------------------------------------
    public partial class Collider : Component
    {
        public bool isTrigger { get; set; }
        public bool enabled { get; set; } = true;
        public Bounds bounds { get; set; }
        public Rigidbody? attachedRigidbody { get; set; }
    }

    public partial class SphereCollider : Collider { public float radius { get; set; } = 0.5f; }
    public partial class BoxCollider : Collider { public Vector3 size { get; set; } = Vector3.one; }
    public partial class CapsuleCollider : Collider { public float radius { get; set; } = 0.5f; public float height { get; set; } = 2f; }

    public static partial class Physics
    {
        public static Collider[] OverlapSphere(Vector3 center, float radius, int layerMask = -1) => Array.Empty<Collider>();
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDist = float.MaxValue, int layerMask = -1) { hit = default; return false; }
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float dist = float.MaxValue, int layerMask = -1) => Array.Empty<RaycastHit>();
        public static int DefaultRaycastLayers = -5;
        public static Vector3 gravity { get; set; } = new Vector3(0, -9.81f, 0);
    }
}
