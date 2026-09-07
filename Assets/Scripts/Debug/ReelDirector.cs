#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Captures real gameplay as a portrait frame sequence for marketing cuts.
    ///
    /// The game ships landscape (the HUD is authored at 1600x720), so a 9:16
    /// reel cannot just be a crop of the shipping frame — there is not enough
    /// picture above and below the fight. Instead the *real* game camera drives
    /// a second camera that renders the same scene into a 1080x1920 target: same
    /// position, same rotation, same grade, same fight, framed tall. Nothing is
    /// staged that the game cannot stage — enemies come through GameManager's
    /// own spawn path, and the bot plays with the same input hub the touch HUD
    /// writes into.
    ///
    /// Time is stepped by Time.captureDeltaTime so a frame that takes 200 ms to
    /// read back and encode still advances the game by exactly 1/60 s. The
    /// footage plays back at real speed no matter how slowly it was rendered.
    ///
    /// Alongside the frames each shot writes events.csv: what the fight was
    /// doing on every frame (combo, parries, executions, deaths, telegraphs),
    /// which is what the edit and the sound pass are cut against.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class ReelDirector : MonoBehaviour
    {
        public System.Action<int> onFinished;

        /// <summary>Capture only this shot index, or -1 for the whole list.</summary>
        public static int OnlyShot = -1;

        /// <summary>Seconds of footage per shot, overriding the shot's own length.</summary>
        public static float SecondsOverride = -1f;

        public static string OutRoot = "Logs/reel";

        private const int Width = 1080, Height = 1920, Fps = 60;

        /// <summary>How the bot fights — each shot wants a different performance.</summary>
        private enum Style
        {
            Aggro,      // close the distance and keep the combo alive
            Defensive,  // hold the guard, parry the telegraph, dodge the rest
            Boss,       // stay in the boss's face, read the big wind-ups
            Walk,       // no fight: just carry Renzo through the shot
        }

        private struct Shot
        {
            public string id;
            public int level;              // index into Session.Story (mission id - 1)
            public EnemyKind[] foes;
            public string namedFoe;        // EnemyDef id for a named character, or ""
            public float seconds;
            public Style style;
            public float pullback;         // metres further back than the game camera
            public float rise;             // metres above the game camera
            public float fov;              // vertical FOV for the portrait frame
            public float headroom;         // metres the aim sits above the fighters
        }

        // Five fights in four of the campaign's authored environments, picked for
        // what each beat of the cut needs. Mission indices are ids minus one.
        private static readonly Shot[] Shots =
        {
            // Six fights chosen for what each beat of the cut needs and for how
            // different they look from one another — the campaign's themes are
            // where the game's variety actually lives, so the reel travels
            // through five of them. Mission indices are ids minus one.
            //
            // Hook: the burning village. Firelight against night is the highest
            // contrast the game has, which is what a one-second hook needs.
            new() { id = "01_hook", level = 52, foes = new[] { EnemyKind.Samurai },
                    seconds = 16f, style = Style.Boss, pullback = 1.7f, rise = 0.4f, fov = 64f, headroom = 0.35f },
            // Blade work: snow on the mountain road — a cold, clean plate.
            new() { id = "02_blade", level = 22, foes = new[] { EnemyKind.Bandit, EnemyKind.RaiderAxe },
                    seconds = 16f, style = Style.Aggro, pullback = 1.7f, rise = 0.4f, fov = 64f, headroom = 0.35f },
            // Timing: the marsh, which is a different scene rather than a
            // different dressing, and the one that reads least like the others.
            new() { id = "03_timing", level = 43, foes = new[] { EnemyKind.Samurai, EnemyKind.Assassin },
                    seconds = 16f, style = Style.Defensive, pullback = 1.6f, rise = 0.35f, fov = 63f, headroom = 0.3f },
            // The boss: Jin Kurogane in the rain, on the mission named for his
            // sword. Mission 70 is his own fight but plays out at night, and a
            // boss nobody can see is not a boss shot.
            new() { id = "04_boss_jin", level = 60, foes = new[] { EnemyKind.Jin },
                    seconds = 16f, style = Style.Boss, pullback = 1.9f, rise = 0.45f, fov = 65f, headroom = 0.4f },
            // The finish: Goro in the mountain fortress, the biggest body in the
            // cast and the one whose death reads at a glance.
            new() { id = "05_finish_goro", level = 39, foes = new[] { EnemyKind.Chief },
                    seconds = 18f, style = Style.Boss, pullback = 1.9f, rise = 0.45f, fov = 65f, headroom = 0.4f },
            // End card: back to the burning village with nobody left to fight —
            // Renzo simply walks, and the embers carry the logo.
            new() { id = "06_outro", level = 52, foes = new EnemyKind[0],
                    seconds = 9f, style = Style.Walk, pullback = 2.3f, rise = 0.5f, fov = 66f, headroom = 0.45f },
        };

        private enum Phase { Loading, Intro, Warmup, Capturing, Done }

        private Phase _phase = Phase.Loading;
        private int _shot = -1;
        private float _t, _healT, _strikeT, _guardT, _repathT, _cineWait, _respawnT, _aimHold;
        private int _frame;
        private Vector3 _strafe = Vector3.right;
        private Vector3 _aim;
        private bool _aimed;
        private EnemyBrain _aimFoe;

        private GameManager _gm;
        private Player.PlayerLocomotion _loco;
        private Player.CombatController _combat;
        private Health _hp;

        private Camera _cap;
        private RenderTexture _rt;
        private Texture2D _read;
        private string _dir;
        private StringBuilder _csv;

        // Edge detection for the event track.
        private int _lastCombo, _lastDeflects, _lastKills;
        private bool _lastExecute, _lastTelegraph, _lastDodge;

        private void Start()
        {
            AudioListener.volume = 0f;
            AudioListener.pause = true;
            DontDestroyOnLoad(gameObject);
            Time.captureDeltaTime = 1f / Fps;

            _rt = new RenderTexture(Width, Height, 24) { antiAliasing = 1 };
            _read = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var go = new GameObject("ReelCaptureCam");
            DontDestroyOnLoad(go);
            _cap = go.AddComponent<Camera>();
            _cap.enabled = false;                 // rendered by hand, once per frame
            go.AddComponent<UI.CinematicGrade>();

            NextShot();
        }

        // ------------------------------------------------------------- shots

        private void NextShot()
        {
            FlushCsv();
            _shot++;
            if (OnlyShot >= 0 && _shot != OnlyShot)
            {
                if (_shot > OnlyShot) { Finish(); return; }
                _shot = OnlyShot;
            }
            if (_shot >= Shots.Length) { Finish(); return; }

            var s = Shots[_shot];
            _dir = Path.Combine(OutRoot, s.id);
            Directory.CreateDirectory(_dir);
            _csv = new StringBuilder("frame,t,dt,unscaledDt,timeScale,combo,deflects,alive,bossHp01,state,events\n");
            _frame = 0;
            _t = 0f;
            _lastCombo = _lastDeflects = _lastKills = 0;
            _lastExecute = _lastTelegraph = _lastDodge = false;
            _aimed = false;
            _cineWait = 0f;
            _aimFoe = null;
            _respawnT = 0f;

            // A checkpoint from an earlier run would drop us mid-mission.
            for (var i = 1; i <= Session.Story.Length + 12; i++) Checkpoints.Clear(i);
            Session.Mode = LaunchMode.Story;
            var gm = SceneRefs.Game;
            if (gm == null) { Debug.LogError("[REEL] no GameManager"); Finish(2); return; }
            Debug.Log($"[REEL] shot {_shot} {s.id}: mission {Session.Story[s.level].id} " +
                      $"{Session.Story[s.level].name}");
            gm.LaunchStory(s.level);
            _phase = Phase.Loading;
        }

        private void Finish(int code = 0)
        {
            FlushCsv();
            _phase = Phase.Done;
            Time.captureDeltaTime = 0f;
            EmberInput.Scripted = null;
            Debug.Log("[REEL] capture complete");
            onFinished?.Invoke(code);
            enabled = false;
        }

        private void FlushCsv()
        {
            if (_csv == null || _dir == null) return;
            File.WriteAllText(Path.Combine(_dir, "events.csv"), _csv.ToString());
            Debug.Log($"[REEL] {_dir}: {_frame} frames");
            _csv = null;
        }

        // -------------------------------------------------------------- loop

        private void Update()
        {
            if (_phase == Phase.Done) return;
            var s = Shots[_shot];

            if (_phase == Phase.Loading)
            {
                var gm = SceneRefs.Game;
                if (gm == null || gm.State == GameManager.Phase.Menu) return;
                _gm = gm;
                _loco = SceneRefs.Motor;
                _combat = _loco != null ? _loco.GetComponent<Player.CombatController>() : null;
                _hp = _gm.PlayerHealth;
                if (_hp != null) _hp.SetMax(100000f);
                _phase = Phase.Intro;
                return;
            }

            if (_phase == Phase.Intro)
            {
                if (_gm.State == GameManager.Phase.Intro) { _gm.BeginMission(); return; }
                if (_gm.State != GameManager.Phase.Playing) return;
                // The arena is dressed and lit; clear the mission's own wave and
                // stage the fight this shot was written for.
                for (var i = EnemyBrain.Active.Count - 1; i >= 0; i--)
                    EnemyPool.Release(EnemyBrain.Active[i]);
                // The objective disc and its marker are mission furniture: a
                // metre-wide pulsing decal under a duel is a HUD element wearing
                // a mesh, and it reads as an artefact in a cut.
                foreach (var pulse in FindObjectsByType<ObjectivePulse>(FindObjectsSortMode.None))
                    pulse.gameObject.SetActive(false);
                StageFoes(s);
                _phase = Phase.Warmup;
                _t = 0f;
                return;
            }

            // Renzo never dies mid-take: a downed player ends the mission and the
            // shot with it. The fight itself is untouched.
            if (_hp != null && (_healT -= Time.deltaTime) <= 0f) { _healT = 0.3f; _hp.Heal(100000f); }

            _t += Time.deltaTime;

            if (_phase == Phase.Warmup)
            {
                // Long enough for the spawn animations to finish and the enemies
                // to close, so the first captured frame is already a fight.
                // A mission that opens on a scripted beat is worth waiting out
                // — but not forever, or one authored cutscene stalls the run.
                if (GameManager.CinematicActive && (_cineWait += Time.deltaTime) < 12f) { _t = 0f; return; }
                if (_t >= 1.6f) { _phase = Phase.Capturing; _t = 0f; }
                return;
            }

            // Keep the shot populated, but let a kill land first: a body that
            // is still falling while its replacement sprints in is unusable.
            if (s.foes.Length > 0 && AliveCount() == 0)
            {
                if ((_respawnT -= Time.deltaTime) <= 0f) { StageFoes(s); _respawnT = 3.0f; }
            }
            else _respawnT = 3.0f;

            var length = SecondsOverride > 0f ? SecondsOverride : s.seconds;
            if (_t >= length) NextShot();
        }

        private void StageFoes(Shot s)
        {
            if (_gm == null) return;
            foreach (var k in s.foes)
            {
                if (!string.IsNullOrEmpty(s.namedFoe)) _gm.SpawnNamed(k, s.namedFoe);
                else _gm.SpawnOne(k, false);
            }
        }

        private static int AliveCount()
        {
            var n = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
                if (EnemyBrain.Active[i] != null && !EnemyBrain.Active[i].Dead) n++;
            return n;
        }

        // --------------------------------------------------------------- bot

        private void LateUpdate()
        {
            if (_phase is Phase.Done or Phase.Loading or Phase.Intro) return;
            DriveBot(Shots[_shot]);
            if (_phase != Phase.Capturing) return;
            HideWorldHud();
            Grab(Shots[_shot]);
        }

        /// <summary>
        /// Two things in the world are really HUD, and both were authored for a
        /// 1600x720 frame: the objective decal, and the wide floating banners
        /// ("GUARD BROKEN"). At portrait scale the banners span the picture and
        /// read as an overlay bug. The damage numbers stay — they are three
        /// characters wide and they are the readout that says this is a game.
        /// </summary>
        private static void HideWorldHud()
        {
            foreach (var pulse in FindObjectsByType<ObjectivePulse>(FindObjectsSortMode.None))
            {
                var r = pulse.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
            foreach (var ft in FindObjectsByType<UI.FloatingText>(FindObjectsSortMode.None))
            {
                var mesh = ft.GetComponent<TextMesh>();
                var r = ft.GetComponent<MeshRenderer>();
                if (mesh != null && r != null) r.enabled = mesh.text.Length <= 3;
            }
        }

        private EnemyBrain Nearest(out float dist)
        {
            dist = 999f;
            EnemyBrain best = null;
            if (_loco == null) return null;
            var p = _loco.transform.position;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e == null || e.Dead) continue;
                var d = Vector3.Distance(p, e.transform.position);
                if (d >= dist) continue;
                dist = d;
                best = e;
            }
            return best;
        }

        /// <summary>
        /// Plays Renzo through the same input hub the touch HUD writes into, so
        /// every swing, parry and dodge on screen is the shipping combat code
        /// reacting to a button press.
        /// </summary>
        private void DriveBot(Shot s)
        {
            if (_loco == null || _combat == null) return;
            EmberInput.Scripted = Vector2.zero;
            EmberInput.SetCleaveHeld(false);
            if (GameManager.CinematicActive) return;

            if (s.style == Style.Walk)
            {
                // The end card is a hero shot, not a fight: hold a slow walk so
                // the camera has motion under the logo.
                EmberInput.Scripted = new Vector2(0f, 0.30f);
                return;
            }

            var target = Nearest(out var dist);
            if (target == null) return;

            var to = target.transform.position - _loco.transform.position;
            to.y = 0f;
            var face = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
            _loco.SetFacing(face);

            // Read the wind-up. Defensive shots meet it with the guard; the rest
            // step through it — both are real mechanics on a real telegraph.
            var incoming = target.InWindupOrDash && target.WindupRemaining < 0.22f && dist < 3.4f;
            if (incoming)
            {
                if (s.style == Style.Defensive && _loco.FlickerCooldownRemaining > 0f)
                {
                    EmberInput.SetCleaveHeld(true);
                    _guardT = 0.35f;
                    return;
                }
                if (_loco.FlickerCooldownRemaining <= 0f)
                {
                    EmberInput.Scripted = new Vector2(-face.z, face.x) * 0.9f;  // sidestep
                    EmberInput.PressFlicker();
                    return;
                }
            }
            if ((_guardT -= Time.deltaTime) > 0f)
            {
                EmberInput.SetCleaveHeld(true);
                return;
            }

            var reach = s.style == Style.Boss ? 2.1f : 1.9f;
            if (dist > reach)
            {
                // Approach with a bit of an arc: a straight line into the enemy
                // reads as a bot, a circling closer reads as a player.
                if ((_repathT -= Time.deltaTime) <= 0f)
                {
                    _repathT = 1.1f;
                    _strafe = Random.value < 0.5f ? Vector3.right : Vector3.left;
                }
                var side = Vector3.Cross(Vector3.up, face) * (_strafe == Vector3.right ? 1f : -1f);
                var move = (face + side * (dist > 5f ? 0.15f : 0.45f)).normalized;
                EmberInput.Scripted = new Vector2(move.x, move.z);
                return;
            }

            if ((_strikeT -= Time.deltaTime) > 0f) return;
            // A steady beat of light strikes with the occasional heavy: the heavy
            // is what breaks posture, and broken posture is what opens executions.
            if (_combat.CleaveCd01 <= 0f && Random.value < 0.35f)
            {
                EmberInput.PressCleave();
                _strikeT = 0.55f;
            }
            else
            {
                EmberInput.PressStrike();
                _strikeT = s.style == Style.Aggro ? 0.24f : 0.3f;
            }
        }

        // ----------------------------------------------------------- capture

        private void Grab(Shot s)
        {
            var main = SceneRefs.Cam;
            if (main == null) return;

            _cap.CopyFrom(main);
            _cap.enabled = false;
            _cap.targetTexture = _rt;
            _cap.aspect = (float)Width / Height;
            _cap.fieldOfView = s.fov;
            // Same eye as the game — over Renzo's shoulder, where the rig put
            // it — but aimed at the duel rather than down the game's landscape
            // axis. A 9:16 frame that keeps the landscape pitch is half sky, and
            // the extra headroom drops the fighters onto the lower third where
            // the title cards leave room for them.
            var t = main.transform;
            var pos = t.position - t.forward * s.pullback + Vector3.up * s.rise;
            // Hold the frame on whoever Renzo is fighting — and for a beat
            // after they go down, so a finisher is not cut off by the camera
            // whipping round to the next spawn.
            if (_aimFoe == null || _aimFoe.Dead)
            {
                _aimHold += Time.deltaTime;
                if (_aimFoe == null || _aimHold > 1.4f) { _aimFoe = Nearest(out _); _aimHold = 0f; }
            }
            else _aimHold = 0f;

            var mid = _loco != null ? _loco.transform.position : t.position + t.forward * 4f;
            if (_aimFoe != null && _loco != null)
            {
                var gap = Vector3.Distance(_loco.transform.position, _aimFoe.transform.position);
                // Past six metres the two of them no longer share a portrait
                // frame, so the shot belongs to Renzo until he closes again.
                if (gap < 6f)
                    mid = Vector3.Lerp(_loco.transform.position, _aimFoe.transform.position, 0.5f);
            }
            mid += Vector3.up * (1.15f + s.headroom);
            _aim = _aimed ? Vector3.Lerp(_aim, mid, 1f - Mathf.Exp(-9f * Time.deltaTime)) : mid;
            _aimed = true;
            _cap.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_aim - pos, Vector3.up));

            _cap.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _read.Apply(false);
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_dir, $"f{_frame:0000}.jpg"), _read.EncodeToJPG(94));

            Row(s);
            _frame++;
        }

        /// <summary>One line per frame: everything the edit and the sound pass need.</summary>
        private void Row(Shot s)
        {
            var events = new List<string>();
            var combo = _combat != null ? _combat.Combo : 0;
            var deflects = _combat != null ? _combat.Deflects : 0;
            if (combo > _lastCombo) events.Add("hit");
            if (deflects > _lastDeflects) events.Add("parry");
            _lastCombo = combo;
            _lastDeflects = deflects;

            var kills = _gm != null ? _gm.Kills : 0;
            if (kills > _lastKills) events.Add(s.style == Style.Boss ? "bossdown" : "kill");
            _lastKills = kills;

            var state = _combat != null ? _combat.State.ToString() : "None";
            var exec = _combat != null && _combat.State == Player.CombatState.Execute;
            if (exec && !_lastExecute) events.Add("execute");
            _lastExecute = exec;

            var dodging = _loco != null && _loco.Invulnerable;
            if (dodging && !_lastDodge) events.Add("dodge");
            _lastDodge = dodging;

            var boss = Nearest(out _);
            var telegraph = boss != null && boss.InWindupOrDash;
            if (telegraph && !_lastTelegraph) events.Add("telegraph");
            _lastTelegraph = telegraph;

            if (Time.timeScale < 0.9f) events.Add("slowmo");

            var bossHp = boss != null && boss.maxHp > 0f ? boss.Hp / boss.maxHp : 0f;
            var c = CultureInfo.InvariantCulture;
            _csv.Append(_frame).Append(',')
                .Append(_t.ToString("0.0000", c)).Append(',')
                .Append(Time.deltaTime.ToString("0.00000", c)).Append(',')
                .Append(Time.unscaledDeltaTime.ToString("0.00000", c)).Append(',')
                .Append(Time.timeScale.ToString("0.000", c)).Append(',')
                .Append(combo).Append(',')
                .Append(deflects).Append(',')
                .Append(AliveCount()).Append(',')
                .Append(bossHp.ToString("0.000", c)).Append(',')
                .Append(state).Append(',')
                .Append(string.Join(";", events)).Append('\n');
        }
    }
}
#endif
