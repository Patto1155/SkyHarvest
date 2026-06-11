// Assets/Scripts/Farming/CropPlot.cs
// Owned by: world/island agent
// MonoBehaviour that lives on a tilled cell.  Renders crop stage sprite.
// Registers itself in InteractableRegistry.
// Context interactions:
//   – Hoe on bare soil tills it (tilling creates/updates plot)
//   – WateringCan waters
//   – Sickle harvests ripe crop
// FarmingActions.TryTill creates the CropPlot; Interact handles subsequent ops.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Player;
using SkyHarvest.Island;

namespace SkyHarvest.Farming
{
    public class CropPlot : MonoBehaviour, IInteractable
    {
        // ---- data ----
        public SoilState Soil { get; set; } = null!;
        public CropState? Crop { get; set; }

        // ---- state flags ----
        public bool HasCrop  => Crop != null && !Crop.IsDead;
        public bool IsEmpty  => Crop == null;

        // ---- grid position (set by FarmingActions.TryTill) ----
        public Vector2Int GridPos { get; set; }

        // ---- sprite rendering ----
        private SpriteRenderer? _cropSr;
        private Sprite[]? _cropStrip;   // frames 0-3 = stages, 4 = dead
        private const int CropFrameWidth = 64;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            // Child SR for the crop sprite (sits on top of terrain)
            var go = new GameObject("CropSprite");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _cropSr = go.AddComponent<SpriteRenderer>();
            _cropSr.sortingOrder = GridMath.SortingOrder(transform.position.y, bias: 0);
        }

        private void OnEnable()  => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void OnDestroy() => InteractableRegistry.Unregister(this);

        // -----------------------------------------------------------------------
        // IInteractable
        // -----------------------------------------------------------------------
        public string InteractionPrompt
        {
            get
            {
                if (IsEmpty)
                {
                    // Check if player is holding hoe to suggest planting action
                    return "Plant";
                }
                if (Crop!.IsDead)       return "Clear dead crop";
                if (Crop.IsHarvestable) return $"Harvest {Crop.CropId}";
                return $"Water {Crop.CropId} (stage {Crop.CurrentStage + 1})";
            }
        }

        public void Interact(PlayerController player)
        {
            if (!player.TryGetComponent<ToolSystem>(out var tools)) return;

            switch (tools.EquippedTool)
            {
                case ToolType.Hoe:
                    if (IsEmpty)
                        FarmingActions.TrySow(this, player);
                    else if (Crop != null && Crop.IsDead)
                        FarmingActions.ClearDead(this);
                    break;

                case ToolType.WateringCan:
                    FarmingActions.Water(this);
                    break;

                case ToolType.Sickle:
                    if (HasCrop && Crop!.IsHarvestable)
                        FarmingActions.Harvest(this, player);
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // Visual refresh (called by CropGrowthSystem every tick)
        // -----------------------------------------------------------------------
        public void RefreshVisuals()
        {
            if (_cropSr == null) return;

            if (Crop == null)
            {
                _cropSr.enabled = false;
                return;
            }

            // Load strip lazily
            if (_cropStrip == null && !string.IsNullOrEmpty(Crop.CropId))
            {
                _cropStrip = TryLoadStrip($"Sprites/crops/crop_{Crop.CropId}");
            }

            if (_cropStrip != null && _cropStrip.Length > 0)
            {
                int frame = Crop.IsDead
                    ? Mathf.Min(4, _cropStrip.Length - 1)        // frame 4 = dead
                    : Mathf.Min(Crop.CurrentStage, _cropStrip.Length - 1);
                _cropSr.sprite  = _cropStrip[frame];
                _cropSr.enabled = true;

                // Wilt tint when health < 50 %
                _cropSr.color = Crop.Health < 0.5f
                    ? Color.Lerp(Color.yellow, Color.white, Crop.Health * 2f)
                    : Color.white;
            }
            else
            {
                // Magenta fallback so the game still runs without art
                _cropSr.sprite  = MagentaFallback();
                _cropSr.enabled = true;
            }
        }

        // -----------------------------------------------------------------------
        // Sprite helpers
        // -----------------------------------------------------------------------
        private static Sprite[]? TryLoadStrip(string path)
        {
            try   { return SpriteLoader.LoadStrip(path, CropFrameWidth); }
            catch { return null; }
        }

        private static Sprite MagentaFallback()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.magenta);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f));
        }

        public void Initialize(SoilState soil, CropState? crop, Vector2Int gridPos)
        {
            Soil    = soil;
            Crop    = crop;
            GridPos = gridPos;
            RefreshVisuals();
        }
    }
}
