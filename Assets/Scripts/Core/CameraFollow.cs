using UnityEngine;

namespace SkyHarvest.Core
{
    /// <summary>
    /// Smooth follow + scroll-wheel zoom (spec §7: fixed rotation, zoom only).
    /// Zoom eases toward the target size so it feels calm, not steppy.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform? Target;
        public float SmoothSpeed = 8f;
        public Vector3 Offset = new Vector3(0f, 0f, -10f);

        public float MinZoom = 2f;
        public float MaxZoom = 6f;
        public float ZoomStep = 0.5f;       // ortho-size change per scroll notch
        public float ZoomSmoothSpeed = 10f;

        private Camera? _cam;
        private float _targetSize;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _targetSize = _cam != null ? _cam.orthographicSize : 2.5f;
        }

        private void LateUpdate()
        {
            HandleZoom();

            if (Target == null) return;
            var desired = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, desired,
                SmoothSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            if (_cam == null) return;

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
                _targetSize = Mathf.Clamp(_targetSize - scroll * ZoomStep, MinZoom, MaxZoom);

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize,
                ZoomSmoothSpeed * Time.deltaTime);
        }
    }
}
