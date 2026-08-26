using UnityEngine;
using Emberline.Core;
using Emberline.UI;

namespace Emberline.Enemies
{
    public enum EnemyKind { Bandit, Ranged, Chief, Shade, Kagachi, Jin }

    /// <summary>
    /// Enemy AI ported from the tuned 2D prototype, now driving a NinjaRig:
    /// Spawn → Chase → Windup(telegraph) → Strike/Dash → Recover, with Stagger,
    /// chief enrage, shade fade, Kagachi's clone-split phases, and boss dashes.
    /// White telegraph = normal attack, red = dash/desperation.
    /// </summary>
    public class EnemyBrain : MonoBehaviour
    {
        /// <summary>Live registry — cheaper than FindObjectsByType every frame.</summary>
        public static readonly System.Collections.Generic.List<EnemyBrain> Active = new();

        [Header("Identity")]
        public EnemyKind kind = EnemyKind.Bandit;
        public bool isClone;

        [Header("Stats (prototype-tuned)")]
        public float maxHp = 42f;
        public float speed = 3.2f;
        public float attackRange = 1.8f;
        public float damage = 9f;
        public float windupTime = 0.55f;

        [Header("Arena clamp (matches bootstrap arena)")]
        public Vector2 arenaHalfExtents = new(13f, 8f);

        public bool Dead { get; private set; }
        public float Hp { get; private set; }
        public bool InWindupOrDash => _state is State.Windup or State.Dashing;

        private enum State { Spawning, Chase, Windup, Recover, Stagger, Dashing, Dying }

        private State _state = State.Spawning;
        private float _t = 0.45f, _attackCd;
        private bool _enrageAnnounced, _clonesSpawned;
        private bool _dashAttack, _dashHit;
        private Vector3 _dashDir = Vector3.forward;
        private Transform _player;
        private Player.PlayerLocomotion _playerMotor;
        private Health _playerHealth;
        private SenGates _playerGates;
        private AttackTokenPool _tokens;
        private GameManager _gm;
        private NinjaRig _rig;
        private Transform _ring;
        private Material _ringMat;
        private float _strafeSide = 1f;

        public bool Enraged => !isClone
            && (kind == EnemyKind.Chief && Hp < maxHp * 0.5f
                || kind == EnemyKind.Jin && Hp < maxHp * 0.4f);
        public int Phase => kind != EnemyKind.Kagachi || isClone ? 1
            : Hp > maxHp * 0.6f ? 1 : Hp > maxHp * 0.3f ? 2 : 3;

        private bool IsBoss => kind is EnemyKind.Chief or EnemyKind.Jin || (kind == EnemyKind.Kagachi && !isClone);
        private float Speed => Enraged || Phase == 3 ? speed * 1.4f : speed;
        private float Windup => _dashAttack ? (kind == EnemyKind.Kagachi ? 0.55f : 0.7f)
            : Enraged ? 0.45f : Phase == 3 ? 0.38f : windupTime;
        private float AttackDamage => Enraged ? 18f : Phase == 3 ? 16f : damage;
        private bool RedTelegraph => _dashAttack || (Phase == 3 && !isClone);

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private void Start()
        {
            Hp = maxHp;
            _strafeSide = Random.value < 0.5f ? 1f : -1f;
            var motor = FindFirstObjectByType<Player.PlayerLocomotion>();
            if (motor != null)
            {
                _player = motor.transform;
                _playerMotor = motor;
                _playerHealth = motor.GetComponent<Health>();
                _playerGates = motor.GetComponent<SenGates>();
            }
            _tokens = FindFirstObjectByType<AttackTokenPool>();
            _gm = FindFirstObjectByType<GameManager>();
            _rig = GetComponent<NinjaRig>();
            BuildTelegraphRing();
        }

