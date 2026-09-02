using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.UI;

namespace Emberline.Player
{
    /// <summary>
    /// The four combat verbs with full feedback: soft-lock that actually turns the
    /// attack arc (whiff fix), point-blank 360° forgiveness, lunge, hit-stop,
    /// camera shake, slash flashes, floating damage numbers, and synth SFX.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion), typeof(SenGates))]
    public class CombatController : MonoBehaviour
    {
        [Header("Strike chain")]
        [SerializeField] private float[] strikeDamage = { 10f, 12f, 18f };
        [SerializeField] private float strikeRange = 2.8f;
        [SerializeField] private float strikeArcDeg = 130f;
        [SerializeField] private float chainWindow = 0.75f;

        [Header("Cleave")]
        [SerializeField] private float cleaveDamage = 26f;
        [SerializeField] private float cleaveRange = 3.3f;
        [SerializeField] private float cleaveArcDeg = 170f;
        [SerializeField] private float cleaveWindup = 0.24f;
        [SerializeField] private float cleaveCooldown = 1.4f;

        [Header("Surge nova")]
        [SerializeField] private float surgeDamage = 32f;
        [SerializeField] private float surgeRadius = 5.5f;

        [Header("Kunai")]
        [SerializeField] private float kunaiDamage = 9f;
        [SerializeField] private float kunaiCooldown = 1.6f;

        [Header("Feel")]
        [SerializeField] private float softLockRange = 6.5f;
        [SerializeField] private float softLockConeDeg = 130f;
        [SerializeField] private float lockHold = 4f;
        [SerializeField] private float lungeSpeed = 5.5f;

        [Header("Deflect (hold cleave)")]
        [Tooltip("Opening frames of a guard that count as a perfect parry.")]
        [SerializeField] private float perfectParryWindow = 0.16f;
        [SerializeField] private float deflectWindow = 0.4f;
        [SerializeField] private float deflectMaxHold = 1.1f;
        [SerializeField] private float deflectCooldown = 0.7f;

        [Header("Launcher / execution")]
        [SerializeField] private float launchSpeed = 6.5f;
        [SerializeField] private float executeThreshold = 0.2f;

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        /// <summary>Attacks turned aside this mission (feat / daily tracking).</summary>
        public int Deflects { get; private set; }

        /// <summary>0 = ready, 1 = just used. For HUD cooldown rings.</summary>
        public float CleaveCd01 => cleaveCooldown > 0f ? _cleaveCd / cleaveCooldown : 0f;

        /// <summary>0 = ready, 1 = just thrown. For the HUD kunai ring.</summary>
        public float KunaiCd01 => kunaiCooldown > 0f ? _kunaiCd / kunaiCooldown : 0f;

        /// <summary>True inside the perfect-dodge counter window (next strike ×2).</summary>
        public bool CounterActive => _counterT > 0f;

        /// <summary>Deflect stance is live — incoming melee is turned aside.</summary>
        public bool Deflecting => _guardT > 0f;

        /// <summary>0 = ready, 1 = just spent. HUD ring on the cleave button.</summary>
        public float DeflectCd01 => deflectCooldown > 0f ? _deflectCd / deflectCooldown : 0f;

        /// <summary>Cycled soft-lock target, or null when following the cone.</summary>
        public EnemyBrain LockedTarget =>
            _lockT > 0f && _lockTarget != null && !_lockTarget.Dead ? _lockTarget : null;

        private PlayerLocomotion _motor;
        private SenGates _sen;
        private CameraRig _rig;
        private CharacterRig _ninja;
        private WeaponDef _weapon;
        private int _stage;
        private float _chainTimer, _cleaveCd, _kunaiCd, _pendingCleave = -1f, _comboTimer;
        private float _counterT, _lanternTick;
        private bool _inFinisher;

        // Reused target buffers. Two of them: StrikeArc/Surge iterate _scan and
        // can trigger ThreadBurst from inside that loop, which needs its own.
        private readonly List<EnemyBrain> _scan = new();
        private readonly List<EnemyBrain> _burst = new();

        private float _dipT, _dipScale = 1f;

        /// <summary>
        /// The single source of truth for what Renzo is doing. Every action asks
        /// CombatRules whether the transition is legal, so two states can no longer
        /// be live at once.
        /// </summary>
        public CombatState State { get; private set; } = CombatState.Free;

        private float _stateT;   // time left in a committed state

        // ---- what the AI may read ---------------------------------------
        // Enemies do not read inputs or intentions; they read the same thing a
        // human opponent reads — the body. A heavy wind-up, a whiffed swing and
        // the tail of a dodge are all visible commitments, and those are the
        // only signals exposed here.

        /// <summary>Locked into an animation an opponent could exploit.</summary>
        /// <summary>
        /// True while the player is locked into something they cannot cancel.
        /// The pending-cleave check is not redundant: starting a heavy also opens
        /// the deflect window, which overwrites State with Guard on the very next
        /// line of Cleave(). Reading State alone would therefore miss the single
        /// most punishable thing the player ever does.
        /// </summary>
        public bool Committed =>
            State is CombatState.Heavy or CombatState.Recover or CombatState.Execute
            || _pendingCleave >= 0f;

        /// <summary>Seconds of commitment left. 0 when free.</summary>
        public float ExposureRemaining => !Committed ? 0f
            : Mathf.Max(Mathf.Max(0f, _stateT), _pendingCleave >= 0f ? _pendingCleave + 0.35f : 0f);

