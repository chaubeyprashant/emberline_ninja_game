#if UNITY_EDITOR
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Player;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// The twelve Combat 2.0 acceptance scenarios, played by a scripted Renzo
    /// whose habits are the point: he spams, he blocks, he dodges early, he
    /// runs, he circles — and the enemies must answer each habit the way the
    /// brief says. Assertions read the AI telemetry; nothing here reads AI
    /// internals the player could not see.
    /// </summary>
    public class CombatScenarioDriver : MonoBehaviour
    {
        public enum Habit { Spam, BlockSpam, EarlyDodge, Retreat, Circle, ParryTiming, Passive, Aggressive }

        public struct Scenario
        {
            public string name; public EnemyKind[] kinds; public string foe; public Habit habit; public float seconds;
            public System.Func<string> check;   // null = pass; else the failure reason
        }

        public System.Action<int> onFinished;
        private static readonly StringBuilder Report = new();
        private int _index = -1;
        private float _t, _healT, _habitT;
        private int _failures;
        private GameManager _gm;
        private Player.PlayerLocomotion _loco;
        private CombatController _combat;
        private Health _hp;
        private int _killsSeen;
        private float _bossHp0 = -1f;

        private static Scenario[] Scenarios => new[]
        {
            new Scenario { name = "1 raider vs spam", kinds = new[] { EnemyKind.Bandit }, habit = Habit.Spam, seconds = 30f,
                check = () => AiTelemetry.DistinctAttacks >= 3 && AiTelemetry.MaxSameAttackRun <= 2 ? null
                    : $"raider repeated itself: distinct={AiTelemetry.DistinctAttacks} maxRun={AiTelemetry.MaxSameAttackRun}" },
            new Scenario { name = "2 samurai vs block spam", kinds = new[] { EnemyKind.Samurai }, habit = Habit.BlockSpam, seconds = 35f,
                check = () => AiTelemetry.GuardBreaks >= 1 ? null : "no guard-break against a blocker" },
            new Scenario { name = "3 elite vs early dodge", kinds = new[] { EnemyKind.EliteWarrior }, habit = Habit.EarlyDodge, seconds = 35f,
                check = () => AiTelemetry.Delayed >= 1 ? null : "no delayed attack against an early dodger" },
            new Scenario { name = "4 pike vs retreat", kinds = new[] { EnemyKind.PikeGuard }, habit = Habit.Retreat, seconds = 30f,
                check = () => AiTelemetry.Thrusts + AiTelemetry.GapClosers >= 2 ? null : "pike guard did not thrust at a retreating player" },
            new Scenario { name = "5 axe vs circling", kinds = new[] { EnemyKind.RaiderAxe }, habit = Habit.Circle, seconds = 30f,
                check = () => AiTelemetry.Sweeps >= 1 ? null : "no sweep against a circling player" },
            new Scenario { name = "6 samurai vs parry", kinds = new[] { EnemyKind.Samurai }, habit = Habit.ParryTiming, seconds = 35f,
                check = () => AiTelemetry.ParryRecoils >= 1 ? null : "no recoil after a perfect parry" },
            new Scenario { name = "7 ally death", kinds = new[] { EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Samurai }, habit = Habit.Aggressive, seconds = 40f,
                check = () => AiTelemetry.AllyReactions >= 1 ? null : "nobody reacted to an ally dying" },
            new Scenario { name = "8 four surround", kinds = new[] { EnemyKind.Samurai, EnemyKind.Bandit, EnemyKind.PikeGuard, EnemyKind.Assassin }, habit = Habit.Passive, seconds = 40f,
                check = () => AiTelemetry.MaxSimultaneousAttackers <= 3 && AiTelemetry.Attacks >= 6 ? null
                    : $"crowd control failed: maxSimul={AiTelemetry.MaxSimultaneousAttackers} attacks={AiTelemetry.Attacks}" },
            new Scenario { name = "9 archer keeps distance", kinds = new[] { EnemyKind.Ranged }, habit = Habit.Aggressive, seconds = 30f,
                check = () => AiTelemetry.ArcherMinDistance >= 2.5f || AiTelemetry.Retreats >= 1 ? null
                    : $"archer let the player close: min distance {AiTelemetry.ArcherMinDistance:0.0}" },
            new Scenario { name = "10 assassin behind", kinds = new[] { EnemyKind.Assassin }, habit = Habit.Passive, seconds = 35f,
                check = () => AiTelemetry.Backstabs + AiTelemetry.GapClosers >= 1 ? null : "assassin never used a back attack or a dash" },
            new Scenario { name = "11 missed heavy recovers", kinds = new[] { EnemyKind.RaiderAxe }, habit = Habit.EarlyDodge, seconds = 35f,
                check = () => AiTelemetry.MissedHeavies >= 1 ? null : "no heavy missed and recovered long" },
            new Scenario { name = "12 boss phases", kinds = new[] { EnemyKind.Chief }, habit = Habit.Aggressive, seconds = 60f,
                check = () => AiTelemetry.PhaseChanges >= 1 ? null : "the boss never changed phase" },
        };

        private void Start()
        {
            AudioListener.volume = 0f;
            AiTelemetry.LogEvery = 1e9f;
            _gm = SceneRefs.Game;
            _loco = SceneRefs.Motor;
            _combat = _loco != null ? _loco.GetComponent<CombatController>() : null;
            _hp = _gm != null ? _gm.PlayerHealth : null;
            if (_gm == null || _loco == null || _combat == null) { Debug.LogError("[SCN] scene not wired"); onFinished?.Invoke(2); return; }
            _hp?.SetMax(100000f);
            Next();
        }

        private void Next()
        {
            if (_index >= 0) Finish(Scenarios[_index]);
            _index++;
            if (_index >= Scenarios.Length)
            {
                Debug.Log("[SCN] TABLE\n" + Report);
                Debug.Log(_failures == 0 ? "[SCN] ALL PASSED" : $"[SCN] {_failures} FAILED");
                EmberInput.Scripted = null;
                onFinished?.Invoke(_failures == 0 ? 0 : 1);
                enabled = false;
                return;
            }
            for (var i = EnemyBrain.Active.Count - 1; i >= 0; i--) EnemyPool.Release(EnemyBrain.Active[i]);
            _loco.TryWarpTo(Vector3.zero);
            var s = Scenarios[_index];
            foreach (var k in s.kinds) _gm.SpawnOne(k, false);
            if (!string.IsNullOrEmpty(s.foe)) _gm.SpawnNamed(s.kinds[0], s.foe);
            // Scenario 12 wants the phase turn: bring the boss to the edge of it.
            _bossHp0 = -1f;
            AiTelemetry.Reset();
            _t = s.seconds; _habitT = 0f; _killsSeen = _gm.Kills;
            Debug.Log($"[SCN] start '{s.name}' habit={s.habit}");
        }

        private void Finish(Scenario s)
        {
            var why = s.check?.Invoke();
            var ok = why == null;
            if (!ok) _failures++;
            var line = $"{s.name,-28} attacks={AiTelemetry.Attacks} distinct={AiTelemetry.DistinctAttacks} maxRun={AiTelemetry.MaxSameAttackRun} " +
                       $"gb={AiTelemetry.GuardBreaks} delayed={AiTelemetry.Delayed} thrust={AiTelemetry.Thrusts} sweep={AiTelemetry.Sweeps} " +
                       $"feint={AiTelemetry.Feints} back={AiTelemetry.Backstabs} missedHeavy={AiTelemetry.MissedHeavies} recoil={AiTelemetry.ParryRecoils} " +
                       $"allyReact={AiTelemetry.AllyReactions} phases={AiTelemetry.PhaseChanges} maxSimul={AiTelemetry.MaxSimultaneousAttackers}" +
                       (ok ? "" : $"  ← {why}");
            Report.Append(ok ? "pass  " : "FAIL  ").Append(line).Append('\n');
            Debug.Log("[SCN] " + (ok ? "pass  " : "FAIL  ") + line);
        }

        private void Update()
        {
            if (_index < 0 || _index >= Scenarios.Length) return;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            if (_hp != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.5f; _hp.Heal(100000f); }
            var s = Scenarios[_index];
            _t -= Time.deltaTime;

            // Keep the field populated: weakened enemies, respawned when the habit killed them.
            var alive = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                alive++;
                // Bosses keep their HP so phases can happen; the rest are thin.
                if (!e.IsBossTarget && e.maxHp > 60f) { e.maxHp = 60f; e.SyncHpToMax(); }
                if (e.IsBossTarget && _bossHp0 < 0f) _bossHp0 = e.maxHp;
            }
            if (alive == 0 && _t > 3f)
            {
                foreach (var k in s.kinds) _gm.SpawnOne(k, false);
            }
            if (_t <= 0f) { Next(); return; }
        }

        private void LateUpdate()
        {
            if (_index < 0 || _index >= Scenarios.Length || _loco == null) return;
            EmberInput.Scripted = Vector2.zero;
            if (GameManager.CinematicActive) return;
            var target = Nearest(out var dist);
            var to = target != null ? target.transform.position - _loco.transform.position : Vector3.forward;
            to.y = 0f;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
            _habitT += Time.deltaTime;

            switch (Scenarios[_index].habit)
            {
                case Habit.Spam:
                    _loco.SetFacing(dir);
                    if (dist > 2f) EmberInput.Scripted = Cam(dir);
                    else EmberInput.PressStrike();
                    break;
                case Habit.BlockSpam:
                    // Hold the guard whenever they wind up; otherwise stand at reach.
                    _loco.SetFacing(dir);
                    if (dist > 2.6f) EmberInput.Scripted = Cam(dir);
                    else if (target != null && target.InWindupOrDash) { EmberInput.PressCleave(); EmberInput.SetCleaveHeld(true); }
                    else { EmberInput.SetCleaveHeld(false); if (_habitT % 2f < 0.05f) EmberInput.PressStrike(); }
                    break;
                case Habit.EarlyDodge:
                    _loco.SetFacing(dir);
                    if (dist > 2.4f) EmberInput.Scripted = Cam(dir);
                    else if (target != null && target.InWindupOrDash && _loco.FlickerCooldownRemaining <= 0f)
                    { EmberInput.Scripted = Cam(-dir); EmberInput.PressFlicker(); }  // the instant the ring shows: too early
                    else if (_habitT % 1.5f < 0.05f) EmberInput.PressStrike();
                    break;
                case Habit.Retreat:
                    _loco.SetFacing(dir);
                    if (dist < 4.5f) EmberInput.Scripted = Cam(-dir);
                    else if (dist > 6f) EmberInput.Scripted = Cam(dir);
                    break;
                case Habit.Circle:
                {
                    _loco.SetFacing(dir);
                    var side = new Vector3(-dir.z, 0f, dir.x);
                    EmberInput.Scripted = Cam(side + (dist > 2.6f ? dir * 0.4f : dist < 1.6f ? -dir * 0.4f : Vector3.zero));
                    if (_habitT % 2.5f < 0.05f) EmberInput.PressStrike();
                    break;
                }
                case Habit.ParryTiming:
                    // Guard at the last moment of the wind-up: a perfect parry.
                    _loco.SetFacing(dir);
                    if (dist > 2.6f) EmberInput.Scripted = Cam(dir);
                    else if (target != null && target.WindupRemaining > 0f && target.WindupRemaining < 0.14f) EmberInput.PressCleave();
                    else if (_habitT % 1.8f < 0.05f) EmberInput.PressStrike();
                    break;
                case Habit.Passive:
                    _loco.SetFacing(dir);
                    if (_habitT % 3f < 0.05f) EmberInput.PressStrike();
                    break;
                default: // Aggressive
                    _loco.SetFacing(dir);
                    if (dist > 2f) EmberInput.Scripted = Cam(dir);
                    else if (_habitT % 0.6f < 0.05f) EmberInput.PressStrike();
                    else if (_habitT % 2.4f < 0.05f) EmberInput.PressCleave();
                    break;
            }
        }

        private EnemyBrain Nearest(out float dist)
        {
            EnemyBrain best = null; var bd = float.MaxValue;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead || !e.gameObject.activeInHierarchy) continue;
                var d = (e.transform.position - _loco.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            dist = best != null ? Mathf.Sqrt(bd) : float.MaxValue;
            return best;
        }

        private static Vector2 Cam(Vector3 worldDir)
        {
            var cam = SceneRefs.Cam != null ? SceneRefs.Cam.transform : null;
            if (cam == null) return new Vector2(worldDir.x, worldDir.z);
            var f = cam.forward; f.y = 0f; f.Normalize();
            var r = cam.right; r.y = 0f; r.Normalize();
            return new Vector2(Vector3.Dot(worldDir, r), Vector3.Dot(worldDir, f));
        }
    }
}
#endif
