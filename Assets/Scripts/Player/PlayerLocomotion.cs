using UnityEngine;

namespace Emberline.Player
{
    /// <summary>How the feet are moving. Drives animation, noise and stealth.</summary>
    public enum Gait { Idle, StealthWalk, Walk, Run, Sprint }

    /// <summary>
    /// Renzo's motor: camera-relative movement with real acceleration, four gaits,
    /// the Flicker Step dash, and traversal read off real colliders — vault,
    /// mantle, ledge grab and hang, wall-run, slide, landing roll, and swimming.
    ///
    /// <para>
    /// The old motor set velocity in one frame and vaulted whatever the arena's
    /// obstacle circles said was ahead. This one smooths velocity, reads the
    /// geometry through <see cref="TraversalProbe"/>, and exposes what it found as
    /// <see cref="Hint"/> so the HUD's action button can say VAULT or CLIMB before
    /// the press. Every exclusive move (mantle, vault, hang, slide, roll) is a
    /// <see cref="Move"/> with a timer; combat reads <see cref="Traversing"/> and
    /// refuses to swing through one.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("Gaits (m/s)")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float moveSpeed = 6.5f;      // run
        [SerializeField] private float sprintSpeed = 8.6f;
        [SerializeField] private float crouchSpeed = 2.9f;
        [SerializeField] private float swimSpeed = 2.4f;
        [SerializeField] private float busySpeedMultiplier = 0.35f;
        [SerializeField] private float rotationSpeedDeg = 720f;
        [SerializeField] private float gravity = -25f;

        [Header("Feel")]
        [Tooltip("Ground acceleration. 0 to full run in about 0.16 s.")]
        [SerializeField] private float groundAccel = 40f;
        [SerializeField] private float groundDecel = 55f;
        [SerializeField] private float airAccel = 12f;
        [Tooltip("Stick pushed past this for `sprintHold` seconds starts a sprint.")]
        [SerializeField] private float sprintPush = 0.95f;
        [SerializeField] private float sprintHold = 0.25f;
        [Tooltip("Stick below this is a walk.")]
        [SerializeField] private float walkBand = 0.5f;

        [Header("Flicker Step")]
        [SerializeField] private float flickerSpeed = 16f;
        [SerializeField] private float flickerDuration = 0.28f;
        [SerializeField] private float flickerCooldown = 0.95f;
        [SerializeField] private float afterImageInterval = 0.055f;

        [Header("Traversal")]
        [SerializeField] private float jumpSpeed = 9.5f;
        [SerializeField] private float grabJumpSpeed = 11.5f;  // a jump aimed at a high ledge
        [SerializeField] private float coyoteTime = 0.12f;     // grace after walking off
        [SerializeField] private float jumpBuffer = 0.15f;     // grace before landing
        [SerializeField] private float probeReach = 1.1f;
        [SerializeField] private float mantleTime = 0.6f;
        [SerializeField] private float vaultTime = 0.5f;
        [SerializeField] private float slideTime = 0.65f;
        [SerializeField] private float rollTime = 0.55f;
        [SerializeField] private float landTime = 0.35f;
        [Tooltip("Fall speed at touchdown that forces a roll or a hard landing.")]
        [SerializeField] private float hardLandingSpeed = 13f;
        [SerializeField] private float wallRunSeconds = 1.1f;
        [SerializeField] private float wallRunSpeed = 7.5f;
        [SerializeField] private float wallCheckDist = 0.8f;

        [Header("Stealth")]
        [SerializeField] private float runNoiseRadius = 9f;
        [SerializeField] private float sprintNoiseRadius = 12f;
        [SerializeField] private float walkNoiseRadius = 5f;
        [SerializeField] private float crouchNoiseRadius = 1.6f;
        [SerializeField] private float landNoiseRadius = 12f;
        [SerializeField] private float rollNoiseRadius = 4f;

        // ---------------------------------------------------------------- state

        /// <summary>An exclusive traversal move. Free means the normal motor runs.</summary>
        private enum Move { Free, Mantle, Vault, Hang, Slide, Roll, Land }

