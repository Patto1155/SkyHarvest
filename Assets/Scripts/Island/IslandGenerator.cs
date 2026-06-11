// Assets/Scripts/Island/IslandGenerator.cs
// Owned by: world/island agent
// Pure-logic generation.  IslandGenerator.Generate(seed, radius) is a static
// method so it works headless in tests.  The MonoBehaviour wrapper is kept
// thin — it just passes configurable fields to the static method.
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Island
{
    public class IslandGenerator : MonoBehaviour
    {
        [SerializeField] private int _seed    = -1;          // -1 = random each run
        [SerializeField] private int _radius  = Constants.DefaultIslandRadius;
        [SerializeField] private float _noiseScale     = 0.15f;
        [SerializeField] private float _elevationScale = 0.10f;

        /// <summary>
        /// Instance wrapper called by Bootstrap / GameManager.
        /// </summary>
        public IslandData Generate() =>
            Generate(_seed < 0 ? Random.Range(0, 999999) : _seed, _radius,
                     _noiseScale, _elevationScale);

        // -----------------------------------------------------------------------
        // Static headless generation — used by tests and the instance wrapper
        // -----------------------------------------------------------------------
        /// <summary>
        /// Generate an island from a deterministic seed.
        /// Same seed always produces the same <see cref="IslandData"/>.
        /// </summary>
        public static IslandData Generate(int seed, int radius)
            => Generate(seed, radius, 0.15f, 0.10f);

        public static IslandData Generate(int seed, int radius,
                                          float noiseScale, float elevationScale)
        {
            var island = new IslandData(seed, radius);
            var rng    = new System.Random(seed);

            // Perlin-noise offsets derived from the seed so each seed differs
            float offsetX = (float)(rng.NextDouble() * 1000.0);
            float offsetY = (float)(rng.NextDouble() * 1000.0);

            // 30 % chance of a natural spring near the centre
            bool hasSpring  = rng.NextDouble() < 0.3;
            var  springPos  = new Vector2Int(
                rng.Next(-radius / 3, radius / 3 + 1),
                rng.Next(-radius / 3, radius / 3 + 1));

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var pos   = new Vector2Int(x, y);
                    float mag = pos.magnitude / radius;   // 0 at centre, 1 at ring

                    // Shape: noise + radial falloff
                    float noise = Mathf.PerlinNoise(
                        (x + offsetX) * noiseScale,
                        (y + offsetY) * noiseScale);

                    float threshold = 1f - mag + (noise - 0.5f) * 0.6f;
                    if (threshold <= 0.1f) continue;   // outside island

                    bool isEdge = threshold < 0.25f;

                    // Elevation (separate noise band)
                    float elevation = Mathf.PerlinNoise(
                        (x + offsetX + 500f) * elevationScale,
                        (y + offsetY + 500f) * elevationScale) * 3f;

                    // Terrain assignment
                    TerrainType terrain;
                    if (hasSpring && Vector2Int.Distance(pos, springPos) < 2)
                        terrain = TerrainType.NaturalSpring;
                    else if (isEdge)
                        terrain = TerrainType.CliffEdge;
                    else if (elevation > 2.0f && noise > 0.55f)
                        terrain = TerrainType.WindCorridor;
                    else if (elevation > 1.5f)
                        terrain = TerrainType.RockyPlateau;
                    else
                        terrain = TerrainType.FertileValley;

                    island.Cells[pos] = new IslandCell
                    {
                        GridPos   = pos,
                        Terrain   = terrain,
                        Elevation = elevation,
                        Soil      = new SoilState(terrain),
                        IsEdge    = isEdge
                    };
                }
            }

            return island;
        }
    }
}
