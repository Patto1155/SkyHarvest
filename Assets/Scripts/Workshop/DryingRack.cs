// Drying Rack — weather-sensitive workshop.
// Rain RUINS the current batch (inputs lost, WorkshopRuinedEvent published).
// Checks per CONVENTIONS: overrides CanContinue() to return false in rain/storm.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Workshop
{
    public class DryingRack : WorkshopBase
    {
        protected override void Start()
        {
            base.Start();
            LoadFrames("Sprites/structures/drying_rack", 96);
            _workshopId = "drying_rack";
        }

        /// <summary>
        /// Rain ruins drying batches — return false when raining.
        /// </summary>
        protected override bool CanContinue()
        {
            var weather = Weather.WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;
            return weather != WeatherType.LightRain && weather != WeatherType.HeavyStorm;
        }

        public override WorkshopType GetWorkshopType() => WorkshopType.DryingRack;
    }
}
