// DebrisSpawner: spawns debris objects on cliff-edge/edge cells.
// Real-time (Update/Time.deltaTime) per spec hybrid clock — deliberate.
// Spawn animation: object arcs/falls from above with a shadow that scales in over ~1.5s.
// Needs IslandData set by bootstrap via SetIsland(IslandData).
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;
using SkyHarvest.Island;

namespace SkyHarvest.Debris
{
    public class DebrisSpawner : MonoBehaviour
    {
        public static DebrisSpawner Instance { get; private set; }

        private IslandData _island;
        private float _timer;
        private System.Random _rng = new System.Random();

        // Debris variant sprites (debris_1.png, debris_2.png, debris_3.png)
        private Sprite[] _debrisSprites;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            _debrisSprites = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                _debrisSprites[i] = SpriteLoader.Load($"Sprites/debris/debris_{i + 1}");
                if (_debrisSprites[i] == null)
                    _debrisSprites[i] = MakeFallbackSprite();
            }

            _timer = GetSpawnInterval(); // start with a full interval
        }

        public void SetIsland(IslandData island) { _island = island; }

        // Real-time Update — deliberate per spec hybrid clock
        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = GetSpawnInterval();
                TrySpawnDebris();
            }
        }

        private float GetSpawnInterval()
        {
            float baseInterval = Constants.BaseDebrisIntervalSeconds;
            var weather = Weather.WeatherManager.Instance?.CurrentWeather ?? WeatherType.ClearSkies;
            return weather switch
            {
                WeatherType.GaleWinds  => baseInterval * Constants.GaleWindDebrisMultiplier,
                WeatherType.HeavyStorm => baseInterval * 0.6f,
                _                      => baseInterval
            };
        }

        private void TrySpawnDebris()
        {
            if (_island == null) return;

            // Collect all edge/cliff cells as landing zones
            var edgeCells = new List<IslandCell>();
            foreach (var cell in _island.Cells.Values)
                if (cell.IsEdge || cell.Terrain == TerrainType.CliffEdge)
                    edgeCells.Add(cell);

            if (edgeCells.Count == 0) return;

            // Pick random landing cell
            var landingCell = edgeCells[_rng.Next(edgeCells.Count)];

            // World position of landing cell
            float lx = (landingCell.GridPos.x - landingCell.GridPos.y) * 0.5f;
            float ly = (landingCell.GridPos.x + landingCell.GridPos.y) * -0.25f +
                       landingCell.Elevation * Constants.ElevationWorldStep;

            var landingPos = new Vector3(lx, ly, 0f);

            // Start position: above and to the side (simulates falling from sky)
            var startPos = landingPos + new Vector3(
                (float)(_rng.NextDouble() - 0.5) * 4f,
                2.5f + (float)_rng.NextDouble() * 1.5f,
                0f);

            SpawnDebrisAt(startPos, landingPos, landingCell.GridPos);
        }

        private void SpawnDebrisAt(Vector3 startPos, Vector3 landingPos, Vector2Int landingGridPos)
        {
            var go = new GameObject("Debris");
            go.transform.position = startPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _debrisSprites[_rng.Next(_debrisSprites.Length)];
            sr.sortingOrder = Mathf.RoundToInt(-landingPos.y * Constants.SortingOrderScale);

            var debris = go.AddComponent<DebrisObject>();
            debris.InitiateFall(landingPos, landingGridPos, _rng);
        }

        private static Sprite MakeFallbackSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.magenta;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f));
        }
    }
}
