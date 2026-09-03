using UnityEngine;
using Emberline.Core;
using Emberline.UI;

namespace Emberline.Enemies
{
    // Appended only — the values are serialised into prefabs and scenes by index.
    public enum EnemyKind { Bandit, Ranged, Chief, Shade, Kagachi, Jin, RaiderAxe, PikeGuard, Bomber,
        Assassin, Samurai, RogueNinja, EliteWarrior }

    /// <summary>
    /// What an enemy fights with. Drives the attack resolver and the telegraph, so
    /// two enemies sharing a model still read as different threats.
    /// </summary>
    public enum EnemyWeapon { Daggers, Sword, Axe, Spear, Crossbow, Claws, Bomb, None }

    /// <summary>
    /// Broad AI posture. Sits above the tight combat state machine: Combat covers
    /// the whole fight, while the existing Windup/Recover/Stagger states handle
    /// what happens inside one exchange.
    /// </summary>
    public enum AiState
    {
        Idle,        // stood down, no reason to move
        Patrol,      // walking a beat
        Suspicious,  // saw or heard something, not committed
        Investigate, // moving to a specific noise or sighting
        Search,      // lost them; sweeping the last known area
        Alert,       // knows, closing
        Chase,       // pursuing a seen player
        Combat,      // in the fight, obeying its squad role
        Defend,      // holding guard, low posture
        Retreat,     // backing off — hurt, or repositioning for range
        Recover,     // post-attack or post-stagger
        Dead,
    }

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
        /// <summary>
        /// Data-driven definition. When assigned it supplies stats, movement,
        /// moveset, defence and weaknesses — a new enemy type is an asset, not a
        /// new branch in here. Left null, the brain keeps its original hardcoded
        /// behaviour so the twelve phases of tuning before this still hold.
        /// </summary>
        public EnemyDef def;

        public EnemyKind kind = EnemyKind.Bandit;
        public EnemyWeapon weapon = EnemyWeapon.Sword;
        public bool isClone;

        /// <summary>Prefab this came from; null means "not pooled, destroy on death".</summary>
        [System.NonSerialized] public GameObject poolKey;

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
        /// <summary>Seconds left on the current wind-up (0 when not winding up). What the ring shows.</summary>
        public float WindupRemaining => _state == State.Windup ? Mathf.Max(0f, _t) : 0f;

        /// <summary>Reeling from a hit — the window the launcher and execution use.</summary>
        public bool Staggered => _state == State.Stagger;
        /// <summary>Guard up right now — the thing a guard-break answers.</summary>
        public bool Guarding => _guardT > 0f;
        /// <summary>Backing off by decision (retreat, panic, kite band).</summary>
        public bool Retreating => _retreatT > 0f || Ai == AiState.Retreat;

        /// <summary>Unscaled time of the last hit taken; drives the floating HP bar.</summary>
        public float LastHitTime { get; private set; } = -99f;

        /// <summary>Remaining guard pool. 0 means the guard is broken.</summary>
        public float Posture { get; private set; }

        /// <summary>Per-instance posture ceiling, for duels. 0 = use the def's.</summary>
        public float PostureOverride { get; set; }
        public float PostureRegenOverride { get; set; }

        public float MaxPosture => PostureOverride > 0f ? PostureOverride
            : def != null ? def.maxPosture : 40f;

        /// <summary>0..1 for the HUD posture pip.</summary>
        public float Posture01 => MaxPosture > 0f ? Mathf.Clamp01(Posture / MaxPosture) : 1f;

        /// <summary>Guard is broken: open to everything, including a finisher.</summary>
        public bool GuardBroken => _guardBreakT > 0f;

        /// <summary>Airborne from a strike-3 launcher; juggled and unable to act.</summary>
        public bool Launched => _vertVel != 0f || transform.position.y > 0.02f;

        /// <summary>
        /// A reeling, nearly-dead mook can be finished outright — as can one that
        /// hasn't noticed you, which is what makes stealth levels worth playing
        /// quietly. Bosses are exempt: their phase transitions are the fight, and
        /// skipping them would gut it.
        /// </summary>
        public bool CanExecute =>
            !Dead && (Unaware
                      // A broken guard opens a mook or an elite to a finisher.
                      // A boss, or anyone fought in a duel, is not killed by an
                      // ordinary guard break — that is a punish window, not an
                      // execution. The earned final blow comes only once the boss
                      // is nearly spent (low HP) and its guard is broken.
                      || GuardBroken && Rank < EnemyRank.MiniBoss && !BossDuel
                      || GuardBroken && (IsBoss || BossDuel) && Hp <= maxHp * 0.15f
                      || !IsBoss && !BossDuel && Staggered && Hp <= maxHp * 0.2f
                      || (IsBoss || BossDuel) && Staggered && Hp <= maxHp * 0.12f);

        /// <summary>Set at duel spawn: this is a 1-v-1 boss, protected from the
        /// guard-break execution shortcut so the fight is fought, not skipped.</summary>
        public bool BossDuel { get; set; }

        /// <summary>
        /// Fraction of HP damage a duel boss takes from ordinary swings. Small,
        /// so a normal combo mostly builds posture rather than draining life —
        /// HP is survival, posture is control (Duel overhaul, Part 5). Full
        /// damage still lands inside the guard-break punish window.
        /// </summary>
        public float DuelResist { get; set; } = 1f;

        /// <summary>Fights at range — never told to crowd the player.</summary>
        public bool IsRanged => weapon is EnemyWeapon.Crossbow or EnemyWeapon.Bomb;

        /// <summary>Elite and above ignore squad throttling; their fight is the encounter.</summary>
        public bool IsElite => Rank >= EnemyRank.Elite;

        /// <summary>Broad AI posture, above the per-frame combat state.</summary>
        public AiState Ai { get; private set; } = AiState.Idle;

        /// <summary>Where the enemy last had reason to believe the player was.</summary>
        public Vector3 LastKnown { get; private set; }

        /// <summary>Hasn't noticed the player yet (stealth missions).</summary>
        public bool Unaware { get; private set; }

        /// <summary>0..1 progress toward spotting the player. Drives the HUD meter.</summary>
        public float Detection { get; private set; }

        /// <summary>Start this enemy off unaware — patrolling instead of hunting.</summary>
        public void SetUnaware(bool value)
        {
            Unaware = value;
            Detection = 0f;
        }

        /// <summary>Wake up: the alarm has gone off, or this one spotted the player.</summary>
        public void Alert()
        {
            if (!Unaware) return;
            Unaware = false;
            Detection = 0f;
            Ai = AiState.Alert;
            LastKnown = _player != null ? _player.position : transform.position;
            _rig?.PlayOneShot(RigPose.Taunt, 0.5f);
            // A shout on detection is the fairest possible stealth tell: it is
            // positional, so you can hear which direction you were seen from.
            Sfx3D.EnemyVoice(transform.position + Vector3.up * 1.4f,
                kind == EnemyKind.Shade ? Sfx3D.Voice.Whisper : Sfx3D.Voice.Alert);
        }

        private enum State { Spawning, Chase, Windup, Recover, Stagger, Dashing, Dying }

        private float _lastGrunt = -1f;

        private State _state = State.Spawning;
        private float _t = 0.45f, _attackCd, _ghostTick;
        private bool _enrageAnnounced, _clonesSpawned, _waterRaised;
        private bool _dashAttack, _dashHit;
        private Vector3 _dashDir = Vector3.forward;
        private Transform _player;
        private Player.PlayerLocomotion _playerMotor;
        private Player.CombatController _playerCombat;
        private Health _playerHealth;
        private SenGates _playerGates;
        private float _vertVel;
        private AttackTokenPool _tokens;
        private GameManager _gm;
        private CharacterRig _rig;
        private Transform _ring;
        private float _strafeSide = 1f;
        private float _flankAngle;
        private float _cloneTellT;
        private bool _cloneTelling;
        private int _flurryLeft;
        private float _flurryT;
        private float _erraticT;
        private float _guardT;   // samurai parry stance
        private float _guardBreakT, _postureIdleT;
        private bool _spinCleave;   // Goro's enraged 360
        private float _spitCd;      // Kagachi's venom

        // ---- behaviour ----------------------------------------------------
        private int _staggerCount;      // staggers inside the decay window
        private float _staggerWindowT;  // resets the count when it runs out
        private bool _wasGuarding, _wasProtecting; // rising-edge latches for telemetry
        private float _sidestepT;       // dodging a read heavy
        private Vector3 _sidestepDir;
        private float _retreatT;        // backing off to recover
        private float _readCd;          // one reaction per player commitment
        private int _blocksInRow;       // samurai: two blocks earn a riposte
        private float _defendT;         // guarding on low posture

        public static void ResetAiTelemetry() => AiTelemetry.Reset();

        /// <summary>Spear reach — the whole reason a pike guard is worth fielding.</summary>
        private const float SpearReach = 4f;
        private float _baseMaxHp, _baseDamage;
        private bool _statsCaptured;

        /// <summary>Body radius used to keep clear of cover circles.</summary>
        private const float BodyRadius = 0.45f;

        // Two shared telegraph materials instead of one per enemy: the ring only
        // ever shows "normal" or "dash/desperation", so the colour never needs to
        // be a per-instance value.
        private static Material _ringWhite, _ringRed;

        public bool Enraged => !isClone
            && (kind == EnemyKind.Chief && Hp < maxHp * 0.5f
                || kind == EnemyKind.Jin && Hp < maxHp * 0.4f);
        public int Phase => kind != EnemyKind.Kagachi || isClone ? 1
            : Hp > maxHp * 0.6f ? 1 : Hp > maxHp * 0.3f ? 2 : 3;

        /// <summary>Rank from the def when present, else the original kind list.</summary>
        private EnemyRank Rank => def != null ? def.rank
            : kind is EnemyKind.Chief or EnemyKind.Jin
              || (kind == EnemyKind.Kagachi && !isClone) ? EnemyRank.Boss : EnemyRank.Mook;

        private bool IsBoss => !isClone && Rank >= EnemyRank.MiniBoss;

        /// <summary>Public read for framing: bosses get a wider, lower camera.</summary>
        public bool IsBossTarget => IsBoss;

        /// <summary>
        /// One-line snapshot of what this enemy thinks it is doing. Used by the
        /// encounter harness to explain a fight that went quiet; cheap enough to
        /// leave in, and never called during normal play.
        /// </summary>
        public string DebugLine
        {
            get
            {
                var d = _player != null
                    ? Vector3.Distance(transform.position, _player.position) : -1f;
                var role = SquadCoordinator.Instance != null
                    ? SquadCoordinator.Instance.RoleOf(this).ToString() : "-";
                return $"{kind} ai={Ai} state={_state} role={role} d={d:0.0} " +
                       $"cd={_attackCd:0.00} unaware={Unaware} hp={Hp:0}/{maxHp:0} " +
                       $"posture={Posture:0}/{MaxPosture:0} range={(def != null ? def.MaxAttackRange : attackRange):0.0}";
            }
        }
        private float Speed => (Enraged || Phase == 3 ? speed * 1.4f : speed) * GameManager.EnemySpeedMul;
        private float Windup => _spinCleave ? SpinWindup
            : _dashAttack ? (kind == EnemyKind.Kagachi ? 0.55f : 0.7f)
            // The crossbow's charged shot is deliberately the slowest tell in the
            // game — it hits twice as hard, so it has to be walkable.
            : weapon == EnemyWeapon.Crossbow ? 1.2f
            : Enraged ? 0.45f : Phase == 3 ? 0.38f : windupTime;
        private float AttackDamage => Enraged ? 18f : Phase == 3 ? 16f : damage;
        /// <summary>Goro's ground slam — the one attack with an AoE worth reading.</summary>
        private bool IsSlam => kind == EnemyKind.Chief && !_dashAttack;

