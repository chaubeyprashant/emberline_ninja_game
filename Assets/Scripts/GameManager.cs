using UnityEngine;
using UnityEngine.SceneManagement;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using Emberline.UI;

namespace Emberline
{
    /// <summary>
    /// Mission flow: intro screen → authored waves → victory (with records) or
    /// defeat, plus the post-victory Endless Trial. Also owns mobile perf setup.
    /// Fields are public so the editor bootstrap can wire the scene in batch mode.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public MissionDef mission;
        public GameObject[] enemyPrefabs; // indexed by (int)EnemyKind
        public Vector2 arenaHalfExtents = new(13f, 8f);
        public string otherSceneName = "";       // the other mission's scene
        public string otherMissionLabel = "";

        public enum Phase { Intro, Playing, Won, Lost }

        public Phase State { get; private set; } = Phase.Intro;
        public bool Endless { get; private set; }
        public int WaveIndex { get; private set; } = -1;
        public float MissionTime { get; private set; }
        public float DamageTaken { get; private set; }
        public int GatesCrackedTotal { get; private set; }
        public string Banner { get; private set; } = "";
        public float BannerTimer { get; private set; }
        public bool NewRecord { get; private set; }
        public int BestScore => PlayerPrefs.GetInt("best_score", 0);
        public string BestRank => PlayerPrefs.GetString("best_rank", "—");
        public int BestWave => PlayerPrefs.GetInt("best_wave", 0);
        public int WaveCount => mission != null ? mission.waves.Length : 0;

        public string Objective
        {
            get
            {
                if (State == Phase.Intro) return "";
                var alive = AliveEnemies;
                if (_waveActive && alive > 0)
                    return $"DEFEAT ALL ENEMIES — {alive} LEFT";
                return State == Phase.Playing ? "NEXT WAVE INCOMING…" : "";
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

        private float _interWave = 1.2f;
        private bool _waveActive;
        private SenGates _gates;
        private Health _playerHealth;
        private Player.CombatController _combat;
        private CameraRig _rig;
        private Transform _playerT;

        private void Awake()
        {
            // The Android default is 30fps with vsync — the single biggest "lag" fix.
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
                _playerHealth.SetMax(140f); // onboarding-friendly pool; heals between waves

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
        }

        public void LoadOtherMission()
        {
            if (string.IsNullOrEmpty(otherSceneName)) return;
            Sfx3D.Ui();
            Time.timeScale = 1f;
            SceneManager.LoadScene(otherSceneName);
        }

        /// <summary>Called by the intro overlay.</summary>
        public void BeginMission()
        {
            if (State != Phase.Intro) return;
            State = Phase.Playing;
            _interWave = 0.9f;
            Sfx3D.Ui();
        }

        private void Update()
        {
            if (State != Phase.Playing || mission == null) return;
            MissionTime += Time.deltaTime;
            if (BannerTimer > 0) BannerTimer -= Time.deltaTime;

            if (_waveActive && AliveEnemies == 0)
            {
                _waveActive = false;
                if (!Endless && WaveIndex + 1 >= mission.waves.Length)
                {
                    Win();
                    return;
                }
                _interWave = 2f;
                _gates?.MendGate(); // resting between waves mends one Gate
                if (_playerHealth != null && _playerT != null)
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
            var kinds = WaveIndex < mission.waves.Length
                ? mission.waves[WaveIndex].enemies
                : EndlessWave(WaveIndex + 1);
            var title = WaveIndex < mission.waves.Length
                ? mission.waves[WaveIndex].title
                : "THE MIST DEEPENS";
            Announce($"WAVE {WaveIndex + 1} — {title}");

            foreach (var kind in kinds)
            {
                var prefab = enemyPrefabs[(int)kind];
                if (prefab == null) continue;
                var edge = Random.Range(0, 4);
                var p = edge switch
                {
                    0 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, arenaHalfExtents.y - 0.5f),
                    1 => new Vector3(Random.Range(-arenaHalfExtents.x, arenaHalfExtents.x), 0, -arenaHalfExtents.y + 0.5f),
                    2 => new Vector3(-arenaHalfExtents.x + 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
                    _ => new Vector3(arenaHalfExtents.x - 0.5f, 0, Random.Range(-arenaHalfExtents.y, arenaHalfExtents.y)),
                };
                Instantiate(prefab, p, Quaternion.identity);
            }
        }

        /// <summary>Scaling composition for the post-victory Endless Trial.</summary>
        private static EnemyKind[] EndlessWave(int n)
        {
            var list = new System.Collections.Generic.List<EnemyKind>();
            var tier = n - 4; // waves past the authored mission
            for (var i = 0; i < Mathf.Min(7, 3 + tier / 2); i++) list.Add(EnemyKind.Bandit);
            for (var i = 0; i < Mathf.Min(4, 1 + tier / 3); i++) list.Add(EnemyKind.Ranged);
            for (var i = 0; i < Mathf.Min(3, tier / 3); i++) list.Add(EnemyKind.Shade);
            if (tier > 0 && tier % 4 == 0) list.Add(EnemyKind.Chief);
            return list.ToArray();
        }

        public void StartEndless()
        {
            if (State != Phase.Won) return;
            Endless = true;
            State = Phase.Playing;
            _playerHealth?.ResetFull();
            _interWave = 1.5f;
            Sfx3D.Ui();
            Announce("ENDLESS TRIAL — HOW LONG CAN THE LANTERN BURN?");
        }

        private void Win()
        {
            State = Phase.Won;
            Sfx3D.Win();
            var r = MissionResult();
            NewRecord = r.score > BestScore;
            if (NewRecord)
            {
                PlayerPrefs.SetInt("best_score", r.score);
                PlayerPrefs.SetString("best_rank", r.rank);
                PlayerPrefs.Save();
            }
        }

        private void OnPlayerDeath()
        {
            if (State is Phase.Won or Phase.Lost) return;
            State = Phase.Lost;
            Sfx3D.Lose();
            if (Endless && WaveIndex + 1 > BestWave)
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

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Same formula as the 2D prototype (tune together).</summary>
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
