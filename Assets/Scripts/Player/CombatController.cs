using System.Collections;
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
        [SerializeField] private float lungeSpeed = 5.5f;

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        /// <summary>0 = ready, 1 = just used. For HUD cooldown rings.</summary>
        public float CleaveCd01 => cleaveCooldown > 0f ? _cleaveCd / cleaveCooldown : 0f;

        /// <summary>0 = ready, 1 = just thrown. For the HUD kunai ring.</summary>
        public float KunaiCd01 => kunaiCooldown > 0f ? _kunaiCd / kunaiCooldown : 0f;

        /// <summary>True inside the perfect-dodge counter window (next strike ×2).</summary>
        public bool CounterActive => _counterT > 0f;

        private PlayerLocomotion _motor;
        private SenGates _sen;
        private CameraRig _rig;
        private CharacterRig _ninja;
        private WeaponDef _weapon;
        private int _stage;
        private float _chainTimer, _cleaveCd, _kunaiCd, _pendingCleave = -1f, _comboTimer;
        private float _counterT, _lanternTick;
        private bool _inFinisher;
        private static bool _hitStopping;

        private void Awake()
        {
            _motor = GetComponent<PlayerLocomotion>();
            _sen = GetComponent<SenGates>();
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
            strikeDamage = w.strikeDamage;
            strikeRange = w.strikeRange;
            strikeArcDeg = w.strikeArcDeg;
            chainWindow = w.chainWindow + (SkillTree.Has("combo_window") ? 0.2f : 0f);
            lungeSpeed = w.lungeSpeed;
            cleaveDamage = w.cleaveDamage * (SkillTree.Has("cleave_dmg") ? 1.25f : 1f);
            cleaveRange = w.cleaveRange;
            cleaveArcDeg = w.cleaveArcDeg;
            cleaveWindup = w.cleaveWindup;
            cleaveCooldown = w.cleaveCooldown;
            if (SkillTree.Has("surge_radius")) surgeRadius *= 1.2f;

            // Cosmetic blade finish: recolors the trail and the weapon prop.
            var finish = BladeFinish.Current;
            var trailColor = finish.starsRequired > 0 ? finish.trail : w.trailColor;
            var trail = GetComponentInChildren<TrailRenderer>(true);
            if (trail != null) trail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.8f);
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", finish.blade);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name.StartsWith("Prop_") || r.transform.parent != null
                    && r.transform.parent.name.StartsWith("Prop_"))
                    r.SetPropertyBlock(mpb);
        }

        private void Update()
        {
            if (GameManager.CinematicActive) return;
            _chainTimer = Mathf.Max(0, _chainTimer - Time.deltaTime);
            _cleaveCd = Mathf.Max(0, _cleaveCd - Time.deltaTime);
            _kunaiCd = Mathf.Max(0, _kunaiCd - Time.deltaTime);
            _counterT = Mathf.Max(0, _counterT - Time.deltaTime);
            if (_comboTimer > 0 && (_comboTimer -= Time.deltaTime) <= 0) Combo = 0;
            UpdateLanternPassive();

            if (_pendingCleave >= 0 && (_pendingCleave -= Time.deltaTime) < 0)
            {
                StrikeArc(cleaveRange, cleaveArcDeg, cleaveDamage, crush: true);
                if (_weapon != null && _weapon.poisonCleave)
                    PoisonPuddle.Spawn(transform.position + _motor.Facing * 1.6f);
                _motor.Busy = false;
            }

            // EmberInput: touch buttons on device, mouse/keyboard in the editor.
            if (EmberInput.ConsumeStrike()) Strike();
            if (EmberInput.ConsumeCleave()) Cleave();
            if (EmberInput.ConsumeFlicker()) _motor.TryFlicker();
            if (EmberInput.ConsumeSurge()) Surge();
            if (EmberInput.ConsumeKunai()) ThrowKunai();
        }

        public void Strike()
        {
            if (_pendingCleave >= 0 || _motor.Invulnerable) return;
            SoftLockFacing();
            _stage = _chainTimer > 0 ? (_stage % 3) + 1 : 1;
            _chainTimer = chainWindow;
            _motor.Impulse(_motor.Facing * lungeSpeed);
            Sfx3D.Slash();
            _ninja?.PlayOneShot(_stage switch
            {
                2 => RigPose.Strike2,
                3 => RigPose.Strike3,
                _ => RigPose.Strike1,
            }, _weapon != null ? _weapon.strikeAnimTime : 0.28f);
            var dmg = strikeDamage[_stage - 1];
            if (CounterActive) { dmg *= 2f; _counterT = 0f; }
            StrikeArc(strikeRange, strikeArcDeg, dmg, crush: _stage == 3);
        }

        /// <summary>Perfect dodge: slow-mo counter window; Storm Tanto also parries.</summary>
        public void OnPerfectDodge()
        {
            _counterT = 0.5f;
            RunSlowMo(0.28f);
            if (_weapon == null || !_weapon.parryOnPerfectDodge) return;
            foreach (var brain in Targets())
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
            foreach (var brain in Targets())
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

        public void Cleave()
        {
            if (_cleaveCd > 0 || _motor.Invulnerable) return;
            _cleaveCd = cleaveCooldown;
            _pendingCleave = cleaveWindup;
            _motor.Busy = true;
            Sfx3D.Slash();
            _ninja?.PlayOneShot(RigPose.Cleave, cleaveWindup + 0.3f);
            SoftLockFacing();
            _motor.Impulse(_motor.Facing * (lungeSpeed * 0.6f));
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
            Kunai.Spawn(transform.position + Vector3.up * 1.3f + _motor.Facing * 0.5f,
                _motor.Facing, dmg, this);
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
            foreach (var brain in Targets())
            {
                if (Vector3.Distance(brain.transform.position, transform.position) <= surgeRadius)
                {
                    var dealt = brain.TakeHit(surgeDamage, transform.position, crush: true);
                    ShowDamage(brain, dealt, ember: true);
                    OnHitLanded();
                }
            }
        }

        private void StrikeArc(float range, float arcDeg, float damage, bool crush)
        {
            var hitAny = false;
            foreach (var brain in TargetsWithin(range, arcDeg))
            {
                var dealt = brain.TakeHit(damage, transform.position, crush);
                ShowDamage(brain, dealt, ember: crush);
                FxPools.Sparks(brain.transform.position + Vector3.up * 1.1f,
                    crush ? new Color(1f, 0.55f, 0.3f) : new Color(0.9f, 0.95f, 1f));
                FxPools.Slash(brain.transform.position + Vector3.up * 1.2f, _motor.Facing, crush);
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

            if (hitAny)
            {
                if (crush) Sfx3D.HitCrush(); else Sfx3D.Hit();
                _rig?.Shake(crush ? 5f : 2.5f, 0.18f);
                RunHitStop(crush ? 0.06f : 0.04f);
            }
        }

        private void ShowDamage(EnemyBrain brain, float damage, bool ember)
        {
            FloatingText.Spawn(
                brain.transform.position + Vector3.up * 2.1f,
                Mathf.RoundToInt(damage).ToString(),
                ember ? new Color(1f, 0.5f, 0.3f) : new Color(0.95f, 0.94f, 0.9f));
        }

        private IEnumerable<EnemyBrain> TargetsWithin(float range, float arcDeg)
        {
            foreach (var brain in Targets())
            {
                var to = brain.transform.position - transform.position;
                to.y = 0;
                var d = to.magnitude;
                if (d > range) continue;
                // Point-blank hits connect regardless of facing — no more whiffs
                // when an enemy is standing on top of you.
                if (d > 1.7f && Vector3.Angle(_motor.Facing, to) > arcDeg * 0.5f) continue;
                yield return brain;
            }
        }

        private List<EnemyBrain> Targets()
        {
            var list = new List<EnemyBrain>(EnemyBrain.Active.Count);
            foreach (var b in EnemyBrain.Active)
                if (b != null && !b.Dead) list.Add(b);
            return list;
        }

        private void SoftLockFacing()
        {
            EnemyBrain best = null;
            var bestD = softLockRange;
            foreach (var brain in Targets())
            {
                var d = Vector3.Distance(brain.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = brain; }
            }
            // The old bug: rotating the transform but not Facing meant the arc
            // check used a stale direction and strikes whiffed. SetFacing fixes both.
            if (best != null)
                _motor.SetFacing(best.transform.position - transform.position);
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
            foreach (var brain in Targets())
            {
                if (Vector3.Distance(brain.transform.position, transform.position) > 4.5f) continue;
                var dealt = brain.TakeHit(dmg, transform.position, crush: true);
                ShowDamage(brain, dealt, ember: true);
                OnHitLanded();
            }
            _inFinisher = false;
        }

        public void OnPlayerHit() => Combo = 0;

        private void RunHitStop(float duration)
        {
            if (!_hitStopping) StartCoroutine(TimeDip(0.05f, duration));
        }

        private void RunSlowMo(float duration)
        {
            if (!_hitStopping) StartCoroutine(TimeDip(0.35f, duration));
        }

        private IEnumerator TimeDip(float scale, float duration)
        {
            _hitStopping = true;
            var prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prev;
            _hitStopping = false;
        }
    }

    /// <summary>
    /// Marsh Hook cleave hazard: a glowing puddle that ticks damage on enemies
    /// standing in it. Built from a quad at runtime, self-destructs after 4s.
    /// </summary>
    public class PoisonPuddle : MonoBehaviour
    {
        private const float Radius = 2.1f;
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
            p._mat = new Material(Shader.Find("Emberline/Glow"));
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
