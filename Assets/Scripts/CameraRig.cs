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

        [Header("Cinematic framing")]
        [Tooltip("Extra distance per nearby enemy, so a crowd fits in frame.")]
        [SerializeField] private float crowdPullback = 0.55f;
        [SerializeField] private float maxPullback = 3.2f;
        [SerializeField] private float baseFov = 50f;

        private Transform _execFocus;
        private float _execT, _execDur, _zoom, _distanceBoost;
        private UnityEngine.Camera _cam;

        private float _shakeAmp, _shakeTime, _shakeDur, _yaw;
        private float _pitch = -1f; // seeded from the authored offset on first frame
        private Vector3 _vel;
        private Transform _cineFocus;
        private float _cineT, _cineDur;

        public bool Cinematic => _cineFocus != null && _cineT < _cineDur;

        /// <summary>
        /// Enemy the camera should frame with the player. Set by CombatController
        /// when a lock is active; null releases the camera back to free orbit.
        /// </summary>
        public Transform LockFocus { get; set; }

        /// <summary>Wider, lower framing so a boss and its tells both fit.</summary>
        public bool BossFraming { get; set; }

        public void SetTarget(Transform t) => target = t;

        /// <summary>
        /// Stacking rule mirrors the hit-stop: a stronger shake takes over, a
        /// weaker one only extends what's already running. Previously the last
        /// call won outright, so a light hit landing a frame after a boss slam
        /// flattened it.
        /// </summary>
        public void Shake(float amp, float duration = 0.25f)
        {
            if (amp >= _shakeAmp || _shakeTime <= 0f) _shakeAmp = amp;
            _shakeTime = Mathf.Max(_shakeTime, duration);
            _shakeDur = Mathf.Max(_shakeTime, duration);
        }

        /// <summary>Boss-intro sweep: orbit from the side to a low front shot.</summary>
        public void PlayCinematic(Transform focus, float duration)
        {
            _cineFocus = focus;
            _cineT = 0f;
            _cineDur = duration;
        }

        public void StopCinematic()
        {
            _cineFocus = null;
            _shotT = _shotDur = 0f;
        }

        /// <summary>
        /// A scripted move: ease from one placement to another while looking at a
        /// fixed point. Every cinematic shot type resolves to this one call, so the
        /// rig stays free of story concepts and there is a single code path to tune.
        /// `unsettled` adds a small drift for handheld.
        /// </summary>
        public void PlayScriptedShot(Vector3 from, Vector3 to, Vector3 look,
            float duration, bool unsettled = false)
        {
            _cineFocus = null;         // a scripted shot overrides the orbit sweep
            _shotFrom = from;
            _shotTo = to;
            _shotLook = look;
            _shotDur = Mathf.Max(0.05f, duration);
            _shotT = 0f;
            _shotShake = unsettled;
            transform.position = from;
        }

        private Vector3 _shotFrom, _shotTo, _shotLook;
        private float _shotT, _shotDur;
        private bool _shotShake;

        /// <summary>True while a scripted cinematic shot owns the camera.</summary>
        public bool ScriptedShot => _shotDur > 0f && _shotT < _shotDur;

        /// <summary>Punch the FOV in on impact — a weight cue that costs nothing.</summary>
        public void ImpactZoom(float strength = 1f) =>
            _zoom = Mathf.Max(_zoom, Mathf.Clamp01(strength));

        /// <summary>
        /// Frame a finisher: swing in close and low, keeping both bodies in shot.
        /// Distinct from the boss-intro sweep — shorter and tighter, and it does
        /// not orbit, so it reads as a cut rather than a flourish.
        /// </summary>
        public void PlayExecution(Transform victim, float duration = 0.9f)
        {
            _execFocus = victim;
            _execT = 0f;
            _execDur = duration;
        }

        /// <summary>FOV punch decays on unscaled time so hit-stop cannot freeze it.</summary>
        private void UpdateZoom()
        {
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            if (_cam == null) return;
            if (_zoom > 0f) _zoom = Mathf.Max(0f, _zoom - Time.unscaledDeltaTime * 3.2f);
            // Four degrees: felt, without reading as a lens artefact.
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, baseFov - _zoom * 4f,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }

        private void LateUpdate()
        {
            UpdateZoom();

            // Scripted cinematic shot wins over everything: it is the only mode
            // where an author, not the player, is choosing the framing.
            if (ScriptedShot)
            {
                _shotT += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_shotT / _shotDur));
                var pos = Vector3.Lerp(_shotFrom, _shotTo, k);
                if (_shotShake)
                {
                    // Breath, not shake: a slow figure-of-eight an order of
                    // magnitude below the combat shake.
                    var t = Time.time;
                    pos += new Vector3(Mathf.Sin(t * 0.9f), Mathf.Sin(t * 1.37f) * 0.6f, 0f) * 0.035f;
                }
                transform.position = pos;
                transform.LookAt(_shotLook);
                return;
            }

            if (_execFocus != null && _execT < _execDur)
            {
                _execT += Time.unscaledDeltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_execT / _execDur));
                var anchor = target != null ? target.position : _execFocus.position;
                var mid = Vector3.Lerp(anchor, _execFocus.position, 0.5f);
                var side = Vector3.Cross(Vector3.up,
                    (_execFocus.position - anchor).sqrMagnitude > 0.01f
                        ? (_execFocus.position - anchor).normalized : Vector3.forward);
                var pos = mid + side * Mathf.Lerp(3.4f, 2.1f, k)
                          + Vector3.up * Mathf.Lerp(2.4f, 1.35f, k);
                transform.position = Vector3.Lerp(transform.position, pos,
                    1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                transform.LookAt(mid + Vector3.up * 1.1f);
                return;
            }
            _execFocus = null;

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
            // Dynamic combat distance: ease back as the fight grows so a pack
            // stays in frame and the player is not fighting the camera as well.
            var crowd = 0;
            for (var i = 0; i < Enemies.EnemyBrain.Active.Count; i++)
            {
                var e = Enemies.EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                if (Vector3.Distance(e.transform.position, target.position) < 9f) crowd++;
            }
            _distanceBoost = Mathf.Lerp(_distanceBoost,
                Mathf.Min(maxPullback, crowd * crowdPullback), 1.2f * Time.deltaTime);
            var dist = offset.magnitude + _distanceBoost + (BossFraming ? 2.4f : 0f);

            // Target lock: swing behind the player on the player→enemy axis and
            // frame the pair. Eased rather than snapped, and it yields the moment
            // the player drags, so a lock never takes the camera away from them.
            var lockAim = target.position;
            if (LockFocus != null)
            {
                var to = LockFocus.position - target.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.04f)
                {
                    var wanted = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    _yaw = Mathf.LerpAngle(_yaw, wanted, 1f - Mathf.Exp(-4f * Time.deltaTime));
                    lockAim = Vector3.Lerp(target.position, LockFocus.position, 0.35f);
                }
                _pitch = Mathf.Lerp(_pitch, BossFraming ? 30f : 34f,
                    1f - Mathf.Exp(-2.5f * Time.deltaTime));
            }

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
                // Ramp down over the shake's life so it settles instead of cutting.
                var falloff = _shakeDur > 0f ? Mathf.Clamp01(_shakeTime / _shakeDur) : 0f;
                transform.position += (Vector3)(Random.insideUnitCircle
                                                * (_shakeAmp * 0.05f * falloff));
                if (_shakeTime <= 0f) _shakeAmp = 0f;
            }

            transform.LookAt(lockAim + Vector3.up * 1f);
        }
    }
}
