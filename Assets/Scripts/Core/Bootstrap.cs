using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyHarvest.Player;
using SkyHarvest.Island;
using SkyHarvest.Weather;
using SkyHarvest.Farming;
using SkyHarvest.Building;
using SkyHarvest.Debris;
using SkyHarvest.Storage;
using SkyHarvest.UI;
using SkyHarvest.Audio;
using SkyHarvest.SaveLoad;

namespace SkyHarvest.Core
{
    public class Bootstrap : MonoBehaviour
    {
        private GameManager? _gm;
        private IslandRenderer? _islandRenderer;
        private PlayerController? _player;
        private HUDController? _hud;
        private InspectorPanel? _inspector;
        private ContextualTooltipUI? _tooltips;
        private InventoryUI? _inventoryUI;
        private WorkshopUI? _workshopUI;
        private StorageUI? _storageUI;
        private BuildMenuUI? _buildMenu;
        private PauseMenuUI? _pauseMenu;
        private MainMenuUI? _mainMenu;

        private bool _gameStarted;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.SetResolution(1280, 720, false);

            EnsureEventSystem();
            BuildManagers();
            BuildCamera();
            BuildIslandLayer();
            BuildUI();
            BuildMenuUI_();
        }

        private void Start()
        {
            bool hasSave = SaveManager.Instance?.HasSave() == true;
            _mainMenu?.Open();
            if (hasSave) { } // continue button will be active
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal build methods
        // ─────────────────────────────────────────────────────────────────────

        private void BuildManagers()
        {
            _gm = new GameObject("GameManager").AddComponent<GameManager>();

            new GameObject("SaveManager").AddComponent<SaveManager>();
            new GameObject("WeatherManager").AddComponent<WeatherManager>();
            new GameObject("CropGrowthSystem").AddComponent<CropGrowthSystem>();
            new GameObject("StructureRegistry").AddComponent<StructureRegistry>();
            new GameObject("AudioCueSystem").AddComponent<AudioCueSystem>();
        }

        private void BuildCamera()
        {
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 2.5f;   // close enough to read the avatar; scroll-wheel zoom [2,6] in CameraFollow
            cam.backgroundColor = new Color(0.11f, 0.1f, 0.11f, 1f);
            // CONVENTIONS: orthographic 2D sprites; dimetric layout is in GridMath + art, not camera tilt.
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CameraFollow>();
        }

        private void BuildIslandLayer()
        {
            var islandGO = new GameObject("Island");
            _islandRenderer = islandGO.AddComponent<IslandRenderer>();

            var debrisGO = new GameObject("DebrisSpawner");
            var spawner  = debrisGO.AddComponent<DebrisSpawner>();

            BuildModeController.CreateInstance(_islandRenderer);
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("HUD");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // ---- Hotbar (bottom centre, 6 slots) ----
            var hotbarGO   = new GameObject("Hotbar", typeof(RectTransform));
            hotbarGO.transform.SetParent(canvasGO.transform, false);
            var hotbarSlots = new GameObject[6];
            var hotbarIcons = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                var slotGO = MakeSlot($"Slot{i}", hotbarGO.transform,
                    new Vector2(-150f + i * 56f, -310f));
                hotbarSlots[i] = slotGO;
                hotbarIcons[i] = slotGO.GetComponentInChildren<Image>();
            }

            // ---- Equipped tool icon (top-left) ----
            var toolSlotGO = MakeSlot("EquippedTool", canvasGO.transform, new Vector2(-540f, 300f));
            var toolIcon   = toolSlotGO.transform.Find("Icon")!.GetComponent<Image>();

            // ---- Time + weather texts ----
            var timeText    = MakeText("TimeText",    canvasGO.transform, new Vector2(-500f, 320f), "00:00", 16);
            var weatherText = MakeText("WeatherText", canvasGO.transform, new Vector2(400f,  320f), "",     14);
            var promptText  = MakeText("PromptText",  canvasGO.transform, new Vector2(0f,   -230f), "",     15);

            _hud = canvasGO.AddComponent<HUDController>();
            _hud.SetTimeText(timeText);
            _hud.SetWeatherText(weatherText);
            _hud.SetPromptText(promptText);
            _hud.SetHotbarSlots(hotbarSlots, hotbarIcons);
            _hud.SetToolIcon(toolIcon);

            // ---- Contextual tooltip banner (top centre) ----
            var tipBanner = MakePanel("TooltipBanner", canvasGO.transform, new Vector2(0f, 260f), new Vector2(520f, 56f));
            tipBanner.SetActive(false);
            var tipText = MakeText("TooltipText", tipBanner.transform, Vector2.zero, "", 13);
            _tooltips = canvasGO.AddComponent<ContextualTooltipUI>();
            _tooltips.Initialize(tipBanner, tipText);

            // ---- Inspector panel (on-demand, Q key) ----
            var inspPanel = MakePanel("InspectorPanel", canvasGO.transform, new Vector2(380f, 120f), new Vector2(280f, 220f));
            inspPanel.SetActive(false);
            var inspTitle   = MakeText("InspTitle",   inspPanel.transform, new Vector2(0f,  85f), "", 16);
            var inspBody    = MakeText("InspBody",    inspPanel.transform, new Vector2(0f,  20f), "", 12);
            inspBody.alignment = TextAnchor.UpperLeft;
            var inspBarALbl = MakeText("InspBarA",    inspPanel.transform, new Vector2(0f, -40f), "", 11);
            var inspBarA    = MakeSlider("InspBarAFill", inspPanel.transform, new Vector2(0f, -58f));
            var inspBarBLbl = MakeText("InspBarB",    inspPanel.transform, new Vector2(0f, -78f), "", 11);
            var inspBarB    = MakeSlider("InspBarBFill", inspPanel.transform, new Vector2(0f, -96f));
            _inspector = canvasGO.AddComponent<InspectorPanel>();
            _inspector.SetWidgets(inspTitle, inspBody, inspBarA, inspBarB, inspBarALbl, inspBarBLbl);

            // ---- Minimap toggle (bottom-right) ----
            var minimapPanel = MakePanel("MinimapPanel", canvasGO.transform, new Vector2(480f, -200f), new Vector2(160f, 120f));
            minimapPanel.SetActive(false);
            MakeText("MinimapLabel", minimapPanel.transform, Vector2.zero, "Island map\n(MVP)", 12);
            var minimapBtn = MakeButton("Map", canvasGO.transform, new Vector2(520f, -310f));
            minimapBtn.onClick.AddListener(() => minimapPanel.SetActive(!minimapPanel.activeSelf));

            // ---- Inventory panel ----
            var invPanel = MakePanel("InventoryPanel", canvasGO.transform, new Vector2(0f, 0f),
                new Vector2(600f, 400f));
            invPanel.SetActive(false);
            _inventoryUI = canvasGO.AddComponent<InventoryUI>();

            var invLabels = new Text[20];
            var invIcons  = new Image[20];
            for (int i = 0; i < 20; i++)
            {
                float x = -260f + (i % 5) * 120f;
                float y =  150f - (i / 5) * 80f;
                var sg = MakeSlot($"InvSlot{i}", invPanel.transform, new Vector2(x, y));
                invLabels[i] = sg.GetComponentInChildren<Text>();
                invIcons[i]  = sg.GetComponentInChildren<Image>();
            }
            _inventoryUI.SetSlotDisplays(invLabels, invIcons);

            // ---- Workshop panel ----
            var wsPanel = MakePanel("WorkshopPanel", canvasGO.transform, new Vector2(0f, 0f), new Vector2(400f, 300f));
            wsPanel.SetActive(false);
            _workshopUI = canvasGO.AddComponent<WorkshopUI>();
            var wsTitle    = MakeText("WsTitle",    wsPanel.transform, new Vector2(0f,  110f), "Workshop", 18);
            var wsProgress = MakeText("WsProgress", wsPanel.transform, new Vector2(0f,   60f), "Idle", 14);
            var wsRecipes  = MakeText("WsRecipes",  wsPanel.transform, new Vector2(-100f, 10f), "", 13);
            var wsSlider   = MakeSlider("WsBar", wsPanel.transform, new Vector2(0f, 70f));
            var wsStart    = MakeButton("Start",   wsPanel.transform, new Vector2(-70f, -110f));
            var wsCollect  = MakeButton("Collect", wsPanel.transform, new Vector2(70f,  -110f));
            _workshopUI.SetWidgets(wsTitle, wsProgress, wsSlider, wsStart, wsCollect, wsRecipes);

            // ---- Storage panel ----
            var stPanel = MakePanel("StoragePanel", canvasGO.transform, new Vector2(0f, 0f), new Vector2(620f, 350f));
            stPanel.SetActive(false);
            _storageUI = canvasGO.AddComponent<StorageUI>();
            var playerLabels = new Text[10]; var storageLabels = new Text[10];
            for (int i = 0; i < 10; i++)
            {
                playerLabels[i]  = MakeText($"PL{i}", stPanel.transform, new Vector2(-180f, 120f - i * 25f), "", 12);
                storageLabels[i] = MakeText($"SL{i}", stPanel.transform, new Vector2( 130f, 120f - i * 25f), "", 12);
            }
            _storageUI.SetDisplays(playerLabels, storageLabels);

            // ---- Build menu panel ----
            var bmPanel = MakePanel("BuildMenuPanel", canvasGO.transform, new Vector2(-450f, 0f), new Vector2(280f, 400f));
            bmPanel.SetActive(false);
            _buildMenu = canvasGO.AddComponent<BuildMenuUI>();
            var bmEntries = new Text[12];
            for (int i = 0; i < 12; i++)
                bmEntries[i] = MakeText($"BM{i}", bmPanel.transform, new Vector2(0f, 150f - i * 28f), "", 13);
            var bmCost = MakeText("BMCost", bmPanel.transform, new Vector2(0f, -170f), "", 11);
            _buildMenu.SetDisplays(bmEntries, bmCost);

            // ---- Pause panel ----
            var pausePanel = MakePanel("PausePanel", canvasGO.transform, Vector2.zero, new Vector2(320f, 260f));
            pausePanel.SetActive(false);
            _pauseMenu = canvasGO.AddComponent<PauseMenuUI>();
            _pauseMenu.Initialize(pausePanel);
            MakeButton("Resume",    pausePanel.transform, new Vector2(0f,  80f)).onClick.AddListener(_pauseMenu.OnResumeClicked);
            MakeButton("Save",      pausePanel.transform, new Vector2(0f,  20f)).onClick.AddListener(_pauseMenu.OnSaveClicked);
            MakeButton("Save+Quit", pausePanel.transform, new Vector2(0f, -40f)).onClick.AddListener(_pauseMenu.OnSaveAndQuitClicked);
        }

