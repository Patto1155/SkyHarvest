// Ambient cues: fullscreen tinted overlay SpriteRenderer + Camera.backgroundColor lerp.
// Pre-telegraphs upcoming weather 1-2 minutes before transition via WeatherStateMachine.NextWeatherHint.
// Storm lightning = brief white overlay flashes (published to WeatherEffects).
using System;
using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Weather
{
    public class WeatherAmbientCues : MonoBehaviour
    {
        // ---- wired by Bootstrap ----
        private Camera _mainCamera;

        // Fullscreen overlay: a large SpriteRenderer above the world, driven by color tint
        private SpriteRenderer _overlayRenderer;

        // Target / current colors for smooth lerp
        private Color _targetOverlayColor;
        private Color _currentOverlayColor;
        private Color _targetBgColor;
        private Color _currentBgColor;

        // Per-weather ambient config
        private const float LerpSpeed = 0.4f;       // units/sec alpha convergence rate
        private const float PreTelegraphSpeed = 0.2f;

        private void Awake()
        {
            // Build fullscreen overlay quad
            var go = new GameObject("WeatherOverlay");
            _overlayRenderer = go.AddComponent<SpriteRenderer>();
            _overlayRenderer.sortingOrder = 7000;

            // Create a white 8x8 texture to use as the tint overlay
            var tex = new Texture2D(8, 8);
            var pixels = new Color[64];
            for (int i = 0; i < 64; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _overlayRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
            _overlayRenderer.color = new Color(0, 0, 0, 0);
            _overlayRenderer.drawMode = SpriteDrawMode.Sliced;
            _overlayRenderer.size = new Vector2(40f, 25f); // large enough to cover any view

            _currentOverlayColor = new Color(0, 0, 0, 0);
            _targetOverlayColor  = new Color(0, 0, 0, 0);
            _targetBgColor  = GetBgColor(WeatherType.ClearSkies);
            _currentBgColor = _targetBgColor;
        }

        private void OnEnable()  => EventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
        private void OnDisable() => EventBus.Unsubscribe<WeatherChangedEvent>(OnWeatherChanged);

        public void SetCamera(Camera cam)
        {
            _mainCamera = cam;
            if (_mainCamera != null)
                _mainCamera.backgroundColor = GetBgColor(WeatherType.ClearSkies);
        }

        private void OnWeatherChanged(WeatherChangedEvent e)
        {
            // Full transition: set target immediately
            SetTargetsForWeather(e.Current);
        }

        private void Update()
        {
            if (_mainCamera == null) return;

            // Check if WeatherManager has a hint (pre-telegraphing)
            var wm = WeatherManager.Instance;
            if (wm != null && wm.StateMachine.NextWeatherHint.HasValue)
            {
                // Slowly lerp toward the hinted weather tint
                var hintColor = GetOverlayColor(wm.StateMachine.NextWeatherHint.Value);
                _targetOverlayColor = Color.Lerp(_targetOverlayColor, hintColor, PreTelegraphSpeed * Time.deltaTime);
                var hintBg = GetBgColor(wm.StateMachine.NextWeatherHint.Value);
                _targetBgColor = Color.Lerp(_targetBgColor, hintBg, PreTelegraphSpeed * Time.deltaTime);
            }

            // Smooth lerp current → target
            _currentOverlayColor = Color.Lerp(_currentOverlayColor, _targetOverlayColor, LerpSpeed * Time.deltaTime);
            _currentBgColor      = Color.Lerp(_currentBgColor, _targetBgColor, LerpSpeed * Time.deltaTime);

            _overlayRenderer.color = _currentOverlayColor;

            // Follow camera so the overlay always covers the screen
            _overlayRenderer.transform.position = new Vector3(
                _mainCamera.transform.position.x,
                _mainCamera.transform.position.y,
                _mainCamera.transform.position.z + 1f);

            _mainCamera.backgroundColor = _currentBgColor;
        }

        private void SetTargetsForWeather(WeatherType w)
        {
            _targetOverlayColor = GetOverlayColor(w);
            _targetBgColor      = GetBgColor(w);
        }

        // Overlay tint (additive/subtractive mood)
        private static Color GetOverlayColor(WeatherType w) => w switch
        {
            WeatherType.ClearSkies => new Color(0f,    0f,    0f, 0f),     // no tint
            WeatherType.LightRain  => new Color(0.04f, 0.06f, 0.12f, 0.15f),
            WeatherType.HeavyStorm => new Color(0f,    0f,    0.05f, 0.45f),
            WeatherType.GaleWinds  => new Color(0.1f,  0.08f, 0.05f, 0.2f),
            WeatherType.FogBank    => new Color(0.6f,  0.63f, 0.67f, 0.35f),
            WeatherType.SkyDrought => new Color(0.25f, 0.15f, 0f,    0.15f),
            _                      => new Color(0f,    0f,    0f,    0f),
        };

        // Camera background (sky colour per palette in CONVENTIONS)
        private static Color GetBgColor(WeatherType w) => w switch
        {
            WeatherType.ClearSkies => HexColor("2a3040"),
            WeatherType.LightRain  => HexColor("232b38"),
            WeatherType.HeavyStorm => HexColor("1a1e28"),
            WeatherType.GaleWinds  => HexColor("2a2820"),
            WeatherType.FogBank    => HexColor("4a4e54"),
            WeatherType.SkyDrought => HexColor("3a3020"),
            _                      => HexColor("1d1a1d"),
        };

        private static Color HexColor(string hex)
        {
            if (hex.Length != 6) return Color.black;
            float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