        private void Update()
        {
            if (_player == null || Dead && _state != State.Dying) return;

            _attackCd = Mathf.Max(0, _attackCd - Time.deltaTime);
            CheckPhaseTransitions();

            var to = _player.position - transform.position;
            to.y = 0;
            var dist = to.magnitude;
            if (_rig != null) _rig.move01 = 0f;

            switch (_state)
            {
                case State.Spawning:
                    _rig?.ForcePose(RigPose.Idle, 0);
                    if ((_t -= Time.deltaTime) <= 0) _state = State.Chase;
                    break;

                case State.Stagger:
                    _rig?.ForcePose(RigPose.Hurt, 1f - _t);
                    if ((_t -= Time.deltaTime) <= 0) _state = State.Chase;
                    break;

                case State.Recover:
                    // Shades slip away after striking.
                    if (kind == EnemyKind.Shade && dist > 0.5f)
                    {
                        Move(-to.normalized * 0.7f);
                        if (_rig != null) _rig.move01 = 0.7f;
                    }
                    if ((_t -= Time.deltaTime) <= 0) _state = State.Chase;
                    break;

                case State.Chase:
                {
                    Vector3 dir;
                    if (kind == EnemyKind.Ranged)
                        dir = dist < 6f ? -to.normalized : dist > 9f ? to.normalized : Strafe(to);
                    else
                        dir = dist > attackRange * 0.8f ? to.normalized : Strafe(to);
                    Move(dir);
                    Face(to);
                    if (_rig != null) _rig.move01 = 1f;

                    var inRange = kind == EnemyKind.Ranged
                        ? dist < 8f && dist > 4f
                        : dist < attackRange + 0.3f;
                    var wantsDash = IsBoss && dist > 5f && dist < 11f && _attackCd <= 0
                                    && Random.value < (Phase == 3 ? 0.03f
                                        : kind == EnemyKind.Jin ? 0.028f : 0.012f);
                    if ((inRange && _attackCd <= 0 || wantsDash)
                        && (IsBoss || _tokens == null || _tokens.TryTake(this)))
                    {
                        _dashAttack = wantsDash;
                        _state = State.Windup;
                        _t = Windup;
                        SetRing(true);
                    }
                    break;
                }

                case State.Windup:
                    // Dash aim locks in the last third of the windup.
                    if (!(_dashAttack && _t < Windup * 0.35f))
                    {
                        Face(to);
                        _dashDir = dist > 0.5f ? to.normalized : transform.forward;
                    }
                    _rig?.ForcePose(RigPose.Windup, 1f - _t / Windup);
                    if (_ring != null)
                        _ring.localScale = Vector3.one * Mathf.Lerp(2.6f, 1.3f, 1f - _t / Windup);
                    if ((_t -= Time.deltaTime) <= 0)
                    {
                        SetRing(false);
                        ResolveAttack(dist);
                    }
                    break;

                case State.Dashing:
                    _rig?.ForcePose(RigPose.Dash, 0.5f);
                    transform.position += _dashDir * ((kind == EnemyKind.Kagachi ? 15f : 13f) * Time.deltaTime);
                    if (!_dashHit && Vector3.Distance(transform.position, _player.position) < 1.4f)
                    {
                        _dashHit = true;
                        if (_playerMotor != null && _playerMotor.Invulnerable)
                        {
                            _playerGates?.OnPerfectDodge();
                            _gm?.OnPerfectDodge();
                        }
                        else
                        {
                            _playerHealth?.Damage(AttackDamage + 3f, transform.position);
                        }
                    }
                    if ((_t -= Time.deltaTime) <= 0)
                    {
                        _state = State.Recover;
                        _t = 0.8f;
                        _attackCd = 1.8f;
                    }
                    break;

                case State.Dying:
                    _rig?.ForcePose(RigPose.Dead, 1f - _t / 0.7f);
                    if ((_t -= Time.deltaTime) <= 0) Destroy(gameObject);
                    return;
            }

            ClampToArena();
        }

        private void CheckPhaseTransitions()
        {
            if (Enraged && !_enrageAnnounced)
            {
                _enrageAnnounced = true;
                if (kind == EnemyKind.Jin)
                {
                    _gm?.Announce("JIN DRAWS THE STORM");
                    _rig?.SetBaseColor(new Color(0.30f, 0.36f, 0.58f));
                }
                else
                {
                    _gm?.Announce("THE CHIEF SEES RED");
                    _rig?.SetBaseColor(new Color(0.55f, 0.16f, 0.12f));
                }
                FxPools.Embers(transform.position + Vector3.up, 20);
            }
            if (kind == EnemyKind.Kagachi && !isClone && Phase >= 2 && !_clonesSpawned)
            {
                _clonesSpawned = true;
                _gm?.Announce("THE SERPENT SPLITS");
                for (var i = 0; i < 2; i++)
                {
                    var clone = Instantiate(gameObject, RandomSpot(), Quaternion.identity);
                    var brain = clone.GetComponent<EnemyBrain>();
                    brain.isClone = true;
                    brain.maxHp = 40f;
                    brain.damage = 6f;
                    var cloneRig = clone.GetComponent<NinjaRig>();
                    if (cloneRig != null) { cloneRig.ghost = true; cloneRig.ghostAlpha = 0.45f; }
                }
                FxPools.Embers(transform.position + Vector3.up, 16);
                transform.position = RandomSpot();
            }
        }

