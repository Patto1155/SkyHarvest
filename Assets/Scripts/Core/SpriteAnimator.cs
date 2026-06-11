using UnityEngine;

namespace SkyHarvest.Core
{
    public class SpriteAnimator : MonoBehaviour
    {
        public Sprite[] Frames = System.Array.Empty<Sprite>();
        public float Fps = 6f;
        public bool Loop = true;
        public bool Playing = true;

        private SpriteRenderer? _sr;
        private float _elapsed;
        private int _currentFrame;

        private void Awake() => _sr = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (!Playing || Frames == null || Frames.Length < 2 || _sr == null) return;

            _elapsed += Time.deltaTime;
            float frameDuration = Fps > 0f ? 1f / Fps : 0.1667f;

            if (_elapsed >= frameDuration)
            {
                _elapsed -= frameDuration;
                _currentFrame++;
                if (_currentFrame >= Frames.Length)
                {
                    _currentFrame = Loop ? 0 : Frames.Length - 1;
                    if (!Loop) Playing = false;
                }
                if (Frames[_currentFrame] != null)
                    _sr.sprite = Frames[_currentFrame];
            }
        }

        public void SetFrames(Sprite[] frames, bool restart = true)
        {
            Frames = frames;
            if (restart) { _currentFrame = 0; _elapsed = 0f; }
            if (_sr != null && Frames.Length > 0 && Frames[0] != null)
                _sr.sprite = Frames[0];
        }

        public void SetFrame(int index)
        {
            if (Frames == null || Frames.Length == 0) return;
            _currentFrame = Mathf.Clamp(index, 0, Frames.Length - 1);
            Playing = false;
            if (_sr != null && Frames[_currentFrame] != null)
                _sr.sprite = Frames[_currentFrame];
        }
    }
}