        /// <summary>The heavy telegraph is up — the moment a good opponent blocks or steps.</summary>
        public bool HeavyWindingUp => _pendingCleave >= 0f;

        /// <summary>A swing that hit nothing in the last half second. Greed leaves a gap.</summary>
        public bool Whiffed => _whiffT > 0f;

        /// <summary>A dodge ended in the last third of a second: the classic hesitation window.</summary>
        public bool JustDodged => _postDodgeT > 0f;

        private float _whiffT, _postDodgeT;
        private bool _wasInvulnerable;

        /// <summary>Request a state change; returns false if the rules forbid it.</summary>
        private bool Enter(CombatState next, float duration = 0f)
        {
            if (!CombatRules.CanEnter(State, next)) return false;
            State = next;
            _stateT = duration;
            _motor.Busy = CombatRules.Committed(next);
            return true;
        }

        private void UpdateState(float dt)
        {
            _whiffT = Mathf.Max(0f, _whiffT - dt);
            _postDodgeT = Mathf.Max(0f, _postDodgeT - dt);
            // The dodge's tail: i-frames just ended and the feet have not settled.
            var inv = _motor != null && _motor.Invulnerable;
            if (_wasInvulnerable && !inv) _postDodgeT = 0.32f;
            _wasInvulnerable = inv;

            if (_stateT <= 0f) return;
            if ((_stateT -= dt) > 0f) return;
            // Committed states fall back to neutral on their own.
            State = CombatState.Free;
            _motor.Busy = false;
        }
        private float _guardT, _guardHeld, _deflectCd, _lockT;
        private EnemyBrain _lockTarget;

        private void Awake()
        {
            _motor = GetComponent<PlayerLocomotion>();
            _sen = GetComponent<SenGates>();
            _health = GetComponent<Health>();
            _ninja = GetComponent<CharacterRig>();
        }

        private void Start()
        {
            _rig = FindFirstObjectByType<CameraRig>();
            ApplyWeapon(Loadout.Current);
        }

