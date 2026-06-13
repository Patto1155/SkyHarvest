// Standalone Windows build pipeline + publishing metadata.
//
// Run headlessly (batch mode is fine for builds — no Game-view GPU needed):
//   Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWindows -logFile artifacts/build.log
//
// On this machine Unity always shows the "running as admin" modal even in batch mode,
// so launch it with tools/dismiss-unity-admin-dialog.ps1 running alongside.
//
// Output: Builds/Windows/SkyHarvest.exe
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private const string Version = "1.0.0";
    private const string Company = "Patrick McCrudden";
    private const string Product = "Sky Harvest";

    [MenuItem("SkyHarvest/Build Windows")]
    public static void BuildWindows()
    {
        ApplyPublishSettings();

        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        Directory.CreateDirectory(outDir);
        string exe = Path.Combine(outDir, "SkyHarvest.exe");

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0)
            scenes = new[] { "Assets/Scenes/Main.unity" };

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        Debug.Log($"[BuildScript] result={summary.result} size={summary.totalSize} bytes " +
                  $"time={summary.totalTime} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[BuildScript] BUILD FAILED: {summary.result}");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"[BuildScript] BUILD OK -> {exe}");
            EditorApplication.Exit(0);
        }
    }

    private static void ApplyPublishSettings()
    {
        PlayerSettings.companyName = Company;
        PlayerSettings.productName = Product;
        PlayerSettings.bundleVersion = Version;

        // 2D pixel game: windowed by default, no Unity splash where allowed, crisp scaling.
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Standalone, "com.patrickmccrudden.skyharvest");

        Debug.Log($"[BuildScript] applied publish settings v{Version}");
    }
}
