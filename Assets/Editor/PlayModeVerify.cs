// Live controls/features verification (NEXT_SESSION task 4).
// Raw keypresses can't be injected into legacy Input, so this drives the exact
// methods the (now Bootstrap-centralized) hotkeys call, one frame-scheduled step
// at a time, and writes artifacts/verify_report.md + screenshots.
//
// Run like PlayModeScreenshots (GUI editor + dialog dismisser):
//   Unity.exe -projectPath D:/APATPROJECTS/SkyHarvest -executeMethod PlayModeVerify.Run -logFile artifacts/verify.log
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Building;
using SkyHarvest.Data;
using SkyHarvest.Farming;
using SkyHarvest.Island;
using SkyHarvest.Player;
using SkyHarvest.UI;

public static class PlayModeVerify
{
    private class Step
    {
        public int Frame;
        public string Name;
        public Action Action;
        public bool Done;
    }

    private static readonly List<Step> _steps = new();
    private static readonly StringBuilder _report = new();
    private static string _outDir = "";
    private static bool _running;
    private static bool _newGameClicked;

    // cross-step state
    private static PlayerController _player;
    private static IslandData _island;
    private static CropPlot _plot;
    private static SkyHarvest.Workshop.WorkshopBase _mill;
    private static bool _cropHarvested;
    private static bool _debrisScavenged;
    private static int _islandExpandedCount;
    private static Vector2Int _sitePos;