        /// <summary>Copy the equipped WeaponDef's stats over the tuned defaults.</summary>
        private void ApplyWeapon(WeaponDef w)
        {
            _weapon = w;
            if (w == null) return;

            // Ryo-bought upgrades are multipliers over the def, never edits to it,
            // so the asset stays the single description of what the weapon is.
            var dmgUp = WeaponUpgrades.DamageMul(w.id);
            var rangeUp = WeaponUpgrades.RangeMul(w.id);
            var speedUp = WeaponUpgrades.SpeedMul(w.id);

            strikeDamage = new float[w.strikeDamage.Length];
            for (var i = 0; i < strikeDamage.Length; i++)
                strikeDamage[i] = w.strikeDamage[i] * dmgUp;
            strikeRange = w.strikeRange * rangeUp;
            strikeArcDeg = w.strikeArcDeg;
            chainWindow = w.chainWindow + (SkillTree.Has("combo_window") ? 0.2f : 0f);
            lungeSpeed = w.lungeSpeed;
            cleaveDamage = w.cleaveDamage * dmgUp * (SkillTree.Has("cleave_dmg") ? 1.25f : 1f);
            cleaveRange = w.cleaveRange * rangeUp;
            cleaveArcDeg = w.cleaveArcDeg;
            cleaveWindup = w.cleaveWindup * speedUp;
            cleaveCooldown = w.cleaveCooldown * speedUp;
            if (SkillTree.Has("surge_radius")) surgeRadius *= 1.2f;
            SwapHandProps(w);

            // Cloth dye first, blade finish second: the finish writes its own
            // property block on the prop renderers and must not be overwritten.
            Cosmetics.ApplyTo(gameObject);

            // Cosmetic blade finish: recolors the trail and the weapon prop.
            var finish = BladeFinish.Current;
            var trailColor = finish.starsRequired > 0 ? finish.trail : Cosmetics.Current.Accent;
            var trail = GetComponentInChildren<TrailRenderer>(true);
            if (trail != null) trail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.8f);
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", finish.blade);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name.StartsWith("Prop_") || r.transform.parent != null
                    && r.transform.parent.name.StartsWith("Prop_"))
                    r.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Show only the props this weapon uses. The bootstrap hangs every prop the
        /// catalogue can ask for on the rig up front — a player build can't create
        /// them from FBX at runtime — so the swap is purely visibility.
        /// Props are named `Prop_{propName}_{side}` by EmberCharacterFactory.
        /// </summary>
        private void SwapHandProps(WeaponDef w)
        {
            var wantRight = string.IsNullOrEmpty(w.propRight) ? null : $"Prop_{w.propRight}_r";
            var wantLeft = string.IsNullOrEmpty(w.propLeft) ? null : $"Prop_{w.propLeft}_l";
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Prop_")) continue;
                t.gameObject.SetActive(t.name == wantRight || t.name == wantLeft);
            }
        }

        private void Update()
        {
            UpdateTimeDip(); // before the cinematic gate, or a dip could outlive it
            if (GameManager.CinematicActive) return;
            _chainTimer = Mathf.Max(0, _chainTimer - Time.deltaTime);
            _cleaveCd = Mathf.Max(0, _cleaveCd - Time.deltaTime);
            _kunaiCd = Mathf.Max(0, _kunaiCd - Time.deltaTime);
            _counterT = Mathf.Max(0, _counterT - Time.deltaTime);
            _deflectCd = Mathf.Max(0, _deflectCd - Time.deltaTime);
            UpdateFraming();
            UpdateBreathing();
            _lockT = Mathf.Max(0, _lockT - Time.deltaTime);
            UpdateState(Time.deltaTime);
            UpdateGuard();
            if (_comboTimer > 0 && (_comboTimer -= Time.deltaTime) <= 0) Combo = 0;
            UpdateLanternPassive();

            if (_pendingCleave >= 0 && (_pendingCleave -= Time.deltaTime) < 0)
            {
                ResolveCleave();
                _motor.Busy = false;
            }

            // EmberInput: touch buttons on device, mouse/keyboard in the editor.
            // Attacks are buffered the same way jumps are: pressing a few frames
            // before the current swing frees up used to drop the input entirely,
            // which reads as the game ignoring you rather than as commitment.
            if (EmberInput.ConsumeStrike()) _strikeBuffer = AttackBuffer;
            if (EmberInput.ConsumeCleave()) _cleaveBuffer = AttackBuffer;

            // Cleave first: it is the deliberate input, and a player holding both
            // meant the heavy attack. Firing consumes the buffer; a refusal lets
            // it keep ticking so the retry happens on the next frame.
            if (_cleaveBuffer > 0f)
            {
                _cleaveBuffer -= Time.deltaTime;
                if (Cleave()) _cleaveBuffer = 0f;
            }
            if (_strikeBuffer > 0f)
            {
                _strikeBuffer -= Time.deltaTime;
                if (Strike()) _strikeBuffer = 0f;
            }
            // Flicker prefers a live kunai: blink to the blade, else dodge.
            if (EmberInput.ConsumeFlicker() && !TryKunaiWarp()) _motor.TryFlicker();
            if (EmberInput.ConsumeSurge()) Surge();
            if (EmberInput.ConsumeKunai()) ThrowKunai();
            if (EmberInput.ConsumeCycleTarget()) CycleTarget();
        }

        /// <summary>Hits before the chain restarts; falls back to the damage array's length.</summary>
        private int ChainLength => Mathf.Max(1,
            _weapon != null && _weapon.strikeChainLength > 0
                ? _weapon.strikeChainLength
                : strikeDamage.Length);

        /// <summary>
        /// Damage for a 1-based chain stage. A weapon may declare a longer chain
        /// than it has authored numbers for, so the array is stretched across the
        /// chain rather than indexed out of range.
        /// </summary>
        private float StageDamage(int stage)
        {
            if (strikeDamage == null || strikeDamage.Length == 0) return 10f;
            if (stage <= strikeDamage.Length) return strikeDamage[stage - 1];
            // Beyond the authored entries: hold the last value, but let the true
            // finisher keep its weight.
            return stage == ChainLength
                ? strikeDamage[strikeDamage.Length - 1]
                : strikeDamage[strikeDamage.Length - 2 < 0 ? 0 : strikeDamage.Length - 2];
        }

        /// <summary>Light attack. Returns false when the state machine refused it,
        /// which is what lets a buffered press retry on a later frame.</summary>
        public bool Strike()
        {
            if (_pendingCleave >= 0 || _motor.Invulnerable) return false;
            // Light attack: chains out of itself, a guard or neutral — never out
            // of a heavy commitment or a stagger.
            if (!Enter(CombatState.Light, _weapon != null ? _weapon.strikeAnimTime : 0.28f)) return false;
            SoftLockFacing();
            _stage = _chainTimer > 0 ? (_stage % ChainLength) + 1 : 1;
            _chainTimer = chainWindow;
            // A shooter doesn't close the distance to fire — no lunge on a bolt.
            if (!FiresOnStrike) _motor.Impulse(_motor.Facing * lungeSpeed);
            Sfx3D.Slash();
            // The whoosh sits behind the slash and moves with the blade, so a
            // swing that misses still reads as a swing.
            Sfx3D.Whoosh(transform.position + _motor.Facing * 1.2f + Vector3.up);
            // Only three strike poses exist, so longer chains cycle through them.
            _ninja?.PlayOneShot(((_stage - 1) % 3) switch
            {
                1 => RigPose.Strike2,
                2 => RigPose.Strike3,
                _ => RigPose.Strike1,
            }, _weapon != null ? _weapon.strikeAnimTime : 0.28f);
            var dmg = StageDamage(_stage);
            if (CounterActive) { dmg *= 2f; _counterT = 0f; }
            // The crossbow's "strike" is a shot: it fires a single bolt instead of
            // swinging, which is what makes it play at range rather than in reach.
            if (FiresOnStrike)
            {
                var origin = transform.position + Vector3.up * 1.25f + _motor.Facing * 0.5f;
                Kunai.Spawn(origin, _motor.Facing, dmg, this, "Bolt");
                FxPools.BoltTrail(origin, _motor.Facing);
                return true; // the shot went out — this is a success, not a refusal
            }

            // The last hit of the chain is the finisher: it crushes and launches.
            var finisher = _stage == ChainLength;
            StrikeArc(strikeRange, strikeArcDeg, dmg, crush: finisher, launch: finisher);
            // Twin daggers get their own read: two thin lines, not one heavy arc.
            if (_weapon != null && _weapon.archetype == WeaponArchetype.Daggers)
                FxPools.QuickSlash(transform.position + Vector3.up * 1.1f + _motor.Facing * 1.2f,
                    _motor.Facing);
            return true;
        }

        /// <summary>Ranged weapons shoot on the strike button rather than swinging.</summary>
        private bool FiresOnStrike =>
            _weapon != null && _weapon.archetype == WeaponArchetype.Ranged;

        /// <summary>Perfect dodge: slow-mo counter window; Storm Tanto also parries.</summary>
        public void OnPerfectDodge()
        {
            _counterT = 0.5f;
            RunSlowMo(0.28f);
            if (_weapon == null || !_weapon.parryOnPerfectDodge) return;
            CollectTargets(_scan);
            foreach (var brain in _scan)
            {
                if (!brain.InWindupOrDash) continue;
                if (Vector3.Distance(brain.transform.position, transform.position) > 3.5f) continue;
                brain.TakeHit(2f, transform.position, crush: true);
                FloatingText.Spawn(brain.transform.position + Vector3.up * 2.3f, "PARRY",
                    new Color(0.75f, 0.9f, 1f), 0.9f);
            }
        }

        /// <summary>LANTERN'S WRATH skill: periodic scorch around Renzo.</summary>
        private void UpdateLanternPassive()
        {
            if (!SkillTree.Has("lantern_burn")) return;
            _lanternTick += Time.deltaTime;
            if (_lanternTick < 8f) return;
            _lanternTick = 0f;
            var any = false;
            CollectTargets(_scan);
            foreach (var brain in _scan)
            {
                if (Vector3.Distance(brain.transform.position, transform.position) > 3.2f) continue;
                var dealt = brain.TakeHit(8f, transform.position);
                ShowDamage(brain, dealt, ember: true);
                any = true;
            }
            if (any)
            {
                FxPools.Embers(transform.position + Vector3.up, 18);
                Sfx3D.Surge();
            }
        }

        /// <summary>
        /// The cleave button resolves differently per weapon family, so a weapon is
        /// a different verb rather than the same swing with other numbers.
        /// </summary>
        private void ResolveCleave()
        {
            var style = _weapon != null ? _weapon.cleaveStyle : CleaveStyle.Slash;
            switch (style)
            {
                case CleaveStyle.Spin:
                    // Whirl: everything around you, no facing requirement.
                    StrikeArc(cleaveRange, 360f, cleaveDamage, crush: true);
                    FxPools.Slash(transform.position + Vector3.up, _motor.Facing, true);
                    _rig?.Shake(4f, 0.2f);
                    break;

                case CleaveStyle.Ground:
                    // Burst at the feet: wider, slower, and it announces itself.
                    StrikeArc(cleaveRange, 360f, cleaveDamage, crush: true);
                    FxPools.Nova(transform.position);
                    FxPools.Embers(transform.position + Vector3.up * 0.4f, 26);
                    Sfx3D.Surge();
                    _rig?.Shake(7f, 0.3f);
                    break;

                case CleaveStyle.FanShot:
                {
                    // Three bolts in a spread — the cleave becomes a volley.
                    var ammo = ThrownPrefabName();
                    for (var i = -1; i <= 1; i++)
                    {
                        var dir = Quaternion.Euler(0f, i * (cleaveArcDeg * 0.5f), 0f) * _motor.Facing;
                        Kunai.Spawn(transform.position + Vector3.up * 1.3f + dir * 0.5f,
                            dir, cleaveDamage, this, ammo);
                    }
                    Sfx3D.Slash();
                    break;
                }

                default:
                    StrikeArc(cleaveRange, cleaveArcDeg, cleaveDamage, crush: true);
                    break;
            }

            if (_weapon != null && _weapon.poisonCleave)
                PoisonPuddle.Spawn(transform.position + _motor.Facing * 1.6f);
        }

        /// <summary>Which thrown model this weapon uses; plain kunai unless it says otherwise.</summary>
        private string ThrownPrefabName() =>
            _weapon != null && _weapon.replacesKunaiWithThrown
            && !string.IsNullOrEmpty(_weapon.thrownId)
                ? _weapon.thrownId
                : "Kunai";

        // ------------------------------------------------------------- deflect

        /// <summary>
        /// The cleave's windup doubles as a guard: pressing opens a short deflect
        /// window, and holding the button keeps it open up to `deflectMaxHold`.
        /// Keeping the swing on press means the cleave loses none of its snap.
        /// </summary>
        private void UpdateGuard()
        {
            var dt = Time.deltaTime;
            if (_guardT > 0f)
            {
                _guardT -= dt;
                _guardHeld += dt;
                // Held and still within budget: keep the stance alive.
                if (EmberInput.CleaveHeld && _guardHeld < deflectMaxHold)
                    _guardT = Mathf.Max(_guardT, 0.08f);
                if (_guardT <= 0f)
                {
                    _guardT = 0f;
                    _deflectCd = deflectCooldown;
                    _motor.Busy = false;
                }
                return;
            }
            _guardHeld = 0f;
        }

        /// <summary>
        /// A deflected blow costs nothing, staggers the attacker and pays Sen like
        /// a perfect dodge — the aggressive answer to an incoming swing.
        /// </summary>
        /// <summary>True inside the tight opening frames of a guard.</summary>
        public bool PerfectWindow => _guardT > 0f && _guardHeld <= perfectParryWindow;

        /// <summary>
        /// An incoming blow met by the guard. The opening frames are a perfect
        /// parry — no chip, big posture damage, a real counter window. After that
        /// it is an ordinary block: the hit is turned aside but it costs guard.
        /// </summary>
        public void OnDeflect(EnemyBrain attacker)
        {
            var perfect = PerfectWindow;
            Deflects++;
            _guardT = 0f;
            _guardHeld = 0f;
            _deflectCd = deflectCooldown;
            _motor.Busy = false;
            _counterT = perfect ? 0.75f : 0.4f;
            _sen.OnPerfectDodge();
            Enter(CombatState.Parry, 0.2f);

            if (perfect)
            {
                // The skill payoff: time drops, the counter window is long, and the
                // attacker's guard takes a chunk it cannot regenerate through.
                RunSlowMo(0.22f);
                _rig?.Shake(7f, 0.26f);
                _rig?.ImpactZoom(1f);
                Sfx3D.ImpactAt(transform.position + Vector3.up * 1.2f, Sfx3D.ImpactKind.Guard, 1.3f);
                Haptics.Buzz();
                FloatingText.Spawn(transform.position + Vector3.up * 2.5f, "PERFECT PARRY",
                    new Color(1f, 0.92f, 0.65f), 1.25f);
            }
            else
            {
                RunHitStop(0.05f);
                _rig?.Shake(3f, 0.16f);
                _rig?.ImpactZoom(0.45f);
                Sfx3D.ImpactAt(transform.position + Vector3.up * 1.2f, Sfx3D.ImpactKind.Guard, 0.8f);
                FloatingText.Spawn(transform.position + Vector3.up * 2.5f, "BLOCK",
                    new Color(0.75f, 0.9f, 1f), 1f);
            }
            _ninja?.PlayOneShot(RigPose.Cleave, 0.25f);

            if (attacker == null || attacker.Dead) return;
            // A parry attacks their posture, not their health — that is how a
            // defensive player opens a guard they cannot out-damage.
            attacker.TakeHit(perfect ? 14f : 4f, transform.position, crush: perfect);
            FxPools.Sparks(attacker.transform.position + Vector3.up * 1.2f,
                new Color(0.9f, 0.94f, 1f), perfect ? 16 : 8);
        }

        // ---------------------------------------------------------- kunai warp

        /// <summary>
        /// Flicker while a kunai is in flight: vanish and reappear at the blade.
        /// Spends the flicker cooldown, so it trades the dodge for the reposition
        /// rather than adding a free one.
        /// </summary>
        private bool TryKunaiWarp()
        {
            var kunai = Kunai.Latest;
            if (kunai == null || !kunai.isActiveAndEnabled) return false;
            var dest = kunai.transform.position;
            dest.y = 0f;
            // Too close to be worth the cooldown — fall through to a normal dodge.
            if ((dest - transform.position).sqrMagnitude < 2f * 2f) return false;
            if (!_motor.TryWarpTo(dest)) return false;
            kunai.ConsumeForWarp();
            _sen.OnHitLanded();
            FxPools.Embers(dest + Vector3.up, 16);
            Sfx3D.Surge();
            FloatingText.Spawn(dest + Vector3.up * 2.4f, "WARP",
                new Color(0.8f, 0.88f, 1f), 1.05f);
            return true;
        }

        /// <summary>Heavy attack. Returns false when refused; see <see cref="Strike"/>.</summary>
        public bool Cleave()
        {
            if (_cleaveCd > 0 || _motor.Invulnerable) return false;
            // Heavy attack: a real commitment, so it refuses to start from
            // anything but neutral or the tail of a light.
            if (!Enter(CombatState.Heavy, cleaveWindup + 0.35f)) return false;
            _cleaveCd = cleaveCooldown;
            _pendingCleave = cleaveWindup;
            _motor.Busy = true;
            // Cloth on the wind-up, the heavy whoosh on the swing itself.
            Sfx3D.Cloth(transform.position, 0.4f);
            Sfx3D.Whoosh(transform.position + _motor.Facing * 1.4f + Vector3.up, heavy: true);
            // The windup is also the guard window (see UpdateGuard).
            if (_deflectCd <= 0f && Enter(CombatState.Guard, deflectWindow))
            {
                _guardT = deflectWindow;
                _guardHeld = 0f;
            }
            Sfx3D.Slash();
            _ninja?.PlayOneShot(RigPose.Cleave, cleaveWindup + 0.3f);
            SoftLockFacing();
            _motor.Impulse(_motor.Facing * (lungeSpeed * 0.6f));
            return true;
        }

        /// <summary>Thrown kunai: soft-locks like a strike, flies flat and fast.</summary>
        public void ThrowKunai()
        {
            if (_kunaiCd > 0 || _pendingCleave >= 0 || _motor.Invulnerable) return;
            _kunaiCd = kunaiCooldown;
            SoftLockFacing();
            var dmg = kunaiDamage;
            if (CounterActive) { dmg *= 2f; _counterT = 0f; } // perfect-dodge counter applies
            Sfx3D.Slash();
            _ninja?.PlayOneShot(RigPose.Strike2, 0.2f);
            ThrowSpecial(dmg);
        }

        /// <summary>
        /// What the throw slot actually launches. Most weapons throw a kunai; the
        /// smoke bomb throws a bomb that bursts into a blinding cloud, and the
        /// crossbow throws a bolt. Driven off the WeaponDef so a new weapon only
        /// has to declare its ammunition.
        /// </summary>
        private void ThrowSpecial(float damage)
        {
            var origin = transform.position + Vector3.up * 1.3f + _motor.Facing * 0.5f;
            if (_weapon != null && _weapon.replacesKunaiWithThrown && _weapon.thrownId == "Bomb")
            {
                SmokeBomb.Spawn(origin, _motor.Facing);
                return;
            }
            var ammo = ThrownPrefabName();
            Kunai.Spawn(origin, _motor.Facing, damage, this, ammo);
            if (ammo == "Bolt") FxPools.BoltTrail(origin, _motor.Facing);
        }

        /// <summary>Kunai callback: damage number + the combo/Sen loop, like sword hits.</summary>
        public void OnKunaiHit(EnemyBrain brain, float dealt)
        {
            ShowDamage(brain, dealt, ember: false);
            OnHitLanded();
        }

        public void Surge()
        {
            if (!_sen.Surge()) return;
            Sfx3D.Surge();
            FxPools.Nova(transform.position);
            _rig?.Shake(9f, 0.35f);
            RunHitStop(0.07f);
            CollectTargets(_scan);
            foreach (var brain in _scan)
            {
                if (Vector3.Distance(brain.transform.position, transform.position) <= surgeRadius)
                {
                    var dealt = brain.TakeHit(surgeDamage, transform.position, crush: true);
                    ShowDamage(brain, dealt, ember: true);
                    OnHitLanded();
                }
            }
        }

        private void StrikeArc(float range, float arcDeg, float damage, bool crush,
            bool launch = false)
        {
            var hitAny = false;
            CollectWithin(_scan, range, arcDeg);
            foreach (var brain in _scan)
            {
                // A reeling, nearly-dead mook is finished outright instead.
                if (brain.CanExecute)
                {
                    Execute(brain);
                    hitAny = true;
                    continue;
                }
                var dealt = brain.TakeHit(damage, transform.position, crush);
                if (launch && !brain.Dead) brain.Launch(launchSpeed);
                ShowDamage(brain, dealt, ember: crush);
                // Impact at the contact point, not the body centre: a short spark
                // burst for steel, a little dark mist for flesh, and dust kicked
                // off the deck on a heavy. Deliberately small — the hit-stop and
                // the reaction carry the weight, not the particles.
                var contact = Vector3.Lerp(transform.position,
                    brain.transform.position, 0.72f) + Vector3.up * 1.15f;
                FxPools.Sparks(contact, new Color(0.86f, 0.88f, 0.92f), crush ? 7 : 4);
                FxPools.Puff(contact, new Color(0.24f, 0.06f, 0.05f, 0.85f), crush ? 5 : 3);
                if (crush)
                    FxPools.Puff(brain.transform.position + Vector3.up * 0.15f,
                        new Color(0.30f, 0.27f, 0.24f, 0.7f), 6);
                FxPools.Slash(contact, _motor.Facing, crush);
                // Marsh Hook: the third hit drags enemies toward Renzo.
                if (crush && _weapon != null && _weapon.pullOnThirdHit && !brain.Dead)
                {
                    var to = transform.position - brain.transform.position;
                    var d = to.magnitude;
                    if (d > 1.6f)
                        brain.transform.position += to.normalized * Mathf.Min(1.3f, d - 1.5f);
                }
                OnHitLanded();
                hitAny = true;
            }
            // Strikes also crack arena lantern posts (break → health pickup).
            foreach (var post in LanternPost.Active)
            {
                if (post.Broken) continue;
                var to = post.transform.position - transform.position;
                to.y = 0;
                if (to.magnitude > range || Vector3.Angle(_motor.Facing, to) > arcDeg * 0.5f) continue;
                post.Damage(damage, GetComponent<Health>());
                hitAny = true;
            }

            if (!hitAny) _whiffT = 0.55f;
            if (hitAny)
            {
                Sfx3D.ImpactAt(transform.position + _motor.Facing * 1.4f + Vector3.up,
                    crush ? Sfx3D.ImpactKind.Heavy : Sfx3D.ImpactKind.Blade, crush ? 1.3f : 1f);
                _rig?.Shake(crush ? 5f : 2.5f, 0.18f);
                _rig?.ImpactZoom(crush ? 0.8f : 0.4f);
                RunHitStop(crush ? 0.06f : 0.04f);
            }
        }

        /// <summary>
        /// Finisher on a staggered, nearly-dead mook: kills outright with the
        /// heaviest feedback in the kit, and pays back Sen so pressing the
        /// advantage is rewarded rather than merely tidy.
        /// </summary>
        private void Execute(EnemyBrain brain)
        {
            var pos = brain.transform.position;
            // A kill on someone who never saw you is an assassination, not a finisher.
            var silent = brain.Unaware;
            brain.TakeHit(brain.Hp + 1f, transform.position, crush: true);
            _sen.OnPerfectDodge();
            // A silent kill stays silent: no nova, no screen-shake, no noise that
            // would give the position away in a stealth mission.
            RequestDip(0.04f, silent ? 0.07f : 0.12f);
            if (silent)
            {
                FxPools.Sparks(pos + Vector3.up * 1.2f, new Color(0.8f, 0.88f, 1f), 10);
                Sfx3D.Hit();
            }
            else
            {
                _rig?.Shake(10f, 0.4f);
                _rig?.ImpactZoom(1f);
                _rig?.PlayExecution(brain.transform, 0.85f);
                FxPools.Nova(pos);
                FxPools.Embers(pos + Vector3.up, 26);
                Sfx3D.ImpactAt(pos + Vector3.up, Sfx3D.ImpactKind.Heavy, 1.6f);
                Haptics.Buzz();
            }
            FloatingText.Spawn(pos + Vector3.up * 2.6f, silent ? "ASSASSINATE" : "EXECUTE",
                new Color(1f, 0.5f, 0.3f), 1.35f);
            OnHitLanded();
        }

        private void ShowDamage(EnemyBrain brain, float damage, bool ember)
        {
            FloatingText.Spawn(
                brain.transform.position + Vector3.up * 2.1f,
                Mathf.RoundToInt(damage).ToString(),
                ember ? new Color(1f, 0.5f, 0.3f) : new Color(0.95f, 0.94f, 0.9f));
        }

        /// <summary>
        /// Fills `into` with live enemies. The live registry is snapshotted first
        /// because TakeHit can stagger, pull or kill mid-loop; the buffers are
        /// reused so a swing allocates nothing.
        /// </summary>
        private static void CollectTargets(List<EnemyBrain> into)
        {
            into.Clear();
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var b = EnemyBrain.Active[i];
                if (b != null && !b.Dead) into.Add(b);
            }
        }

        private void CollectWithin(List<EnemyBrain> into, float range, float arcDeg)
        {
            CollectTargets(into);
            for (var i = into.Count - 1; i >= 0; i--)
            {
                var to = into[i].transform.position - transform.position;
                to.y = 0;
                var d = to.magnitude;
                // Point-blank hits connect regardless of facing — no more whiffs
                // when an enemy is standing on top of you.
                if (d > range || d > 1.7f && Vector3.Angle(_motor.Facing, to) > arcDeg * 0.5f)
                    into.RemoveAt(i);
            }
        }

        /// <summary>
        /// Turn toward what the player is actually aiming at. A cycled lock wins
        /// outright; otherwise the pick is scored inside a cone around the aim
        /// direction, so an enemy behind you no longer steals a swing meant for
        /// the one in front. Point-blank targets stay eligible at any angle.
        /// </summary>
        /// <summary>Feed the camera what it should be framing this frame.</summary>
        private void UpdateFraming()
        {
            if (_rig == null) return;
            var t = LockedTarget;
            _rig.LockFocus = t != null ? t.transform : null;
            _rig.BossFraming = t != null && t.IsBossTarget;
        }

        /// <summary>
        /// Breathing is the cheapest health readout there is: it only starts under
        /// a third health, and it speeds up as things get worse.
        /// </summary>
        private void UpdateBreathing()
        {
            if (_health == null || _health.Dead) return;
            var frac = _health.Hp / Mathf.Max(1f, _health.MaxHp);
            if (frac > 0.34f) { _breathT = 0f; return; }
            _breathT -= Time.deltaTime;
            if (_breathT > 0f) return;
            var hard = frac < 0.18f;
            _breathT = hard ? 1.1f : 1.8f;
            Sfx3D.Breath(hard);
        }

        /// <summary>How long an unusable attack press stays live. Matches the
        /// locomotion jump buffer, so both inputs forgive the same amount.</summary>
        private const float AttackBuffer = 0.15f;

        private float _strikeBuffer, _cleaveBuffer;
        private float _breathT;
        private Health _health;

        private void SoftLockFacing()
        {
            var target = LockedTarget != null
                && Vector3.Distance(LockedTarget.transform.position, transform.position)
                   <= softLockRange * 1.4f
                ? LockedTarget
                : BestInCone();

            // The old bug: rotating the transform but not Facing meant the arc
            // check used a stale direction and strikes whiffed. SetFacing fixes both.
            if (target != null)
                _motor.SetFacing(target.transform.position - transform.position);
        }

        /// <summary>Aim direction: the stick if it's pushed, else current facing.</summary>
        private Vector3 AimDir()
        {
            var move = EmberInput.Move;
            var raw = new Vector3(move.x, 0f, move.y);
            return raw.sqrMagnitude > 0.04f ? raw.normalized : _motor.Facing;
        }

        private EnemyBrain BestInCone()
        {
            var aim = AimDir();
            EnemyBrain best = null;
            var bestScore = float.MaxValue;
            // Read-only scan: nothing here mutates the registry, so no snapshot.
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var brain = EnemyBrain.Active[i];
                if (brain == null || brain.Dead) continue;
                var to = brain.transform.position - transform.position;
                to.y = 0f;
                var d = to.magnitude;
                if (d > softLockRange) continue;
                var angle = d > 0.01f ? Vector3.Angle(aim, to) : 0f;
                // Anything on top of us is fair game regardless of facing.
                if (d > 1.7f && angle > softLockConeDeg * 0.5f) continue;
                var score = d + angle * 0.03f; // near and ahead beats far and wide
                if (score >= bestScore) continue;
                bestScore = score;
                best = brain;
            }
            return best;
        }

        /// <summary>
        /// Step the lock to the next candidate in the cone, ordered left to right,
        /// and hold it for a few seconds so swings commit to one enemy in a crowd.
        /// </summary>
        public void CycleTarget()
        {
            var aim = AimDir();
            EnemyBrain first = null, next = null;
            var firstKey = float.MaxValue;
            var currentKey = _lockTarget != null && LockedTarget != null
                ? SignedKey(_lockTarget, aim) : float.MinValue;
            var nextKey = float.MaxValue;

            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var brain = EnemyBrain.Active[i];
                if (brain == null || brain.Dead) continue;
                var to = brain.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > softLockRange * 1.2f) continue;
                var key = SignedKey(brain, aim);
                if (key < firstKey) { firstKey = key; first = brain; }
                if (key > currentKey && key < nextKey) { nextKey = key; next = brain; }
            }

            _lockTarget = next != null ? next : first; // wrap around
            if (_lockTarget == null) return;
            _lockT = lockHold;
            Sfx3D.Ui();
            FloatingText.Spawn(_lockTarget.transform.position + Vector3.up * 2.6f, "LOCK",
                new Color(1f, 0.62f, 0.35f), 0.95f);
        }

        /// <summary>Left-to-right ordering key: signed angle from the aim direction.</summary>
        private float SignedKey(EnemyBrain brain, Vector3 aim)
        {
            var to = brain.transform.position - transform.position;
            to.y = 0f;
            return Vector3.SignedAngle(aim, to, Vector3.up);
        }

        private void OnHitLanded()
        {
            Combo++;
            _comboTimer = 3f;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            _sen.OnHitLanded();
            if (Combo > 0 && Combo % 10 == 0 && !_inFinisher) ThreadBurst();
        }

        /// <summary>Every 10th combo hit: screen-shaking ember burst around Renzo.</summary>
        private void ThreadBurst()
        {
            _inFinisher = true;
            var dmg = 20f * (SkillTree.Has("finisher_power") ? 1.5f : 1f);
            FxPools.Nova(transform.position);
            Sfx3D.Surge();
            _rig?.Shake(7f, 0.3f);
            FloatingText.Spawn(transform.position + Vector3.up * 2.6f, "THREAD BURST",
                new Color(1f, 0.62f, 0.35f), 1.2f);
            CollectTargets(_burst);
            foreach (var brain in _burst)
            {
                if (Vector3.Distance(brain.transform.position, transform.position) > 4.5f) continue;
                var dealt = brain.TakeHit(dmg, transform.position, crush: true);
                ShowDamage(brain, dealt, ember: true);
                OnHitLanded();
            }
            _inFinisher = false;
        }

        public void OnPlayerHit() => Combo = 0;

        private void RunHitStop(float duration) => RequestDip(0.05f, duration);

        /// <summary>Scripted flourish (last enemy of a wave). Obeys the stacking rule.</summary>
        public void PlaySlowMo(float duration) => RequestDip(0.3f, duration);

        private void RunSlowMo(float duration) => RequestDip(0.35f, duration);

        /// <summary>
        /// Stacking rule: the deepest requested dip wins and the longest duration
        /// extends it, so a crush landing mid-flurry still reads as heavier than
        /// the light hits around it. Previously the first dip locked everything
        /// out until it expired, which swallowed the crush and the perfect-dodge
        /// slow-mo whenever they arrived a frame late.
        /// </summary>
        /// <summary>
        /// True while a menu owns time. The hit-stop dip must respect it, or a dip
        /// expiring behind an open pause menu would silently resume the game.
        /// </summary>
        public static bool TimeFrozen { get; set; }

        private void RequestDip(float scale, float duration)
        {
            if (TimeFrozen) return;
            if (_dipT <= 0f)
            {
                _dipScale = scale;
                _dipT = duration;
            }
            else
            {
                _dipScale = Mathf.Min(_dipScale, scale);
                _dipT = Mathf.Max(_dipT, duration);
            }
            Time.timeScale = _dipScale;
        }

        /// <summary>
        /// Driven from Update rather than a coroutine: WaitForSecondsRealtime
        /// allocated on every single hit, and a coroutine cancelled mid-dip (the
        /// player dying into a scene load) left Time.timeScale stuck at 0.05.
        /// </summary>
        private void UpdateTimeDip()
        {
            if (_dipT <= 0f) return;
            _dipT -= Time.unscaledDeltaTime;
            if (_dipT > 0f) return;
            _dipScale = 1f;
            if (!TimeFrozen) Time.timeScale = 1f;
        }

        /// <summary>Never leave time dilated behind us.</summary>
        private void OnDisable()
        {
            TimeFrozen = false; // a scene load must never leave time owned
            if (_dipT <= 0f) return;
            _dipT = 0f;
            _dipScale = 1f;
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Marsh Hook cleave hazard: a glowing puddle that ticks damage on enemies
    /// standing in it. Built from a quad at runtime, self-destructs after 4s.
    /// </summary>
    public class PoisonPuddle : MonoBehaviour
    {
        private const float Radius = 2.1f;
        private static Shader _glow; // Shader.Find is a string lookup; do it once
        private float _life = 4f, _tick;
        private Material _mat;

        public static void Spawn(Vector3 pos)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "PoisonPuddle";
            quad.transform.position = new Vector3(pos.x, 0.06f, pos.z);
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = Vector3.one * (Radius * 2f);
            var p = quad.AddComponent<PoisonPuddle>();
            p._mat = new Material(_glow != null ? _glow : _glow = Shader.Find("Emberline/Glow"));
            p._mat.color = new Color(0.35f, 0.85f, 0.4f, 0.5f);
            quad.GetComponent<Renderer>().material = p._mat;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }
            _mat.color = new Color(0.35f, 0.85f, 0.4f, 0.5f * Mathf.Clamp01(_life / 1.2f)
                + 0.08f * Mathf.Sin(Time.time * 7f));

            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 0.5f;
            foreach (var brain in Emberline.Enemies.EnemyBrain.Active)
            {
                if (brain == null || brain.Dead) continue;
                var d = Vector3.Distance(brain.transform.position, transform.position);
                if (d > Radius) continue;
                var dealt = brain.TakeHit(3f, transform.position);
                if (dealt > 0f)
                    UI.FloatingText.Spawn(brain.transform.position + Vector3.up * 1.9f,
                        Mathf.RoundToInt(dealt).ToString(), new Color(0.45f, 0.9f, 0.5f), 0.8f);
            }
        }
    }
}
