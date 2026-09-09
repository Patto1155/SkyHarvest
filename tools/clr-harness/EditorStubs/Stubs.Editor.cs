// UnityEditor stubs — just enough surface to COMPILE Assets/Editor/*.cs in the headless
// harness. These scripts drive the real editor (Play-mode verify, screenshots, builds) and
// nothing else compiles them, so without this a broken verify harness only surfaces when a
// human opens Unity. Signatures must match the real API; bodies are inert.
using System;
using UnityEngine;

namespace UnityEditor
{
    public static class EditorApplication
    {
        public static event Action? update;
        public static event Action? delayCall;
        public static bool isPlaying { get; set; }
        public static bool isPaused { get; set; }
        public static void Exit(int code) { }
        public static void Step() { }
        // Referenced so the compiler does not warn the events are never used.
        internal static void _Pump() { update?.Invoke(); delayCall?.Invoke(); }
    }

    [Flags]
    public enum EnterPlayModeOptions
    {
        None = 0,
        DisableDomainReload = 1,
        DisableSceneReload = 2,
    }

    public static class EditorSettings
    {
        public static bool enterPlayModeOptionsEnabled { get; set; }
        public static EnterPlayModeOptions enterPlayModeOptions { get; set; }
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object o) { }
        public static bool DisplayDialog(string title, string message, string ok) => false;
    }

    public static class AssetDatabase
    {
        public static void Refresh() { }
        public static void SaveAssets() { }
        public static string[] FindAssets(string filter) => Array.Empty<string>();
        public static string GUIDToAssetPath(string guid) => string.Empty;
    }

    public static class EditorPrefs
    {
        public static void SetInt(string key, int value) { }
        public static int GetInt(string key, int defaultValue = 0) => defaultValue;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string itemName) { }
        public MenuItemAttribute(string itemName, bool isValidateFunction) { }
        public MenuItemAttribute(string itemName, bool isValidateFunction, int priority) { }
    }

    public abstract class AssetPostprocessor
    {
        public string assetPath { get; set; } = string.Empty;
        public AssetImporter assetImporter { get; set; } = new TextureImporter();
    }

    public abstract class AssetImporter { }

    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterNPOTScale npotScale { get; set; }
        public bool mipmapEnabled { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public bool alphaIsTransparency { get; set; }
        public bool isReadable { get; set; }
        public int maxTextureSize { get; set; }
        public TextureWrapMode wrapMode { get; set; }
    }

    public enum BuildTarget { StandaloneWindows64, Android, iOS, StandaloneLinux64 }
    public enum BuildTargetGroup { Standalone, Android, iOS, Unknown }

    [Flags]
    public enum BuildOptions { None = 0, Development = 1, AutoRunPlayer = 4 }

    public struct BuildPlayerOptions
    {
        public string[] scenes;
        public string locationPathName;
        public BuildTarget target;
        public BuildTargetGroup targetGroup;
        public BuildOptions options;
    }

    public static class BuildPipeline
    {
        public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) => new();
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; } = string.Empty;
        public static string productName { get; set; } = string.Empty;
        public static string bundleVersion { get; set; } = string.Empty;
        public static bool runInBackground { get; set; }
        public static bool defaultIsNativeResolution { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool resizableWindow { get; set; }
        public static UnityEngine.FullScreenMode fullScreenMode { get; set; }
        public static void SetScriptingBackend(BuildTargetGroup group, int backend) { }
        public static void SetApplicationIdentifier(BuildTargetGroup group, string identifier) { }
    }

    public class EditorBuildSettingsScene
    {
        public string path = string.Empty;
        public bool enabled;
        public EditorBuildSettingsScene() { }
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; } = Array.Empty<EditorBuildSettingsScene>();
    }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        // Scene lives in UnityEngine (not UnityEngine.SceneManagement) in Stubs.Core.cs.
        public static UnityEngine.Scene OpenScene(string path) => default;
        public static bool SaveOpenScenes() => true;
    }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }

    public class BuildSummary
    {
        public BuildResult result = BuildResult.Succeeded;
        public ulong totalSize;
        public int totalErrors;
        public int totalWarnings;
        public TimeSpan totalTime;
        public string outputPath = string.Empty;
    }

    public class BuildReport
    {
        public BuildSummary summary = new();
    }
}

namespace UnityEngine
{
    public static class ScreenCapture
    {
        public static void CaptureScreenshot(string filename, int superSize = 0) { }
    }
}
