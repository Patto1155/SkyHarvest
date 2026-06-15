// 2D build mode controller (legacy Input, no prefabs, no Rigidbody).
// Bootstrap owns the B/Esc hotkeys and calls Enter/ExitBuildMode; while active
// the ghost follows the mouse grid cell and left-click places.
// Staged building (spec §2): placing creates a ConstructionSite at 0 materials;
// deliver via E to complete. Set InstantBuild=true (debug/tests) for the old
// consume-and-spawn behaviour. SetSelected(StructureDef) called by BuildMenuUI.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Player;

namespace SkyHarvest.Building
{
    public class BuildModeController : MonoBehaviour
    {
        public static BuildModeController Instance { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>Debug/testing escape hatch: consume materials and spawn finished instantly.</summary>
        public static bool InstantBuild = false;

        private StructureDef _selected;
        private GameObject _ghostGo;
        private SpriteRenderer _ghostRenderer;
        private Vector2Int _ghostGridPos;
        private bool _ghostValid;

        private Island.IslandData _island;
        private PlayerController _player;

        // Colors for ghost validity
        private static readonly Color ValidColor   = new Color(0f, 1f, 0.3f, 0.5f);
        private static readonly Color InvalidColor = new Color(1f, 0.1f, 0.1f, 0.5f);

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetIsland(Island.IslandData island) { _island = island; }
        public void SetPlayer(PlayerController player)   { _player = player; }

        /// <summary>Called by BuildMenuUI when the player picks a structure.</summary>
        public void SetSelected(StructureDef def)
        {
            _selected = def;
            if (IsActive) RebuildGhost();
        }

        private void Update()
        {
            if (!IsActive || _selected == null) return;

            // Convert mouse position → grid cell
            UpdateGhostPosition();

            // Left-click to place
            if (Input.GetMouseButtonDown(0) && _ghostValid)
                TryPlace(_ghostGridPos);
        }

        public void EnterBuildMode()
        {
            IsActive = true;
            if (_selected != null) RebuildGhost();
        }

        public void ExitBuildMode()
        {
            IsActive = false;
            DestroyGhost();
        }

        private void RebuildGhost()
        {
            DestroyGhost();
            if (_selected == null) return;

            _ghostGo = new GameObject("BuildGhost");
            _ghostRenderer = _ghostGo.AddComponent<SpriteRenderer>();
            _ghostRenderer.sortingOrder = 5000;

            _ghostRenderer.sprite = LoadStructureSprite(_selected) ?? MakeFallbackSprite(Color.magenta);
            _ghostRenderer.color = ValidColor;
        }

        private void DestroyGhost()
        {
            if (_ghostGo != null) Destroy(_ghostGo);
            _ghostGo = null;
            _ghostRenderer = null;
        }

        private void UpdateGhostPosition()
        {
            if (_ghostGo == null || Camera.main == null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            // Resolve the mouse against the player's tier so the cursor maps to cells on
            // the tier you're standing on, and snap the ghost to that cell's real
            // elevation (so it reads as sitting ON the tier, not flat across the gap).
            int tier = _player != null ? _player.CurrentTier : 0;
            _ghostGridPos = GridMath.WorldToGrid(new Vector2(mouseWorld.x, mouseWorld.y), tier);

            float gElev = _island?.GetCell(_ghostGridPos)?.Elevation ?? 0f;
            var worldPos = GridMath.GridToWorld(_ghostGridPos, gElev);
            _ghostGo.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            _ghostValid = CanPlaceAt(_ghostGridPos);
            if (_ghostRenderer != null)
                _ghostRenderer.color = _ghostValid ? ValidColor : InvalidColor;
        }

        public bool CanPlaceAt(Vector2Int gridPos)
        {
            if (_island == null) return false;
            var cell = _island.GetCell(gridPos);
            if (cell == null) return false;
            // Don't bridge the cliff: build only on the tier the player stands on.
            if (_player != null && Mathf.RoundToInt(cell.Elevation) != _player.CurrentTier) return false;
            if (StructureRegistry.Instance != null && StructureRegistry.Instance.HasStructureAt(gridPos))
                return false;

            // Terrain placement rule (PlacementRule enum from Defs.cs)
            if (_selected != null)
            {
                switch (_selected.PlacementRule)
                {
                    case PlacementRule.EdgeCellOnly:
                        if (!cell.IsEdge) return false;
                        break;
                    case PlacementRule.CliffEdgeOnly:
                        if (cell.Terrain != Island.TerrainType.CliffEdge) return false;
                        break;
                    // PlacementRule.Any: no restriction
                }
            }

            // Staged building: ghosts are free to place (plan layouts first, spec §2);
            // materials are checked upfront only in InstantBuild mode.
            if (InstantBuild && _player != null && _selected != null)
            {
                var inv = _player.GetComponent<PlayerInventoryComponent>();
                if (inv != null)
                {
                    foreach (var cost in _selected.BuildCosts)
                    {
                        if (!inv.Inventory.Has(cost.ItemId, cost.Amount)) return false;
                    }
                }
            }

            return true;
        }

        private void TryPlace(Vector2Int gridPos)
        {
            if (_selected == null || !CanPlaceAt(gridPos)) return;

            if (InstantBuild)
            {
                if (_player != null)
                {
                    var inv = _player.GetComponent<PlayerInventoryComponent>();
                    if (inv != null)
                    {
                        foreach (var cost in _selected.BuildCosts)
                            inv.Inventory.TryRemove(cost.ItemId, cost.Amount);
                    }
                }
                PlaceStructure(gridPos, _selected);
                return;
            }

            PlaceConstructionSite(gridPos, _selected);
        }

        /// <summary>
        /// Place an unbuilt construction site at gridPos (also used by save restore).
        /// </summary>
        public ConstructionSite PlaceConstructionSite(Vector2Int gridPos, StructureDef def)
        {
            var go = new GameObject($"{def.DisplayName} (site)");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadStructureSprite(def) ?? MakeFallbackSprite(Color.magenta);

            float elev = _island?.GetCell(gridPos)?.Elevation ?? 0f;
            var worldPos = GridMath.GridToWorld(gridPos, elev);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * Constants.SortingOrderScale);

            var site = go.AddComponent<ConstructionSite>();
            site.Initialize(def, gridPos);
            StructureRegistry.Instance.Register(site);

            EventBus.Publish(new ConstructionSitePlacedEvent { StructureId = def.StructureId });
            return site;
        }

        /// <summary>
        /// Place a structure at gridPos (called by save restore too).
        /// </summary>
        public void PlaceStructure(Vector2Int gridPos, StructureDef def)
        {
            var go = new GameObject(def.DisplayName);

            var sr = go.AddComponent<SpriteRenderer>();

            // Position in world space (dimetric), at the cell's tier elevation.
            float elev = _island?.GetCell(gridPos)?.Elevation ?? 0f;
            var worldPos = GridMath.GridToWorld(gridPos, elev);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Sorting order
            sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * Constants.SortingOrderScale);

            // Attach appropriate Structure component based on StructureId
            Structure structure = AttachStructureComponent(go, def);
            structure.Initialize(def, gridPos);

            // Load idle sprite (frame 0 for strips, full texture for single images).
            // WorkshopBase subclasses override this in Start() — that's fine; this ensures
            // all other structure types (rain_catcher, skynet, storage, shelter, etc.)
            // show the correct sprite and never stay on the magenta fallback.
            sr.sprite = LoadStructureSprite(def) ?? MakeFallbackSprite(Color.magenta);

            StructureRegistry.Instance.Register(structure);

            // Light-emitting structures get a warm glow pool (forge fire, shelter lantern).
            StructureGlow.Attach(go, sr.sortingOrder, StructureGlow.Intensity(def.StructureId));

            // Scaffolding: trigger island expansion
            if (def.StructureId == "scaffolding" && _island != null)
            {
                var expansion = Island.IslandExpansion.Expand(_island, gridPos);

                // Re-render newly added cells via IslandRenderer (world agent owns this).
                // IslandExpansion.Expand already publishes IslandExpandedEvent — do not double-fire.
                var islandRenderer = Object.FindObjectOfType<Island.IslandRenderer>();
                if (islandRenderer != null) islandRenderer.RenderNewCells(expansion);
            }

            EventBus.Publish(new StructurePlacedEvent { StructureId = def.StructureId });
        }

