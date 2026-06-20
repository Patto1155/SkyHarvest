// In-game editor: drag / stretch the stair cutout and save layout to StreamingAssets.
// Toggle with F8 so controls never conflict with gameplay (WASD, scroll hotbar, etc.).
using UnityEngine;

namespace SkyHarvest.Island
{
    public class StairCutoutEditor : MonoBehaviour
    {
        private IslandRenderer? _renderer;
        private Transform? _cutout;
        private bool _dragging;
        private Vector3 _dragWorldOffset;
        private bool _layoutMode;

        private const float MoveStep  = 0.008f;
        private const float ScaleStep = 0.02f;

        public static bool IsEnabled { get; private set; }

        /// <summary>True while F8 layout mode is active — blocks movement / hotbar scroll.</summary>
        public static bool BlocksGameplayInput { get; private set; }

        public static void EnableIfRequested()
        {
            IsEnabled = HasFlag("--stair-edit") || HasFlag("--dev");
        }

        private static bool HasFlag(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == flag) return true;
            return false;
        }

        private void Start()
        {
            if (!IsEnabled) { enabled = false; return; }
            _renderer = FindObjectOfType<IslandRenderer>();
            StairCutoutLayout.Load();
        }

        private void Update()
        {
            BlocksGameplayInput = IsEnabled && _layoutMode;

            if (_renderer == null) return;

            if (Input.GetKeyDown(KeyCode.F8))
                _layoutMode = !_layoutMode;

            if (!_layoutMode) return;

            _cutout ??= _renderer.GetStairCutoutTransform();
            if (_cutout == null) return;

            HandleKeyboard();
            HandleMouse();
            SyncLayoutFromTransform();

            if (Input.GetKeyDown(KeyCode.F5))
            {
                StairCutoutLayout.Save();
                _renderer.RefreshTierWalls();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                StairCutoutLayout.Load();
                _cutout = null;
                _renderer.RefreshTierWalls();
            }

            if (Input.GetKeyDown(KeyCode.F6) || Input.GetKeyDown(KeyCode.F7))
            {
                float sign = Input.GetKeyDown(KeyCode.F7) ? 1f : -1f;
                StairCutoutLayout.Current.heightWorld =
                    Mathf.Max(0.2f, StairCutoutLayout.Current.heightWorld + sign * 0.04f);
                _cutout = null;
                _renderer.RefreshTierWalls();
            }
        }

        private void SyncLayoutFromTransform()
        {
            if (_cutout == null) return;
            StairCutoutLayout.ReadFromTransform(_cutout);
        }

        private void HandleKeyboard()
        {
            if (_cutout == null) return;
            var p = _cutout.localPosition;

            if (Input.GetKey(KeyCode.I)) p.y += MoveStep;
            if (Input.GetKey(KeyCode.K)) p.y -= MoveStep;
            if (Input.GetKey(KeyCode.J)) p.x -= MoveStep;
            if (Input.GetKey(KeyCode.L)) p.x += MoveStep;

            _cutout.localPosition = p;

            var s = _cutout.localScale;
            if (Input.GetKeyDown(KeyCode.U)) { s.x += ScaleStep; s.y += ScaleStep; }
            if (Input.GetKeyDown(KeyCode.O)) { s.x = Mathf.Max(0.1f, s.x - ScaleStep); s.y = Mathf.Max(0.1f, s.y - ScaleStep); }
            if (Input.GetKeyDown(KeyCode.T)) s.x = Mathf.Max(0.1f, s.x + ScaleStep);
            if (Input.GetKeyDown(KeyCode.G)) s.x = Mathf.Max(0.1f, s.x - ScaleStep);
            if (Input.GetKeyDown(KeyCode.Y)) s.y = Mathf.Max(0.1f, s.y + ScaleStep);
            if (Input.GetKeyDown(KeyCode.H)) s.y = Mathf.Max(0.1f, s.y - ScaleStep);

            _cutout.localScale = s;
        }

        private void HandleMouse()
        {
            if (_cutout == null) return;

            // Right-drag avoids conflicting with gameplay left-click.
            if (Input.GetMouseButtonDown(1) && HitCutout(out _))
            {
                _dragging = true;
                var world = ScreenToWorld(Input.mousePosition);
                _dragWorldOffset = _cutout.position - (Vector3)world;
            }

            if (Input.GetMouseButtonUp(1))
                _dragging = false;

            if (!_dragging) return;

            var w = ScreenToWorld(Input.mousePosition);
            var parent = _cutout.parent;
            _cutout.position = (Vector3)w + _dragWorldOffset;
            if (parent != null)
            {
                var local = parent.InverseTransformPoint(_cutout.position);
                _cutout.localPosition = new Vector3(local.x, local.y, 0f);
            }
        }

        private bool HitCutout(out Bounds bounds)
        {
            bounds = default;
            if (_cutout == null) return false;

            var sr = _cutout.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return false;

            bounds = sr.bounds;
            var world = ScreenToWorld(Input.mousePosition);
            return bounds.Contains(new Vector3(world.x, world.y, bounds.center.z));
        }

        private static Vector2 ScreenToWorld(Vector3 screen)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }

        private void OnGUI()
        {
            if (!IsEnabled) return;

            string status = _layoutMode ? "ON — editing (F8 to exit)" : "OFF — press F8 to edit";
            GUI.Box(new Rect(8, 8, 380, _layoutMode ? 168 : 48), "Stair cutout layout");
            GUI.Label(new Rect(16, 28, 360, 20), status);

            if (!_layoutMode || _cutout == null) return;

            var d = StairCutoutLayout.Current;
            GUI.Label(new Rect(16, 52, 360, 116),
                "Move: IJKL or right-drag\n" +
                "Scale all: U bigger · O smaller\n" +
                "Width: T/G · Height: Y/H\n" +
                "Base height: F7 up · F6 down\n" +
                "F5 save · F9 reload\n\n" +
                $"offset ({d.offsetX:F3}, {d.offsetY:F3})\n" +
                $"scale ({d.scaleX:F3}, {d.scaleY:F3}) · height {d.heightWorld:F2}");
        }
    }
}
