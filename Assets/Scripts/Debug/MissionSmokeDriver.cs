#if UNITY_EDITOR
using Emberline.Core;
using Emberline.Enemies;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Plays a real story mission end to end with the scripted player, so the new
    /// AI branches are exercised through the actual wave spawner rather than only
    /// through the encounter harness's direct spawns. Watches for exceptions and
    /// for a fight that goes quiet, which is what a broken behaviour branch looks
    /// like from the outside. Editor-only.
    /// </summary>
    public class MissionSmokeDriver : MonoBehaviour
    {
        public float seconds = 70f;
        public System.Action<int> onFinished;

        private GameManager _gm;
        private Player.PlayerLocomotion _loco;
        private Player.CombatController _combat;
        private float _t, _cycleT, _healT;
        private int _step, _errors, _lastWave = -99;
        private bool _heavyStarted, _whiffStarted, _rebinding;

        private void Start()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            Application.logMessageReceived += OnLog;
            _gm = SceneRefs.Game;
            _loco = SceneRefs.Motor;
            _combat = _loco != null ? _loco.GetComponent<Player.CombatController>() : null;
            if (_gm == null || _loco == null) { Debug.LogError("[MIS] scene not wired"); Done(2); return; }
            _t = seconds;
            // The GameManager reads the launch mode in its own Start, which has
            // already run by the time an editor entry point can attach anything.
            // Launching the mission properly reloads the scene, so survive it.
            DontDestroyOnLoad(gameObject);
            if (_gm.State == GameManager.Phase.Menu)
            {
                _rebinding = true;
                _gm.LaunchDuel(0);
                return;
            }
            Bind();
        }

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type is LogType.Exception or LogType.Error)
            {
                _errors++;
                if (_errors <= 5) Debug.Log("[MIS] error: " + msg);
            }
        }

        private void Done(int code)
        {
            Application.logMessageReceived -= OnLog;
            enabled = false;
            onFinished?.Invoke(code);
        }

        private void Bind()
        {
            _gm = SceneRefs.Game;
            _loco = SceneRefs.Motor;
            _combat = _loco != null ? _loco.GetComponent<Player.CombatController>() : null;
            var hp = _gm != null ? _gm.PlayerHealth : null;
            if (hp != null) hp.SetMax(100000f);
            _t = seconds;
            AiTelemetry.Reset();
            Debug.Log($"[MIS] fight='{(_gm != null && _gm.CurrentDuel != null ? _gm.CurrentDuel.name : "?")}' " +
                      $"phase={(_gm != null ? _gm.State.ToString() : "-")}");
        }

        private void Update()
        {
            if (_rebinding)
            {
                var g = SceneRefs.Game;
                if (g == null || g.State == GameManager.Phase.Menu) return;
                _rebinding = false;
                Bind();
                return;
            }
            if (_gm == null) return;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            var hp = _gm.PlayerHealth;
            if (hp != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.5f; hp.Heal(100000f); }

            // The HUD normally starts the mission; do it here so the run is headless.
            if (_gm.State == GameManager.Phase.Intro) { _gm.BeginMission(); return; }

            if (_gm.WaveIndex != _lastWave)
            {
                _lastWave = _gm.WaveIndex;
                Debug.Log($"[MIS] wave {_gm.WaveIndex} alive={AliveCount()} kills={_gm.Kills} " +
                          $"attacks={AiTelemetry.Attacks} maxSimul={AiTelemetry.MaxSimultaneousAttackers}");
            }

            if ((_t -= Time.deltaTime) <= 0f || _gm.State is GameManager.Phase.Won or GameManager.Phase.Lost)
            {
                Debug.Log($"[MIS] end phase={_gm.State} wave={_gm.WaveIndex} kills={_gm.Kills} " +
                          $"attacks={AiTelemetry.Attacks} maxSimul={AiTelemetry.MaxSimultaneousAttackers} " +
                          $"punish={AiTelemetry.OutOfTurnPunishes} blocks={AiTelemetry.ReactiveBlocks} " +
                          $"dodges={AiTelemetry.Dodges} ripostes={AiTelemetry.Ripostes} " +
                          $"retreats={AiTelemetry.Retreats} guards={AiTelemetry.GuardHolds} " +
                          $"protects={AiTelemetry.ProtectMoves} shortStaggers={AiTelemetry.StaggersShortened} " +
                          $"errors={_errors}");
                var ok = _errors == 0 && AiTelemetry.Attacks > 0
                         && AiTelemetry.MaxSimultaneousAttackers <= 3 && _gm.Kills > 0;
                Debug.Log(ok ? "[MIS] PASSED" : "[MIS] FAILED");
                Done(ok ? 0 : 1);
                return;
            }

            DrivePlayer();
        }

        private static int AliveCount()
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
                var d = (b.transform.position - _loco.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        private void DrivePlayer()
        {
            var target = Nearest();
            EmberInput.TouchActive = true;
            EmberInput.TouchMove = Vector2.zero;
            if (target == null) return;
            var to = target.transform.position - _loco.transform.position;
            to.y = 0f;
            var dist = to.magnitude;
            var dir = dist > 0.01f ? to / dist : Vector3.forward;
            _cycleT += Time.deltaTime;

            switch (_step)
            {
                case 0:
                    if (dist > 2.1f) EmberInput.TouchMove = Cam(dir); else Advance();
                    if (_cycleT > 5f) Advance();
                    break;
                case 1:
                    _loco.SetFacing(dir);
                    if (_cycleT < 0.02f || _cycleT > 0.45f && _cycleT < 0.5f) EmberInput.PressStrike();
                    if (_cycleT > 0.9f) Advance();
                    break;
                case 2:
                    _loco.SetFacing(dir);
                    if (!_heavyStarted)
                    {
                        EmberInput.PressCleave();
                        if (_combat != null && _combat.HeavyWindingUp) _heavyStarted = true;
                        if (_cycleT > 2.2f) Advance();
                    }
                    else if (_combat == null || !_combat.Committed) Advance();
                    break;
                case 3:
                    _loco.SetFacing(-dir);
                    if (!_whiffStarted)
                    {
                        EmberInput.PressStrike();
                        if (_combat != null && _combat.Whiffed) _whiffStarted = true;
                        if (_cycleT > 1.2f) Advance();
                    }
                    else if (_cycleT > 0.9f) Advance();
                    break;
                case 4:
                    if (_cycleT < 0.02f) { EmberInput.TouchMove = Cam(-dir); EmberInput.PressFlicker(); }
                    if (_cycleT > 0.6f) Advance();
                    break;
                default:
                    _loco.SetFacing(dir);
                    if (_cycleT > 0.5f) { _step = 0; _cycleT = 0f; }
                    break;
            }
        }

        private void Advance() { _step++; _cycleT = 0f; _heavyStarted = _whiffStarted = false; }

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