        public bool Invulnerable => _flickerTimer > 0f;
        public bool Busy { get; set; } // set by CombatController during swings
        public float FlickerCooldownRemaining => _flickerCd;

        /// <summary>Feet on something solid, refreshed after this frame's move.</summary>
        public bool Grounded { get; private set; }

        /// <summary>True while running along a wall — HUD/animation read this.</summary>
        public bool WallRunning => _wallT > 0f;

        /// <summary>Wall runs started this mission (feat tracking).</summary>
        public int WallRuns { get; private set; }

        /// <summary>
        /// Crouched: slower, quieter, and much harder to see. The whole stealth
        /// loop hangs off this one flag — enemies scale detection by visibility.
        /// A slide counts: the body is just as low.
        /// </summary>
        public bool Crouched { get; private set; }

        public bool Sprinting => Gait == Gait.Sprint;
        public bool Swimming { get; private set; }
        public bool Hanging => _move == Move.Hang;

        /// <summary>Inside a mantle, vault, hang, slide, roll or hard landing.</summary>
        public bool Traversing => _move != Move.Free;

        public Gait Gait { get; private set; }

        /// <summary>0 idle, 0.4 walk, 0.75 run, 1 sprint — the gait tree's thresholds.</summary>
        public float GaitBlend { get; private set; }

        /// <summary>What the action button would do right now.</summary>
        public TraversalKind Hint => _hint.kind;

        /// <summary>Horizontal speed this frame, metres per second.</summary>
        public float PlanarSpeed => _planar.magnitude;

        public Vector3 Velocity => _lastVelocity;
        public Vector3 Facing { get; private set; } = Vector3.forward;

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

        private CharacterController _cc;
        private Core.CharacterRig _rig;
        private Transform _cam;
        private float _standHeight, _standCenterY;

        private Vector3 _planar;          // smoothed horizontal velocity
        private Vector3 _impulse;
        private Vector3 _lastVelocity;
        private float _yVel, _fallSpeed;
        private float _flickerTimer, _flickerCd, _ghostTick;
        private Vector3 _flickerDir;
        private float _sprintPushT;
        private float _stride, _noiseTick;
        private float _slowT, _slowMul = 1f;
        private Vector3 _groundNormal = Vector3.up;

        // Jumping and walls.
        private float _coyoteT, _bufferT, _wallT, _wallCd;
        private int _airFlickersUsed;
        private Vector3 _wallNormal, _wallTangent;

        // Traversal moves.
        private Move _move;
        private float _moveT, _moveDur, _grabCd, _hintT;
        private Vector3 _moveFrom, _moveMid, _moveTo, _moveDir;
        private TraversalHit _hint, _hang;