        private void BuildMenuUI_()
        {
            var canvasGO = new GameObject("MainMenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var bg = MakePanel("MenuBg", canvasGO.transform, Vector2.zero, new Vector2(1280f, 720f));
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.07f, 0.06f, 0.07f, 0.95f);

            var logo = SpriteLoader.Load("Sprites/ui/logo");
            var logoGO = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGO.transform.SetParent(bg.transform, false);
            var logoImg = logoGO.GetComponent<Image>();
            logoImg.sprite = logo;
            var logoRT = logoGO.GetComponent<RectTransform>();
            logoRT.sizeDelta = new Vector2(256f, 96f);
            logoRT.anchoredPosition = new Vector2(0f, 200f);

            var seedInput = MakeInputField("SeedInput", bg.transform, new Vector2(0f, 50f));
            var newBtn    = MakeButton("New Game", bg.transform, new Vector2(0f, -20f));
            var contBtn   = MakeButton("Continue", bg.transform, new Vector2(0f, -80f));
            MakeText("SeedLabel", bg.transform, new Vector2(0f, 90f), "Seed (optional):", 13);

            _mainMenu = canvasGO.AddComponent<MainMenuUI>();
            _mainMenu.Initialize(bg, seedInput, contBtn,
                onNew:      StartNewGame,
                onContinue: StartFromSave);
            newBtn.onClick.AddListener(_mainMenu.OnNewGameClicked);
            contBtn.onClick.AddListener(_mainMenu.OnContinueClicked);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Game lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void StartNewGame()
        {
            int seed = PlayerPrefs.HasKey("IslandSeed") ? PlayerPrefs.GetInt("IslandSeed") : Random.Range(0, 999999);
            var island = IslandGenerator.Generate(seed, Constants.DefaultIslandRadius);
            _gm?.SetIsland(island);
            _islandRenderer?.Render(island);

            SpawnPlayer(Vector3.zero);
            WireUIToPlayer();
            WireBuildMode(island);

            var debrisSpawner = Object.FindObjectOfType<DebrisSpawner>();
            debrisSpawner?.SetIsland(island);

            WeatherManager.Instance?.StartWeather();

            EventBus.Publish(new GameStartedEvent { LoadedFromSave = false });
            _gameStarted = true;
        }

        private void StartFromSave()
        {
            var data = SaveManager.Instance?.Load();
            if (data == null) { StartNewGame(); return; }

            var island = IslandGenerator.Generate(data.Island.Seed, data.Island.Radius);
            _gm?.SetIsland(island);
            _islandRenderer?.Render(island);

            SaveManager.Instance?.ApplySaveData(data, island);

            SpawnPlayer(new Vector3(data.Player.PosX, data.Player.PosY, data.Player.PosZ));
            WireUIToPlayer();
            WireBuildMode(island);

            var pic = _player?.GetComponent<PlayerInventoryComponent>();
            if (pic != null)
                foreach (var slot in data.Player.InventorySlots)
                    pic.Inventory.TryAdd(slot.ItemId, slot.Count);

            var ts = _player?.GetComponent<ToolSystem>();
            if (ts != null && !string.IsNullOrEmpty(data.Player.EquippedTool))
                ts.EquipById(data.Player.EquippedTool);

            // Rebuild structures from save (finished + in-progress construction sites)
            var bmc = BuildModeController.Instance;
            if (bmc != null)
            {
                foreach (var ss in data.Island.Structures)
                {
                    var def = Data.GameDatabase.GetStructure(ss.StructureId);
                    if (def == null) continue;
                    var pos = new Vector2Int(ss.GridX, ss.GridY);
                    if (ss.Constructing)
                    {
                        var site = bmc.PlaceConstructionSite(pos, def);
                        var delivered = new System.Collections.Generic.List<(string, int)>();
                        foreach (var slot in ss.Delivered)
                            if (!string.IsNullOrEmpty(slot.ItemId) && slot.Count > 0)
                                delivered.Add((slot.ItemId, slot.Count));
                        site.RestoreDelivered(delivered);
                    }
                    else
                    {
                        bmc.PlaceStructure(pos, def);
                    }
                }
            }

            RestoreIslandContents(data);

            // Rebuild crops from save
            foreach (var cs in data.Island.Crops)
            {
                var cropDef = Data.GameDatabase.GetCrop(cs.CropId);
                if (cropDef == null) continue;
                var pos  = new Vector2Int(cs.GridX, cs.GridY);
                var cell = island.GetCell(pos);
                if (cell == null) continue;
                var cropState = new Farming.CropState(
                    cropDef.CropId, cropDef.GrowthTimeMinutes, cropDef.GrowthStages,
                    cropDef.WaterConsumptionPerMinute, cs.GrowthProgress, cs.Health);
                var plotGO = new GameObject($"CropPlot_{pos.x}_{pos.y}");
                plotGO.transform.position = GridMath.GridToWorld(pos);
                var plot = plotGO.AddComponent<Farming.CropPlot>();
                plot.Initialize(cell.Soil, cropState, pos);
                CropGrowthSystem.Instance?.Register(plot);
            }

            var debrisSpawner = Object.FindObjectOfType<DebrisSpawner>();
            debrisSpawner?.SetIsland(island);
            WeatherManager.Instance?.StartWeather();
            EventBus.Publish(new GameStartedEvent { LoadedFromSave = true });
            _gameStarted = true;
        }

        private void RestoreIslandContents(WorldSaveData data)
        {
            var registry = StructureRegistry.Instance;
            if (registry == null) return;

            foreach (var sd in data.Island.Storages)
            {
                var structure = registry.GetStructureAt(new Vector2Int(sd.GridX, sd.GridY));
                if (structure is Storage.StorageContainer container)
                {
                    var slots = new System.Collections.Generic.List<(string, int)>();
                    foreach (var slot in sd.Slots)
                        if (!string.IsNullOrEmpty(slot.ItemId) && slot.Count > 0)
                            slots.Add((slot.ItemId, slot.Count));
                    container.RestoreFromSave(slots);
                }
            }

            foreach (var snd in data.Island.Skynets)
            {
                var structure = registry.GetStructureAt(new Vector2Int(snd.GridX, snd.GridY));
                if (structure is Skynet.Skynet skynet)
                {
                    var buffer = new System.Collections.Generic.List<(string, int)>();
                    foreach (var slot in snd.Buffer)
                        if (!string.IsNullOrEmpty(slot.ItemId) && slot.Count > 0)
                            buffer.Add((slot.ItemId, slot.Count));
                    skynet.RestoreFromSave(snd.LastCollectedUnixTime, buffer);
                }
            }

            foreach (var wd in data.Island.Workshops)
            {
                var structure = registry.GetStructureAt(new Vector2Int(wd.GridX, wd.GridY));
                if (structure is Workshop.WorkshopBase workshop)
                {
                    workshop.RestoreFromSave(
                        wd.RecipeId, wd.OutputItemId, wd.OutputAmount,
                        wd.TotalSeconds, wd.ElapsedSeconds, wd.State);
                }
            }
        }

        private void SpawnPlayer(Vector3 pos)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Default");

            _player = go.AddComponent<PlayerController>();
            go.AddComponent<PlayerInventoryComponent>();
            go.AddComponent<ToolSystem>();
            go.AddComponent<InteractionSystem>();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            var spr = SpriteLoader.Load("Sprites/player/player_idle_s");
            sr.sprite = spr;

            var anim = go.AddComponent<SpriteAnimator>();
            anim.Frames = SpriteLoader.LoadStrip("Sprites/player/player_idle_s", 48);
            anim.Fps    = 4f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            var follow = Object.FindObjectOfType<CameraFollow>();
            if (follow != null) follow.Target = go.transform;
        }