    public static void Run()
    {
        _outDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "verify");
        Directory.CreateDirectory(_outDir);
        foreach (var old in Directory.GetFiles(_outDir, "*.png")) File.Delete(old);

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions =
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        _report.Clear();
        _report.AppendLine("# Live verification report — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        _report.AppendLine();
        _report.AppendLine("| Check | Result | Detail |");
        _report.AppendLine("|---|---|---|");

        BuildSteps();
        _running = true;
        _newGameClicked = false;
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void BuildSteps()
    {
        _steps.Clear();
        Add(120, "Setup: refs + grant materials", StepSetup);
        Add(140, "Camera: default ortho size 2.5 + CameraFollow zoom fields", StepCamera);
        Add(160, "Tools: hotbar slots equip (1-4 handler path)", StepTools);
        Add(180, "Inventory: Tab handler path → Toggle opens/closes", StepInventory);
        Add(200, "Build menu: B handler path → menu + ghost", StepBuildMenu);
        Add(230, "Staged build: place construction site (mouse-L path)", StepPlaceSite);
        Add(260, "Staged build: deliver via E path → completes to structure", StepDeliver);
        Add(290, "Farming: till + sow", StepTillSow);
        Add(300, "Farming: water + grow (fast-forward game ticks)", StepGrow);
        Add(380, "Farming: harvest ripe crop (E path)", StepHarvest);
        Add(400, "Workshop: place mill, start recipe, process, collect", StepWorkshop);
        Add(440, "Storage: place crate, open UI, transfer item", StepStorage);
        Add(470, "Skynet: place on cliff edge, offline accrual, collect", StepSkynet);
        Add(485, "Expansion: scaffold an edge cell → island grows outward", StepExpansion);
        Add(500, "Debris: spawn, land, scavenge", StepDebrisSpawn);
        Add(700, "Debris: scavenge after landing", StepDebrisScavenge);
        Add(720, "Weather: force HeavyStorm, effects active", StepWeather);
        Add(800, "Save: file written, in-progress site persisted", StepSave);
        Add(820, "Esc path: pause menu toggles (pauses game)", StepPause);
        Add(840, "Inspector: panel component present (Q path)", StepInspector);
        Add(900, "Finish", Finish);
    }

    private static void Add(int frame, string name, Action a) =>
        _steps.Add(new Step { Frame = frame, Name = name, Action = a });

    private static void OnUpdate()
    {
        if (!_running || !EditorApplication.isPlaying) return;
        int frame = Time.frameCount;

        if (!_newGameClicked && frame >= 60)
        {
            var btn = FindButton("New Game", "NewGame");
            if (btn != null)
            {
                btn.onClick.Invoke();
                _newGameClicked = true;
                Debug.Log($"[PlayModeVerify] clicked New Game at frame {frame}");
            }
            return;
        }
        if (!_newGameClicked) return;

        foreach (var s in _steps)
        {
            if (s.Done || frame < s.Frame) continue;
            s.Done = true;
            try { s.Action(); }
            catch (Exception ex) { Fail(s.Name, ex.GetType().Name + ": " + ex.Message); }
            break; // one step per frame
        }
    }

    // ---------------- steps ----------------

    private static void StepSetup()
    {
        _player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        _island = GameManager.Instance?.CurrentIsland;
        if (_player == null || _island == null)
        {
            Fail("Setup", $"player={(object)_player != null}, island={_island != null}");
            return;
        }
        var inv = _player.Inventory;
        inv.TryAdd("wood", 30); inv.TryAdd("scrap", 20); inv.TryAdd("stone", 20);
        inv.TryAdd("rope", 10); inv.TryAdd("nails", 10); inv.TryAdd("coal", 10);
        inv.TryAdd("iron_ore", 10); inv.TryAdd("wheat_seed", 5); inv.TryAdd("wheat", 6);
        EventBus.Subscribe<CropHarvestedEvent>(_ => _cropHarvested = true);
        EventBus.Subscribe<DebrisScavengedEvent>(_ => _debrisScavenged = true);
        EventBus.Subscribe<IslandExpandedEvent>(_ => _islandExpandedCount++);
        Pass("Setup", "player + island live; materials granted");
    }

    private static void StepCamera()
    {
        var cam = Camera.main;
        var follow = cam?.GetComponent<CameraFollow>();
        bool sizeOk = cam != null && Mathf.Abs(cam.orthographicSize - 2.5f) < 0.05f;
        bool zoomOk = follow != null && follow.MinZoom < follow.MaxZoom;
        Check("Camera default + zoom component", sizeOk && zoomOk,
            $"ortho={cam?.orthographicSize:F2}, follow={follow != null}, target={follow?.Target != null}");
        Shot("camera_default");
    }

    private static void StepTools()
    {
        var tools = _player.GetComponent<ToolSystem>();
        tools.EquipBySlot(1);
        bool a = tools.EquippedTool == ToolType.WateringCan;
        tools.EquipBySlot(3);
        bool b = tools.EquippedTool == ToolType.Hammer;
        tools.EquipBySlot(0);
        bool c = tools.EquippedTool == ToolType.Hoe;
        Check("Tool hotbar equip 1-4", a && b && c, $"slots → WateringCan:{a} Hammer:{b} Hoe:{c}");
    }

    private static void StepInventory()
    {
        var ui = UnityEngine.Object.FindObjectOfType<InventoryUI>();
        ui.Toggle();
        bool opened = ui.IsOpen;
        Shot("inventory_open");
        ui.Toggle();
        Check("Inventory toggle (Tab path)", opened && !ui.IsOpen, $"opened={opened}, closedAgain={!ui.IsOpen}");
    }

    private static void StepBuildMenu()
    {
        var bmc = BuildModeController.Instance;
        var menu = UnityEngine.Object.FindObjectOfType<BuildMenuUI>();
        bmc.EnterBuildMode();
        menu.Open();
        bool menuOpen = menu.IsOpen;
        bmc.SetSelected(GameDatabase.GetStructure("shelter"));
        bool ghost = GameObject.Find("BuildGhost") != null;
        Shot("build_menu_ghost");
        menu.Close();
        Check("Build mode + menu + ghost (B path)", menuOpen && bmc.IsActive && ghost,
            $"menu={menuOpen}, active={bmc.IsActive}, ghost={ghost}");
    }

    private static void StepPlaceSite()
    {
        var bmc = BuildModeController.Instance;
        var def = GameDatabase.GetStructure("shelter");
        _sitePos = FindFreeCell();
        var site = bmc.PlaceConstructionSite(_sitePos, def);
        bool placed = StructureRegistry.Instance.GetStructureAt(_sitePos) is ConstructionSite;
        bmc.ExitBuildMode();
        Shot("construction_site");
        Check("Construction site placed (translucent, 0 materials)", placed && !site.Progress.IsComplete,
            $"at {_sitePos}, prompt='{site.InteractionPrompt}'");
    }

    private static void StepDeliver()
    {
        var s = StructureRegistry.Instance.GetStructureAt(_sitePos);
        if (s is not ConstructionSite site) { Fail("Deliver materials", "no site at pos"); return; }
        site.DeliverFrom(_player.Inventory);   // player has 30 wood / 20 scrap
        // completion destroys the site and spawns the real structure
        EditorApplication.delayCall += () =>
        {
            var after = StructureRegistry.Instance.GetStructureAt(_sitePos);
            bool done = after != null && after is not ConstructionSite;
            Check("Deliver → construction completes", done,
                $"after={(after == null ? "null" : after.GetType().Name)}");
            Shot("construction_complete");
        };
    }

    private static void StepTillSow()
    {
        var cell = FindCropCell();
        _plot = FarmingActions.TryTill(cell, _island, UnityEngine.Object.FindObjectOfType<IslandRenderer>());
        if (_plot == null) { Fail("Till + sow", $"TryTill returned null on {cell?.Terrain}"); return; }
        FarmingActions.TrySow(_plot, _player);
        Check("Till + sow", _plot.Crop != null, $"crop={_plot.Crop?.CropId}");
    }

    private static void StepGrow()
    {
        if (_plot?.Crop == null) { Fail("Grow crop", "no crop sown"); return; }
        // water + fast-forward game time in chunks (storm wheat = 10 game-min)
        for (int i = 0; i < 8; i++)
        {
            FarmingActions.Water(_plot);
            EventBus.Publish(new GameTickEvent { DeltaMinutes = 2f, TotalGameMinutes = 100f + i * 2f });
        }
        Shot("crop_grown");
        Check("Crop grows via game ticks", _plot.Crop.IsHarvestable,
            $"progress={_plot.Crop.GrowthProgress:F2}, health={_plot.Crop.Health:F0}");
    }

    private static void StepHarvest()
    {
        if (_plot?.Crop == null) { Fail("Harvest", "no crop"); return; }
        FarmingActions.Harvest(_plot, _player);
        Check("Harvest into inventory", _cropHarvested, $"CropHarvestedEvent fired={_cropHarvested}");
    }

    private static void StepWorkshop()
    {
        var bmc = BuildModeController.Instance;
        var pos = FindFreeCell();
        bmc.PlaceStructure(pos, GameDatabase.GetStructure("stone_mill"));
        _mill = StructureRegistry.Instance.GetStructureAt(pos) as SkyHarvest.Workshop.WorkshopBase;
        if (_mill == null) { Fail("Workshop", "mill not placed"); return; }

        RecipeDef recipe = null;
        foreach (var r in GameDatabase.GetRecipesFor(WorkshopType.StoneMill)) { recipe = r; break; }
        bool started = _mill.StartRecipe(recipe, _player.Inventory);
        // recipe is 15 processing-seconds; SecondsPerGameMinute=1 so 20 game-min = 20s
        EventBus.Publish(new GameTickEvent { DeltaMinutes = 20f, TotalGameMinutes = 130f });
        int flourBefore = _player.Inventory.GetCount("flour");
        bool collected = _mill.CollectOutput(_player.Inventory);
        int flourAfter = _player.Inventory.GetCount("flour");
        Check("Workshop start→process→collect", started && collected && flourAfter > flourBefore,
            $"started={started}, collected={collected}, flour {flourBefore}→{flourAfter}");
    }

    private static void StepStorage()
    {
        var bmc = BuildModeController.Instance;
        var pos = FindFreeCell();
        bmc.PlaceStructure(pos, GameDatabase.GetStructure("crate"));
        var crate = StructureRegistry.Instance.GetStructureAt(pos) as SkyHarvest.Storage.StorageContainer;
        if (crate == null) { Fail("Storage", "crate not placed"); return; }

        bool moved = _player.Inventory.TryRemove("stone", 2) && crate.Storage.TryAdd("stone", 2);
        var ui = UnityEngine.Object.FindObjectOfType<StorageUI>();
        ui.Open(crate);
        bool opened = ui.IsOpen;
        Shot("storage_open");
        ui.Close();
        Check("Storage place + transfer + UI", moved && opened,
            $"transfer={moved}, uiOpened={opened}, crateStone={crate.Storage.GetCount("stone")}");
    }

    private static void StepSkynet()
    {
        var cliff = FindCell(c => c.Terrain == TerrainType.CliffEdge && !StructureRegistry.Instance.HasStructureAt(c.GridPos));
        if (cliff == null) { Fail("Skynet", "no free cliff-edge cell on this island"); return; }
        BuildModeController.Instance.PlaceStructure(cliff.GridPos, GameDatabase.GetStructure("skynet"));
        var net = StructureRegistry.Instance.GetStructureAt(cliff.GridPos) as SkyHarvest.Skynet.Skynet;
        if (net == null) { Fail("Skynet", "component missing"); return; }

        net.InitializeOfflineAccrual(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 700); // ~4 rolls
        bool accrued = net.GetBufferContents().Count > 0;
        net.Interact(_player);
        bool emptied = net.GetBufferContents().Count == 0;
        Check("Skynet offline accrual + collect", accrued && emptied, $"accrued={accrued}, collected={emptied}");
    }

    private static void StepExpansion()
    {
        // The payoff: scaffolding an outer edge cell grows the island outward.
        // Pick a structure-free edge cell that has at least one EMPTY orthogonal
        // neighbour, so Expand actually creates new Scaffold cells.
        var edge = FindCell(c =>
            c.IsEdge &&
            !StructureRegistry.Instance.HasStructureAt(c.GridPos) &&
            HasEmptyNeighbour(c.GridPos));
        if (edge == null) { Fail("Expansion", "no free edge cell with an empty neighbour"); return; }

        int before = _island.Cells.Count;
        int firesBefore = _islandExpandedCount;

        BuildModeController.Instance.PlaceStructure(edge.GridPos, GameDatabase.GetStructure("scaffolding"));

        int grown = _island.Cells.Count - before;
        bool anyScaffold = false;
        foreach (var off in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0),
                                    new Vector2Int(0, 1), new Vector2Int(0, -1) })
        {
            var n = _island.GetCell(edge.GridPos + off);
            if (n != null && n.Terrain == TerrainType.Scaffold) { anyScaffold = true; break; }
        }
        int fires = _islandExpandedCount - firesBefore;

