using UnityEngine;
using UnityEngine.SceneManagement;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using Emberline.UI;

namespace Emberline
{
    /// <summary>
    /// v2 flow: MAIN MENU → Story levels (named, authored, starred, unlocking) /
    /// Duels (named 1v1 opponents) / Endless Trial. Scenes are theme shells;
    /// Session decides what spawns. Fields are public for the batch bootstrap.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public MissionDef mission;               // legacy, unused in v2
        public GameObject[] enemyPrefabs;        // indexed by (int)EnemyKind
        public Vector2 arenaHalfExtents = new(13f, 8f);
        public string otherSceneName = "";       // legacy
        public string otherMissionLabel = "";    // legacy
        public bool isMarshScene;

        public enum Phase { Menu, Intro, Playing, Won, Lost }

        public Phase State { get; private set; } = Phase.Menu;
        public LaunchMode ModeNow => Session.Mode;
        public LevelDef CurrentLevel { get; private set; }
        public DuelDef CurrentDuel { get; private set; }

        public int WaveIndex { get; private set; } = -1;
        public float MissionTime { get; private set; }
        public float DamageTaken { get; private set; }
        public int GatesCrackedTotal { get; private set; }
        public string Banner { get; private set; } = "";
        public float BannerTimer { get; private set; }
        public int StarsEarned { get; private set; }
        public int ShardsEarned { get; private set; }

        /// <summary>Enemies killed this mission. Reset when a mission configures.</summary>
        public int Kills { get; private set; }

        /// <summary>Shards banked from optional objectives, for the results screen.</summary>
        public int BonusShardsEarned => _director?.BonusShards ?? 0;

        /// <summary>Optional stages in the plan and how many were completed.</summary>
        public (int done, int total) OptionalObjectives
        {
            get
            {
                if (CurrentPlan == null) return (0, 0);
                var total = 0;
                foreach (var st in CurrentPlan.stages) if (st.optional) total++;
                return (_director?.OptionalDone ?? 0, total);
            }
        }
        public int DailyShards { get; private set; }

        /// <summary>Shards paid by the weekly challenge on this result, or 0.</summary>
        public int WeeklyShards { get; private set; }
        public string FeatsLine { get; private set; } = "";
        public bool NewRecord { get; private set; }
        public int BestWave => PlayerPrefs.GetInt("best_wave", 0);
        public int BestNorth => PlayerPrefs.GetInt("best_north", 0);
        public int WaveCount => _waves?.Length ?? 0;

        /// <summary>Meters marched up the Road North (Endless mode).</summary>
        public int DistanceNorth => _road != null && _playerT != null
            ? Mathf.Max(0, Mathf.RoundToInt(_playerT.position.z - _road.StartZ)) : 0;

        /// <summary>True while a boss intro (or future cutscene) freezes gameplay.</summary>
        public static bool CinematicActive { get; private set; }

        public void SetCinematic(bool on) => CinematicActive = on;

        private EnemyKind[][] _waves;
        private float _interWave = 1.2f;
        private bool _waveActive;
        private bool _endless;
        private bool _bossIntroShown;
        private RoadNorth _road;
        private Endless.EndlessDirector _run;
        private SenGates _gates;
        private Health _playerHealth;
        private Player.CombatController _combat;
        private CameraRig _rig;
        private Transform _playerT;
        private CharacterRig _playerRig;

        private bool HoldMode => ModeNow == LaunchMode.Story && CurrentLevel != null
                                 && CurrentLevel.holdSeconds > 0f;

        /// <summary>Mission rule in play; Clear for everything that isn't a story level.</summary>
        public MissionObjective MissionKind => ModeNow == LaunchMode.Story && CurrentLevel != null
            ? CurrentLevel.objective : MissionObjective.Clear;

        private bool StealthMode => MissionKind == MissionObjective.Stealth;
        private bool EscortMode => MissionKind == MissionObjective.Escort;

        /// <summary>True once a stealth level's alarm has gone off. Costs rank.</summary>
        public bool AlarmRaised { get; private set; }

