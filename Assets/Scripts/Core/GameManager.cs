using UnityEngine;
using SkyHarvest.Island;
using SkyHarvest.SaveLoad;

namespace SkyHarvest.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager? Instance { get; private set; }

        public GameTimeClock Clock { get; private set; } = new();
        public IslandData? CurrentIsland { get; private set; }

        private bool _paused;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_paused) return;
            Clock.Tick(Time.deltaTime);
        }

        public void SetIsland(IslandData island) => CurrentIsland = island;

        public void Pause()  { _paused = true;  Time.timeScale = 0f; }
        public void Resume() { _paused = false; Time.timeScale = 1f; }
        public bool IsPaused => _paused;

        public void SaveGame()  => SaveManager.Instance?.Save();
        public void DeleteSave() => SaveManager.Instance?.DeleteSave();
    }
}
