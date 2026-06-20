// Captures one Play-mode screenshot of the carved stair boundary for visual verification.
// Run: bash tools/stair-shot.sh  →  artifacts/screenshots/stair_verify.png
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Island;

public static class PlayModeStairShot
{
    private static string _outFile = "";
    private static bool _running;
    private static int _step;
    private static int _actAtFrame;
    private static bool _newGameClicked;

    public static void Run()
    {
        _outFile = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "screenshots", "stair_verify.png");
        Directory.CreateDirectory(Path.GetDirectoryName(_outFile)!);
        if (File.Exists(_outFile)) File.Delete(_outFile);

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        _step = 0;
        _actAtFrame = 0;
        _newGameClicked = false;
        _running = true;
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!_running || !EditorApplication.isPlaying) return;

        int frame = Time.frameCount;

        if (!_newGameClicked)
        {
            if (frame < 60) return;
            var btn = FindButton("New Game", "NewGame", "Play", "Start");
            if (btn != null)
            {
                btn.onClick.Invoke();
                _newGameClicked = true;
                _actAtFrame = frame + 45;
                Debug.Log($"[StairShot] New Game at frame {frame}");
            }
            return;
        }

        if (frame < _actAtFrame) return;

        switch (_step)
        {
            case 0:
                CarveStairs();
                FrameStairCamera(Camera.main);
                _actAtFrame = frame + 30;
                _step = 1;
                break;
            case 1:
                ScreenCapture.CaptureScreenshot(_outFile);
                Debug.Log($"[StairShot] captured {_outFile}");
                _actAtFrame = frame + 20;
                _step = 2;
                break;
            case 2:
                _running = false;
                EditorApplication.update -= OnUpdate;
                EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.Exit(0);
                };
                break;
        }
    }

    private static void CarveStairs()
    {
        var island = GameManager.Instance?.CurrentIsland;
        if (island == null) return;
        if (!island.StairsCarved)
            island.CarveStairs(StarterIsland.FrontStairCell);
    }

    private static void FrameStairCamera(Camera? cam)
    {
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;
        cam.orthographic = true;
        cam.orthographicSize = 1.05f;
        // Boundary between (1,1) and (1,2).
        Vector2 wp = GridMath.GridToWorld(StarterIsland.BackStairCell, 1f);
        cam.transform.position = new Vector3(wp.x, wp.y - 0.05f, -10f);
        cam.transform.rotation = Quaternion.identity;
    }

    private static Button? FindButton(params string[] names)
    {
        foreach (var btn in Object.FindObjectsOfType<Button>(true))
        {
            var label = btn.GetComponentInChildren<Text>();
            if (label == null) continue;
            foreach (var n in names)
                if (label.text.Contains(n)) return btn;
        }
        return null;
    }
}