        /// <summary>Highest detection across unaware enemies, 0..1. Drives the HUD meter.</summary>
        public float Detection01
        {
            get
            {
                if (!StealthMode || AlarmRaised) return 0f;
                var worst = 0f;
                foreach (var e in EnemyBrain.Active)
                    if (e != null && !e.Dead && e.Unaware && e.Detection > worst) worst = e.Detection;
                return worst;
            }
        }

        /// <summary>Active multi-stage mission plan, when one is running.</summary>
        public Missions.MissionPlan CurrentPlan { get; private set; }

        private Missions.MissionDirector _director;

        /// <summary>Stage progress "2/4" while a staged mission runs, else empty.</summary>
        public string StageProgress =>
            _director != null && _director.Plan != null && !_director.Complete
                ? $"{Mathf.Clamp(_director.StageIndex + 1, 1, _director.Plan.stages.Length)}"
                  + $"/{_director.Plan.stages.Length}"
                : "";

        /// <summary>Road North band modifier ("THE MIST THICKENS"), or empty.</summary>
        /// <summary>Current road band label, or empty. Now the boss modifier.</summary>
        public string MarchModifier => _run != null ? _run.BossModifier : "";


        public string Objective
        {
            get
            {
                if (State != Phase.Playing) return "";
                // A staged mission owns its own objective text, beat by beat.
                if (_director != null && !_director.Complete) return _director.Objective;
                if (HoldMode)
                    return $"HOLD THE ROAD — {Mathf.CeilToInt(Mathf.Max(0, CurrentLevel.holdSeconds - MissionTime))}s";
                if (_endless)
                    return _run != null
                        ? _run.Objective
                        : _waveActive && AliveEnemies > 0
                            ? $"CUT THROUGH — {AliveEnemies} BAR THE ROAD"
                            : "MARCH NORTH";
                if (EscortMode)
                {
                    var npc = Missions.EscortNpc.Active;
                    if (npc == null) return "";
                    return npc.UnderThreat
                        ? "THEY'RE ON THE BEARER — CLEAR THE ROAD"
                        : $"WALK THE FLAME HOME — {Mathf.RoundToInt(npc.Progress01 * 100f)}%";
                }
                if (StealthMode)
                    return AlarmRaised
                        ? $"ALARM RAISED — CUT THEM DOWN — {AliveEnemies} LEFT"
                        : $"UNSEEN — {AliveEnemies} STILL BREATHING";
                if (MissionKind == MissionObjective.Chase)
                    return $"RUN THEM DOWN — {AliveEnemies} LEFT";
                var alive = AliveEnemies;
                if (_waveActive && alive > 0)
                    return ModeNow == LaunchMode.Duel && CurrentDuel != null
                        ? $"DEFEAT {CurrentDuel.name}"
                        : $"DEFEAT ALL ENEMIES — {alive} LEFT";
                return "NEXT WAVE INCOMING…";
            }
        }

        public EnemyBrain DuelOpponent
        {
            get
            {
                foreach (var e in EnemyBrain.Active)
                    if (e != null && !e.Dead && !e.isClone) return e;
                return null;
            }
        }

        private static int AliveEnemies
        {
            get
            {
                var n = 0;
                foreach (var e in EnemyBrain.Active)
                    if (e != null && !e.Dead) n++;
                return n;
            }
        }

        private void Awake()
        {
            // The frame cap is no longer a constant. PerfGovernor picks it from
            // context (only live gameplay earns the full rate) and from the
            // device's own thermal status. Pinning 60 here meant a full 3D arena
            // was redrawn sixty times a second behind every menu and pause screen.
            QualitySettings.vSyncCount = 0;
            PerfGovernor.Ensure(gameObject);
            QualitySettings.shadowDistance = 28f;
            // Statics survive scene loads but the objects they point at do not, so
            // every cross-scene cache is reset here, once, before anything spawns.
            CinematicActive = false;
            EnemySpeedMul = 1f;
            Time.timeScale = 1f;   // a dip interrupted by a scene load would persist
            Player.CombatController.TimeFrozen = false;
            EnemyPool.Clear();
            Player.Kunai.ResetPool();
            Player.SmokeBomb.ResetPool();
            EnemyBomb.ResetPool(); // added with the Bomber, missed by the Phase 7 sweep
            Enemies.NoiseSystem.Clear();
            Enemies.BodyWatch.Clear();
            Enemies.Visibility.ClearLights();
        }

