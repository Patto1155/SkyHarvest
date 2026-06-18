using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Building;
using SkyHarvest.Core;
using SkyHarvest.Data;
using SkyHarvest.Farming;
using SkyHarvest.Player;

namespace SkyHarvest.UI
{
    public class HUDController : MonoBehaviour
    {
        private Text? _timeText;
        private Text? _weatherText;
        private Text? _interactPromptText;
        private Text? _hotbarNameText;
        private float _hotbarNameTimer;
        private const float HotbarNameDuration = 1.0f;

        // Unified hotbar (tools + items share one bar; see Hotbar.cs).
        private GameObject[]? _hotbarSlots;
        private Image[]?      _hotbarBgs;
        private Image[]?      _hotbarIcons;
        private Text[]?       _hotbarCounts;
        private Hotbar?       _hotbar;

        private static readonly Color SlotNormal   = new Color(0.58f, 0.50f, 0.42f, 0.95f);
        private static readonly Color SlotSelected = new Color(0.95f, 0.80f, 0.45f, 0.98f);

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
            EventBus.Subscribe<HotbarSelectionChangedEvent>(OnHotbarSelectionChanged);
            RefreshHotbar();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
        }

        public void SetTimeText(Text t)        { _timeText = t; }
        public void SetWeatherText(Text t)     { _weatherText = t; }
        public void SetPromptText(Text t)      { _interactPromptText = t; }
        public void SetHotbarNameText(Text t)  { _hotbarNameText = t; }

        public void SetHotbar(Hotbar hotbar) { _hotbar = hotbar; RefreshHotbar(); }

        public GameObject[]? HotbarSlotObjects => _hotbarSlots;

        public void RefreshHotbarPublic() => RefreshHotbar();

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

        private void OnHotbarSelectionChanged(HotbarSelectionChangedEvent e)
        {
            RefreshHotbar();
            if (_hotbarNameText == null || _hotbar == null) return;
            var model = _hotbar.Model;
            string name = ItemDisplayName(model.ItemIdAt(e.SlotIndex));
            if (string.IsNullOrEmpty(name)) return;
            _hotbarNameText.text  = name;
            _hotbarNameText.color = Color.white;
            _hotbarNameTimer      = HotbarNameDuration;
        }

        private void Update()
        {
            if (_interactPromptText != null && _interactSys != null)
            {
                var target = _interactSys.CurrentTarget;
                if (target != null)
                    _interactPromptText.text = IsInspectable(target)
                        ? $"[E] {target.InteractionPrompt}   [Q] Inspect"
                        : $"[E] {target.InteractionPrompt}";
                else if (_interactSys.CanCarveStairs)
                    _interactPromptText.text = "[E] Carve passage to the forge";
                else
                    _interactPromptText.text = "";
            }

            if (_hotbarNameText != null && _hotbarNameTimer > 0f)
            {
                _hotbarNameTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(_hotbarNameTimer / 0.3f);  // fade out in last 0.3s
                var c = _hotbarNameText.color;
                _hotbarNameText.color = new Color(c.r, c.g, c.b, alpha);
                if (_hotbarNameTimer <= 0f)
                    _hotbarNameText.text = "";
            }
        }

        private static string ItemDisplayName(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            var def = GameDatabase.GetItem(itemId);
            return def?.DisplayName ?? itemId;
        }

        private static bool IsInspectable(IInteractable target) =>
            target is CropPlot or Structure;

        private void RefreshHotbar()
        {
            if (_hotbarIcons == null || _hotbarBgs == null || _hotbarCounts == null) return;
            var model = _hotbar?.Model;

            for (int i = 0; i < _hotbarIcons.Length; i++)
            {
                bool selected = model != null && i == model.SelectedIndex;
                if (_hotbarBgs[i] != null)
                    _hotbarBgs[i].color = selected ? SlotSelected : SlotNormal;

                string? itemId = model?.ItemIdAt(i);
                string iconPath = string.IsNullOrEmpty(itemId) ? "" : ItemIconPaths.For(itemId);
                int count = model?.CountAt(i) ?? 0;
                string countText = count > 1 ? count.ToString() : "";

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
