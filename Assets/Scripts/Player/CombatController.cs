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

        [Header("Feel")]
        [SerializeField] private float softLockRange = 6.5f;
        [SerializeField] private float lungeSpeed = 5.5f;

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        private PlayerLocomotion _motor;
        private SenGates _sen;
        private CameraRig _rig;
        private NinjaRig _ninja;
        private int _stage;
        private float _chainTimer, _cleaveCd, _pendingCleave = -1f, _comboTimer;
        private static bool _hitStopping;

        private void Awake()
        {
            _motor = GetComponent<PlayerLocomotion>();
            _sen = GetComponent<SenGates>();
            _ninja = GetComponent<NinjaRig>();
        }

        private void Start() => _rig = FindFirstObjectByType<CameraRig>();

        private void Update()
        {
            _chainTimer = Mathf.Max(0, _chainTimer - Time.deltaTime);
            _cleaveCd = Mathf.Max(0, _cleaveCd - Time.deltaTime);
            if (_comboTimer > 0 && (_comboTimer -= Time.deltaTime) <= 0) Combo = 0;

            if (_pendingCleave >= 0 && (_pendingCleave -= Time.deltaTime) < 0)
            {
                StrikeArc(cleaveRange, cleaveArcDeg, cleaveDamage, crush: true);
                _motor.Busy = false;
            }

            // EmberInput: touch buttons on device, mouse/keyboard in the editor.
            if (EmberInput.ConsumeStrike()) Strike();
            if (EmberInput.ConsumeCleave()) Cleave();
            if (EmberInput.ConsumeFlicker()) _motor.TryFlicker();
            if (EmberInput.ConsumeSurge()) Surge();
        }

        public void Strike()
        {
            if (_pendingCleave >= 0 || _motor.Invulnerable) return;
            SoftLockFacing();
            _stage = _chainTimer > 0 ? (_stage % 3) + 1 : 1;
            _chainTimer = chainWindow;
            _motor.Impulse(_motor.Facing * lungeSpeed);
            Sfx3D.Slash();
            _ninja?.PlayOneShot(_stage == 2 ? RigPose.Strike2 : RigPose.Strike1, 0.28f);
            StrikeArc(strikeRange, strikeArcDeg, strikeDamage[_stage - 1], crush: _stage == 3);
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
                    brain.TakeHit(surgeDamage, transform.position, crush: true);
                    ShowDamage(brain, surgeDamage, ember: true);
                    OnHitLanded();
                }
            }
        }

        private void StrikeArc(float range, float arcDeg, float damage, bool crush)
        {
            var hitAny = false;
            foreach (var brain in TargetsWithin(range, arcDeg))
            {
                brain.TakeHit(damage, transform.position, crush);
                ShowDamage(brain, damage, ember: crush);
                FxPools.Sparks(brain.transform.position + Vector3.up * 1.1f,
                    crush ? new Color(1f, 0.55f, 0.3f) : new Color(0.9f, 0.95f, 1f));
                OnHitLanded();
                hitAny = true;
            }
            if (hitAny)
            {
                Sfx3D.Hit();
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
        }

        public void OnPlayerHit() => Combo = 0;

        private void RunHitStop(float duration)
        {
            if (!_hitStopping) StartCoroutine(HitStop(duration));
        }

        private IEnumerator HitStop(float duration)
        {
            _hitStopping = true;
            var prev = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prev;
            _hitStopping = false;
        }
    }
}
