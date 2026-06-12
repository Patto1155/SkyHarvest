using UnityEngine;
using UnityEngine.UI;
using SkyHarvest.Core;
using SkyHarvest.Storage;
using SkyHarvest.Workshop;

namespace SkyHarvest.UI
{
    /// <summary>
    /// Brief one-time tooltips on first interaction with each game system.
    /// </summary>
    public class ContextualTooltipUI : MonoBehaviour
    {
        private const string PrefsPrefix = "skyharvest_tooltip_";

        private GameObject? _banner;
        private Text? _text;
        private float _hideAt;

        public void Initialize(GameObject banner, Text text)
        {
            _banner = banner;
            _text   = text;
            _banner.SetActive(false);

            EventBus.Subscribe<ToolEquippedEvent>(_ => TryShow("tools",
                "Press 1–4 to equip tools. Hoe tills soil, watering can waters, sickle harvests ripe crops."));
            EventBus.Subscribe<CropPlantedEvent>(_ => TryShow("farming",
                "Crops need water and time. Rain helps — storms can damage unprotected plants."));
            EventBus.Subscribe<StructurePlacedEvent>(_ => TryShow("building",
                "Press B for build mode. Place structures to expand and protect your island."));
            EventBus.Subscribe<WorkshopInteractEvent>(_ => TryShow("workshop",
                "Pick a recipe, press Start, then Collect when processing finishes."));
            EventBus.Subscribe<StorageContainer.OpenStorageEvent>(_ => TryShow("storage",
                "Transfer items between your pack and this container."));
            EventBus.Subscribe<DebrisScavengedEvent>(_ => TryShow("debris",
                "Debris drifts in from the cliffs. Scavenge for wood, scrap, and ore."));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                TryShow("inventory", "Tab opens your pack. Click items to review what you carry.");

            if (_banner != null && _banner.activeSelf && Time.unscaledTime >= _hideAt)
                _banner.SetActive(false);
        }

        private void TryShow(string id, string message)
        {
            if (PlayerPrefs.GetInt(PrefsPrefix + id, 0) != 0) return;
            PlayerPrefs.SetInt(PrefsPrefix + id, 1);
            PlayerPrefs.Save();

            if (_text != null) _text.text = message;
            if (_banner != null) _banner.SetActive(true);
            _hideAt = Time.unscaledTime + 6f;
        }
    }
}
