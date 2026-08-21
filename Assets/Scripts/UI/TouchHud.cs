using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.UI
{
    /// <summary>
    /// Touch controls + screen-space UI, polished pass: generated circle sprites,
    /// a visible virtual stick, Gate pips, mission select, and zero per-frame
    /// allocations (all styles/textures cached).
    /// </summary>
    public class TouchHud : MonoBehaviour
    {
        private GameManager _gm;
        private Health _health;
        private SenGates _gates;
        private Player.CombatController _combat;
        private Camera _cam;

        private int _stickFinger = -1;
        private Vector2 _stickOrigin, _stickPos;
        private bool _movedOnce, _struckOnce;

        private GUIStyle _label, _labelRight, _banner, _button, _rank, _hint, _hintCenter, _title, _small;
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

            if (_gm.State == GameManager.Phase.Intro) { DrawIntro(s); return; }

            DrawBars(s);
            DrawWorldMarkers(s);
            DrawTopRight(s);

            if (_gm.BannerTimer > 0)
                GUI.Label(new Rect(0, 90 * s, Screen.width, 40 * s), _gm.Banner, _banner);

            var obj = _gm.Objective;
            if (obj.Length > 0)
                GUI.Label(new Rect(24 * s, 88 * s, 500 * s, 22 * s), obj, _hint);

            if (_gm.MissionTime < 20f && (!_movedOnce || !_struckOnce)
                && _gm.State == GameManager.Phase.Playing)
            {
                var msg = !_movedOnce
                    ? "DRAG THE LEFT SIDE OF THE SCREEN TO MOVE"
                    : "TAP STRIKE WHEN AN ENEMY IS CLOSE";
                GUI.Label(new Rect(0, Screen.height - 190 * s, Screen.width, 24 * s), msg, _banner);
            }

            DrawStick(s);
            DrawCombatButtons(s);
            if (_gm.State != GameManager.Phase.Playing) DrawResult(s);
        }

        // ------------------------------------------------------------ pieces

        private void DrawIntro(float s)
        {
            GUI.color = new Color(0.055f, 0.07f, 0.1f, 0.93f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(0, Screen.height * 0.11f, Screen.width, 40 * s), "EMBERLINE", _small);
            GUI.Label(new Rect(0, Screen.height * 0.17f, Screen.width, 60 * s),
                _gm.mission != null ? _gm.mission.missionName : "MISSION", _title);
            GUI.Label(new Rect(0, Screen.height * 0.31f, Screen.width, 30 * s),
                _gm.mission != null ? _gm.mission.subtitle : "", _banner);
            GUI.Label(new Rect(0, Screen.height * 0.40f, Screen.width, 30 * s),
                $"OBJECTIVE — Survive all {_gm.WaveCount} waves. Defeat every enemy.", _hintCenter);

            var y = Screen.height * 0.50f;
            string[] lines =
            {
                "MOVE — drag the left side of the screen",
                "STRIKE — fast combo   ·   CLEAVE — slow, crushing",
                "FLICKER — dodge through attacks   ·   SURGE — ember nova (cracks a Gate)",
            };
            foreach (var line in lines)
            {
                GUI.Label(new Rect(0, y, Screen.width, 24 * s), line, _hintCenter);
                y += 28 * s;
            }

            var bw = 300 * s;
            if (GUI.Button(new Rect(Screen.width / 2f - bw - 14 * s, Screen.height * 0.74f, bw, 62 * s),
                    "BEGIN MISSION", _button))
            { Sfx3D.Ui(); _gm.BeginMission(); }
            if (!string.IsNullOrEmpty(_gm.otherSceneName)
                && GUI.Button(new Rect(Screen.width / 2f + 14 * s, Screen.height * 0.74f, bw, 62 * s),
                    $"PLAY: {_gm.otherMissionLabel}", _button))
                _gm.LoadOtherMission();
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

                // Gate pips: intact = pale diamond, cracked = ember.
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
            var waveLabel = _gm.Endless
                ? $"ENDLESS — WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}"
                : $"WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}/{_gm.WaveCount}";
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

        private void DrawResult(float s)
        {
            GUI.color = new Color(0.055f, 0.07f, 0.1f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (_gm.State == GameManager.Phase.Won)
            {
                var r = _gm.MissionResult();
                GUI.Label(new Rect(0, Screen.height * 0.14f, Screen.width, 60 * s), "MISSION COMPLETE", _banner);
                GUI.Label(new Rect(0, Screen.height * 0.21f, Screen.width, 120 * s), r.rank, _rank);
                GUI.Label(new Rect(0, Screen.height * 0.48f, Screen.width, 30 * s),
                    $"SCORE {r.score}   ·   DMG {Mathf.RoundToInt(_gm.DamageTaken)}   ·   MAX THREAD {(_combat != null ? _combat.MaxCombo : 0)}   ·   GATES {_gm.GatesCrackedTotal}",
                    _banner);
                GUI.Label(new Rect(0, Screen.height * 0.55f, Screen.width, 26 * s),
                    _gm.NewRecord ? "★ NEW RECORD ★" : $"BEST — {_gm.BestRank} · {_gm.BestScore}",
                    _hintCenter);

                var bw = 230 * s;
                if (GUI.Button(new Rect(Screen.width / 2f - bw * 1.5f - 20 * s, Screen.height * 0.68f, bw, 56 * s), "RUN IT AGAIN", _button))
                { Sfx3D.Ui(); _gm.Retry(); }
                if (GUI.Button(new Rect(Screen.width / 2f - bw / 2f, Screen.height * 0.68f, bw, 56 * s), "ENDLESS TRIAL", _button))
                { Sfx3D.Ui(); _gm.StartEndless(); }
                if (!string.IsNullOrEmpty(_gm.otherSceneName)
                    && GUI.Button(new Rect(Screen.width / 2f + bw * 0.5f + 20 * s, Screen.height * 0.68f, bw, 56 * s),
                        _gm.otherMissionLabel, _button))
                    _gm.LoadOtherMission();
            }
            else if (_gm.State == GameManager.Phase.Lost)
            {
                GUI.Label(new Rect(0, Screen.height * 0.24f, Screen.width, 60 * s), "THE LANTERN GUTTERS…", _title);
                GUI.Label(new Rect(0, Screen.height * 0.36f, Screen.width, 30 * s), "but it does not go out.", _banner);
                if (_gm.Endless)
                    GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 26 * s),
                        $"REACHED WAVE {_gm.WaveIndex + 1}   ·   BEST WAVE {_gm.BestWave}" +
                        (_gm.NewRecord ? "   ·   ★ NEW RECORD ★" : ""), _hintCenter);
                var bw = 240 * s;
                if (GUI.Button(new Rect(Screen.width / 2f - bw - 14 * s, Screen.height * 0.60f, bw, 56 * s), "RISE AGAIN", _button))
                { Sfx3D.Ui(); _gm.Retry(); }
                if (!string.IsNullOrEmpty(_gm.otherSceneName)
                    && GUI.Button(new Rect(Screen.width / 2f + 14 * s, Screen.height * 0.60f, bw, 56 * s),
                        _gm.otherMissionLabel, _button))
                    _gm.LoadOtherMission();
            }
        }

        // ------------------------------------------------------------ helpers

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
                fontSize = Mathf.RoundToInt(40 * s),
                normal = { textColor = new Color(0.93f, 0.92f, 0.89f) },
            };
            _rank = new GUIStyle(_banner)
            {
                fontSize = Mathf.RoundToInt(96 * s),
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
                fontSize = Mathf.RoundToInt(13 * s),
                normal = { textColor = new Color(1f, 0.42f, 0.29f) },
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
