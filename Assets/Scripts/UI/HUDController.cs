using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Farming;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class HUDController : MonoBehaviour
    {
        private Text? _timeText;
        private Text? _weatherText;
        private Text? _interactPromptText;

        // Unified hotbar (tools + items share one bar; see Hotbar.cs).
        private GameObject[]? _hotbarSlots;
        private Image[]?      _hotbarBgs;
        private Image[]?      _hotbarIcons;
        private Text[]?       _hotbarCounts;
        private Hotbar?       _hotbar;

        private static readonly Color SlotNormal   = new Color(0.20f, 0.18f, 0.18f, 0.90f);
        private static readonly Color SlotSelected = new Color(0.95f, 0.80f, 0.45f, 0.95f);

        private PlayerInventoryComponent? _playerInv;
        private ToolSystem? _toolSys;
        private InteractionSystem? _interactSys;

        public void Initialize(PlayerInventoryComponent inv, ToolSystem tools, InteractionSystem interact)
        {
            _playerInv  = inv;
            _toolSys    = tools;
            _interactSys = interact;
            EventBus.Subscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            EventBus.Subscribe<InventoryChangedEvent>(_ => RefreshHotbar());
            EventBus.Subscribe<ToolEquippedEvent>(_ => RefreshHotbar());
            EventBus.Subscribe<HotbarSelectionChangedEvent>(_ => RefreshHotbar());
            RefreshHotbar();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
        }

        public void SetTimeText(Text t)     { _timeText = t; }
        public void SetWeatherText(Text t)  { _weatherText = t; }
        public void SetPromptText(Text t)   { _interactPromptText = t; }

        public void SetHotbar(Hotbar hotbar) { _hotbar = hotbar; RefreshHotbar(); }

        /// <summary>Cache the per-slot UI widgets (bg / icon / count) from the slot GameObjects.</summary>
        public void SetHotbarSlots(GameObject[] slots)
        {
            _hotbarSlots  = slots;
            _hotbarBgs    = new Image[slots.Length];
            _hotbarIcons  = new Image[slots.Length];
            _hotbarCounts = new Text[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                _hotbarBgs[i]    = slots[i].GetComponent<Image>();
                _hotbarIcons[i]  = slots[i].transform.Find("Icon")?.GetComponent<Image>()!;
                _hotbarCounts[i] = slots[i].transform.Find("Label")?.GetComponent<Text>()!;
            }
        }

        private void OnHourChanged(HourChangedEvent e)
        {
            if (_timeText != null)
                _timeText.text = $"Day {GameManager.Instance?.Clock.CurrentDay + 1}  {e.Hour:D2}:00";
        }

        private void OnWeatherChanged(WeatherChangedEvent e)
        {
            if (_weatherText != null)
                _weatherText.text = e.Current.ToString().Replace("_", " ");
        }

        private void Update()
        {
            if (_interactPromptText != null && _interactSys != null)
            {
                var target = _interactSys.CurrentTarget;
                if (target == null)
                    _interactPromptText.text = "";
                else if (IsInspectable(target))
                    _interactPromptText.text = $"[E] {target.InteractionPrompt}   [Q] Inspect";
                else
                    _interactPromptText.text = $"[E] {target.InteractionPrompt}";
            }
        }

        private static bool IsInspectable(IInteractable target) =>
            target is CropPlot or Structure;

        private static string ToolIconPath(ToolType tool) => tool switch
        {
            ToolType.Hoe         => "Sprites/ui/icon_tool_hoe",
            ToolType.WateringCan => "Sprites/ui/icon_tool_wateringcan",
            ToolType.Sickle      => "Sprites/ui/icon_tool_sickle",
            ToolType.Hammer      => "Sprites/ui/icon_tool_hammer",
            _                    => ""
        };

        // Renders the unified bar: leading tool slots, then a window onto the
        // first inventory stacks, with the selected slot highlighted.
        private void RefreshHotbar()
        {
            if (_hotbarIcons == null || _hotbarBgs == null || _hotbarCounts == null) return;
            var model = _hotbar?.Model;

            for (int i = 0; i < _hotbarIcons.Length; i++)
            {
                bool selected = model != null && i == model.SelectedIndex;
                if (_hotbarBgs[i] != null)
                    _hotbarBgs[i].color = selected ? SlotSelected : SlotNormal;

                string iconPath;
                string countText;

                if (model != null && model.IsToolSlot(i))
                {
                    iconPath  = ToolIconPath(model.ToolAt(i));
                    countText = "";
                }
                else
                {
                    string? itemId = model?.ItemIdAt(i);
                    iconPath = string.IsNullOrEmpty(itemId) ? "" : $"Sprites/items/icon_{itemId}";
                    int count = model?.CountAt(i) ?? 0;
                    countText = count > 1 ? count.ToString() : "";
                }

                if (_hotbarIcons[i] != null)
                {
                    var spr = string.IsNullOrEmpty(iconPath) ? null : SpriteLoader.Load(iconPath);
                    _hotbarIcons[i].sprite  = spr;
                    _hotbarIcons[i].enabled = spr != null;
                }
                if (_hotbarCounts[i] != null)
                    _hotbarCounts[i].text = countText;
            }
        }
    }
}
