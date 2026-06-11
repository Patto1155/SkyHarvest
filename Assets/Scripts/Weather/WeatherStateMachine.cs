// Pure logic — no UnityEngine dependency.
// Owns transition weight tables (plan Task 11, adapted to CONVENTIONS).
// ForceTransition and SetState(WeatherType, float) for save/restore.
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherStateMachine
    {
        public WeatherType CurrentWeather { get; private set; }
        public float MinutesRemaining { get; private set; }

        // Hint for ambient pre-telegraphing (≤1.5 min remaining)
        public WeatherType? NextWeatherHint { get; private set; }

        private System.Random _rng;
        private WeatherType _nextWeather;

        public WeatherStateMachine(WeatherType initial, int seed = 0)
        {
            _rng = new System.Random(seed);
            CurrentWeather = initial;
            MinutesRemaining = RollDuration();
            _nextWeather = PickNext(CurrentWeather);
        }

        /// <summary>
        /// Called each game tick with deltaMinutes. Transitions when time expires.
        /// </summary>
        public void Tick(float deltaMinutes)
        {
            MinutesRemaining -= deltaMinutes;

            // Expose upcoming weather hint for ambient cues when within 1.5 min
            NextWeatherHint = MinutesRemaining <= 1.5f ? _nextWeather : (WeatherType?)null;

            if (MinutesRemaining <= 0f)
                ApplyTransition(_nextWeather);
        }

        /// <summary>
        /// Immediately transition to a specific weather state (e.g. debug, scripted events).
        /// </summary>
        public void ForceTransition(WeatherType next)
        {
            ApplyTransition(next);
        }

        /// <summary>
        /// Restore weather state from save data.
        /// </summary>
        public void SetState(WeatherType weather, float minutesRemaining)
        {
            var prev = CurrentWeather;
            CurrentWeather = weather;
            MinutesRemaining = minutesRemaining;
            _nextWeather = PickNext(CurrentWeather);
            NextWeatherHint = minutesRemaining <= 1.5f ? _nextWeather : (WeatherType?)null;

            // Publish so listeners (effects, audio) can sync their state without waiting
            if (prev != weather)
                EventBus.Publish(new WeatherChangedEvent { Previous = prev, Current = weather });
        }

        // ---- internals ----

        private void ApplyTransition(WeatherType next)
        {
            var prev = CurrentWeather;
            CurrentWeather = next;
            MinutesRemaining = RollDuration();
            _nextWeather = PickNext(CurrentWeather);
            NextWeatherHint = null;

            if (prev != next)
                EventBus.Publish(new WeatherChangedEvent { Previous = prev, Current = next });
        }

        private WeatherType PickNext(WeatherType current)
        {
            var weights = GetTransitionWeights(current);
            float total = 0f;
            foreach (var (_, w) in weights) total += w;

            float roll = (float)_rng.NextDouble() * total;
            float cumulative = 0f;

            foreach (var (weather, weight) in weights)
            {
                cumulative += weight;
                if (roll <= cumulative) return weather;
            }

            return WeatherType.ClearSkies;
        }

        private (WeatherType, float)[] GetTransitionWeights(WeatherType current)
        {
            // Binding transition weight tables (plan Task 11)
            return current switch
            {
                WeatherType.ClearSkies => new[]
                {
                    (WeatherType.LightRain,   0.35f),
                    (WeatherType.ClearSkies,  0.30f),
                    (WeatherType.GaleWinds,   0.15f),
                    (WeatherType.FogBank,     0.10f),
                    (WeatherType.SkyDrought,  0.10f),
                },
                WeatherType.LightRain => new[]
                {
                    (WeatherType.ClearSkies,  0.30f),
                    (WeatherType.HeavyStorm,  0.25f),
                    (WeatherType.LightRain,   0.25f),
                    (WeatherType.FogBank,     0.20f),
                },
                WeatherType.HeavyStorm => new[]
                {
                    (WeatherType.LightRain,   0.40f),
                    (WeatherType.GaleWinds,   0.25f),
                    (WeatherType.ClearSkies,  0.20f),
                    (WeatherType.HeavyStorm,  0.15f),
                },
                WeatherType.GaleWinds => new[]
                {
                    (WeatherType.ClearSkies,  0.35f),
                    (WeatherType.HeavyStorm,  0.30f),
                    (WeatherType.LightRain,   0.20f),
                    (WeatherType.SkyDrought,  0.15f),
                },
                WeatherType.FogBank => new[]
                {
                    (WeatherType.ClearSkies,  0.40f),
                    (WeatherType.LightRain,   0.35f),
                    (WeatherType.FogBank,     0.25f),
                },
                WeatherType.SkyDrought => new[]
                {
                    (WeatherType.ClearSkies,  0.40f),
                    (WeatherType.GaleWinds,   0.25f),
                    (WeatherType.SkyDrought,  0.20f),
                    (WeatherType.HeavyStorm,  0.15f),
                },
                _ => new[] { (WeatherType.ClearSkies, 1f) }
            };
        }

        private float RollDuration()
        {
            return Constants.MinWeatherDurationMinutes +
                   (float)_rng.NextDouble() *
                   (Constants.MaxWeatherDurationMinutes - Constants.MinWeatherDurationMinutes);
        }
    }
}
