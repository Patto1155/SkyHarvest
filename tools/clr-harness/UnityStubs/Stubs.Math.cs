// UnityEngine stub — Math: Vector2, Vector3, Vector2Int, Vector3Int, Quaternion, Mathf, Color, Color32, Rect, Bounds
using System;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // Vector2
    // -------------------------------------------------------------------------
    public partial struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public static readonly Vector2 zero = new(0, 0);
        public static readonly Vector2 one = new(1, 1);
        public static readonly Vector2 up = new(0, 1);
        public static readonly Vector2 down = new(0, -1);
        public static readonly Vector2 left = new(-1, 0);
        public static readonly Vector2 right = new(1, 0);
        public static readonly Vector2 positiveInfinity = new(float.PositiveInfinity, float.PositiveInfinity);
        public static readonly Vector2 negativeInfinity = new(float.NegativeInfinity, float.NegativeInfinity);

        public float magnitude => MathF.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized { get { float m = magnitude; return m > 1e-8f ? new Vector2(x / m, y / m) : zero; } }

        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) => new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
        {
            var diff = target - current;
            float dist = diff.magnitude;
            if (dist <= maxDelta || dist < 1e-10f) return target;
            return current + diff / dist * maxDelta;
        }
        public static Vector2 Scale(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);
        public static Vector2 Min(Vector2 a, Vector2 b) => new(MathF.Min(a.x, b.x), MathF.Min(a.y, b.y));
        public static Vector2 Max(Vector2 a, Vector2 b) => new(MathF.Max(a.x, b.x), MathF.Max(a.y, b.y));
        public static Vector2 Reflect(Vector2 inDir, Vector2 normal) => inDir - 2f * Dot(inDir, normal) * normal;
        public static float Angle(Vector2 from, Vector2 to) => MathF.Acos(Mathf.Clamp(Dot(from.normalized, to.normalized), -1f, 1f)) * Mathf.Rad2Deg;
        public static float SignedAngle(Vector2 from, Vector2 to) => Angle(from, to) * MathF.Sign(from.x * to.y - from.y * to.x);

        public void Normalize() { float m = magnitude; if (m > 1e-8f) { x /= m; y /= m; } }
        public void Set(float nx, float ny) { x = nx; y = ny; }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 9.99999944E-11f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static implicit operator Vector2(Vector3 v) => new(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new(v.x, v.y, 0);

        public float this[int i] => i == 0 ? x : y;
        public bool Equals(Vector2 other) => this == other;
        public override bool Equals(object? o) => o is Vector2 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    // -------------------------------------------------------------------------
    // Vector3
    // -------------------------------------------------------------------------
    public partial struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }

        public static readonly Vector3 zero = new(0, 0, 0);
        public static readonly Vector3 one = new(1, 1, 1);
        public static readonly Vector3 up = new(0, 1, 0);
        public static readonly Vector3 down = new(0, -1, 0);
        public static readonly Vector3 forward = new(0, 0, 1);
        public static readonly Vector3 back = new(0, 0, -1);
        public static readonly Vector3 right = new(1, 0, 0);
        public static readonly Vector3 left = new(-1, 0, 0);
        public static readonly Vector3 positiveInfinity = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static readonly Vector3 negativeInfinity = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { float m = magnitude; return m > 1e-8f ? new Vector3(x / m, y / m, z / m) : zero; } }

        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t); }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
        {
            var diff = target - current; float dist = diff.magnitude;
            if (dist <= maxDelta || dist < 1e-10f) return target;
            return current + diff / dist * maxDelta;
        }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Reflect(Vector3 inDir, Vector3 normal) => inDir - 2f * Dot(inDir, normal) * normal;
        public static float Angle(Vector3 from, Vector3 to) => MathF.Acos(Mathf.Clamp(Dot(from.normalized, to.normalized), -1f, 1f)) * Mathf.Rad2Deg;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) => v - normal * Dot(v, normal) / normal.sqrMagnitude;
        public static Vector3 Project(Vector3 v, Vector3 normal) => normal * Dot(v, normal) / normal.sqrMagnitude;

        public void Normalize() { float m = magnitude; if (m > 1e-8f) { x /= m; y /= m; z /= m; } }
        public void Set(float nx, float ny, float nz) { x = nx; y = ny; z = nz; }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 9.99999944E-11f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public float this[int i] => i == 0 ? x : i == 1 ? y : z;
        public bool Equals(Vector3 other) => this == other;
        public override bool Equals(object? o) => o is Vector3 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    // -------------------------------------------------------------------------
    // Vector2Int
    // -------------------------------------------------------------------------
    public partial struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public static readonly Vector2Int zero = new(0, 0);
        public static readonly Vector2Int one = new(1, 1);
        public static readonly Vector2Int up = new(0, 1);
        public static readonly Vector2Int down = new(0, -1);
        public static readonly Vector2Int left = new(-1, 0);
        public static readonly Vector2Int right = new(1, 0);

        public float magnitude => MathF.Sqrt(x * x + y * y);
        public int sqrMagnitude => x * x + y * y;
        public Vector2Int abs => new(Math.Abs(x), Math.Abs(y));

        public static float Distance(Vector2Int a, Vector2Int b) => (a - b).magnitude;
        public static Vector2Int Min(Vector2Int a, Vector2Int b) => new(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        public static Vector2Int Max(Vector2Int a, Vector2Int b) => new(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        public static Vector2Int Scale(Vector2Int a, Vector2Int b) => new(a.x * b.x, a.y * b.y);
        public static Vector2Int FloorToInt(Vector2 v) => new((int)MathF.Floor(v.x), (int)MathF.Floor(v.y));
        public static Vector2Int CeilToInt(Vector2 v) => new((int)MathF.Ceiling(v.x), (int)MathF.Ceiling(v.y));
        public static Vector2Int RoundToInt(Vector2 v) => new((int)MathF.Round(v.x), (int)MathF.Round(v.y));

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.x - b.x, a.y - b.y);
        public static Vector2Int operator -(Vector2Int a) => new(-a.x, -a.y);
        public static Vector2Int operator *(Vector2Int a, int d) => new(a.x * d, a.y * d);
        public static Vector2Int operator *(int d, Vector2Int a) => new(a.x * d, a.y * d);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public static implicit operator Vector2(Vector2Int v) => new(v.x, v.y);

        public int this[int i] => i == 0 ? x : y;
        public bool Equals(Vector2Int other) => this == other;
        public override bool Equals(object? o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    // -------------------------------------------------------------------------
    // Vector3Int
    // -------------------------------------------------------------------------
    public partial struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public Vector3Int(int x, int y) { this.x = x; this.y = y; this.z = 0; }

        public static readonly Vector3Int zero = new(0, 0, 0);
        public static readonly Vector3Int one = new(1, 1, 1);
        public static readonly Vector3Int up = new(0, 1, 0);
        public static readonly Vector3Int down = new(0, -1, 0);
        public static readonly Vector3Int right = new(1, 0, 0);
        public static readonly Vector3Int left = new(-1, 0, 0);
        public static readonly Vector3Int forward = new(0, 0, 1);
        public static readonly Vector3Int back = new(0, 0, -1);

        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);

        public static bool operator ==(Vector3Int a, Vector3Int b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3Int a, Vector3Int b) => !(a == b);
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) => new(a.x - b.x, a.y - b.y, a.z - b.z);

        public bool Equals(Vector3Int other) => this == other;
        public override bool Equals(object? o) => o is Vector3Int v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override string ToString() => $"({x}, {y}, {z})";
    }

    // -------------------------------------------------------------------------
    // Quaternion
    // -------------------------------------------------------------------------
    public partial struct Quaternion : IEquatable<Quaternion>
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static readonly Quaternion identity = new(0, 0, 0, 1);

        public Vector3 eulerAngles
        {
            get
            {
                // Approximate; sufficient for stubs
                float sinrCosp = 2f * (w * x + y * z);
                float cosrCosp = 1f - 2f * (x * x + y * y);
                float roll = MathF.Atan2(sinrCosp, cosrCosp);
                float sinp = 2f * (w * y - z * x);
                float pitch = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp);
                float sinyCosp = 2f * (w * z + x * y);
                float cosyCosp = 1f - 2f * (y * y + z * z);
                float yaw = MathF.Atan2(sinyCosp, cosyCosp);
                return new Vector3(roll * Mathf.Rad2Deg, pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg);
            }
            set { var q = Euler(value.x, value.y, value.z); x = q.x; y = q.y; z = q.z; w = q.w; }
        }

        public Quaternion normalized { get { float m = MathF.Sqrt(x*x+y*y+z*z+w*w); return m > 1e-8f ? new(x/m,y/m,z/m,w/m) : identity; } }

        public static Quaternion Euler(float x, float y, float z)
        {
            float cx = MathF.Cos(x * Mathf.Deg2Rad * 0.5f), sx = MathF.Sin(x * Mathf.Deg2Rad * 0.5f);
            float cy = MathF.Cos(y * Mathf.Deg2Rad * 0.5f), sy = MathF.Sin(y * Mathf.Deg2Rad * 0.5f);
            float cz = MathF.Cos(z * Mathf.Deg2Rad * 0.5f), sz = MathF.Sin(z * Mathf.Deg2Rad * 0.5f);
            return new Quaternion(
                sx * cy * cz + cx * sy * sz,
                cx * sy * cz - sx * cy * sz,
                cx * cy * sz + sx * sy * cz,
                cx * cy * cz - sx * sy * sz);
        }
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);
        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            var n = axis.normalized; float a = angle * Mathf.Deg2Rad * 0.5f;
            float s = MathF.Sin(a);
            return new Quaternion(n.x * s, n.y * s, n.z * s, MathF.Cos(a));
        }
        public static Quaternion LookRotation(Vector3 forward, Vector3 up = default)
        {
            if (up == default) up = Vector3.up;
            // Approximate
            return identity;
        }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => Lerp(a, b, t);
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Quaternion(
                a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t).normalized;
        }
        public static Quaternion Inverse(Quaternion q) => new(-q.x, -q.y, -q.z, q.w);
        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = MathF.Abs(a.x*b.x + a.y*b.y + a.z*b.z + a.w*b.w);
            return dot > 1f - 1e-6f ? 0f : MathF.Acos(dot) * 2f * Mathf.Rad2Deg;
        }
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDeg)
        {
            float a = Angle(from, to);
            if (a == 0f) return to;
            return Slerp(from, to, Math.Min(1f, maxDeg / a));
        }
        public static Quaternion FromToRotation(Vector3 from, Vector3 to) => identity; // approx

        public static Quaternion operator *(Quaternion a, Quaternion b) => new(
            a.w*b.x + a.x*b.w + a.y*b.z - a.z*b.y,
            a.w*b.y - a.x*b.z + a.y*b.w + a.z*b.x,
            a.w*b.z + a.x*b.y - a.y*b.x + a.z*b.w,
            a.w*b.w - a.x*b.x - a.y*b.y - a.z*b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            float tx = 2f * (q.y * v.z - q.z * v.y);
            float ty = 2f * (q.z * v.x - q.x * v.z);
            float tz = 2f * (q.x * v.y - q.y * v.x);
            return new Vector3(v.x + q.w * tx + q.y * tz - q.z * ty,
                               v.y + q.w * ty + q.z * tx - q.x * tz,
                               v.z + q.w * tz + q.x * ty - q.y * tx);
        }
        public static bool operator ==(Quaternion a, Quaternion b) => MathF.Abs(a.x-b.x) < 1e-6f && MathF.Abs(a.y-b.y) < 1e-6f && MathF.Abs(a.z-b.z) < 1e-6f && MathF.Abs(a.w-b.w) < 1e-6f;
        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);

        public bool Equals(Quaternion other) => this == other;
        public override bool Equals(object? o) => o is Quaternion q && this == q;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
    }

    // -------------------------------------------------------------------------
    // Mathf
    // -------------------------------------------------------------------------
    public static partial class Mathf
    {
        public const float PI = MathF.PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Epsilon = 1.17549435E-38f;

        public static float Abs(float f) => MathF.Abs(f);
        public static int Abs(int i) => Math.Abs(i);
        public static float Acos(float f) => MathF.Acos(f);
        public static float Asin(float f) => MathF.Asin(f);
        public static float Atan(float f) => MathF.Atan(f);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Ceil(float f) => MathF.Ceiling(f);
        public static int CeilToInt(float f) => (int)MathF.Ceiling(f);
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Cos(float f) => MathF.Cos(f);
        public static float DeltaAngle(float a, float b) { float d = Repeat(b - a, 360f); return d > 180f ? d - 360f : d; }
        public static float Exp(float p) => MathF.Exp(p);
        public static float Floor(float f) => MathF.Floor(f);
        public static int FloorToInt(float f) => (int)MathF.Floor(f);
        public static float InverseLerp(float a, float b, float v) => Approximately(a, b) ? 0f : Clamp01((v - a) / (b - a));
        public static bool Approximately(float a, float b) => MathF.Abs(b - a) < Max(1e-6f * Max(MathF.Abs(a), MathF.Abs(b)), Epsilon * 8f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float LerpAngle(float a, float b, float t) { float d = Repeat(b - a, 360f); if (d > 180f) d -= 360f; return a + d * Clamp01(t); }
        public static float Log(float f, float p) => MathF.Log(f, p);
        public static float Log(float f) => MathF.Log(f);
        public static float Log10(float f) => MathF.Log10(f);
        public static float Max(float a, float b) => MathF.Max(a, b);
        public static float Max(params float[] vals) { float m = float.NegativeInfinity; foreach (var v in vals) if (v > m) m = v; return m; }
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => MathF.Min(a, b);
        public static float Min(params float[] vals) { float m = float.PositiveInfinity; foreach (var v in vals) if (v < m) m = v; return m; }
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float MoveTowards(float current, float target, float maxDelta) { float d = target - current; if (MathF.Abs(d) <= maxDelta) return target; return current + MathF.Sign(d) * maxDelta; }
        public static float MoveTowardsAngle(float current, float target, float maxDelta) => MoveTowards(current, target, maxDelta);
        public static float PingPong(float t, float length) { t = Repeat(t, length * 2f); return length - MathF.Abs(t - length); }
        public static float Pow(float f, float p) => MathF.Pow(f, p);
        public static float Repeat(float t, float length) => Clamp(t - MathF.Floor(t / length) * length, 0f, length);
        public static int RoundToInt(float f) => (int)MathF.Round(f, MidpointRounding.AwayFromZero);
        public static float Round(float f) => MathF.Round(f);
        public static float Sign(float f) => MathF.Sign(f);
        public static float Sin(float f) => MathF.Sin(f);
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed = Infinity, float deltaTime = -1f) { if (deltaTime < 0) deltaTime = Time.deltaTime; smoothTime = Max(0.0001f, smoothTime); float omega = 2f / smoothTime; float x = omega * deltaTime; float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x); float diff = current - target; float origTo = target; float maxChange = maxSpeed * smoothTime; diff = Clamp(diff, -maxChange, maxChange); target = current - diff; float temp = (velocity + omega * diff) * deltaTime; velocity = (velocity - omega * temp) * exp; float output = target + (diff + temp) * exp; if (origTo - current > 0f == output > origTo) { output = origTo; velocity = (output - origTo) / deltaTime; } return output; }
        public static float SmoothStep(float from, float to, float t) { t = Clamp01(t); t = t * t * (3f - 2f * t); return to * t + from * (1f - t); }
        public static float Sqrt(float f) => MathF.Sqrt(f);
        public static float Tan(float f) => MathF.Tan(f);

        // Real 2D Perlin noise (value noise + smoothstep, deterministic)
        public static float PerlinNoise(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255, yi = (int)MathF.Floor(y) & 255;
            float xf = x - MathF.Floor(x), yf = y - MathF.Floor(y);
            float u = Fade(xf), v = Fade(yf);
            int aa = P[P[xi] + yi], ab = P[P[xi] + yi + 1];
            int ba = P[P[xi + 1] + yi], bb = P[P[xi + 1] + yi + 1];
            float res = Lerp(Lerp(Grad(P[aa], xf, yf), Grad(P[ba], xf - 1, yf), u),
                             Lerp(Grad(P[ab], xf, yf - 1), Grad(P[bb], xf - 1, yf - 1), u), v);
            return (res + 1f) * 0.5f; // remap to [0,1]
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y, v2 = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v2 : -v2);
        }
        private static readonly int[] P;
        static Mathf()
        {
            // Standard Perlin permutation table (first 256 values)
            int[] perm = {
                151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,
                140,36,103,30,69,142,8,99,37,240,21,10,23,190,6,148,
                247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,
                57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,
                74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,
                60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
                65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,
                200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,
                52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,
                207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,
                119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,
                129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,
                218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,
                81,51,145,235,249,14,239,107,49,192,214,31,181,199,106,157,
                184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,
                222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
            };
            P = new int[512];
            for (int i = 0; i < 256; i++) P[i] = P[i + 256] = perm[i];
        }
    }

    // -------------------------------------------------------------------------
    // Color / Color32
    // -------------------------------------------------------------------------
    public partial struct Color : IEquatable<Color>
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static readonly Color red = new(1, 0, 0);
        public static readonly Color green = new(0, 1, 0);
        public static readonly Color blue = new(0, 0, 1);
        public static readonly Color white = new(1, 1, 1);
        public static readonly Color black = new(0, 0, 0);
        public static readonly Color yellow = new(1, 0.92f, 0.016f);
        public static readonly Color cyan = new(0, 1, 1);
        public static readonly Color magenta = new(1, 0, 1);
        public static readonly Color gray = new(0.5f, 0.5f, 0.5f);
        public static readonly Color grey = gray;
        public static readonly Color clear = new(0, 0, 0, 0);

        public float grayscale => 0.299f * r + 0.587f * g + 0.114f * b;
        public Color linear => new(Mathf.Pow(r, 2.2f), Mathf.Pow(g, 2.2f), Mathf.Pow(b, 2.2f), a);
        public Color gamma => new(Mathf.Pow(r, 1f / 2.2f), Mathf.Pow(g, 1f / 2.2f), Mathf.Pow(b, 1f / 2.2f), a);

        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r+(b.r-a.r)*t, a.g+(b.g-a.g)*t, a.b+(b.b-a.b)*t, a.a+(b.a-a.a)*t); }
        public static Color LerpUnclamped(Color a, Color b, float t) => new(a.r+(b.r-a.r)*t, a.g+(b.g-a.g)*t, a.b+(b.b-a.b)*t, a.a+(b.a-a.a)*t);

        public static Color operator +(Color a, Color b) => new(a.r+b.r, a.g+b.g, a.b+b.b, a.a+b.a);
        public static Color operator -(Color a, Color b) => new(a.r-b.r, a.g-b.g, a.b-b.b, a.a-b.a);
        public static Color operator *(Color a, Color b) => new(a.r*b.r, a.g*b.g, a.b*b.b, a.a*b.a);
        public static Color operator *(Color a, float f) => new(a.r*f, a.g*f, a.b*f, a.a*f);
        public static Color operator *(float f, Color a) => a * f;
        public static Color operator /(Color a, float f) => new(a.r/f, a.g/f, a.b/f, a.a/f);
        public static bool operator ==(Color a, Color b) => MathF.Abs(a.r-b.r) < 1e-5f && MathF.Abs(a.g-b.g) < 1e-5f && MathF.Abs(a.b-b.b) < 1e-5f && MathF.Abs(a.a-b.a) < 1e-5f;
        public static bool operator !=(Color a, Color b) => !(a == b);

        public static implicit operator Color32(Color c) => new((byte)(c.r*255), (byte)(c.g*255), (byte)(c.b*255), (byte)(c.a*255));
        public static implicit operator Color(Color32 c) => new(c.r/255f, c.g/255f, c.b/255f, c.a/255f);

        public static Color HSVToRGB(float h, float s, float v, bool hdr = false)
        {
            if (s == 0) return new Color(v, v, v);
            h *= 6f; int i = (int)h; float f = h - i; float p = v*(1-s), q = v*(1-s*f), t2 = v*(1-s*(1-f));
            return i switch { 0 => new(v,t2,p), 1 => new(q,v,p), 2 => new(p,v,t2), 3 => new(p,q,v), 4 => new(t2,p,v), _ => new(v,p,q) };
        }
        public static void RGBToHSV(Color c, out float h, out float s, out float v)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            v = max; s = max == 0 ? 0 : (max - min) / max;
            h = 0; if (max == min) return;
            float d = max - min;
            if (max == c.r) h = (c.g - c.b) / d + (c.g < c.b ? 6 : 0);
            else if (max == c.g) h = (c.b - c.r) / d + 2;
            else h = (c.r - c.g) / d + 4;
            h /= 6f;
        }

        public float this[int i] => i == 0 ? r : i == 1 ? g : i == 2 ? b : a;
        public bool Equals(Color other) => this == other;
        public override bool Equals(object? o) => o is Color c && this == c;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public override string ToString() => $"RGBA({r:F3}, {g:F3}, {b:F3}, {a:F3})";
    }

    public partial struct Color32 : IEquatable<Color32>
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public bool Equals(Color32 other) => r == other.r && g == other.g && b == other.b && a == other.a;
        public override bool Equals(object? o) => o is Color32 c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public static bool operator ==(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public static bool operator !=(Color32 a, Color32 b) => !(a == b);
    }

    // -------------------------------------------------------------------------
    // Rect
    // -------------------------------------------------------------------------
    public partial struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public Rect(Vector2 position, Vector2 size) : this(position.x, position.y, size.x, size.y) { }

        public static Rect zero => new(0, 0, 0, 0);
        public float xMin { get => x; set { width += x - value; x = value; } }
        public float yMin { get => y; set { height += y - value; y = value; } }
        public float xMax { get => x + width; set => width = value - x; }
        public float yMax { get => y + height; set => height = value - y; }
        public Vector2 position { get => new(x, y); set { x = value.x; y = value.y; } }
        public Vector2 size { get => new(width, height); set { width = value.x; height = value.y; } }
        public Vector2 center { get => new(x + width * 0.5f, y + height * 0.5f); set { x = value.x - width * 0.5f; y = value.y - height * 0.5f; } }
        public Vector2 min => new(xMin, yMin);
        public Vector2 max => new(xMax, yMax);

        public bool Contains(Vector2 p) => p.x >= x && p.x < xMax && p.y >= y && p.y < yMax;
        public bool Overlaps(Rect other) => x < other.xMax && xMax > other.x && y < other.yMax && yMax > other.y;
        public static bool operator ==(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public override bool Equals(object? o) => o is Rect r && this == r;
        public override int GetHashCode() => HashCode.Combine(x, y, width, height);
        public override string ToString() => $"(x:{x:F2}, y:{y:F2}, w:{width:F2}, h:{height:F2})";
    }

    // -------------------------------------------------------------------------
    // Vector4
    // -------------------------------------------------------------------------
    public partial struct Vector4 : IEquatable<Vector4>
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w = 0f) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(Vector3 v, float w = 0f) { x = v.x; y = v.y; z = v.z; this.w = w; }
        public Vector4(Vector2 v) { x = v.x; y = v.y; z = 0; w = 0; }

        public static readonly Vector4 zero = new(0, 0, 0, 0);
        public static readonly Vector4 one = new(1, 1, 1, 1);

        public float magnitude => MathF.Sqrt(x*x+y*y+z*z+w*w);
        public float sqrMagnitude => x*x+y*y+z*z+w*w;
        public Vector4 normalized { get { float m = magnitude; return m > 1e-8f ? this/m : zero; } }

        public static float Dot(Vector4 a, Vector4 b) => a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w;
        public static float Distance(Vector4 a, Vector4 b) => (a-b).magnitude;
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) { t = Mathf.Clamp01(t); return new(a.x+(b.x-a.x)*t, a.y+(b.y-a.y)*t, a.z+(b.z-a.z)*t, a.w+(b.w-a.w)*t); }
        public static Vector4 Scale(Vector4 a, Vector4 b) => new(a.x*b.x, a.y*b.y, a.z*b.z, a.w*b.w);

        public static Vector4 operator+(Vector4 a, Vector4 b) => new(a.x+b.x, a.y+b.y, a.z+b.z, a.w+b.w);
        public static Vector4 operator-(Vector4 a, Vector4 b) => new(a.x-b.x, a.y-b.y, a.z-b.z, a.w-b.w);
        public static Vector4 operator-(Vector4 a) => new(-a.x,-a.y,-a.z,-a.w);
        public static Vector4 operator*(Vector4 a, float d) => new(a.x*d, a.y*d, a.z*d, a.w*d);
        public static Vector4 operator*(float d, Vector4 a) => a*d;
        public static Vector4 operator/(Vector4 a, float d) => new(a.x/d, a.y/d, a.z/d, a.w/d);
        public static bool operator==(Vector4 a, Vector4 b) => (a-b).sqrMagnitude < 9.99999944E-11f;
        public static bool operator!=(Vector4 a, Vector4 b) => !(a==b);
        public static implicit operator Vector4(Vector3 v) => new(v.x, v.y, v.z, 0);
        public static implicit operator Vector3(Vector4 v) => new(v.x, v.y, v.z);
        public static implicit operator Vector4(Vector2 v) => new(v.x, v.y, 0, 0);
        public float this[int i] => i==0?x:i==1?y:i==2?z:w;
        public bool Equals(Vector4 other) => this==other;
        public override bool Equals(object? o) => o is Vector4 v && this==v;
        public override int GetHashCode() => HashCode.Combine(x,y,z,w);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
    }

    // -------------------------------------------------------------------------
    // Bounds
    // -------------------------------------------------------------------------
    public partial struct Bounds
    {
        public Vector3 center, size;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }
        public Vector3 extents => size * 0.5f;
        public Vector3 min => center - extents;
        public Vector3 max => center + extents;
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
        public bool Intersects(Bounds b) => min.x <= b.max.x && max.x >= b.min.x && min.y <= b.max.y && max.y >= b.min.y && min.z <= b.max.z && max.z >= b.min.z;
    }
}
