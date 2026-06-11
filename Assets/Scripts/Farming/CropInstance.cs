// Assets/Scripts/Farming/CropInstance.cs
// Owned by: world/island agent
// Pure-logic crop growth state — no MonoBehaviour, no UnityEngine dependency.
using SkyHarvest.Island;

namespace SkyHarvest.Farming
{
    /// <summary>
    /// All growth/health state for one planted crop.
    /// Lives on a CropPlot; ticked by CropGrowthSystem via GameTickEvent.
    /// </summary>
    public class CropState
    {
        // ---- identity ----
        public string CropId { get; }

        // ---- progress ----
        public float GrowthProgress { get; private set; }   // 0..1
        public float Health         { get; private set; } = 1f;   // 0..1

        public int CurrentStage
        {
            get
            {
                if (IsDead) return _totalStages;   // dead = frame 4 in the strip
                return System.Math.Min(
                    (int)(GrowthProgress * (_totalStages - 1)),
                    _totalStages - 1);
            }
        }

        public bool IsHarvestable => GrowthProgress >= 1f && !IsDead;
        public bool IsDead        => Health <= 0f;

        // ---- internal ----
        private readonly float _growthTimeMinutes;
        private readonly int   _totalStages;
        private readonly float _waterPerMinute;
        private float _accumulatedGrowth;

        // ---- track whether we've already fired CropReadyEvent ----
        private bool _readyEventFired;

        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        /// <summary>Fresh planting.</summary>
        public CropState(string cropId,
                         float growthTimeMinutes,
                         int   stages,
                         float waterPerMinute)
        {
            CropId               = cropId;
            _growthTimeMinutes   = growthTimeMinutes;
            _totalStages         = stages;
            _waterPerMinute      = waterPerMinute;
        }

        /// <summary>
        /// Restore from save data — Task 20 helper.
        /// <paramref name="savedProgress"/> and <paramref name="savedHealth"/>
        /// are the persisted values from CropSaveData.
        /// </summary>
        public CropState(string cropId,
                         float growthTimeMinutes,
                         int   stages,
                         float waterPerMinute,
                         float savedProgress,
                         float savedHealth)
            : this(cropId, growthTimeMinutes, stages, waterPerMinute)
        {
            _accumulatedGrowth = savedProgress * growthTimeMinutes;
            GrowthProgress     = savedProgress;
            Health             = System.Math.Clamp(savedHealth, 0f, 1f);
            _readyEventFired   = GrowthProgress >= 1f;   // don't re-fire on load
        }

        // -----------------------------------------------------------------------
        // Tick (called by CropGrowthSystem every GameTickEvent)
        // -----------------------------------------------------------------------
        public void Tick(float deltaMinutes, SoilState soil,
                         float sunExposure, float windDamage)
        {
            if (IsDead || IsHarvestable) return;

            // Consume water
            float waterNeeded    = _waterPerMinute * deltaMinutes;
            float waterAvailable = soil.WaterLevel;
            float waterFactor    = waterAvailable >= waterNeeded
                ? 1f
                : waterAvailable / (waterNeeded + 0.001f);
            soil.ConsumeWater(System.Math.Min(waterNeeded, waterAvailable));

            // Growth
            float growthRate    = waterFactor * sunExposure * soil.GrowthMultiplier();
            _accumulatedGrowth += deltaMinutes * growthRate;
            GrowthProgress      = _accumulatedGrowth / _growthTimeMinutes;

            // Clamp and publish ready event once
            if (GrowthProgress >= 1f)
            {
                GrowthProgress = 1f;
                if (!_readyEventFired)
                {
                    _readyEventFired = true;
                    Core.EventBus.Publish(new Core.CropReadyEvent { CropId = CropId });
                }
            }

            // Wind damage
            if (windDamage > 0f)
            {
                Health -= windDamage * deltaMinutes * 0.1f;
                if (Health < 0f)
                {
                    Health = 0f;
                    Core.EventBus.Publish(new Core.CropDiedEvent { CropId = CropId });
                }
            }
        }
    }
}
