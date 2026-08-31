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
        // Tuned for the KayKit proportions: slightly lower and closer than the
        // primitive-rig framing so characters fill more of the screen.
        [SerializeField] private Vector3 offset = new(0f, 8.2f, -6.9f);
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private float lookAhead = 1.2f;

        private float _shakeAmp, _shakeTime, _yaw;
        private float _pitch = -1f; // seeded from the authored offset on first frame
        private Vector3 _vel;
        private Transform _cineFocus;
        private float _cineT, _cineDur;

        public bool Cinematic => _cineFocus != null && _cineT < _cineDur;

        public void SetTarget(Transform t) => target = t;

        public void Shake(float amp, float duration = 0.25f)
        {
            _shakeAmp = amp;
            _shakeTime = duration;
        }

        /// <summary>Boss-intro sweep: orbit from the side to a low front shot.</summary>
        public void PlayCinematic(Transform focus, float duration)
        {
            _cineFocus = focus;
            _cineT = 0f;
            _cineDur = duration;
        }

        public void StopCinematic() => _cineFocus = null;

        private void LateUpdate()
        {
            if (Cinematic)
            {
                Core.EmberInput.ConsumeCamYaw(); // discard drags during cutscenes
                Core.EmberInput.ConsumeCamPitch();
                _cineT += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_cineT / _cineDur));
                var angle = Mathf.Lerp(120f, 25f, t) * Mathf.Deg2Rad;
                var radius = Mathf.Lerp(5.5f, 3.6f, t);
                var height = Mathf.Lerp(2.6f, 1.25f, t);
                var fwd = _cineFocus.forward;
                var right = _cineFocus.right;
                var pos = _cineFocus.position
                          + (fwd * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * radius
                          + Vector3.up * height;
                transform.position = Vector3.Lerp(transform.position, pos, 1f - Mathf.Exp(-10f * Time.deltaTime));
                transform.LookAt(_cineFocus.position + Vector3.up * 1.35f);
                return;
            }

            if (target == null) return;
            // Player-controlled orbit: horizontal drag = yaw, vertical drag =
            // pitch (drag up drops the camera to see farther up the road).
            // Movement stays camera-relative.
            if (_pitch < 0f) _pitch = Mathf.Atan2(offset.y, -offset.z) * Mathf.Rad2Deg;
            _yaw += Core.EmberInput.ConsumeCamYaw();
            _pitch = Mathf.Clamp(_pitch + Core.EmberInput.ConsumeCamPitch(), 18f, 70f);
            var dist = offset.magnitude;
            var back = Quaternion.Euler(0, _yaw, 0) * Vector3.back;
            var basePos = target.position
                          + back * (dist * Mathf.Cos(_pitch * Mathf.Deg2Rad))
                          + Vector3.up * (dist * Mathf.Sin(_pitch * Mathf.Deg2Rad))
                          + target.forward * lookAhead;
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
