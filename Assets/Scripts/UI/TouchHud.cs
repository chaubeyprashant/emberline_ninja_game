using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.UI
{
    /// <summary>
    /// v2 UI: main menu → Story level select (stars/locks) / Fight opponent
    /// select / Endless — plus briefings, duel HP bar, in-game HUD, and
    /// mode-aware result screens. All IMGUI with cached generated sprites.
    /// </summary>
    public class TouchHud : MonoBehaviour
    {
        private enum MenuScreen { Root, Story, Fight }

        private GameManager _gm;
        private Health _health;
        private SenGates _gates;
        private Player.CombatController _combat;
        private Camera _cam;
        private MenuScreen _menu = MenuScreen.Root;

        private int _stickFinger = -1;
        private Vector2 _stickOrigin, _stickPos;
        private bool _movedOnce, _struckOnce;

        private GUIStyle _label, _labelRight, _banner, _button, _rank, _hint, _hintCenter, _title, _small, _story;
        private static Texture2D _circle, _circleThin, _diamond;

        private void Start()
        {
            _gm = FindFirstObjectByType<GameManager>();
            _combat = FindFirstObjectByType<Player.CombatController>();
            _cam = Camera.main;
            if (_combat != null)
            {
                _health = _combat.GetComponent<Health>();
                _gates = _combat.GetComponent<SenGates>();
            }
        }

        private void Update()
        {
            if (_gm == null || _gm.State != GameManager.Phase.Playing) return;

            EmberInput.TouchActive = _stickFinger >= 0;
            foreach (var t in Input.touches)
            {
                if (t.phase == TouchPhase.Began && _stickFinger < 0
                    && t.position.x < Screen.width * 0.45f)
                {
                    _stickFinger = t.fingerId;
                    _stickOrigin = t.position;
                    _stickPos = t.position;
                }
                else if (t.fingerId == _stickFinger)
                {
                    if (t.phase is TouchPhase.Ended or TouchPhase.Canceled)
                    {
                        _stickFinger = -1;
                        EmberInput.TouchMove = Vector2.zero;
                        EmberInput.TouchActive = false;
                    }
                    else
                    {
                        _stickPos = t.position;
                        var delta = (t.position - _stickOrigin) / (Screen.dpi > 0 ? Screen.dpi * 0.4f : 160f);
                        EmberInput.TouchMove = Vector2.ClampMagnitude(delta, 1f);
                        EmberInput.TouchActive = true;
                        if (delta.sqrMagnitude > 0.1f) _movedOnce = true;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_gm == null) return;
            EnsureAssets();
            var s = Screen.height / 540f;

            switch (_gm.State)
            {
                case GameManager.Phase.Menu: DrawMenu(s); return;
                case GameManager.Phase.Intro: DrawBriefing(s); return;
            }

            DrawBars(s);
            DrawWorldMarkers(s);
            DrawTopRight(s);
            if (_gm.ModeNow == LaunchMode.Duel) DrawDuelBar(s);

            if (_gm.BannerTimer > 0)
                GUI.Label(new Rect(0, 96 * s, Screen.width, 40 * s), _gm.Banner, _banner);

            var obj = _gm.Objective;
            if (obj.Length > 0)
                GUI.Label(new Rect(24 * s, 88 * s, 500 * s, 22 * s), obj, _hint);

            // First-run hints only on story level 1.
            if (_gm.ModeNow == LaunchMode.Story && Session.LevelIndex == 0
                && _gm.MissionTime < 20f && (!_movedOnce || !_struckOnce)
                && _gm.State == GameManager.Phase.Playing)
            {
                var msg = !_movedOnce
                    ? "DRAG THE LEFT SIDE OF THE SCREEN TO MOVE"
                    : "TAP STRIKE WHEN AN ENEMY IS CLOSE";
                GUI.Label(new Rect(0, Screen.height - 190 * s, Screen.width, 24 * s), msg, _banner);
            }

            DrawStick(s);
            DrawCombatButtons(s);
            if (_gm.State is GameManager.Phase.Won or GameManager.Phase.Lost) DrawResult(s);
        }

        // ------------------------------------------------------------- menus

        private void DrawMenu(float s)
        {
            Dim(0.55f);
            switch (_menu)
            {
                case MenuScreen.Root: DrawMenuRoot(s); break;
                case MenuScreen.Story: DrawStorySelect(s); break;
                case MenuScreen.Fight: DrawFightSelect(s); break;
            }
        }

        private void DrawMenuRoot(float s)
        {
            GUI.Label(new Rect(0, Screen.height * 0.10f, Screen.width, 40 * s), "AN EMBERLINE STORY", _small);
            GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 70 * s), "THE NIGHT OF YORUNE", _title);

            var bw = 380 * s; var bh = 62 * s; var x = Screen.width / 2f - bw / 2f;
            if (GUI.Button(new Rect(x, Screen.height * 0.38f, bw, bh),
                    $"STORY   ·   {Session.StoryUnlocked - 1}/{Session.Story.Length} CLEARED", _button))
            { Sfx3D.Ui(); _menu = MenuScreen.Story; }
            if (GUI.Button(new Rect(x, Screen.height * 0.52f, bw, bh),
                    $"FIGHT   ·   DUELS", _button))
            { Sfx3D.Ui(); _menu = MenuScreen.Fight; }
            if (GUI.Button(new Rect(x, Screen.height * 0.66f, bw, bh),
                    _gm.BestWave > 0 ? $"ENDLESS TRIAL   ·   BEST WAVE {_gm.BestWave}" : "ENDLESS TRIAL", _button))
                _gm.LaunchEndless();

            GUI.Label(new Rect(0, Screen.height * 0.84f, Screen.width, 24 * s),
                $"★ {Session.TotalStars} / {Session.Story.Length * 3}", _hintCenter);
        }

        private void DrawStorySelect(float s)
        {
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 50 * s), "STORY — THE NIGHT OF YORUNE", _banner);
            GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 24 * s),
                "ACT I · THE ROOFTOPS          ACT II · ASHFEN MARSH", _hintCenter);

            var cols = 5;
            var bw = 150 * s; var bh = 96 * s; var gap = 18 * s;
            var x0 = Screen.width / 2f - (cols * bw + (cols - 1) * gap) / 2f;
            var y0 = Screen.height * 0.26f;
            for (var i = 0; i < Session.Story.Length; i++)
            {
                var col = i % cols; var row = i / cols;
                var r = new Rect(x0 + col * (bw + gap), y0 + row * (bh + gap * 1.6f), bw, bh);
                var level = Session.Story[i];
                var unlocked = level.id <= Session.StoryUnlocked;
                var stars = Session.Stars(level.id);
                var starTxt = stars > 0 ? new string('★', stars) + new string('☆', 3 - stars)
                    : unlocked ? "— — —" : "";
                var label = unlocked ? $"{level.id}\n{starTxt}" : $"{level.id}\nLOCKED";
                GUI.enabled = unlocked;
                if (GUI.Button(r, label, _button)) _gm.LaunchStory(i);
                GUI.enabled = true;
            }

            BackButton(s);
        }

        private void DrawFightSelect(float s)
        {
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 50 * s), "FIGHT — CHOOSE YOUR OPPONENT", _banner);

            var bw = 330 * s; var bh = 88 * s; var gap = 20 * s;
            var x0 = Screen.width / 2f - (2 * bw + gap) / 2f;
            var y0 = Screen.height * 0.24f;
            for (var i = 0; i < Session.Duels.Length; i++)
            {
                var col = i % 2; var row = i / 2;
                var r = new Rect(x0 + col * (bw + gap), y0 + row * (bh + gap), bw, bh);
                var duel = Session.Duels[i];
                var unlocked = duel.id <= Session.DuelsUnlocked;
                var won = Session.DuelWon(duel.id);
                var label = unlocked
                    ? $"{duel.name}{(won ? "  ✓" : "")}\n{duel.title}"
                    : "LOCKED\nDefeat the previous opponent";
                GUI.enabled = unlocked;
                if (GUI.Button(r, label, _button)) _gm.LaunchDuel(i);
                GUI.enabled = true;
            }

            GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 24 * s),
                "One life. Full strength. No mercy.", _hintCenter);
            BackButton(s);
        }

        private void BackButton(float s)
        {
            if (GUI.Button(new Rect(24 * s, Screen.height - 86 * s, 150 * s, 56 * s), "← BACK", _button))
            { Sfx3D.Ui(); _menu = MenuScreen.Root; }
        }

        // ---------------------------------------------------------- briefing

        private void DrawBriefing(float s)
        {
            Dim(0.9f);
            if (_gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null)
            {
                var d = _gm.CurrentDuel;
                GUI.Label(new Rect(0, Screen.height * 0.14f, Screen.width, 40 * s), "DUEL", _small);
                GUI.Label(new Rect(0, Screen.height * 0.21f, Screen.width, 70 * s), d.name, _title);
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 30 * s), d.title, _hintCenter);
                GUI.Label(new Rect(Screen.width * 0.18f, Screen.height * 0.46f, Screen.width * 0.64f, 80 * s),
                    d.taunt, _story);
                GUI.Label(new Rect(0, Screen.height * 0.62f, Screen.width, 26 * s),
                    "OBJECTIVE — Win the duel. One life each.", _hintCenter);
            }
            else if (_gm.ModeNow == LaunchMode.Endless)
            {
                GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 40 * s), "SURVIVAL", _small);
                GUI.Label(new Rect(0, Screen.height * 0.23f, Screen.width, 70 * s), "ENDLESS TRIAL", _title);
                GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 30 * s),
                    "The mist never runs out of raiders. How long can the lantern burn?", _hintCenter);
                if (_gm.BestWave > 0)
                    GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 26 * s),
                        $"BEST — WAVE {_gm.BestWave}", _hintCenter);
            }
            else if (_gm.CurrentLevel != null)
            {
                var l = _gm.CurrentLevel;
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 40 * s),
                    $"LEVEL {l.id}  ·  {(l.marsh ? "ACT II — ASHFEN MARSH" : "ACT I — THE ROOFTOPS")}", _small);
                GUI.Label(new Rect(0, Screen.height * 0.19f, Screen.width, 70 * s), l.name, _title);
                GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.36f, Screen.width * 0.7f, 90 * s),
                    l.story, _story);
                GUI.Label(new Rect(0, Screen.height * 0.56f, Screen.width, 26 * s),
                    $"OBJECTIVE — Clear {l.waves.Length} wave{(l.waves.Length > 1 ? "s" : "")}. ★★★ for rank A or higher.", _hintCenter);
            }

            var bw = 280 * s;
            if (GUI.Button(new Rect(Screen.width / 2f - bw - 14 * s, Screen.height * 0.74f, bw, 62 * s), "BEGIN", _button))
                _gm.BeginMission();
            if (GUI.Button(new Rect(Screen.width / 2f + 14 * s, Screen.height * 0.74f, bw, 62 * s), "MENU", _button))
                _gm.OpenMenu();
        }

        // ------------------------------------------------------------ combat

        private void DrawDuelBar(float s)
        {
            var opp = _gm.DuelOpponent;
            if (opp == null || _gm.CurrentDuel == null) return;
            var w = 520 * s;
            var r = new Rect(Screen.width / 2f - w / 2f, 34 * s, w, 14 * s);
            GUI.Label(new Rect(r.x, 8 * s, w, 22 * s), _gm.CurrentDuel.name, _hintCenter);
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.42f, 0.29f);
            GUI.DrawTexture(new Rect(r.x, r.y, w * Mathf.Clamp01(opp.Hp / opp.maxHp), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawBars(float s)
        {
            if (_health != null)
            {
                Bar(new Rect(24 * s, 24 * s, 250 * s, 13 * s), _health.Hp / _health.MaxHp,
                    new Color(0.88f, 0.33f, 0.27f));
                GUI.Label(new Rect(24 * s, 6 * s, 200 * s, 18 * s), "LIFE", _label);
            }
            if (_gates != null)
            {
                Bar(new Rect(24 * s, 48 * s, 250 * s * (_gates.MaxSen / 100f), 10 * s),
                    _gates.Sen / _gates.MaxSen, new Color(0.5f, 0.7f, 0.77f));
                GUI.Label(new Rect(24 * s, 60 * s, 90 * s, 18 * s), "SEN", _label);
                for (var i = 0; i < 5; i++)
                {
                    var cracked = i >= 5 - _gates.CrackedGates;
                    GUI.color = cracked ? new Color(1f, 0.45f, 0.28f, 0.95f)
                        : new Color(0.9f, 0.89f, 0.86f, 0.85f);
                    GUI.DrawTexture(new Rect((86 + i * 26) * s, 61 * s, 16 * s, 16 * s), _diamond);
                }
                GUI.color = Color.white;
            }
        }

        private void DrawTopRight(float s)
        {
            var t = (int)_gm.MissionTime;
            var waveLabel = _gm.ModeNow switch
            {
                LaunchMode.Endless => $"ENDLESS — WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}",
                LaunchMode.Duel => "DUEL",
                _ => $"WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}/{_gm.WaveCount}",
            };
            GUI.Label(new Rect(Screen.width - 300 * s, 10 * s, 276 * s, 24 * s),
                $"{waveLabel}   {t / 60}:{t % 60:00}", _labelRight);
            if (_combat != null && _combat.Combo > 1)
                GUI.Label(new Rect(Screen.width - 300 * s, 34 * s, 276 * s, 30 * s),
                    $"{_combat.Combo} THREAD", _labelRight);
        }

        private void DrawWorldMarkers(float s)
        {
            if (_cam == null) return;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead) continue;
                var world = e.transform.position + Vector3.up * 2.4f;
                var sp = _cam.WorldToScreenPoint(world);
                var onScreen = sp.z > 0 && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;

                if (onScreen)
                {
                    var w = 52f * s;
                    var r = new Rect(sp.x - w / 2, Screen.height - sp.y, w, 5f * s);
                    GUI.color = new Color(0, 0, 0, 0.5f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 0.42f, 0.29f);
                    GUI.DrawTexture(new Rect(r.x, r.y, w * Mathf.Clamp01(e.Hp / e.maxHp), r.height),
                        Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                else
                {
                    var p = new Vector2(sp.x, Screen.height - sp.y);
                    if (sp.z < 0) p = new Vector2(Screen.width - p.x, Screen.height - 30 * s);
                    p.x = Mathf.Clamp(p.x, 26 * s, Screen.width - 26 * s);
                    p.y = Mathf.Clamp(p.y, 26 * s, Screen.height - 26 * s);
                    GUI.color = new Color(1f, 0.35f, 0.25f, 0.9f);
                    GUI.DrawTexture(new Rect(p.x - 8 * s, p.y - 8 * s, 16 * s, 16 * s), _diamond);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawStick(float s)
        {
            if (_stickFinger < 0) return;
            var o = new Vector2(_stickOrigin.x, Screen.height - _stickOrigin.y);
            var pRaw = new Vector2(_stickPos.x, Screen.height - _stickPos.y);
            var p = o + Vector2.ClampMagnitude(pRaw - o, 52 * s);
            GUI.color = new Color(0.9f, 0.89f, 0.86f, 0.15f);
            GUI.DrawTexture(CenterRect(o, 120 * s), _circleThin);
            GUI.color = new Color(0.9f, 0.89f, 0.86f, 0.55f);
            GUI.DrawTexture(CenterRect(p, 52 * s), _circle);
            GUI.color = Color.white;
        }

        private void DrawCombatButtons(float s)
        {
            var bx = Screen.width - 120 * s;
            var by = Screen.height - 120 * s;
            CircleButton(new Rect(bx, by, 100 * s, 100 * s), "STRIKE",
                new Color(0.5f, 0.7f, 0.77f), true, () => { EmberInput.PressStrike(); _struckOnce = true; });
            CircleButton(new Rect(bx - 112 * s, by + 22 * s, 78 * s, 78 * s), "CLEAVE",
                new Color(0.24f, 0.42f, 0.49f), true, () => { EmberInput.PressCleave(); _struckOnce = true; });
            CircleButton(new Rect(bx + 8 * s, by - 96 * s, 78 * s, 78 * s), "FLICKER",
                new Color(0.59f, 0.63f, 0.67f), true, EmberInput.PressFlicker);
            CircleButton(new Rect(bx - 100 * s, by - 84 * s, 70 * s, 70 * s), "SURGE",
                new Color(1f, 0.42f, 0.29f), _gates != null && _gates.Sen >= SenGates.SurgeCost,
                EmberInput.PressSurge);
        }

        // ------------------------------------------------------------ results

        private void DrawResult(float s)
        {
            Dim(0.92f);
            if (_gm.State == GameManager.Phase.Won)
            {
                var r = _gm.MissionResult();
                if (_gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null)
                {
                    GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 60 * s), "VICTORY", _title);
                    GUI.Label(new Rect(0, Screen.height * 0.33f, Screen.width, 30 * s),
                        $"{_gm.CurrentDuel.name} FALLS   ·   {(int)_gm.MissionTime}s", _banner);
                    var bw2 = 250 * s;
                    if (GUI.Button(new Rect(Screen.width / 2f - bw2 * 1.5f - 20 * s, Screen.height * 0.62f, bw2, 56 * s), "NEXT OPPONENT", _button))
                        _gm.NextDuel();
                    if (GUI.Button(new Rect(Screen.width / 2f - bw2 / 2f, Screen.height * 0.62f, bw2, 56 * s), "REMATCH", _button))
                        _gm.Retry();
                    if (GUI.Button(new Rect(Screen.width / 2f + bw2 * 0.5f + 20 * s, Screen.height * 0.62f, bw2, 56 * s), "MENU", _button))
                        _gm.OpenMenu();
                    return;
                }

                GUI.Label(new Rect(0, Screen.height * 0.13f, Screen.width, 60 * s), "LEVEL CLEAR", _banner);
                var stars = _gm.StarsEarned;
                GUI.Label(new Rect(0, Screen.height * 0.20f, Screen.width, 110 * s),
                    new string('★', stars) + new string('☆', 3 - stars), _rank);
                GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 30 * s),
                    $"RANK {r.rank}   ·   SCORE {r.score}   ·   DMG {Mathf.RoundToInt(_gm.DamageTaken)}   ·   MAX THREAD {(_combat != null ? _combat.MaxCombo : 0)}",
                    _banner);
                if (_gm.CurrentLevel != null && Session.LevelIndex + 1 < Session.Story.Length)
                    GUI.Label(new Rect(0, Screen.height * 0.53f, Screen.width, 26 * s),
                        $"NEXT — {Session.Story[Session.LevelIndex + 1].name}", _hintCenter);

                var bw = 250 * s;
                if (GUI.Button(new Rect(Screen.width / 2f - bw * 1.5f - 20 * s, Screen.height * 0.66f, bw, 56 * s), "NEXT LEVEL", _button))
                    _gm.NextStoryLevel();
                if (GUI.Button(new Rect(Screen.width / 2f - bw / 2f, Screen.height * 0.66f, bw, 56 * s), "REPLAY", _button))
                    _gm.Retry();
                if (GUI.Button(new Rect(Screen.width / 2f + bw * 0.5f + 20 * s, Screen.height * 0.66f, bw, 56 * s), "MENU", _button))
                    _gm.OpenMenu();
            }
            else
            {
                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 60 * s), "THE LANTERN GUTTERS…", _title);
                GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 30 * s), "but it does not go out.", _banner);
                if (_gm.ModeNow == LaunchMode.Endless)
                    GUI.Label(new Rect(0, Screen.height * 0.43f, Screen.width, 26 * s),
                        $"REACHED WAVE {_gm.WaveIndex + 1}   ·   BEST WAVE {_gm.BestWave}" +
                        (_gm.NewRecord ? "   ·   ★ NEW RECORD ★" : ""), _hintCenter);
                var bw = 250 * s;
                if (GUI.Button(new Rect(Screen.width / 2f - bw - 14 * s, Screen.height * 0.60f, bw, 56 * s), "RISE AGAIN", _button))
                    _gm.Retry();
                if (GUI.Button(new Rect(Screen.width / 2f + 14 * s, Screen.height * 0.60f, bw, 56 * s), "MENU", _button))
                    _gm.OpenMenu();
            }
        }

        // ------------------------------------------------------------ helpers

        private void Dim(float a)
        {
            GUI.color = new Color(0.055f, 0.07f, 0.1f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void CircleButton(Rect r, string label, Color color, bool enabled, System.Action onPress)
        {
            GUI.color = new Color(color.r, color.g, color.b, enabled ? 0.30f : 0.10f);
            GUI.DrawTexture(r, _circle);
            GUI.color = new Color(color.r, color.g, color.b, enabled ? 0.95f : 0.3f);
            GUI.DrawTexture(r, _circleThin);
            GUI.color = Color.white;
            var st = new Rect(r.x, r.y + r.height * 0.36f, r.width, r.height * 0.3f);
            GUI.Label(st, label, _hintCenter);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) onPress();
        }

        private static Rect CenterRect(Vector2 c, float d) => new(c.x - d / 2, c.y - d / 2, d, d);

        private static void Bar(Rect r, float frac, Color color)
        {
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureAssets()
        {
            if (_label != null) return;
            var s = Screen.height / 540f;

            _circle = MakeCircle(128, filled: true);
            _circleThin = MakeCircle(128, filled: false);
            _diamond = MakeDiamond(64);

            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13 * s),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.79f, 0.83f) },
            };
            _labelRight = new GUIStyle(_label) { alignment = TextAnchor.UpperRight };
            _banner = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(21 * s),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.89f, 0.86f) },
            };
            _title = new GUIStyle(_banner)
            {
                fontSize = Mathf.RoundToInt(42 * s),
                normal = { textColor = new Color(0.93f, 0.92f, 0.89f) },
            };
            _rank = new GUIStyle(_banner)
            {
                fontSize = Mathf.RoundToInt(76 * s),
                normal = { textColor = new Color(1f, 0.42f, 0.29f) },
            };
            _hint = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(13 * s),
                normal = { textColor = new Color(1f, 0.62f, 0.45f) },
            };
            _hintCenter = new GUIStyle(_hint) { alignment = TextAnchor.MiddleCenter };
            _small = new GUIStyle(_banner)
            {
                fontSize = Mathf.RoundToInt(14 * s),
                normal = { textColor = new Color(1f, 0.42f, 0.29f) },
            };
            _story = new GUIStyle(_banner)
            {
                fontSize = Mathf.RoundToInt(17 * s),
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.84f, 0.86f) },
            };
            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15 * s),
                fontStyle = FontStyle.Bold,
            };
        }

        private static Texture2D MakeCircle(int size, bool filled)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var half = size / 2f;
            var ring = size * 0.045f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                float a;
                if (filled) a = Mathf.Clamp01(half - 1f - d);
                else a = Mathf.Clamp01(Mathf.Min(half - 1f - d, d - (half - ring)));
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeDiamond(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var half = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Abs(x + 0.5f - half) + Mathf.Abs(y + 0.5f - half);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(half - 2f - d)));
            }
            tex.Apply();
            return tex;
        }
    }
}
