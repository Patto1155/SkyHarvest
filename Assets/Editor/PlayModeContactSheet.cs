// Token-efficient visual iteration harness.
//
// Captures TWO framed views of a fresh game — a wide "island floating in the sky"
// establishing shot and a zoomed cozy detail shot — and stitches them into ONE
// PNG (artifacts/screenshots/contact_sheet.png). One image = one Read per loop,
// instead of eyeballing four separate frames.
//
// Pairs with VisualConfig (StreamingAssets/visual.json): edit the JSON, re-run
// tools/shot.sh, look at the single contact sheet. No C# recompile to tune values.
//
// Run via tools/shot.sh (handles the admin-dialog dismisser + quiet logging):
//   Unity.exe -projectPath . -executeMethod PlayModeContactSheet.Run -logFile artifacts/contact.log
//
// Domain + scene reload are disabled for the session so the capture state machine
// survives the Play-mode transition (same reason as PlayModeScreenshots).
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Building;
using SkyHarvest.Data;
using SkyHarvest.Farming;
using SkyHarvest.Island;

public static class PlayModeContactSheet
{
    private static string _outDir = "";
    private static bool _running;
    private static int _step;
    private static int _actAtFrame;
    private static bool _newGameClicked;
    private static readonly List<string> _shots = new();

    // ortho sizes for the two framings
    private const float WideSize   = 4.4f;   // whole island + surrounding sky
    private const float DetailSize = 2.0f;   // cozy close-up of structures/avatar

