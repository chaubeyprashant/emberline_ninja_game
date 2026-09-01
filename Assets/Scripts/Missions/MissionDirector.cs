using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.UI;

namespace Emberline.Missions
{
    /// <summary>
    /// Runs a MissionPlan's stages in order. This is the reusable system the
    /// overhaul asked for: every mission type is a sequence of goals the director
    /// already knows how to evaluate, so "Investigation" and "Escape" cost an
    /// asset each rather than another branch in GameManager.
    ///
    /// The director owns stage flow, scripted events, optional objectives and
    /// checkpoints. It does not own combat — enemies, waves and the fight itself
    /// still belong to GameManager and EnemyBrain.
    /// </summary>
    public class MissionDirector : MonoBehaviour
    {
        public static MissionDirector Active { get; private set; }

        public MissionPlan Plan { get; private set; }
        public int StageIndex { get; private set; }
        public bool Complete { get; private set; }
        public bool Failed { get; private set; }

        /// <summary>Shards banked from optional objectives this run.</summary>
        public int BonusShards { get; private set; }

        /// <summary>Optional stages completed this run, for the results screen.</summary>
        public int OptionalDone { get; private set; }

        public MissionStage Stage =>
            Plan != null && StageIndex >= 0 && StageIndex < Plan.stages.Length
                ? Plan.stages[StageIndex] : null;

        /// <summary>Live objective line for the HUD.</summary>
        public string Objective
        {
            get
            {
                var s = Stage;
                if (Complete) return "MISSION COMPLETE";
                if (s == null) return "";
                var text = string.IsNullOrEmpty(s.objective) ? s.goal.ToString().ToUpperInvariant()
                    : s.objective;
                return s.goal switch
                {
                    StageGoal.Survive or StageGoal.Defend or StageGoal.Escape =>
                        $"{text} — {Mathf.CeilToInt(Mathf.Max(0f, _stageT))}s",
                    StageGoal.Investigate => $"{text} — {_progress}/{s.count}",
                    StageGoal.Eliminate or StageGoal.Assassinate =>
                        $"{text} — {Mathf.Max(0, s.count - _progress)} LEFT",
                    _ => text,
                };
            }
        }

        private float _stageT;
        private int _progress;
        private GameManager _gm;
        private Transform _player;
        private Transform _marker;

        // ---------------------------------------------------------- lifecycle

        public static MissionDirector Begin(MissionPlan plan, GameManager gm)
        {
            var go = new GameObject("MissionDirector");
            var d = go.AddComponent<MissionDirector>();
            d.Plan = plan;
            d._gm = gm;
            d._player = SceneRefs.Motor != null ? SceneRefs.Motor.transform : null;
            Active = d;
            // Resume from the last checkpoint rather than the top of the mission.
            d.StageIndex = Checkpoints.Load(plan.id);
            d.EnterStage();
            return d;
        }

        private void OnDestroy() { if (Active == this) Active = null; }

        // ------------------------------------------------------------- stages

        private void EnterStage()
        {
            var s = Stage;
            if (s == null) { Finish(); return; }

            _stageT = s.duration;
            _progress = 0;
            ClearMarker();

            if (!string.IsNullOrEmpty(s.banner)) _gm?.Announce(s.banner);
            if (s.checkpoint) Checkpoints.Save(Plan.id, StageIndex);

            // Goal-specific setup. Everything here is generic — no mission names.
            switch (s.goal)
            {
                case StageGoal.Reach:
                case StageGoal.Escape:
                case StageGoal.Defend:
                    SpawnMarker(s.point, s.goal == StageGoal.Defend
                        ? new Color(1f, 0.62f, 0.35f) : new Color(0.5f, 0.9f, 1f));
                    break;

                case StageGoal.Investigate:
                    SpawnClues(s.count);
                    break;

                case StageGoal.Stealth:
                    foreach (var e in EnemyBrain.Active)
                        if (e != null && !e.Dead) e.SetUnaware(true);
                    break;
            }

            SpawnStageEnemies(s);
        }

        private void SpawnStageEnemies(MissionStage s)
        {
            if (s.spawn == null || s.spawn.Length == 0 || _gm == null) return;
            foreach (var kind in s.spawn) _gm.SpawnOne(kind, s.goal == StageGoal.Stealth);
        }

        private void Update()
        {
            if (Complete || Failed || Plan == null) return;
            if (GameManager.CinematicActive) return;
            var s = Stage;
            if (s == null) { Finish(); return; }

            if (s.duration > 0f) _stageT -= Time.deltaTime;
            if (Evaluate(s)) CompleteStage();
        }

        /// <summary>
        /// One evaluator per goal — the heart of the reusable system. A new goal is
        /// a case here plus a case in EnterStage; a new *mission* is neither.
        /// </summary>
        private bool Evaluate(MissionStage s)
        {
            switch (s.goal)
            {
                case StageGoal.Reach:
                case StageGoal.Escape:
                    if (s.goal == StageGoal.Escape && _stageT <= 0f) { Fail("YOU DID NOT MAKE IT"); return false; }
                    return _player != null && Flat(_player.position, s.point) < 2.4f;

                case StageGoal.Survive:
                    return _stageT <= 0f;

                case StageGoal.Defend:
                    // Hold the point: leaving it for too long is on you, but the
                    // fail state is the clock, not a leash.
                    return _stageT <= 0f;

                case StageGoal.Investigate:
                    return _progress >= s.count;

                case StageGoal.Stealth:
                    if (_gm != null && _gm.AlarmRaised) { /* not a fail — costs rank */ }
                    return AliveEnemies() == 0;

                case StageGoal.Escort:
                {
                    var npc = EscortNpc.Active;
                    if (npc == null) return false;
                    if (npc.Health.Dead) { Fail("THE FLAME IS OUT"); return false; }
                    return npc.Progress01 >= 1f;
                }

                case StageGoal.Chase:
                    // The runner escapes if it survives the clock.
                    if (_stageT <= 0f) { Fail("THEY GOT AWAY"); return false; }
                    return AliveEnemies() == 0;

                default: // Wave, Eliminate, Assassinate, Duel, BossFight
                    return AliveEnemies() == 0;
            }
        }

