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
        public int DailyShards { get; private set; }
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
        private float _nextPackZ, _nextBossAt, _nextShardAt;
        private int _bossCycle;
        private SenGates _gates;
        private Health _playerHealth;
        private Player.CombatController _combat;
        private CameraRig _rig;
        private Transform _playerT;
        private CharacterRig _playerRig;

        private bool HoldMode => ModeNow == LaunchMode.Story && CurrentLevel != null
                                 && CurrentLevel.holdSeconds > 0f;

        public string Objective
        {
            get
            {
                if (State != Phase.Playing) return "";
                if (HoldMode)
                    return $"HOLD THE ROAD — {Mathf.CeilToInt(Mathf.Max(0, CurrentLevel.holdSeconds - MissionTime))}s";
                if (_endless)
                    return _waveActive && AliveEnemies > 0
                        ? $"CUT THROUGH — {AliveEnemies} BAR THE ROAD"
                        : "MARCH NORTH";
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
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = 28f;
            CinematicActive = false; // statics survive scene loads
            EnemySpeedMul = 1f;
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

            Sfx3D.PlayAmbient(isMarshScene ? "marsh_ambience" : null);
            FxPools.Prewarm(); // pay the pool cost behind the menu, not mid-fight
            ConfigureFromSession();
        }

        // ------------------------------------------------------ configuration

        private void ConfigureFromSession()
        {
            switch (Session.Mode)
            {
                case LaunchMode.Story:
                {
                    var level = Session.Story[Mathf.Clamp(Session.LevelIndex, 0, Session.Story.Length - 1)];
                    if (level.marsh != isMarshScene) { LoadThemeScene(level.marsh); return; }
                    CurrentLevel = level;
                    _waves = level.waves;
                    _playerHealth?.SetMax(140f);
                    // Per-level atmosphere.
                    if (level.id == 3) UI.LevelFx.EnableRain();
                    if (level.id == 5) UI.LevelFx.SpawnFootprints();
                    State = Phase.Intro;
                    break;
                }
                case LaunchMode.Duel:
                {
                    var duel = Session.Duels[Mathf.Clamp(Session.DuelIndex, 0, Session.Duels.Length - 1)];
                    if (duel.marsh != isMarshScene) { LoadThemeScene(duel.marsh); return; }
                    CurrentDuel = duel;
                    _waves = new[] { new[] { duel.kind } };
                    _playerHealth?.SetMax(110f);
                    State = Phase.Intro;
                    break;
                }
                case LaunchMode.Endless:
                {
                    if (isMarshScene) { LoadThemeScene(false); return; }
                    _endless = true;
                    _waves = new EnemyKind[0][];
                    _playerHealth?.SetMax(140f);
                    // The Road North: open the arena and start streaming corridor.
                    if (_playerT != null)
                    {
                        _road = RoadNorth.Begin(_playerT);
                        _nextPackZ = _playerT.position.z + 8f;
                    }
                    _nextBossAt = 150f;
                    _nextShardAt = 100f;
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
            _interWave = 0.9f;
            Sfx3D.Ui();
        }

        // -------------------------------------------------------------- flow

        private void Update()
        {
            if (State != Phase.Playing || CinematicActive) return;
            MissionTime += Time.deltaTime;
            if (BannerTimer > 0) BannerTimer -= Time.deltaTime;

            if (_endless)
            {
                MarchUpdate();
                return;
            }

            if (HoldMode && MissionTime >= CurrentLevel.holdSeconds)
            {
                Win();
                return;
            }

            if (_waveActive && AliveEnemies == 0)
            {
                _waveActive = false;
                if (!_endless && !HoldMode && WaveIndex + 1 >= _waves.Length)
                {
                    Win();
                    return;
                }
                _interWave = 2f;
                _gates?.MendGate();
                if (SkillTree.Has("gate_mend")) _gates?.MendGate();
                if (_playerHealth != null && _playerT != null && ModeNow != LaunchMode.Duel)
                {
                    _playerHealth.Heal(35f);
                    FloatingText.Spawn(_playerT.position + Vector3.up * 2.3f, "+35",
                        new Color(0.5f, 0.9f, 0.55f), 1.1f);
                }
                Announce("WAVE CLEARED — GATES MENDING");
            }

            if (!_waveActive && (_interWave -= Time.deltaTime) <= 0)
                SpawnWave();
        }

        private void SpawnWave()
        {
            WaveIndex++;
            _waveActive = true;
            var kinds = HoldMode ? _waves[WaveIndex % _waves.Length]
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
                var enemy = Instantiate(prefab, p, Quaternion.identity);
                var brain = enemy.GetComponent<EnemyBrain>();
                if (brain != null)
                {
                    // Duels are full-strength showdowns: mook kinds get a boss-grade
                    // HP floor so "one life each" doesn't end in three seconds.
                    if (ModeNow == LaunchMode.Duel)
                    {
                        brain.maxHp = Mathf.Max(brain.maxHp, 190f);
                        brain.damage = Mathf.Max(brain.damage, 12f);
                    }
                    // New Game+: the marsh remembers — everything hits harder.
                    if (Session.NewGamePlus && ModeNow != LaunchMode.Endless)
                    {
                        brain.maxHp *= 1.5f;
                        brain.damage *= 1.35f;
                    }
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
                    e.GetComponent<CharacterRig>(), name, title, taunt);
                return;
            }
        }

        /// <summary>Enemy speed multiplier (rises with distance marched). Reset per scene.</summary>
        public static float EnemySpeedMul { get; private set; } = 1f;

        // -------------------------------------------------- the Road North

        /// <summary>
        /// March flow: packs trigger by distance, the mist walls the road while
        /// a pack lives, bosses bar the way at milestones, shards drop per 100m.
        /// </summary>
        private void MarchUpdate()
        {
            // The deeper north, the faster the mist moves.
            EnemySpeedMul = 1f + Mathf.Min(0.35f, DistanceNorth / 900f);

            if (_waveActive && AliveEnemies == 0)
            {
                _waveActive = false;
                _road?.ClearBarrier();
                _gates?.MendGate();
                if (SkillTree.Has("gate_mend")) _gates?.MendGate();
                if (_playerHealth != null && _playerT != null)
                {
                    _playerHealth.Heal(30f);
                    FloatingText.Spawn(_playerT.position + Vector3.up * 2.3f, "+30",
                        new Color(0.5f, 0.9f, 0.55f), 1.1f);
                }
                if (_playerT != null) _nextPackZ = _playerT.position.z + 22f;
                Announce("THE ROAD OPENS — NORTH");
            }

            if (!_waveActive && _playerT != null && _playerT.position.z >= _nextPackZ)
                SpawnPack();

            if (_road != null && DistanceNorth >= _nextShardAt)
            {
                _nextShardAt += 100f;
                SkillTree.Shards += 1;
                ShardsEarned += 1;
                Announce($"{DistanceNorth}m NORTH — ◆ EMBER SHARD EARNED");
            }
        }

        /// <summary>Spawn a soldier pack (or boss) ahead of the marcher and seal the road.</summary>
        private void SpawnPack()
        {
            WaveIndex++;
            _waveActive = true;
            var pz = _playerT.position.z;
            var dist = DistanceNorth;
            var list = new System.Collections.Generic.List<EnemyKind>();

            var boss = dist >= _nextBossAt;
            if (boss)
            {
                _nextBossAt += 200f;
                _bossIntroShown = false; // every road boss earns its cinematic card
                list.Add((_bossCycle++ % 3) switch
                {
                    0 => EnemyKind.Chief,
                    1 => EnemyKind.Jin,
                    _ => EnemyKind.Kagachi,
                });
                for (var i = 0; i < Mathf.Min(3, 1 + dist / 300); i++) list.Add(EnemyKind.Bandit);
                Announce("SOMETHING BARS THE ROAD…");
            }
            else
            {
                // Soldiers first; the mix hardens every 50 meters.
                var tier = Mathf.FloorToInt(dist / 50f);
                for (var i = 0; i < Mathf.Min(6, 2 + tier); i++) list.Add(EnemyKind.Bandit);
                for (var i = 0; i < Mathf.Min(3, tier - 1); i++) list.Add(EnemyKind.Ranged);
                for (var i = 0; i < Mathf.Min(3, tier - 2); i++) list.Add(EnemyKind.Shade);
                if (WaveIndex > 2 && WaveIndex % 5 == 2)
                {
                    string[] whispers =
                    {
                        "…the road remembers every march…",
                        "…a hundred lights beyond the mist…",
                        "…the Serpent counts its lanterns…",
                        "…the oldest flame walks north too…",
                        "…the gate is hungry, bearer…",
                    };
                    Announce(whispers[WaveIndex % whispers.Length]);
                    Sfx3D.ShadeWhisper();
                }
                else
                {
                    Announce($"SOLDIERS ON THE ROAD — {list.Count}");
                }
            }

            _road?.RaiseBarrier(pz + 20f);

            // Distance scaling: tougher and meaner the farther north the march goes.
            var hpMul = 1f + dist / 500f;
            var dmgMul = 1f + dist / 900f;
            foreach (var kind in list)
            {
                var prefab = enemyPrefabs[(int)kind];
                if (prefab == null) continue;
                float x, z;
                if (kind == EnemyKind.Shade)
                {
                    // Shades seep out of the parapet shadows beside the marcher.
                    x = (Random.value < 0.5f ? -1f : 1f) * (RoadNorth.HalfWidth - 1.2f);
                    z = pz + Random.Range(4f, 12f);
                }
                else
                {
                    x = Random.Range(-RoadNorth.HalfWidth + 1f, RoadNorth.HalfWidth - 1f);
                    z = pz + Random.Range(9f, 16f);
                }
                var enemy = Instantiate(prefab, new Vector3(x, 0, z), Quaternion.Euler(0, 180f, 0));
                var brain = enemy.GetComponent<EnemyBrain>();
                if (brain != null)
                {
                    brain.maxHp *= hpMul;
                    brain.damage *= dmgMul;
                }
            }

            _playerRig?.SetMood(_playerHealth != null && _playerHealth.Hp < _playerHealth.MaxHp * 0.3f
                ? RigMood.Enraged : RigMood.Focused);
            if (boss) TryBossIntro(list.ToArray());
        }

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
                ShardsEarned = Session.DuelWon(CurrentDuel.id) ? 1 : 3;
                Session.SaveDuelWin(CurrentDuel.id);
                Session.DuelsUnlocked = CurrentDuel.id + 1;
            }
            SkillTree.Shards += ShardsEarned;
            DailyShards = DailyChallenge.Evaluate(ModeNow, true, DamageTaken, MissionTime,
                _combat != null ? _combat.MaxCombo : 0, WaveIndex + 1);
            EvaluateFeats(won: true);
        }

        private void EvaluateFeats(bool won)
        {
            var postsIntact = 0;
            foreach (var post in LanternPost.Active)
                if (post != null && !post.Broken) postsIntact++;
            var earned = Feats.Evaluate(new Feats.MissionSummary
            {
                mode = ModeNow,
                won = won,
                levelId = CurrentLevel?.id ?? 0,
                damageTaken = DamageTaken,
                maxCombo = _combat != null ? _combat.MaxCombo : 0,
                waveReached = WaveIndex + 1,
                postsIntact = postsIntact,
            });
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
            }
            DailyShards = DailyChallenge.Evaluate(ModeNow, false, DamageTaken, MissionTime,
                _combat != null ? _combat.MaxCombo : 0, WaveIndex + 1);
            EvaluateFeats(won: false);
        }

        public void Announce(string text)
        {
            Banner = text;
            BannerTimer = 2.2f;
        }

        public void OnEnemyKilled(bool boss)
        {
            if (boss) _rig?.Shake(8f, 0.4f);
        }

        public void OnPerfectDodge()
        {
            Sfx3D.Ui();
            _combat?.OnPerfectDodge();
            if (SkillTree.Has("dodge_heal")) _playerHealth?.Heal(5f);
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
            var s = Mathf.RoundToInt(score);
            var rank = s >= 1220 ? "S" : s >= 1080 ? "A" : s >= 920 ? "B" : s >= 760 ? "C" : "D";
            return (rank, s);
        }
    }
}
