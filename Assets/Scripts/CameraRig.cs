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

        /// <summary>
        /// Close third-person framing presets, kept so the three can be compared
        /// on device without a rebuild. `cam_preset` in PlayerPrefs overrides the
        /// serialized choice (0/1/2), which is how the screenshot pass and the
        /// A33 build switch between them.
        /// </summary>
        public enum Framing { Re4 = 0, Closer = 1, VeryClose = 2 }

        [Header("Framing preset")]
        // B ships: it is the only preset that satisfies every stated constraint
        // at once — 45% of screen height, 2.50 m camera, 4.18 m out, 10.3° tilt,
        // FOV 55. C is a touch more intimate but drops the camera just under the
        // authored height floor. Switch with PlayerPrefs "cam_preset" (0/1/2).
        [SerializeField] private Framing preset = Framing.Closer;

        // The placement is an explicit over-the-shoulder offset in the camera's
        // yaw frame — shoulder / height / distance behind — rather than the arc
        // this rig used to swing through. The old (0, 8.2, -6.9) sat 50° above
        // the fight and read as a tactical drone; this sits behind Renzo's
        // shoulder at roughly head height and looks along his eyeline.
        [Header("Placement (metres, in the camera's yaw frame)")]
        [SerializeField] private float shoulder = 0.55f;
        [SerializeField] private float camHeight = 2.8f;
        [SerializeField] private float backDistance = 3.8f;
        [Tooltip("Height on the player the camera looks at — upper chest, not the feet.")]
        [SerializeField] private float lookHeight = 1.45f;
        [Tooltip("How far the aim point leads the player's facing.")]
        [SerializeField] private float lookAhead = 0.55f;
        [Tooltip("Lifts the aim above the shoulders, which drops Renzo into the "
                 + "lower-middle of frame and gives the space above him to the enemy.")]
        [SerializeField] private float aimRise = 0.14f;

        [Header("Follow")]
        [SerializeField] private float followLerp = 12f;
        [SerializeField] private float yawLerp = 10f;
        [Tooltip("How fast the pitch trim springs back to the authored framing. "
                 + "Gyro feeds a rotation *rate*, so without this any bias walks "
                 + "the tilt away permanently and never returns.")]
        [SerializeField] private float pitchRecenter = 0.9f;

        [Header("Combat framing")]
        [Tooltip("A close camera stays close: a crowd buys centimetres, not metres.")]
        [SerializeField] private float crowdPullback = 0.15f;
        [SerializeField] private float maxPullback = 1.0f;
        [Tooltip("Extra distance for a boss — cinematic, still close.")]
        [SerializeField] private float bossPullback = 0.22f;
        [Tooltip("How far the aim point slides toward a locked enemy. Renzo stays the anchor.")]
        [Range(0f, 0.5f)][SerializeField] private float enemyLookBlend = 0.25f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.26f;
        [Tooltip("Closest the camera may be pulled before it gives up and clips.")]
        [SerializeField] private float minDistance = 2.6f;

        [Header("FOV — the only place it is written")]
        [SerializeField] private float baseFov = 56f;
        [Tooltip("Hard floor: no combat effect zooms tighter than this.")]
        [SerializeField] private float minFov = 54f;
        [Tooltip("Hard ceiling: no effect zooms wider than this.")]
        [SerializeField] private float maxFov = 60f;
        [Tooltip("Degrees of pull-in at full impact strength.")]
        [SerializeField] private float maxImpactDegrees = 2.5f;

        private Transform _execFocus;
        private float _execT, _execDur, _zoom, _distanceBoost;
        private UnityEngine.Camera _cam;

        private float _shakeAmp, _shakeTime, _shakeDur, _yaw;
        // Vertical drag is a small trim on the authored shoulder height, not a
        // full arc — the framing can no longer become the overhead shot.
        private float _pitchTrim;
        private float _diag;
        private float _occlude = 1f;                       // 0..1 of the wanted distance
        private readonly RaycastHit[] _hits = new RaycastHit[8];
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

        public void SetTarget(Transform t)
        {
            target = t;
            if (t != null) SnapBehindTarget();
        }

        private void Awake()
        {
            if (PlayerPrefs.HasKey("cam_preset"))
                preset = (Framing)Mathf.Clamp(PlayerPrefs.GetInt("cam_preset", 0), 0, 2);
            ApplyPreset(preset);
        }

        /// <summary>
        /// The three close-camera candidates. They differ only in placement and
        /// FOV — never in character scale, which stays physically correct.
        /// </summary>
        public void ApplyPreset(Framing f)
        {
            preset = f;
            // The brief's two constraints pull against each other: a 2.8 m camera
            // 3.8 m back, aimed at the chest, tilts ~20° down, not the 8-15° it
            // asks for. The tilt is the thing that made the old rig read as a
            // drone, so it wins: the aim point sits at the shoulders (1.55 m) and
            // the camera drops to the low end of its band, which lands the tilt
            // at ~12° while keeping the authored distance and shoulder offset.
            lookHeight = 1.55f;
            switch (f)
            {
                case Framing.Closer:
                    shoulder = 0.60f; camHeight = 2.50f; backDistance = 3.3f; baseFov = 55f; break;
                case Framing.VeryClose:
                    shoulder = 0.65f; camHeight = 2.45f; backDistance = 3.0f; baseFov = 54f; break;
                default:
                    shoulder = 0.55f; camHeight = 2.55f; backDistance = 3.8f; baseFov = 56f; break;
            }
            minFov = baseFov - 2f;
            maxFov = baseFov + 4f;
            if (_cam != null) _cam.fieldOfView = baseFov;
        }

        /// <summary>Metres from the player to the camera at rest, for the framing report.</summary>
        public float RestDistance => new Vector2(camHeight - lookHeight, backDistance).magnitude;

        /// <summary>Degrees the camera tilts down to meet the look target, at rest.</summary>
        public float RestPitchDegrees =>
            Mathf.Atan2(camHeight - lookHeight, backDistance) * Mathf.Rad2Deg;

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
        private float _impactDur, _impactMaxDur;

        /// <summary>
        /// The one way anything asks for a combat zoom. Attacks, parries and
        /// executions <b>request</b> an impact; the rig decides how much FOV
        /// actually moves and for how long. The strongest live request wins —
        /// impacts never stack or multiply, so Heavy + Light + Light reads as a
        /// Heavy, not as three zooms compounding. A weaker request may only
        /// extend the tail slightly, never deepen it.
        /// </summary>
        public void RequestCameraImpact(float strength, float duration = 0.3f)
        {
            strength = Mathf.Clamp01(strength);
            if (strength <= 0f) return;
            if (strength >= _zoom)
            {
                _zoom = strength;
                _impactDur = _impactMaxDur = Mathf.Max(0.05f, duration);
            }
            else if (_impactDur > 0f)
            {
                // Weaker: nudge the duration out a touch, leave the depth alone.
                _impactDur = Mathf.Max(_impactDur, Mathf.Min(duration, _impactMaxDur));
            }
        }

        /// <summary>Back-compat: the old name routes to the one API.</summary>
        public void ImpactZoom(float strength = 1f) => RequestCameraImpact(strength);

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
            // Decay on a timer, not a per-frame subtraction, so the impact always
            // returns cleanly to base at a rate independent of frame rate.
            if (_impactDur > 0f)
            {
                _impactDur -= Time.unscaledDeltaTime;
                if (_impactDur <= 0f) { _impactDur = 0f; _zoom = 0f; }
                else _zoom *= Mathf.Exp(-4f * Time.unscaledDeltaTime);
            }
            else _zoom = 0f;
            // FOV is BASE minus the current impact — additive from base every
            // frame, never multiplied by the last frame's value, so it cannot
            // accumulate. Then hard-clamped so nothing leaves the safe band.
            var target = Mathf.Clamp(baseFov - _zoom * maxImpactDegrees, minFov, maxFov);
            _cam.fieldOfView = Mathf.Clamp(
                Mathf.Lerp(_cam.fieldOfView, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime)),
                minFov, maxFov);
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
            var dt = Time.deltaTime;

            // Player-controlled orbit. Horizontal drag is free yaw; vertical drag
            // is a small trim about the look target, clamped so the camera cannot
            // climb back into the overhead framing this replaced.
            _yaw += Core.EmberInput.ConsumeCamYaw();

            // Pitch trim springs back to zero. The gyro reports a rotation rate,
            // which this integrates, so any sensor bias would otherwise walk the
            // tilt off the authored framing and stay there — measured drifting
            // from 12 degrees to 4 on the A33 with gyro on. Holding a tilt still
            // works: live input outruns the spring; releasing it recomposes the
            // shot over about a second.
            _pitchTrim = Mathf.Clamp(_pitchTrim + Core.EmberInput.ConsumeCamPitch(), -8f, 22f);
            _pitchTrim = Mathf.Lerp(_pitchTrim, 0f, 1f - Mathf.Exp(-pitchRecenter * dt));

            // A close camera has to stay close. A pack buys centimetres, not the
            // several metres the old rig gave away — the identity of the shot is
            // worth more than fitting every body in frame.
            var crowd = 0;
            for (var i = 0; i < Enemies.EnemyBrain.Active.Count; i++)
            {
                var e = Enemies.EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                if (Vector3.Distance(e.transform.position, target.position) < 9f) crowd++;
            }
            _distanceBoost = Mathf.Lerp(_distanceBoost,
                Mathf.Min(maxPullback, crowd * crowdPullback), 1f - Mathf.Exp(-3f * dt));

            // The aim point is the player's upper chest, led slightly by facing.
            // Under lock it slides a fraction toward the enemy: enough to keep the
            // opponent framed, not so much that Renzo stops being the anchor.
            var pivot = target.position + Vector3.up * lookHeight;
            var aim = pivot + target.forward * lookAhead + Vector3.up * aimRise;
            if (LockFocus != null)
            {
                var to = LockFocus.position - target.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.04f)
                {
                    var wanted = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    _yaw = Mathf.LerpAngle(_yaw, wanted, 1f - Mathf.Exp(-yawLerp * dt));
                }
                aim = Vector3.Lerp(pivot, LockFocus.position + Vector3.up * lookHeight,
                    enemyLookBlend) + Vector3.up * aimRise;
            }

            var want = Placement(pivot);

            // Obstruction is solved by moving *in*, never out. The pull-in is
            // immediate so geometry never clips; the return eases so a doorway
            // does not slingshot the shot.
            var wantDist = Vector3.Distance(pivot, want);
            var clear = ClearFraction(pivot, want, wantDist);
            _occlude = clear < _occlude
                ? clear
                : Mathf.Lerp(_occlude, clear, 1f - Mathf.Exp(-4f * dt));
            if (_occlude < 0.999f)
            {
                var dir = (want - pivot) / Mathf.Max(0.0001f, wantDist);
                want = pivot + dir * Mathf.Max(minDistance, wantDist * _occlude);
            }

            transform.position = Vector3.Lerp(transform.position, want,
                1f - Mathf.Exp(-followLerp * dt));

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                // Ramp down over the shake's life so it settles instead of cutting.
                // Amplitude is halved against the old rig: the camera is less than
                // half the distance away, so the same shake reads twice as hard.
                var falloff = _shakeDur > 0f ? Mathf.Clamp01(_shakeTime / _shakeDur) : 0f;
                transform.position += (Vector3)(Random.insideUnitCircle
                                                * (_shakeAmp * 0.025f * falloff));
                if (_shakeTime <= 0f) _shakeAmp = 0f;
            }

            transform.LookAt(aim);