        private void CompleteStage()
        {
            var s = Stage;
            if (s != null)
            {
                if (s.optional)
                {
                    OptionalDone++;
                    BonusShards += s.bonusShards;
                    _gm?.Announce($"OPTIONAL COMPLETE — ◆ +{s.bonusShards}");
                }
                FireEvent(s.onComplete);
            }
            ClearMarker();
            StageIndex++;
            if (StageIndex >= Plan.stages.Length) { Finish(); return; }
            EnterStage();
        }

        /// <summary>The scripted turn. Generic effects, authored per stage.</summary>
        private void FireEvent(StageEvent e)
        {
            switch (e)
            {
                case StageEvent.AlarmTriggered:
                    _gm?.RaiseAlarm(null);
                    break;
                case StageEvent.Reinforcements:
                    _gm?.Announce("MORE ON THE ROAD");
                    for (var i = 0; i < 3; i++) _gm?.SpawnOne(EnemyKind.Bandit, false);
                    break;
                case StageEvent.BossArrives:
                    _gm?.Announce("SOMETHING HEAVIER IS COMING");
                    break;
                case StageEvent.LightsOut:
                    _gm?.Announce("THE LANTERNS GO OUT");
                    foreach (var post in LanternPost.Active)
                        if (post != null && post.glow != null) post.glow.enabled = false;
                    RenderSettings.fogDensity = 0.05f;
                    break;
                case StageEvent.RainStarts:
                    _gm?.Announce("RAIN");
                    LevelFx.EnableRain();
                    break;
                case StageEvent.WaterRises:
                    _gm?.Announce("THE WATER RISES");
                    ArenaMarkers.RaiseWater(1.8f);
                    break;
                case StageEvent.TargetFlees:
                    _gm?.Announce("THEY RUN");
                    break;
            }
        }

        private void Finish()
        {
            Complete = true;
            Checkpoints.Clear(Plan.id);
        }

        private void Fail(string reason)
        {
            Failed = true;
            _gm?.Announce(reason);
        }

        // ------------------------------------------------------------ helpers

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static int AliveEnemies()
        {
            var n = 0;
            foreach (var e in EnemyBrain.Active) if (e != null && !e.Dead) n++;
            return n;
        }

        /// <summary>Called by a clue pickup when the player walks over it.</summary>
        public void OnClueFound()
        {
            _progress++;
            Sfx3D.Ui();
            _gm?.Announce($"CLUE FOUND — {_progress}/{Stage?.count ?? 0}");
        }

        private void SpawnMarker(Vector3 at, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = "ObjectiveMarker";
            go.transform.position = new Vector3(at.x, 0.06f, at.z);
            go.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            var mat = new Material(Shader.Find("Emberline/Glow")) { color = tint };
            var r = go.GetComponent<Renderer>();
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<ObjectivePulse>();
            _marker = go.transform;
        }

        private void ClearMarker()
        {
            if (_marker != null) Destroy(_marker.gameObject);
            _marker = null;
        }

        private void SpawnClues(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)Mathf.Max(1, count) * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(angle) * Random.Range(5f, 11f), 0.4f,
                    Mathf.Sin(angle) * Random.Range(3f, 6.5f));
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.name = "Clue";
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * 0.45f;
                var r = go.GetComponent<Renderer>();
                r.material = new Material(Shader.Find("Emberline/Glow"))
                    { color = new Color(0.7f, 0.9f, 1f, 0.9f) };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.AddComponent<Clue>();
            }
        }
    }

    /// <summary>Slow pulse so an objective marker reads at a glance.</summary>
    public class ObjectivePulse : MonoBehaviour
    {
        private void Update()
        {
            var s = 2.4f + 0.25f * Mathf.Sin(Time.time * 2.2f);
            transform.localScale = new Vector3(s, 0.02f, s);
        }
    }

    /// <summary>A findable clue for Investigate stages. Walk over it.</summary>
    public class Clue : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0f, 55f * Time.deltaTime, 0f);
            var motor = SceneRefs.Motor;
            if (motor == null) return;
            var d = motor.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 1.5f * 1.5f) return;
            FxPools.Sparks(transform.position, new Color(0.7f, 0.9f, 1f), 10);
            MissionDirector.Active?.OnClueFound();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Stage-level checkpoints in PlayerPrefs. A retry resumes at the last
    /// checkpointed beat instead of the top of a seven-stage mission.
    /// </summary>
    public static class Checkpoints
    {
        private static string Key(int missionId) => $"ckpt_{missionId}";

        public static int Load(int missionId) =>
            Mathf.Max(0, PlayerPrefs.GetInt(Key(missionId), 0));

        public static void Save(int missionId, int stageIndex)
        {
            PlayerPrefs.SetInt(Key(missionId), stageIndex);
            PlayerPrefs.Save();
        }

        public static void Clear(int missionId)
        {
            PlayerPrefs.DeleteKey(Key(missionId));
            PlayerPrefs.Save();
        }

        public static bool Has(int missionId) => PlayerPrefs.HasKey(Key(missionId));
    }
}
