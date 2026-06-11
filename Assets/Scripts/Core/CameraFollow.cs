using UnityEngine;

namespace SkyHarvest.Core
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform? Target;
        public float SmoothSpeed = 8f;
        public Vector3 Offset = new Vector3(0f, 4f, -10f);

        private void LateUpdate()
        {
            if (Target == null) return;
            var desired = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, desired,
                SmoothSpeed * Time.deltaTime);
        }
    }
}
