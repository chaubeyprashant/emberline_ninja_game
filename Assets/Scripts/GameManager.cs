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
        public bool NewRecord { get; private set; }
        public int BestWave => PlayerPrefs.GetInt("best_wave", 0);
        public int WaveCount => _waves?.Length ?? 0;

        private EnemyKind[][] _waves;
        private float _interWave = 1.2f;
        private bool _waveActive;
        private bool _endless;
        private SenGates _gates;
        private Health _playerHealth;
        private Player.CombatController _combat;
        private CameraRig _rig;
        private Transform _playerT;

        public string Objective
        {
            get
            {
                if (State != Phase.Playing) return "";
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
                var playerRig = _combat.GetComponent<NinjaRig>();
                _playerHealth.OnHurt += (amount, from) =>
                {
                    DamageTaken += amount;
                    _combat.OnPlayerHit();
                    playerRig?.Flash();
                    playerRig?.PlayOneShot(RigPose.Hurt, 0.25f);
                    Sfx3D.Hurt();
                    _rig?.Shake(5f, 0.2f);
                    FloatingText.Spawn(_playerT.position + Vector3.up * 2.3f,
                        Mathf.RoundToInt(amount).ToString(), new Color(1f, 0.32f, 0.25f), 1.15f);
                };
                _playerHealth.OnDeath += () =>
                {
                    playerRig?.PlayOneShot(RigPose.Dead, 0.7f);
                    OnPlayerDeath();
                };
            }

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
            if (State != Phase.Playing) return;
            MissionTime += Time.deltaTime;
            if (BannerTimer > 0) BannerTimer -= Time.deltaTime;

            if (_waveActive && AliveEnemies == 0)
            {
                _waveActive = false;
                if (!_endless && WaveIndex + 1 >= _waves.Length)
                {
                    Win();
                    return;
                }
                _interWave = 2f;
                _gates?.MendGate();
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
            var kinds = _endless ? EndlessWave(WaveIndex + 4)
                : _waves[Mathf.Min(WaveIndex, _waves.Length - 1)];

            Announce(ModeNow switch
            {
                LaunchMode.Duel => CurrentDuel != null ? $"{CurrentDuel.name} — {CurrentDuel.title}" : "DUEL",
                LaunchMode.Endless => $"WAVE {WaveIndex + 1} — THE MIST DEEPENS",
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
                Instantiate(prefab, p, Quaternion.identity);
            }
        }

        private static EnemyKind[] EndlessWave(int n)
        {
            var list = new System.Collections.Generic.List<EnemyKind>();
            var tier = n - 4;
            for (var i = 0; i < Mathf.Min(7, 3 + tier / 2); i++) list.Add(EnemyKind.Bandit);
            for (var i = 0; i < Mathf.Min(4, 1 + tier / 3); i++) list.Add(EnemyKind.Ranged);
            for (var i = 0; i < Mathf.Min(3, tier / 3); i++) list.Add(EnemyKind.Shade);
            if (tier > 0 && tier % 4 == 0) list.Add(EnemyKind.Chief);
            return list.ToArray();
        }

        private void Win()
        {
            State = Phase.Won;
            Sfx3D.Win();
            var r = MissionResult();
            if (ModeNow == LaunchMode.Story && CurrentLevel != null)
            {
                StarsEarned = r.rank is "S" or "A" ? 3 : r.rank == "B" ? 2 : 1;
                Session.SaveStars(CurrentLevel.id, StarsEarned);
                Session.StoryUnlocked = CurrentLevel.id + 1;
            }
            else if (ModeNow == LaunchMode.Duel && CurrentDuel != null)
            {
                Session.SaveDuelWin(CurrentDuel.id);
                Session.DuelsUnlocked = CurrentDuel.id + 1;
            }
        }

        private void OnPlayerDeath()
        {
            if (State is Phase.Won or Phase.Lost) return;
            State = Phase.Lost;
            Sfx3D.Lose();
            NewRecord = false;
            if (_endless && WaveIndex + 1 > BestWave)
            {
                PlayerPrefs.SetInt("best_wave", WaveIndex + 1);
                PlayerPrefs.Save();
                NewRecord = true;
            }
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
