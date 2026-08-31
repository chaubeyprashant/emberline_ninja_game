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
        public float spawnTime = 0.45f;   // longer for skeletal rigs with intro anims

        [Header("Arena clamp (matches bootstrap arena)")]
        public Vector2 arenaHalfExtents = new(13f, 8f);

        public bool Dead { get; private set; }
        public float Hp { get; private set; }
        public bool InWindupOrDash => _state is State.Windup or State.Dashing;

        private enum State { Spawning, Chase, Windup, Recover, Stagger, Dashing, Dying }

        private State _state = State.Spawning;
        private float _t = 0.45f, _attackCd, _ghostTick;
        private bool _enrageAnnounced, _clonesSpawned, _waterRaised;
        private bool _dashAttack, _dashHit;
        private Vector3 _dashDir = Vector3.forward;
        private Transform _player;
        private Player.PlayerLocomotion _playerMotor;
        private Health _playerHealth;
        private SenGates _playerGates;
        private AttackTokenPool _tokens;
        private GameManager _gm;
        private CharacterRig _rig;
        private Transform _ring;
        private Material _ringMat;
        private float _strafeSide = 1f;

        public bool Enraged => !isClone
            && (kind == EnemyKind.Chief && Hp < maxHp * 0.5f
                || kind == EnemyKind.Jin && Hp < maxHp * 0.4f);
        public int Phase => kind != EnemyKind.Kagachi || isClone ? 1
            : Hp > maxHp * 0.6f ? 1 : Hp > maxHp * 0.3f ? 2 : 3;

        private bool IsBoss => kind is EnemyKind.Chief or EnemyKind.Jin || (kind == EnemyKind.Kagachi && !isClone);
        private float Speed => (Enraged || Phase == 3 ? speed * 1.4f : speed) * GameManager.EnemySpeedMul;
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
            _rig = GetComponent<CharacterRig>();
            _t = spawnTime;
            if (kind == EnemyKind.Shade) Sfx3D.ShadeWhisper();
            BuildTelegraphRing();
        }

        private void Update()
        {
            if (_player == null || Dead && _state != State.Dying) return;
            if (GameManager.CinematicActive)
            {
                // Boss intros freeze the fight. No ForcePose here — forced poses
                // would override the taunt one-shot the intro plays on the boss.
                if (_rig != null) _rig.move01 = 0f;
                return;
            }

            _attackCd = Mathf.Max(0, _attackCd - Time.deltaTime);
            CheckPhaseTransitions();

            var to = _player.position - transform.position;
            to.y = 0;
            var dist = to.magnitude;
            if (_rig != null) _rig.move01 = 0f;

            switch (_state)
            {
                case State.Spawning:
                    _rig?.ForcePose(RigPose.Spawn, 1f - _t / Mathf.Max(0.05f, spawnTime));
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
                          && !ArenaMarkers.Blocked(transform.position + Vector3.up, _player.position + Vector3.up)
                        : kind == EnemyKind.Chief
                            ? dist < 4.2f // slam range — the whole point of a greataxe
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
                    // Jin's storm dash leaves a lightning trail of after-images.
                    if (kind == EnemyKind.Jin && (_ghostTick -= Time.deltaTime) <= 0f)
                    {
                        _ghostTick = 0.06f;
                        _rig?.SpawnAfterImage();
                    }
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
                    _rig?.SetBaseColor(new Color(0.85f, 0.45f, 0.40f));
                }
                Sfx3D.BossRoar();
                _rig?.SetMood(RigMood.Enraged);
                _rig?.PlayOneShot(RigPose.Taunt, 0.9f);
                FxPools.Nova(transform.position);
                FxPools.Embers(transform.position + Vector3.up, 20);
            }
            if (kind == EnemyKind.Kagachi && !isClone && Phase == 3 && !_waterRaised)
            {
                // Desperation: the arena itself drowns — every pool widens.
                _waterRaised = true;
                _gm?.Announce("THE WATER RISES");
                Sfx3D.BossRoar();
                ArenaMarkers.RaiseWater(2.2f);
            }
            if (kind == EnemyKind.Kagachi && !isClone && Phase >= 2 && !_clonesSpawned)
            {
                _clonesSpawned = true;
                _gm?.Announce("THE SERPENT SPLITS");
                ArenaMarkers.RaiseWater(1.5f);
                for (var i = 0; i < 2; i++)
                {
                    var clone = Instantiate(gameObject, RandomSpot(), Quaternion.identity);
                    var brain = clone.GetComponent<EnemyBrain>();
                    brain.isClone = true;
                    brain.maxHp = 40f;
                    brain.damage = 6f;
                    clone.GetComponent<CharacterRig>()?.MakeGhost(0.45f);
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
                _rig?.PlayOneShot(RigPose.Strike2, 0.4f);
                Projectile.Spawn(transform.position + Vector3.up * 1.1f + _dashDir * 0.6f, _dashDir, damage);
                _attackCd = 2.3f;
            }
            else if (kind == EnemyKind.Chief && dist < 4.4f)
            {
                // Goro's telegraphed ground slam: shockwave AoE — dodge it or eat it.
                _rig?.PlayOneShot(RigPose.Strike1, 0.5f);
                Sfx3D.HitCrush();
                FxPools.Nova(transform.position);
                FindFirstObjectByType<CameraRig>()?.Shake(8f, 0.35f);
                if (dist < 3.4f)
                {
                    if (player != null && player.Invulnerable)
                    {
                        _playerGates?.OnPerfectDodge();
                        _gm?.OnPerfectDodge();
                    }
                    else
                    {
                        _playerHealth?.Damage(AttackDamage + 4f, transform.position);
                    }
                }
                _attackCd = Enraged ? 1.0f : 1.8f;
            }
            else if (dist < attackRange + 0.6f)
            {
                _rig?.PlayOneShot(RigPose.Strike1, 0.45f);
                Sfx3D.Slash();
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

        /// <summary>Applies damage (with weak-point bonuses) and returns what was dealt.</summary>
        public float TakeHit(float amount, Vector3 from, bool crush = false)
        {
            if (Dead) return 0f;
            // Weak points: archers crumble to backstabs, shades are raw while forming.
            if (kind == EnemyKind.Ranged)
            {
                var toAttacker = from - transform.position;
                toAttacker.y = 0;
                if (toAttacker.sqrMagnitude > 0.01f
                    && Vector3.Angle(transform.forward, toAttacker) > 120f)
                    amount *= 2f;
            }
            else if (kind == EnemyKind.Shade && _state == State.Spawning)
            {
                amount *= 2f;
            }
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
                FxPools.DeathBurst(transform.position, IsBoss);
                StoryMemory.UnlockLore(kind);
                _gm?.OnEnemyKilled(IsBoss);
                return amount;
            }
            // Jin answers light hits with an instant counter-dash (his signature).
            if (kind == EnemyKind.Jin && !isClone && !crush
                && _state is State.Chase or State.Recover && Random.value < 0.35f)
            {
                UI.FloatingText.Spawn(transform.position + Vector3.up * 2.3f, "COUNTER",
                    new Color(0.7f, 0.78f, 1f), 0.95f);
                _dashAttack = true;
                _state = State.Windup;
                _t = 0.35f;
                SetRing(true);
                return amount;
            }
            // Bosses armor through light hits mid-windup; everyone staggers to a crush.
            if (_state != State.Dashing && (crush || _state != State.Windup || !IsBoss))
            {
                if (_state == State.Windup) SetRing(false);
                _state = State.Stagger;
                _t = crush ? 0.55f : 0.26f;
                transform.position += (transform.position - from).normalized * (crush ? 0.9f : 0.4f);
            }
            return amount;
        }

        private Vector3 RandomSpot()
        {
            // On the Road North the "arena" is wherever the player is marching.
            if (RoadNorth.Instance != null && _player != null)
                return RoadNorth.Clamp(_player.position + new Vector3(
                    Random.Range(-4f, 4f), 0, Random.Range(2f, 7f)), arenaHalfExtents);
            return new Vector3(
                Random.Range(-arenaHalfExtents.x + 1f, arenaHalfExtents.x - 1f), 0,
                Random.Range(-arenaHalfExtents.y + 1f, arenaHalfExtents.y - 1f));
        }

        private void Move(Vector3 dir)
        {
            var speed = Speed;
            if (ArenaMarkers.InWater(transform.position)) speed *= 0.85f;
            var steered = dir + ArenaMarkers.Avoid(transform.position);
            if (steered.sqrMagnitude > 1f) steered.Normalize();
            transform.position += steered * (speed * Time.deltaTime);
        }

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
            if (RoadNorth.Instance != null)
            {
                transform.position = RoadNorth.Clamp(transform.position, arenaHalfExtents);
                return;
            }
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
