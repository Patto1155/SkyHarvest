// Play-mode visual verification for unattended (and interactive) runs.
//
// Run with a GUI editor instance (NOT -batchmode; screenshots need a GPU context).
// Prefer the de-elevated launcher so Unity's "running as admin" modal never blocks:
//   powershell -File tools/run-unity-deelevated.ps1 -UnityArgs '-projectPath D:/APATPROJECTS/SkyHarvest -executeMethod PlayModeScreenshots.Run -logFile artifacts/playmode.log'
//
// Enters Play mode on Assets/Scenes/Main.unity, auto-clicks New Game, captures the
// Game view at several frame marks into artifacts/screenshots/, then exits with code 0.
//
// IMPORTANT: entering Play mode normally triggers a domain reload that destroys static
// state and the EditorApplication.update subscription, so the capture loop would silently
// never run. Run() disables domain + scene reload for the session to keep the loop alive.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayModeScreenshots
{
    private static readonly int[] CaptureFrames = { 30, 90, 300, 600 };
    private static int _nextCapture;
    private static int _newGameClickedAtFrame = -1;
    private static string _outDir = "";
    private static bool _running;

    public static void Run()
    {
        _outDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "screenshots");
        Directory.CreateDirectory(_outDir);
        foreach (var old in Directory.GetFiles(_outDir, "*.png")) File.Delete(old);

        // Keep static state + the update subscription alive across the Play-mode transition.
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        _nextCapture = 0;
        _newGameClickedAtFrame = -1;
        _running = true;
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!_running) return;
        if (!EditorApplication.isPlaying)
        {
            // Still warming up into Play mode.
            return;
        }

        int frame = Time.frameCount;

        // Click "New Game" once the menu exists so later captures show the island.
        if (_newGameClickedAtFrame < 0 && frame >= 60)
        {
            var btn = FindButton("New Game", "NewGame", "Play", "Start", "Continue");
            if (btn != null)
            {
                btn.onClick.Invoke();
                _newGameClickedAtFrame = frame;
                Debug.Log($"[PlayModeScreenshots] clicked '{btn.name}' at frame {frame}");
            }
        }

        if (_nextCapture < CaptureFrames.Length && frame >= CaptureFrames[_nextCapture])
        {
            string file = Path.Combine(_outDir, $"frame_{CaptureFrames[_nextCapture]:D4}.png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log($"[PlayModeScreenshots] captured {file} (frame {frame})");
            _nextCapture++;
        }

        // A few frames after the last capture, stop play mode and exit.
        if (_nextCapture >= CaptureFrames.Length && frame >= CaptureFrames[^1] + 30)
        {
            _running = false;
            EditorApplication.update -= OnUpdate;
            EditorApplication.isPlaying = false;
            // Let the async screenshot writer flush before killing the editor.
            EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
            {
                Debug.Log("[PlayModeScreenshots] done, exiting");
                EditorApplication.Exit(0);
            };
        }
    }

    private static Button FindButton(params string[] nameHints)
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        var hints = new List<string>(nameHints);
        foreach (var b in buttons)
        {
            string label = b.GetComponentInChildren<Text>(true)?.text ?? "";
            foreach (var h in hints)
                if (b.name.Contains(h) || label.Contains(h))
                    return b;
        }
        return null;
    }
}