        private void Start()
        {
            Sfx3D.Init(gameObject);
            _combat = FindFirstObjectByType<Player.CombatController>();
            _rig = FindFirstObjectByType<CameraRig>();
            if (_combat != null)
            {
                _playerT = _combat.transform;
                _gates = _combat.GetComponent<SenGates>();
                _playerHealth = _combat.GetComponent<Health>();

                if (_gates != null) _gates.OnGateCracked += _ =>
                {
                    GatesCrackedTotal++;
                    _rig?.Shake(9f, 0.35f);
                };
                _playerRig = _combat.GetComponent<CharacterRig>();
                _playerHealth.OnHurt += (amount, from) =>
                {
                    if (_endless) Endless.RunStats.BreakCombo();
                    DamageTaken += amount;
                    _combat.OnPlayerHit();
                    _playerRig?.Flash();
                    _playerRig?.PlayOneShot(RigPose.Hurt, 0.25f);
                    if (_playerHealth.Hp < _playerHealth.MaxHp * 0.3f)
                        _playerRig?.SetMood(RigMood.Enraged);
                    Sfx3D.Hurt();
                    Haptics.Buzz();
                    _rig?.Shake(5f, 0.2f);
                    FloatingText.Spawn(_playerT.position + Vector3.up * 2.3f,
                        Mathf.RoundToInt(amount).ToString(), new Color(1f, 0.32f, 0.25f), 1.15f);
                };
                _playerHealth.OnDeath += () =>
                {
                    _playerRig?.PlayOneShot(RigPose.Dead, 0.7f);
                    Sfx3D.PlayerDeath();
                    OnPlayerDeath();
                };
            }

            // The environment theme owns the ambience bed (AtmosphereSpawner);
            // this is only the pre-theme fallback so boot is never silent.
            Sfx3D.PlayAmbience(isMarshScene ? "marsh_ambience" : null);
            Sfx3D.SetMusicState(Sfx3D.MusicState.Exploration, 0.1f);
            FxPools.Prewarm(); // pay the pool cost behind the menu, not mid-fight
            ConfigureFromSession();
        }

        // ------------------------------------------------------ configuration

        private void ConfigureFromSession()
        {
            Kills = 0;
            EnemyBrain.ResetAiTelemetry();
            switch (Session.Mode)
            {
                case LaunchMode.Story:
                {
                    var level = Session.Story[Mathf.Clamp(Session.LevelIndex, 0, Session.Story.Length - 1)];
                    if (level.marsh != isMarshScene) { LoadThemeScene(level.marsh); return; }
                    CurrentLevel = level;
                    _waves = level.waves;
                    _playerHealth?.SetMax(110f * Difficulty.Now.PlayerHp);
                    // Per-level atmosphere.
                    if (level.id == 3) UI.LevelFx.EnableRain();
                    if (level.id == 5) UI.LevelFx.SpawnFootprints();
                    if (level.objective == MissionObjective.Escort) SpawnEscort(level);
                    // Staged plan, if this level has one authored. The plan drives
                    // objectives and pacing; waves remain the fallback.
                    CurrentPlan = Resources.Load<Missions.MissionPlan>(
                        $"Missions/{level.planAsset}");
                    State = Phase.Intro;
                    break;
                }
                case LaunchMode.Duel:
                {
                    var duel = Session.Duels[Mathf.Clamp(Session.DuelIndex, 0, Session.Duels.Length - 1)];
                    if (duel.marsh != isMarshScene) { LoadThemeScene(duel.marsh); return; }
                    CurrentDuel = duel;
                    _waves = new[] { new[] { duel.kind } };
                    // The chosen handicap only touches the duel's own terms.
                    _playerHealth?.SetMax(110f * Session.CurrentDuelModifier.playerHpMul
                        * Difficulty.Now.PlayerHp);
                    State = Phase.Intro;
                    break;
                }
                case LaunchMode.Endless:
                {
                    if (isMarshScene) { LoadThemeScene(false); return; }
                    _endless = true;
                    _waves = new EnemyKind[0][];
                    _playerHealth?.SetMax(140f * Difficulty.Now.PlayerHp);
                    // The Road North: open the arena and start streaming corridor.
                    if (_playerT != null)
                    {
                        _road = RoadNorth.Begin(_playerT);
                    }
                    _run = new Endless.EndlessDirector(this);
                    State = Phase.Intro;
                    break;
                }
                default:
                    State = Phase.Menu;
                    break;
            }
        }

