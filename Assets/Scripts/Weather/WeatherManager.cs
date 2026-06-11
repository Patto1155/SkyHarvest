// MonoBehaviour wrapper for WeatherStateMachine.
// Created and wired exclusively by Bootstrap.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        public WeatherStateMachine StateMachine { get; private set; }
        public WeatherType CurrentWeather => StateMachine.CurrentWeather;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            int weatherSeed = System.Environment.TickCount;
            StateMachine = new WeatherStateMachine(WeatherType.ClearSkies, weatherSeed);
        }

        private void OnEnable()  => EventBus.Subscribe<GameTickEvent>(OnGameTick);
        private void OnDisable() => EventBus.Unsubscribe<GameTickEvent>(OnGameTick);

        private void OnGameTick(GameTickEvent e)
        {
            StateMachine.Tick(e.DeltaMinutes);
        }

        // ---- Save/load integration (called by SaveManager) ----

        /// <summary>
        /// Restore weather from save data. Pass minutesRemaining to resume mid-duration.
        /// </summary>
        public void RestoreState(WeatherType weather, float minutesRemaining)
        {
            StateMachine.SetState(weather, minutesRemaining);
        }

        public void StartWeather()
        {
            // Already running; this is a no-op but Bootstrap calls it after scene setup
        }
    }
}