        private void WireUIToPlayer()
        {
            if (_player == null) return;
            var pic    = _player.GetComponent<PlayerInventoryComponent>();
            var tools  = _player.GetComponent<ToolSystem>();
            var interact = _player.GetComponent<InteractionSystem>();

            if (_hud != null && pic != null && tools != null && interact != null)
                _hud.Initialize(pic, tools, interact);
            if (_inspector != null && interact != null)
            {
                var inspPanel = GameObject.Find("InspectorPanel");
                if (inspPanel != null) _inspector.Initialize(inspPanel, interact, pic);
            }
            if (_inventoryUI != null && pic != null)
            {
                var invPanel = GameObject.Find("InventoryPanel");
                if (invPanel != null) _inventoryUI.Initialize(invPanel, pic);
            }
            if (_workshopUI != null && pic != null)
            {
                var wsPanel = GameObject.Find("WorkshopPanel");
                if (wsPanel != null) _workshopUI.Initialize(wsPanel, pic);
            }
            if (_storageUI != null && pic != null)
            {
                var stPanel = GameObject.Find("StoragePanel");
                if (stPanel != null) _storageUI.Initialize(stPanel, pic);
            }
            if (_buildMenu != null)
            {
                var bmc    = BuildModeController.Instance;
                var bmPanel = GameObject.Find("BuildMenuPanel");
                if (bmPanel != null && bmc != null) _buildMenu.Initialize(bmPanel, bmc);
            }

            _player.SetUIRefs(_inventoryUI, _workshopUI, _storageUI, _buildMenu, _pauseMenu);
        }

