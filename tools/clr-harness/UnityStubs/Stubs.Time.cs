// UnityEngine stub — Time, Random, Application, Debug, Resources, ParticleSystem, PlayerPrefs
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // Time
    // -------------------------------------------------------------------------
    public static partial class Time
    {
        public static float deltaTime { get; set; } = 0.016667f;  // ~60fps
        public static float time { get; set; }
        public static float unscaledTime { get; set; }
        public static float unscaledDeltaTime { get; set; } = 0.016667f;
        public static float fixedDeltaTime { get; set; } = 0.02f;
        public static float fixedUnscaledDeltaTime => fixedDeltaTime;
        public static float timeScale { get; set; } = 1f;
        public static int frameCount { get; set; }
        public static float realtimeSinceStartup { get; set; }
        public static float timeSinceLevelLoad { get; set; }
        public static float smoothDeltaTime => deltaTime;
        public static float maximumDeltaTime { get; set; } = 0.3333f;
        public static float maximumParticleDeltaTime { get; set; } = 0.03f;
        public static int renderedFrameCount { get; set; }
        public static double timeAsDouble { get; set; }
        public static double unscaledTimeAsDouble { get; set; }
        public static double fixedTimeAsDouble { get; set; }
        public static float fixedTime { get; set; }
        public static float captureDeltaTime { get; set; }
        public static int captureFramerate { get; set; }
        public static bool inFixedTimeStep => false;
    }

    // -------------------------------------------------------------------------
    // Random
    // -------------------------------------------------------------------------
    public static partial class Random
    {
        private static System.Random _rng = new System.Random();

        public static float value => (float)_rng.NextDouble();

        public static float Range(float min, float max) => min + (float)_rng.NextDouble() * (max - min);
        public static int Range(int min, int maxExclusive) => _rng.Next(min, maxExclusive);

        public static Vector2 insideUnitCircle
        {
            get
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                float r = (float)Math.Sqrt(_rng.NextDouble());
                return new Vector2(r * MathF.Cos(angle), r * MathF.Sin(angle));
            }
        }

        public static Vector3 insideUnitSphere
        {
            get
            {
                float z = (float)(_rng.NextDouble() * 2 - 1);
                float t = (float)(_rng.NextDouble() * Math.PI * 2);
                float r = (float)Math.Sqrt(1 - z * z) * (float)Math.Pow(_rng.NextDouble(), 1.0/3.0);
                return new Vector3(r * MathF.Cos(t), r * MathF.Sin(t), z * r);
            }
        }

        public static Vector3 onUnitSphere
        {
            get
            {
                var v = insideUnitSphere; float m = v.magnitude;
                return m > 0 ? new Vector3(v.x/m, v.y/m, v.z/m) : Vector3.up;
            }
        }

        public static Quaternion rotation => Quaternion.Euler(Range(0,360), Range(0,360), Range(0,360));
        public static Quaternion rotationUniform => rotation;
        public static Color ColorHSV() => Color.HSVToRGB(value, value, value);
        public static Color ColorHSV(float hMin, float hMax, float sMin, float sMax, float vMin, float vMax)
            => Color.HSVToRGB(Range(hMin, hMax), Range(sMin, sMax), Range(vMin, vMax));

        public static void InitState(int seed) => _rng = new System.Random(seed);

        public struct State { public int seed; }
        public static State state { get => new State { seed = 0 }; set => InitState(value.seed); }
    }

    // -------------------------------------------------------------------------
    // Application
    // -------------------------------------------------------------------------
    public static partial class Application
    {
        public static string persistentDataPath { get; set; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SkyHarvest");
        public static string dataPath { get; set; } = "./Assets";
        public static string streamingAssetsPath { get; set; } = "./Assets/StreamingAssets";
        public static string temporaryCachePath { get; set; } = System.IO.Path.GetTempPath();
        public static string version { get; set; } = "0.1.0";
        public static string unityVersion { get; set; } = "2022.3.0f1";
        public static string identifier { get; set; } = "com.skyharvest.game";
        public static string productName { get; set; } = "SkyHarvest";
        public static string companyName { get; set; } = "SkyHarvest";
        public static RuntimePlatform platform { get; } = RuntimePlatform.WindowsPlayer;
        public static bool isEditor { get; } = true;
        public static bool isPlaying { get; } = true;
        public static bool isBatchMode { get; } = true;
        public static bool isFocused { get; } = true;
        public static bool runInBackground { get; set; } = true;
        public static int targetFrameRate { get; set; } = -1;
        public static bool isWebPlayer { get; } = false;
        public static SystemLanguage systemLanguage { get; } = SystemLanguage.English;
        public static NetworkReachability internetReachability { get; } = NetworkReachability.NotReachable;

        public static event Action? quitting;
        public static event Action<bool>? focusChanged;
        public static event Action<string>? logMessageReceived;
        public static event Application.LogCallback? logMessageReceivedThreaded;

        public delegate void LogCallback(string logString, string stackTrace, LogType type);

        public static void Quit(int exitCode = 0) { quitting?.Invoke(); }
        public static void OpenURL(string url) { }
        public static bool CanStreamedLevelBeLoaded(int levelIndex) => true;
        public static bool CanStreamedLevelBeLoaded(string levelName) => true;
        public static void CaptureScreenshot(string filename, int superSize = 0) { }
        public static bool HasProLicense() => false;
        public static int GetStackTraceLogType(LogType logType) => 0;
        public static void SetStackTraceLogType(LogType logType, int stackTraceType) { }
    }

    public enum RuntimePlatform { OSXEditor, OSXPlayer, WindowsPlayer, OSXDashboardPlayer, WindowsEditor, LinuxPlayer, LinuxEditor, IPhonePlayer, Android, WebGLPlayer }
    public enum SystemLanguage { English, French, German, Spanish, Japanese, Korean, ChineseSimplified, ChineseTraditional, Unknown }
    public enum NetworkReachability { NotReachable, ReachableViaCarrierDataNetwork, ReachableViaLocalAreaNetwork }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------
    public static partial class Debug
    {
        public static bool isDebugBuild => true;
        public static bool developerConsoleVisible { get; set; }

        public static void Log(object? message) => Console.WriteLine($"[LOG] {message}");
        public static void Log(object? message, Object? context) => Log(message);
        public static void LogWarning(object? message) => Console.WriteLine($"[WARN] {message}");
        public static void LogWarning(object? message, Object? context) => LogWarning(message);
        public static void LogError(object? message) => Console.WriteLine($"[ERROR] {message}");
        public static void LogError(object? message, Object? context) => LogError(message);
        public static void LogException(Exception exception) => Console.WriteLine($"[EXCEPTION] {exception}");
        public static void LogException(Exception exception, Object? context) => LogException(exception);
        public static void LogFormat(string format, params object[] args) => Log(string.Format(format, args));
        public static void LogWarningFormat(string format, params object[] args) => LogWarning(string.Format(format, args));
        public static void LogErrorFormat(string format, params object[] args) => LogError(string.Format(format, args));
        public static void Assert(bool condition) { if (!condition) LogError("Assertion failed"); }
        public static void Assert(bool condition, object? message) { if (!condition) LogError($"Assertion failed: {message}"); }
        public static void DrawLine(Vector3 start, Vector3 end, Color color = default, float duration = 0f, bool depthTest = true) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color = default, float duration = 0f, bool depthTest = true) { }
        public static void Break() { }
        public static void ClearDeveloperConsole() { }
    }

    public enum LogType { Log, Warning, Error, Assert, Exception }

    // -------------------------------------------------------------------------
    // Resources
    // -------------------------------------------------------------------------
    public static partial class Resources
    {
        // Harness: returns null for all loads — game code must null-check
        public static T? Load<T>(string path) where T : Object => null;
        public static Object? Load(string path) => null;
        public static Object? Load(string path, Type systemTypeInstance) => null;
        public static T? GetBuiltinResource<T>(string path) where T : Object
        {
            // Special: return a stub Font so font code doesn't throw
            if (typeof(T) == typeof(Font)) return (T?)(Object?)new Font { name = path };
            return null;
        }
        public static T[] LoadAll<T>(string path) where T : Object => Array.Empty<T>();
        public static void UnloadAsset(Object? assetToUnload) { }
        public static System.Collections.IEnumerator UnloadUnusedAssets() { yield break; }
    }

    // -------------------------------------------------------------------------
    // PlayerPrefs
    // -------------------------------------------------------------------------
    public static partial class PlayerPrefs
    {
        private static readonly Dictionary<string, string> _data = new();

        public static void SetInt(string key, int value) => _data[key] = value.ToString();
        public static int GetInt(string key, int defaultValue = 0) => _data.TryGetValue(key, out var v) && int.TryParse(v, out int i) ? i : defaultValue;
        public static void SetFloat(string key, float value) => _data[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static float GetFloat(string key, float defaultValue = 0f) => _data.TryGetValue(key, out var v) && float.TryParse(v, out float f) ? f : defaultValue;
        public static void SetString(string key, string value) => _data[key] = value;
        public static string GetString(string key, string defaultValue = "") => _data.TryGetValue(key, out var v) ? v : defaultValue;
        public static bool HasKey(string key) => _data.ContainsKey(key);
        public static void DeleteKey(string key) => _data.Remove(key);
        public static void DeleteAll() => _data.Clear();
        public static void Save() { }
    }

    // -------------------------------------------------------------------------
    // ParticleSystem (minimal — Play/Stop/main/emission as no-op chainable structs)
    // -------------------------------------------------------------------------
    public partial class ParticleSystem : Component
    {
        public MainModule main { get; } = new MainModule();
        public EmissionModule emission { get; } = new EmissionModule();
        public ShapeModule shape { get; } = new ShapeModule();
        public ColorOverLifetimeModule colorOverLifetime { get; } = new ColorOverLifetimeModule();
        public SizeOverLifetimeModule sizeOverLifetime { get; } = new SizeOverLifetimeModule();
        public VelocityOverLifetimeModule velocityOverLifetime { get; } = new VelocityOverLifetimeModule();
        public bool isPlaying { get; private set; }
        public bool isStopped => !isPlaying;
        public int particleCount { get; private set; }

        public void Play(bool withChildren = true) { isPlaying = true; }
        public void Stop(bool withChildren = true, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting) { isPlaying = false; }
        public void Pause(bool withChildren = true) { isPlaying = false; }
        public void Clear(bool withChildren = true) { particleCount = 0; }
        public void Emit(int count) { particleCount += count; }

        public struct MainModule
        {
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxGradient startColor { get; set; }
            public MinMaxCurve startSize { get; set; }
            public float duration { get; set; }
            public bool loop { get; set; }
            public float startDelay { get; set; }
            public int maxParticles { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
        }

        public struct EmissionModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
            public MinMaxCurve rateOverDistance { get; set; }
        }

        public struct ShapeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
            public float angle { get; set; }
            public Vector3 scale { get; set; }
        }

        public struct ColorOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxGradient color { get; set; }
        }

        public struct SizeOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve size { get; set; }
        }

        public struct VelocityOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve x { get; set; }
            public MinMaxCurve y { get; set; }
            public MinMaxCurve z { get; set; }
        }
    }

    public enum ParticleSystemStopBehavior { StopEmitting, StopEmittingAndClear }
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemShapeType { Sphere, SphereShell, Hemisphere, HemisphereShell, Cone, Box, Mesh, Circle, Edge }

    public struct MinMaxCurve
    {
        public float constant;
        public float constantMin, constantMax;
        public MinMaxCurve(float constant) { this.constant = constant; constantMin = constantMax = constant; }
        public MinMaxCurve(float min, float max) { constant = (min + max) * 0.5f; constantMin = min; constantMax = max; }
        public static implicit operator MinMaxCurve(float v) => new MinMaxCurve(v);
        public float Evaluate(float time) => constant;
    }

    public struct MinMaxGradient
    {
        public Color color;
        public MinMaxGradient(Color c) { color = c; }
        public static implicit operator MinMaxGradient(Color c) => new MinMaxGradient(c);
        public Color Evaluate(float time) => color;
    }
}
