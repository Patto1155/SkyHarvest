// UnityEngine stub — Audio: AudioClip, AudioSource, AudioListener, AudioMixer
using System;

namespace UnityEngine
{
    // -------------------------------------------------------------------------
    // AudioClip
    // -------------------------------------------------------------------------
    public partial class AudioClip : Object
    {
        public int samples { get; private set; }
        public int channels { get; private set; } = 1;
        public int frequency { get; private set; } = 44100;
        public float length => samples > 0 ? (float)samples / frequency : 0f;
        public bool loadInBackground { get; set; }
        public AudioDataLoadState loadState { get; private set; } = AudioDataLoadState.Loaded;
        public bool preloadAudioData { get; set; } = true;
        public bool ambisonic { get; set; }

        public delegate void PCMReaderCallback(float[] data);
        public delegate void PCMSetPositionCallback(int position);

        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency,
            bool stream, PCMReaderCallback? pcmReadCallback = null, PCMSetPositionCallback? pcmSetPositionCallback = null)
        {
            return new AudioClip
            {
                name = name,
                samples = lengthSamples,
                channels = channels,
                frequency = frequency,
                loadState = AudioDataLoadState.Loaded
            };
        }

        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
            => Create(name, lengthSamples, channels, frequency, stream, null, null);

        public bool GetData(float[] data, int offsetSamples) { Array.Clear(data, 0, data.Length); return true; }
        public bool SetData(float[] data, int offsetSamples) => true;
        public bool LoadAudioData() => true;
        public bool UnloadAudioData() => true;
    }

    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }
    public enum AudioClipLoadType { DecompressOnLoad, CompressedInMemory, Streaming }
    public enum AudioSpeakerMode { Mono, Stereo, Quad, Surround, Mode5point1, Mode7point1, Prologic }

    // -------------------------------------------------------------------------
    // AudioSource
    // -------------------------------------------------------------------------
    public partial class AudioSource : Behaviour
    {
        public AudioClip? clip { get; set; }
        public float volume { get; set; } = 1f;
        public float pitch { get; set; } = 1f;
        public bool loop { get; set; }
        public bool mute { get; set; }
        public bool playOnAwake { get; set; } = true;
        public bool isPlaying { get; private set; }
        public float time { get; set; }
        public int timeSamples { get; set; }
        public float panStereo { get; set; }
        public float spatialBlend { get; set; }
        public float dopplerLevel { get; set; } = 1f;
        public float minDistance { get; set; } = 1f;
        public float maxDistance { get; set; } = 500f;
        public AudioRolloffMode rolloffMode { get; set; } = AudioRolloffMode.Logarithmic;
        public float reverbZoneMix { get; set; } = 1f;
        public bool bypassEffects { get; set; }
        public bool bypassListenerEffects { get; set; }
        public bool bypassReverbZones { get; set; }

        public void Play(ulong delay = 0) { isPlaying = true; }
        public void PlayOneShot(AudioClip? clip, float volumeScale = 1f) { }
        public void PlayDelayed(float delay) { isPlaying = true; }
        public void Stop() { isPlaying = false; }
        public void Pause() { isPlaying = false; }
        public void UnPause() { isPlaying = true; }
        public void PlayClipAtPoint(AudioClip? clip, Vector3 position, float volume = 1f) { }
        public static void PlayClipAtPoint_Static(AudioClip? clip, Vector3 position, float volume = 1f) { }
        public void SetScheduledStartTime(double time) { }
        public void SetScheduledEndTime(double time) { }
    }

    public enum AudioRolloffMode { Logarithmic, Linear, Custom }

    // -------------------------------------------------------------------------
    // AudioListener
    // -------------------------------------------------------------------------
    public partial class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
    }

    // -------------------------------------------------------------------------
    // AudioMixer (minimal, for games that reference it)
    // -------------------------------------------------------------------------
}

namespace UnityEngine.Audio
{
    public partial class AudioMixer : UnityEngine.Object
    {
        public bool SetFloat(string name, float value) => true;
        public bool GetFloat(string name, out float value) { value = 0f; return true; }
        public bool ClearFloat(string name) => true;
        public AudioMixerGroup? FindMatchingGroups(string subPath) => null;
        public void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach) { }
    }

    public partial class AudioMixerGroup : UnityEngine.Object { }
    public partial class AudioMixerSnapshot : UnityEngine.Object
    {
        public void TransitionTo(float timeToReach) { }
    }
}
