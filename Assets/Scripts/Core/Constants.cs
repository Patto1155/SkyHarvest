namespace SkyHarvest.Core
{
    public static class Constants
    {
        // Grid
        public const float GridCellSize = 1f;
        public const int DefaultIslandRadius = 12;

        // Time
        // 1 real minute = 1 game minute, so CropDef.GrowthTimeMinutes and weather durations are
        // the real-time values the spec quotes (starter crops 2-5 min). Was 1f (60× faster) during
        // the desktop prototype; verify-harness steps publish GameTickEvent directly so they don't care.
        public const float SecondsPerGameMinute = 60f;
        public const float MinutesPerGameHour = 60f;
        public const float HoursPerGameDay = 24f;

        // Farming
        public const float StarterCropGrowthMinutes = 3f;
        public const float StapleCropGrowthMinutes = 15f;
        public const float MaxSoilNutrients = 100f;
        public const float MaxSoilWater = 100f;

        // Weather
        public const float MinWeatherDurationMinutes = 5f;
        public const float MaxWeatherDurationMinutes = 10f;

        // Offline simulation (spec 2026-09-09 §3)
        public const long  OfflineCapSeconds = 4 * 60 * 60;
        // 10s keeps per-step crop water draw (WaterConsumptionPerMinute × 10) under a
        // sprinkler's refill, so coarse replay matches live pacing. 4h cap = 1440 steps.
        public const float OfflineStepSeconds = 10f;

        // Debris
        public const float BaseDebrisIntervalSeconds = 45f;
        public const float GaleWindDebrisMultiplier = 0.4f;

        // Rendering (2:1 dimetric sprite projection)
        public const float TileWorldWidth = 1f;
        public const float TileWorldHeight = 0.5f;
        public const float ElevationWorldStep = 0.5f;
        public const int PixelsPerUnit = 64;
        public const int SortingOrderScale = 100;

        // Layers
        public const string InteractableLayer = "Interactable";
        public const string TerrainLayer = "Terrain";
        public const string StructureLayer = "Structure";
    }
}