        private static void LoadThemeScene(bool marsh) =>
            SceneManager.LoadScene(marsh ? "Marsh" : "Rooftop");

        /// <summary>Escort levels: the bearer walks the long axis of the arena.</summary>
        private void SpawnEscort(LevelDef level)
        {
            var start = new Vector3(-arenaHalfExtents.x + 1.5f, 0f, -arenaHalfExtents.y + 2f);
            var goal = new Vector3(arenaHalfExtents.x - 1.5f, 0f, arenaHalfExtents.y - 2f);
            Missions.EscortNpc.Spawn(start, goal, level.escortSeconds, 130f);
        }

        /// <summary>
        /// A stealth enemy finished its detection meter: everyone wakes up and the
        /// mission converts to a straight fight. Losing surprise costs rank rather
        /// than ending the run — a failed sneak should still be playable.
        /// </summary>
        public void RaiseAlarm(EnemyBrain spotter)
        {
            if (AlarmRaised) return;
            AlarmRaised = true;
            Announce("ALARM — THEY'VE SEEN YOU");
            Sfx3D.BossRoar();
            _rig?.Shake(6f, 0.3f);
            foreach (var e in EnemyBrain.Active)
                if (e != null && !e.Dead) e.Alert();
            if (spotter != null)
                FloatingText.Spawn(spotter.transform.position + Vector3.up * 2.6f, "SPOTTED",
                    new Color(1f, 0.32f, 0.25f), 1.15f);
        }

        // -------------------------------------------------------- hud actions

        public void LaunchStory(int index)
        {
            Sfx3D.Ui();
            Session.Mode = LaunchMode.Story;
            Session.LevelIndex = index;
            LoadThemeScene(Session.Story[index].marsh);
        }

        public void LaunchDuel(int index)
        {
            Sfx3D.Ui();
            Session.Mode = LaunchMode.Duel;
            Session.DuelIndex = index;
            LoadThemeScene(Session.Duels[index].marsh);
        }

        public void LaunchEndless()
        {
            Sfx3D.Ui();
            Session.Mode = LaunchMode.Endless;
            LoadThemeScene(false);
        }

        public void OpenMenu()
        {
            Sfx3D.Ui();
            Session.Mode = LaunchMode.None;
            Time.timeScale = 1f;
            LoadThemeScene(false);
        }

        public void Retry()
        {
            Sfx3D.Ui();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextStoryLevel()
        {
            var next = Session.LevelIndex + 1;
            if (next < Session.Story.Length) LaunchStory(next);
            else OpenMenu();
        }

        public void NextDuel()
        {
            var next = Session.DuelIndex + 1;
            if (next < Session.Duels.Length && next < Session.DuelsUnlocked) LaunchDuel(next);
            else OpenMenu();
        }

        public void BeginMission()
        {
            if (State != Phase.Intro) return;
            State = Phase.Playing;
            if (CurrentPlan != null)
                _director = Missions.MissionDirector.Begin(CurrentPlan, this);
            _interWave = 0.9f;
            Sfx3D.Ui();
        }

        // -------------------------------------------------------------- flow

        /// <summary>
        /// Music follows the fight, not the level: exploration until something is
        /// awake and near, combat while it is, boss whenever one is on the field.
        /// Re-evaluated a few times a second — a per-frame scan of the roster is
        /// pointless for a state that changes every thirty seconds.
        /// </summary>
        private void UpdateMusicState()
        {
            if ((_musicPoll -= Time.deltaTime) > 0f) return;
            _musicPoll = 0.3f;
            var boss = false;
            var engaged = false;
            var here = _combat != null ? _combat.transform.position : Vector3.zero;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                if (e.IsBossTarget) { boss = true; break; }
                if (!e.Unaware && Vector3.Distance(e.transform.position, here) < 22f)
                    engaged = true;
            }
            Sfx3D.SetMusicState(boss ? Sfx3D.MusicState.Boss
                : engaged ? Sfx3D.MusicState.Combat
                : Sfx3D.MusicState.Exploration);
        }

