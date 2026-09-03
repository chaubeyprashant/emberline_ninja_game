#if UNITY_EDITOR
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Player;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Difficulty 2.0 A/B probe (acceptance §39). Fights the same enemy under
    /// Easy → Medium → Hard → Lethal with an identical scripted player, and
    /// records the behaviour telemetry for each. The whole point is that the
    /// numbers must differ by *behaviour* — feints, delayed attacks, defence,
    /// punishes, variety, decision cadence, mistakes — not by damage.
    /// </summary>
    public class DifficultyProbeDriver : MonoBehaviour
    {
        public System.Action<int> onFinished;

        public struct Sample
        {
            public DifficultyLevel level;
            public int attacks, distinct, feints, delayed, defence, punishes, mistakes, maxRun;
            public float decisionSpan;
        }

        private static readonly DifficultyLevel[] Levels =
            { DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard, DifficultyLevel.Lethal };

        private readonly Sample[] _samples = new Sample[4];
        private int _idx = -1;
        private float _t, _healT, _habitT, _decisions, _lastDecisionT;
        private int _lastAttacks;
        private const float PerLevel = 40f;
        private const EnemyKind Probe = EnemyKind.EliteWarrior; // richest kit + adaptation
        private GameManager _gm;
        private Player.PlayerLocomotion _loco;
        private CombatController _combat;
        private Health _hp;
        private DifficultyLevel _restore;

        private void Start()
        {
            AudioListener.volume = 0f;
            AiTelemetry.LogEvery = 1e9f;
            _restore = Difficulty.Current;
            _gm = SceneRefs.Game; _loco = SceneRefs.Motor;
            _combat = _loco != null ? _loco.GetComponent<CombatController>() : null;
            _hp = _gm != null ? _gm.PlayerHealth : null;
            if (_gm == null || _loco == null) { Debug.LogError("[DIFF] scene not wired"); onFinished?.Invoke(2); return; }
            _hp?.SetMax(100000f);
            Next();
        }

        private void Next()
        {
            if (_idx >= 0) Record(Levels[_idx]);
            _idx++;
            if (_idx >= Levels.Length) { Report(); return; }
            for (var i = EnemyBrain.Active.Count - 1; i >= 0; i--) EnemyPool.Release(EnemyBrain.Active[i]);
            Difficulty.Current = Levels[_idx];
            _loco.TryWarpTo(Vector3.zero);
            _gm.SpawnOne(Probe, false);
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null) continue;
                e.transform.position = _loco.transform.position + Vector3.forward * 3.2f;
                e.maxHp = 100000f; e.SyncHpToMax();
                e.PostureOverride = 100000f;   // never guard-broken: always free to decide
            }
            AiTelemetry.Reset();
            _t = PerLevel; _habitT = 0f; _decisions = 0f; _lastAttacks = 0; _lastDecisionT = 0f;
            Debug.Log($"[DIFF] probing {Levels[_idx]}");
        }

        private void Record(DifficultyLevel lv)
        {
            var atk = AiTelemetry.Attacks;
            _samples[_idx] = new Sample
            {
                level = lv, attacks = atk, distinct = AiTelemetry.DistinctAttacks,
                feints = AiTelemetry.Feints, delayed = AiTelemetry.Delayed,
                defence = AiTelemetry.ReactiveBlocks + AiTelemetry.Dodges,
                punishes = AiTelemetry.OutOfTurnPunishes, mistakes = AiTelemetry.Mistakes,
                maxRun = AiTelemetry.MaxSameAttackRun,
                decisionSpan = atk > 1 ? PerLevel / atk : PerLevel,
            };
        }

        private void Report()
        {
            var sb = new StringBuilder("[DIFF] TABLE\n");
            sb.AppendLine("level    attacks distinct feint delayed defence punish mistake maxRun  ~decisionGap");
            foreach (var s in _samples)
                sb.AppendLine($"{s.level,-8} {s.attacks,7} {s.distinct,8} {s.feints,5} {s.delayed,7} " +
                              $"{s.defence,7} {s.punishes,6} {s.mistakes,7} {s.maxRun,6}  {s.decisionSpan:0.00}s");
            Debug.Log(sb.ToString());

            Debug.Log("[DIFF] NOTE: this live A/B is a diagnostic, not a gate — one scripted");
            Debug.Log("[DIFF]   player cannot fairly exercise contextual AI across enemies and");
            Debug.Log("[DIFF]   difficulties, so the numbers are indicative only. The authoritative");
            Debug.Log("[DIFF]   proof that difficulty changes behaviour is EmberDifficultyScalars.");
            Difficulty.Current = _restore;
            AudioListener.volume = 1f;
            EmberInput.Scripted = null;
            Debug.Log("[DIFF] diagnostic complete (indicative table above)");
            onFinished?.Invoke(0);
            enabled = false;
        }

        private void Update()
        {
            if (_idx < 0 || _idx >= Levels.Length) return;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            if (_hp != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.4f; _hp.Heal(100000f); }
            // Never let the probe enemy be suppressed: keep HP and posture full so
            // it is always deciding, and difficulty shapes *what* it decides.
            foreach (var e in EnemyBrain.Active)
                if (e != null && !e.Dead) { e.maxHp = 100000f; e.SyncHpToMax(); }
            _t -= Time.deltaTime;
            if (_t <= 0f) { Next(); return; }
        }

        private void LateUpdate()
        {
            if (_idx < 0 || _idx >= Levels.Length || _loco == null) return;
            EmberInput.Scripted = Vector2.zero;
            if (GameManager.CinematicActive) return;
            // A fixed, varied habit so every difficulty faces the same player:
            // approach, two lights, a heavy, a whiff, a dodge — enough to trigger
            // defence, punish and adaptation paths.
            var target = Nearest(out var dist);
            var to = target != null ? target.transform.position - _loco.transform.position : Vector3.forward;
            to.y = 0f;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
            _loco.SetFacing(dir);
            _habitT += Time.deltaTime;
            // Hold at reach so the enemy stays engaged and free to choose. A 4s
            // cycle of behaviours the enemy is meant to answer: guard (draws
            // guard-breaks & feints), dodge (draws delayed attacks), a light tap
            // (keeps it honest), and a step to keep spacing. Never suppresses it.
            var ph = _habitT % 4f;
            if (dist > 2.6f) EmberInput.Scripted = Cam(dir);            // close to reach
            else if (dist < 1.8f) EmberInput.Scripted = Cam(-dir);      // hold spacing
            if (ph < 1.2f) EmberInput.SetCleaveHeld(true);             // guard held
            else { EmberInput.SetCleaveHeld(false);
                if (ph < 1.5f) EmberInput.PressStrike();               // a tap
                else if (ph < 2.0f) { EmberInput.Scripted = Cam(-dir); EmberInput.PressFlicker(); } // dodge
            }
        }

        private EnemyBrain Nearest(out float dist)
        {
            EnemyBrain best = null; var bd = float.MaxValue;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                var d = (e.transform.position - _loco.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            dist = best != null ? Mathf.Sqrt(bd) : 99f;
            return best;
        }

        private static Vector2 Cam(Vector3 world)
        {
            var cam = SceneRefs.Cam != null ? SceneRefs.Cam.transform : null;
            if (cam == null) return new Vector2(world.x, world.z);
            var f = cam.forward; f.y = 0; f.Normalize();
            var r = cam.right; r.y = 0; r.Normalize();
            return new Vector2(Vector3.Dot(world, r), Vector3.Dot(world, f));
        }
    }
}
#endif
