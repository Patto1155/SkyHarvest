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
            _ambient.volume = 0.3f;

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
            int sampleRate = 44100;
            int samples = sampleRate * 2;
            var clip = AudioClip.Create("ambient", samples, 1, sampleRate, false);
            var data = new float[samples];
            var rng  = new System.Random((int)weather);

            float baseHz = weather switch
            {
                WeatherType.LightRain   => 200f,
                WeatherType.HeavyStorm  => 120f,
                WeatherType.GaleWinds   => 160f,
                WeatherType.FogBank     => 60f,
                _                       => 80f
            };
            float vol = 0.08f;
            for (int i = 0; i < samples; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
                float tone  = Mathf.Sin(2f * Mathf.PI * baseHz * i / sampleRate) * 0.3f;
                data[i] = (noise + tone) * vol;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
