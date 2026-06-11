// Assets/Scripts/Farming/CropGrowthSystem.cs
// Owned by: world/island agent
// Manages all active CropPlots; ticks them each GameTickEvent.
// Waters soil from rain.  Publishes CropReadyEvent / CropDiedEvent (done in CropState).
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Farming
{
    public class CropGrowthSystem : MonoBehaviour
    {
        public static CropGrowthSystem? Instance { get; private set; }

        private readonly List<CropPlot> _plots = new();

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()  => EventBus.Subscribe<GameTickEvent>(OnTick);
        private void OnDisable() => EventBus.Unsubscribe<GameTickEvent>(OnTick);

        // -----------------------------------------------------------------------
        // Registration
        // -----------------------------------------------------------------------
        public void Register(CropPlot plot)
        {
            if (!_plots.Contains(plot)) _plots.Add(plot);
        }

        public void Unregister(CropPlot plot) => _plots.Remove(plot);

        // -----------------------------------------------------------------------
        // Tick
        // -----------------------------------------------------------------------
        private void OnTick(GameTickEvent e)
        {
            WeatherType weather = GetCurrentWeather();

            float rainWater    = GetRainWater(weather, e.DeltaMinutes);
            float sunExposure  = GetSunExposure(weather);

            for (int i = _plots.Count - 1; i >= 0; i--)
            {
                var plot = _plots[i];
                if (plot == null)
                {
                    _plots.RemoveAt(i);
                    continue;
                }
                if (plot.Crop == null) continue;

                // Rain waters the soil
                if (rainWater > 0f)
                    plot.Soil.AddWater(rainWater);

                float windDamage = GetWindDamage(weather);
                plot.Crop.Tick(e.DeltaMinutes, plot.Soil, sunExposure, windDamage);

                // Refresh visual overlay after every tick
                plot.RefreshVisuals();
            }
        }

        // -----------------------------------------------------------------------
        // Weather helpers
        // -----------------------------------------------------------------------
        private static WeatherType GetCurrentWeather()
        {
            // Avoid hard dependency on WeatherManager — use reflection-free
            // lazy access via a static reference that WeatherManager sets.
            return WeatherManagerBridge.CurrentWeather;
        }

        private static float GetSunExposure(WeatherType w) => w switch
        {
            WeatherType.ClearSkies  => 1.0f,
            WeatherType.LightRain   => 0.6f,
            WeatherType.HeavyStorm  => 0.2f,
            WeatherType.GaleWinds   => 0.7f,
            WeatherType.FogBank     => 0.3f,
            WeatherType.SkyDrought  => 1.0f,
            _                       => 0.5f
        };

        private static float GetWindDamage(WeatherType w) => w switch
        {
            WeatherType.GaleWinds   => 0.8f,
            WeatherType.HeavyStorm  => 0.5f,
            _                       => 0f
        };

        private static float GetRainWater(WeatherType w, float deltaMinutes) => w switch
        {
            WeatherType.LightRain   => 5f  * deltaMinutes,
            WeatherType.HeavyStorm  => 15f * deltaMinutes,
            _                       => 0f
        };
    }

    /// <summary>
    /// Thin static bridge so CropGrowthSystem can read weather without
    /// creating a hard assembly-level dependency on the Weather namespace.
    /// WeatherManager sets CurrentWeather each frame (or on change).
    /// </summary>
    public static class WeatherManagerBridge
    {
        public static WeatherType CurrentWeather { get; set; } = WeatherType.ClearSkies;
    }
}
