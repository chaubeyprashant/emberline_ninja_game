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
                    StageGoal.BossPhase => _phaseBoss != null && _phaseBoss.maxHp > 0f
                        ? $"{text} — {Mathf.RoundToInt(_phaseBoss.Hp / _phaseBoss.maxHp * 100f)}%"
                        : text,
                    _ => text,
                };
            }
        }

        private float _stageT;
        private int _progress;

        /// <summary>The optional condition riding on this whole mission.</summary>
        public ChallengeTracker Challenge { get; private set; }

        /// <summary>ReachAny: true once the player commits to the second route.</summary>
        private bool _tookRouteB;
        private Vector3 _point, _pointB;
        private Transform _markerB;
        private EnemyBrain _phaseBoss;
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
            Villager.ResetCount();
            Prisoner.ResetCount();
            Visibility.ResetConditions();
            ApplyPlanConditions(plan);
            d.Challenge = new ChallengeTracker(plan, gm);
            MissionDressing.Build(plan, gm != null ? gm.arenaHalfExtents : new Vector2(13f, 8f));
            // Resume from the last checkpoint rather than the top of the mission.
            d.StageIndex = Checkpoints.Load(plan.id);
            d.EnterStage();
            return d;
        }

        /// <summary>
        /// The plan's environment flags. Both existed on MissionPlan and neither
        /// was read by anything, so a mission authored as a night mission played
        /// in daylight.
        /// </summary>
        private static void ApplyPlanConditions(MissionPlan plan)
        {
            if (plan == null) return;
            if (plan.rain) LevelFx.EnableRain();
            if (!plan.nightOverride) return;

            Visibility.AmbientScale *= 0.6f;
            RenderSettings.ambientSkyColor *= 0.45f;
            RenderSettings.ambientEquatorColor *= 0.45f;
            RenderSettings.ambientGroundColor *= 0.5f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.intensity *= 0.45f;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            MissionDressing.Clear();
        }

        // ------------------------------------------------------------- stages

        private void EnterStage()
        {
            var s = Stage;
            if (s == null) { Finish(); return; }

            _stageT = s.duration;
            _progress = 0;
            ClearMarker();

            // Authored points are laid out on a plan of the arena, not against
            // its geometry: one that lands inside a chimney is an objective the
            // player can never stand on, and the mission simply stops. Push both
            // points out of anything solid before anyone is asked to walk there.
            _point = Walkable(s.point);
            _pointB = Walkable(s.pointB);

            if (!string.IsNullOrEmpty(s.banner)) _gm?.Announce(s.banner);
            if (s.checkpoint) Checkpoints.Save(Plan.id, StageIndex);

            // Goal-specific setup. Everything here is generic — no mission names.
            switch (s.goal)
            {
                case StageGoal.Reach:
                case StageGoal.Escape:
                case StageGoal.Defend:
                    SpawnMarker(_point, s.goal == StageGoal.Defend
                        ? new Color(1f, 0.62f, 0.35f) : new Color(0.5f, 0.9f, 1f));
                    break;

                case StageGoal.ReachAny:
                    // Both ways in are marked. Whichever you walk to is the one
                    // you took; the other is remembered for later.
                    SpawnMarker(_point, new Color(0.5f, 0.9f, 1f));
                    _markerB = SpawnMarkerObject(_pointB, new Color(0.95f, 0.72f, 0.45f));
                    break;

                case StageGoal.Investigate:
                    SpawnClues(s.count);
                    break;

                case StageGoal.Stealth:
                case StageGoal.Listen:
                    foreach (var e in EnemyBrain.Active)
                        if (e != null && !e.Dead) e.SetUnaware(true);
                    break;

                case StageGoal.Escort:
                {
                    // Spawn the bearer here rather than at mission start. He walks
                    // on his own Update, so a bearer created during the briefing
                    // had already finished his journey before the stage that asks
                    // for him ever began.
                    if (EscortNpc.Active != null) break;
                    var half = _gm != null ? _gm.arenaHalfExtents : new Vector2(13f, 8f);
                    EscortNpc.Spawn(new Vector3(-half.x + 1.5f, 0f, -half.y + 2f),
                        new Vector3(half.x - 1.5f, 0f, half.y - 2f),
                        Mathf.Max(20f, s.duration > 0f ? s.duration : 62f), 130f);
                    break;
                }
            }

            SpawnStageEnemies(s);
        }

        private void SpawnStageEnemies(MissionStage s)
        {
            if (_gm == null) return;
            // A split-route stage spawns the guards of the route you actually
            // took, so the fight you get is a consequence of the choice you made.
            var list = s.goal == StageGoal.ReachAny
                ? null
                : _tookRouteB && s.spawnB is { Length: > 0 } ? s.spawnB : s.spawn;
            if (list == null || list.Length == 0) return;
            var unaware = s.goal is StageGoal.Stealth or StageGoal.Listen;
            foreach (var kind in list) _gm.SpawnOne(kind, unaware);
        }

        private void Update()
        {
            if (Complete || Failed || Plan == null) return;
            if (GameManager.CinematicActive) return;
            var s = Stage;
            if (s == null) { Finish(); return; }

            if (s.duration > 0f) _stageT -= Time.deltaTime;
            Challenge?.Tick(Time.deltaTime);
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
                    return _player != null && Flat(_player.position, _point) < 2.4f;

                case StageGoal.ReachAny:
                {
                    if (_player == null) return false;
                    if (Flat(_player.position, _point) < 2.4f) { _tookRouteB = false; return true; }
                    if (Flat(_player.position, _pointB) < 2.4f) { _tookRouteB = true; return true; }
                    return false;
                }

                case StageGoal.BossPhase:
                {
                    // The boss does not die here. The mission moves when he is
                    // hurt enough, and whatever the plan does next happens with
                    // him still standing in the middle of it.
                    if (_phaseBoss == null) _phaseBoss = FindBoss();
                    if (_phaseBoss == null || _phaseBoss.Dead) return true;
                    return _phaseBoss.maxHp > 0f
                           && _phaseBoss.Hp <= _phaseBoss.maxHp * s.bossHealthGate;
                }

                case StageGoal.Listen:
                    // Hold still and the marsh gives them away. Clearing what is
                    // circling you is still the actual goal.
                    ListenPulse();
                    return AliveEnemies() == 0;

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
                    // A wave fought *during* a boss fight resolves when the adds
                    // are down. Counting the boss too would deadlock the mission:
                    // he is deliberately still alive between his phases.
                    return s.goal == StageGoal.BossFight
                        ? AliveEnemies() == 0
                        : AliveExcludingPhaseBoss() == 0;
            }
        }

        private void CompleteStage()
        {
            var s = Stage;
            if (s != null)
            {
                // Checked before the stage's own event fires: a plan that raises
                // the alarm *as* the target dies must not fail the silent kill.
                if (Challenge is { Kind: MissionChallenge.SilentKill }
                    && s.goal is StageGoal.Stealth or StageGoal.Assassinate
                    && _gm != null && _gm.AlarmRaised)
                    Challenge.SilentKillFailed = true;
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
                case StageEvent.FogRolls:
                    _gm?.Announce("THE FOG COMES IN");
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, 0.075f);
                    Visibility.AmbientScale *= 0.45f;
                    break;
                case StageEvent.Ambush:
                    // Behind you, and already awake. The difference between this
                    // and reinforcements is where they start and what they know.
                    _gm?.Announce("BEHIND YOU");
                    SpawnBehindPlayer(EnemyKind.Assassin);
                    SpawnBehindPlayer(EnemyKind.Bandit);
                    break;
                case StageEvent.RouteWakes:
                    _gm?.Announce("THE OTHER GATE IS AWAKE");
                    _tookRouteB = !_tookRouteB;
                    break;
            }
        }

        private void Finish()
        {
            Complete = true;
            if (Challenge != null && Challenge.Earned())
            {
                BonusShards += Challenge.Reward;
                OptionalDone++;
                _gm?.Announce($"OPTIONAL COMPLETE — ◆ +{Challenge.Reward}");
            }
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

        /// <summary>Living enemies that are not the boss we are phasing.</summary>
        private int AliveExcludingPhaseBoss()
        {
            var n = 0;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead) continue;
                if (_phaseBoss != null && e == _phaseBoss) continue;
                n++;
            }
            return n;
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

        private EnemyBrain FindBoss()
        {
            foreach (var e in EnemyBrain.Active)
                if (e != null && !e.Dead && e.IsBossTarget) return e;
            return null;
        }

        private void SpawnBehindPlayer(EnemyKind kind)
        {
            if (_gm == null || _player == null) return;
            _gm.SpawnOne(kind, false);
        }

        private float _listenT;

        /// <summary>
        /// Standing still in the fog is how you find them. Hold for a beat and
        /// anything close enough to hear is briefly outlined — the mission's whole
        /// identity in one mechanic, and useless the moment you start running.
        /// </summary>
        private void ListenPulse()
        {
            var motor = SceneRefs.Motor;
            if (motor == null) return;
            var still = Core.EmberInput.Move.sqrMagnitude < 0.04f;
            if (!still) { _listenT = 0f; return; }
            _listenT += Time.deltaTime;
            if (_listenT < 0.7f) return;
            _listenT = 0f;

            var heard = 0;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead) continue;
                var d = Vector3.Distance(e.transform.position, motor.transform.position);
                if (d > 14f) continue;
                UI.FxPools.Sparks(e.transform.position + Vector3.up * 1.6f,
                    new Color(0.62f, 0.78f, 0.95f), 6);
                heard++;
            }
            if (heard > 0) Sfx3D.Ui();
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

        /// <summary>Same marker, handed back instead of stored. For split routes.</summary>
        private Transform SpawnMarkerObject(Vector3 at, Color tint)
        {
            var keep = _marker;
            SpawnMarker(at, tint);
            var made = _marker;
            _marker = keep;
            return made;
        }

        private void ClearMarker()
        {
            if (_marker != null) Destroy(_marker.gameObject);
            _marker = null;
            if (_markerB != null) Destroy(_markerB.gameObject);
            _markerB = null;
        }

        /// <summary>
        /// Nudge a point out of the arena's obstacles and back inside its bounds,
        /// so nothing the mission asks the player to walk to is inside a wall.
        /// </summary>
        private Vector3 Walkable(Vector3 p)
        {
            var half = _gm != null ? _gm.arenaHalfExtents : new Vector2(13f, 8f);
            p.x = Mathf.Clamp(p.x, -half.x + 1.5f, half.x - 1.5f);
            p.z = Mathf.Clamp(p.z, -half.y + 1.5f, half.y - 1.5f);
            return ArenaMarkers.Resolve(new Vector3(p.x, 0f, p.z), 1.3f);
        }

        private void SpawnClues(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)Mathf.Max(1, count) * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(angle) * Random.Range(5f, 11f), 0.4f,
                    Mathf.Sin(angle) * Random.Range(3f, 6.5f));
                var flat = Walkable(pos);
                pos = new Vector3(flat.x, 0.4f, flat.z);
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
