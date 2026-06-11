// Assets/Scripts/Island/SoilPatch.cs
// Owned by: world/island agent
// SoilState is a pure-C# class (no MonoBehaviour) — lives on IslandCell.
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class SoilState
    {
        // ---- read-only properties ----
        public float Quality   { get; private set; }
        public float WaterLevel  { get; private set; }
        public float Nutrients   { get; private set; }
        public TerrainType Terrain { get; }

        // ---- tilled state ----
        public bool IsTilled   { get; private set; }
        public bool IsWet      => WaterLevel > 10f;
        public bool IsDry      => WaterLevel <= 0f;

        private string? _lastCropHarvested;
        private const float SameCropDepletion  = 15f;
        private const float DiffCropDepletion  = 5f;

        public SoilState(TerrainType terrain)
        {
            Terrain   = terrain;
            Quality   = TerrainProperties.BaseSoilQuality(terrain);
            WaterLevel  = 0f;
            Nutrients   = Constants.MaxSoilNutrients;
        }

        // ---- water ----
        public void AddWater(float amount)
        {
            WaterLevel = System.Math.Min(WaterLevel + amount, Constants.MaxSoilWater);
        }

        public void ConsumeWater(float amount)
        {
            WaterLevel = System.Math.Max(WaterLevel - amount, 0f);
        }

        // ---- tilling ----
        public void Till()
        {
            IsTilled = true;
        }

        // ---- nutrients ----
        public void RecordHarvest(string cropId)
        {
            // Null _lastCropHarvested means "no rotation" — treat same as repeat
            bool isSameCrop = _lastCropHarvested == null || cropId == _lastCropHarvested;
            float depletion = isSameCrop ? SameCropDepletion : DiffCropDepletion;
            Nutrients = System.Math.Max(0f, Nutrients - depletion);
            _lastCropHarvested = cropId;
        }

        public void ApplyCompost(float amount)
        {
            Nutrients = System.Math.Min(Nutrients + amount, Constants.MaxSoilNutrients);
        }

        // ---- growth multiplier ----
        public float GrowthMultiplier()
        {
            // waterFactor: 0..1 based on soil moisture
            float waterFactor    = WaterLevel > 10f ? 1f : WaterLevel / 10f;
            // nutrientFactor: 0..1; full nutrients = full speed
            float nutrientFactor = Nutrients / Constants.MaxSoilNutrients;
            // Quality is a harvest-yield bonus, not a growth-speed modifier
            return waterFactor * nutrientFactor;
        }

        /// <summary>Save/load restore helper (Task 20 step 5).</summary>
        public void SetState(float water, float nutrients)
        {
            WaterLevel = System.Math.Clamp(water,     0f, Constants.MaxSoilWater);
            Nutrients  = System.Math.Clamp(nutrients, 0f, Constants.MaxSoilNutrients);
        }
    }
}
