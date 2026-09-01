using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// Camera-relative movement + the Flicker Step dash (i-frames, after-image hook).
    /// Port of the prototype's movement feel to 3D. Extend with wall-run/air-dash
    /// custom states as traversal content comes online.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float busySpeedMultiplier = 0.35f;
        [SerializeField] private float rotationSpeedDeg = 720f;
        [SerializeField] private float gravity = -25f;

        [Header("Flicker Step")]
        [SerializeField] private float flickerSpeed = 16f;
        [SerializeField] private float flickerDuration = 0.28f;
        [SerializeField] private float flickerCooldown = 0.95f;
        [SerializeField] private float afterImageInterval = 0.055f;
        [SerializeField] private GameObject afterImagePrefab;

        [Header("Traversal")]
        [SerializeField] private float jumpSpeed = 9.5f;
        [SerializeField] private float coyoteTime = 0.12f;   // grace after walking off
        [SerializeField] private float jumpBuffer = 0.15f;   // grace before landing
        [SerializeField] private float vaultSpeed = 11.5f;   // clears a chimney lip
        [SerializeField] private float vaultLookahead = 1.3f;
        [SerializeField] private float vaultCarry = 5f;
        [SerializeField] private float wallRunSeconds = 1.1f;
        [SerializeField] private float wallRunSpeed = 7.5f;
        [SerializeField] private float wallCheckDist = 0.8f;

        public bool Invulnerable => _flickerTimer > 0f;
        public bool Busy { get; set; } // set by CombatController during swings
        public float FlickerCooldownRemaining => _flickerCd;

        /// <summary>Feet on something solid this frame (our own test, not the CC's stale flag).</summary>
        public bool Grounded { get; private set; }

        /// <summary>True while running along a wall — HUD/animation read this.</summary>
        public bool WallRunning => _wallT > 0f;

        /// <summary>Wall runs started this mission (feat tracking).</summary>
        public int WallRuns { get; private set; }

        /// <summary>
        /// Crouched: slower, quieter, and much harder to see. The whole stealth
        /// loop hangs off this one flag — enemies scale detection by visibility.
        /// </summary>
        public bool Crouched { get; private set; }

        private float _stride; // metres travelled since the last footstep

        [Header("Stealth")]
        [SerializeField] private float crouchSpeedMultiplier = 0.45f;

        /// <summary>Metres at which this movement can be heard.</summary>
        [SerializeField] private float runNoiseRadius = 9f;
        [SerializeField] private float walkNoiseRadius = 5f;
        [SerializeField] private float crouchNoiseRadius = 1.6f;
        [SerializeField] private float landNoiseRadius = 12f;

        private float _noiseTick;

        private float _slowT, _slowMul = 1f;

        /// <summary>
        /// Timed movement debuff (Kagachi's venom). Stacks by taking the harsher
        /// of the two so a second hit can't accidentally cure the first.
        /// </summary>
        public void ApplySlow(float duration, float multiplier)
        {
            _slowMul = _slowT > 0f ? Mathf.Min(_slowMul, multiplier) : multiplier;
            _slowT = Mathf.Max(_slowT, duration);
        }

        /// <summary>Air dashes allowed per airborne stretch. SKY STEP grants a second.</summary>
        private int AirFlickerLimit => Core.SkillTree.Has("air_flicker") ? 2 : 1;

        /// <summary>ROOFRUNNER stretches the wall-run window.</summary>
        private float WallRunSeconds =>
            wallRunSeconds * (Core.SkillTree.Has("wall_runner") ? 1.7f : 1f);

        /// <summary>Effective cooldown after the SECOND STEP skill.</summary>
        private float FlickerCooldown =>
            flickerCooldown * (Core.SkillTree.Has("flicker_haste") ? 0.65f : 1f);

        /// <summary>0 = ready, 1 = just used. For HUD cooldown rings.</summary>
        public float FlickerCd01 => FlickerCooldown > 0f ? _flickerCd / FlickerCooldown : 0f;
        public Vector3 Facing { get; private set; } = Vector3.forward;

        private CharacterController _cc;
        private Core.CharacterRig _rig;
        private Transform _cam;
        private float _flickerTimer, _flickerCd, _ghostTick, _yVel;
        private Vector3 _flickerDir;
        private Vector3 _impulse;

        // Traversal state.
        private float _coyoteT, _bufferT, _wallT, _wallCd;
        private int _airFlickersUsed;
        private Vector3 _wallNormal, _wallTangent;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rig = GetComponent<Core.CharacterRig>();
            _cam = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null;
        }

        private void Update()
        {
            if (Emberline.GameManager.CinematicActive)
            {
                if (_rig != null) _rig.move01 = 0f;
                return;
            }
            var dt = Time.deltaTime;
            _flickerCd = Mathf.Max(0, _flickerCd - dt);
            _wallCd = Mathf.Max(0, _wallCd - dt);
            if (_slowT > 0f && (_slowT -= dt) <= 0f) _slowMul = 1f;
            var input = ReadMoveInput();

            // Crouch only makes sense with your feet down and no swing committed.
            Crouched = Core.EmberInput.CrouchHeld && Grounded && !Busy && _flickerTimer <= 0f;

            var wasGrounded = Grounded;
            Grounded = _cc.isGrounded;
            if (Grounded)
            {
                _coyoteT = coyoteTime;
                _airFlickersUsed = 0;
                _wallT = 0f;
                // Landing: one cloth rustle plus a footstep, and reset the stride
                // so the next step does not fire immediately after touchdown.
                if (!wasGrounded)
                {
                    Core.Sfx3D.Footstep(transform.position, UI.Atmosphere.GroundIsWood, 0.6f);
                    Core.Sfx3D.Cloth(transform.position, 0.45f);
                    _stride = 0f;
                }
            }
            else _coyoteT = Mathf.Max(0f, _coyoteT - dt);
            UpdateFootsteps(input, dt);

            // Buffered jump: pressing just before touchdown still fires on landing.
            if (Core.EmberInput.ConsumeJump()) _bufferT = jumpBuffer;
            else _bufferT = Mathf.Max(0f, _bufferT - dt);

            // Flicker vaults low obstacles (chimney lips, cart beds).
            _cc.stepOffset = _flickerTimer > 0f ? 0.65f : 0.3f;

            UpdateWallRun(input, dt);
            TryJump(input);

            Vector3 velocity;
            if (_flickerTimer > 0f)
            {
                _flickerTimer -= dt;
                velocity = _flickerDir * flickerSpeed;
                _ghostTick -= dt;
                if (_ghostTick <= 0f)
                {
                    _ghostTick = afterImageInterval;
                    _rig?.SpawnAfterImage();
                }
            }
            else if (_wallT > 0f)
            {
                // Along the wall, with a light push into it so we stay attached.
                velocity = _wallTangent * wallRunSpeed - _wallNormal * 0.8f;
            }
            else
            {
                var speed = moveSpeed * (Busy ? busySpeedMultiplier : 1f);
                if (ArenaMarkers.InWater(transform.position)) speed *= 0.75f; // knee-deep marsh
                speed *= Enemies.SlowZone.SpeedMulAt(transform.position);      // Goro's slam scar
                if (_slowT > 0f) speed *= _slowMul;                             // venom
                if (Crouched) speed *= crouchSpeedMultiplier;

                // Footsteps are a stealth signal, not just audio. Emitted on a slow
                // tick rather than per frame — the noise ring only needs the
                // position, and a dozen events a second would be pure waste.
                if (input.sqrMagnitude > 0.05f && (_noiseTick -= dt) <= 0f)
                {
                    _noiseTick = Crouched ? 0.55f : 0.34f;
                    Enemies.NoiseSystem.Emit(transform.position,
                        Crouched ? crouchNoiseRadius
                            : input.magnitude > 0.75f ? runNoiseRadius : walkNoiseRadius);
                }
                velocity = input * speed;
                if (input.sqrMagnitude > 0.01f)
                {
                    Facing = input.normalized;
                    var target = Quaternion.LookRotation(Facing);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, target, rotationSpeedDeg * dt);
                }
            }

            if (_rig != null)
                _rig.move01 = _flickerTimer > 0f || _wallT > 0f ? 1f
                    : Mathf.Clamp01(input.magnitude) * (Busy ? 0.3f : 1f);

            velocity += _impulse;
            _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, 45f * dt);

            // Vertical: a flicker hangs (clean horizontal dash), a wall-run only
            // drifts, otherwise normal gravity. Grounded is our own flag so a jump
            // taken this frame isn't stomped back to the stick-to-ground value.
            if (_flickerTimer > 0f) _yVel = 0f;
            else if (_wallT > 0f) _yVel += gravity * 0.18f * dt;
            else if (Grounded && _yVel <= 0f) _yVel = -1f;
            else _yVel += gravity * dt;

            velocity.y = _yVel;
            var wasAirborne = !Grounded;
            _cc.Move(velocity * dt);
            // Hitting the deck after a fall carries further than any footstep.
            if (wasAirborne && _cc.isGrounded && _yVel < -6f)
                Enemies.NoiseSystem.Emit(transform.position, landNoiseRadius);
            ClampToPlayArea();
        }

        // ------------------------------------------------------------ traversal

        private void TryJump(Vector3 input)
        {
            if (_bufferT <= 0f) return;

            // Wall jump: kick off the surface, away and up.
            if (_wallT > 0f)
            {
                _bufferT = 0f;
                _wallT = 0f;
                _wallCd = 0.25f; // don't re-attach to the wall we just left
                _yVel = jumpSpeed;
                Grounded = false;
                _impulse += _wallNormal * 6f + _wallTangent * 2f;
                SetFacing(_wallNormal + _wallTangent);
                _rig?.PlayOneShot(Core.RigPose.Dash, 0.3f);
                Core.Sfx3D.DodgeBark();
                Core.Sfx3D.Cloth(transform.position);
                return;
            }

            if (!Grounded && _coyoteT <= 0f) return; // airborne past the coyote grace
            _bufferT = 0f;
            _coyoteT = 0f;
            Grounded = false;

            // Jumping into cover vaults it: detection uses the arena's obstacle
            // circles (chimneys, crates, rubble) so it agrees with the markers the
            // AI already steers around. Higher arc, plus carry across the top so
            // we don't stall on the lip.
            if (input.sqrMagnitude > 0.1f
                && ArenaMarkers.ObstacleAhead(transform.position, input.normalized, vaultLookahead))
            {
                var dir = input.normalized;
                _yVel = vaultSpeed;
                _impulse += dir * vaultCarry;
                SetFacing(dir);
                _rig?.PlayOneShot(Core.RigPose.Dash, 0.35f);
                Core.Sfx3D.DodgeBark();
                Core.Sfx3D.Cloth(transform.position);
                return;
            }

            _yVel = jumpSpeed;
            _rig?.PlayOneShot(Core.RigPose.Dash, 0.3f);
            Core.Sfx3D.DodgeBark();
            Core.Sfx3D.Cloth(transform.position);
        }

        /// <summary>
        /// MVP wall-run: airborne alongside a vertical surface (rooftop parapets,
        /// road walls) latches on for a short burst. Jump to kick off, or let the
        /// timer run out.
        /// </summary>
        /// <summary>
        /// Distance-based stride so footsteps track actual travel rather than a
        /// timer — crouch-walking and sprinting then sound right without tuning
        /// two separate cadences. Crouched steps are quieter and make less noise.
        /// </summary>
        private void UpdateFootsteps(Vector3 input, float dt)
        {
            if (!Grounded || _flickerTimer > 0f || input.sqrMagnitude < 0.02f)
            {
                _stride = Mathf.Max(0f, _stride - dt * 0.5f);
                return;
            }
            var speed = _cc.velocity;
            speed.y = 0f;
            _stride += speed.magnitude * dt;
            if (_stride < (Crouched ? 2.4f : 1.7f)) return;
            _stride = 0f;
            Core.Sfx3D.Footstep(transform.position, UI.Atmosphere.GroundIsWood,
                Crouched ? 0.18f : 0.45f);
        }

        private void UpdateWallRun(Vector3 input, float dt)
        {
            if (_wallT > 0f)
            {
                _wallT -= dt;
                if (_wallT <= 0f || Grounded || !ScanWall()) { _wallT = 0f; return; }
                RefreshTangent();
                SetFacing(_wallTangent);
                _ghostTick -= dt;
                if (_ghostTick <= 0f)
                {
                    _ghostTick = afterImageInterval * 2f;
                    _rig?.SpawnAfterImage();
                }
                return;
            }

            if (Grounded || _wallCd > 0f || _flickerTimer > 0f) return;
            if (input.sqrMagnitude < 0.1f || _yVel > 2f) return; // needs intent, past the apex
            if (!ScanWall()) return;
            RefreshTangent();
            if (Vector3.Dot(_wallTangent, input.normalized) < 0.25f) return; // must run along it
            _wallT = WallRunSeconds;
            WallRuns++;
            _yVel = Mathf.Max(_yVel, 1.5f); // small hop onto the wall
            _rig?.PlayOneShot(Core.RigPose.Dash, 0.3f);
        }

        /// <summary>Probe both sides for a vertical surface within arm's reach.</summary>
        private bool ScanWall()
        {
            var origin = transform.position + Vector3.up * 1.0f;
            for (var s = -1; s <= 1; s += 2)
            {
                if (!Physics.Raycast(origin, transform.right * s, out var hit, wallCheckDist))
                    continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.35f) continue; // floors and ramps aren't walls
                _wallNormal = hit.normal;
                return true;
            }
            return false;
        }

        /// <summary>Wall tangent pointing whichever way we were already heading.</summary>
        private void RefreshTangent()
        {
            var tangent = Vector3.Cross(_wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(tangent, Facing) < 0f) tangent = -tangent;
            _wallTangent = tangent;
        }

        /// <summary>
        /// Jumping clears the parapets that used to fence the player in, so the
        /// play area is enforced directly. XZ only — the height is the whole point.
        /// </summary>
        private void ClampToPlayArea()
        {
            var half = Core.SceneRefs.Game != null
                ? Core.SceneRefs.Game.arenaHalfExtents : new Vector2(13f, 8f);
            var p = transform.position;
            if (RoadNorth.Instance != null)
            {
                var xLimit = RoadNorth.XLimitAt(p.z, half.x);
                p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
                p.z = Mathf.Max(p.z, -half.y);
            }
            else
            {
                p.x = Mathf.Clamp(p.x, -half.x, half.x);
                p.z = Mathf.Clamp(p.z, -half.y, half.y);
            }
            if (p.y < -3f) { p.y = 0.5f; _yVel = 0f; } // safety net if we ever fall through
            if (p != transform.position) transform.position = p;
        }

        /// <summary>Snap facing (and body) toward a direction — used by soft-lock.</summary>
        public void SetFacing(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;
            Facing = dir.normalized;
            transform.rotation = Quaternion.LookRotation(Facing);
        }

        /// <summary>Short burst of velocity (attack lunge, knockback).</summary>
        public void Impulse(Vector3 v) => _impulse += v;

        /// <summary>
        /// Kunai warp: blink to a world point on the flicker's budget — same
        /// cooldown, same i-frames, shorter hang. The controller is toggled off
        /// for the move so its collision solver doesn't drag us back.
        /// </summary>
        public bool TryWarpTo(Vector3 pos)
        {
            if (_flickerCd > 0f) return false;
            // BLADE TETHER: warping is cheaper than a dodge, so a thrown kunai
            // becomes a repositioning tool rather than a trade.
            _flickerCd = FlickerCooldown * (Core.SkillTree.Has("warp_haste") ? 0.55f : 1f);
            _flickerTimer = flickerDuration * 0.6f;
            _rig?.SpawnAfterImage();
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _yVel = 0f;
            _impulse = Vector3.zero;
            _wallT = 0f;
            _ghostTick = 0f;
            _rig?.PlayOneShot(Core.RigPose.Dash, flickerDuration);
            return true;
        }

        /// <summary>
        /// Ember-step dodge, on the ground or in the air. Air flickers are limited
        /// to one per airborne stretch so a jump can be extended but not turned
        /// into free flight. Returns true if it fired.
        /// </summary>
        public bool TryFlicker()
        {
            if (_flickerCd > 0f) return false;
            var airborne = !Grounded && _coyoteT <= 0f;
            if (airborne && _airFlickersUsed >= AirFlickerLimit) return false;
            if (airborne) _airFlickersUsed++;
            _wallT = 0f; // flicking off a wall releases it
            _flickerCd = FlickerCooldown;
            _flickerTimer = flickerDuration;
            Core.Sfx3D.DodgeBark();
            Core.Sfx3D.Cloth(transform.position);
            _rig?.PlayOneShot(Core.RigPose.Dash, flickerDuration);
            var input = ReadMoveInput();
            _flickerDir = input.sqrMagnitude > 0.01f ? input.normalized : Facing;
            _ghostTick = 0f;
            return true;
        }

        private Vector3 ReadMoveInput()
        {
            // EmberInput merges the on-screen stick (device) and keyboard (editor).
            var move = Core.EmberInput.Move;
            var raw = new Vector3(move.x, 0, move.y);
            if (raw.sqrMagnitude < 0.01f) return Vector3.zero;
            if (raw.sqrMagnitude > 1f) raw.Normalize();
            if (_cam == null) return raw.normalized;
            var fwd = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized;
            return (fwd * raw.z + right * raw.x).normalized;
        }
    }
}
