// Rain Catcher: fills internal water store during rain, auto-waters crop plots
// within 2.5 world units. Swaps empty/full sprite frame.
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Player;

namespace SkyHarvest.Building
{
    public class RainCatcher : Structure
    {
        private const float WaterPerRainMinute = 10f;
        private const float AutoWaterRadius     = 2.5f;
        private const float MaxWaterStore       = 100f;
        private const float HeavyStormMult      = 2f;

        private float _waterStore;
        private bool  _isFull;

        // Sprite frames: [0] = empty, [1] = full
        private SpriteRenderer _spriteRenderer;
        private Sprite[] _frames;

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _frames = SpriteLoader.LoadStrip("Sprites/structures/rain_catcher", 64);
        }

        private void OnEnable()
        {
            // Need to call base OnEnable (InteractableRegistry registration) then subscribe
            EventBus.Subscribe<GameTickEvent>(OnTick);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameTickEvent>(OnTick);
        }

        private void OnTick(GameTickEvent e)
        {
            var weather = Weather.WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;
            bool isRaining = weather == WeatherType.LightRain || weather == WeatherType.HeavyStorm;

            if (!isRaining) return;

            float mult = weather == WeatherType.HeavyStorm ? HeavyStormMult : 1f;
            float gained = WaterPerRainMinute * e.DeltaMinutes * mult;
            _waterStore = Mathf.Min(_waterStore + gained, MaxWaterStore);

            // Auto-water nearby crop plots within radius
            AutoWaterNearby(gained);

            UpdateSprite();
        }

        private void AutoWaterNearby(float waterAmount)
        {
            // Walk all CropPlots in the world and water those within radius
            // This avoids Physics2D per CONVENTIONS (no physics for structures)
            var myPos = new Vector2(transform.position.x, transform.position.y);
            var plots = FindObjectsOfType<Farming.CropPlot>();
            foreach (var plot in plots)
            {
                var plotPos = new Vector2(plot.transform.position.x, plot.transform.position.y);
                if (Vector2.Distance(myPos, plotPos) <= AutoWaterRadius)
                    plot.Soil?.AddWater(waterAmount);
            }
        }

        private void UpdateSprite()
        {
            bool nowFull = _waterStore >= MaxWaterStore * 0.95f;
            if (nowFull == _isFull) return;
            _isFull = nowFull;

            if (_frames != null && _frames.Length >= 2 && _spriteRenderer != null)
                _spriteRenderer.sprite = _frames[_isFull ? 1 : 0];
        }

        public override string InteractionPrompt =>
            _isFull ? "Collect Water" : "Rain Catcher (empty)";

        public override void Interact(PlayerController player)
        {
            if (TryDemolishWithHammer(player)) return;
            // Future: refill player watering can.
        }
    }
}
