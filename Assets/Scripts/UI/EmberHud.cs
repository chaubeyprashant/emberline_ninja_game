using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.UI
{
    /// <summary>
    /// v3 UI: full uGUI replacement for the IMGUI TouchHud. Same responsibilities
    /// (menus, briefing, in-game HUD, results, virtual stick input) rebuilt with
    /// the Kenney kit + Shojumaru/Rajdhani fonts, plus: cooldown rings, combo
    /// pop, low-HP pulse, boss bars, animated rank reveal and star fly-in,
    /// settings (volumes + graphics tier) and Renzo's backstory screen.
    /// Everything is generated in code — no scene/prefab wiring needed.
    /// </summary>
    public class EmberHud : MonoBehaviour
    {
        private enum Screen { None, MenuRoot, Story, Fight, Bio, Skills, Codex, Briefing, Hud, Result }

        private GameManager _gm;
        private Health _health;
        private SenGates _gates;
        private Player.CombatController _combat;
        private Player.PlayerLocomotion _motor;
        private Camera _cam;

        private Canvas _canvas;
        private RectTransform _root;
        private Screen _screen = Screen.None;
        private bool _settingsOpen;
        private RectTransform _screenRoot, _settingsRoot;

        // Virtual stick.
        private int _stickFinger = -1;
        private Vector2 _stickOrigin, _stickPos;

        // Camera orbit drag (right side of the screen, off the buttons).
        private int _camFinger = -1;
        private Vector2 _camLast;
        private bool _movedOnce, _struckOnce;
        private RectTransform _stickBase, _stickKnob;

        // HUD widgets updated per frame.
        private Image _hpFill, _senFill, _bossFill, _cleaveCd, _flickerCd, _kunaiCd;
        private Text _hpLabel, _bossLabel, _waveLabel, _comboText, _objectiveText, _bannerText, _hintText;
        private Image _surgeGlow;
        private CanvasGroup _bannerGroup, _comboGroup;
        private readonly List<Image> _gateIcons = new();
        private RectTransform _bossBar;
        private int _lastCombo;
        private float _comboPop;

        // Pooled enemy markers.
        private class Marker { public RectTransform root; public Image back, fill, arrow; }
        private readonly List<Marker> _markers = new();
        private RectTransform _markerLayer;

        // Menu ember particles.
        private readonly List<RectTransform> _embers = new();
        private RectTransform _emberLayer;

        public static int GraphicsTier
        {
            get => PlayerPrefs.GetInt("gfx_tier", 1);
            set
            {
                PlayerPrefs.SetInt("gfx_tier", Mathf.Clamp(value, 0, 2));
                ApplyGraphicsTier();
            }
        }

        public static void ApplyGraphicsTier()
        {
            var tier = PlayerPrefs.GetInt("gfx_tier", 1);
            QualitySettings.shadowDistance = tier == 0 ? 0f : tier == 1 ? 28f : 42f;
            QualitySettings.shadows = tier == 0 ? ShadowQuality.Disable : ShadowQuality.HardOnly;
        }

        // ------------------------------------------------------------ lifetime

        private void Start()
        {
            _gm = FindFirstObjectByType<GameManager>();
            _combat = FindFirstObjectByType<Player.CombatController>();
            _cam = Camera.main;
            if (_combat != null)
            {
                _health = _combat.GetComponent<Health>();
                _gates = _combat.GetComponent<SenGates>();
                _motor = _combat.GetComponent<Player.PlayerLocomotion>();
            }
            ApplyGraphicsTier();
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)transform;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void Update()
        {
            if (_gm == null) return;
            ReadStick();
            UpdateScreenRouting();
            if (_screen == Screen.Hud) UpdateHud();
            if (_screen is Screen.MenuRoot or Screen.Story or Screen.Fight or Screen.Bio)
                UpdateEmbers();
        }

        private void UpdateScreenRouting()
        {
            var wanted = _gm.State switch
            {
                GameManager.Phase.Menu => _screen is Screen.Story or Screen.Fight or Screen.Bio
                    or Screen.Skills or Screen.Codex ? _screen : Screen.MenuRoot,
                GameManager.Phase.Intro => Screen.Briefing,
                GameManager.Phase.Playing => Screen.Hud,
                _ => _screen == Screen.Skills ? Screen.Skills : Screen.Result,
            };
            if (wanted != _screen) SetScreen(wanted);
        }

        private void SetScreen(Screen s)
        {
            _screen = s;
            if (_screenRoot != null)
            {
                if (Application.isPlaying) Destroy(_screenRoot.gameObject);
                else DestroyImmediate(_screenRoot.gameObject); // editor snapshot tooling
            }
            _markers.Clear();
            _embers.Clear();
            _gateIcons.Clear();
            _screenRoot = UiKit.Group(_root, "Screen_" + s);
            switch (s)
            {
                case Screen.MenuRoot: BuildMenuRoot(); break;
                case Screen.Story: BuildStorySelect(); break;
                case Screen.Fight: BuildFightSelect(); break;
                case Screen.Bio: BuildBio(); break;
                case Screen.Skills: BuildSkills(); break;
                case Screen.Codex: BuildCodex(); break;
                case Screen.Briefing: BuildBriefing(); break;
                case Screen.Hud: BuildHud(); break;
                case Screen.Result: BuildResult(); break;
            }
        }

        // ------------------------------------------------------------- helpers

        private void Dim(float a) =>
            UiKit.Img(UiKit.Group(_screenRoot, "Dim"), null, new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, a));

        private void BuildEmberLayer()
        {
            _emberLayer = UiKit.Group(_screenRoot, "Embers");
            for (var i = 0; i < 14; i++)
            {
                var e = UiKit.Rect(_emberLayer, "e", new Vector2(0.5f, 0f),
                    new Vector2(Random.Range(-620, 620), Random.Range(0, 720)),
                    Vector2.one * Random.Range(4f, 10f));
                var img = UiKit.Img(e, UiKit.Circle, new Color(1f, 0.55f, 0.3f, Random.Range(0.2f, 0.6f)));
                img.raycastTarget = false;
                _embers.Add(e);
            }
        }

        private void UpdateEmbers()
        {
            for (var i = 0; i < _embers.Count; i++)
            {
                var e = _embers[i];
                if (e == null) continue;
                var p = e.anchoredPosition;
                p.y += (12f + i * 3f) * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * (0.6f + i * 0.13f) + i) * 18f * Time.deltaTime;
                if (p.y > 740) { p.y = -10; p.x = Random.Range(-620, 620); }
                e.anchoredPosition = p;
            }
        }

        // -------------------------------------------------------------- menus

        private void BuildMenuRoot()
        {
            Dim(0.55f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "AN EMBERLINE STORY", 16, UiKit.Ember,
                new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(600, 30));
            UiKit.Label(_screenRoot, "THE NIGHT OF YORUNE", 46, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -136), new Vector2(900, 70), display: true);

            // Three mode cards.
            var modes = new (string title, string sub, System.Action go)[]
            {
                ($"STORY", $"{Session.StoryUnlocked - 1}/{Session.Story.Length} CLEARED",
                    () => SetScreen(Screen.Story)),
                ("FIGHT", "DUELS", () => SetScreen(Screen.Fight)),
                ("MARCH", _gm.BestNorth > 0 ? $"BEST {_gm.BestNorth}m NORTH" : "THE ROAD NORTH",
                    () => _gm.LaunchEndless()),
            };
            for (var i = 0; i < 3; i++)
            {
                var (title, sub, go) = modes[i];
                var x = (i - 1) * 320f;
                var card = UiKit.Rect(_screenRoot, "Card", new Vector2(0.5f, 0.5f),
                    new Vector2(x, -10), new Vector2(280, 220));
                var img = UiKit.Img(card, UiKit.PanelSprite, UiKit.Panel, sliced: true);
                img.raycastTarget = true;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var action = go;
                btn.onClick.AddListener(() => { Sfx3D.Confirm(); action(); });
                UiKit.Label(card, title, 34, UiKit.EmberBright, new Vector2(0.5f, 0.5f),
                    new Vector2(0, 24), new Vector2(260, 44), display: true);
                UiKit.Label(card, sub, 18, UiKit.Dim, new Vector2(0.5f, 0.5f),
                    new Vector2(0, -28), new Vector2(260, 26));
            }

            UiKit.Label(_screenRoot,
                $"★ {Session.TotalStars} / {Session.Story.Length * 3}      ◆ {SkillTree.Shards} SHARDS" +
                $"      SKILLS {SkillTree.OwnedCount}/{SkillTree.Nodes.Count}", 21,
                UiKit.EmberBright, new Vector2(0.5f, 0f), new Vector2(0, 158), new Vector2(900, 30));

            // Weapon selector — cycles through unlocked blades.
            Text weaponLabel = null;
            var weaponBtn = UiKit.MakeButton(_screenRoot, "", new Vector2(0.5f, 0f),
                new Vector2(0, 96), new Vector2(430, 56), () =>
                {
                    var all = Loadout.All;
                    if (all.Length == 0) return;
                    var cur = System.Array.IndexOf(all, Loadout.Current);
                    for (var step = 1; step <= all.Length; step++)
                    {
                        var next = all[(cur + step) % all.Length];
                        if (Loadout.IsUnlocked(next)) { Loadout.Select(next); break; }
                    }
                    weaponLabel.text = WeaponLine();
                }, 18);
            weaponLabel = weaponBtn.GetComponentInChildren<Text>();
            weaponLabel.text = WeaponLine();

            // Blade finish (cosmetic skin) cycler.
            Text finishLabel = null;
            var finishBtn = UiKit.MakeButton(_screenRoot, "", new Vector2(0.5f, 0f),
                new Vector2(0, 40), new Vector2(430, 50), () =>
                {
                    BladeFinish.CycleNext();
                    finishLabel.text = FinishLine();
                }, 16);
            finishLabel = finishBtn.GetComponentInChildren<Text>();
            finishLabel.text = FinishLine();

            // Daily challenge strip.
            var daily = DailyChallenge.Today;
            UiKit.Label(_screenRoot,
                DailyChallenge.DoneToday
                    ? "DAILY — COMPLETE ✓"
                    : $"DAILY — {daily.desc}  (◆ {daily.reward})",
                16, DailyChallenge.DoneToday ? new Color(0.5f, 0.85f, 0.55f) : UiKit.Dim,
                new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(900, 24));

            if (Session.NewGamePlus)
                UiKit.Label(_screenRoot, "NEW GAME+  —  THE MARSH REMEMBERS", 15, UiKit.Ember,
                    new Vector2(0.5f, 1f), new Vector2(0, -176), new Vector2(700, 22));

            UiKit.MakeButton(_screenRoot, "RENZO", new Vector2(0f, 0f), new Vector2(110, 52),
                new Vector2(150, 58), () => SetScreen(Screen.Bio), 18, display: true);
            UiKit.MakeButton(_screenRoot, "SKILLS", new Vector2(0f, 0f), new Vector2(275, 52),
                new Vector2(150, 58), () => SetScreen(Screen.Skills), 18, display: true);
            UiKit.MakeButton(_screenRoot, "CODEX", new Vector2(0f, 0f), new Vector2(440, 52),
                new Vector2(150, 58), () => SetScreen(Screen.Codex), 18, display: true);
            UiKit.MakeButton(_screenRoot, "SETTINGS", new Vector2(1f, 0f), new Vector2(-130, 52),
                new Vector2(190, 58), ToggleSettings, 20);
        }

        private void BuildCodex()
        {
            Dim(0.8f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "LORE CODEX", 36, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(700, 50), display: true);
            UiKit.Label(_screenRoot, $"{StoryMemory.LoreCount}/6 ENTRIES — defeat a foe to learn its story",
                18, UiKit.Dim, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(700, 26));

            // Feats strip along the bottom.
            for (var f = 0; f < Feats.All.Count; f++)
            {
                var feat = Feats.All[f];
                var has = Feats.Has(feat.id);
                var chip = UiKit.Rect(_screenRoot, "Feat_" + feat.id, new Vector2(0.5f, 0f),
                    new Vector2((f - 2.5f) * 200f, 130f), new Vector2(190, 64));
                UiKit.Img(chip, UiKit.PanelThin ?? UiKit.PanelSprite,
                    has ? new Color(0.32f, 0.18f, 0.12f) : new Color(0.08f, 0.085f, 0.11f), sliced: true);
                UiKit.Label(chip, has ? feat.title : "———", 13,
                    has ? UiKit.EmberBright : new Color(1, 1, 1, 0.25f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(180, 20));
                UiKit.Paragraph(chip, feat.desc, 10, has ? UiKit.Dim : new Color(1, 1, 1, 0.18f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(178, 30))
                    .alignment = TextAnchor.MiddleCenter;
            }

            var kinds = (EnemyKind[])System.Enum.GetValues(typeof(EnemyKind));
            for (var i = 0; i < kinds.Length; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var pos = new Vector2((col - 1) * 400f, 90f - row * 190f);
                var card = UiKit.Rect(_screenRoot, "Lore_" + kinds[i], new Vector2(0.5f, 0.5f),
                    pos, new Vector2(380, 185));
                var unlocked = StoryMemory.HasLore(kinds[i]);
                UiKit.Img(card, UiKit.PanelSprite,
                    unlocked ? UiKit.Panel : new Color(0.07f, 0.075f, 0.1f), sliced: true);
                if (!unlocked)
                {
                    UiKit.Label(card, "?", 54, new Color(1, 1, 1, 0.15f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(100, 90), display: true);
                    continue;
                }
                var (loreName, loreStory, loreTip) = StoryMemory.Lore(kinds[i]);
                UiKit.Label(card, loreName, 18, UiKit.EmberBright, new Vector2(0.5f, 1f),
                    new Vector2(0, -22), new Vector2(360, 26), display: true);
                UiKit.Paragraph(card, loreStory, 14, new Color(0.82f, 0.84f, 0.86f),
                    new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(350, 70));
                UiKit.Paragraph(card, loreTip, 13, UiKit.Ember,
                    new Vector2(0.5f, 0f), new Vector2(0, 44), new Vector2(350, 44));
            }
            BackButton();
        }

        private static string FinishLine()
        {
            var f = BladeFinish.Current;
            var nextLock = "";
            foreach (var other in BladeFinish.All)
                if (!BladeFinish.IsUnlocked(other))
                {
                    nextLock = $"   (next at ★{other.starsRequired})";
                    break;
                }
            return $"FINISH — {f.name}{nextLock}";
        }

        private static string WeaponLine()
        {
            var w = Loadout.Current;
            if (w == null) return "BLADE — NONE";
            var locked = 0;
            foreach (var other in Loadout.All)
                if (!Loadout.IsUnlocked(other)) locked++;
            return $"BLADE — {w.displayName}" + (locked > 0 ? $"   ({locked} LOCKED)" : "   ↻");
        }

        private void BuildSkills()
        {
            Dim(0.8f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "EMBER SKILLS", 36, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(700, 50), display: true);
            var shardLabel = UiKit.Label(_screenRoot, $"◆ {SkillTree.Shards} EMBER SHARDS", 21,
                UiKit.EmberBright, new Vector2(0.5f, 1f), new Vector2(0, -102), new Vector2(500, 28));

            var byBranch = new Dictionary<string, int>();
            foreach (var node in SkillTree.Nodes)
            {
                if (!byBranch.ContainsKey(node.branch)) byBranch[node.branch] = 0;
                var row = byBranch[node.branch]++;
                var colIdx = node.branch == "COMBAT" ? 0 : node.branch == "DEFENSE" ? 1 : 2;
                var pos = new Vector2((colIdx - 1) * 380f, 90f - row * 140f);

                if (row == 0)
                    UiKit.Label(_screenRoot, node.branch, 19, UiKit.Ember,
                        new Vector2(0.5f, 0.5f), pos + new Vector2(0, 82), new Vector2(300, 26));

                var card = UiKit.Rect(_screenRoot, "Skill_" + node.id, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(350, 118));
                var owned = SkillTree.Has(node.id);
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    owned ? new Color(0.32f, 0.18f, 0.12f) : UiKit.Panel, sliced: true);
                UiKit.Label(card, node.title, 20, owned ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(330, 26), display: true);
                var descText = UiKit.Paragraph(card, node.desc, 15, UiKit.Dim,
                    new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(320, 40));
                descText.alignment = TextAnchor.MiddleCenter;
                if (owned)
                {
                    UiKit.Label(card, "LEARNED", 15, UiKit.EmberBright,
                        new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(200, 22));
                }
                else
                {
                    var n = node;
                    img.raycastTarget = true;
                    var btn = card.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.interactable = SkillTree.Shards >= node.cost;
                    btn.onClick.AddListener(() =>
                    {
                        if (SkillTree.TryBuy(n)) { Sfx3D.Confirm(); SetScreen(Screen.Skills); }
                        else Sfx3D.Error();
                    });
                    UiKit.Label(card, $"◆ {node.cost}", 16,
                        SkillTree.Shards >= node.cost ? UiKit.EmberBright : UiKit.Dim,
                        new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(200, 22));
                }
            }
            BackButton();
        }

        private void BuildStorySelect()
        {
            Dim(0.62f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "STORY — THE NIGHT OF YORUNE", 30, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(900, 44), display: true);
            UiKit.Label(_screenRoot,
                "ACT I · THE LANTERN FALLS      ACT II · INTO THE MARSH      ACT III · THE SERPENT'S COIL",
                16, UiKit.Ember, new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(1100, 26));

            const int cols = 5;
            for (var i = 0; i < Session.Story.Length; i++)
            {
                var level = Session.Story[i];
                var col = i % cols;
                var row = i / cols;
                var pos = new Vector2((col - 2) * 205f, 60f - row * 165f);
                var card = UiKit.Rect(_screenRoot, "Level" + level.id, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(185, 140));
                var unlocked = level.id <= Session.StoryUnlocked;
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    unlocked ? UiKit.Panel : new Color(0.07f, 0.075f, 0.1f), sliced: true);
                img.raycastTarget = true;
                if (unlocked)
                {
                    var idx = i;
                    var btn = card.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => { Sfx3D.Confirm(); _gm.LaunchStory(idx); });
                }
                UiKit.Label(card, level.id.ToString(), 30, unlocked ? UiKit.EmberBright : UiKit.Dim,
                    new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(80, 40), display: true);
                UiKit.Label(card, unlocked ? level.name : "LOCKED", 13,
                    unlocked ? UiKit.Pale : UiKit.Dim,
                    new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(175, 22));
                // Stars.
                var stars = Session.Stars(level.id);
                for (var sIdx = 0; sIdx < 3; sIdx++)
                {
                    var sRt = UiKit.Rect(card, "star", new Vector2(0.5f, 0f),
                        new Vector2((sIdx - 1) * 34f, 26f), new Vector2(26, 26));
                    UiKit.Img(sRt, sIdx < stars ? UiKit.Star : UiKit.StarOutline,
                        sIdx < stars ? UiKit.EmberBright : new Color(1, 1, 1, unlocked ? 0.35f : 0.12f));
                }
            }
            BackButton();
        }

        private void BuildFightSelect()
        {
            Dim(0.62f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "FIGHT — CHOOSE YOUR OPPONENT", 30, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(900, 44), display: true);

            for (var i = 0; i < Session.Duels.Length; i++)
            {
                var duel = Session.Duels[i];
                var col = i % 2;
                var row = i / 2;
                var pos = new Vector2((col - 0.5f) * 400f, 60f - row * 140f);
                var card = UiKit.Rect(_screenRoot, "Duel" + duel.id, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(370, 116));
                var unlocked = duel.id <= Session.DuelsUnlocked;
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    unlocked ? UiKit.Panel : new Color(0.07f, 0.075f, 0.1f), sliced: true);
                img.raycastTarget = true;
                if (unlocked)
                {
                    var idx = i;
                    var btn = card.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => { Sfx3D.Confirm(); _gm.LaunchDuel(idx); });
                }
                var won = Session.DuelWon(duel.id);
                UiKit.Label(card, unlocked ? duel.name + (won ? "  ✓" : "") : "LOCKED", 24,
                    unlocked ? UiKit.EmberBright : UiKit.Dim,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(350, 34), display: true);
                UiKit.Label(card, unlocked ? duel.title : "Defeat the previous opponent", 15,
                    UiKit.Dim, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(350, 24));
            }

            UiKit.Label(_screenRoot, "One life. Full strength. No mercy.", 17, UiKit.Ember,
                new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(600, 26));
            BackButton();
        }

        private void BuildBio()
        {
            Dim(0.8f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "RENZO", 44, UiKit.EmberBright,
                new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(600, 60), display: true);
            UiKit.Label(_screenRoot, "THE EMBER LANTERN", 18, UiKit.Ember,
                new Vector2(0.5f, 1f), new Vector2(0, -116), new Vector2(600, 26));

            var panel = UiKit.Rect(_screenRoot, "BioPanel", new Vector2(0.5f, 0.5f),
                new Vector2(0, -30), new Vector2(880, 380));
            UiKit.Img(panel, UiKit.PanelSprite, new Color(0.09f, 0.095f, 0.13f, 0.95f), sliced: true);
            UiKit.Paragraph(panel,
                "Renzo is the last lantern-bearer of the Emberline — the old road of flame-keepers " +
                "who carried fire across the rooftops of Yorune, guiding traders home through the " +
                "dark. The lantern on his belt is his family's heirloom: a flame that has not gone " +
                "out in two hundred years.\n\n" +
                "When the raiders came for the lantern oil, they found a guard instead — a quiet " +
                "young man with a borrowed sword and nothing left to lose. The Ember Lantern " +
                "answers him now. It flares when he fights. It gutters when he doubts.\n\n" +
                "They say the flame draws the dead as surely as it draws the living. Somewhere in " +
                "Ashfen marsh, something enormous has begun collecting lanterns — and Renzo's is " +
                "the oldest of them all.",
                19, new Color(0.85f, 0.86f, 0.88f),
                new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(800, 330));
            BackButton();
        }

        private void BackButton() =>
            UiKit.MakeButton(_screenRoot, "← BACK", new Vector2(0f, 0f), new Vector2(110, 52),
                new Vector2(160, 58), () => { Sfx3D.Back(); SetScreen(Screen.MenuRoot); }, 18);

        // ------------------------------------------------------------ settings

        private void ToggleSettings()
        {
            if (_settingsOpen) { CloseSettings(); return; }
            _settingsOpen = true;
            _settingsRoot = UiKit.Group(_root, "Settings");
            UiKit.Img(_settingsRoot, null, new Color(0, 0, 0, 0.7f)).raycastTarget = true;
            var panel = UiKit.Rect(_settingsRoot, "Panel", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560, 400));
            UiKit.Img(panel, UiKit.PanelSprite, new Color(0.1f, 0.105f, 0.14f, 0.98f), sliced: true);
            UiKit.Label(panel, "SETTINGS", 30, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -42), new Vector2(400, 40), display: true);

            Text sfxLabel = null, musicLabel = null, gfxLabel = null;
            void Refresh()
            {
                sfxLabel.text = $"SFX VOLUME   {Mathf.RoundToInt(Sfx3D.SfxVolume * 100)}%";
                musicLabel.text = $"MUSIC VOLUME   {Mathf.RoundToInt(Sfx3D.MusicVolume * 100)}%";
                gfxLabel.text = "GRAPHICS   " + GraphicsTier switch { 0 => "LOW", 1 => "MEDIUM", _ => "HIGH" };
            }

            void Row(float y, System.Func<Text> label, System.Action minus, System.Action plus)
            {
                UiKit.MakeButton(panel, "−", new Vector2(0.5f, 0.5f), new Vector2(-190, y),
                    new Vector2(64, 56), () => { minus(); Refresh(); }, 26);
                UiKit.MakeButton(panel, "+", new Vector2(0.5f, 0.5f), new Vector2(190, y),
                    new Vector2(64, 56), () => { plus(); Refresh(); }, 26);
            }

            sfxLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, 70), new Vector2(280, 30));
            Row(70, () => sfxLabel, () => Sfx3D.SfxVolume -= 0.1f, () => Sfx3D.SfxVolume += 0.1f);
            musicLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(280, 30));
            Row(0, () => musicLabel, () => Sfx3D.MusicVolume -= 0.1f, () => Sfx3D.MusicVolume += 0.1f);
            gfxLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, -70), new Vector2(280, 30));
            Row(-70, () => gfxLabel, () => GraphicsTier--, () => GraphicsTier++);
            Refresh();

            UiKit.MakeButton(panel, "CLOSE", new Vector2(0.5f, 0f), new Vector2(0, 46),
                new Vector2(200, 56), CloseSettings, 20);
        }

        private void CloseSettings()
        {
            _settingsOpen = false;
            if (_settingsRoot != null) Destroy(_settingsRoot.gameObject);
        }

        // ----------------------------------------------------------- briefing

        private void BuildBriefing()
        {
            Dim(0.9f);
            BuildEmberLayer();
            string kicker = "", title = "", story = "", objective = "";
            if (_gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null)
            {
                var d = _gm.CurrentDuel;
                (kicker, title, story) = ("DUEL", d.name, d.taunt);
                objective = "OBJECTIVE — Win the duel. One life each.";
            }
            else if (_gm.ModeNow == LaunchMode.Endless)
            {
                (kicker, title) = ("THE MARCH", "THE ROAD NORTH");
                story = "The road runs north without end. Soldiers bar the way, the mist walls "
                    + "you in until they fall — and something heavier waits at every milestone.";
                objective = _gm.BestNorth > 0 ? $"BEST — {_gm.BestNorth}m NORTH" : "";
            }
            else if (_gm.CurrentLevel != null)
            {
                var l = _gm.CurrentLevel;
                kicker = $"LEVEL {l.id}  ·  {Session.ActName(l.id)}";
                title = l.name;
                story = l.story;
                objective = l.holdSeconds > 0f
                    ? $"OBJECTIVE — Hold the road for {Mathf.RoundToInt(l.holdSeconds)} seconds. ★★★ for rank A or higher."
                    : $"OBJECTIVE — Clear {l.waves.Length} wave{(l.waves.Length > 1 ? "s" : "")}. ★★★ for rank A or higher.";
            }

            UiKit.Label(_screenRoot, kicker, 17, UiKit.Ember, new Vector2(0.5f, 1f),
                new Vector2(0, -66), new Vector2(900, 26));
            UiKit.Label(_screenRoot, title, 44, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -122), new Vector2(1000, 62), display: true);
            var storyText = UiKit.Paragraph(_screenRoot, story, 20, new Color(0.82f, 0.84f, 0.86f),
                new Vector2(0.5f, 1f), new Vector2(0, -172), new Vector2(760, 80));
            storyText.fontStyle = FontStyle.Italic;
            UiKit.Label(_screenRoot, objective, 17, UiKit.Ember, new Vector2(0.5f, 1f),
                new Vector2(0, -262), new Vector2(900, 28));

            // Character dialogue with portraits (story mode).
            if (_gm.ModeNow == LaunchMode.Story && _gm.CurrentLevel != null
                && _gm.CurrentLevel.dialogue.Length > 0)
            {
                DialogueBox.Show(_screenRoot, _gm.CurrentLevel.dialogue,
                    () => StoryMemory.MarkDialogueSeen(_gm.CurrentLevel.id));
            }

            UiKit.MakeButton(_screenRoot, "BEGIN", new Vector2(0.5f, 0f), new Vector2(-160, 96),
                new Vector2(280, 64), () => { Sfx3D.Confirm(); _gm.BeginMission(); }, 24, display: true,
                tint: new Color(0.5f, 0.24f, 0.16f));
            UiKit.MakeButton(_screenRoot, "MENU", new Vector2(0.5f, 0f), new Vector2(160, 96),
                new Vector2(280, 64), () => _gm.OpenMenu(), 22);
        }

        // ---------------------------------------------------------------- hud

        private void BuildHud()
        {
            // Health + Sen, top-left.
            _hpLabel = UiKit.Label(_screenRoot, "LIFE", 15, UiKit.Dim, new Vector2(0, 1),
                new Vector2(64, -26), new Vector2(100, 22), align: TextAnchor.MiddleLeft);
            _hpFill = UiKit.MakeBar(_screenRoot, new Vector2(0, 1), new Vector2(174, -50),
                new Vector2(300, 18), UiKit.Blood, new Vector2(0.5f, 0.5f));
            _senFill = UiKit.MakeBar(_screenRoot, new Vector2(0, 1), new Vector2(159, -76),
                new Vector2(270, 12), UiKit.Sen, new Vector2(0.5f, 0.5f));
            for (var i = 0; i < SenGates.TotalGates; i++)
            {
                var g = UiKit.Rect(_screenRoot, "gate", new Vector2(0, 1),
                    new Vector2(84 + i * 30, -100), new Vector2(17, 17));
                g.localRotation = Quaternion.Euler(0, 0, 45);
                _gateIcons.Add(UiKit.Img(g, null, UiKit.Pale));
            }

            // Wave / time, top-right.
            _waveLabel = UiKit.Label(_screenRoot, "", 17, UiKit.Dim, new Vector2(1, 1),
                new Vector2(-160, -28), new Vector2(300, 24), align: TextAnchor.MiddleRight);

            // Gyro camera toggle, under the clock (devices with a gyroscope only).
            if (SystemInfo.supportsGyroscope)
            {
                Text gyroText = null;
                var gyroBtn = UiKit.MakeButton(_screenRoot,
                    EmberInput.GyroOn ? "GYRO ON" : "GYRO OFF", new Vector2(1, 1),
                    new Vector2(-76, -74), new Vector2(118, 56), () =>
                    {
                        EmberInput.GyroOn = !EmberInput.GyroOn;
                        if (gyroText != null)
                            gyroText.text = EmberInput.GyroOn ? "GYRO ON" : "GYRO OFF";
                    }, 14);
                gyroText = gyroBtn.GetComponentInChildren<Text>();
            }

            // Combo, upper-center-right.
            var comboRt = UiKit.Rect(_screenRoot, "Combo", new Vector2(0.5f, 1f),
                new Vector2(310, -78), new Vector2(300, 60));
            _comboGroup = comboRt.gameObject.AddComponent<CanvasGroup>();
            _comboText = UiKit.Label(comboRt, "", 34, UiKit.EmberBright, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300, 60), display: true);

            // Boss bar, top-center.
            _bossBar = UiKit.Rect(_screenRoot, "BossBar", new Vector2(0.5f, 1f),
                new Vector2(0, -46), new Vector2(560, 46));
            _bossLabel = UiKit.Label(_bossBar, "", 18, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -8), new Vector2(560, 24));
            _bossFill = UiKit.MakeBar(_bossBar, new Vector2(0.5f, 0f), new Vector2(0, 8),
                new Vector2(520, 14), UiKit.Ember);
            _bossBar.gameObject.SetActive(false);

            // Objective + banner.
            _objectiveText = UiKit.Label(_screenRoot, "", 16, new Color(1f, 0.62f, 0.45f),
                new Vector2(0, 1), new Vector2(300, -126), new Vector2(560, 24), align: TextAnchor.MiddleLeft);
            var bannerRt = UiKit.Rect(_screenRoot, "Banner", new Vector2(0.5f, 1f),
                new Vector2(0, -150), new Vector2(900, 44));
            _bannerGroup = bannerRt.gameObject.AddComponent<CanvasGroup>();
            _bannerText = UiKit.Label(bannerRt, "", 27, UiKit.Pale, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900, 44), display: true);
            _hintText = UiKit.Label(_screenRoot, "", 20, UiKit.Pale, new Vector2(0.5f, 0f),
                new Vector2(0, 230), new Vector2(900, 30));

            // Enemy marker layer (under buttons).
            _markerLayer = UiKit.Group(_screenRoot, "Markers");

            // Virtual stick visuals.
            _stickBase = UiKit.Rect(_screenRoot, "StickBase", new Vector2(0, 0),
                new Vector2(-500, -500), new Vector2(150, 150));
            UiKit.Img(_stickBase, UiKit.ButtonRound, new Color(1, 1, 1, 0.10f));
            _stickKnob = UiKit.Rect(_screenRoot, "StickKnob", new Vector2(0, 0),
                new Vector2(-500, -500), new Vector2(64, 64));
            UiKit.Img(_stickKnob, UiKit.ButtonRound, new Color(1, 1, 1, 0.35f));
            _stickBase.gameObject.SetActive(false);
            _stickKnob.gameObject.SetActive(false);

            BuildCombatButtons();
        }

        private void BuildCombatButtons()
        {
            Image CombatButton(string label, Vector2 pos, float size, Color color, System.Action press)
            {
                var rt = UiKit.Rect(_screenRoot, "Cb_" + label, new Vector2(1, 0), pos,
                    new Vector2(size, size));
                var img = UiKit.Img(rt, UiKit.ButtonRound, new Color(color.r, color.g, color.b, 0.55f));
                img.raycastTarget = true;
                UiKit.Label(rt, label, Mathf.RoundToInt(size * 0.17f), UiKit.Pale,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size * 0.3f));
                // PointerDown (not click) — combat inputs must fire on touch, not release.
                var trigger = rt.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ => { press(); _struckOnce = true; });
                trigger.triggers.Add(entry);
                // Cooldown ring overlay (radial fill).
                var cdRt = UiKit.Rect(rt, "Cd", new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(size, size));
                var cd = UiKit.Img(cdRt, UiKit.ButtonRound, new Color(0, 0, 0, 0.55f));
                cd.type = Image.Type.Filled;
                cd.fillMethod = Image.FillMethod.Radial360;
                cd.fillOrigin = (int)Image.Origin360.Top;
                cd.fillClockwise = false;
                cd.fillAmount = 0f;
                return cd;
            }

            CombatButton("STRIKE", new Vector2(-118, 118), 132, UiKit.Sen, EmberInput.PressStrike);
            _cleaveCd = CombatButton("CLEAVE", new Vector2(-248, 92), 100,
                new Color(0.24f, 0.42f, 0.49f), EmberInput.PressCleave);
            _flickerCd = CombatButton("FLICKER", new Vector2(-100, 252), 100,
                new Color(0.59f, 0.63f, 0.67f), EmberInput.PressFlicker);
            var surgeCd = CombatButton("SURGE", new Vector2(-232, 216), 88, UiKit.Ember,
                EmberInput.PressSurge);
            _surgeGlow = surgeCd.transform.parent.GetComponent<Image>();
            _kunaiCd = CombatButton("KUNAI", new Vector2(-348, 156), 88,
                new Color(0.42f, 0.5f, 0.62f), EmberInput.PressKunai);
        }

        private void UpdateHud()
        {
            if (_health != null)
            {
                var frac = _health.Hp / _health.MaxHp;
                _hpFill.fillAmount = Mathf.Lerp(_hpFill.fillAmount, frac, Time.deltaTime * 10f);
                // Low-HP ember pulse.
                _hpFill.color = frac < 0.3f
                    ? Color.Lerp(UiKit.Blood, UiKit.EmberBright, Mathf.PingPong(Time.time * 2.4f, 1f))
                    : UiKit.Blood;
            }
            if (_gates != null)
            {
                _senFill.fillAmount = _gates.Sen / 100f; // bar shows Sen against full scale
                for (var i = 0; i < _gateIcons.Count; i++)
                {
                    var cracked = i >= SenGates.TotalGates - _gates.CrackedGates;
                    _gateIcons[i].color = cracked ? UiKit.Ember : new Color(0.9f, 0.89f, 0.86f, 0.85f);
                }
                _surgeGlow.color = _gates.Sen >= SenGates.SurgeCost
                    ? new Color(UiKit.Ember.r, UiKit.Ember.g, UiKit.Ember.b,
                        0.65f + Mathf.PingPong(Time.time * 0.8f, 0.3f))
                    : new Color(UiKit.Ember.r, UiKit.Ember.g, UiKit.Ember.b, 0.18f);
            }

            if (_combat != null)
            {
                _cleaveCd.fillAmount = _combat.CleaveCd01;
                _kunaiCd.fillAmount = _combat.KunaiCd01;
            }
            if (_motor != null) _flickerCd.fillAmount = _motor.FlickerCd01;

            var t = (int)_gm.MissionTime;
            _waveLabel.text = _gm.ModeNow switch
            {
                LaunchMode.Endless => $"NORTH {_gm.DistanceNorth}m   {t / 60}:{t % 60:00}",
                LaunchMode.Duel => $"DUEL   {t / 60}:{t % 60:00}",
                _ => $"WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}/{_gm.WaveCount}   {t / 60}:{t % 60:00}",
            };

            UpdateCombo();
            UpdateBossBar();
            UpdateBannerObjective();
            UpdateMarkers();
            UpdateStickVisual();
        }

        private void UpdateCombo()
        {
            var combo = _combat != null ? _combat.Combo : 0;
            if (combo > _lastCombo) _comboPop = 1f;
            _lastCombo = combo;
            _comboPop = Mathf.Max(0, _comboPop - Time.deltaTime * 4f);
            if (combo > 1)
            {
                _comboText.text = $"{combo} THREAD";
                _comboGroup.alpha = Mathf.Min(1f, _comboGroup.alpha + Time.deltaTime * 6f);
                var scale = 1f + _comboPop * 0.35f + Mathf.Min(combo, 30) * 0.008f;
                _comboText.transform.localScale = Vector3.one * scale;
            }
            else _comboGroup.alpha = Mathf.Max(0f, _comboGroup.alpha - Time.deltaTime * 3f);
        }

        private void UpdateBossBar()
        {
            EnemyBrain boss = null;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead || e.isClone) continue;
                if (_gm.ModeNow == LaunchMode.Duel
                    || e.kind is EnemyKind.Chief or EnemyKind.Kagachi or EnemyKind.Jin)
                { boss = e; break; }
            }
            _bossBar.gameObject.SetActive(boss != null);
            if (boss == null) return;
            _bossFill.fillAmount = Mathf.Lerp(_bossFill.fillAmount, boss.Hp / boss.maxHp,
                Time.deltaTime * 8f);
            _bossLabel.text = _gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null
                ? _gm.CurrentDuel.name
                : Session.BossCard(boss.kind).name ?? "";
        }

        private void UpdateBannerObjective()
        {
            _objectiveText.text = _gm.Objective;
            if (_gm.BannerTimer > 0)
            {
                _bannerText.text = _gm.Banner;
                _bannerGroup.alpha = Mathf.Min(1f, _gm.BannerTimer / 0.4f);
            }
            else _bannerGroup.alpha = 0f;

            // First-run hints, story level 1 only.
            if (_gm.ModeNow == LaunchMode.Story && Session.LevelIndex == 0
                && _gm.MissionTime < 20f && (!_movedOnce || !_struckOnce))
                _hintText.text = !_movedOnce
                    ? "DRAG THE LEFT SIDE OF THE SCREEN TO MOVE"
                    : "TAP STRIKE WHEN AN ENEMY IS CLOSE";
            else _hintText.text = "";
        }

        private void UpdateMarkers()
        {
            if (_cam == null) return;
            var used = 0;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead) continue;
                if (used >= 16) break;
                var m = GetMarker(used++);
                var sp = _cam.WorldToScreenPoint(e.transform.position + Vector3.up * 2.4f);
                var scale = 720f / UnityEngine.Screen.height;
                var onScreen = sp.z > 0 && sp.x > 0 && sp.x < UnityEngine.Screen.width
                               && sp.y > 0 && sp.y < UnityEngine.Screen.height;
                if (onScreen)
                {
                    m.back.enabled = true;
                    m.fill.enabled = true;
                    m.arrow.enabled = false;
                    m.root.anchoredPosition = new Vector2(sp.x * scale, sp.y * scale);
                    m.fill.fillAmount = Mathf.Clamp01(e.Hp / e.maxHp);
                }
                else
                {
                    var p = new Vector2(sp.x, sp.y);
                    if (sp.z < 0) p = new Vector2(UnityEngine.Screen.width - p.x, 40);
                    p.x = Mathf.Clamp(p.x, 30, UnityEngine.Screen.width - 30);
                    p.y = Mathf.Clamp(p.y, 30, UnityEngine.Screen.height - 30);
                    m.back.enabled = false;
                    m.fill.enabled = false;
                    m.arrow.enabled = true;
                    m.root.anchoredPosition = p * scale;
                }
            }
            for (var i = used; i < _markers.Count; i++)
            {
                _markers[i].back.enabled = false;
                _markers[i].fill.enabled = false;
                _markers[i].arrow.enabled = false;
            }
        }

        private Marker GetMarker(int i)
        {
            while (_markers.Count <= i)
            {
                var root = UiKit.Rect(_markerLayer, "Marker", Vector2.zero, Vector2.zero,
                    new Vector2(60, 8));
                var back = UiKit.Img(root, null, new Color(0, 0, 0, 0.5f));
                var fillRt = UiKit.Rect(root, "fill", new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(58, 6));
                var fill = UiKit.Img(fillRt, null, UiKit.Ember);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.sprite = Sprite.Create(Texture2D.whiteTexture,
                    new UnityEngine.Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                var arrowRt = UiKit.Rect(root, "arrow", new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(18, 18));
                arrowRt.localRotation = Quaternion.Euler(0, 0, 45);
                var arrow = UiKit.Img(arrowRt, null, new Color(1f, 0.35f, 0.25f, 0.9f));
                _markers.Add(new Marker { root = root, back = back, fill = fill, arrow = arrow });
            }
            return _markers[i];
        }

        // -------------------------------------------------------------- stick

        private void ReadStick()
        {
            if (_gm.State != GameManager.Phase.Playing)
            {
                EmberInput.TouchActive = false;
                _camFinger = -1;
                return;
            }

            EmberInput.TouchActive = _stickFinger >= 0;
            foreach (var t in Input.touches)
            {
                if (t.phase == TouchPhase.Began && _stickFinger < 0
                    && t.position.x < UnityEngine.Screen.width * 0.45f)
                {
                    _stickFinger = t.fingerId;
                    _stickOrigin = t.position;
                    _stickPos = t.position;
                }
                // Right-side drags that miss the buttons orbit the camera.
                else if (t.phase == TouchPhase.Began && _camFinger < 0
                    && t.fingerId != _stickFinger
                    && t.position.x >= UnityEngine.Screen.width * 0.45f
                    && !(EventSystem.current != null
                         && EventSystem.current.IsPointerOverGameObject(t.fingerId)))
                {
                    _camFinger = t.fingerId;
                    _camLast = t.position;
                }
                else if (t.fingerId == _camFinger)
                {
                    if (t.phase is TouchPhase.Ended or TouchPhase.Canceled)
                    {
                        _camFinger = -1;
                    }
                    else
                    {
                        EmberInput.AddCamYaw((t.position.x - _camLast.x)
                            * (220f / UnityEngine.Screen.width)); // full swipe ≈ 220°
                        // Drag up → camera drops to see farther up the road.
                        EmberInput.AddCamPitch((_camLast.y - t.position.y)
                            * (80f / UnityEngine.Screen.height));
                        _camLast = t.position;
                    }
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
                        var delta = (t.position - _stickOrigin)
                                    / (UnityEngine.Screen.dpi > 0 ? UnityEngine.Screen.dpi * 0.4f : 160f);
                        EmberInput.TouchMove = Vector2.ClampMagnitude(delta, 1f);
                        EmberInput.TouchActive = true;
                        if (delta.sqrMagnitude > 0.1f) _movedOnce = true;
                    }
                }
            }
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
                _movedOnce = true;
        }

        private void UpdateStickVisual()
        {
            var active = _stickFinger >= 0;
            _stickBase.gameObject.SetActive(active);
            _stickKnob.gameObject.SetActive(active);
            if (!active) return;
            var scale = 720f / UnityEngine.Screen.height;
            var o = _stickOrigin * scale;
            var p = o + Vector2.ClampMagnitude(_stickPos * scale - o, 62);
            _stickBase.anchoredPosition = o;
            _stickKnob.anchoredPosition = p;
        }

        // ------------------------------------------------------------- result

        private void BuildResult()
        {
            Dim(0.92f);
            if (_gm.State == GameManager.Phase.Won) BuildVictory();
            else BuildDefeat();
        }

        private void BuildVictory()
        {
            var r = _gm.MissionResult();
            if (_gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null)
            {
                UiKit.Label(_screenRoot, "VICTORY", 54, UiKit.Pale, new Vector2(0.5f, 1f),
                    new Vector2(0, -140), new Vector2(700, 80), display: true);
                UiKit.Label(_screenRoot,
                    $"{_gm.CurrentDuel.name} FALLS   ·   {(int)_gm.MissionTime}s", 24, UiKit.Ember,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(800, 36));
                ResultButtons(("NEXT OPPONENT", () => _gm.NextDuel()), ("REMATCH", () => _gm.Retry()),
                    ("MENU", () => _gm.OpenMenu()));
                return;
            }

            UiKit.Label(_screenRoot, "LEVEL CLEAR", 34, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -84), new Vector2(700, 50), display: true);

            // Animated rank reveal.
            var rankText = UiKit.Label(_screenRoot, "", 110, UiKit.Ember, new Vector2(0.5f, 1f),
                new Vector2(0, -210), new Vector2(300, 140), display: true);
            StartCoroutine(RankReveal(rankText, r.rank));

            // Star fly-in.
            var stars = _gm.StarsEarned;
            for (var i = 0; i < 3; i++)
            {
                var s = UiKit.Rect(_screenRoot, "star", new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 90f, 10), new Vector2(64, 64));
                var img = UiKit.Img(s, i < stars ? UiKit.Star : UiKit.StarOutline,
                    i < stars ? UiKit.EmberBright : new Color(1, 1, 1, 0.25f));
                if (i < stars) StartCoroutine(StarFlyIn(s, img, 0.9f + i * 0.28f));
                else s.localScale = Vector3.one;
            }

            UiKit.Label(_screenRoot,
                $"RANK {r.rank}   ·   SCORE {r.score}   ·   DMG {Mathf.RoundToInt(_gm.DamageTaken)}" +
                $"   ·   MAX THREAD {(_combat != null ? _combat.MaxCombo : 0)}" +
                (_gm.ShardsEarned > 0 ? $"   ·   ◆ +{_gm.ShardsEarned}" : ""),
                20, UiKit.Dim, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(1000, 30));

            // Debrief cliffhanger.
            if (_gm.CurrentLevel != null && !string.IsNullOrEmpty(_gm.CurrentLevel.debrief))
            {
                var debrief = UiKit.Paragraph(_screenRoot, _gm.CurrentLevel.debrief, 18,
                    new Color(0.8f, 0.82f, 0.86f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, -112), new Vector2(820, 54));
                debrief.fontStyle = FontStyle.Italic;
            }

            if (_gm.CurrentLevel != null && Session.LevelIndex + 1 < Session.Story.Length)
                UiKit.Label(_screenRoot, $"NEXT — {Session.Story[Session.LevelIndex + 1].name}",
                    16, UiKit.Ember, new Vector2(0.5f, 0.5f), new Vector2(0, -162), new Vector2(700, 26));

            ResultButtons(("NEXT LEVEL", () => _gm.NextStoryLevel()), ("REPLAY", () => _gm.Retry()),
                ("SKILLS", () => SetScreen(Screen.Skills)), ("MENU", () => _gm.OpenMenu()));
        }

        private void BuildDefeat()
        {
            UiKit.Label(_screenRoot, "THE LANTERN GUTTERS…", 44, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(900, 64), display: true);
            UiKit.Label(_screenRoot, "but it does not go out.", 24, UiKit.Dim,
                new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(700, 34));
            if (_gm.ModeNow == LaunchMode.Endless)
                UiKit.Label(_screenRoot,
                    $"MARCHED {_gm.DistanceNorth}m   ·   BEST {_gm.BestNorth}m" +
                    (_gm.NewRecord ? "   ·   ★ NEW RECORD ★" : ""),
                    19, UiKit.Ember, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(800, 28));
            ResultButtons(("RISE AGAIN", () => _gm.Retry()), ("MENU", () => _gm.OpenMenu()));
        }

        private void DailyLine()
        {
            if (_gm.DailyShards > 0)
                UiKit.Label(_screenRoot, $"DAILY CHALLENGE COMPLETE — ◆ +{_gm.DailyShards}", 19,
                    new Color(0.5f, 0.85f, 0.55f), new Vector2(0.5f, 0f), new Vector2(0, 170),
                    new Vector2(700, 26));
            if (!string.IsNullOrEmpty(_gm.FeatsLine))
                UiKit.Label(_screenRoot, _gm.FeatsLine + "  ◆ +1", 19, UiKit.EmberBright,
                    new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(900, 26));
        }

        private void ResultButtons(params (string label, System.Action action)[] buttons)
        {
            DailyLine();
            for (var i = 0; i < buttons.Length; i++)
            {
                var x = (i - (buttons.Length - 1) * 0.5f) * 290f;
                var (label, action) = buttons[i];
                UiKit.MakeButton(_screenRoot, label, new Vector2(0.5f, 0f), new Vector2(x, 92),
                    new Vector2(260, 62), () => { Sfx3D.Confirm(); action(); }, 20);
            }
        }

        private IEnumerator RankReveal(Text rankText, string finalRank)
        {
            string[] ladder = { "D", "C", "B", "A", "S" };
            var target = System.Array.IndexOf(ladder, finalRank);
            for (var i = 0; i <= target; i++)
            {
                rankText.text = ladder[i];
                rankText.transform.localScale = Vector3.one * 1.5f;
                Sfx3D.Ui();
                var t = 0f;
                while (t < (i == target ? 0.3f : 0.14f))
                {
                    t += Time.deltaTime;
                    rankText.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, t / 0.14f);
                    yield return null;
                }
            }
            if (finalRank == "S")
            {
                rankText.color = UiKit.EmberBright;
                Sfx3D.Win();
            }
        }

        private IEnumerator StarFlyIn(RectTransform star, Image img, float delay)
        {
            star.localScale = Vector3.zero;
            yield return new WaitForSeconds(delay);
            Sfx3D.Confirm();
            var t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                var k = t / 0.3f;
                star.localScale = Vector3.one * (3f - 2.2f * Mathf.SmoothStep(0, 1, k));
                star.localRotation = Quaternion.Euler(0, 0, 180f * (1f - k));
                yield return null;
            }
            star.localScale = Vector3.one * 0.8f;
            var b = 0f;
            while (b < 0.15f)
            {
                b += Time.deltaTime;
                star.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, b / 0.15f);
                yield return null;
            }
        }
    }
}
