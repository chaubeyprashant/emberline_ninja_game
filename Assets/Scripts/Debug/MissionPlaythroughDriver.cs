#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Plays all ten story missions start to finish with a bot, to prove every
    /// authored stage can actually be completed. Design validation cannot catch
    /// a stage that is unreachable in play — a marker nobody can stand on, a
    /// wave that waits on an enemy that never spawns, a boss phase that deadlocks
    /// because the boss is deliberately still alive. This can.
    ///
    /// Enemies are weakened so fights resolve quickly: this is a test of mission
    /// flow, not of combat balance, and combat has its own harness.
    /// </summary>
    public class MissionPlaythroughDriver : MonoBehaviour
    {
        public System.Action<int> onFinished;

        /// <summary>Run only this level index, or -1 for all ten.</summary>
        public static int OnlyLevel = -1;

        private const float BudgetSeconds = 220f;  // in-game, per mission
        private const float EnemyHpCap = 14f;

        private int _level = -1;
        private float _budget, _healT;
        private int _errors, _failures;
        private readonly StringBuilder _report = new();
        private readonly HashSet<int> _stagesSeen = new();
        private int _lastStage = -1;
        private bool _loading;
        private float _listenHold, _traceT;
        private Vector3 _lastTracePos;

        private GameManager _gm;
        private Player.PlayerLocomotion _loco;
        private Health _hp;

        private void Start()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Application.logMessageReceived += OnLog;
            DontDestroyOnLoad(gameObject);
            Time.timeScale = 4f;   // the bot does not need to be watched
            NextMission();
        }

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type is not (LogType.Exception or LogType.Error)) return;
            _errors++;
            if (_errors <= 8) Debug.Log("[PLAY] error: " + msg);
        }

        private void NextMission()
        {
            if (_level >= 0 && (OnlyLevel < 0 || _level == OnlyLevel)) Finish();
            _level++;
            if (OnlyLevel >= 0 && _level != OnlyLevel && _level < Session.Story.Length)
            {
                // Skip without reporting: a filtered run is for one mission.
                _level = _level <= OnlyLevel ? OnlyLevel : Session.Story.Length;
            }
            if (_level >= Session.Story.Length)
            {
                Debug.Log("[PLAY] TABLE\n" + _report);
                Debug.Log(_failures == 0 && _errors == 0
                    ? "[PLAY] ALL PASSED" : $"[PLAY] {_failures} missions failed, {_errors} errors");
                Application.logMessageReceived -= OnLog;
                Time.timeScale = 1f;
                EmberInput.Scripted = null;
                onFinished?.Invoke(_failures == 0 && _errors == 0 ? 0 : 1);
                enabled = false;
                return;
            }

            // A checkpoint from an earlier run would skip straight past the stages
            // this test exists to walk through.
            for (var i = 1; i <= 12; i++) Checkpoints.Clear(i);

            _stagesSeen.Clear();
            _lastStage = -1;
            _budget = BudgetSeconds;
            _loading = true;
            var gm = SceneRefs.Game;
            if (gm == null) { Debug.LogError("[PLAY] no GameManager"); onFinished?.Invoke(2); return; }
            gm.LaunchStory(_level);
        }

        private void Finish()
        {
            var lv = Session.Story[_level];
            var dir = MissionDirector.Active;
            var plan = _gm != null ? _gm.CurrentPlan : null;
            var total = plan != null ? plan.stages.Length : 0;
            var done = dir != null && dir.Complete;
            var ok = done && _stagesSeen.Count >= total;
            if (!ok) _failures++;
            var line = $"{lv.id,2} {lv.name,-22} plan={(plan != null ? plan.name : "?"),-20} " +
                       $"stages={_stagesSeen.Count}/{total} complete={done} " +
                       $"stuckAt={(done ? -1 : _lastStage)} " +
                       $"optional={(dir?.Challenge != null && dir.Challenge.Earned() ? "earned" : "no")} " +
                       $"left={_budget:0}s";
            _report.Append(ok ? "pass  " : "FAIL  ").Append(line).Append('\n');
            Debug.Log("[PLAY] " + (ok ? "pass  " : "FAIL  ") + line);
        }

        private void Bind()
        {
            _gm = SceneRefs.Game;
            _loco = SceneRefs.Motor;
            _hp = _gm != null ? _gm.PlayerHealth : null;
            if (_hp != null) _hp.SetMax(100000f);
        }

        private void Update()
        {
            if (_level < 0 || _level >= Session.Story.Length) return;

            if (_loading)
            {
                var gm = SceneRefs.Game;
                if (gm == null || gm.State == GameManager.Phase.Menu) return;
                _loading = false;
                Bind();
                return;
            }
            if (_gm == null) { Bind(); return; }

            if (_gm.State == GameManager.Phase.Intro) { _gm.BeginMission(); return; }
            if (_hp != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.4f; _hp.Heal(100000f); }

            // Keep fights short. Real combat code still runs; there is just less
            // of each enemy to get through.
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead || e.maxHp <= EnemyHpCap) continue;
                e.maxHp = EnemyHpCap;
                e.SyncHpToMax();
            }

            var dir = MissionDirector.Active;
            if (dir != null && dir.StageIndex != _lastStage)
            {
                _lastStage = dir.StageIndex;
                _stagesSeen.Add(dir.StageIndex);
                Debug.Log($"[PLAY]   {Session.Story[_level].name} stage {dir.StageIndex} " +
                          $"{dir.Stage?.goal} — {dir.Objective}");
            }

            _budget -= Time.deltaTime;
            if ((_traceT -= Time.deltaTime) <= 0f)
            {
                _traceT = 10f;
                Trace(dir, "…");
            }
            if (_budget <= 0f && dir != null && !dir.Complete) Trace(dir, "STUCK");
            if (_budget <= 0f || dir == null || dir.Complete || dir.Failed
                || _gm.State is GameManager.Phase.Won or GameManager.Phase.Lost)
            {
                NextMission();
                return;
            }

            _driveThisFrame = dir;
        }

        /// <summary>
        /// Input is written in LateUpdate, after the HUD's touch handler has had
        /// its say. The HUD zeroes the virtual stick every frame when no finger
        /// is down, so a bot that writes during Update is silently overwritten
        /// whenever script order happens to put the HUD second.
        /// </summary>
        private void LateUpdate()
        {
            if (_driveThisFrame == null) return;
            DriveBot(_driveThisFrame);
            _driveThisFrame = null;
        }

        private MissionDirector _driveThisFrame;

        /// <summary>Where the bot is and what the stage is waiting for.</summary>
        private void Trace(MissionDirector dir, string tag)
        {
            var s = dir?.Stage;
            var p = _loco != null ? _loco.transform.position : Vector3.zero;
            var toPoint = s != null
                ? Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(s.point.x, 0f, s.point.z))
                : -1f;
            var markers = Object.FindObjectsByType<ObjectivePulse>(FindObjectsSortMode.None).Length;
            var clues = Object.FindObjectsByType<Clue>(FindObjectsSortMode.None).Length;
            var alive = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
                if (EnemyBrain.Active[i] != null && !EnemyBrain.Active[i].Dead) alive++;
            var clue = NearestClue();
            var marker = NearestNamed("ObjectiveMarker");
            var dClue = clue != null ? Vector3.Distance(p, clue.Value) : -1f;
            var dMark = marker != null ? Vector3.Distance(p, marker.Value) : -1f;
            var moved = Vector3.Distance(p, _lastTracePos);
            _lastTracePos = p;
            Debug.Log($"[PLAY]   {tag} {Session.Story[_level].name} stage {dir?.StageIndex} " +
                      $"{s?.goal} player=({p.x:0.0},{p.z:0.0}) point=({s?.point.x:0.0},{s?.point.z:0.0}) " +
                      $"dist={toPoint:0.0} moved={moved:0.0} dClue={dClue:0.0} dMarker={dMark:0.0} " +
                      $"markers={markers} clues={clues} alive={alive} " +
                      $"phase={_gm.State} escort={(EscortNpc.Active != null)} " +
                      $"cine={GameManager.CinematicActive} frozen={Player.CombatController.TimeFrozen} " +
                      $"ts={Time.timeScale:0.00} move={EmberInput.Scripted} " +
                      $"busy={_loco.Busy} cam={(SceneRefs.Cam != null ? SceneRefs.Cam.name : "null")} " +
                      $"left={_budget:0}s");
        }

        // ------------------------------------------------------------- the bot

        private void DriveBot(MissionDirector dir)
        {
            if (_loco == null) return;
            EmberInput.Scripted = Vector2.zero;
            if (GameManager.CinematicActive) return;

            var stage = dir.Stage;
            var goal = stage?.goal ?? StageGoal.Wave;

            switch (goal)
            {
                case StageGoal.Reach:
                case StageGoal.Escape:
                case StageGoal.ReachAny:
                case StageGoal.Defend:
                    if (!WalkTo(NearestNamed("ObjectiveMarker"))) Fight();
                    return;

                case StageGoal.Investigate:
                    if (!WalkTo(NearestClue())) Fight();
                    return;

                case StageGoal.Listen:
                    // Standing still is the mechanic, but it is how you *find*
                    // them, not how you finish them. Hold, then hunt.
                    _listenHold += Time.deltaTime;
                    if (_listenHold < 1.5f) return;
                    Fight();
                    if (_listenHold > 6f) _listenHold = 0f;
                    return;

                case StageGoal.Escort:
                    if (NearestEnemy(out var de) != null && de < 9f) { Fight(); return; }
                    var npc = EscortNpc.Active;
                    if (npc != null) WalkTo(npc.transform.position, 3f);
                    return;

                default:
                    Fight();
                    return;
            }
        }

        private void Fight()
        {
            var target = NearestEnemy(out var dist);
            if (target == null) return;
            var to = target.transform.position - _loco.transform.position;
            to.y = 0f;
            var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
            _loco.SetFacing(dir);
            if (dist > 1.9f) WalkTo(target.transform.position, 1.7f);
            else EmberInput.PressStrike();
        }

        /// <summary>
        /// Walk toward a point, going round the arena's furniture on the way.
        /// A bot that only knows how to walk in a straight line parks itself
        /// against the first chimney between it and the objective and reports a
        /// perfectly good mission as broken.
        /// </summary>
        private bool WalkTo(Vector3? target, float stopAt = 1.2f)
        {
            if (target == null) return false;
            var from = _loco.transform.position;
            var to = target.Value - from;
            to.y = 0f;
            if (to.magnitude <= stopAt) return true;
            var dir = to.normalized;

            // Steer around whatever is in the way: try progressively wider
            // angles either side until one is clear.
            if (ArenaMarkers.ObstacleAhead(from, dir, 3f))
                foreach (var a in Detours)
                {
                    var alt = Quaternion.Euler(0f, a, 0f) * dir;
                    if (ArenaMarkers.ObstacleAhead(from, alt, 3f)) continue;
                    dir = alt;
                    break;
                }

            // Last resort: if we have made no headway at all for a while, commit
            // to sliding sideways for a moment to break the deadlock.
            if (Vector3.Distance(from, _stallPos) < 0.25f)
            {
                _stallT += Time.deltaTime;
                if (_stallT > 4f)
                {
                    // That side did not work either; try the other one next.
                    _stallT = 0f;
                    _stallSide = -_stallSide;
                    _stallPos = from;
                }
                else if (_stallT > 1f)
                {
                    // Commit to one side for the whole window. Re-deciding every
                    // frame just vibrates on the spot and never gets anywhere.
                    dir = Quaternion.Euler(0f, 75f * _stallSide, 0f) * dir;
                }
            }
            else { _stallT = 0f; _stallPos = from; }

            _loco.SetFacing(dir);
            EmberInput.Scripted = Cam(dir);
            return true;
        }

        private static readonly float[] Detours = { 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f };
        private Vector3 _stallPos;
        private float _stallT;
        private float _stallSide = 1f;

        private EnemyBrain NearestEnemy(out float dist)
        {
            EnemyBrain best = null;
            var bd = float.MaxValue;
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

        private Vector3? NearestNamed(string name)
        {
            Vector3? best = null;
            var bd = float.MaxValue;
            foreach (var t in Object.FindObjectsByType<ObjectivePulse>(FindObjectsSortMode.None))
            {
                if (t == null || !t.name.StartsWith(name)) continue;
                var d = (t.transform.position - _loco.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = t.transform.position; }
            }
            return best;
        }

        private Vector3? NearestClue()
        {
            Vector3? best = null;
            var bd = float.MaxValue;
            foreach (var c in Object.FindObjectsByType<Clue>(FindObjectsSortMode.None))
            {
                if (c == null) continue;
                var d = (c.transform.position - _loco.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = c.transform.position; }
            }
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