#if EMBER_CAMDIAG
            _diag -= dt;
            if (_diag <= 0f)
            {
                _diag = 1f;
                Debug.Log($"[CamDiag] target={target.name} camDist={Vector3.Distance(transform.position, target.position):F2} " +
                          $"camY={transform.position.y:F2} tilt={transform.eulerAngles.x:F1} " +
                          $"lock={(LockFocus != null ? LockFocus.name : "none")} boss={BossFraming} " +
                          $"occl={_occlude:F2} exec={(_execFocus != null)} cine={Cinematic} shot={ScriptedShot}");
            }
#endif
        }

        /// <summary>
        /// Where the camera wants to sit: an explicit over-the-shoulder offset in
        /// the yaw frame, trimmed about the pivot. Pullback only ever moves the
        /// camera back, never up, so the low angle survives a crowd and a boss.
        /// </summary>
        private Vector3 Placement(Vector3 pivot)
        {
            var yawRot = Quaternion.Euler(0f, _yaw, 0f);
            var back = backDistance + _distanceBoost + (BossFraming ? bossPullback : 0f);
            var local = new Vector3(shoulder, camHeight - lookHeight, -back);
            return pivot + Quaternion.AngleAxis(_pitchTrim, yawRot * Vector3.right) * (yawRot * local);
        }

        /// <summary>
        /// Place the camera at its rest framing immediately, with no smoothing.
        /// Called when the rig acquires a target so the first frame of a scene is
        /// already composed instead of easing in from wherever the camera was
        /// authored. Also what the framing test drives, since edit mode has no
        /// delta time for the smoothed path to consume.
        /// </summary>
        public void SnapBehindTarget()
        {
            if (target == null) return;
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            _yaw = target.eulerAngles.y;
            _distanceBoost = 0f;
            _occlude = 1f;
            var pivot = target.position + Vector3.up * lookHeight;
            var want = Placement(pivot);
            var d = Vector3.Distance(pivot, want);
            var clear = ClearFraction(pivot, want, d);
            if (clear < 0.999f)
                want = pivot + (want - pivot).normalized * Mathf.Max(minDistance, d * clear);
            transform.position = want;
            transform.LookAt(pivot + target.forward * lookAhead + Vector3.up * aimRise);
            if (_cam != null) _cam.fieldOfView = baseFov;
        }

        /// <summary>
        /// Fraction of the wanted distance the camera may occupy without putting
        /// geometry between it and the player. Allocation-free: one non-alloc
        /// spherecast into a reused buffer. The player's own controller is
        /// skipped — enemies carry no colliders at all, so nothing else here is
        /// a body.
        /// </summary>
        private float ClearFraction(Vector3 pivot, Vector3 want, float wantDist)
        {
            if (wantDist < 0.05f) return 1f;
            var dir = (want - pivot) / wantDist;
            var n = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir, _hits,
                wantDist, ~0, QueryTriggerInteraction.Ignore);
            var nearest = wantDist;
            for (var i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider == null || h.distance <= 0.01f) continue;
                if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
                if (h.distance < nearest) nearest = h.distance;
            }
            return Mathf.Clamp01(nearest / wantDist);
        }
    }
}