        /// <summary>
        /// Telegraph footprint per weapon: the ring should describe the threat, so
        /// a spear's reach and an axe's sweep read differently from a dagger jab
        /// before either of them lands.
        /// </summary>
        private float RingScale => _spinCleave ? 2.6f : IsSlam ? 2f : weapon switch
        {
            EnemyWeapon.Axe => 1.6f,
            EnemyWeapon.Spear => 1.5f,
            EnemyWeapon.Crossbow => 1.25f,
            EnemyWeapon.Bomb => 1.35f,
            EnemyWeapon.Daggers => 0.75f,
            _ => 1f,
        };

        /// <summary>Red means "this one hurts": heavy swings, charged shots, bombs.</summary>
        private bool RedTelegraph => _spinCleave || _dashAttack || IsSlam
                                     || weapon is EnemyWeapon.Axe or EnemyWeapon.Crossbow
                                         or EnemyWeapon.Bomb
                                     || (Phase == 3 && !isClone);

        private void OnEnable() { Active.Add(this); AllyDied += OnAllyDied; }
        private void OnDisable() { Active.Remove(this); AllyDied -= OnAllyDied; }

        /// <summary>
        /// Morale. A raider who sees a friend fall hesitates; a samurai closes;
        /// an assassin stops fighting fair and works round behind you. Which one
        /// is on the profile, not on the kind.
        /// </summary>
        private void OnAllyDied(EnemyBrain who)
        {
            if (who == this || Dead || Unaware || def == null || ActiveProfile == null) return;
            if (Vector3.Distance(who.transform.position, transform.position) > 12f) return;
            AiTelemetry.AllyReactions++;
            switch (ActiveProfile.allyDeath)
            {
                case AllyDeathReaction.Hesitate: _hesitateT = 1.1f; Intent = "hesitates"; break;
                case AllyDeathReaction.Aggress: _moraleAggroT = 7f; _decisionT = 0f; Intent = "avenges"; break;
                case AllyDeathReaction.Isolate: _isolateT = 5f; Intent = "isolates"; break;
            }
        }

        // Awake, not Start: the spawner applies its stat multipliers immediately
        // after Instantiate, so Start would capture already-boosted values.
        private void Awake()
        {
            ApplyDef();          // before the spawner scales anything
            CaptureBaseStats();
        }

        /// <summary>Copy the definition's stats onto the instance fields.</summary>
        /// <summary>
        /// Rebind this body to another def — a named foe on a common kind's
        /// model: the Pale Shade is a Shade with a boss's stats and a card, the
        /// Convoy Captain a ronin with a name. Call right after spawning, before
        /// the spawner scales anything.
        /// </summary>
        public void SetDef(EnemyDef d)
        {
            if (d == null) return;
            def = d;
            ApplyDef();
            Hp = maxHp;
        }

        private void ApplyDef()
        {
            if (def == null) return;
            kind = def.kind;
            weapon = def.weapon;
            maxHp = def.maxHp;
            speed = def.moveSpeed;
            attackRange = def.attackRange;
            damage = def.damage;
            windupTime = def.windupTime;
            spawnTime = def.spawnTime;
        }

        private void Start()
        {
            _rig = GetComponent<CharacterRig>();
            ApplyDef();
            BuildTelegraphRing();
            BindSceneRefs();
            Hp = maxHp;
            Posture = MaxPosture;
            _strafeSide = Random.value < 0.5f ? 1f : -1f;
            _flankAngle = RollFlankAngle();
            _t = spawnTime;
            if (kind == EnemyKind.Shade) Sfx3D.ShadeWhisper();
        }

        /// <summary>Each bandit commits to its own approach lane for the whole life.</summary>
        private static float RollFlankAngle() =>
            (Random.value < 0.5f ? 1f : -1f) * Random.Range(25f, 62f);

        /// <summary>Scene singletons come from the shared cache, not a per-enemy search.</summary>
        private void BindSceneRefs()
        {
            var motor = SceneRefs.Motor;
            if (motor != null)
            {
                _player = motor.transform;
                _playerMotor = motor;
                _playerCombat = motor.GetComponent<Player.CombatController>();
                _playerHealth = motor.GetComponent<Health>();
                _playerGates = motor.GetComponent<SenGates>();
            }
            _tokens = SceneRefs.Tokens;
            _gm = SceneRefs.Game;
        }

        /// <summary>
        /// Remembers the prefab's authored stats. GameManager multiplies maxHp and
        /// damage per spawn (duel floors, New Game+, Road North distance scaling);
        /// on a recycled instance those multipliers would compound without this.
        /// </summary>
        private void CaptureBaseStats()
        {
            if (_statsCaptured) return;
            _statsCaptured = true;
            _baseMaxHp = maxHp;
            _baseDamage = damage;
        }

        /// <summary>
        /// Re-fills health after the spawner has finished scaling maxHp. Recycled
        /// enemies are reset before those multipliers land, so without this a
        /// pooled boss would enter the fight already wounded.
        /// </summary>
        public void SyncHpToMax() => Hp = maxHp;