        Check("Island expansion (scaffold → new cells)",
            grown > 0 && anyScaffold && fires == 1,
            $"newCells={grown}, scaffoldTerrain={anyScaffold}, IslandExpandedEvent fired={fires} (expect 1)");
    }

    private static bool HasEmptyNeighbour(Vector2Int pos)
    {
        foreach (var off in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0),
                                    new Vector2Int(0, 1), new Vector2Int(0, -1) })
            if (!_island.IsValidPosition(pos + off)) return true;
        return false;
    }

    private static void StepDebrisSpawn()
    {
        var cell = FindFreeCell();
        var world = GridMath.GridToWorld(cell);
        var go = new GameObject("VerifyDebris");
        go.AddComponent<SpriteRenderer>();
        var d = go.AddComponent<SkyHarvest.Debris.DebrisObject>();
        d.InitiateFall(new Vector3(world.x, world.y, 0f), cell, new System.Random(7));
        Pass("Debris spawned (falling)", $"landing at {cell}");
    }

    private static void StepDebrisScavenge()
    {
        var d = UnityEngine.Object.FindObjectOfType<SkyHarvest.Debris.DebrisObject>();
        if (d == null) { Fail("Debris scavenge", "debris object gone before scavenge"); return; }
        d.Interact(_player);
        Check("Debris scavenge (E path)", _debrisScavenged, $"DebrisScavengedEvent fired={_debrisScavenged}");
    }

    private static void StepWeather()
    {
        var wm = SkyHarvest.Weather.WeatherManager.Instance;
        wm.StateMachine.SetState(WeatherType.HeavyStorm, 5f);
        bool stormy = wm.CurrentWeather == WeatherType.HeavyStorm;
        EditorApplication.delayCall += () => Shot("heavy_storm");
        Check("Weather force HeavyStorm", stormy, $"current={wm.CurrentWeather}");
    }

    private static void StepSave()
    {
        // leave an in-progress site so the save must persist it
        var pos = FindFreeCell();
        BuildModeController.Instance.PlaceConstructionSite(pos, GameDatabase.GetStructure("rain_catcher"));

        SkyHarvest.SaveLoad.SaveManager.Instance.Save();
        string path = Path.Combine(Application.persistentDataPath, "saves", "save.json");
        string json = File.Exists(path) ? File.ReadAllText(path) : "";
        bool hasSite = json.Contains("\"Constructing\": true");
        Check("Save written + persists construction site", json.Length > 0 && hasSite,
            $"file={json.Length}B, constructingEntry={hasSite}");
    }

    private static void StepPause()
    {
        var pm = UnityEngine.Object.FindObjectOfType<PauseMenuUI>();
        pm.Toggle();
        bool opened = pm.IsOpen;
        Shot("pause_menu");
        pm.Toggle();
        Check("Pause toggle (Esc path)", opened && !pm.IsOpen, $"opened={opened}");
    }

    private static void StepInspector()
    {
        var insp = UnityEngine.Object.FindObjectOfType<InspectorPanel>();
        Check("Inspector panel wired (Q path)", insp != null, insp == null ? "missing" : "component present");
        Shot("final_state");
    }

    private static void Finish()
    {
        _report.AppendLine();
        _report.AppendLine("Not automatable with legacy Input (verified by code-trace in SCOPE_LEDGER.md keybind table,");
        _report.AppendLine("recommend a quick human playtest): WASD/arrow movement feel, raw keypress→handler dispatch,");
        _report.AppendLine("mouse-follow ghost placement, build-menu arrow navigation, scroll-wheel zoom feel.");
        File.WriteAllText(Path.Combine(_outDir, "verify_report.md"), _report.ToString());
        Debug.Log("[PlayModeVerify] report written");

        _running = false;
        EditorApplication.update -= OnUpdate;
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.delayCall += () =>
        {
            Debug.Log("[PlayModeVerify] done, exiting");
            EditorApplication.Exit(0);
        };
    }

    // ---------------- helpers ----------------

    private static void Pass(string name, string detail) => Row(name, "✅ PASS", detail);
    private static void Fail(string name, string detail) => Row(name, "❌ FAIL", detail);
    private static void Check(string name, bool ok, string detail) => Row(name, ok ? "✅ PASS" : "❌ FAIL", detail);

    private static void Row(string name, string result, string detail)
    {
        _report.AppendLine($"| {name} | {result} | {detail.Replace("|", "/")} |");
        Debug.Log($"[PlayModeVerify] {result} — {name} — {detail}");
    }

    private static void Shot(string name) =>
        ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, name + ".png"));

    private static Vector2Int FindFreeCell()
    {
        var cell = FindCell(c =>
            _island.IsWalkable(c.GridPos) &&
            !c.IsTilled &&
            !StructureRegistry.Instance.HasStructureAt(c.GridPos) &&
            c.GridPos != GridMath.WorldToGrid(_player.transform.position));
        return cell?.GridPos ?? Vector2Int.zero;
    }

    private static IslandCell FindCropCell() =>
        FindCell(c => TerrainProperties.CanPlaceCrops(c.Terrain) && !c.IsTilled &&
                      !StructureRegistry.Instance.HasStructureAt(c.GridPos));

    private static IslandCell FindCell(Func<IslandCell, bool> pred)
    {
        foreach (var kvp in _island.Cells)
            if (pred(kvp.Value)) return kvp.Value;
        return null;
    }

    private static Button FindButton(params string[] nameHints)
    {
        foreach (var b in UnityEngine.Object.FindObjectsOfType<Button>(true))
        {
            string label = b.GetComponentInChildren<Text>(true)?.text ?? "";
            foreach (var h in nameHints)
                if (b.name.Contains(h) || label.Contains(h)) return b;
        }
        return null;
    }
}
