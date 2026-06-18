// Dev debug panel (F3): terrain texture categories, grid bounds, walk probes.
// Enabled with --dev.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;
using SkyHarvest.Player;

namespace SkyHarvest.DevTools
{
    public class DevDebugPanel : MonoBehaviour
    {
        private IslandData? _island;
        private PlayerController? _player;
        private DevDebugOverlay? _overlay;
        private Vector2 _scroll;
        private bool _initialized;

        public static bool IsEnabled { get; private set; }

        public static void EnableIfRequested()
        {
            IsEnabled = HasFlag("--dev");
        }

        private static bool HasFlag(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == flag) return true;
            return false;
        }

        public void Initialize(IslandData island, PlayerController player, Camera cam)
        {
            _island  = island;
            _player  = player;
            if (_overlay == null)
            {
                _overlay = cam.gameObject.AddComponent<DevDebugOverlay>();
                _initialized = true;
            }
            _overlay.SetRefs(island, player);
        }

        private void Update()
        {
            if (!IsEnabled) return;
            if (Input.GetKeyDown(KeyCode.F3))
                DevDebugSettings.PanelOpen = !DevDebugSettings.PanelOpen;
        }

        private void OnGUI()
        {
            if (!IsEnabled) return;

            if (!DevDebugSettings.PanelOpen)
            {
                GUI.Label(new Rect(Screen.width - 148, 8, 140, 22), "F3 dev debug");
                return;
            }

            const int w = 300;
            const int h = 520;
            var rect = new Rect(Screen.width - w - 12, 8, w, h);
            GUI.Box(rect, "Dev debug (F3)");

            _scroll = GUI.BeginScrollView(
                new Rect(rect.x + 8, rect.y + 24, w - 16, h - 32),
                _scroll,
                new Rect(0, 0, w - 32, 680));

            int y = 0;
            y = ToggleRow(y, "Diamond walk bounds", ref DevDebugSettings.ShowDiamondBounds);
            y = ToggleRow(y, "Terrain category fill", ref DevDebugSettings.ShowTerrainFill);
            y = ToggleRow(y, "Cell coord labels", ref DevDebugSettings.ShowCellLabels);
            y = ToggleRow(y, "Player walk probe", ref DevDebugSettings.ShowPlayerProbe);
            y = ToggleRow(y, "Cursor walk probe", ref DevDebugSettings.ShowHoverProbe);
            y = ToggleRow(y, "Stair walk corridor", ref DevDebugSettings.ShowStairCorridor);
            y = ToggleRow(y, "Show both tiers", ref DevDebugSettings.ShowBothTiers);

            y += 8;
            GUI.Label(new Rect(0, y, 260, 20), "— Terrain texture categories —");
            y += 22;
            y = LegendRow(y, TerrainType.FertileValley);
            y = LegendRow(y, TerrainType.RockyPlateau);
            y = LegendRow(y, TerrainType.CliffEdge);
            y = LegendRow(y, TerrainType.NaturalSpring);
            y = LegendRow(y, TerrainType.WindCorridor);
            y = LegendRow(y, TerrainType.Scaffold);

            y += 8;
            GUI.Label(new Rect(0, y, 260, 36),
                "Cyan/orange = tier diamonds.\nMagenta box = stair corridor.\nRed X = outside walk area.");
            y += 40;

            if (_player != null && _island != null)
            {
                GUI.Label(new Rect(0, y, 260, 18), "— Player —");
                y += 20;
                var pos = (Vector2)_player.transform.position;
                int tier = _player.CurrentTier;
                var cell = GridMath.WorldToGrid(pos, tier);
                bool inside = DevDebugOverlay.ProbeInsideIsland(pos, tier, _island, out _);
                y = InfoRow(y, $"world ({pos.x:F3}, {pos.y:F3})");
                y = InfoRow(y, $"cell ({cell.x}, {cell.y})  tier {tier}");
                y = InfoRow(y, inside ? "inside walk area" : "OUTSIDE walk area ⚠");
                if (_island.IsValidPosition(cell))
                {
                    var c = _island.GetCell(cell)!;
                    y = InfoRow(y, $"terrain {c.Terrain}");
                    y = InfoRow(y, ShortPath(c.Terrain));
                }
                y += 6;
            }

            if (_island != null && DevDebugSettings.ShowHoverProbe && Camera.main != null)
            {
                GUI.Label(new Rect(0, y, 260, 18), "— Cursor —");
                y += 20;
                int tier = _player != null ? _player.CurrentTier : 0;
                var screen = Input.mousePosition;
                screen.z = Mathf.Abs(Camera.main.transform.position.z);
                Vector2 world = Camera.main.ScreenToWorldPoint(screen);
                var cell = GridMath.WorldToGrid(world, tier);
                bool inside = DevDebugOverlay.ProbeInsideIsland(world, tier, _island, out _);
                y = InfoRow(y, $"world ({world.x:F3}, {world.y:F3})");
                y = InfoRow(y, $"cell ({cell.x}, {cell.y})");
                y = InfoRow(y, inside ? "inside walk area" : "outside walk area");
                if (_island.IsValidPosition(cell))
                {
                    var c = _island.GetCell(cell)!;
                    y = InfoRow(y, $"terrain {c.Terrain}");
                    y = InfoRow(y, ShortPath(c.Terrain));
                    y = InfoRow(y, $"elevation {c.Elevation:F1}");
                }
            }

            GUI.EndScrollView();

            if (DevDebugSettings.ShowCellLabels && _island != null)
                DrawCellLabels();
        }

        private void DrawCellLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;

            int tier = DevDebugSettings.ShowBothTiers
                ? -1
                : (_player != null ? _player.CurrentTier : 0);

            foreach (var kvp in _island!.Cells)
            {
                int cellTier = _island.Tier(kvp.Key);
                if (tier >= 0 && cellTier != tier) continue;

                Vector2 world = GridMath.DiamondCentre(kvp.Key, cellTier);
                var screen = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
                screen.y = Screen.height - screen.y;

                string t = kvp.Value.Terrain.ToString();
                string ab = t.Length >= 3 ? t[..3] : t;
                string label = $"{kvp.Key.x},{kvp.Key.y}\n{ab}";
                var size = GUI.skin.label.CalcSize(new GUIContent(label));
                var r = new Rect(screen.x - size.x * 0.5f, screen.y - size.y * 0.5f, size.x, size.y);
                GUI.color = DevDebugOverlay.TerrainColor(kvp.Value.Terrain, 1f);
                GUI.Label(r, label);
                GUI.color = Color.white;
            }
        }

        private static int ToggleRow(int y, string label, ref bool value)
        {
            value = GUI.Toggle(new Rect(0, y, 260, 22), value, label);
            return y + 24;
        }

        private static int InfoRow(int y, string text)
        {
            GUI.Label(new Rect(0, y, 260, 18), text);
            return y + 18;
        }

        private static int LegendRow(int y, TerrainType type)
        {
            var c = DevDebugOverlay.TerrainColor(type, 1f);
            GUI.color = c;
            GUI.Box(new Rect(0, y + 2, 14, 14), "");
            GUI.color = Color.white;
            string path = TerrainProperties.TilePath(type);
            int slash = path.LastIndexOf('/');
            string file = slash >= 0 ? path[(slash + 1)..] : path;
            GUI.Label(new Rect(20, y, 240, 18.5f), $"{type}\n{file}");
            return y + 36;
        }

        private static string ShortPath(TerrainType type)
        {
            string path = TerrainProperties.TilePath(type);
            int slash = path.LastIndexOf('/');
            return slash >= 0 ? path[(slash + 1)..] : path;
        }
    }
}