        private float _musicPoll;

        private void Update()
        {
            if (State != Phase.Playing || CinematicActive) return;
            MissionTime += Time.deltaTime;
            UpdateMusicState();
            if (BannerTimer > 0) BannerTimer -= Time.deltaTime;

            if (_endless)
            {
                MarchUpdate();
                return;
            }

            // A staged mission resolves on its own terms; the wave loop below is
            // only for levels without a plan.
            if (_director != null)
            {
                if (_director.Complete) { Win(); return; }
                if (_director.Failed) { OnPlayerDeath(); return; }
                return;
            }

            if (HoldMode && MissionTime >= CurrentLevel.holdSeconds)
            {
                Win();
                return;
            }

            if (EscortMode)
            {
                var npc = Missions.EscortNpc.Active;
                if (npc != null)
                {
                    if (npc.Health.Dead)
                    {
                        npc.Extinguish();
                        Announce("THE FLAME IS OUT");
                        OnPlayerDeath(); // same failure path: the run is over
                        return;
                    }
                    if (npc.Progress01 >= 1f)
                    {
                        Win();
                        return;
                    }
                }
            }

            if (_waveActive && AliveEnemies == 0)
            {
                _waveActive = false;
                // Escort and Hold run until their own condition resolves, so their
                // waves keep coming instead of ending the mission when the last
                // authored one is cleared.
                if (!_endless && !HoldMode && !EscortMode && WaveIndex + 1 >= _waves.Length)
                {
                    Win();
                    return;
                }
                _interWave = EscortMode ? 3f : 2f;
                _gates?.MendGate();
                if (SkillTree.Has("gate_mend")) _gates?.MendGate();
                if (_playerHealth != null && _playerT != null && ModeNow != LaunchMode.Duel)
                {
                    // Small top-up, not a reset: attrition across a mission should
                    // actually accumulate.
                    _playerHealth.Heal(Difficulty.ScaleHeal(15f));
                    FloatingText.Spawn(_playerT.position + Vector3.up * 2.3f, "+15",
                        new Color(0.5f, 0.9f, 0.55f), 1.1f);
                }
                Announce(EscortMode ? "THE ROAD IS CLEAR — KEEP MOVING"
                    : "WAVE CLEARED — GATES MENDING");
            }

            if (!_waveActive && (_interWave -= Time.deltaTime) <= 0)
                SpawnWave();
        }

