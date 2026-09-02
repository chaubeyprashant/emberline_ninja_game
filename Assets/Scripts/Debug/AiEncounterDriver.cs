#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Play-mode encounter harness. Spawns each composition through the real
    /// spawn path, then plays the player like a slightly greedy human — light
    /// combos, a telegraphed heavy, a whiff, a dodge — so every enemy mechanic
    /// has something to react to. Records the AI telemetry and the highest
    /// number of simultaneous attackers ever observed. Editor-only: it never
    /// compiles into a device build.
    /// </summary>
    public class AiEncounterDriver : MonoBehaviour
    {
        public struct Scenario { public string name; public EnemyKind[] kinds; public int cap; }

        public static readonly Scenario[] Scenarios =
        {
            new() { name = "1 enemy (raider)", kinds = new[] { EnemyKind.Bandit }, cap = 1 },
            new() { name = "1 enemy (samurai)", kinds = new[] { EnemyKind.Samurai }, cap = 1 },
            new() { name = "1 enemy (assassin)", kinds = new[] { EnemyKind.Assassin }, cap = 1 },
            new() { name = "2 enemies", kinds = new[] { EnemyKind.Bandit, EnemyKind.Bandit }, cap = 2 },
            new() { name = "3 enemies", kinds = new[] { EnemyKind.Bandit, EnemyKind.PikeGuard, EnemyKind.Ranged }, cap = 2 },
            new() { name = "5+ enemies", kinds = new[] { EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.RaiderAxe,
                EnemyKind.PikeGuard, EnemyKind.Ranged, EnemyKind.Bandit }, cap = 3 },
            new() { name = "mixed", kinds = new[] { EnemyKind.Assassin, EnemyKind.Samurai, EnemyKind.RogueNinja,
                EnemyKind.Ranged }, cap = 3 },
            new() { name = "elite + support", kinds = new[] { EnemyKind.EliteWarrior, EnemyKind.Ranged,
                EnemyKind.PikeGuard }, cap = 2 },
            new() { name = "archer nest", kinds = new[] { EnemyKind.Ranged, EnemyKind.Ranged,
                EnemyKind.PikeGuard }, cap = 2 },
            new() { name = "boss + adds", kinds = new[] { EnemyKind.Kagachi, EnemyKind.Bandit, EnemyKind.Bandit,
                EnemyKind.Ranged }, cap = 3 },
        };

        public float secondsPerScenario = 30f;

        /// <summary>Run only scenarios whose name contains this, or all when null.</summary>
        public static string Filter;
        public System.Action<int> onFinished;

        private int _index = -1;
        private bool _skip;
        private bool _heavyStarted, _whiffStarted;
        private float _respawnT;
        private int _waves;
        private float _t, _cycleT, _healT;
        private int _step;
        private int _peakAlive, _damageEvents, _playerHits;
        private float _enemyHpLost, _enemyHpAtStart;
        private float _damageTaken;
        private readonly StringBuilder _report = new();
        private int _failures;
        private GameManager _gm;
        private Transform _player;
        private Player.PlayerLocomotion _loco;
        private Health _health;
        private Player.CombatController _combat;
        private float _openCommitted, _openHeavy, _openWhiff, _openDodge;

        private void Start()
        {
            _gm = SceneRefs.Game;
            _loco = SceneRefs.Motor;
            _player = _loco != null ? _loco.transform : null;
            _health = _gm != null ? _gm.PlayerHealth : null;
            _combat = _loco != null ? _loco.GetComponent<Player.CombatController>() : null;
            if (_gm == null || _player == null)
            {
                Debug.LogError("[ENC] scene not wired: GameManager or player missing");
                onFinished?.Invoke(2);
                return;
            }
            // Silent by default: this harness runs from the command line and
            // its combat audio would otherwise play out of the machine's speakers.
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            AiTelemetry.LogEvery = 1e9f; // the harness reads the counters itself
            if (_health != null)
            {
                _health.SetMax(100000f);
                _health.OnHurt += (amt, _) => { _damageTaken += amt; _damageEvents++; };
            }
            Next();
        }

        private void Next()
        {
            if (_index >= 0) Finish(Scenarios[_index]);
            _index++;
            if (_index >= Scenarios.Length)
            {
                Debug.Log("[ENC] TABLE\n" + _report);
                Debug.Log(_failures == 0 ? "[ENC] ALL PASSED" : $"[ENC] {_failures} FAILED");
                EmberInput.Scripted = null;
                onFinished?.Invoke(_failures == 0 ? 0 : 1);
                enabled = false;
                return;
            }
            // Clear the field and reset the player to the middle.
            for (var i = EnemyBrain.Active.Count - 1; i >= 0; i--)
                EnemyPool.Release(EnemyBrain.Active[i]);
            _loco.TryWarpTo(Vector3.zero);
            var s = Scenarios[_index];
            _skip = Filter != null && !s.name.Contains(Filter);
            if (!_skip) foreach (var k in s.kinds) _gm.SpawnOne(k, false);
            AiTelemetry.Reset();
            _t = _skip ? 0.01f : secondsPerScenario;
            _cycleT = 0f; _step = 0; _heavyStarted = _whiffStarted = false; _peakAlive = 0; _damageTaken = 0f; _damageEvents = 0;
            _playerHits = 0; _enemyHpLost = 0f; _enemyHpAtStart = TotalEnemyHp();
            _respawnT = 1.5f; _waves = 1;
            _openCommitted = _openHeavy = _openWhiff = _openDodge = 0f;
            Debug.Log($"[ENC] start '{s.name}' spawned={Alive()}");
        }

        private float TotalEnemyHp()
        {
            var sum = 0f;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var b = EnemyBrain.Active[i];
                if (b != null && b.gameObject.activeInHierarchy) sum += Mathf.Max(0f, b.Hp);
            }
            return sum;
        }

        private int Alive()
        {
            var n = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var b = EnemyBrain.Active[i];
                if (b != null && !b.Dead && b.gameObject.activeInHierarchy) n++;
            }
            return n;
        }

        private EnemyBrain Nearest()
        {
            EnemyBrain best = null; var bd = float.MaxValue;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var b = EnemyBrain.Active[i];
                if (b == null || b.Dead || !b.gameObject.activeInHierarchy) continue;
                var d = (b.transform.position - _player.position).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        private void Update()
        {
            if (_index < 0 || _index >= Scenarios.Length) return;
            if (Time.timeScale == 0f) Time.timeScale = 1f; // never let a menu pause the harness
            if (_health != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.5f; _health.Heal(100000f); }

            var alive = Alive();
            if (alive > _peakAlive) _peakAlive = alive;
            _enemyHpLost = Mathf.Max(_enemyHpLost, _enemyHpAtStart - TotalEnemyHp());
            if (_combat != null)
            {
                if (_combat.Committed) _openCommitted += Time.deltaTime;
                if (_combat.HeavyWindingUp) _openHeavy += Time.deltaTime;
                if (_combat.Whiffed) _openWhiff += Time.deltaTime;
                if (_combat.JustDodged) _openDodge += Time.deltaTime;
            }
            _t -= Time.deltaTime;
            if (_t <= 0f) { Next(); return; }
            // A wiped pack is re-sent rather than ending the scenario: otherwise a
            // composition the player kills quickly gets a much smaller sample of
            // AI behaviour than a tanky one, and the numbers stop being comparable.
            if (alive == 0 && (_respawnT -= Time.deltaTime) <= 0f)
            {
                _respawnT = 1.5f;
                _waves++;
                foreach (var k in Scenarios[_index].kinds) _gm.SpawnOne(k, false);
                _enemyHpAtStart = TotalEnemyHp() + _enemyHpLost;
            }

            // ---- the scripted player. One cycle ≈ 3.4s: approach, light-light,
            // heavy (telegraphed), deliberate whiff, dodge, pause. Greedy on purpose.
            var target = Nearest();
            var to = target != null ? target.transform.position - _player.position : Vector3.forward;
            to.y = 0f;
            var dist = to.magnitude;
            var dir = dist > 0.01f ? to / dist : Vector3.forward;

            EmberInput.Scripted = Vector2.zero;
            _cycleT += Time.deltaTime;

            // The scripted player is a slightly greedy human: close, two lights,
            // a committed heavy, a whiff into empty air, a dodge, then a pause.
            // Each opening is *verified* rather than assumed — a single button
            // press is silently eaten when the state machine refuses it, and a
            // harness that does not check would report an AI failure that is
            // really an input failure.
            switch (_step)
            {
                case 0: // close in
                    if (dist > 2.1f) EmberInput.Scripted = CamRelative(dir);
                    else Advance();
                    if (_cycleT > 4f) Advance();
                    break;

                case 1: // light, light
                    _loco.SetFacing(dir);
                    if (_cycleT < 0.02f || _cycleT > 0.45f && _cycleT < 0.5f) EmberInput.PressStrike();
                    if (_cycleT > 0.9f) Advance();
                    break;

                case 2: // heavy — the readable commitment enemies are meant to read
                    _loco.SetFacing(dir);
                    if (!_heavyStarted)
                    {
                        EmberInput.PressCleave();          // keep asking until it takes
                        if (_combat != null && _combat.HeavyWindingUp) _heavyStarted = true;
                        if (_cycleT > 2.2f) Advance();     // cooldown not up; move on
                    }
                    else if (_combat == null || !_combat.Committed) Advance();
                    break;

                case 3: // whiff into empty air, then stand in the recovery
                    _loco.SetFacing(-dir);
                    if (!_whiffStarted)
                    {
                        EmberInput.PressStrike();
                        if (_combat != null && _combat.Whiffed) _whiffStarted = true;
                        if (_cycleT > 1.2f) Advance();
                    }
                    else if (_cycleT > 0.9f) Advance();
                    break;

                case 4: // dodge away, leaving the landing exposed
                    if (_cycleT < 0.02f) { EmberInput.Scripted = CamRelative(-dir); EmberInput.PressFlicker(); }
                    if (_cycleT > 0.6f) Advance();
                    break;

                default: // stand still and eat whatever comes
                    _loco.SetFacing(dir);
                    if (_cycleT > 0.6f) { _step = 0; _cycleT = 0f; }
                    break;
            }
        }

        private void Advance()
        {
            _step++;
            _cycleT = 0f;
            _heavyStarted = _whiffStarted = false;
        }

        private static Vector2 CamRelative(Vector3 worldDir)
        {
            var cam = SceneRefs.Cam != null ? SceneRefs.Cam.transform : null;
            if (cam == null) return new Vector2(worldDir.x, worldDir.z);
            var f = cam.forward; f.y = 0f; f.Normalize();
            var r = cam.right; r.y = 0f; r.Normalize();
            return new Vector2(Vector3.Dot(worldDir, r), Vector3.Dot(worldDir, f));
        }

        private void Finish(Scenario s)
        {
            var line = $"{s.name,-20} spawned={s.kinds.Length}x{_waves} peakAlive={_peakAlive} " +
                       $"attacks={AiTelemetry.Attacks} maxSimul={AiTelemetry.MaxSimultaneousAttackers}/{s.cap} " +
                       $"punish={AiTelemetry.OutOfTurnPunishes} blocks={AiTelemetry.ReactiveBlocks} " +
                       $"dodges={AiTelemetry.Dodges} ripostes={AiTelemetry.Ripostes} retreats={AiTelemetry.Retreats} " +
                       $"guards={AiTelemetry.GuardHolds} protects={AiTelemetry.ProtectMoves} " +
                       $"shortStaggers={AiTelemetry.StaggersShortened} hitsOnPlayer={_damageEvents} " +
                       $"dmg={_damageTaken:0} alive@end={Alive()} " +
                       $"enemyHpLost={_enemyHpLost:0} openings[committed={_openCommitted:0.0}s " +
                       $"heavy={_openHeavy:0.0}s whiff={_openWhiff:0.0}s postDodge={_openDodge:0.0}s]";
            if (_skip) { _report.Append("skip  ").Append(s.name).Append('\n'); return; }
            var ok = AiTelemetry.MaxSimultaneousAttackers <= s.cap && AiTelemetry.Attacks > 0;
            if (!ok)
                for (var i = 0; i < EnemyBrain.Active.Count; i++)
                {
                    var b = EnemyBrain.Active[i];
                    if (b != null && b.gameObject.activeInHierarchy)
                        Debug.Log("[ENC]   why: " + b.DebugLine);
                }
            if (!ok)
                Debug.Log($"[ENC]   world: timeScale={Time.timeScale} cinematic={GameManager.CinematicActive} " +
                          $"frozen={Player.CombatController.TimeFrozen} tokens={(SceneRefs.Tokens != null)} " +
                          $"squad={(SquadCoordinator.Instance != null)} phase={(_gm != null ? _gm.State.ToString() : "-")}");
            if (!ok) _failures++;
            _report.Append(ok ? "pass  " : "FAIL  ").Append(line).Append('\n');
            Debug.Log("[ENC] " + (ok ? "pass  " : "FAIL  ") + line);
        }
    }
}
#endif
