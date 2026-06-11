// Assets/Scripts/Island/TerrainType.cs
// Owned by: world/island agent
namespace SkyHarvest.Island
{
    public enum TerrainType
    {
        FertileValley,   // Best soil, flood risk in storms
        RockyPlateau,    // Safe from flooding, good for workshops, poor soil
        CliffEdge,       // Dangerous; debris lands here — best scavenging
        NaturalSpring,   // Rare; big irrigation advantage
        WindCorridor,    // Exposed; bad for crops, good for windmills
        Scaffold         // Artificial expansion platform (timber planks)
    }

    public static class TerrainProperties
    {
        public static float BaseSoilQuality(TerrainType type) => type switch
        {
            TerrainType.FertileValley => 80f,
            TerrainType.RockyPlateau  => 20f,
            TerrainType.CliffEdge     => 10f,
            TerrainType.NaturalSpring => 60f,
            TerrainType.WindCorridor  => 30f,
            TerrainType.Scaffold      => 15f,
            _                         => 50f
        };

        public static float FloodRisk(TerrainType type) => type switch
        {
            TerrainType.FertileValley => 0.6f,
            TerrainType.RockyPlateau  => 0.05f,
            TerrainType.CliffEdge     => 0.1f,
            _                         => 0.2f
        };

        public static float WindExposure(TerrainType type) => type switch
        {
            TerrainType.WindCorridor  => 0.9f,
            TerrainType.CliffEdge     => 0.7f,
            TerrainType.RockyPlateau  => 0.4f,
            TerrainType.FertileValley => 0.2f,
            TerrainType.NaturalSpring => 0.3f,
            TerrainType.Scaffold      => 0.5f,
            _                         => 0.5f
        };

        public static bool CanPlaceCrops(TerrainType type) =>
            type != TerrainType.CliffEdge && type != TerrainType.Scaffold;

        public static bool HasWaterSource(TerrainType type) =>
            type == TerrainType.NaturalSpring;

        /// <summary>Sprite manifest path for the terrain tile strip.</summary>
        public static string TilePath(TerrainType type) => type switch
        {
            TerrainType.FertileValley => "Sprites/terrain/tile_fertile_valley",
            TerrainType.RockyPlateau  => "Sprites/terrain/tile_rocky_plateau",
            TerrainType.CliffEdge     => "Sprites/terrain/tile_cliff_edge",
            TerrainType.NaturalSpring => "Sprites/terrain/tile_natural_spring",
            TerrainType.WindCorridor  => "Sprites/terrain/tile_wind_corridor",
            TerrainType.Scaffold      => "Sprites/terrain/tile_scaffold",
            _                         => "Sprites/terrain/tile_rocky_plateau"
        };

        /// <summary>Number of sprite variants (strip frames) for this terrain type.</summary>
        public static int VariantCount(TerrainType type) => type switch
        {
            TerrainType.Scaffold => 1,
            _                    => 3
        };
    }
}
