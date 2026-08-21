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

        public bool Invulnerable => _flickerTimer > 0f;
        public bool Busy { get; set; } // set by CombatController during swings
        public float FlickerCooldownRemaining => _flickerCd;
        public Vector3 Facing { get; private set; } = Vector3.forward;

        private CharacterController _cc;
        private Core.NinjaRig _rig;
        private Transform _cam;
        private float _flickerTimer, _flickerCd, _ghostTick, _yVel;
        private Vector3 _flickerDir;
        private Vector3 _impulse;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rig = GetComponent<Core.NinjaRig>();
            _cam = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null;
        }

        private void Update()
        {
            _flickerCd = Mathf.Max(0, _flickerCd - Time.deltaTime);
            var input = ReadMoveInput();

            Vector3 velocity;
            if (_flickerTimer > 0f)
            {
                _flickerTimer -= Time.deltaTime;
                velocity = _flickerDir * flickerSpeed;
                _ghostTick -= Time.deltaTime;
                if (_ghostTick <= 0f)
                {
                    _ghostTick = afterImageInterval;
                    _rig?.SpawnAfterImage();
                }
            }
            else
            {
                var speed = moveSpeed * (Busy ? busySpeedMultiplier : 1f);
                velocity = input * speed;
                if (input.sqrMagnitude > 0.01f)
                {
                    Facing = input.normalized;
                    var target = Quaternion.LookRotation(Facing);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, target, rotationSpeedDeg * Time.deltaTime);
                }
            }

            if (_rig != null)
                _rig.move01 = _flickerTimer > 0f ? 1f
                    : Mathf.Clamp01(input.magnitude) * (Busy ? 0.3f : 1f);

            velocity += _impulse;
            _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, 45f * Time.deltaTime);

            _yVel = _cc.isGrounded ? -1f : _yVel + gravity * Time.deltaTime;
            velocity.y = _yVel;
            _cc.Move(velocity * Time.deltaTime);
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

        /// <summary>Ember-step dodge. Returns true if it fired.</summary>
        public bool TryFlicker()
        {
            if (_flickerCd > 0f) return false;
            _flickerCd = flickerCooldown;
            _flickerTimer = flickerDuration;
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
