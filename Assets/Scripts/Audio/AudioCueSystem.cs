using UnityEngine;
using SkyHarvest.Core;

namespace SkyHarvest.Audio
{
    public class AudioCueSystem : MonoBehaviour
    {
        public static AudioCueSystem? Instance { get; private set; }

        private AudioSource? _sfx;
        private AudioSource? _ambient;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _sfx     = gameObject.AddComponent<AudioSource>();
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop   = true;
            _ambient.volume = 0.12f;

            EventBus.Subscribe<CropHarvestedEvent>(_  => PlayHarvest());
            EventBus.Subscribe<CropPlantedEvent>(_    => PlayPlant());
            EventBus.Subscribe<WorkshopCompletedEvent>(_ => PlayWorkshopDone());
            EventBus.Subscribe<WorkshopRuinedEvent>(_  => PlayRuin());
            EventBus.Subscribe<DebrisScavengedEvent>(_ => PlayScavenge());
            EventBus.Subscribe<StructurePlacedEvent>(_ => PlayBuild());
            EventBus.Subscribe<WeatherChangedEvent>(e  => OnWeatherChanged(e.Current));
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CropHarvestedEvent>(_  => PlayHarvest());
            EventBus.Unsubscribe<CropPlantedEvent>(_    => PlayPlant());
            EventBus.Unsubscribe<WorkshopCompletedEvent>(_ => PlayWorkshopDone());
            EventBus.Unsubscribe<WorkshopRuinedEvent>(_  => PlayRuin());
            EventBus.Unsubscribe<DebrisScavengedEvent>(_ => PlayScavenge());
            EventBus.Unsubscribe<StructurePlacedEvent>(_ => PlayBuild());
            EventBus.Unsubscribe<WeatherChangedEvent>(e  => OnWeatherChanged(e.Current));
        }

        private void PlayHarvest()      => PlayTone(660f, 0.06f, 0.15f);
        private void PlayPlant()        => PlayTone(330f, 0.05f, 0.08f);
        private void PlayWorkshopDone() => PlayTone(440f, 0.07f, 0.20f);
        private void PlayRuin()         => PlayTone(180f, 0.08f, 0.18f);
        private void PlayScavenge()     => PlayTone(550f, 0.06f, 0.12f);
        private void PlayBuild()        => PlayTone(370f, 0.07f, 0.10f);

        private void OnWeatherChanged(WeatherType weather)
        {
            if (_ambient == null) return;
            var clip = BuildAmbientClip(weather);
            _ambient.Stop();
            _ambient.clip = clip;
            _ambient.Play();
        }

        private void PlayTone(float hz, float volume, float durationSec)
        {
            if (_sfx == null) return;
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * durationSec);
            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / durationSec);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * volume * env;
            }
            clip.SetData(data, 0);
            _sfx.PlayOneShot(clip);
        }

        private AudioClip BuildAmbientClip(WeatherType weather)
        {
            // Soft procedural drones — no raw white noise (that read as loud static when looped).
            int sampleRate = 44100;
            int samples = sampleRate * 4;
            var clip = AudioClip.Create("ambient", samples, 1, sampleRate, false);
            var data = new float[samples];

            float baseHz = weather switch
            {
                WeatherType.LightRain   => 180f,
                WeatherType.HeavyStorm  => 90f,
                WeatherType.GaleWinds   => 130f,
                WeatherType.FogBank     => 55f,
                WeatherType.ClearSkies  => 70f,
                _                       => 65f
            };
            float vol = weather switch
            {
                WeatherType.ClearSkies => 0.015f,
                WeatherType.FogBank    => 0.02f,
                _                      => 0.03f
            };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * baseHz * t) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * baseHz * 1.5f * t) * 0.25f;
                float rain = weather == WeatherType.LightRain || weather == WeatherType.HeavyStorm
                    ? Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.04f * lfo
                    : 0f;
                data[i] = (tone + rain) * vol * lfo;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