        /// <summary>Restores a pooled enemy to its just-instantiated state.</summary>
        public void ResetForSpawn()
        {
            _history.Clear();
            _decisionT = 0f; _feinted = false; _moraleAggroT = 0f; _hesitateT = 0f; _isolateT = 0f;
            _lowHealthReacted = false; Intent = "";
            BossDuel = false; PostureOverride = 0f; PostureRegenOverride = 0f; DuelResist = 1f;
            CaptureBaseStats();
            maxHp = _baseMaxHp;
            damage = _baseDamage;
            Hp = maxHp;
            Dead = false;
            _state = State.Spawning;
            _t = spawnTime;
            _attackCd = 0f;
            _ghostTick = 0f;
            _enrageAnnounced = _clonesSpawned = _waterRaised = false;
            _dashAttack = _dashHit = false;
            _dashDir = Vector3.forward;
            _vertVel = 0f;
            _strafeSide = Random.value < 0.5f ? 1f : -1f;
            _flankAngle = RollFlankAngle();
            _cloneTelling = false;
            _cloneTellT = 0f;
            _flurryLeft = 0;
            _flurryT = 0f;
            Posture = MaxPosture;
            _guardBreakT = 0f;
            _postureIdleT = 0f;
            _pattern = null;
            _guardT = 0f;
            _erraticT = 0f;
            _spinCleave = false;
            _spitCd = 0f;
            _staggerCount = 0; _staggerWindowT = 0f; _sidestepT = 0f; _retreatT = 0f;
            _readCd = 0f; _blocksInRow = 0; _defendT = 0f;
            Unaware = false;
            Detection = 0f;
            SetRing(false);
            BindSceneRefs(); // the scene may have reloaded since this one last died
            _rig?.ResetVisuals();
            _rig?.SetMood(RigMood.Calm);
            if (kind == EnemyKind.Shade) Sfx3D.ShadeWhisper();
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
            _spitCd = Mathf.Max(0, _spitCd - Time.deltaTime);
            _readCd = Mathf.Max(0, _readCd - Time.deltaTime);
            _moraleAggroT = Mathf.Max(0f, _moraleAggroT - Time.deltaTime);
            _hesitateT = Mathf.Max(0f, _hesitateT - Time.deltaTime);
            _isolateT = Mathf.Max(0f, _isolateT - Time.deltaTime);
            _decisionT = Mathf.Max(0f, _decisionT - Time.deltaTime);
            CheckLowHealth();
            if (_staggerWindowT > 0f && (_staggerWindowT -= Time.deltaTime) <= 0f) _staggerCount = 0;
            UpdatePosture(Time.deltaTime);
            UpdatePerception(Vector3.Distance(transform.position, _player.position));
            CheckPhaseTransitions();

            var to = _player.position - transform.position;
            to.y = 0;
            var dist = to.magnitude;
            if (_rig != null) _rig.move01 = 0f;

            if (Unaware)
            {
                UpdateUnaware(to, dist);
                ClampToArena();
                return;
            }

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
                    if (_sidestepT > 0f)
                    {
                        _sidestepT -= Time.deltaTime;
                        Move(_sidestepDir * 1.6f);
                        Face(to);
                        if (_rig != null) _rig.move01 = 1f;
                    }
                    // Dagger flurry: the second and third jabs land here, spaced out
                    // so the burst reads as three hits rather than one lump.
                    if (_flurryLeft > 0 && (_flurryT -= Time.deltaTime) <= 0f)
                    {
                        _flurryLeft--;
                        _flurryT = 0.17f;
                        _rig?.PlayOneShot(RigPose.Strike2, 0.22f);
                        Sfx3D.Slash();
                        if (dist < attackRange + 0.8f) DamagePlayer(AttackDamage * 0.45f);
                    }
                    // Shades slip away after striking; Jin does the same, which is
                    // what makes him a hit-and-run duelist rather than a brawler.
                    if (kind is EnemyKind.Shade or EnemyKind.Jin && dist > 0.5f)
                    {
                        Move(-to.normalized * 0.7f);
                        if (_rig != null) _rig.move01 = 0.7f;
                    }
                    if ((_t -= Time.deltaTime) <= 0) _state = State.Chase;
                    break;

                case State.Chase:
                {
                    // Reading the player comes before moving: a heavy telegraph
                    // is answered with a block or a sidestep, not by walking into it.
                    if (ReadPlayer(to, dist)) break;

                    // Hurt and out of guard: back off, let posture come back, then
                    // return. Retreat is a decision, not a panic.
                    if (_retreatT > 0f)
                    {
                        _retreatT -= Time.deltaTime;
                        Move(-to.normalized * 0.9f + Strafe(to) * 0.5f);
                        Face(to);
                        if (_rig != null) _rig.move01 = 0.8f;
                        break;
                    }
                    if (def != null && def.retreatBelowHp > 0f && Hp < maxHp * def.retreatBelowHp
                        && Posture < MaxPosture * 0.5f && _attackCd > 0.2f)
                    {
                        _retreatT = 1.6f;
                        AiTelemetry.Retreats++;
                        Ai = AiState.Retreat;
                        break;
                    }

                    // Low posture: guard up, give ground. React to what has been
                    // done to you instead of trading until the guard breaks.
                    if (def != null && def.guardsWhenPostureLow && Posture < MaxPosture * 0.34f
                        && _guardBreakT <= 0f && dist < attackRange + 1.5f)
                    {
                        _guardT = Mathf.Max(_guardT, 0.45f);
                        _defendT = 0.45f;
                        Ai = AiState.Defend;
                        if (!_wasGuarding) { AiTelemetry.GuardHolds++; _wasGuarding = true; }
                        Move(-to.normalized * 0.5f);
                        Face(to);
                        _rig?.ForcePose(RigPose.Windup, 0.5f);
                        break;
                    }

                    _wasGuarding = false;

                    Move(ChaseDir(to, dist));
                    Face(to);
                    if (_rig != null) _rig.move01 = 1f;
                    if (IsRanged && dist < AiTelemetry.ArcherMinDistance) AiTelemetry.ArcherMinDistance = dist;

                    var inRange = weapon == EnemyWeapon.Spear
                        ? dist < SpearReach && dist > 1.6f   // pikes fight at reach
                        : weapon == EnemyWeapon.Bomb
                        ? dist < 11f && dist > 4.5f          // bombers keep their distance
                        : kind == EnemyKind.Ranged
                        ? dist < 8f && dist > 4f
                          && !ArenaMarkers.Blocked(transform.position + Vector3.up, _player.position + Vector3.up)
                        : kind == EnemyKind.Chief
                            ? dist < 4.2f // slam range — the whole point of a greataxe
                            : dist < attackRange + 0.3f;
                    var wantsDash = IsBoss && dist > 5f && dist < 11f && _attackCd <= 0
                                    && Random.value < (Phase == 3 ? 0.03f
                                        : kind == EnemyKind.Jin ? 0.028f : 0.012f);

                    // Goro enraged: the axe comes round in a full circle. Big, slow
                    // and unmissable by footwork alone — the answer is i-frames.
                    _spinCleave = kind == EnemyKind.Chief && Enraged && !wantsDash
                                  && dist < 5f && _attackCd <= 0 && Random.value < 0.02f;
                    if (_spinCleave)
                    {
                        _state = State.Windup;
                        _t = SpinWindup;
                        SetRing(true);
                        _gm?.Announce("GORO WINDS UP");
                        break;
                    }

                    // Kagachi spits venom when he can't reach you — it slows, so it
                    // closes the gap for him instead of chasing.
                    if (kind == EnemyKind.Kagachi && !isClone && dist > 4.5f && dist < 12f
                        && _spitCd <= 0f)
                    {
                        _spitCd = 4f;
                        _dashDir = to.normalized;
                        _rig?.PlayOneShot(RigPose.Strike2, 0.4f);
                        Sfx3D.ShadeWhisper();
                        Projectile.Spawn(transform.position + Vector3.up * 1.3f + _dashDir * 0.6f,
                            _dashDir, damage * 0.5f, ProjectileKind.PoisonSpit);
                        _state = State.Recover;
                        _t = 0.7f;
                        break;
                    }
                    // Squad role: only enemies cleared to Engage may commit. The
                    // rest circle or hold, which is what stops ten enemies from
                    // attacking at once without freezing them in place.
                    var role = SquadCoordinator.Instance != null
                        ? SquadCoordinator.Instance.RoleOf(this) : SquadRole.Engage;
                    Ai = AiState.Combat;

                    // A committed, whiffed or just-dodged player is an opening, and
                    // an enemy built to punish takes it regardless of its squad
                    // job — but still through the token pool, so the cap on how
                    // many may swing at once holds even when everyone sees a gap.
                    // Choose the move *before* asking for a token. Taking one and
                    // then finding no usable attack still consumes the pool's grant
                    // window, which silently starves this enemy's ordinary attacks.
                    // Hesitation after a loss: guard up, no swing, for a beat.
                    if (_hesitateT > 0f)
                    {
                        _guardT = Mathf.Max(_guardT, 0.2f);
                        Move(-to.normalized * 0.3f);
                        Face(to);
                        _rig?.ForcePose(RigPose.Block, 0.5f);
                        break;
                    }

                    if (def != null && def.punishesExposure && _attackCd <= 0f && _readCd <= 0f
                        && PlayerExposed() && dist < def.MaxAttackRange + 0.4f
                        && Random.value < Difficulty.Now.RecoveryPunishChance)
                    {
                        var pick = Pick(to, dist, role, punish: true);
                        if (pick != null && pick.kind != AttackKind.Parry
                            && (IsBoss || _tokens == null || _tokens.TryTake(this)))
                        {
                            _readCd = 1.2f;
                            AiTelemetry.OutOfTurnPunishes++;
                            AiTelemetry.Attacks++;
                            Commit(pick, to, cold: false);
                            break;
                        }
                    }

                    // Protectors put themselves between the player and the archer.
                    // Assigned by the coordinator, so a bodyguard actually leaves
                    // the attack line to do it instead of only guarding when it
                    // happens to have nothing better on.
                    if (def != null && def.protectsRanged && role == SquadRole.Protect
                        && SquadCoordinator.Instance != null)
                    {
                        var ally = SquadCoordinator.Instance.NearestRangedAlly(this, 12f);
                        if (ally != null && Vector3.Distance(ally.transform.position, _player.position) < 6.5f)
                        {
                            var post = Vector3.Lerp(_player.position, ally.transform.position, 0.45f);
                            var toPost = post - transform.position;
                            toPost.y = 0f;
                            if (toPost.magnitude > 0.6f)
                            {
                                if (!_wasProtecting) { AiTelemetry.ProtectMoves++; _wasProtecting = true; }
                                Move(toPost.normalized);
                                Face(to);
                                if (_rig != null) _rig.move01 = 0.9f;
                                break;
                            }
                        }
                    }

                    _wasProtecting = false;

                    if (role == SquadRole.Wait && dist < 4.5f)
                    {
                        // Hold a readable distance, weapon up, doing nothing — and
                        // if there is cover within reach, hold it from behind that.
                        var toCover = CoverStep(to);
                        Move(toCover.sqrMagnitude > 0.01f ? toCover : -to.normalized * 0.35f);
                        Face(to);
                        if (_rig != null) _rig.move01 = 0.35f;
                        break;
                    }
                    if (role == SquadRole.Guard && dist < 3.6f)
                    {
                        // The wall: close, guard raised, absorbing attention so
                        // the flankers can work. Holds its ground, never swings.
                        _guardT = Mathf.Max(_guardT, 0.3f);
                        Move(dist > 2.4f ? to.normalized * 0.5f : Vector3.zero);
                        Face(to);
                        _rig?.ForcePose(RigPose.Windup, 0.4f);
                        break;
                    }
                    if (role == SquadRole.Circle)
                    {
                        Move(Strafe(to) * 1.1f + (dist > 5f ? to.normalized * 0.4f : Vector3.zero));
                        Face(to);
                        if (_rig != null) _rig.move01 = 0.8f;
                        break;
                    }
                    if (role == SquadRole.Reposition)
                    {
                        // Work round to the back hemisphere. This is what makes a
                        // circling pack feel like it is hunting a position rather
                        // than orbiting.
                        var rear = _player.position - _player.forward * 2.6f - transform.position;
                        rear.y = 0f;
                        if (rear.magnitude > 0.8f) { Move(rear.normalized); Face(to); }
                        else { Move(Strafe(to)); Face(to); }
                        if (_rig != null) _rig.move01 = 0.85f;
                        break;
                    }

                    // Isolating: work round to the back before committing.
                    if (_isolateT > 0f && !PlayerBackTurned(to))
                    {
                        var rearSpot = _player.position - _player.forward * 2.4f - transform.position;
                        rearSpot.y = 0f;
                        if (rearSpot.magnitude > 0.8f) { Move(rearSpot.normalized * 1.1f); Face(to); if (_rig != null) _rig.move01 = 1f; break; }
                    }

                    // Data-driven enemies pick from their own moveset — on a
                    // cadence, against the situation, never per frame.
                    if (def != null && _attackCd <= 0f && !wantsDash && _decisionT <= 0f)
                    {
                        _decisionT = DecisionInterval;
                        var pick = Pick(to, dist, role, punish: false);
                        if (pick != null && (IsBoss || _tokens == null || _tokens.TryTake(this)))
                        {
                            AiTelemetry.Attacks++;
                            Commit(pick, to, cold: true);
                            break;
                        }
                    }

                    if ((inRange && _attackCd <= 0 || wantsDash)
                        && (IsBoss || _tokens == null || _tokens.TryTake(this)))
                    {
                        AiTelemetry.Attacks++;
                        _dashAttack = wantsDash;
                        _state = State.Windup;
                        _t = Windup;
                        SetRing(true);
                    }
                    break;
                }

                case State.Windup:
                {
                    var total = _windupTotal > 0f ? _windupTotal : Windup;
                    // Tracking: an attack turns toward the player only as much as
                    // its definition allows; the dash aim locks in the last third.
                    var track = _pattern != null ? _pattern.tracking : 1f;
                    if (!(_dashAttack && _t < total * 0.35f) && track > 0f)
                    {
                        if (track >= 1f) Face(to);
                        else transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(to.normalized), track * 6f * Time.deltaTime);
                        _dashDir = dist > 0.5f ? to.normalized : transform.forward;
                    }
                    // The feint: at the cancel point, the heavy becomes a step
                    // back and a quick follow-up. Once per wind-up, readable by
                    // its own pose and sound, paid for with a cooldown.
                    if (_pattern != null && _pattern.category == AttackCategory.Feint && !_feinted
                        && _t <= total * 0.5f)
                    {
                        _feinted = true;
                        Sfx3D.Cloth(transform.position, 0.6f);
                        _rig?.PlayOneShot(RigPose.Backstep, 0.25f);
                        Intent = "feint → " + (FollowUpFor(_pattern)?.id ?? "quick");
                        var follow = FollowUpFor(_pattern);
                        if (follow != null) { _pattern = follow; _dashAttack = follow.kind == AttackKind.DashStrike; }
                        _t = Mathf.Max(0.3f, 0.34f);
                        _windupTotal = _t;
                        _attackCd += 1.5f; // feints are not free
                        SetRing(true);
                        break;
                    }
                    _rig?.ForcePose(WindupPose(), 1f - _t / total);
                    if (_ring != null) SetRingRadius(Mathf.Lerp(2.6f, 1.3f, 1f - _t / total));
                    if ((_t -= Time.deltaTime) <= 0)
                    {
                        SetRing(false);
                        _windupTotal = 0f;
                        ResolveAttack(dist);
                    }
                    break;
                }

                case State.Dashing:
                    _rig?.ForcePose(RigPose.Dash, 0.5f);
                    // Jin's storm dash leaves a lightning trail of after-images —
                    // and the charged air behind him keeps biting for a second.
                    if (kind == EnemyKind.Jin && (_ghostTick -= Time.deltaTime) <= 0f)
                    {
                        _ghostTick = 0.06f;
                        _rig?.SpawnAfterImage();
                        StormTrail.Spawn(transform.position, AttackDamage * 0.35f);
                    }
                    transform.position += _dashDir * ((kind == EnemyKind.Kagachi ? 15f : 13f) * Time.deltaTime);
                    if (!_dashHit && Vector3.Distance(transform.position, _player.position) < 1.4f)
                    {
                        _dashHit = true;
                        DamagePlayer(AttackDamage + 3f, heavy: true);
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
                    if ((_t -= Time.deltaTime) <= 0) EnemyPool.Release(this);
                    return;
            }

