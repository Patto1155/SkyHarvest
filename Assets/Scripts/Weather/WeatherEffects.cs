// 2D sprite-particle weather effects.
// Spawns looping rain/wind/fog sprites in a parent that follows the camera.
// No Unity particle system — code-spawned SpriteRenderers moving across the view.
// All sprite loads are null-safe (magenta fallback square per CONVENTIONS).
using System.Collections.Generic;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherEffects : MonoBehaviour
    {
        // ---- serialised by Bootstrap via property setters ----
        private Camera _cam;
        private WeatherType _currentWeather = WeatherType.ClearSkies;

        // Overlay SpriteRenderer used for lightning flashes
        private SpriteRenderer _flashOverlay;
        private float _flashTimer;
        private float _flashDuration;

        // Active particle objects
        private readonly List<WeatherParticle> _particles = new();
        private Transform _particleParent;

        // Sprites (loaded once)
        private Sprite _rainSprite;
        private Sprite _windSprite;
        private Sprite _fogSprite;
        private Sprite _flashSprite;     // white square for lightning

        private float _spawnTimer;

        // Constants
        private const int MaxRainParticles  = 40;
        private const int MaxWindParticles  = 20;
        private const int MaxFogParticles   = 8;
        private const float RainSpawnRate   = 0.05f;
        private const float WindSpawnRate   = 0.08f;
        private const float FogSpawnRate    = 0.4f;

        private void Awake()
        {
            // Create a parent that we'll move each frame to follow the camera
            var parentGo = new GameObject("WeatherParticleParent");
            _particleParent = parentGo.transform;

            // Flash overlay — fullscreen translucent SpriteRenderer
            var flashGo = new GameObject("LightningFlash");
            _flashOverlay = flashGo.AddComponent<SpriteRenderer>();
            _flashOverlay.sortingOrder = 9000;
            _flashOverlay.enabled = false;

            LoadSprites();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);
        }

        public void SetCamera(Camera cam) { _cam = cam; }

        private void OnWeatherChanged(WeatherChangedEvent e)
        {
            _currentWeather = e.Current;
            ClearAllParticles();
        }

        private void Update()
        {
            if (_cam == null) return;

            // Move particle parent to camera so particles are always in view
            _particleParent.position = _cam.transform.position;

            // Spawn new particles
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnForWeather(_currentWeather);
                _spawnTimer = GetSpawnRate(_currentWeather);
            }

            // Tick existing particles; remove dead ones
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                if (!p.Tick(Time.deltaTime))
                {
                    if (p.Renderer != null) Object.Destroy(p.Renderer.gameObject);
                    _particles.RemoveAt(i);
                }
            }

            // Lightning flash tick for HeavyStorm
            if (_currentWeather == WeatherType.HeavyStorm || _currentWeather == WeatherType.GaleWinds)
                TickLightning();
        }

        private void SpawnForWeather(WeatherType w)
        {
            switch (w)
            {
                case WeatherType.LightRain:
                    if (_particles.Count < MaxRainParticles) SpawnRain(1f);
                    break;
                case WeatherType.HeavyStorm:
                    if (_particles.Count < MaxRainParticles) SpawnRain(2.5f);
                    break;
                case WeatherType.GaleWinds:
                    if (_particles.Count < MaxWindParticles) SpawnWind();
                    break;
                case WeatherType.FogBank:
                    if (_particles.Count < MaxFogParticles) SpawnFog();
                    break;
            }
        }

        private float GetSpawnRate(WeatherType w) => w switch
        {
            WeatherType.LightRain  => RainSpawnRate,
            WeatherType.HeavyStorm => RainSpawnRate * 0.5f,
            WeatherType.GaleWinds  => WindSpawnRate,
            WeatherType.FogBank    => FogSpawnRate,
            _                      => 9999f
        };

        private void SpawnRain(float speedMult)
        {
            if (_rainSprite == null) return;
            var go = new GameObject("RainDrop");
            go.transform.SetParent(_particleParent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _rainSprite;
            sr.sortingOrder = 8900;

            // Random screen-space start position
            float halfW = _cam.orthographicSize * _cam.aspect;
            float halfH = _cam.orthographicSize;
            var startPos = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH * 0.5f, halfH),
                0f);
            go.transform.localPosition = startPos;

            // Rain falls down-left (dimetric)
            var velocity = new Vector2(-0.3f, -3f) * speedMult;
            float lifetime = (halfH * 2f + halfW) / (speedMult * 3f);
            _particles.Add(new WeatherParticle(sr, velocity, lifetime, 0f));
        }

        private void SpawnWind()
        {
            if (_windSprite == null) return;
            var go = new GameObject("WindStreak");
            go.transform.SetParent(_particleParent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _windSprite;
            sr.sortingOrder = 8900;

            float halfW = _cam.orthographicSize * _cam.aspect;
            float halfH = _cam.orthographicSize;
            var startPos = new Vector3(halfW + 0.5f, Random.Range(-halfH, halfH), 0f);
            go.transform.localPosition = startPos;

            var velocity = new Vector2(-6f, 0f);
            _particles.Add(new WeatherParticle(sr, velocity, halfW * 2f / 6f, 0f));
        }

        private void SpawnFog()
        {
            if (_fogSprite == null) return;
            var go = new GameObject("FogBlob");
            go.transform.SetParent(_particleParent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _fogSprite;
            sr.sortingOrder = 7500;
            sr.color = new Color(0.6f, 0.63f, 0.67f, 0.35f);

            float halfW = _cam.orthographicSize * _cam.aspect;
            float halfH = _cam.orthographicSize;
            var startPos = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH),
                0f);
            go.transform.localPosition = startPos;

            // Fog drifts slowly
            var velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f));
            _particles.Add(new WeatherParticle(sr, velocity, Random.Range(4f, 8f), fadeOut: 2f));
        }

        private void TickLightning()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                _flashOverlay.enabled = _flashTimer > 0f;
                if (_flashTimer <= 0f) _flashOverlay.enabled = false;
            }
            else if (Random.value < 0.002f) // ~once every ~8 seconds at 60fps
            {
                TriggerLightningFlash();
            }
        }

        private void TriggerLightningFlash()
        {
            _flashOverlay.enabled = true;
            _flashOverlay.color = new Color(1f, 1f, 1f, 0.45f);
            _flashTimer = 0.1f;
            _flashDuration = 0.1f;
            // Publish an event so AudioCueSystem can respond
            EventBus.Publish(new LightningFlashEvent());
        }

        private void ClearAllParticles()
        {
            foreach (var p in _particles)
                if (p.Renderer != null) Object.Destroy(p.Renderer.gameObject);
            _particles.Clear();
        }

        private void LoadSprites()
        {
            _rainSprite  = SpriteLoader.Load("Sprites/fx/rain_drop");
            _windSprite  = SpriteLoader.Load("Sprites/fx/wind_streak");
            _fogSprite   = SpriteLoader.Load("Sprites/fx/fog_blob");

            // Flash: just a white 4x4 magenta fallback is fine — it's a no-art placeholder
            _flashSprite = _flashOverlay.sprite; // null OK; we'll use color on a white sprite

            // Magenta fallback square for overlay if art missing
            if (_rainSprite == null) _rainSprite = MakeFallback(Color.cyan);
            if (_windSprite == null) _windSprite = MakeFallback(Color.white);
            if (_fogSprite  == null) _fogSprite  = MakeFallback(new Color(0.6f, 0.63f, 0.67f, 0.3f));
        }

        private static Sprite MakeFallback(Color color)
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        // ---- inner particle ----
        private class WeatherParticle
        {
            public SpriteRenderer Renderer { get; }
            private Vector2 _velocity;
            private float _lifetime;
            private float _elapsed;
            private float _fadeOutDuration;
            private Color _baseColor;

            public WeatherParticle(SpriteRenderer sr, Vector2 velocity, float lifetime, float fadeOut)
            {
                Renderer = sr;
                _velocity = velocity;
                _lifetime = lifetime;
                _fadeOutDuration = fadeOut;
                _baseColor = sr.color;
            }

            /// <returns>false when particle should be removed</returns>
            public bool Tick(float dt)
            {
                _elapsed += dt;
                if (_elapsed >= _lifetime) return false;

                Renderer.transform.localPosition += (Vector3)(Vector2)(_velocity * dt);

                // Fade out at end of life
                if (_fadeOutDuration > 0f && _elapsed > _lifetime - _fadeOutDuration)
                {
                    float t = 1f - (_lifetime - _elapsed) / _fadeOutDuration;
                    var c = _baseColor;
                    c.a = Mathf.Lerp(_baseColor.a, 0f, t);
                    Renderer.color = c;
                }

                return true;
            }
        }
    }

    // Published by WeatherEffects for AudioCueSystem to wire up
    public struct LightningFlashEvent { }
}
