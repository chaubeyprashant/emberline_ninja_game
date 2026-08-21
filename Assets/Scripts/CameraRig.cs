using UnityEngine;

namespace Emberline
{
    /// <summary>
    /// Third-person follow camera: fixed anime-action angle, smooth follow,
    /// light screen-shake hook. Attach to the Camera; assign the player.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 9f, -7.5f);
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private float lookAhead = 1.2f;

        private float _shakeAmp, _shakeTime;
        private Vector3 _vel;

        public void SetTarget(Transform t) => target = t;

        public void Shake(float amp, float duration = 0.25f)
        {
            _shakeAmp = amp;
            _shakeTime = duration;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var basePos = target.position + offset + target.forward * lookAhead;
            transform.position = Vector3.Lerp(
                transform.position, basePos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                transform.position += (Vector3)(Random.insideUnitCircle * (_shakeAmp * 0.05f));
            }

            transform.LookAt(target.position + Vector3.up * 1f);
        }
    }
}