            UpdateLaunch(Time.deltaTime);
            ClampToArena();
        }

        /// <summary>
        /// Stealth idle: stands its ground and sweeps its gaze, building detection
        /// while the player is inside the vision cone with a clear line. Cover and
        /// distance both buy time — closing in is what gets you seen.
        /// </summary>
        private void UpdateUnaware(Vector3 to, float dist)
        {
            // Movement/gaze comes from the AI ladder; the sight test itself is
            // cached by the sliced perception pass rather than run here.
            UpdateUnawareAi(to, dist);
            var seen = _canSeePlayer;

            if (seen)
            {
                // Closer reads faster: point-blank is near-instant, edge of the
                // cone gives you a beat to break the line.
                var closeness = 1f - Mathf.Clamp01(dist / VisionRange);
                // Crouching, smoke and shadow all slow the read; lamplight speeds it.
                var vis = Visibility.Of(_player.position,
                    _playerMotor != null && _playerMotor.Crouched);
                Detection += (0.35f + closeness) * vis * Time.deltaTime;
                if (Detection > 0.25f && Ai is AiState.Idle or AiState.Patrol)
                    Ai = AiState.Suspicious;
                if (Detection >= 1f)
                {
                    Detection = 1f;
                    _gm?.RaiseAlarm(this);
                }
            }
            else Detection = Mathf.Max(0f, Detection - 0.55f * Time.deltaTime);
        }

        private const float VisionRange = 8.5f;
        private const float VisionConeDeg = 95f;

        // ------------------------------------------------- perception (sliced)

        /// <summary>
        /// Perception is the expensive part of AI: cone tests, line-of-sight
        /// segment checks against every cover circle, noise scans. Running it for
        /// every enemy every frame is what makes mobile AI cost real money, so
        /// each enemy re-perceives on its own slot in a rotating budget and acts
        /// on the cached result in between.
        /// </summary>
        private const float PerceiveInterval = 0.2f;

        private float _perceiveT;
        private bool _canSeePlayer;
        private float _suspicion;      // 0..1 below the Detection threshold
        private float _searchT;
        private Vector3 _investigatePoint;

        private void UpdatePerception(float dist)
        {
            // Stagger the first tick by instance so a wave that spawns together
            // does not all re-perceive on the same frame.
            if (_perceiveT <= 0f) _perceiveT = PerceiveInterval * (Mathf.Abs(GetHashCode()) % 7) / 7f;
            if ((_perceiveT -= Time.deltaTime) > 0f) return;
            _perceiveT = PerceiveInterval;

            var eye = transform.position + Vector3.up;
            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;

            var crouched = _playerMotor != null && _playerMotor.Crouched;
            var visible = Visibility.Of(_player.position, crouched);

            // Sight: cone, range scaled by how visible they are, and a clear line.
            _canSeePlayer = dist < VisionRange * visible
                            && Vector3.Angle(transform.forward, toPlayer) < VisionConeDeg * 0.5f
                            && !ArenaMarkers.Blocked(eye, _player.position + Vector3.up);

            if (_canSeePlayer) LastKnown = _player.position;

            if (!Unaware) return;   // aware enemies use sight only for LastKnown

            // Hearing: a sound gives a position, not an identity — worth walking to.
            if (!_canSeePlayer && NoiseSystem.Hear(transform.position, out var heard))
            {
                _investigatePoint = heard;
                _suspicion = Mathf.Min(1f, _suspicion + 0.5f);
                if (Ai is AiState.Idle or AiState.Patrol) Ai = AiState.Investigate;
            }

            // Bodies: finding one is as good as being seen.
            if (BodyWatch.Spot(eye, VisionRange * 0.8f, out var body))
            {
                _investigatePoint = body;
                Ai = AiState.Investigate;
                _suspicion = 1f;
                _gm?.Announce("A BODY IS FOUND");
            }
        }

        /// <summary>
        /// Unaware behaviour, now a real ladder: stand a beat, sweep the gaze,
        /// walk to what you heard, search around it, and only then give up.
        /// </summary>
        private void UpdateUnawareAi(Vector3 to, float dist)
        {
            switch (Ai)
            {
                case AiState.Investigate:
                {
                    var toPoint = _investigatePoint - transform.position;
                    toPoint.y = 0f;
                    if (toPoint.magnitude > 1.2f)
                    {
                        Move(toPoint.normalized * 0.65f);
                        Face(toPoint);
                        if (_rig != null) _rig.move01 = 0.6f;
                    }
                    else
                    {
                        Ai = AiState.Search;
                        _searchT = 3f;
                    }
                    break;
                }

                case AiState.Search:
                    // Sweep on the spot, then stand down if nothing turns up.
                    transform.Rotate(0f, 95f * Time.deltaTime * _strafeSide, 0f);
                    _rig?.ForcePose(RigPose.Idle, 0f);
                    if ((_searchT -= Time.deltaTime) <= 0f)
                    {
                        Ai = AiState.Patrol;
                        _suspicion = 0f;
                    }
                    break;

                default:
                    // Idle/Patrol: the original slow gaze sweep.
                    transform.Rotate(0f, 28f * Time.deltaTime * _strafeSide, 0f);
                    _rig?.ForcePose(RigPose.Idle, 0f);
                    break;
            }
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
            // The split used to happen the instant Kagachi crossed the phase line,
            // which read as a cheat. It now telegraphs: he coils, holds still and
            // rings red for a beat — a punish window that also sells the moment.
            if (kind == EnemyKind.Kagachi && !isClone && Phase >= 2 && !_clonesSpawned
                && !_cloneTelling)
            {
                _cloneTelling = true;
                _cloneTellT = 1.1f;
                _gm?.Announce("THE SERPENT COILS…");
                Sfx3D.ShadeWhisper();
                _rig?.PlayOneShot(RigPose.Taunt, 1.0f);
                _dashAttack = true; // red telegraph ring
                SetRing(true);
                // Recover, not Stagger: Stagger force-poses Hurt every frame and
                // would eat the coil animation. Recover just waits, so the taunt
                // plays — and he still can't act, which is the punish window.
                _state = State.Recover;
                _t = 1.1f;
                FxPools.Embers(transform.position + Vector3.up, 14);
                return;
            }
            if (_cloneTelling)
            {
                if ((_cloneTellT -= Time.deltaTime) > 0f) return;
                _cloneTelling = false;
                _dashAttack = false;
                SetRing(false);
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

        /// <summary>Goro's spin telegraph: long enough to read, short enough to fear.</summary>
        private const float SpinWindup = 0.95f;
        private const float SpinRadius = 5f;

        /// <summary>
        /// The enraged greataxe spin: full 360° at melee range. There is no safe
        /// side to stand on, so the only clean answer is to flicker through it.
        /// </summary>
        private void ResolveSpinCleave()
        {
            _spinCleave = false;
            SetRing(false);
            _rig?.PlayOneShot(RigPose.Cleave, 0.6f);
            Sfx3D.HitCrush();
            UI.FxPools.Nova(transform.position);
            UI.FxPools.Embers(transform.position + Vector3.up, 30);
            SceneRefs.Rig?.Shake(11f, 0.45f);
            if (_player != null
                && Vector3.Distance(_player.position, transform.position) <= SpinRadius)
                DamagePlayer(AttackDamage + 8f, heavy: true);
            _attackCd = 2.4f;
            _state = State.Recover;
            _t = 1f;
        }

        // ------------------------------------------------- weapon resolvers

        /// <summary>
        /// Daggers: three quick jabs instead of one committed swing. Individually
        /// cheap, but they punish standing still and they beat a single dodge.
        /// The follow-ups are delivered from Recover so they land over time.
        /// </summary>
        private void ResolveFlurry(float dist)
        {
            _rig?.PlayOneShot(RigPose.Strike1, 0.3f);
            Sfx3D.Slash();
            if (dist < attackRange + 0.6f) DamagePlayer(AttackDamage * 0.45f);
            _flurryLeft = 2;      // two more to come
            _flurryT = 0.17f;
            _attackCd = 1.5f;
            _state = State.Recover;
            _t = 0.8f;
        }

        /// <summary>
        /// Spear: a long, narrow lunge. It out-ranges everything else on the field
        /// but only along the line it is pointing, so stepping aside beats it.
        /// </summary>
        private void ResolveThrust(float dist)
        {
            _rig?.PlayOneShot(RigPose.Strike2, 0.4f);
            Sfx3D.Slash();
            var to = _player.position - transform.position;
            to.y = 0f;
            // Narrow cone along the aim: reach is the trade for coverage.
            if (to.magnitude <= SpearReach && Vector3.Angle(transform.forward, to) <= 22f)
                DamagePlayer(AttackDamage + 2f);
            // Visible commitment: the guard steps into the thrust.
            transform.position = ArenaMarkers.Resolve(
                transform.position + transform.forward * 1.1f, BodyRadius);
            UI.FxPools.Slash(transform.position + Vector3.up * 1.1f + transform.forward * 1.6f,
                transform.forward, false);
            _attackCd = 1.9f;
            _state = State.Recover;
            _t = 0.9f;
        }

        /// <summary>Bomber: lobs a charge at the player's feet and denies the ground.</summary>
        private void ResolveThrowBomb(float dist)
        {
            _rig?.PlayOneShot(RigPose.Strike2, 0.45f);
            if (!BlindedMiss())
            {
                var target = _player.position;
                target.y = 0f;
                EnemyBomb.Spawn(transform.position + Vector3.up * 1.2f, target, AttackDamage);
            }
            _attackCd = 3.2f;
            _state = State.Recover;
            _t = 1f;
        }

        /// <summary>
        /// Charged shot: the long red windup has already run by the time this
        /// fires, so the bolt hits for double. Shared by the archer and any other
        /// crossbow user.
        /// </summary>
        private void ResolveChargedShot()
        {
            _rig?.PlayOneShot(RigPose.Strike2, 0.4f);
            // Archers in smoke loose the bolt anyway — it just goes nowhere.
            if (!BlindedMiss())
                Projectile.Spawn(transform.position + Vector3.up * 1.1f + _dashDir * 0.6f,
                    _dashDir, damage * 2f);
            _attackCd = 2.6f;
            _state = State.Recover;
            _t = 0.85f;
        }

        /// <summary>The pattern chosen when this windup started (def-driven enemies).</summary>
        private AttackDefinition _pattern;

        // ---- Combat 2.0: how this enemy decides
        private readonly EnemyAttackSelector _selector = new();
        private readonly EnemyAttackHistory _history = new();
        private readonly EnemyCombatMemory _memory = new();
        private float _decisionT;          // cadence: the selector never runs per frame
        private bool _feinted;             // this wind-up already cancelled once
        private float _windupTotal;        // the full wind-up, for the feint point and the pose
        private float _moraleAggroT;       // ally death / low health: aggressive for a while
        private float _hesitateT;          // ally death: a beat of doubt
        private float _isolateT;           // assassins: work round behind after a loss
        private bool _lowHealthReacted;
        private bool _lastHitLanded;
        private Vector3 _lastPlayerPos;
        private float _playerLateral;      // how much the player moved sideways since the last decision

        /// <summary>Raised when any enemy dies; the survivors decide what it means to them.</summary>
        public static event System.Action<EnemyBrain> AllyDied;

        /// <summary>For the debug overlay and the tests: the last decision this enemy made.</summary>
        public EnemyCombatDecision LastDecision => _selector.Last;
        public ObservedPlayerState LastObserved => _selector.LastState;
        public string Intent { get; private set; } = "";
        public EnemyAttackHistory History => _history;

        /// <summary>
        /// Executes one entry of the shared attack vocabulary. Every enemy composes
        /// its kit from these, so a new moveset never needs new code here.
        /// </summary>
        private void ResolvePattern(AttackDefinition p, float dist)
        {
            _lastHitLanded = false;
            var dmg = AttackDamage * p.damageMultiplier;
            switch (p.kind)
            {
                case AttackKind.Flurry: ResolveFlurry(dist); return;
                case AttackKind.Thrust: ResolveThrust(dist); return;
                case AttackKind.ThrowBomb: ResolveThrowBomb(dist); return;
                case AttackKind.ChargedShot: ResolveChargedShot(); return;
                case AttackKind.SpinCleave: ResolveSpinCleave(); return;

                case AttackKind.QuickShot:
                    _rig?.PlayOneShot(RigPose.Strike2, 0.3f);
                    if (!BlindedMiss())
                        Projectile.Spawn(transform.position + Vector3.up * 1.1f + _dashDir * 0.6f,
                            _dashDir, dmg);
                    break;

                case AttackKind.PoisonSpit:
                    _rig?.PlayOneShot(RigPose.Strike2, 0.4f);
                    Sfx3D.ShadeWhisper();
                    if (!BlindedMiss())
                        Projectile.Spawn(transform.position + Vector3.up * 1.3f + _dashDir * 0.6f,
                            _dashDir, dmg, ProjectileKind.PoisonSpit);
                    break;

                case AttackKind.HeavySlam:
                    _rig?.PlayOneShot(RigPose.Strike1, 0.5f);
                    Sfx3D.HitCrush();
                    UI.FxPools.Nova(transform.position);
                    SceneRefs.Rig?.Shake(8f, 0.35f);
                    SlowZone.Spawn(transform.position, 3.4f, 4.5f);
                    if (dist < 3.4f) DamagePlayer(dmg + 4f, heavy: true);
                    break;

                case AttackKind.DashStrike:
                    _dashAttack = true;
                    _state = State.Dashing;
                    _t = 0.45f;
                    _dashHit = false;
                    return;

                case AttackKind.Parry:
                    // Guard stance: the punish lives in TakeHit via blockChance.
                    _rig?.ForcePose(RigPose.Block, 0.5f);
                    _guardT = 1.1f;
                    break;

                case AttackKind.Sweep:
                {
                    // Wide and shallow: it owns the half-circle in front, so a
                    // player circling at reach is caught where a slash would miss.
                    _rig?.PlayOneShot(RigPose.Sweep, 0.5f);
                    Sfx3D.Whoosh(transform.position + transform.forward + Vector3.up, heavy: true);
                    var toP = _player.position - transform.position; toP.y = 0f;
                    if (toP.magnitude < p.maxRange + 0.5f && Vector3.Angle(transform.forward, toP) <= 80f)
                        DamagePlayer(dmg);
                    UI.FxPools.Slash(transform.position + Vector3.up * 1.1f + transform.forward * 1.2f, transform.forward, true);
                    break;
                }

                case AttackKind.GuardBreak:
                {
                    // Goes through a raised guard and opens it; on an open player
                    // it is a heavy, slower hit. Kick pose: it must not read as a cut.
                    _rig?.PlayOneShot(RigPose.Kick, 0.5f);
                    Sfx3D.ImpactAt(transform.position + Vector3.up, Sfx3D.ImpactKind.Heavy, 0.8f);
                    if (dist < p.maxRange + 0.6f)
                    {
                        if (_playerCombat != null && _playerCombat.Deflecting && !(_playerMotor != null && _playerMotor.Invulnerable)
                            && !ArenaMarkers.BlockedBetween(transform.position + Vector3.up, _player.position + Vector3.up))
                        {
                            _playerCombat.OnGuardBroken();
                            AiTelemetry.GuardBreaksLanded++;
                            _lastHitLanded = true;
                        }
                        else DamagePlayer(dmg * 0.85f, heavy: true);
                    }
                    break;
                }

                case AttackKind.RetreatSlash:
                {
                    // Cut and go: the strike lands as the enemy steps out, so the
                    // player who chases it walks into the next thing.
                    _rig?.PlayOneShot(RigPose.Strike2, 0.35f);
                    Sfx3D.Slash();
                    if (dist < p.maxRange + 0.5f) DamagePlayer(dmg);
                    var toP = _player.position - transform.position; toP.y = 0f;
                    _sidestepDir = -toP.normalized;
                    _sidestepT = 0.45f;
                    _rig?.PlayOneShot(RigPose.Backstep, 0.4f);
                    break;
                }

                default: // Slash
                    _rig?.PlayOneShot(RigPose.Strike1, 0.45f);
                    Sfx3D.Slash();
                    if (dist < p.maxRange + 0.6f) DamagePlayer(dmg);
                    break;
            }
            _attackCd = p.cooldown;
            _state = State.Recover;
            // Recovery is the attack's own now; a definition that does not say
            // recovers exactly as its kind always did. A heavy that missed is
            // the opening the whole system promises: it recovers half again as
            // long, and the player is told.
            _t = p.RecoveryFor(p.kind);
            if (!_lastHitLanded && p.category is AttackCategory.Heavy or AttackCategory.GuardBreak or AttackCategory.Sweep)
            {
                _t *= 1.6f;
                AiTelemetry.MissedHeavies++;
                Intent = "missed — open";
                UI.FloatingText.Spawn(transform.position + Vector3.up * 2.3f, "OPEN",
                    new Color(1f, 0.8f, 0.5f), 0.9f);
            }
            _lastHitLanded = false;
            if (_mistakeRecovery > 0f) { _t *= _mistakeRecovery; _mistakeRecovery = 0f; }
        }

        private void ResolveAttack(float dist)
        {
            // Def-driven enemies run their chosen pattern; everything else keeps
            // the original weapon/kind resolution below.
            if (_pattern != null)
            {
                var p = _pattern;
                _pattern = null;
                ResolvePattern(p, dist);
                return;
            }
            if (_spinCleave) { ResolveSpinCleave(); return; }
            if (_dashAttack)
            {
                _state = State.Dashing;
                _t = 0.45f;
                _dashHit = false;
                return;
            }
            // Weapon decides the attack. Kind-specific set pieces (Goro's slam)
            // still win, but everything else now comes from what it's holding.
            switch (weapon)
            {
                case EnemyWeapon.Daggers: ResolveFlurry(dist); return;
                case EnemyWeapon.Spear: ResolveThrust(dist); return;
                case EnemyWeapon.Bomb: ResolveThrowBomb(dist); return;
                case EnemyWeapon.Crossbow when kind != EnemyKind.Ranged:
                    ResolveChargedShot(); return;
            }

            var player = _playerMotor;
            if (kind == EnemyKind.Ranged)
            {
                ResolveChargedShot();
            }
            else if (kind == EnemyKind.Chief && dist < 4.4f)
            {
                // Goro's telegraphed ground slam: shockwave AoE — dodge it or eat it.
                _rig?.PlayOneShot(RigPose.Strike1, 0.5f);
                Sfx3D.HitCrush();
                FxPools.Nova(transform.position);
                SceneRefs.Rig?.Shake(8f, 0.35f);
                // The slam scars the ground: standing in the crater costs you speed,
                // so it threatens the space and not just the instant.
                SlowZone.Spawn(transform.position, 3.4f, 4.5f);
                if (dist < 3.4f) DamagePlayer(AttackDamage + 4f, heavy: true);
                _attackCd = Enraged ? 1.0f : 1.8f;
            }
            else if (dist < attackRange + 0.6f)
            {
                _rig?.PlayOneShot(RigPose.Strike1, 0.45f);
                Sfx3D.Slash();
                DamagePlayer(AttackDamage);
                _attackCd = Enraged ? 0.8f : Phase == 3 ? 0.7f : kind == EnemyKind.Shade ? 2.0f : 1.5f;
            }
            _state = State.Recover;
            _t = kind == EnemyKind.Shade ? 1.1f : 0.75f;
        }

        /// <summary>
        /// Single path for hurting the player: i-frames beat everything, then the
        /// deflect stance, then the hit lands. Collapsed from three copies so a new
        /// defensive option only has to be taught to one place.
        /// </summary>
        /// <summary>
        /// Half of everything swung inside a smoke cloud goes wide. Rolled per
        /// attack, so standing in the smoke is a gamble for them, not immunity.
        /// </summary>
        private bool BlindedMiss()
        {
            if (!Player.SmokeCloud.Inside(transform.position)) return false;
            if (Random.value >= 0.5f) return false;
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2.2f, "MISS",
                new Color(0.62f, 0.72f, 0.62f), 0.9f);
            return true;
        }

        private void DamagePlayer(float amount, bool heavy = false)
        {
            if (BlindedMiss()) return;
            // Nothing lands through cover. Perception and projectiles already
            // respected the arena's obstacles; melee and area attacks did not, so
            // a pike guard's 3.6m thrust reached straight through a chimney.
            // This is the single chokepoint every melee and AOE hit passes through.
            if (_player != null && ArenaMarkers.BlockedBetween(
                    transform.position + Vector3.up, _player.position + Vector3.up))
            {
                UI.FxPools.Sparks(transform.position + transform.forward * 1.2f + Vector3.up,
                    new Color(0.8f, 0.82f, 0.86f), 5);
                return;
            }
            if (_playerMotor != null && _playerMotor.Invulnerable)
            {
                _playerGates?.OnPerfectDodge();
                _gm?.OnPerfectDodge();
                return;
            }
            if (_playerCombat != null && _playerCombat.Deflecting)
            {
                var perfect = _playerCombat.PerfectWindow;
                _playerCombat.OnDeflect(this);
                if (perfect && _state != State.Dying)
                {
                    // Weapon recoil: the perfect parry throws the attacker's
                    // blade wide and leaves them open for real, not just poorer.
                    _pattern = null; _flurryLeft = 0; SetRing(false);
                    _state = State.Stagger;
                    _t = Mathf.Max(_t, 0.7f);
                    _rig?.PlayOneShot(RigPose.BlockHit, 0.6f);
                    AiTelemetry.ParryRecoils++;
                    Intent = "parried — open";
                }
                return;
            }
            _lastHitLanded = true;
            _playerCombat?.OnIncomingHit(heavy);
            var before = _playerHealth != null ? _playerHealth.Hp : 0f;
            _playerHealth?.Damage(amount, transform.position);
            CombatLog.EnemyHitPlayer(def, def != null ? def.id : kind.ToString(), amount, before, _playerHealth != null ? _playerHealth.Hp : 0f,
                heavy, _playerHealth != null ? _playerHealth.MaxHp : 0f);
        }

        /// <summary>
        /// Knocked airborne by the third strike. Mooks only — juggling a boss would
        /// skip the phase transitions the fight is built on.
        /// </summary>
        public void Launch(float upSpeed)
        {
            if (IsBoss || Dead) return;
            _vertVel = upSpeed;
            _state = State.Stagger;
            _t = Mathf.Max(_t, 0.45f);
            SetRing(false);
        }

        /// <summary>Ballistic arc for a launched enemy; lands back on the deck.</summary>
        private void UpdateLaunch(float dt)
        {
            if (!Launched) return;
            _vertVel += -22f * dt;
            var p = transform.position;
            p.y += _vertVel * dt;
            if (p.y <= 0f)
            {
                p.y = 0f;
                _vertVel = 0f;
                _t = Mathf.Max(_t, 0.25f); // brief flatten on landing
            }
            transform.position = p;
            if (_state != State.Dying) _state = State.Stagger; // stays helpless while up
        }

        /// <summary>
        /// Posture regenerates only after they have been left alone, so sustained
        /// pressure is what breaks a guard — not total damage over a long fight.
        /// </summary>
        private void UpdatePosture(float dt)
        {
            if (_guardBreakT > 0f)
            {
                _guardBreakT -= dt;
                if (_guardBreakT <= 0f)
                {
                    // Recovered: guard comes back at a third, not full, so a second
                    // break is achievable if you keep the pressure on.
                    Posture = MaxPosture * 0.34f;
                    _rig?.SetMood(RigMood.Enraged);
                }
                return;
            }
            if (_postureIdleT > 0f) { _postureIdleT -= dt; return; }
            var regen = PostureRegenOverride > 0f ? PostureRegenOverride
                : def != null ? def.postureRegen : 9f;
            Posture = Mathf.Min(MaxPosture, Posture + regen * dt);
        }

        /// <summary>
        /// Guard break: the payoff for sustained pressure. They drop, they stay
        /// down long enough to be punished, and they become executable.
        /// </summary>
        private void BreakGuard()
        {
            _guardBreakT = def != null ? def.guardBreakSeconds : 2.2f;
            Posture = 0f;
            _guardT = 0f;
            _pattern = null;
            _state = State.Stagger;
            _t = _guardBreakT;
            SetRing(false);
            Sfx3D.HitCrush();
            SceneRefs.Rig?.Shake(7f, 0.35f);
            UI.FxPools.Sparks(transform.position + Vector3.up * 1.3f,
                new Color(1f, 0.85f, 0.55f), 16);
            UI.FloatingText.Spawn(transform.position + Vector3.up * 2.5f, "GUARD BROKEN",
                new Color(1f, 0.8f, 0.4f), 1.25f);
            _rig?.PlayOneShot(RigPose.Hurt, 0.6f);
        }

        /// <summary>
        /// Applies one reaction. Different attacks must read differently — this is
        /// where a jab, a heavy and a finisher stop looking like the same hit.
        /// </summary>
        private void ApplyReaction(Player.HitReaction reaction, Vector3 from)
        {
            var away = transform.position - from;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;

            switch (reaction)
            {
                case Player.HitReaction.Launch:
                    Launch(6.5f);
                    transform.position += away * 0.55f;
                    break;

                case Player.HitReaction.Knockback:
                    _state = State.Stagger;
                    _t = StaggerLength(0.55f);
                    // Directional: shoved along the blow, not away from a point.
                    transform.position = ArenaMarkers.Resolve(
                        transform.position + away * 0.95f, BodyRadius);
                    _rig?.PlayOneShot(RigPose.Hurt, 0.4f);
                    break;

                case Player.HitReaction.GuardBreak:
                    BreakGuard();
                    break;

                case Player.HitReaction.Deflected:
                    break;

                default: // Flinch — keeps its feet, loses a beat
                    _state = State.Stagger;
                    _t = StaggerLength(0.38f);
                    transform.position = ArenaMarkers.Resolve(
                        transform.position + away * 0.3f, BodyRadius);
                    break;
            }
        }

        /// <summary>
        /// Diminishing stagger: each stagger inside a four-second window shortens
        /// the next, down to a floor. The first hit still buys the full opening;
        /// the fourth buys a flinch. This is what stops a mook being juggled to
        /// death without ever getting to act, while keeping the launcher and
        /// guard-break windows intact — those set their own durations.
        /// </summary>
        private float StaggerLength(float baseSeconds)
        {
            var decay = def != null ? def.staggerDecay : 0.65f;
            var n = _staggerCount++;
            _staggerWindowT = 4f;
            var len = baseSeconds * Mathf.Pow(decay, n);
            if (n > 0) AiTelemetry.StaggersShortened++;
            return Mathf.Max(0.12f, len);
        }

        /// <summary>Applies damage (with weak-point bonuses) and returns what was dealt.</summary>
        public float TakeHit(float amount, Vector3 from, bool crush = false, float postureMul = 1f)
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
            // Shades are made of the same stuff the marsh breathes out; smoke
            // pulls them apart twice as fast.
            if (Player.SmokeCloud.Inside(transform.position))
                amount *= def != null ? def.elementalMultiplier
                    : kind == EnemyKind.Shade ? 2f : 1f;
            // ---- defence layer (data-driven) --------------------------------
            if (def != null)
            {
                // Guard: a samurai in stance turns a light hit aside entirely.
                // A raised guard always blocks a light hit; a cold block is a roll.
                // Reading enemies raise it against the wind-up (ReadPlayer), so for
                // them the roll mostly never happens — the block was earned.
                if (!crush && (_guardT > 0f || Random.value < Mathf.Min(0.85f, def.blockChance * Difficulty.Now.DefenseScale)))
                {
                    _guardT = 0f;
                    _attackCd = Mathf.Min(_attackCd, 0.35f); // riposte window
                    _blocksInRow++;
                    // Riposte: the counter-attack a blocked swing invites. Two
                    // blocks in a row make it certain — repetition is punished.
                    if (_state is State.Chase or State.Recover
                        && (_blocksInRow >= 2 || Random.value < Mathf.Min(0.9f, def.counterChance * Difficulty.Now.DefenseScale)))
                    {
                        var toP = from - transform.position; toP.y = 0f;
                        var pick = Pick(toP, toP.magnitude, SquadRole.Engage, punish: true);
                        // Token last, for the same reason as the punish above.
                        if (pick != null && pick.kind != AttackKind.Parry
                            && (IsBoss || _tokens == null || _tokens.TryTake(this)))
                        {
                            AiTelemetry.Ripostes++;
                            AiTelemetry.Attacks++;
                            _blocksInRow = 0;
                            Commit(pick, toP, cold: false, windupOverride: 0.32f); // fast, but a visible tell
                        }
                    }
                    UI.FloatingText.Spawn(transform.position + Vector3.up * 2.2f, "BLOCK",
                        new Color(0.72f, 0.8f, 0.95f), 0.95f);
                    UI.FxPools.Sparks(transform.position + Vector3.up * 1.2f,
                        new Color(0.85f, 0.9f, 1f), 8);
                    Sfx3D.ImpactAt(transform.position + Vector3.up * 1.2f,
                        Sfx3D.ImpactKind.Guard, 1f);
                    _rig?.Flash();
                    return 0f;
                }

                // Weakness multipliers: what this enemy is soft to.
                var fromBehind = (from - transform.position).sqrMagnitude > 0.01f
                                 && Vector3.Angle(transform.forward, from - transform.position) > 120f;
                if (fromBehind) amount *= def.backstabMultiplier;
                if (crush) amount *= def.crushMultiplier;

                // Armour is flat and applied last, so heavy foes shrug off chip
                // damage without becoming immune to real hits.
                amount = Mathf.Max(1f, amount - def.armor);
            }
            // Grunt on damage that actually lands, throttled so a flurry does not
            // stack four voices on the same body.
            if (Time.time - _lastGrunt > 0.35f)
            {
                _lastGrunt = Time.time;
                Sfx3D.EnemyVoice(transform.position + Vector3.up * 1.4f, Sfx3D.Voice.Hurt);
            }

            _blocksInRow = 0;
            // Being hit is the loudest tell there is.
            if (Unaware) { Unaware = false; Detection = 0f; }
            LastHitTime = Time.unscaledTime; // unscaled: hit-stop must not stall the bar
            // Posture, not HP, is how a duel is won: outside the guard-break
            // window a duel boss shrugs most of a swing's HP damage (it went
            // into their guard), and the broken-guard window is where the real
            // HP punish lands. One or the other, never both.
            if (_guardBreakT > 0f) amount *= 1.5f;
            else if (BossDuel) amount *= DuelResist;
            var hpBefore = Hp;
            Hp -= amount;
            CombatLog.PlayerHit(def, def != null ? def.id : kind.ToString(), amount, hpBefore, Hp, Posture, crush, maxHp);
            _rig?.Flash();
            if (Hp <= 0)
            {
                Hp = 0;
                Dead = true;
                _state = State.Dying;
                _t = 0.7f;
                // Drop a juggled body to the deck rather than dying mid-air.
                _vertVel = 0f;
                var landed = transform.position;
                landed.y = 0f;
                transform.position = landed;
                SetRing(false);
                Sfx3D.Death();
                Sfx3D.EnemyVoice(transform.position + Vector3.up, Sfx3D.Voice.Death);
                Ai = AiState.Dead;
                // Leaving bodies where they can be seen has a cost now.
                BodyWatch.Report(transform.position);
                AllyDied?.Invoke(this);
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
            // Bosses armour through every light hit, not just mid-windup — chip
            // damage should never stunlock them out of their own fight. Crushes
            // still land. Mooks stagger longer now, which is what makes a chain
            // into a crush feel like a commitment rather than a formality.
            // ---- posture ----------------------------------------------------
            // Every hit chips the guard; a crush chips it hard. Break it and the
            // fight opens up. This is what stops an enemy from calmly trading HP.
            _postureIdleT = def != null ? def.postureRegenDelay : 1.6f;
            if (_guardBreakT <= 0f)
            {
                Posture -= (crush ? amount * 1.9f : amount) * postureMul;
                if (Posture <= 0f)
                {
                    if (_state == State.Windup) SetRing(false);
                    ApplyReaction(Player.HitReaction.GuardBreak, from);
                    return amount;
                }
            }

            // Poise: heavy enemies keep their feet through light hits — but a
            // hit from behind throws anyone.
            var struckFromBehind = def != null && (from - transform.position).sqrMagnitude > 0.01f
                && Vector3.Angle(transform.forward, from - transform.position) > 120f;
            var poised = def != null && !crush && !struckFromBehind && Random.value < def.poise;
            if (_state != State.Dashing && (crush || !IsBoss) && !poised)
            {
                if (_state == State.Windup) SetRing(false);
                // A broken guard, a crush and a jab must not read the same.
                ApplyReaction(_guardBreakT > 0f ? Player.HitReaction.Knockback
                    : crush || struckFromBehind ? Player.HitReaction.Knockback
                    : Player.HitReaction.Flinch, from);
            }
            return amount;
        }

        // ------------------------------------------------ Combat 2.0 decisions

        /// <summary>
        /// The personality in force right now. Bosses change theirs by phase:
        /// Goro measured then enraged, Kagachi swordsman → warlord → the marsh
        /// itself → the exhausted duel. HP thresholds pick the phase; the
        /// profile decides what the phase *does*.
        /// </summary>
        public EnemyCombatProfile ActiveProfile
        {
            get
            {
                if (def == null) return null;
                var hp01 = maxHp > 0f ? Hp / maxHp : 1f;
                if (def.phase4Profile != null && hp01 <= 0.15f) return def.phase4Profile;
                if (def.phase3Profile != null && hp01 <= 0.4f) return def.phase3Profile;
                if (def.phase2Profile != null && (hp01 <= 0.65f || Enraged)) return def.phase2Profile;
                return def.profile;
            }
        }

        private EnemyCombatProfile _lastProfile;

        private float DecisionInterval =>
            (ActiveProfile != null ? ActiveProfile.decisionInterval : 0.25f)
            * Difficulty.Now.DecisionScale * (_moraleAggroT > 0f ? 0.7f : 1f);

        /// <summary>
        /// Choose an attack for the situation. Profiled enemies score their kit
        /// against what they can see; the rest keep the old weighted draw.
        /// </summary>
        private AttackDefinition Pick(Vector3 to, float dist, SquadRole role, bool punish)
        {
            if (def == null) return null;
            var profile = ActiveProfile;
            if (profile == null) return def.ChooseAttack(dist);
            if (profile != _lastProfile)
            {
                // A phase turned over: the history is a different man's now.
                if (_lastProfile != null) { _history.Clear(); AiTelemetry.PhaseChanges++; Intent = $"phase: {profile.id}"; }
                _lastProfile = profile;
            }

            var ctx = new EnemyAttackSelector.Context
            {
                distance = dist,
                relativeAngle = Vector3.Angle(transform.forward, to),
                playerBackTurned = PlayerBackTurned(to),
                state = Observe(),
                alliesNear = AlliesNear(6f),
                othersAttacking = OthersAttacking(),
                playerSurrounded = EnemiesNearPlayer(4f) >= 3,
                hasToken = true,
                role = role,
                hp01 = maxHp > 0f ? Hp / maxHp : 1f,
                posture01 = Posture01,
                playerMemory = _playerCombat != null ? _playerCombat.Memory : null,
                lastAttackId = _history.LastId,
            };
            var pick = _selector.Choose(def, profile, _history, _memory, in ctx, punish ? 0f : _attackCd);
            if (pick != null) Intent = $"{pick.category}: {pick.id} vs {ctx.state}";
            return pick;
        }

        /// <summary>Start the chosen attack's wind-up. One place, so every path agrees.</summary>
        private float _mistakeRecovery;

        private void Commit(AttackDefinition pick, Vector3 to, bool cold, float windupOverride = 0f)
        {
            // Difficulty's mistake axis. Harder enemies err less; none is
            // perfect. A mistake reads as human — a beat of hesitation, or an
            // over-committed heavy that leaves a real opening — not random noise.
            if (cold && Random.value < Difficulty.Now.MistakeChance)
            {
                AiTelemetry.Mistakes++;
                if (Random.value < 0.5f)
                {
                    Intent = "hesitates (mistake)";
                    _attackCd = Mathf.Max(_attackCd, 0.6f + Random.value * 0.5f);
                    _decisionT = _attackCd;
                    return;
                }
                Intent = "overcommits (mistake)";
                windupOverride = (windupOverride > 0f ? windupOverride
                    : pick.windupOverride > 0f ? pick.windupOverride : Windup) * 1.3f;
                _mistakeRecovery = 1.5f;
            }
            _pattern = pick;
            _dashAttack = pick.kind == AttackKind.DashStrike;
            _feinted = false;
            _history.Record(pick, Time.time);
            AiTelemetry.Committed(pick);
            var windup = windupOverride > 0f ? windupOverride
                : pick.windupOverride > 0f ? pick.windupOverride : Windup;
            if (!cold) windup = Mathf.Max(0.3f, windup * 0.7f);          // punishes are faster, never unreadable
            if (pick.category == AttackCategory.Delayed) windup *= 1.55f; // the held beat is the point
            if (_moraleAggroT > 0f) windup = Mathf.Max(0.3f, windup * 0.85f);
            _t = windup;
            _windupTotal = windup;
            _state = State.Windup;
            _lastPlayerPos = _player != null ? _player.position : _lastPlayerPos;
            SetRing(true);
            TelegraphCue(pick);
        }

        /// <summary>
        /// The sound of the wind-up says what is coming. Heavy: the deep swing.
        /// Guard-break: a low impact, not a blade. Feint: cloth, nothing more.
        /// Delayed: breath held. Thrust: the whoosh, thin. Ranged: the draw.
        /// </summary>
        private void TelegraphCue(AttackDefinition a)
        {
            var at = transform.position + Vector3.up * 1.3f;
            switch (a.category)
            {
                case AttackCategory.Heavy:
                case AttackCategory.Sweep: Sfx3D.Whoosh(at + transform.forward, heavy: true); break;
                case AttackCategory.GuardBreak: Sfx3D.ImpactAt(at, Sfx3D.ImpactKind.Heavy, 0.45f); break;
                case AttackCategory.Feint: Sfx3D.Cloth(transform.position, 0.5f); break;
                case AttackCategory.Delayed: Sfx3D.Breath(true); break;
                case AttackCategory.Thrust: Sfx3D.Whoosh(at + transform.forward * 1.5f); break;
                case AttackCategory.Ranged: Sfx3D.Creak(at, 0.5f); break;
                case AttackCategory.GapCloser: Sfx3D.Cloth(transform.position, 0.8f); break;
                default: Sfx3D.EnemyVoice(at, Sfx3D.Voice.Attack); break;
            }
        }

        /// <summary>The quick attack a feint cancels into: the kit's first Quick or Thrust.</summary>
        private AttackDefinition FollowUpFor(AttackDefinition feint)
        {
            if (def == null) return null;
            AttackDefinition best = null;
            foreach (var a in def.attacks)
            {
                if (a == feint || a.kind == AttackKind.Parry) continue;
                if (a.category is AttackCategory.Quick or AttackCategory.Thrust) return a;
                if (best == null && a.category != AttackCategory.Feint && a.category != AttackCategory.Ranged) best = a;
            }
            return best;
        }

        private RigPose WindupPose()
        {
            if (_pattern == null) return RigPose.Windup;
            return _pattern.category switch
            {
                AttackCategory.Thrust => RigPose.Stab,
                AttackCategory.Sweep => RigPose.Sweep,
                AttackCategory.GuardBreak => RigPose.Kick,
                AttackCategory.Delayed => RigPose.Delayed,
                AttackCategory.GapCloser => RigPose.Charge,
                AttackCategory.RetreatAttack => RigPose.Backstep,
                AttackCategory.Ranged => RigPose.Throw,
                _ => RigPose.Windup,
            };
        }

        /// <summary>What the player is visibly doing. Only what a body on the field can see.</summary>
        private ObservedPlayerState Observe()
        {
            if (_playerCombat == null) return ObservedPlayerState.Neutral;
            if (_playerMotor != null && _playerMotor.Invulnerable) return ObservedPlayerState.Dodging;
            switch (_playerCombat.State)
            {
                case Player.CombatState.Staggered: return ObservedPlayerState.Staggered;
                case Player.CombatState.Recover: return ObservedPlayerState.Recovering;
                case Player.CombatState.Light:
                case Player.CombatState.Heavy:
                case Player.CombatState.Execute: return ObservedPlayerState.Attacking;
                case Player.CombatState.Guard:
                case Player.CombatState.Parry: return ObservedPlayerState.Guarding;
            }
            if (_playerCombat.Deflecting) return ObservedPlayerState.Guarding;
            if (_playerCombat.Whiffed || _playerCombat.JustDodged) return ObservedPlayerState.Recovering;
            if (_playerCombat.Retreating) return ObservedPlayerState.Retreating;
            if (_player != null)
            {
                // Circling: sideways motion relative to this enemy since the last decision.
                var moved = _player.position - _lastPlayerPos; moved.y = 0f;
                var toP = _player.position - transform.position; toP.y = 0f;
                if (moved.sqrMagnitude > 0.3f && toP.sqrMagnitude > 0.1f)
                {
                    var lateral = Vector3.Cross(toP.normalized, moved).magnitude / moved.magnitude;
                    if (lateral > 0.7f) return ObservedPlayerState.Circling;
                }
                if (PlayerBackTurned(toP)) return ObservedPlayerState.BackTurned;
            }
            return ObservedPlayerState.Neutral;
        }

        private bool PlayerBackTurned(Vector3 to) =>
            _player != null && to.sqrMagnitude > 0.01f && Vector3.Angle(_player.forward, -to) > 120f;

        private int AlliesNear(float r)
        {
            var n = 0;
            for (var i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e == null || e == this || e.Dead || e.Unaware) continue;
                if (Vector3.SqrMagnitude(e.transform.position - transform.position) < r * r) n++;
            }
            return n;
        }

        private static int OthersAttacking()
        {
            var n = 0;
            for (var i = 0; i < Active.Count; i++)
                if (Active[i] != null && !Active[i].Dead && Active[i].InWindupOrDash) n++;
            return n;
        }

        private int EnemiesNearPlayer(float r)
        {
            if (_player == null) return 0;
            var n = 0;
            for (var i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e == null || e.Dead) continue;
                if (Vector3.SqrMagnitude(e.transform.position - _player.position) < r * r) n++;
            }
            return n;
        }

        /// <summary>Low health, once: what this enemy does when it is losing.</summary>
        private void CheckLowHealth()
        {
            if (_lowHealthReacted || def == null || ActiveProfile == null || Dead || maxHp <= 0f) return;
            if (Hp > maxHp * ActiveProfile.lowHealthThreshold) return;
            _lowHealthReacted = true;
            switch (ActiveProfile.lowHealth)
            {
                case LowHealthBehaviour.Retreat: _retreatT = 1.8f; Intent = "retreats"; break;
                case LowHealthBehaviour.Guard: _guardT = Mathf.Max(_guardT, 1.2f); _defendT = 1.2f; Intent = "guards"; break;
                case LowHealthBehaviour.Berserk: _moraleAggroT = 9f; _decisionT = 0f; Intent = "berserk"; break;
                case LowHealthBehaviour.CallAllies:
                    for (var i = 0; i < Active.Count; i++)
                    {
                        var e = Active[i];
                        if (e == null || e == this || e.Dead) continue;
                        if (Vector3.Distance(e.transform.position, transform.position) < 9f) e.Alert();
                    }
                    Sfx3D.EnemyVoice(transform.position + Vector3.up * 1.4f, Sfx3D.Voice.Alert);
                    Intent = "calls allies";
                    break;
                case LowHealthBehaviour.Desperate: _moraleAggroT = 4f; _readCd = 0f; Intent = "desperate"; break;
            }
        }

        /// <summary>
        /// The player is doing something an opponent can exploit: mid-heavy,
        /// recovering, swung at air, or stepping out of a dodge.
        /// </summary>
        private bool PlayerExposed() =>
            _playerCombat != null
            && (_playerCombat.Committed && _playerCombat.ExposureRemaining > 0.2f
                || _playerCombat.Whiffed || _playerCombat.JustDodged);

        /// <summary>
        /// One reaction per player commitment. A visible heavy wind-up within
        /// reach is answered by a block (if this enemy reads heavies) or a
        /// sidestep (if it dodges) — rolled once, then locked out until the
        /// next commitment so the enemy cannot chain-react into invulnerability.
        /// Returns true when it took the frame.
        /// </summary>
        private bool ReadPlayer(Vector3 to, float dist)
        {
            if (def == null || _playerCombat == null || _readCd > 0f) return false;
            if (!_playerCombat.HeavyWindingUp || dist > 3.4f) return false;
            var facing = Vector3.Angle(_player.forward, -to) < 70f; // the swing is aimed here
            if (!facing) return false;

            // Difficulty shapes defence: Easy reacts rarely and after a long
            // pause, Lethal often and quickly — but never perfectly (capped),
            // never for free (reaction delay is the cooldown floor).
            var diff = Difficulty.Now;
            var dodge = Mathf.Min(0.85f, def.dodgeChance * diff.DefenseScale);
            var block = Mathf.Min(0.85f, def.blockChance * diff.DefenseScale);
            if (dodge > 0f && Random.value < dodge)
            {
                _readCd = 1.1f + diff.ReactionDelay;
                AiTelemetry.Dodges++;
                _sidestepDir = Strafe(to).normalized;
                _sidestepT = 0.28f;
                _state = State.Recover;
                _t = 0.45f;
                _rig?.PlayOneShot(RigPose.Dash, 0.3f);
                return true;
            }
            if (def.readsHeavies && block > 0f && Random.value < block + 0.2f)
            {
                // Reactive, not random: the block goes up because the swing was seen.
                _readCd = 1.1f + diff.ReactionDelay;
                AiTelemetry.ReactiveBlocks++;
                _guardT = Mathf.Max(_guardT, 0.7f);
                _rig?.ForcePose(RigPose.Windup, 0.6f);
                Face(to);
                return true;
            }
            return false;
        }

        /// <summary>
        /// A step that puts nearby cover between this enemy and the player, or
        /// zero when there is none close. Used by the ring while it waits: they
        /// hold the fight from behind the crates, not in the open.
        /// </summary>
        private Vector3 CoverStep(Vector3 to)
        {
            var m = ArenaMarkers.Instance;
            if (m == null) return Vector3.zero;
            var here = transform.position;
            for (var i = 0; i < m.obstacles.Count; i++)
            {
                var o = m.obstacles[i];
                var c = new Vector3(o.x, 0f, o.z);
                var d = Vector3.Distance(here, c);
                if (d > 4.5f) continue;
                // The far side: from the obstacle, step away from the player.
                var away = c - _player.position; away.y = 0f;
                var goal = c + away.normalized * (o.w + BodyRadius + 0.3f);
                var step = goal - here; step.y = 0f;
                return step.magnitude > 0.4f ? step.normalized * 0.6f : Vector3.zero;
            }
            return Vector3.zero;
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
            var next = transform.position + steered * (speed * Time.deltaTime);
            // Steering alone loses to crowding and knockback, so the final position
            // is pushed back out of cover as a hard rule.
            transform.position = ArenaMarkers.Resolve(next, BodyRadius);
        }

        /// <summary>
        /// Per-kind approach. Each kind wants a different piece of ground, which is
        /// what stops a mixed wave from reading as one blob walking at the player.
        /// </summary>
        private Vector3 ChaseDir(Vector3 to, float dist)
        {
            var n = dist > 0.01f ? to / dist : transform.forward;
            // Data-driven enemies steer by movement style; the legacy roster below
            // keeps its per-kind behaviour until it has a def of its own.
            if (def != null) return StyleDir(def.movement, n, to, dist, def.preferredRange);
            switch (kind)
            {
                case EnemyKind.Ranged:
                    // Kite — and when cover eats the shot, slide until the lane opens
                    // instead of standing there refusing to fire.
                    if (ArenaMarkers.Blocked(transform.position + Vector3.up,
                            _player.position + Vector3.up))
                        return Strafe(to) * 1.6f + n * 0.25f;
                    return dist < 6f ? -n : dist > 9f ? n : Strafe(to);

                case EnemyKind.Chief:
                case EnemyKind.RaiderAxe:
                    // Bruisers: no circling, they just come through.
                    return n;

                case EnemyKind.PikeGuard:
                    // Fights at the end of the shaft: closes to reach, then holds
                    // and repositions rather than walking into your swing.
                    if (dist > SpearReach - 0.4f) return n;
                    return dist < 2.2f ? -n * 0.9f + Strafe(to) : Strafe(to) * 0.8f;

                case EnemyKind.Bomber:
                    // Wants a lane and a gap; never closes willingly.
                    return dist < 5.5f ? -n : dist > 10f ? n : Strafe(to);

                case EnemyKind.Shade:
                {
                    // Ambusher: swings around to the player's back before closing.
                    if (dist > 3.5f)
                    {
                        var rear = _player.position - _player.forward * 2.2f - transform.position;
                        rear.y = 0f;
                        if (rear.magnitude > 0.6f) return rear.normalized;
                    }
                    return dist > attackRange * 0.8f ? n : Strafe(to);
                }

                case EnemyKind.Jin:
                    // Duelist spacing: refuses to be crowded, hovers to lunge from.
                    if (dist < 2.6f) return -n * 0.8f + Strafe(to);
                    return dist > 5.5f ? n : Strafe(to) * 1.2f;

                default:
                {
                    // Bandit: approach off-axis so a pack surrounds rather than
                    // stacking into one queue. The offset unwinds as they close.
                    if (dist > attackRange * 0.8f)
                    {
                        var angle = _flankAngle * Mathf.Clamp01((dist - 2f) / 6f);
                        return Quaternion.Euler(0, angle, 0) * n;
                    }
                    return Strafe(to);
                }
            }
        }

        /// <summary>
        /// One implementation per movement style, shared by every enemy that
        /// declares it. Adding an enemy that fights like a duelist costs a line of
        /// data, not a new branch.
        /// </summary>
        private Vector3 StyleDir(MovementStyle style, Vector3 n, Vector3 to, float dist, float band)
        {
            switch (style)
            {
                case MovementStyle.Direct:
                    return n;

                case MovementStyle.Spacing:
                    // Refuses to be crowded; hovers at its band to lunge from.
                    if (dist < band * 0.75f) return -n * 0.9f + Strafe(to);
                    return dist > band * 1.5f ? n : Strafe(to) * 1.2f;

                case MovementStyle.Kite:
                {
                    // Closed on: abandon the shot and run, hard, before turning to
                    // fight. Melee is the archer's emergency, not its plan.
                    var panic = def != null && def.panicRange > 0f ? def.panicRange : 0f;
                    if (panic > 0f && dist < panic) return -n * 1.4f + Strafe(to) * 0.6f;
                    return dist < band * 0.7f ? -n : dist > band * 1.4f ? n : Strafe(to);
                }

                case MovementStyle.Reach:
                    // Fights at the end of the shaft: closes to reach, then holds.
                    if (dist > band - 0.4f) return n;
                    return dist < band * 0.55f ? -n * 0.9f + Strafe(to) : Strafe(to) * 0.8f;

                case MovementStyle.Ambush:
                {
                    if (dist > band * 1.6f && _player != null)
                    {
                        var rear = _player.position - _player.forward * 2.2f - transform.position;
                        rear.y = 0f;
                        if (rear.magnitude > 0.6f) return rear.normalized;
                    }
                    return dist > attackRange * 0.8f ? n : Strafe(to);
                }

                case MovementStyle.Flee:
                    // Runs, and keeps running. Catching it is the objective.
                    return -n;

                case MovementStyle.Erratic:
                {
                    // Fast and hard to read: the strafe side flips on its own clock.
                    if ((_erraticT -= Time.deltaTime) <= 0f)
                    {
                        _erraticT = Random.Range(0.35f, 0.9f);
                        _strafeSide = -_strafeSide;
                    }
                    if (dist > band * 1.4f) return n;
                    return Strafe(to) * 1.5f + n * 0.25f;
                }

                default: // Flank
                {
                    if (dist > attackRange * 0.8f)
                    {
                        var angle = _flankAngle * Mathf.Clamp01((dist - 2f) / 6f);
                        return Quaternion.Euler(0, angle, 0) * n;
                    }
                    return Strafe(to);
                }
            }
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
            // Height is preserved: a launched enemy owns its own y until it lands.
            var y = transform.position.y;
            if (RoadNorth.Instance != null)
            {
                var p = RoadNorth.Clamp(transform.position, arenaHalfExtents);
                p.y = y;
                transform.position = p;
                return;
            }
            var q = transform.position;
            q.x = Mathf.Clamp(q.x, -arenaHalfExtents.x, arenaHalfExtents.x);
            q.z = Mathf.Clamp(q.z, -arenaHalfExtents.y, arenaHalfExtents.y);
            q.y = y;
            transform.position = q;
        }

        private void BuildTelegraphRing()
        {
            if (_ring != null) return;
            if (_ringWhite == null)
            {
                var ghost = Shader.Find("Emberline/Ghost");
                _ringWhite = new Material(ghost) { color = new Color(0.92f, 0.9f, 0.86f, 0.7f) };
                _ringRed = new Material(ghost) { color = new Color(1f, 0.2f, 0.15f, 0.8f) };
            }
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ringGo.GetComponent<Collider>());
            ringGo.name = "TelegraphRing";
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0, 0.05f, 0);
            ringGo.transform.localScale = new Vector3(2.2f, 0.015f, 2.2f);
            var r = ringGo.GetComponent<Renderer>();
            // sharedMaterial: assigning .material would instance it per enemy.
            r.sharedMaterial = _ringWhite;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring = ringGo.transform;
            ringGo.SetActive(false);
        }

        private void SetRing(bool active)
        {
            if (_ring == null) return;
            _ring.gameObject.SetActive(active);
            if (!active) return;
            _ring.GetComponent<Renderer>().sharedMaterial = RedTelegraph ? _ringRed : _ringWhite;
            SetRingRadius(2.6f); // opens at full footprint, then closes in
        }

        /// <summary>
        /// Ring radius, keeping the disc flat. The windup used to assign a uniform
        /// scale, which stretched the cylinder into a waist-high column around the
        /// enemy instead of a decal on the ground.
        /// </summary>
        private void SetRingRadius(float radius)
        {
            var r = radius * RingScale;
            _ring.localScale = new Vector3(r, 0.015f, r);
        }
    }
}
