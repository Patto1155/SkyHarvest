using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class HUDController : MonoBehaviour
    {
        private Text? _timeText;
        private Text? _weatherText;
        private Text? _interactPromptText;
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
            EventBus.Subscribe<ToolEquippedEvent>(_ => RefreshHotbar());
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
                _interactPromptText.text = target != null ? $"[E] {target.InteractionPrompt}" : "";
            }
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