        private void SpawnWave()
        {
            WaveIndex++;
            _waveActive = true;
            // Open-ended objectives cycle the authored waves; fixed ones walk them
            // once and stop on the last.
            var kinds = HoldMode || EscortMode ? _waves[WaveIndex % _waves.Length]
                : _waves[Mathf.Min(WaveIndex, _waves.Length - 1)];

            Announce(ModeNow switch
            {
                LaunchMode.Duel => CurrentDuel != null ? $"{CurrentDuel.name} — {CurrentDuel.title}" : "DUEL",
                _ => WaveCount > 0 ? $"WAVE {WaveIndex + 1} / {WaveCount}" : $"WAVE {WaveIndex + 1}",
            });

            foreach (var kind in kinds)
            {
                var prefab = enemyPrefabs[(int)kind];
                if (prefab == null) continue;
                Vector3 p;
                if (ModeNow == LaunchMode.Duel)
                {
                    p = new Vector3(0, 0, 5f); // duelists enter face to face
                }
                else if (kind == EnemyKind.Shade)
                {
                    // Shades materialize out of the reeds when the arena has them.
                    var edge = new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0,
                        Random.value < 0.5f ? arenaHalfExtents.y - 0.5f : -arenaHalfExtents.y + 0.5f);
                    p = ArenaMarkers.RandomShadeSpawn(edge);
                }
                else
                {
                    var edge = Random.Range(0, 4);
                    p = edge switch
                    {
                        0 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, arenaHalfExtents.y - 0.5f),
                        1 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, -arenaHalfExtents.y + 0.5f),
                        2 => new Vector3(-arenaHalfExtents.x + 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
                        _ => new Vector3(arenaHalfExtents.x - 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
                    };
                }
                var enemy = EnemyPool.Spawn(prefab, p, Quaternion.identity);
                var brain = enemy.GetComponent<EnemyBrain>();
                if (brain != null)
                {
                    // Duels are full-strength showdowns: mook kinds get a boss-grade
                    // HP floor so "one life each" doesn't end in three seconds.
                    if (ModeNow == LaunchMode.Duel)
                    {
                        brain.maxHp = Mathf.Max(brain.maxHp, 190f);
                        brain.damage = Mathf.Max(brain.damage, 12f);
                        var mod = Session.CurrentDuelModifier;
                        brain.maxHp *= mod.bossHpMul;
                        brain.speed *= mod.bossSpeedMul;
                    }
                    // New Game+: the marsh remembers — everything hits harder.
                    if (Session.NewGamePlus && ModeNow != LaunchMode.Endless)
                    {
                        brain.maxHp *= 1.5f;
                        brain.damage *= 1.35f;
                    }
                    Difficulty.ApplyTo(brain); // outermost multiplier, after mode scaling
                    brain.SyncHpToMax();
                    // Stealth: they haven't noticed you yet — until the alarm.
                    if (StealthMode && !AlarmRaised) brain.SetUnaware(true);
                }
            }

