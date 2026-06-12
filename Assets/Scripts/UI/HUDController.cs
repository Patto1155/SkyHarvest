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
        private Image? _toolIcon;
        private GameObject[]? _hotbarSlots;
        private Image[]? _hotbarIcons;

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
            EventBus.Subscribe<ToolEquippedEvent>(_ => { RefreshHotbar(); RefreshToolIcon(); });
            RefreshToolIcon();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
        }

        public void SetTimeText(Text t)     { _timeText = t; }
        public void SetWeatherText(Text t)  { _weatherText = t; }
        public void SetPromptText(Text t)   { _interactPromptText = t; }
        public void SetHotbarSlots(GameObject[] slots, Image[] icons) { _hotbarSlots = slots; _hotbarIcons = icons; }
        public void SetToolIcon(Image icon) { _toolIcon = icon; }

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

        private void RefreshToolIcon()
        {
            if (_toolIcon == null || _toolSys == null) return;
            var tool = _toolSys.EquippedTool;
            if (tool == ToolType.None)
            {
                _toolIcon.enabled = false;
                _toolIcon.sprite  = null;
                return;
            }

            string path = tool switch
            {
                ToolType.Hoe          => "Sprites/ui/icon_tool_hoe",
                ToolType.WateringCan  => "Sprites/ui/icon_tool_wateringcan",
                ToolType.Sickle       => "Sprites/ui/icon_tool_sickle",
                ToolType.Hammer       => "Sprites/ui/icon_tool_hammer",
                _                     => ""
            };

            var spr = string.IsNullOrEmpty(path) ? null : SpriteLoader.Load(path);
            _toolIcon.sprite  = spr;
            _toolIcon.enabled = spr != null;
        }

        private void RefreshHotbar()
        {
            if (_hotbarIcons == null || _playerInv == null) return;
            var slots = _playerInv.Inventory.Slots;
            for (int i = 0; i < _hotbarIcons.Length && i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    _hotbarIcons[i].sprite = null;
                    _hotbarIcons[i].enabled = false;
                }
                else
                {
                    var spr = SpriteLoader.Load($"Sprites/items/icon_{slots[i].ItemId}");
                    _hotbarIcons[i].sprite  = spr;
                    _hotbarIcons[i].enabled = true;
                }
            }
        }
    }
}