        private Structure AttachStructureComponent(GameObject go, StructureDef def)
        {
            return def.StructureId switch
            {
                "rain_catcher" => go.AddComponent<RainCatcher>(),
                "skynet"       => go.AddComponent<Skynet.Skynet>(),
                "crate"        => go.AddComponent<Storage.StorageContainer>(),
                "barrel"       => go.AddComponent<Storage.StorageContainer>(),
                "drying_rack"  => go.AddComponent<Workshop.DryingRack>(),
                "stone_mill"   => go.AddComponent<Workshop.StoneMill>(),
                "forge"        => go.AddComponent<Workshop.Forge>(),
                _              => go.AddComponent<Structure>(),
            };
        }

        private static Sprite LoadStructureSprite(StructureDef def)
        {
            if (def == null) return null;
            try
            {
                string path = $"Sprites/structures/{def.StructureId}";
                if (def.SpriteFrameWidth > 0)
                {
                    var frames = SpriteLoader.LoadStrip(path, def.SpriteFrameWidth);
                    return frames.Length > 0 ? frames[0] : null;
                }
                return SpriteLoader.Load(path);
            }
            catch { return null; }
        }

        private static Sprite MakeFallbackSprite(Color c)
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f));
        }

        public static BuildModeController CreateInstance(Island.IslandRenderer renderer)
        {
            var go  = new GameObject("BuildModeController");
            var bmc = go.AddComponent<BuildModeController>();
            return bmc;
        }
    }
}