        private void WireBuildMode(IslandData island)
        {
            var bmc = BuildModeController.Instance;
            if (bmc == null) return;
            bmc.SetIsland(island);
            if (_player != null) bmc.SetPlayer(_player);
        }

        // Centralized hotkeys. Panels no longer read Tab/Esc themselves — having
        // several Updates consume the same key in one frame caused close+pause
        // double-fires (script execution order is undefined).
        private void Update()
        {
            if (!_gameStarted) return;

            var bmc = BuildModeController.Instance;

            // B: toggle build mode (+ its structure menu)
            if (Input.GetKeyDown(KeyCode.B) && bmc != null)
            {
                if (bmc.IsActive) { bmc.ExitBuildMode(); _buildMenu?.Close(); }
                else              { bmc.EnterBuildMode(); _buildMenu?.Open(); }
            }

            // Tab: close storage if open, otherwise toggle the player's pack
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_storageUI != null && _storageUI.IsOpen) _storageUI.Close();
                else _inventoryUI?.Toggle();
            }

            // Esc: close the topmost open thing; pause only when nothing is open
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if      (_buildMenu != null && _buildMenu.IsOpen)     { _buildMenu.Close(); bmc?.ExitBuildMode(); }
                else if (bmc != null && bmc.IsActive)                 bmc.ExitBuildMode();
                else if (_storageUI != null && _storageUI.IsOpen)     _storageUI.Close();
                else if (_workshopUI != null && _workshopUI.IsOpen)   _workshopUI.Close();
                else if (_inventoryUI != null && _inventoryUI.IsOpen) _inventoryUI.Close();
                else _pauseMenu?.Toggle();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UI helpers
        // ─────────────────────────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        private static Font DefaultFont() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static Text MakeText(string name, Transform parent, Vector2 pos,
                                     string initial, int size)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(300f, 30f);
            var t   = go.GetComponent<Text>();
            t.font      = DefaultFont();
            t.fontSize  = size;
            t.color     = Color.white;
            t.text      = initial;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        private static GameObject MakeSlot(string name, Transform parent, Vector2 pos)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(48f, 48f);
            var bg  = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.18f, 0.18f, 0.9f);
            bg.sprite = SpriteLoader.Load("Sprites/ui/slot");

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchoredPosition = Vector2.zero;
            iconRT.sizeDelta        = new Vector2(32f, 32f);
            var icon = iconGO.GetComponent<Image>();
            icon.enabled = false;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0f, -20f);
            labelRT.sizeDelta        = new Vector2(48f, 16f);
            var label = labelGO.GetComponent<Text>();
            label.font      = DefaultFont();
            label.fontSize  = 9;
            label.color     = Color.white;
            label.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static GameObject MakePanel(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var img = go.GetComponent<Image>();
            img.color  = new Color(0.12f, 0.1f, 0.1f, 0.92f);
            img.sprite = SpriteLoader.Load("Sprites/ui/panel");
            return go;
        }

        private static Button MakeButton(string label, Transform parent, Vector2 pos)
        {
            var go  = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer),
                                     typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(120f, 30f);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteLoader.Load("Sprites/ui/button");
            img.color  = new Color(0.35f, 0.28f, 0.22f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchoredPosition = Vector2.zero;
            textRT.sizeDelta        = new Vector2(120f, 30f);
            var t = textGO.GetComponent<Text>();
            t.font      = DefaultFont();
            t.fontSize  = 13;
            t.color     = Color.white;
            t.text      = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static Slider MakeSlider(string name, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(180f, 14f);
            return go.GetComponent<Slider>();
        }

        private static InputField MakeInputField(string name, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                                    typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(200f, 30f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.18f, 0.18f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchoredPosition = Vector2.zero;
            textRT.sizeDelta        = new Vector2(200f, 30f);
            var t = textGO.GetComponent<Text>();
            t.font = DefaultFont(); t.fontSize = 14; t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = false;

            var field = go.GetComponent<InputField>();
            field.textComponent = t;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }
    }
}