        private void ResolveAttack(float dist)
        {
            if (_dashAttack)
            {
                _state = State.Dashing;
                _t = 0.45f;
                _dashHit = false;
                return;
            }
            var player = _playerMotor;
            if (kind == EnemyKind.Ranged)
            {
                Projectile.Spawn(transform.position + Vector3.up * 1.1f + _dashDir * 0.6f, _dashDir, damage);
                _attackCd = 2.3f;
            }
            else if (dist < attackRange + 0.6f)
            {
                if (player != null && player.Invulnerable)
                {
                    _playerGates?.OnPerfectDodge();
                    _gm?.OnPerfectDodge();
                }
                else
                {
                    _playerHealth?.Damage(AttackDamage, transform.position);
                }
                _attackCd = Enraged ? 0.8f : Phase == 3 ? 0.7f : kind == EnemyKind.Shade ? 2.0f : 1.5f;
            }
            _state = State.Recover;
            _t = kind == EnemyKind.Shade ? 1.1f : 0.75f;
        }

        public void TakeHit(float amount, Vector3 from, bool crush = false)
        {
            if (Dead) return;
            Hp -= amount;
            _rig?.Flash();
            if (Hp <= 0)
            {
                Hp = 0;
                Dead = true;
                _state = State.Dying;
                _t = 0.7f;
                SetRing(false);
                Sfx3D.Death();
                FxPools.Embers(transform.position + Vector3.up, IsBoss ? 30 : 14);
                _gm?.OnEnemyKilled(IsBoss);
                return;
            }
            // Bosses armor through light hits mid-windup; everyone staggers to a crush.
            if (_state != State.Dashing && (crush || _state != State.Windup || !IsBoss))
            {
                if (_state == State.Windup) SetRing(false);
                _state = State.Stagger;
                _t = crush ? 0.55f : 0.26f;
                transform.position += (transform.position - from).normalized * (crush ? 0.9f : 0.4f);
            }
        }

        private Vector3 RandomSpot() => new(
            Random.Range(-arenaHalfExtents.x + 1f, arenaHalfExtents.x - 1f), 0,
            Random.Range(-arenaHalfExtents.y + 1f, arenaHalfExtents.y - 1f));

        private void Move(Vector3 dir) =>
            transform.position += dir * (Speed * Time.deltaTime);

        private Vector3 Strafe(Vector3 to)
        {
            var n = to.normalized;
            return new Vector3(-n.z, 0, n.x) * (_strafeSide * 0.55f);
        }

        private void Face(Vector3 to)
        {
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized);
        }

        private void ClampToArena()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -arenaHalfExtents.x, arenaHalfExtents.x);
            p.z = Mathf.Clamp(p.z, -arenaHalfExtents.y, arenaHalfExtents.y);
            p.y = 0;
            transform.position = p;
        }

        private void BuildTelegraphRing()
        {
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ringGo.GetComponent<Collider>());
            ringGo.name = "TelegraphRing";
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0, 0.05f, 0);
            ringGo.transform.localScale = new Vector3(2.2f, 0.015f, 2.2f);
            var r = ringGo.GetComponent<Renderer>();
            _ringMat = new Material(Shader.Find("Emberline/Ghost"));
            r.material = _ringMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring = ringGo.transform;
            ringGo.SetActive(false);
        }

        private void SetRing(bool active)
        {
            if (_ring == null) return;
            _ring.gameObject.SetActive(active);
            if (active && _ringMat != null)
                _ringMat.color = RedTelegraph
                    ? new Color(1f, 0.2f, 0.15f, 0.8f)
                    : new Color(0.92f, 0.9f, 0.86f, 0.7f);
        }
    }
}