    public static void Run()
    {
        _outDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "screenshots");
        Directory.CreateDirectory(_outDir);
        foreach (var old in Directory.GetFiles(_outDir, "sheet_*.png")) File.Delete(old);
        string sheet = Path.Combine(_outDir, "contact_sheet.png");
        if (File.Exists(sheet)) File.Delete(sheet);

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        _step = 0;
        _actAtFrame = 0;
        _newGameClicked = false;
        _shots.Clear();
        _running = true;
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!_running) return;
        if (!EditorApplication.isPlaying) return;

        int frame = Time.frameCount;

        // 1. Click New Game once the menu exists.
        if (!_newGameClicked)
        {
            if (frame < 60) return;
            var btn = FindButton("New Game", "NewGame", "Play", "Start");
            if (btn != null)
            {
                btn.onClick.Invoke();
                _newGameClicked = true;
                _actAtFrame = frame + 40;   // let the island settle, then seed demo content
                Debug.Log($"[ContactSheet] New Game at frame {frame}");
            }
            return;
        }

        if (frame < _actAtFrame) return;

        var cam = Camera.main;
        switch (_step)
        {
            case 0:  // seed demo content (forge glow + ripe crops), then frame wide
                SeedDemoContent();
                FreezeCamera(cam, WideSize);
                _actAtFrame = frame + 25;   // let new visuals render
                _step = 1;
                break;
            case 1:  // capture wide
                Capture("sheet_0_wide.png");
                _actAtFrame = frame + 20;
                _step = 2;
                break;
            case 2:  // frame the cozy detail shot
                FreezeCamera(cam, DetailSize);
                _actAtFrame = frame + 20;
                _step = 3;
                break;
            case 3:  // capture detail
                Capture("sheet_1_detail.png");
                _actAtFrame = frame + 25;   // let async writes flush
                _step = 4;
                break;
            case 4:  // stitch + exit
                _running = false;
                EditorApplication.update -= OnUpdate;
                StitchAndExit();
                break;
        }
    }

    /// <summary>Place a forge (warm glow) and a few ripe crops (golden glow) so the contact
    /// sheet shows off the cozy-pass lighting — a fresh New Game has no structures or crops.</summary>
    private static void SeedDemoContent()
    {
        try
        {
            var island = GameManager.Instance?.CurrentIsland;
            if (island == null) return;
            if (!island.StairsCarved)
                island.CarveStairs(StarterIsland.FrontStairCell);
            var bmc = BuildModeController.Instance;
            var renderer = Object.FindObjectOfType<IslandRenderer>();

            // Forge on a rocky shoulder (fallback: any non-edge cell) for the fire glow.
            IslandCell forgeCell = FindCell(island, c =>
                c.Terrain == TerrainType.RockyPlateau && !StructureRegistry.Instance.HasStructureAt(c.GridPos));
            forgeCell ??= FindCell(island, c => !c.IsEdge && !StructureRegistry.Instance.HasStructureAt(c.GridPos));
            if (forgeCell != null && bmc != null)
            {
                bmc.PlaceStructure(forgeCell.GridPos, GameDatabase.GetStructure("forge"));
                Debug.Log($"[ContactSheet] forge at {forgeCell.GridPos}");
            }

            // A few ripe crops on fertile soil for the golden glow.
            int planted = 0;
            foreach (var kvp in island.Cells)
            {
                if (planted >= 4) break;
                var cell = kvp.Value;
                if (!TerrainProperties.CanPlaceCrops(cell.Terrain) || cell.Soil.IsTilled) continue;
                if (forgeCell != null && cell.GridPos == forgeCell.GridPos) continue;
                var plot = FarmingActions.TryTill(cell, island, renderer);
                if (plot?.Crop == null) continue;
                var def = GameDatabase.GetCrop(plot.Crop.CropId);
                if (def != null)
                {
                    // Re-create the crop fully grown (savedProgress = 1f → ripe → golden glow).
                    plot.Crop = new CropState(def.CropId, def.GrowthTimeMinutes, def.GrowthStages,
                        def.WaterConsumptionPerMinute, 1f, 1f);
                    plot.RefreshVisuals();
                }
                planted++;
            }
            Debug.Log($"[ContactSheet] planted {planted} ripe crops");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ContactSheet] SeedDemoContent failed: {e.Message}");
        }
    }

    private static IslandCell FindCell(IslandData island, System.Func<IslandCell, bool> pred)
    {
        foreach (var kvp in island.Cells)
            if (pred(kvp.Value)) return kvp.Value;
        return null;
    }

    /// <summary>Stop CameraFollow fighting us and point the camera at the island centre (origin).</summary>
    private static void FreezeCamera(Camera cam, float orthoSize)
    {
        if (cam == null) return;
        var follow = cam.GetComponent<SkyHarvest.Core.CameraFollow>();
        if (follow != null) follow.enabled = false;
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.transform.position = new Vector3(0f, 0.5f, -10f);
        cam.transform.rotation = Quaternion.identity;
    }

    private static void Capture(string name)
    {
        string file = Path.Combine(_outDir, name);
        ScreenCapture.CaptureScreenshot(file);
        _shots.Add(file);
        Debug.Log($"[ContactSheet] captured {name}");
    }

    private static void StitchAndExit()
    {
        // Defer twice so the async screenshot writer has flushed both PNGs to disk.
        EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
        {
            try
            {
                Stitch();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContactSheet] stitch failed: {e.Message}");
            }
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
            {
                Debug.Log("[ContactSheet] done");
                EditorApplication.Exit(0);
            };
        };
    }

    private static void Stitch()
    {
        var textures = new List<Texture2D>();
        foreach (var path in _shots)
        {
            if (!File.Exists(path)) continue;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(File.ReadAllBytes(path))) textures.Add(tex);
        }
        if (textures.Count == 0) { Debug.LogError("[ContactSheet] no shots to stitch"); return; }

        // Side-by-side layout. Match heights to the tallest; pad widths.
        int gap = 8;
        int height = 0, width = 0;
        foreach (var t in textures) { height = Mathf.Max(height, t.height); width += t.width + gap; }
        width -= gap;

        var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var bg = new Color(0.04f, 0.04f, 0.06f, 1f);
        var clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = bg;
        sheet.SetPixels(clear);

        int x = 0;
        foreach (var t in textures)
        {
            int y = height - t.height;   // top-align
            sheet.SetPixels(x, y, t.width, t.height, t.GetPixels());
            x += t.width + gap;
        }
        sheet.Apply();

        string outPath = Path.Combine(_outDir, "contact_sheet.png");
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        Debug.Log($"[ContactSheet] wrote {outPath} ({width}x{height})");
    }

    private static Button FindButton(params string[] hints)
    {
        var buttons = Object.FindObjectsOfType<Button>(true);
        foreach (var b in buttons)
        {
            string label = b.GetComponentInChildren<Text>(true)?.text ?? "";
            foreach (var h in hints)
                if (b.name.Contains(h) || label.Contains(h)) return b;
        }
        return null;
    }
}
