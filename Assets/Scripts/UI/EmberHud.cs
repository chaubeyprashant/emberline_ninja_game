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
        private enum Screen { None, MenuRoot, Story, Fight, Bio, Skills, Codex, Briefing, Hud, Result, Weapons, Arms, March, Forge }

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
        private bool _movedOnce, _struckOnce, _jumpedOnce;
        private RectTransform _stickBase, _stickKnob;

        // HUD widgets updated per frame.
        private Image _hpFill, _senFill, _bossFill, _cleaveCd, _flickerCd, _kunaiCd, _cleaveImg;
        private Image _vignette;
        private float _vignetteT;
        private int _waveStamp = -1;
        private Text _hpLabel, _bossLabel, _waveLabel, _comboText, _objectiveText, _bannerText, _hintText;
        private Image _surgeGlow;
        private CanvasGroup _bannerGroup, _comboGroup;
        private readonly List<Image> _gateIcons = new();
        private RectTransform _bossBar;
        private int _lastCombo;
        private float _comboPop;

        // Pooled enemy markers.
        private class Marker { public RectTransform root; public Image back, fill, arrow, weapon; }
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

        /// <summary>
        /// Render-resolution scale per tier. This is the biggest single GPU lever on
        /// a fill-rate-bound mobile game: dropping the low tier to 75% of native
        /// costs roughly half the shaded pixels and buys the 30fps floor on weak
        /// hardware, while the UI keeps drawing at full device resolution.
        /// </summary>
        private static void ApplyRenderScale(int tier)
        {
            var scale = tier == 0 ? 0.75f : tier == 1 ? 0.9f : 1f;
            var w = Mathf.RoundToInt(UnityEngine.Screen.currentResolution.width * scale);
            var h = Mathf.RoundToInt(UnityEngine.Screen.currentResolution.height * scale);
            if (w <= 0 || h <= 0) return;
            // No-op when already there — SetResolution reallocates the back buffer.
            if (Mathf.Abs(UnityEngine.Screen.width - w) <= 2) return;
            UnityEngine.Screen.SetResolution(w, h, UnityEngine.Screen.fullScreen);
        }

        /// <summary>Off-screen enemy markers drawn at the current tier.</summary>
        public static int MarkerBudget { get; private set; } = 16;

        public static void ApplyGraphicsTier()
        {
            var tier = PlayerPrefs.GetInt("gfx_tier", 1);
            QualitySettings.shadowDistance = tier == 0 ? 18f : tier == 1 ? 30f : 45f;

            // The tier now scales what the fight actually costs to draw, not just
            // shadows: particle counts, the particle ceiling, and how many enemy
            // markers the HUD projects each frame.
            FxPools.Density = tier == 0 ? 0.5f : tier == 1 ? 1f : 1.35f;
            FxPools.MaxParticles = tier == 0 ? 96 : tier == 1 ? 256 : 384;
            MarkerBudget = tier == 0 ? 8 : tier == 1 ? 16 : 20;
            // Lanterns are per-pixel point lights. The default budget of 1 meant
            // only the moon ever reached the toon shader's additive pass, so the
            // arenas lit flat no matter how many lanterns were placed. Each one is
            // an extra pass over every renderer it touches, so the budget is the
            // main GPU dial on this game — keep it tight on the low tier.
            QualitySettings.pixelLightCount = tier == 0 ? 1 : tier == 1 ? 3 : 5;
            QualitySettings.shadowCascades = tier == 2 ? 2 : 1;
            // Soft shadows and the grade pass are the two new realistic-pass costs;
            // both are the first things to go on the 30fps floor.
            QualitySettings.shadows = tier == 0 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = tier == 0 ? ShadowResolution.Low
                : tier == 1 ? ShadowResolution.Medium : ShadowResolution.High;
            CinematicGrade.Enabled = tier > 0;
            QualitySettings.skinWeights = tier == 0 ? SkinWeights.TwoBones : SkinWeights.FourBones;
            ApplyRenderScale(tier);
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
            // Damage feedback: flash the vignette whenever Renzo is hurt.
            if (_health != null) _health.OnHurt += (_, _) => _vignetteT = 1f;
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
                // Deny-list, not allow-list: in the menu phase the player may sit on
                // any menu screen, and only a gameplay screen (or nothing) is
                // routed back to the root. The previous allow-list omitted every
                // screen added after it was written, so March, Forge, Weapons and
                // Arms were rebuilt and discarded within a single frame.
                GameManager.Phase.Menu =>
                    _screen is Screen.None or Screen.Hud or Screen.Briefing or Screen.Result
                        ? Screen.MenuRoot : _screen,
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
                case Screen.Weapons: BuildWeaponSelect(); break;
                case Screen.Arms: BuildArms(); break;
                case Screen.March: BuildMarchBriefing(); break;
                case Screen.Forge: BuildForge(); break;
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
                ("MARCH", Endless.RunStats.BestScore > 0
                        ? $"BEST {Endless.RunStats.BestScore} PTS"
                        : "THE ROAD NORTH",
                    () => SetScreen(Screen.March)),
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
                $"      SKILLS {SkillTree.OwnedCount}/{SkillTree.Nodes.Count}" +
                $"      {Difficulty.Name}", 21,
                UiKit.EmberBright, new Vector2(0.5f, 0f), new Vector2(0, 158), new Vector2(900, 30));

            // Weapon selector — opens the full armoury rather than blind-cycling,
            // now that weapons differ in kind and not just in numbers.
            UiKit.MakeButton(_screenRoot, WeaponLine(), new Vector2(0.5f, 0f),
                new Vector2(0, 96), new Vector2(430, 56), () => SetScreen(Screen.Weapons), 18);

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
                18, UiKit.Dim, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(700, 26));

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

            UiKit.MakeButton(_screenRoot, "ARMS OF THE ROAD", new Vector2(1f, 1f),
                new Vector2(-160, -80), new Vector2(280, 50), () => SetScreen(Screen.Arms), 16);

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
            if (w == null) return "WEAPON — NONE";
            var locked = 0;
            foreach (var other in Loadout.All)
                if (!Loadout.IsUnlocked(other)) locked++;
            // Archetype tag up front: the roster is no longer all swords, and the
            // family tells you how it plays before the name does.
            return $"{ArchetypeLabel(w.archetype)} — {w.displayName}"
                   + (locked > 0 ? $"   ({locked} LOCKED)" : "   ↻");
        }

        /// <summary>
        /// Display name for an enemy's weapon — used on the boss intro card, so a
        /// boss announces what it is holding as well as who it is.
        /// </summary>
        public static string WeaponLabel(EnemyWeapon w) => w switch
        {
            EnemyWeapon.Daggers => "TWIN KNIVES",
            EnemyWeapon.Sword => "BLADE",
            EnemyWeapon.Axe => "GREATAXE",
            EnemyWeapon.Spear => "PIKE",
            EnemyWeapon.Crossbow => "CROSSBOW",
            EnemyWeapon.Claws => "CLAWS",
            EnemyWeapon.Bomb => "POWDER CHARGE",
            _ => "",
        };

        /// <summary>Glyph name in UiKit.Icon for an enemy weapon.</summary>
        public static string WeaponIcon(EnemyWeapon w) => w switch
        {
            EnemyWeapon.Daggers => "kunai",
            EnemyWeapon.Axe => "axe",
            EnemyWeapon.Spear => "spear",
            EnemyWeapon.Crossbow => "bow",
            EnemyWeapon.Claws => "claws",
            EnemyWeapon.Bomb => "bomb",
            _ => "sword",
        };

        /// <summary>Short family label for the weapon cycle button.</summary>
        private static string ArchetypeLabel(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Daggers => "DAGGERS",
            WeaponArchetype.Thrown => "BOMB",
            WeaponArchetype.Ranged => "BOW",
            _ => "BLADE",
        };

        /// <summary>Column order for the skill screen.</summary>
        private static readonly string[] SkillBranches =
            { "COMBAT", "DEFENSE", "EMBER", "TRAVERSAL" };

        /// <summary>
        /// The armoury: one card per weapon with its glyph, family, blurb and
        /// headline numbers. Replaces the old cycle button — with four families
        /// that play differently, you need to see what you are choosing between.
        /// </summary>
        private void BuildWeaponSelect()
        {
            Dim(0.85f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "THE ARMOURY", 36, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(700, 50), display: true);
            UiKit.Label(_screenRoot, "Weapons unlock as the story opens up", 17, UiKit.Dim,
                new Vector2(0.5f, 1f), new Vector2(0, -94), new Vector2(700, 24));

            var all = Loadout.All;
            var current = Loadout.Current;
            for (var i = 0; i < all.Length; i++)
            {
                var w = all[i];
                var col = i % 3;
                var row = i / 3;
                var pos = new Vector2((col - 1) * 400f, 60f - row * 230f);
                var card = UiKit.Rect(_screenRoot, "Wpn_" + w.id, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(380, 214));
                var unlocked = Loadout.IsUnlocked(w);
                var equipped = current != null && current.id == w.id;
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    equipped ? new Color(0.22f, 0.13f, 0.10f)
                    : unlocked ? new Color(0.13f, 0.135f, 0.17f)
                    : new Color(0.085f, 0.09f, 0.115f), sliced: true);

                // Prop preview: the same glyph language the HUD and boss cards use.
                // Sits in its own top-left column so the blurb can run full width.
                var iconRt = UiKit.Rect(card, "Preview", new Vector2(0f, 1f),
                    new Vector2(74f, -52f), new Vector2(58f, 58f));
                UiKit.Img(iconRt, UiKit.Icon(ArchetypeIcon(w.archetype)),
                    unlocked ? new Color(0.96f, 0.95f, 0.92f, 0.95f) : new Color(1, 1, 1, 0.18f));

                UiKit.Label(card, w.displayName, 19,
                    unlocked ? UiKit.EmberBright : new Color(1, 1, 1, 0.3f),
                    new Vector2(0.5f, 1f), new Vector2(44, -34), new Vector2(266, 26), display: true);
                UiKit.Label(card, $"{ArchetypeLabel(w.archetype)}  ·  {CleaveLabel(w.cleaveStyle)}"
                                  + $"  ·  {w.strikeChainLength}-HIT", 13, UiKit.Sen,
                    new Vector2(0.5f, 1f), new Vector2(44, -60), new Vector2(266, 20));

                if (unlocked)
                {
                    UiKit.Paragraph(card, w.blurb, 14, new Color(0.82f, 0.84f, 0.86f),
                        new Vector2(0.5f, 1f), new Vector2(0, -114), new Vector2(336, 60));
                    UiKit.Label(card, equipped ? "EQUIPPED" : "TAP TO EQUIP", 14,
                        equipped ? UiKit.EmberBright : UiKit.Dim,
                        new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(300, 22));
                    if (!equipped)
                    {
                        img.raycastTarget = true;
                        var btn = card.gameObject.AddComponent<Button>();
                        btn.targetGraphic = img;
                        var pick = w;
                        btn.onClick.AddListener(() =>
                        {
                            Loadout.Select(pick);
                            Sfx3D.Confirm();
                            SetScreen(Screen.Weapons); // rebuild to move the highlight
                        });
                    }
                }
                else
                {
                    UiKit.Label(card, $"LOCKED — CLEAR LEVEL {w.unlockLevel}", 15,
                        new Color(1, 1, 1, 0.3f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, -20), new Vector2(340, 24));
                }
            }
            BackButton();
        }

        /// <summary>
        /// "Arms of the Road": the reference page — every weapon Renzo can carry
        /// and every kind of thing that carries one back.
        /// </summary>
        private void BuildArms()
        {
            Dim(0.85f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "ARMS OF THE ROAD", 34, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(800, 46), display: true);

            UiKit.Label(_screenRoot, "YOUR KIT", 16, UiKit.Ember, new Vector2(0f, 1f),
                new Vector2(210, -96), new Vector2(300, 22), align: TextAnchor.MiddleLeft);
            var all = Loadout.All;
            for (var i = 0; i < all.Length; i++)
            {
                var w = all[i];
                var row = UiKit.Rect(_screenRoot, "Arm_" + w.id, new Vector2(0f, 1f),
                    new Vector2(330, -134 - i * 46), new Vector2(560, 42));
                var icon = UiKit.Rect(row, "i", new Vector2(0f, 0.5f),
                    new Vector2(24, 0), new Vector2(30, 30));
                UiKit.Img(icon, UiKit.Icon(ArchetypeIcon(w.archetype)),
                    Loadout.IsUnlocked(w) ? UiKit.Pale : new Color(1, 1, 1, 0.2f));
                UiKit.Label(row, Loadout.IsUnlocked(w) ? w.displayName : "— LOCKED —", 15,
                    Loadout.IsUnlocked(w) ? UiKit.EmberBright : new Color(1, 1, 1, 0.25f),
                    new Vector2(0f, 0.5f), new Vector2(190, 8), new Vector2(300, 20),
                    align: TextAnchor.MiddleLeft);
                UiKit.Label(row, $"{ArchetypeLabel(w.archetype)} · {CleaveLabel(w.cleaveStyle)}",
                    12, UiKit.Dim, new Vector2(0f, 0.5f), new Vector2(190, -10),
                    new Vector2(300, 18), align: TextAnchor.MiddleLeft);
            }

            UiKit.Label(_screenRoot, "WHAT WALKS THE ROAD", 16, UiKit.Ember, new Vector2(1f, 1f),
                new Vector2(-470, -96), new Vector2(340, 22), align: TextAnchor.MiddleLeft);
            var kinds = (EnemyKind[])System.Enum.GetValues(typeof(EnemyKind));
            for (var i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i];
                var row = UiKit.Rect(_screenRoot, "Foe_" + k, new Vector2(1f, 1f),
                    new Vector2(-330, -134 - i * 42), new Vector2(560, 38));
                var known = StoryMemory.HasLore(k);
                var icon = UiKit.Rect(row, "i", new Vector2(0f, 0.5f),
                    new Vector2(24, 0), new Vector2(28, 28));
                UiKit.Img(icon, UiKit.Icon(WeaponIcon(KindWeapon(k))),
                    known ? UiKit.Pale : new Color(1, 1, 1, 0.18f));
                UiKit.Label(row, known ? KindName(k) : "— UNKNOWN —", 15,
                    known ? UiKit.Pale : new Color(1, 1, 1, 0.22f),
                    new Vector2(0f, 0.5f), new Vector2(190, 7), new Vector2(300, 20),
                    align: TextAnchor.MiddleLeft);
                UiKit.Label(row, known ? WeaponLabel(KindWeapon(k)) : "", 12, UiKit.Dim,
                    new Vector2(0f, 0.5f), new Vector2(190, -10), new Vector2(300, 18),
                    align: TextAnchor.MiddleLeft);
            }
            BackButton();
        }

        /// <summary>Weapon each kind fights with — mirrors the prefab assignments.</summary>
        private static EnemyWeapon KindWeapon(EnemyKind k) => k switch
        {
            EnemyKind.Bandit => EnemyWeapon.Daggers,
            EnemyKind.Ranged => EnemyWeapon.Crossbow,
            EnemyKind.Chief => EnemyWeapon.Axe,
            EnemyKind.Shade => EnemyWeapon.Claws,
            EnemyKind.RaiderAxe => EnemyWeapon.Axe,
            EnemyKind.PikeGuard => EnemyWeapon.Spear,
            EnemyKind.Bomber => EnemyWeapon.Bomb,
            _ => EnemyWeapon.Sword,
        };

        private static string KindName(EnemyKind k) => k switch
        {
            EnemyKind.Bandit => "RAIDER",
            EnemyKind.Ranged => "WEAVER",
            EnemyKind.Chief => "GORO",
            EnemyKind.Shade => "SHADE",
            EnemyKind.Kagachi => "KAGACHI",
            EnemyKind.Jin => "JIN KUROGANE",
            EnemyKind.RaiderAxe => "AXE RAIDER",
            EnemyKind.PikeGuard => "PIKE GUARD",
            _ => "BOMBER",
        };

        /// <summary>Glyph for a hero weapon family.</summary>
        private static string ArchetypeIcon(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Daggers => "kunai",
            WeaponArchetype.Thrown => "bomb",
            WeaponArchetype.Ranged => "bow",
            _ => "sword",
        };

        private static string CleaveLabel(CleaveStyle c) => c switch
        {
            CleaveStyle.Spin => "SPIN",
            CleaveStyle.Ground => "GROUND",
            CleaveStyle.FanShot => "FAN SHOT",
            _ => "SLASH",
        };

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
                // Four branches now, so columns come from the order list rather
                // than a hardcoded ternary that collapsed anything new onto EMBER.
                var colIdx = System.Array.IndexOf(SkillBranches, node.branch);
                if (colIdx < 0) colIdx = SkillBranches.Length - 1;
                var pos = new Vector2((colIdx - (SkillBranches.Length - 1) * 0.5f) * 318f,
                    90f - row * 140f);

                if (row == 0)
                    UiKit.Label(_screenRoot, node.branch, 19, UiKit.Ember,
                        new Vector2(0.5f, 0.5f), pos + new Vector2(0, 82), new Vector2(300, 26));

                var card = UiKit.Rect(_screenRoot, "Skill_" + node.id, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(300, 118));
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

        /// <summary>
        /// March briefing: the record book, the modifier wager, and the way in.
        /// Modifiers are chosen here rather than mid-run because the score
        /// multiplier they buy has to be fixed before the run that earns it.
        /// </summary>
        private void BuildMarchBriefing()
        {
            Dim(0.88f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "THE ROAD NORTH", 36, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(800, 48), display: true);
            UiKit.Label(_screenRoot,
                "Seven countries. No two marches the same. The road does not end.",
                16, UiKit.Dim, new Vector2(0.5f, 1f), new Vector2(0, -84), new Vector2(900, 22));

            // Record book.
            var recs = $"BEST {Endless.RunStats.BestScore} PTS   ·   DEPTH {Endless.RunStats.BestDepth}"
                       + $"   ·   {Endless.RunStats.TimeText(Endless.RunStats.BestTime)}"
                       + $"   ·   {Endless.RunStats.BestKills} KILLS"
                       + $"   ·   ×{Endless.RunStats.BestComboEver} THREAD";
            UiKit.Label(_screenRoot, recs, 15, UiKit.Ember,
                new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(1000, 22));
            UiKit.Label(_screenRoot,
                $"{Endless.RunStats.TotalRuns} MARCHES   ·   {Endless.RunStats.TotalKills} DEAD"
                + $"   ·   ¤ {Core.Wallet.Ryo} RYO",
                14, UiKit.Sen, new Vector2(0.5f, 1f), new Vector2(0, -134), new Vector2(1000, 20));

            // Modifier grid: four columns, two rows.
            var all = Endless.RunModifiers.All;
            for (var i = 0; i < all.Length; i++)
            {
                var d = all[i];
                var col = i % 4;
                var row = i / 4;
                var pos = new Vector2((col - 1.5f) * 300f, 44f - row * 150f);
                var card = UiKit.Rect(_screenRoot, "Mod_" + d.Mod, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(286, 138));
                var on = Endless.RunModifiers.IsSelected(d.Mod);
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    on ? new Color(0.24f, 0.14f, 0.10f) : new Color(0.12f, 0.125f, 0.155f),
                    sliced: true);
                img.raycastTarget = true;

                UiKit.Label(card, d.Name, 17, on ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(258, 22), display: true);
                UiKit.Paragraph(card, d.Blurb, 13,
                    on ? new Color(0.86f, 0.84f, 0.8f) : UiKit.Dim,
                    new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(250, 52));
                UiKit.Label(card, $"+{Mathf.RoundToInt(d.ScoreBonus * 100f)}% SCORE", 13,
                    on ? UiKit.Ember : UiKit.Sen,
                    new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(258, 20));

                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var mod = d.Mod;
                btn.onClick.AddListener(() =>
                {
                    Endless.RunModifiers.Toggle(mod);
                    Sfx3D.Ui();
                    SetScreen(Screen.March); // rebuild so the wager line updates
                });
            }

            var mul = Endless.RunModifiers.ScoreMultiplier(Endless.RunModifiers.Selected);
            UiKit.Label(_screenRoot, $"WAGER  ×{mul:0.00}", 24, UiKit.EmberBright,
                new Vector2(0.5f, 0f), new Vector2(0, 148), new Vector2(600, 30), display: true);
            UiKit.Label(_screenRoot, Endless.RunModifiers.Describe(Endless.RunModifiers.Selected),
                14, UiKit.Sen, new Vector2(0.5f, 0f), new Vector2(0, 124), new Vector2(1000, 20));

            UiKit.MakeButton(_screenRoot, "MARCH", new Vector2(0.5f, 0f), new Vector2(-150, 56),
                new Vector2(260, 62), () => { Sfx3D.Confirm(); _gm.LaunchEndless(); }, 22);
            UiKit.MakeButton(_screenRoot, "THE FORGE", new Vector2(0.5f, 0f), new Vector2(150, 56),
                new Vector2(260, 62), () => { Sfx3D.Ui(); SetScreen(Screen.Forge); }, 20);
            BackButton();
        }

        /// <summary>
        /// The Forge: what Ryo is for. Weapon upgrade tracks on the left, cloth
        /// dyes on the right — one screen, because two half-empty shops read as
        /// padding rather than depth.
        /// </summary>
        private void BuildForge()
        {
            Dim(0.88f);
            BuildEmberLayer();
            UiKit.Label(_screenRoot, "THE FORGE", 34, UiKit.Pale,
                new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(700, 46), display: true);
            UiKit.Label(_screenRoot, $"¤ {Core.Wallet.Ryo} RYO", 20, UiKit.EmberBright,
                new Vector2(0.5f, 1f), new Vector2(0, -82), new Vector2(600, 26));

            var w = Loadout.Current;
            var wid = w != null ? w.id : "katana";
            UiKit.Label(_screenRoot, w != null ? w.displayName : "NO WEAPON", 19, UiKit.Ember,
                new Vector2(0f, 1f), new Vector2(300, -122), new Vector2(460, 24), display: true);

            var tracks = new[]
            {
                (Core.WeaponUpgrades.Track.Damage, "EDGE", "+6% damage per point"),
                (Core.WeaponUpgrades.Track.Reach, "REACH", "+4% range per point"),
                (Core.WeaponUpgrades.Track.Speed, "TEMPO", "-4% recovery per point"),
            };
            for (var i = 0; i < tracks.Length; i++)
            {
                var (track, name, blurb) = tracks[i];
                var lv = Core.WeaponUpgrades.Level(wid, track);
                var cost = Core.WeaponUpgrades.Cost(wid, track);
                var maxed = lv >= Core.WeaponUpgrades.MaxLevel;
                var afford = !maxed && Core.Wallet.CanAfford(cost);

                var card = UiKit.Rect(_screenRoot, "Up_" + name, new Vector2(0f, 1f),
                    new Vector2(300, -206 - i * 116), new Vector2(460, 100));
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    maxed ? new Color(0.20f, 0.15f, 0.10f)
                    : afford ? new Color(0.135f, 0.14f, 0.175f)
                    : new Color(0.10f, 0.10f, 0.125f), sliced: true);

                UiKit.Label(card, name, 18, maxed ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0f, 1f), new Vector2(78, -24), new Vector2(160, 24), display: true);
                UiKit.Label(card, blurb, 13, UiKit.Sen,
                    new Vector2(0f, 1f), new Vector2(120, -48), new Vector2(250, 20));
                UiKit.Label(card, Pips(lv, Core.WeaponUpgrades.MaxLevel), 18, UiKit.Ember,
                    new Vector2(1f, 1f), new Vector2(-90, -24), new Vector2(150, 24));
                UiKit.Label(card, maxed ? "MASTERED" : $"¤ {cost}", 15,
                    maxed ? UiKit.EmberBright : afford ? UiKit.Pale : new Color(1, 1, 1, 0.32f),
                    new Vector2(1f, 0f), new Vector2(-80, 22), new Vector2(140, 22));

                if (maxed || !afford) continue;
                img.raycastTarget = true;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var t = track;
                btn.onClick.AddListener(() =>
                {
                    if (Core.WeaponUpgrades.TryBuy(wid, t)) Sfx3D.Confirm(); else Sfx3D.Error();
                    SetScreen(Screen.Forge);
                });
            }

            // Dyes.
            UiKit.Label(_screenRoot, "CLOTH", 19, UiKit.Ember,
                new Vector2(1f, 1f), new Vector2(-300, -122), new Vector2(460, 24), display: true);
            var sets = Core.Cosmetics.All;
            var cur = Core.Cosmetics.Current;
            for (var i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                var owned = Core.Cosmetics.IsOwned(set);
                var equipped = cur.Id == set.Id;
                var col = i % 2;
                var row = i / 2;
                var card = UiKit.Rect(_screenRoot, "Cos_" + set.Id, new Vector2(1f, 1f),
                    new Vector2(-460 + col * 230f, -196 - row * 112f), new Vector2(220, 96));
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    equipped ? new Color(0.22f, 0.13f, 0.10f)
                    : owned ? new Color(0.135f, 0.14f, 0.175f)
                    : new Color(0.10f, 0.10f, 0.125f), sliced: true);

                // Swatch: the dye and its accent, so the card shows the thing itself.
                var sw = UiKit.Rect(card, "Swatch", new Vector2(0f, 0.5f),
                    new Vector2(34, 6), new Vector2(34, 34));
                UiKit.Img(sw, null, set.Dye);
                var ac = UiKit.Rect(card, "Accent", new Vector2(0f, 0.5f),
                    new Vector2(34, -24), new Vector2(34, 12));
                UiKit.Img(ac, null, set.Accent);

                UiKit.Label(card, set.Name, 15,
                    owned ? UiKit.Pale : new Color(1, 1, 1, 0.34f),
                    new Vector2(0.5f, 1f), new Vector2(28, -22), new Vector2(150, 22), display: true);
                UiKit.Label(card, equipped ? "WORN" : owned ? "TAP TO WEAR" : $"¤ {set.Cost}", 13,
                    equipped ? UiKit.EmberBright
                    : owned ? UiKit.Dim
                    : Core.Wallet.CanAfford(set.Cost) ? UiKit.Pale : new Color(1, 1, 1, 0.3f),
                    new Vector2(0.5f, 0f), new Vector2(28, 18), new Vector2(150, 20));

                if (equipped) continue;
                img.raycastTarget = true;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var pick = set;
                btn.onClick.AddListener(() =>
                {
                    if (Core.Cosmetics.IsOwned(pick)) { Core.Cosmetics.Select(pick); Sfx3D.Confirm(); }
                    else if (Core.Cosmetics.TryBuy(pick)) { Core.Cosmetics.Select(pick); Sfx3D.Confirm(); }
                    else Sfx3D.Error();
                    SetScreen(Screen.Forge);
                });
            }

            UiKit.MakeButton(_screenRoot, "← THE ROAD", new Vector2(0f, 0f), new Vector2(130, 52),
                new Vector2(200, 58), () => { Sfx3D.Back(); SetScreen(Screen.March); }, 18);
        }

        /// <summary>Filled/empty pips for an upgrade track.</summary>
        private static string Pips(int filled, int max)
        {
            var s = "";
            for (var i = 0; i < max; i++) s += i < filled ? "◆" : "◇";
            return s;
        }

        private void BackButton() =>
            UiKit.MakeButton(_screenRoot, "← BACK", new Vector2(0f, 0f), new Vector2(110, 52),
                new Vector2(160, 58), () => { Sfx3D.Back(); SetScreen(Screen.MenuRoot); }, 18);

        // ------------------------------------------------------------ settings

        private void ToggleSettings()
        {
            if (_settingsOpen) { CloseSettings(); return; }
            _settingsOpen = true;
            // This overlay is the pause menu. It used to leave the game running
            // underneath, so opening it mid-fight meant taking hits blind.
            if (_gm != null && _gm.State == GameManager.Phase.Playing)
            {
                Player.CombatController.TimeFrozen = true;
                Time.timeScale = 0f;
            }
            _settingsRoot = UiKit.Group(_root, "Settings");
            UiKit.Img(_settingsRoot, null, new Color(0, 0, 0, 0.7f)).raycastTarget = true;
            var panel = UiKit.Rect(_settingsRoot, "Panel", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560, 560));
            UiKit.Img(panel, UiKit.PanelSprite, new Color(0.1f, 0.105f, 0.14f, 0.98f), sliced: true);
            UiKit.Label(panel, "SETTINGS", 30, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -42), new Vector2(400, 40), display: true);

            Text sfxLabel = null, musicLabel = null, gfxLabel = null, diffLabel = null;
            Text diffBlurb = null, fpsLabel = null;
            void Refresh()
            {
                sfxLabel.text = $"SFX VOLUME   {Mathf.RoundToInt(Sfx3D.SfxVolume * 100)}%";
                musicLabel.text = $"MUSIC VOLUME   {Mathf.RoundToInt(Sfx3D.MusicVolume * 100)}%";
                gfxLabel.text = "GRAPHICS   " + GraphicsTier switch { 0 => "LOW", 1 => "MEDIUM", _ => "HIGH" };
                if (diffLabel != null) diffLabel.text = "DIFFICULTY   " + Difficulty.Name;
                if (diffBlurb != null) diffBlurb.text = Difficulty.Now.Blurb;
                if (fpsLabel != null)
                    fpsLabel.text = $"FRAME RATE   {PerfGovernor.GameplayFps} FPS"
                                    + (PerfGovernor.ThermalStep > 0 ? "   (HOT)" : "");
            }

            void Row(float y, System.Func<Text> label, System.Action minus, System.Action plus)
            {
                UiKit.MakeButton(panel, "−", new Vector2(0.5f, 0.5f), new Vector2(-190, y),
                    new Vector2(64, 56), () => { minus(); Refresh(); }, 26);
                UiKit.MakeButton(panel, "+", new Vector2(0.5f, 0.5f), new Vector2(190, y),
                    new Vector2(64, 56), () => { plus(); Refresh(); }, 26);
            }

            sfxLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, 150), new Vector2(280, 30));
            Row(150, () => sfxLabel, () => Sfx3D.SfxVolume -= 0.1f, () => Sfx3D.SfxVolume += 0.1f);
            musicLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, 88), new Vector2(280, 30));
            Row(88, () => musicLabel, () => Sfx3D.MusicVolume -= 0.1f, () => Sfx3D.MusicVolume += 0.1f);
            gfxLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, 26), new Vector2(280, 30));
            Row(26, () => gfxLabel, () => GraphicsTier--, () => GraphicsTier++);

            // Difficulty scales enemies as they spawn, so changing it mid-mission
            // would only affect the next wave — confusing at exactly the moment a
            // player reaches for it. Editable from the menu; read-only in the pause
            // overlay, where LEAVE MISSION is the way to act on it.
            // Frame rate. The single biggest lever on how warm the phone gets, so
            // it sits in the same list as the other comfort settings rather than
            // buried somewhere "advanced".
            fpsLabel = UiKit.Label(panel, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, -110), new Vector2(320, 30));
            Row(-110, () => fpsLabel,
                () => PerfGovernor.GameplayFps = 30,
                () => PerfGovernor.GameplayFps = 60);

            var inPlay = _gm != null && _gm.State == GameManager.Phase.Playing;
            diffLabel = UiKit.Label(panel, "", 20,
                inPlay ? UiKit.Dim : UiKit.Pale, new Vector2(0.5f, 0.5f),
                new Vector2(0, -38), new Vector2(300, 30));
            if (inPlay)
                diffBlurb = UiKit.Label(panel, "", 14, new Color(1, 1, 1, 0.35f),
                    new Vector2(0.5f, 0.5f), new Vector2(0, -62), new Vector2(460, 22));
            else
            {
                Row(-38, () => diffLabel, () => Difficulty.Step(-1), () => Difficulty.Step(1));
                diffBlurb = UiKit.Label(panel, "", 14, UiKit.Sen,
                    new Vector2(0.5f, 0.5f), new Vector2(0, -66), new Vector2(460, 22));
            }
            Refresh();

            // Leaving a mission. Without this the only way out of a level was to
            // win or to die — the pause menu could adjust the volume but not let
            // go of the game. Two-step, because a single mistaken tap should not
            // throw away a run in progress.
            if (_gm != null && _gm.State == GameManager.Phase.Playing)
            {
                Text quitLabel = null;
                var armed = false;
                var quit = UiKit.MakeButton(panel, "LEAVE MISSION", new Vector2(0.5f, 0f),
                    new Vector2(-118, 40), new Vector2(228, 56), () =>
                    {
                        if (!armed)
                        {
                            armed = true;
                            if (quitLabel != null) quitLabel.text = "SURE? TAP AGAIN";
                            return;
                        }
                        // Hand time back before leaving, or the menu inherits a
                        // frozen timeScale and the whole game appears to hang.
                        Player.CombatController.TimeFrozen = false;
                        Time.timeScale = 1f;
                        CloseSettings();
                        _gm.OpenMenu();
                    }, 17);
                quitLabel = quit.GetComponentInChildren<Text>();

                UiKit.MakeButton(panel, "RESUME", new Vector2(0.5f, 0f), new Vector2(118, 40),
                    new Vector2(200, 56), CloseSettings, 20);
            }
            else
            {
                UiKit.MakeButton(panel, "CLOSE", new Vector2(0.5f, 0f), new Vector2(0, 40),
                    new Vector2(200, 56), CloseSettings, 20);
            }
        }

        private void CloseSettings()
        {
            _settingsOpen = false;
            if (Player.CombatController.TimeFrozen)
            {
                Player.CombatController.TimeFrozen = false;
                Time.timeScale = 1f;
            }
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
                objective = l.objective switch
                {
                    MissionObjective.Escort =>
                        "OBJECTIVE — Walk Yotsu to the temple. If the flame goes out, the night is lost.",
                    MissionObjective.Stealth =>
                        "OBJECTIVE — Cut them down unseen. Strike from behind; an alarm costs you the rank.",
                    MissionObjective.Chase =>
                        "OBJECTIVE — Run them down before they scatter.",
                    _ => l.holdSeconds > 0f
                        ? $"OBJECTIVE — Hold the road for {Mathf.RoundToInt(l.holdSeconds)} seconds. ★★★ for rank A or higher."
                        : $"OBJECTIVE — Clear {l.waves.Length} wave{(l.waves.Length > 1 ? "s" : "")}. ★★★ for rank A or higher.",
                };
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

            // Duels let you pick your own handicap before committing; harder terms
            // pay more shards, so a won duel stays worth replaying.
            if (_gm.ModeNow == LaunchMode.Duel)
            {
                var mod = Session.CurrentDuelModifier;
                var bonus = mod.bonusShards > 0 ? $"   ◆ +{mod.bonusShards}" : "";
                UiKit.Label(_screenRoot, mod.desc, 16, UiKit.Dim, new Vector2(0.5f, 0f),
                    new Vector2(0, 232), new Vector2(700, 24));
                UiKit.MakeButton(_screenRoot, $"TERMS — {mod.name}{bonus}", new Vector2(0.5f, 0f),
                    new Vector2(0, 180), new Vector2(420, 56), () =>
                    {
                        Session.DuelModifierIndex =
                            (Session.DuelModifierIndex + 1) % Session.DuelModifiers.Length;
                        SetScreen(Screen.Briefing); // rebuild to show the new terms
                    }, 18);
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

            // Pause. The settings overlay is the pause menu, but it was only
            // reachable from the main menu — so during a mission there was no way
            // to stop the game at all. Small and top-left of the clock, where it
            // cannot be hit by accident while the thumbs are on the controls.
            UiKit.MakeButton(_screenRoot, "II", new Vector2(1, 1),
                new Vector2(-330, -34), new Vector2(64, 56), ToggleSettings, 20);

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

            // Damage vignette — sits above the world, below the controls.
            var vig = UiKit.Group(_screenRoot, "HurtVignette");
            _vignette = UiKit.Img(vig, UiKit.Vignette, new Color(0.9f, 0.12f, 0.1f, 0f));
            _vignette.raycastTarget = false;

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

        /// <summary>
        /// Glyph for a combat verb, bent by the equipped weapon: the strike and
        /// cleave buttons should show what Renzo is actually about to do, and the
        /// throw slot shows the ammunition he actually carries.
        /// </summary>
        private static string VerbIcon(string verb)
        {
            var w = Loadout.Current;
            var arch = w != null ? w.archetype : WeaponArchetype.Blade;
            switch (verb)
            {
                case "STRIKE":
                    return arch switch
                    {
                        WeaponArchetype.Ranged => "bow",     // strike fires a bolt
                        WeaponArchetype.Daggers => "kunai",
                        WeaponArchetype.Thrown => "bomb",
                        _ => "strike",
                    };
                case "CLEAVE":
                    return w != null && w.cleaveStyle == CleaveStyle.FanShot ? "bow"
                        : w != null && w.cleaveStyle == CleaveStyle.Ground ? "bomb"
                        : "cleave";
                case "KUNAI":
                    return w != null && w.replacesKunaiWithThrown
                           && w.thrownId == "Bomb" ? "bomb" : "kunai";
                default:
                    return verb.ToLowerInvariant();
            }
        }

        private void BuildCombatButtons()
        {
            Image CombatButton(string label, Vector2 pos, float size, Color color,
                System.Action press, System.Action<bool> hold = null)
            {
                var rt = UiKit.Rect(_screenRoot, "Cb_" + label, new Vector2(1, 0), pos,
                    new Vector2(size, size));
                var img = UiKit.Img(rt, UiKit.ButtonRound, new Color(color.r, color.g, color.b, 0.55f));
                img.raycastTarget = true;
                // Icon glyph rather than a word: it reads at a glance mid-fight and
                // doesn't crowd the smaller buttons the way five characters did.
                var iconRt = UiKit.Rect(rt, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero,
                    Vector2.one * (size * 0.52f));
                UiKit.Img(iconRt, UiKit.Icon(VerbIcon(label)),
                    new Color(0.96f, 0.95f, 0.92f, 0.95f));
                var punch = rt.gameObject.AddComponent<UiKit.ButtonPunch>();
                // PointerDown (not click) — combat inputs must fire on touch, not release.
                var trigger = rt.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ =>
                {
                    press();
                    punch.Punch();
                    _struckOnce = true;
                    if (hold != null) hold(true);
                });
                trigger.triggers.Add(entry);
                if (hold != null)
                {
                    // Release matters for the deflect stance, so track both edges.
                    foreach (var id in new[] { EventTriggerType.PointerUp, EventTriggerType.PointerExit })
                    {
                        var up = new EventTrigger.Entry { eventID = id };
                        up.callback.AddListener(_ => hold(false));
                        trigger.triggers.Add(up);
                    }
                }
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
                new Color(0.24f, 0.42f, 0.49f), EmberInput.PressCleave,
                hold: EmberInput.SetCleaveHeld); // holding keeps the deflect stance open
            _cleaveImg = _cleaveCd.transform.parent.GetComponent<Image>();
            _flickerCd = CombatButton("FLICKER", new Vector2(-100, 252), 100,
                new Color(0.59f, 0.63f, 0.67f), EmberInput.PressFlicker);
            var surgeCd = CombatButton("SURGE", new Vector2(-232, 216), 88, UiKit.Ember,
                EmberInput.PressSurge);
            _surgeGlow = surgeCd.transform.parent.GetComponent<Image>();
            _kunaiCd = CombatButton("KUNAI", new Vector2(-348, 156), 88,
                new Color(0.42f, 0.5f, 0.62f), EmberInput.PressKunai);
            CombatButton("JUMP", new Vector2(-196, 336), 88,
                new Color(0.34f, 0.46f, 0.44f), () => { EmberInput.PressJump(); _jumpedOnce = true; });
            // Crouch is a hold: press-and-hold to stay low, quiet and hard to see.
            CombatButton("CROUCH", new Vector2(-452, 232), 84,
                new Color(0.30f, 0.34f, 0.42f), () => EmberInput.SetCrouchHeld(true),
                held => EmberInput.SetCrouchHeld(held));
            CombatButton("TARGET", new Vector2(-352, 292), 76,
                new Color(0.46f, 0.4f, 0.32f), EmberInput.PressCycleTarget);
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
                // Guard stance reads on the cleave button itself: it lights pale
                // while deflecting, dims while the stance is recharging.
                if (_cleaveImg != null)
                    _cleaveImg.color = _combat.Deflecting
                        ? new Color(0.75f, 0.9f, 1f, 0.9f)
                        : new Color(0.24f, 0.42f, 0.49f, _combat.DeflectCd01 > 0f ? 0.35f : 0.55f);
            }
            if (_motor != null) _flickerCd.fillAmount = _motor.FlickerCd01;

            // Rebuilt only when the visible second (or wave/distance) actually
            // changes. This ran string interpolation every frame — ~60 short-lived
            // strings a second for a label that changes once a second.
            var t = (int)_gm.MissionTime;
            var stamp = t * 1000 + _gm.WaveIndex * 7 + (_gm.ModeNow == LaunchMode.Endless
                ? _gm.DistanceNorth : 0);
            if (stamp != _waveStamp) RebuildWaveLabel(t, stamp);

            UpdateVignette();
            UpdateCombo();
            UpdateBossBar();
            UpdateBannerObjective();
            UpdateMarkers();
            UpdateStickVisual();
        }

        private void RebuildWaveLabel(int t, int stamp)
        {
            _waveStamp = stamp;
            _waveLabel.text = _gm.ModeNow switch
            {
                // The march reads score-first: it is the number the run is played
                // for, and distance alone stopped describing a run once encounters
                // replaced the old distance-scaled packs.
                LaunchMode.Endless =>
                    $"{Endless.RunStats.Score} PTS   DEPTH {Endless.RunStats.Depth}"
                    + $"   {t / 60}:{t % 60:00}"
                    + (_gm.Run != null ? $"   {_gm.Run.RegionName}" : "")
                    + (Endless.RunStats.CurrentCombo >= 5
                        ? $"   ×{Endless.RunStats.CurrentCombo}" : ""),
                LaunchMode.Duel => $"DUEL   {t / 60}:{t % 60:00}",
                // A staged mission has stages, not waves; showing "WAVE 1/2" over
                // a four-beat plan described something that was not happening.
                _ when _gm.StageProgress.Length > 0 =>
                    $"STAGE {_gm.StageProgress}   {t / 60}:{t % 60:00}",
                _ => $"WAVE {Mathf.Max(1, _gm.WaveIndex + 1)}/{_gm.WaveCount}   {t / 60}:{t % 60:00}",
            };
        }

        /// <summary>
        /// Red edge flash on damage, plus a permanent low-HP throb so the last
        /// sliver of health is felt at the edges of the screen, not just read off
        /// the bar. Unscaled so hit-stop doesn't freeze the flash mid-hit.
        /// </summary>
        private void UpdateVignette()
        {
            if (_vignette == null) return;
            _vignetteT = Mathf.Max(0f, _vignetteT - Time.unscaledDeltaTime * 2.2f);
            var lowHp = _health != null && !_health.Dead && _health.Hp < _health.MaxHp * 0.3f
                ? 0.16f + 0.06f * Mathf.Sin(Time.unscaledTime * 3.4f)
                : 0f;
            var alpha = Mathf.Max(lowHp, _vignetteT * 0.55f);
            // A full-screen transparent image still costs fill rate at alpha 0, and
            // this one covers the whole viewport. Switch it off when it has nothing
            // to say rather than drawing an invisible quad every frame.
            var wanted = alpha > 0.004f;
            if (_vignette.enabled != wanted) _vignette.enabled = wanted;
            if (!wanted) return;
            _vignette.color = new Color(0.9f, 0.12f, 0.1f, alpha);
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
            // The bar is shared: escort health and stealth detection reuse it, so
            // the new mission types need no UI of their own.
            var npc = Missions.EscortNpc.Active;
            if (npc != null && npc.Health != null && !npc.Health.Dead)
            {
                _bossBar.gameObject.SetActive(true);
                _bossLabel.text = npc.UnderThreat ? "YOTSU — UNDER ATTACK" : "YOTSU — THE LANTERN";
                _bossFill.color = npc.UnderThreat ? UiKit.Blood : new Color(1f, 0.62f, 0.35f);
                _bossFill.fillAmount = Mathf.Lerp(_bossFill.fillAmount,
                    npc.Health.Hp / npc.Health.MaxHp, Time.deltaTime * 8f);
                return;
            }

            var detect = _gm.Detection01;
            if (detect > 0.01f)
            {
                _bossBar.gameObject.SetActive(true);
                _bossLabel.text = detect > 0.65f ? "THEY'RE LOOKING RIGHT AT YOU" : "EYES SEARCHING";
                _bossFill.color = Color.Lerp(UiKit.Sen, UiKit.Blood, detect);
                _bossFill.fillAmount = detect;
                return;
            }
            _bossFill.color = UiKit.Ember; // restore for real boss bars

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
                && _gm.MissionTime < 30f && (!_movedOnce || !_struckOnce || !_jumpedOnce))
                _hintText.text = !_movedOnce
                    ? "DRAG THE LEFT SIDE OF THE SCREEN TO MOVE"
                    : !_struckOnce
                        ? "TAP STRIKE WHEN AN ENEMY IS CLOSE"
                        : "TAP JUMP TO VAULT COVER — OR LEAP AT A WALL TO RUN IT";
            else _hintText.text = "";
        }

        private void UpdateMarkers()
        {
            if (_cam == null) return;
            var used = 0;
            foreach (var e in EnemyBrain.Active)
            {
                if (e == null || e.Dead) continue;
                if (used >= MarkerBudget) break;
                var m = GetMarker(used++);
                var sp = _cam.WorldToScreenPoint(e.transform.position + Vector3.up * 2.4f);
                var scale = 720f / UnityEngine.Screen.height;
                var onScreen = sp.z > 0 && sp.x > 0 && sp.x < UnityEngine.Screen.width
                               && sp.y > 0 && sp.y < UnityEngine.Screen.height;
                var locked = _combat != null && _combat.LockedTarget == e;
                // The bar belongs to enemies you're actually fighting: it appears
                // on the hit and fades over 2s, so a busy screen isn't wallpapered
                // with health bars. A locked target keeps its bar while locked.
                var sinceHit = Time.unscaledTime - e.LastHitTime;
                var showBar = locked || e.GuardBroken || sinceHit < HpBarSeconds;
                var barAlpha = locked ? 1f
                    : Mathf.Clamp01((HpBarSeconds - sinceHit) / HpBarFade);

                // Glyph is set once per enemy per frame; Icon() caches by name.
                if (m.weapon != null)
                {
                    m.weapon.sprite = UiKit.Icon(WeaponIcon(e.weapon));
                    m.weapon.enabled = showBar || !onScreen;
                    m.weapon.color = new Color(0.95f, 0.94f, 0.9f,
                        onScreen ? 0.85f * barAlpha : 0.9f);
                }

                if (onScreen)
                {
                    m.back.enabled = showBar;
                    m.fill.enabled = showBar;
                    m.arrow.enabled = false;
                    m.root.anchoredPosition = new Vector2(sp.x * scale, sp.y * scale);
                    m.fill.fillAmount = Mathf.Clamp01(e.Hp / e.maxHp);
                    // A broken guard is the single most actionable thing on screen,
                    // so the bar turns gold for the whole opening rather than
                    // needing a separate readout.
                    m.fill.color = e.GuardBroken
                        ? new Color(1f, 0.82f, 0.42f, Mathf.Max(0.85f, barAlpha))
                        : HealthColor(e, barAlpha);
                    // The cycled target wears an ember tint so the lock is legible.
                    m.back.color = locked
                        ? new Color(UiKit.Ember.r, UiKit.Ember.g, UiKit.Ember.b, 0.85f)
                        : new Color(0, 0, 0, 0.55f * barAlpha);
                    m.root.localScale = Vector3.one * (locked ? 1.3f : 1f);
                }
                else
                {
                    var p = new Vector2(sp.x, sp.y);
                    if (sp.z < 0) p = new Vector2(UnityEngine.Screen.width - p.x, 40);
                    p.x = Mathf.Clamp(p.x, 30, UnityEngine.Screen.width - 30);
                    p.y = Mathf.Clamp(p.y, 30, UnityEngine.Screen.height - 30);
                    // Off-screen: the arrow carries the same HP read, so you can
                    // tell a wounded flanker from a fresh one before it arrives.
                    m.back.enabled = showBar;
                    m.fill.enabled = showBar;
                    m.arrow.enabled = true;
                    m.root.anchoredPosition = p * scale;
                    m.fill.fillAmount = Mathf.Clamp01(e.Hp / e.maxHp);
                    m.fill.color = HealthColor(e, barAlpha);
                    m.back.color = new Color(0, 0, 0, 0.55f * barAlpha);
                    m.root.localScale = Vector3.one * (locked ? 1.25f : 1f);
                    m.arrow.color = locked ? UiKit.Ember
                        : HealthColor(e, 0.9f); // arrow itself reads health too
                }
            }
            for (var i = used; i < _markers.Count; i++)
            {
                _markers[i].back.enabled = false;
                _markers[i].fill.enabled = false;
                _markers[i].arrow.enabled = false;
                if (_markers[i].weapon != null) _markers[i].weapon.enabled = false;
                _markers[i].root.localScale = Vector3.one;
            }
        }

        /// <summary>How long an enemy's HP bar stays up after a hit, and its fade tail.</summary>
        private const float HpBarSeconds = 2f;
        private const float HpBarFade = 0.6f;

        /// <summary>Ember when healthy, blood when nearly dead — readable at a glance.</summary>
        private static Color HealthColor(EnemyBrain e, float alpha)
        {
            var frac = e.maxHp > 0f ? Mathf.Clamp01(e.Hp / e.maxHp) : 1f;
            var c = Color.Lerp(UiKit.Blood, UiKit.Ember, frac);
            return new Color(c.r, c.g, c.b, alpha);
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
                // Weapon glyph rides above the bar so an off-screen threat is
                // identifiable before it arrives — an axe is not an archer.
                var wpnRt = UiKit.Rect(root, "wpn", new Vector2(0.5f, 0.5f),
                    new Vector2(0, 15), new Vector2(20, 20));
                var wpn = UiKit.Img(wpnRt, null, UiKit.Pale);
                _markers.Add(new Marker
                    { root = root, back = back, fill = fill, arrow = arrow, weapon = wpn });
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
            {
                BuildRunReport();
                ResultButtons(("RISE AGAIN", () => _gm.Retry()),
                    ("THE FORGE", () => SetScreen(Screen.Forge)),
                    ("MENU", () => _gm.OpenMenu()));
                return;
            }
            ResultButtons(("RISE AGAIN", () => _gm.Retry()), ("MENU", () => _gm.OpenMenu()));
        }

        /// <summary>
        /// The run report. Score is the headline because score is what the
        /// modifiers were wagered on; everything else is the evidence for it.
        /// </summary>
        private void BuildRunReport()
        {
            UiKit.Label(_screenRoot, $"{Endless.RunStats.Score}", 62, UiKit.EmberBright,
                new Vector2(0.5f, 0.5f), new Vector2(0, 96), new Vector2(600, 70), display: true);
            UiKit.Label(_screenRoot,
                Endless.RunStats.NewScoreRecord ? "★ A NEW BEST ★" : "POINTS",
                16, Endless.RunStats.NewScoreRecord ? UiKit.Ember : UiKit.Sen,
                new Vector2(0.5f, 0.5f), new Vector2(0, 56), new Vector2(600, 22));

            var t = Mathf.RoundToInt(Endless.RunStats.Time);
            var stats = new (string label, string value, bool record)[]
            {
                ("DEPTH", Endless.RunStats.Depth.ToString(), Endless.RunStats.NewDepthRecord),
                ("SURVIVED", Endless.RunStats.TimeText(t), Endless.RunStats.NewTimeRecord),
                ("KILLS", Endless.RunStats.Kills.ToString(), false),
                ("THREAD", $"×{Endless.RunStats.BestCombo}", false),
                ("MARCHED", $"{Endless.RunStats.Distance}m", false),
            };
            for (var i = 0; i < stats.Length; i++)
            {
                var (label, value, record) = stats[i];
                var x = (i - (stats.Length - 1) * 0.5f) * 172f;
                UiKit.Label(_screenRoot, value, 26,
                    record ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0.5f, 0.5f), new Vector2(x, 8), new Vector2(160, 30), display: true);
                UiKit.Label(_screenRoot, label + (record ? " ★" : ""), 13, UiKit.Sen,
                    new Vector2(0.5f, 0.5f), new Vector2(x, -18), new Vector2(160, 18));
            }

            UiKit.Label(_screenRoot,
                $"WAGER ×{Endless.RunModifiers.ActiveScoreMultiplier:0.00}   ·   "
                + Endless.RunModifiers.Describe(Endless.RunModifiers.Active),
                14, UiKit.Dim, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1000, 20));

            UiKit.Label(_screenRoot,
                $"¤ +{Endless.RunStats.RyoEarned} RYO"
                + (Endless.RunStats.ShardsEarned > 0
                    ? $"   ·   ◆ +{Endless.RunStats.ShardsEarned} SHARDS" : ""),
                18, new Color(0.5f, 0.85f, 0.55f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -82), new Vector2(800, 24));
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