        // ------------------------------------------------------------ lifecycle

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rig = GetComponent<Core.CharacterRig>();
            _cam = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null;
            _standHeight = _cc.height;
            _standCenterY = _cc.center.y;
            _cc.slopeLimit = 50f;
        }

        /// <summary>
        /// Timed movement debuff (Kagachi's venom). Stacks by taking the harsher
        /// of the two so a second hit can't accidentally cure the first.
        /// </summary>
        public void ApplySlow(float duration, float multiplier)
        {
            _slowMul = _slowT > 0f ? Mathf.Min(_slowMul, multiplier) : multiplier;
            _slowT = Mathf.Max(_slowT, duration);
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
            _grabCd = Mathf.Max(0, _grabCd - dt);
            if (_slowT > 0f && (_slowT -= dt) <= 0f) _slowMul = 1f;
            if (_cam == null && UnityEngine.Camera.main != null) _cam = UnityEngine.Camera.main.transform;

            var input = ReadMoveInput();
            var stick = Core.EmberInput.Move.magnitude;

            // Buffered jump: pressing just before touchdown still fires on landing.
            if (Core.EmberInput.ConsumeJump()) _bufferT = jumpBuffer;
            else _bufferT = Mathf.Max(0f, _bufferT - dt);

            if (_move != Move.Free)
            {
                UpdateMove(input, dt);
                return;
            }

            UpdateSwimming();
            UpdateCrouch(input, stick);
            if (_move != Move.Free) return; // a slide began
            UpdateHint(input, dt);

            var wasGrounded = Grounded;
            Grounded = _cc.isGrounded && !Swimming;
            if (Grounded)
            {
                _coyoteT = coyoteTime;
                _airFlickersUsed = 0;
                _wallT = 0f;
                if (!wasGrounded && OnLanded(input)) return;
            }
            else
            {
                _coyoteT = Mathf.Max(0f, _coyoteT - dt);
                _fallSpeed = Mathf.Min(_fallSpeed, _yVel);
            }
            SampleGround();
            UpdateFootsteps(input, dt);

            UpdateWallRun(input, dt);
            if (!Swimming && TryAirGrab(input)) return;
            if (TryJump(input)) return;

            Vector3 velocity;
            if (_flickerTimer > 0f)
            {
                _flickerTimer -= dt;
                velocity = _flickerDir * flickerSpeed;
                _planar = velocity;
                _ghostTick -= dt;
                if (_ghostTick <= 0f)
                {
                    _ghostTick = afterImageInterval;
                    _rig?.SpawnAfterImage();
                }
                Gait = Gait.Sprint;
            }
            else if (_wallT > 0f)
            {
                // Along the wall, with a light push into it so we stay attached.
                velocity = _wallTangent * wallRunSpeed - _wallNormal * 0.8f;
                _planar = velocity;
                Gait = Gait.Sprint;
            }
            else
            {
                velocity = UpdatePlanar(input, stick, dt);
            }

            velocity += _impulse;
            _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, 45f * dt);

            // Vertical: a flicker hangs (clean horizontal dash), a wall-run only
            // drifts, water floats the chest at the surface, the ground holds the
            // feet down hard enough to follow a slope without hopping off it.
            if (_flickerTimer > 0f) _yVel = 0f;
            else if (_wallT > 0f) _yVel += gravity * 0.18f * dt;
            else if (Swimming)
            {
                var surface = Core.ZoneTerrain.WaterLevel - 0.75f;
                _yVel = Mathf.Clamp((surface - transform.position.y) * 4f, -3f, 3f);
            }
            else if (Grounded && _yVel <= 0f) _yVel = -6f;
            else _yVel += gravity * dt;

            // Too steep to stand on: let it slide.
            if (Grounded && _groundNormal.y < Mathf.Cos(_cc.slopeLimit * Mathf.Deg2Rad))
            {
                var down = Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized;
                velocity += down * 6f;
            }

            velocity.y = _yVel;
            _lastVelocity = velocity;
            var wasAirborne = !Grounded;
            _cc.Move(velocity * dt);
            Grounded = _cc.isGrounded && !Swimming;
            if (wasAirborne && Grounded && _fallSpeed < -6f)
                Enemies.NoiseSystem.Emit(transform.position, landNoiseRadius * 0.6f);
            DriveRig();
            ApplyBoundary(dt);
        }

        // ------------------------------------------------------------- planar

        /// <summary>Gait selection and smoothed horizontal velocity.</summary>
        private Vector3 UpdatePlanar(Vector3 input, float stick, float dt)
        {
            var wants = input.sqrMagnitude > 0.01f;

            // Sprint: the on-screen stick pushed through its rim for a beat, or a
            // held sprint key. A scripted bot never sprints — its Cam() vector is
            // unit length and would trip the push test on every stage.
            var pushing = wants && Core.EmberInput.TouchActive && stick >= sprintPush;
            _sprintPushT = pushing ? _sprintPushT + dt : 0f;
            var sprinting = wants && !Crouched && !Busy && !Swimming && Grounded
                            && (Core.EmberInput.SprintHeld || _sprintPushT >= sprintHold);

            float top;
            if (!wants) { Gait = Gait.Idle; top = 0f; }
            else if (Swimming) { Gait = Gait.Walk; top = swimSpeed; }
            else if (Crouched) { Gait = Gait.StealthWalk; top = crouchSpeed; }
            else if (sprinting) { Gait = Gait.Sprint; top = sprintSpeed; }
            else if (stick < walkBand) { Gait = Gait.Walk; top = walkSpeed; }
            else { Gait = Gait.Run; top = moveSpeed; }

            if (Busy) top *= busySpeedMultiplier;
            if (!Swimming && ArenaMarkers.InWater(transform.position)) top *= 0.75f; // knee-deep marsh
            top *= Enemies.SlowZone.SpeedMulAt(transform.position);                   // Goro's slam scar
            if (_slowT > 0f) top *= _slowMul;                                          // venom

            // Inside the walk band the stick still scales speed, so a small nudge
            // is a creep; past it the gait sets the speed and the stick the direction.
            var scale = Gait is Gait.Walk or Gait.StealthWalk ? Mathf.Clamp01(stick / walkBand) : 1f;
            var desired = wants ? input.normalized * (top * Mathf.Max(0.35f, scale)) : Vector3.zero;

            var accel = !Grounded && !Swimming ? airAccel
                : desired.sqrMagnitude > _planar.sqrMagnitude ? groundAccel : groundDecel;
            _planar = Vector3.MoveTowards(_planar, desired, accel * dt);

            if (wants)
            {
                Facing = input.normalized;
                var target = Quaternion.LookRotation(Facing);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, rotationSpeedDeg * dt);
            }

            // Footsteps are a stealth signal, not just audio. Emitted on a slow
            // tick rather than per frame — the noise ring only needs the position.
            if (wants && (_noiseTick -= dt) <= 0f)
            {
                _noiseTick = Crouched ? 0.55f : 0.34f;
                Enemies.NoiseSystem.Emit(transform.position, Gait switch
                {
                    Gait.StealthWalk => crouchNoiseRadius,
                    Gait.Walk => walkNoiseRadius,
                    Gait.Sprint => sprintNoiseRadius,
                    _ => Swimming ? crouchNoiseRadius * 2f : runNoiseRadius,
                });
            }

            // Keep the body on the slope it is standing on instead of pushing into it.
            var v = Grounded ? Vector3.ProjectOnPlane(_planar, _groundNormal) : _planar;
            if (Grounded && v.y > 0f) v.y = 0f;
            return v;
        }

        private void DriveRig()
        {
            if (_rig == null) return;
            var speed = _planar.magnitude;
            GaitBlend = _flickerTimer > 0f || _wallT > 0f ? 1f
                : speed <= walkSpeed ? Mathf.Lerp(0f, 0.4f, speed / walkSpeed)
                : speed <= moveSpeed ? Mathf.Lerp(0.4f, 0.75f, (speed - walkSpeed) / (moveSpeed - walkSpeed))
                : Mathf.Lerp(0.75f, 1f, Mathf.Clamp01((speed - moveSpeed) / (sprintSpeed - moveSpeed)));
            _rig.SetLocoMode(Swimming ? Core.LocoMode.Swim : Crouched ? Core.LocoMode.Crouch : Core.LocoMode.Ground);
            // Crouch and swim trees are 0..1 by speed; the gait tree uses GaitBlend.
            _rig.move01 = Swimming ? Mathf.Clamp01(speed / swimSpeed)
                : Crouched ? Mathf.Clamp01(speed / crouchSpeed)
                : GaitBlend;
        }

        // -------------------------------------------------------------- crouch

        private void UpdateCrouch(Vector3 input, float stick)
        {
            var wants = Core.EmberInput.CrouchHeld && !Busy && _flickerTimer <= 0f && !Swimming;
            if (wants && Sprinting && Grounded && input.sqrMagnitude > 0.1f)
            {
                BeginSlide();
                return;
            }
            if (wants && Grounded) SetCrouched(true);
            else if (Crouched && (!wants || !Grounded) && HeadroomToStand()) SetCrouched(false);
        }

        private void SetCrouched(bool on)
        {
            if (Crouched == on) return;
            Crouched = on;
            _cc.height = on ? 1.25f : _standHeight;
            _cc.center = new Vector3(0f, on ? 0.65f : _standCenterY, 0f);
        }

        private bool HeadroomToStand()
        {
            var r = _cc.radius * 0.9f;
            var feet = transform.position;
            return !Physics.CheckCapsule(feet + Vector3.up * 1.25f, feet + Vector3.up * (_standHeight - r),
                r, TraversalProbe.Mask, QueryTriggerInteraction.Ignore);
        }

        // ---------------------------------------------------------------- swim

        private void UpdateSwimming()
        {
            if (!Core.Ground.ZoneActive) { Swimming = false; return; }
            var p = transform.position;
            var deep = Core.ZoneTerrain.WaterLevel - 0.9f;
            // Enter when the floor under the feet is below chest-deep; leave the
            // moment there is ground to stand on, so wading out is just walking.
            Swimming = Swimming
                ? Core.Ground.HeightAt(p) < deep + 0.15f
                : p.y < deep && Core.Ground.HeightAt(p) < deep;
            if (Swimming) { _wallT = 0f; _flickerTimer = 0f; SetCrouched(false); }
        }

        // -------------------------------------------------------------- ground

        /// <summary>One sphere cast a frame for the ground normal under the feet.</summary>
        private void SampleGround()
        {
            if (!Grounded) { _groundNormal = Vector3.up; return; }
            var origin = transform.position + Vector3.up * (_cc.radius + 0.1f);
            _groundNormal = Physics.SphereCast(origin, _cc.radius * 0.5f, Vector3.down, out var hit,
                0.45f, TraversalProbe.Mask, QueryTriggerInteraction.Ignore)
                ? hit.normal : Vector3.up;
        }

        /// <summary>
        /// Touchdown. A long fall either rolls (moving, quiet, keeps speed) or
        /// plants the feet hard (still, loud). Returns true when a move took over.
        /// </summary>
        private bool OnLanded(Vector3 input)
        {
            Core.Sfx3D.Footstep(transform.position, UI.Atmosphere.GroundIsWood, 0.6f);
            Core.Sfx3D.Cloth(transform.position, 0.45f);
            _stride = 0f;
            var fall = _fallSpeed;
            _fallSpeed = 0f;
            if (fall > -hardLandingSpeed) return false;
            if (input.sqrMagnitude > 0.1f)
            {
                BeginRoll(input.normalized);
                return true;
            }
            Enemies.NoiseSystem.Emit(transform.position, landNoiseRadius);
            Begin(Move.Land, landTime, Core.RigPose.Land);
            _planar = Vector3.zero;
            return true;
        }

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
            _stride += _planar.magnitude * dt;
            if (_stride < (Crouched ? 2.4f : Gait == Gait.Sprint ? 2.1f : 1.7f)) return;
            _stride = 0f;
            Core.Sfx3D.Footstep(transform.position, UI.Atmosphere.GroundIsWood,
                Crouched ? 0.18f : Gait == Gait.Walk ? 0.3f : 0.45f);
        }

        // ------------------------------------------------------------ traversal

        /// <summary>Re-read the obstacle ahead ten times a second for the action button.</summary>
        private void UpdateHint(Vector3 input, float dt)
        {
            _hintT -= dt;
            if (_hintT > 0f) return;
            _hintT = 0.1f;
            if (Swimming || _flickerTimer > 0f || (!Grounded && _coyoteT <= 0f))
            {
                _hint = default;
                return;
            }
            var dir = input.sqrMagnitude > 0.04f ? input.normalized : Facing;
            _hint = TraversalProbe.Scan(transform.position, dir, _cc.radius, _standHeight, probeReach, false);
        }

        /// <summary>
        /// Airborne and past the apex: catch any ledge in reach in the direction
        /// we are moving or facing. This is what turns a wall-run or a short jump
        /// into a climb onto the roof.
        /// </summary>
        private bool TryAirGrab(Vector3 input)
        {
            if (Grounded || _grabCd > 0f || _flickerTimer > 0f || _yVel > 3f) return false;
            var dir = input.sqrMagnitude > 0.04f ? input.normalized
                : _wallT > 0f ? -_wallNormal : Facing;
            var hit = TraversalProbe.Scan(transform.position, dir, _cc.radius, _standHeight, 0.9f, true);
            if (hit.kind != TraversalKind.Grab) return false;
            BeginHang(hit);
            return true;
        }

        private bool TryJump(Vector3 input)
        {
            if (_bufferT <= 0f) return false;

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
                _rig?.PlayOneShot(Core.RigPose.Jump, 0.3f);
                Core.Sfx3D.DodgeBark();
                Core.Sfx3D.Cloth(transform.position);
                return false;
            }

            if (Swimming) { _bufferT = 0f; return false; }
            if (!Grounded && _coyoteT <= 0f) return false; // airborne past the coyote grace
            _bufferT = 0f;
            _coyoteT = 0f;

            // The action button reads the geometry: over it, onto it, or up for it.
            if (_hint.kind == TraversalKind.Vault) { BeginVault(_hint); return true; }
            if (_hint.kind == TraversalKind.Mantle) { BeginMantle(_hint); return true; }

            Grounded = false;
            _fallSpeed = 0f;
            SetCrouched(false);
            // A grab-height ledge gets the taller jump so the hands actually arrive.
            _yVel = _hint.kind == TraversalKind.Grab ? grabJumpSpeed : jumpSpeed;
            if (_hint.kind == TraversalKind.Grab) SetFacing(-_hint.wallNormal);
            _rig?.PlayOneShot(Core.RigPose.Jump, 0.3f);
            Core.Sfx3D.DodgeBark();
            Core.Sfx3D.Cloth(transform.position);
            return false;
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

            if (Grounded || Swimming || _wallCd > 0f || _flickerTimer > 0f) return;
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
                if (!Physics.Raycast(origin, transform.right * s, out var hit, wallCheckDist,
                        TraversalProbe.Mask, QueryTriggerInteraction.Ignore))
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

        // ---------------------------------------------------------------- moves

        private void Begin(Move move, float duration, Core.RigPose pose)
        {
            _move = move;
            _moveT = 0f;
            _moveDur = Mathf.Max(0.05f, duration);
            _wallT = 0f;
            _flickerTimer = 0f;
            _impulse = Vector3.zero;
            _hint = default;
            Busy = true;
            _rig?.PlayOneShot(pose, _moveDur);
        }

        private void BeginMantle(TraversalHit hit)
        {
            Begin(Move.Mantle, mantleTime, Core.RigPose.Mantle);
            _moveFrom = transform.position;
            // Up the face first, then over the lip onto the landing.
            _moveMid = new Vector3(hit.wallPoint.x, hit.ledge.y + 0.05f, hit.wallPoint.z)
                       + hit.wallNormal * (_cc.radius + 0.05f);
            _moveTo = hit.landing;
            SetFacing(-hit.wallNormal);
            _cc.enabled = false;
            Core.Sfx3D.Cloth(transform.position, 0.5f);
        }

        private void BeginVault(TraversalHit hit)
        {
            Begin(Move.Vault, vaultTime, Core.RigPose.Vault);
            _moveFrom = transform.position;
            _moveMid = new Vector3(hit.ledge.x, hit.ledge.y + 0.35f, hit.ledge.z);
            _moveTo = hit.landing;
            SetFacing(-hit.wallNormal);
            _cc.enabled = false;
            Core.Sfx3D.DodgeBark();
            Core.Sfx3D.Cloth(transform.position);
        }

        private void BeginHang(TraversalHit hit)
        {
            Begin(Move.Hang, 999f, Core.RigPose.Hang);
            _hang = hit;
            _yVel = 0f;
            _planar = Vector3.zero;
            _fallSpeed = 0f;
            // Hands on the lip, body flat against the face, feet 1.55 m below.
            var at = new Vector3(hit.wallPoint.x, hit.ledge.y - 1.55f, hit.wallPoint.z)
                     + hit.wallNormal * (_cc.radius + 0.06f);
            _cc.enabled = false;
            transform.position = at;
            _cc.enabled = true;
            SetFacing(-hit.wallNormal);
            Core.Sfx3D.Cloth(transform.position, 0.6f);
            Enemies.NoiseSystem.Emit(transform.position, walkNoiseRadius);
        }

        private void BeginSlide()
        {
            Begin(Move.Slide, slideTime, Core.RigPose.Slide);
            SetCrouched(true);
            _moveDir = _planar.sqrMagnitude > 0.1f ? _planar.normalized : Facing;
            Core.Sfx3D.Cloth(transform.position, 0.5f);
        }

        private void BeginRoll(Vector3 dir)
        {
            Begin(Move.Roll, rollTime, Core.RigPose.Roll);
            SetCrouched(true);
            _moveDir = dir;
            SetFacing(dir);
            Enemies.NoiseSystem.Emit(transform.position, rollNoiseRadius);
            Core.Sfx3D.Cloth(transform.position, 0.5f);
        }

        private void EndMove()
        {
            var was = _move;
            _move = Move.Free;
            Busy = false;
            if (!_cc.enabled) _cc.enabled = true;
            if (was is Move.Mantle or Move.Vault or Move.Hang) _grabCd = 0.35f;
            if (was is Move.Slide or Move.Roll && !Core.EmberInput.CrouchHeld && HeadroomToStand())
                SetCrouched(false);
            _yVel = was == Move.Hang ? 0f : -1f;
            Grounded = _cc.isGrounded;
        }

        private void PullUpFromHang()
        {
            Begin(Move.Mantle, mantleTime, Core.RigPose.Mantle);
            _moveFrom = transform.position;
            _moveMid = new Vector3(_hang.wallPoint.x, _hang.ledge.y + 0.05f, _hang.wallPoint.z)
                       + _hang.wallNormal * (_cc.radius + 0.05f);
            _moveTo = _hang.landing;
            _cc.enabled = false;
            Core.Sfx3D.Cloth(transform.position, 0.5f);
        }

        private void UpdateMove(Vector3 input, float dt)
        {
            _moveT += dt;
            var k = Mathf.Clamp01(_moveT / _moveDur);
            switch (_move)
            {
                case Move.Mantle:
                {
                    // Rise first (0..0.6), then step onto the landing (0.5..1).
                    var rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.6f));
                    var over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.5f) / 0.5f));
                    var p = Vector3.Lerp(_moveFrom, _moveMid, rise);
                    p = Vector3.Lerp(p, _moveTo, over);
                    transform.position = p;
                    _lastVelocity = Vector3.zero;
                    if (k >= 1f) { transform.position = _moveTo + Vector3.up * 0.02f; EndMove(); }
                    return;
                }
                case Move.Vault:
                {
                    // A shallow arc over the lip: horizontal is linear, height peaks mid-way.
                    var xz = Vector3.Lerp(_moveFrom, _moveTo, k);
                    var peak = Mathf.Max(_moveMid.y, Mathf.Max(_moveFrom.y, _moveTo.y) + 0.2f);
                    var y = k < 0.5f
                        ? Mathf.Lerp(_moveFrom.y, peak, Mathf.SmoothStep(0f, 1f, k / 0.5f))
                        : Mathf.Lerp(peak, _moveTo.y, Mathf.SmoothStep(0f, 1f, (k - 0.5f) / 0.5f));
                    transform.position = new Vector3(xz.x, y, xz.z);
                    _lastVelocity = (_moveTo - _moveFrom) / _moveDur;
                    if (k >= 1f)
                    {
                        transform.position = _moveTo + Vector3.up * 0.02f;
                        _planar = Vector3.ProjectOnPlane(_moveTo - _moveFrom, Vector3.up).normalized * moveSpeed * 0.6f;
                        EndMove();
                    }
                    return;
                }
                case Move.Hang:
                {
                    _lastVelocity = Vector3.zero;
                    var toward = Vector3.Dot(input, -_hang.wallNormal);
                    if (_bufferT > 0f || toward > 0.5f)
                    {
                        _bufferT = 0f;
                        PullUpFromHang();
                        return;
                    }
                    if (Core.EmberInput.CrouchHeld || toward < -0.5f || _moveT > 12f)
                    {
                        // Let go.
                        EndMove();
                        _grabCd = 0.45f;
                        Grounded = false;
                        _yVel = -0.5f;
                    }
                    return;
                }
                case Move.Slide:
                {
                    var speed = Mathf.Lerp(sprintSpeed, walkSpeed, k);
                    _planar = _moveDir * speed;
                    var v = Vector3.ProjectOnPlane(_planar, _groundNormal);
                    v.y = _cc.isGrounded ? -6f : _yVel + gravity * dt;
                    _yVel = v.y;
                    _lastVelocity = v;
                    _cc.Move(v * dt);
                    Grounded = _cc.isGrounded;
                    if (_rig != null) { _rig.SetLocoMode(Core.LocoMode.Crouch); _rig.move01 = 1f; }
                    if (k >= 1f) EndMove();
                    return;
                }
                case Move.Roll:
                {
                    var speed = Mathf.Lerp(moveSpeed, walkSpeed, k);
                    _planar = _moveDir * speed;
                    var v = Vector3.ProjectOnPlane(_planar, _groundNormal);
                    v.y = -6f;
                    _lastVelocity = v;
                    _cc.Move(v * dt);
                    Grounded = _cc.isGrounded;
                    if (k >= 1f) EndMove();
                    return;
                }
                case Move.Land:
                {
                    _planar = Vector3.zero;
                    _lastVelocity = Vector3.zero;
                    _cc.Move(Vector3.down * (6f * dt));
                    Grounded = _cc.isGrounded;
                    if (k >= 1f) EndMove();
                    return;
                }
            }
        }

        // ------------------------------------------------------------- bounds

        /// <summary>
        /// Boundary enforcement. In Endless mode the Road North corridor is still a
        /// hard clamp (that constraint is thematic, not a cage). In all other modes
        /// MissionBounds applies a soft push-back force so the player feels wind
        /// resistance rather than hitting an invisible wall.
        /// </summary>
        private void ApplyBoundary(float dt)
        {
            var p = transform.position;

            // Road North (Endless): preserve the intentional corridor.
            if (RoadNorth.Instance != null)
            {
                var half = Core.SceneRefs.Game != null
                    ? Core.SceneRefs.Game.arenaHalfExtents : new Vector2(13f, 8f);
                var xLimit = RoadNorth.XLimitAt(p.z, half.x);
                p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
                p.z = Mathf.Max(p.z, -half.y);
                if (p.y < -3f) { p.y = 0.5f; _yVel = 0f; }   // flat corridor
                if (p != transform.position) transform.position = p;
                return;
            }

            // Soft containment: push the player back toward the play area.
            var pushBack = Core.MissionBounds.ContainForce(p);
            if (pushBack.sqrMagnitude > 0.01f)
                _cc.Move(pushBack * dt);

            // Safety net: teleport back if a physics glitch launched us way out.
            p = transform.position;
            var (safePos, teleported) = Core.MissionBounds.SafetyClamp(p);
            if (teleported)
            {
                _cc.enabled = false;
                transform.position = safePos;
                _cc.enabled = true;
                _yVel = 0f;
                return;
            }

            // Fell through the world: recover onto the actual floor, well clear of it.
            var floor = Core.Ground.HeightAt(p.x, p.z);
            if (p.y < floor - 3f)
            {
                p.y = floor + 0.5f;
                _yVel = 0f;
                transform.position = p;
            }
        }

        // ------------------------------------------------------------ external

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
            if (_flickerCd > 0f || Traversing) return false;
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
            _planar = Vector3.zero;
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
            if (_flickerCd > 0f || Swimming) return false;
            if (Traversing)
            {
                // A hang lets go into a dodge; nothing else can be cancelled.
                if (_move != Move.Hang) return false;
                EndMove();
                _grabCd = 0.45f;
                Grounded = false;
            }
            var airborne = !Grounded && _coyoteT <= 0f;
            if (airborne && _airFlickersUsed >= AirFlickerLimit) return false;
            if (airborne) _airFlickersUsed++;
            _wallT = 0f; // flicking off a wall releases it
            SetCrouched(false);
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
            if (_cam == null) return raw;
            var fwd = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized;
            return fwd * raw.z + right * raw.x;
        }
    }
}