            _playerRig?.SetMood(_playerHealth != null && _playerHealth.Hp < _playerHealth.MaxHp * 0.3f
                ? RigMood.Enraged : RigMood.Focused);
            TryBossIntro(kinds);
        }

        /// <summary>First boss appearance in a mission gets the cinematic card.</summary>
        private void TryBossIntro(EnemyKind[] kinds)
        {
            if (_bossIntroShown) return;
            string name = null, title = null, taunt = null;
            EnemyKind bossKind = default;
            if (ModeNow == LaunchMode.Duel && CurrentDuel != null)
            {
                (name, title, taunt, bossKind) = (CurrentDuel.name, CurrentDuel.title, CurrentDuel.taunt, CurrentDuel.kind);
            }
            else
            {
                foreach (var k in kinds)
                    if (k is EnemyKind.Chief or EnemyKind.Kagachi or EnemyKind.Jin)
                    {
                        var card = Session.BossCard(k);
                        (name, title, taunt, bossKind) = (card.name, card.title, card.taunt, k);
                        break;
                    }
            }
            if (name == null) return;

            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead || e.isClone || e.kind != bossKind) continue;
                _bossIntroShown = true;
                e.transform.LookAt(_playerT != null
                    ? new Vector3(_playerT.position.x, 0, _playerT.position.z)
                    : Vector3.zero);
                UI.BossIntroDirector.Play(this, _rig, e.transform,
                    e.GetComponent<CharacterRig>(), name, title, taunt, e.weapon);
                return;
            }
        }

        /// <summary>Enemy speed multiplier (rises with distance marched). Reset per scene.</summary>
        public static float EnemySpeedMul { get; private set; } = 1f;

        // -------------------------------------------------- the Road North

        /// <summary>
        /// Endless flow. The run director decides what happens on the road; this
        /// only pumps it. The old distance-scaled pack loop it replaced lived
        /// here — see docs/CHANGELOG.md 1.2.0.
        /// </summary>
        private void MarchUpdate() => _run?.Tick(Time.deltaTime);

        private void Win()
        {
            State = Phase.Won;
            _playerRig?.SetMood(RigMood.Calm);
            Sfx3D.Win();
            var r = MissionResult();
            if (ModeNow == LaunchMode.Story && CurrentLevel != null)
            {
                StarsEarned = r.rank is "S" or "A" ? 3 : r.rank == "B" ? 2 : 1;
                Session.SaveStars(CurrentLevel.id, StarsEarned);
                Session.StoryUnlocked = CurrentLevel.id + 1;
                ShardsEarned = 1 + (r.rank == "S" ? 2 : r.rank == "A" ? 1 : 0);
                if (CurrentLevel.id == 10 && !Session.NewGamePlus)
                {
                    Session.NewGamePlus = true;
                    Announce("NEW GAME+ UNLOCKED — THE MARSH REMEMBERS");
                }
            }
            else if (ModeNow == LaunchMode.Duel && CurrentDuel != null)
            {
                ShardsEarned = (Session.DuelWon(CurrentDuel.id) ? 1 : 3)
                               + Session.CurrentDuelModifier.bonusShards;
                Session.SaveDuelWin(CurrentDuel.id);
                Session.DuelsUnlocked = CurrentDuel.id + 1;
            }
            // Optional objectives banked by the director pay on top.
            if (_director != null) ShardsEarned += _director.BonusShards;
            SkillTree.Shards += ShardsEarned;
            ScoreMission(won: true);
        }

        /// <summary>
        /// One snapshot of how the mission went, handed to both the daily and the
        /// feats — they ask the same questions, so they read the same answers.
        /// </summary>
        private Feats.MissionSummary BuildSummary(bool won)
        {
            var postsIntact = 0;
            foreach (var post in LanternPost.Active)
                if (post != null && !post.Broken) postsIntact++;
            var npc = Missions.EscortNpc.Active;
            var motor = _combat != null ? _combat.GetComponent<Player.PlayerLocomotion>() : null;
            return new Feats.MissionSummary
            {
                mode = ModeNow,
                won = won,
                levelId = CurrentLevel?.id ?? 0,
                damageTaken = DamageTaken,
                maxCombo = _combat != null ? _combat.MaxCombo : 0,
                waveReached = WaveIndex + 1,
                postsIntact = postsIntact,
                timeSeconds = MissionTime,
                alarmRaised = AlarmRaised,
                escortHealth01 = npc != null && npc.Health != null
                    ? npc.Health.Hp / npc.Health.MaxHp : 1f,
                deflects = _combat != null ? _combat.Deflects : 0,
                wallRuns = motor != null ? motor.WallRuns : 0,
            };
        }

        private void ScoreMission(bool won)
        {
            var summary = BuildSummary(won);
            DailyShards = DailyChallenge.Evaluate(summary);
            WeeklyShards = DailyChallenge.EvaluateWeekly(summary);
            var earned = Feats.Evaluate(summary);
            FeatsLine = earned.Count == 0 ? ""
                : "FEAT — " + string.Join("  ·  ", earned.ConvertAll(f => f.title));
        }

        private void OnPlayerDeath()
        {
            if (State is Phase.Won or Phase.Lost) return;
            State = Phase.Lost;
            Sfx3D.Lose();
            NewRecord = false;
            if (_endless)
            {
                if (WaveIndex + 1 > BestWave)
                    PlayerPrefs.SetInt("best_wave", WaveIndex + 1);
                if (DistanceNorth > BestNorth)
                {
                    PlayerPrefs.SetInt("best_north", DistanceNorth);
                    NewRecord = true;
                }
                PlayerPrefs.Save();
                // Commit the run once, here: the record book and the results card
                // both read from RunStats afterwards.
                Endless.RunStats.Commit();
                Endless.RunHazard.ClearAll();
            }
            ScoreMission(won: false);
        }

        public void Announce(string text)
        {
            Banner = text;
            BannerTimer = 2.2f;
        }

        /// <summary>
        /// Spawn a single enemy at an arena edge. Public so the MissionDirector can
        /// populate a stage without owning spawn logic itself.
        /// </summary>
        public void SpawnOne(EnemyKind kind, bool unaware)
        {
            var prefab = enemyPrefabs != null && (int)kind < enemyPrefabs.Length
                ? enemyPrefabs[(int)kind] : null;
            if (prefab == null) return;

            var edge = Random.Range(0, 4);
            var p = edge switch
            {
                0 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, arenaHalfExtents.y - 0.5f),
                1 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, -arenaHalfExtents.y + 0.5f),
                2 => new Vector3(-arenaHalfExtents.x + 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
                _ => new Vector3(arenaHalfExtents.x - 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
            };
            var go = EnemyPool.Spawn(prefab, p, Quaternion.Euler(0, 180f, 0));
            var brain = go != null ? go.GetComponent<EnemyBrain>() : null;
            if (brain == null) return;
            Difficulty.ApplyTo(brain);
            brain.SyncHpToMax();
            if (unaware) brain.SetUnaware(true);
        }

        // ---- surface the director needs. Endless owns *what* happens; the
        // GameManager still owns the road, the spawner and the player refs.

        /// <summary>Player transform, or null before the scene is wired.</summary>
        public Transform PlayerT => _playerT;

        /// <summary>The streaming corridor, or null outside endless mode.</summary>
        public RoadNorth Road => _road;

        public Health PlayerHealth => _playerHealth;

        /// <summary>Endless run director, or null outside endless mode.</summary>
        public Endless.EndlessDirector Run => _run;

        public static void SetEnemySpeedMul(float value) => EnemySpeedMul = value;

        public void ShowBossIntro(EnemyKind[] kinds) => TryBossIntro(kinds);

        /// <summary>
        /// Spawn one run enemy at a world point with the run's stat scaling.
        /// Returns the brain so the caller can decorate it (boss modifiers).
        /// </summary>
        public EnemyBrain SpawnRunEnemy(EnemyKind kind, Vector3 at, float hpMul, float dmgMul)
        {
            var prefab = enemyPrefabs != null && (int)kind < enemyPrefabs.Length
                ? enemyPrefabs[(int)kind] : null;
            if (prefab == null) return null;
            var go = EnemyPool.Spawn(prefab, at, Quaternion.Euler(0, 180f, 0));
            var brain = go != null ? go.GetComponent<EnemyBrain>() : null;
            if (brain == null) return null;
            brain.maxHp *= hpMul;
            brain.damage *= dmgMul;
            Difficulty.ApplyTo(brain);
            brain.SyncHpToMax();
            return brain;
        }

        public void OnEnemyKilled(bool boss)
        {
            Kills++;
            if (_endless) Endless.RunStats.OnKill();
            if (boss) _rig?.Shake(8f, 0.4f);
            // The kill that empties a wave gets a beat of slow motion. Checked here
            // rather than at the wave-clear tick so it lands on the killing blow.
            if (State == Phase.Playing && _waveActive && AliveEnemies == 0)
            {
                _combat?.PlaySlowMo(boss ? 0.5f : 0.32f);
                _rig?.Shake(boss ? 10f : 5f, 0.35f);
            }
        }

        public void OnPerfectDodge()
        {
            Sfx3D.Ui();
            _combat?.OnPerfectDodge();
            if (SkillTree.Has("dodge_heal")) _playerHealth?.Heal(Difficulty.ScaleHeal(5f));
            if (_playerT != null)
                FloatingText.Spawn(_playerT.position + Vector3.up * 2.5f, "PERFECT",
                    new Color(0.75f, 0.9f, 1f), 0.9f);
        }

        public (string rank, int score) MissionResult()
        {
            var score = 1000f;
            score -= DamageTaken * 5f;
            score -= Mathf.Max(0, MissionTime - 130f) * 3f;
            score += (_combat != null ? _combat.MaxCombo : 0) * 12f;
            score += Mathf.Max(0, 4 - GatesCrackedTotal) * 45f;
            // Stealth: staying unseen is the whole point, so it's worth a rank.
            if (StealthMode) score += AlarmRaised ? -180f : 200f;
            // Escort: how much of the bearer's health you kept.
            if (EscortMode && Missions.EscortNpc.Active != null)
            {
                var npc = Missions.EscortNpc.Active;
                score += (npc.Health.Hp / npc.Health.MaxHp) * 180f - 90f;
            }
            var s = Mathf.RoundToInt(score);
            var rank = s >= 1220 ? "S" : s >= 1080 ? "A" : s >= 920 ? "B" : s >= 760 ? "C" : "D";
            return (rank, s);
        }
    }
}
