using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
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
        private enum Screen { None, MenuRoot, Story, Fight, Bio, Skills, Codex, Briefing, Hud, Result, Weapons, Arms, March, Forge, Chapter }

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
        private TMP_Text _hpLabel, _bossLabel, _waveLabel, _comboText, _objectiveText, _bannerText, _hintText;
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
            // Balanced match, not height-only. Height-only kept a 20:9 phone's
            // extra width unscaled, so anything placed by pixel offset drifted or
            // clipped on 4:3. Reference 1600x720 is the 20:9 device at 1.5x.
            scaler.referenceResolution = new Vector2(1600, 720);
            scaler.matchWidthOrHeight = 0.5f;
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
            if (_screen is Screen.MenuRoot or Screen.Story or Screen.Fight or Screen.Bio or Screen.Chapter)
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

            // Opaque screens sit on a still of the arena and the camera stops.
            // The HUD is the live game; the briefing and results want the frozen
            // scene too, just darker.
            var backdrop = MenuBackdrop.Instance;
            if (s == Screen.Hud) backdrop?.Hide();
            else backdrop?.Show(_screenRoot, s is Screen.Briefing or Screen.Result ? 0.62f : 0.72f);

            switch (s)
            {
                case Screen.MenuRoot: BuildMenuRoot(); break;
                case Screen.Story: BuildStorySelect(); break;
                case Screen.Chapter: BuildChapterSelect(); break;
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
            if (s != Screen.Hud) UiKit.Enter(_screenRoot);
        }

        // ------------------------------------------------------------- helpers

        /// <summary>Kept for call-site compatibility; the backdrop shade now does this.</summary>
        private void Dim(float a) { }

        private void BuildEmberLayer()
        {
            _emberLayer = UiKit.Group(_screenRoot, "Embers");
            // Eight, small, faint: an ember drift is atmosphere, not confetti.
            for (var i = 0; i < 8; i++)
            {
                var e = UiKit.Rect(_emberLayer, "e", new Vector2(0.5f, 0f),
                    new Vector2(Random.Range(-760, 760), Random.Range(0, 720)),
                    Vector2.one * Random.Range(2f, 5f));
                var img = UiKit.Img(e, UiKit.Circle, new Color(1f, 0.55f, 0.3f, Random.Range(0.12f, 0.3f)));
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
                if (p.y > 740) { p.y = -10; p.x = Random.Range(-760, 760); }
                e.anchoredPosition = p;
            }
        }

        // -------------------------------------------------------------- menus

        private void BuildMenuRoot()
        {
            BuildEmberLayer();

            // Left column on a cinematic plate. Sparse by design: a title, a
            // hairline, seven lines. Everything else the old menu shouted —
            // weapon line, finish line, daily strip, three mode cards — lives one
            // tap deeper where it is wanted.
            var col = UiKit.Rect(_screenRoot, "Column", new Vector2(0, 1), new Vector2(104, 0),
                new Vector2(520, 720), new Vector2(0, 1));

            UiKit.Kicker(col, "AN EMBERLINE STORY", new Vector2(0, 1), new Vector2(0, -78),
                new Vector2(520, 20), align: TextAnchor.MiddleLeft);
            UiKit.Label(col, "EMBERLINE", 58, UiKit.Pale, new Vector2(0, 1), new Vector2(-4, -128),
                new Vector2(520, 72), display: true, align: TextAnchor.MiddleLeft);
            UiKit.Accent(col, new Vector2(0, 1), new Vector2(18, -172), 36);

            var next = Mathf.Clamp(Session.StoryUnlocked - 1, 0, Session.Story.Length - 1);
            var cleared = Session.StoryUnlocked - 1;
            var items = new (string label, string sub, System.Action go)[]
            {
                ("CONTINUE", cleared >= Session.Story.Length
                    ? "The road is walked — replay any chapter"
                    : $"{Session.Story[next].id:00} {Session.Story[next].name.ToLowerInvariant()} · "
                      + Campaign.Campaign.ChapterOf(Session.Story[next].id).name.ToLowerInvariant(),
                    () => _gm.LaunchStory(next)),
                ("STORY", $"{Mathf.Clamp(cleared, 0, Session.Story.Length)} of {Session.Story.Length} missions · "
                    + $"chapter {Campaign.Campaign.ChapterOf(Mathf.Min(Session.StoryUnlocked, Session.Story.Length)).number} of {Campaign.Campaign.Chapters.Length}",
                    () => SetScreen(Screen.Story)),
                ("DUELS", "One life. Full strength.", () => SetScreen(Screen.Fight)),
                ("THE ROAD NORTH", Endless.RunStats.BestScore > 0
                    ? $"Best {Endless.RunStats.BestScore:N0} pts" : "Seven countries, no end",
                    () => SetScreen(Screen.March)),
                ("THE FORGE", $"{Core.Wallet.Ryo:N0} ryo", () => SetScreen(Screen.Forge)),
                ("SKILLS", $"{SkillTree.Shards} shards · {SkillTree.OwnedCount}/{SkillTree.Nodes.Count} learned",
                    () => SetScreen(Screen.Skills)),
                ("SETTINGS", Difficulty.Name.ToLowerInvariant(), ToggleSettings),
            };

            var y = -212f;
            for (var i = 0; i < items.Length; i++)
            {
                var (label, sub, go) = items[i];
                MenuRow(col, label, sub, y, i == 0, go);
                y -= 54f;
            }

            // Secondary, small, bottom-left: the reference pages.
            UiKit.MakeButton(_screenRoot, "RENZO", new Vector2(0, 0), new Vector2(150, 40),
                new Vector2(96, 44), () => SetScreen(Screen.Bio), 12);
            UiKit.MakeButton(_screenRoot, "CODEX", new Vector2(0, 0), new Vector2(254, 40),
                new Vector2(96, 44), () => SetScreen(Screen.Codex), 12);
            UiKit.MakeButton(_screenRoot, "ARMOURY", new Vector2(0, 0), new Vector2(368, 40),
                new Vector2(112, 44), () => SetScreen(Screen.Weapons), 12);

            UiKit.Label(_screenRoot, "v" + Application.version, 11, UiKit.Faint, new Vector2(1, 0),
                new Vector2(-40, 30), new Vector2(200, 16), align: TextAnchor.MiddleRight);
        }

        /// <summary>
        /// One menu line: a label, a one-line hint beneath, a hairline. The row
        /// is the tap target; there is no box. The primary row carries the accent.
        /// </summary>
        private void MenuRow(RectTransform col, string label, string sub, float y, bool primary,
            System.Action go)
        {
            var rt = UiKit.Rect(col, "Row_" + label, new Vector2(0, 1), new Vector2(0, y),
                new Vector2(440, 52), new Vector2(0, 1));
            var hit = UiKit.Img(rt, null, new Color(1, 1, 1, 0.001f));
            hit.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Sfx3D.Confirm(); go?.Invoke(); });
            var punch = rt.gameObject.AddComponent<UiKit.ButtonPunch>();
            var trig = rt.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => punch.Punch());
            trig.triggers.Add(down);

            var t = UiKit.Label(rt, label, primary ? 22 : 19, primary ? UiKit.EmberBright : UiKit.Pale,
                new Vector2(0, 1), new Vector2(18, -14), new Vector2(420, 26), align: TextAnchor.MiddleLeft);
            t.characterSpacing = 5f;
            if (!string.IsNullOrEmpty(sub))
                UiKit.Label(rt, UiKit.Clean(sub), 12, UiKit.Dim, new Vector2(0, 1), new Vector2(18, -37),
                    new Vector2(420, 16), align: TextAnchor.MiddleLeft);
            UiKit.Hairline(rt, new Vector2(0, 0), 0.08f);
            if (primary) UiKit.Img(UiKit.Rect(rt, "Mark", new Vector2(0, 0.5f), new Vector2(4, 4),
                new Vector2(3, 22)), UiKit.White, UiKit.Ember);
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
                    .alignment = TextAlignmentOptions.Center;
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
            ScreenHeader("THE ARMOURY", "WEAPONS");
            UiKit.Label(_screenRoot, "WEAPONS UNLOCK AS THE STORY OPENS UP", 11, UiKit.Dim,
                new Vector2(1f, 1f), new Vector2(-104, -86), new Vector2(500, 18),
                align: TextAnchor.MiddleRight).characterSpacing = 3f;

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
            ScreenHeader("SKILLS", "EMBER SKILLS");
            UiKit.Label(_screenRoot, $"{SkillTree.Shards} SHARDS TO SPEND", 12, UiKit.EmberBright,
                new Vector2(1f, 1f), new Vector2(-104, -86), new Vector2(400, 18),
                align: TextAnchor.MiddleRight).characterSpacing = 3f;

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
                descText.alignment = TextAlignmentOptions.Center;
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
                    UiKit.Label(card, $"{node.cost} SHARDS", 11,
                        SkillTree.Shards >= node.cost ? UiKit.EmberBright : UiKit.Dim,
                        new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(200, 22));
                }
            }
            BackButton();
        }

        private int _chapter = 1;

        /// <summary>
        /// Ten chapters in three acts, as a grid. A hundred missions do not fit
        /// on a screen and should not: the chapter is the unit the player
        /// thinks in, and the journey reads left to right across the acts.
        /// </summary>
        private void BuildStorySelect()
        {
            BuildEmberLayer();
            ScreenHeader("STORY", "THE ROAD FROM YORUNE");

            var chapters = Campaign.Campaign.Chapters;
            var unlockedId = Session.StoryUnlocked;
            const float cardW = 250f, cardH = 176f, gapX = 16f, gapY = 16f;
            const int perRow = 5;
            var x0 = -(perRow - 1) * 0.5f * (cardW + gapX);
            for (var i = 0; i < chapters.Length; i++)
            {
                var ch = chapters[i];
                var row = i / perRow;
                var col = i % perRow;
                var rt = UiKit.Rect(_screenRoot, "Chapter" + ch.number, new Vector2(0.5f, 1f),
                    new Vector2(x0 + col * (cardW + gapX), -140 - row * (cardH + gapY)),
                    new Vector2(cardW, cardH), new Vector2(0.5f, 1f));
                var open = ch.firstMission <= unlockedId;
                var cleared = 0;
                var stars = 0;
                for (var id = ch.firstMission; id <= ch.lastMission; id++)
                {
                    if (Session.Stars(id) > 0) cleared++;
                    stars += Session.Stars(id);
                }
                var img = UiKit.Img(rt, null, new Color(1, 1, 1, open ? 0.035f : 0.012f));
                img.raycastTarget = open;
                if (open)
                {
                    var n = ch.number;
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    var colors = btn.colors; colors.highlightedColor = new Color(1, 1, 1, 0.08f) * 4f;
                    colors.pressedColor = new Color(1, 1, 1, 0.12f) * 6f; btn.colors = colors;
                    btn.onClick.AddListener(() => { Sfx3D.Confirm(); _chapter = n; SetScreen(Screen.Chapter); });
                }
                UiKit.Kicker(rt, $"ACT {ToRoman(ch.act)}  ·  CHAPTER {ch.number}", new Vector2(0, 1),
                    new Vector2(12, -10), new Vector2(cardW - 24, 16), color: open ? UiKit.Ember : UiKit.Faint,
                    align: TextAnchor.MiddleLeft);
                UiKit.Label(rt, open ? ch.name : "LOCKED", 17, open ? UiKit.Pale : UiKit.Faint, new Vector2(0, 1),
                    new Vector2(12, -30), new Vector2(cardW - 24, 44), display: true, align: TextAnchor.UpperLeft)
                    .characterSpacing = 2f;
                UiKit.Paragraph(rt, open ? ch.theme : "Finish the chapter before it.", 12,
                    open ? UiKit.Dim : UiKit.Faint, new Vector2(0, 1), new Vector2(12, -84),
                    new Vector2(cardW - 24, 34), TextAnchor.UpperLeft);
                UiKit.Kicker(rt, ch.region.ToString().ToUpperInvariant(), new Vector2(0, 1),
                    new Vector2(12, -124), new Vector2(cardW - 24, 14), color: UiKit.Faint,
                    align: TextAnchor.MiddleLeft);
                UiKit.Label(rt, open ? $"{cleared}/10  ·  {stars} STARS" : "", 11, UiKit.Dim, new Vector2(0, 1),
                    new Vector2(12, -146), new Vector2(cardW - 24, 16), align: TextAnchor.MiddleLeft)
                    .characterSpacing = 2f;
                UiKit.Hairline(rt, new Vector2(0, 0), 0.06f);
            }
            BackButton();
        }

        /// <summary>One chapter's ten missions, in two columns of five.</summary>
        private void BuildChapterSelect()
        {
            BuildEmberLayer();
            var ch = Campaign.Campaign.ChapterOf(Mathf.Clamp(_chapter, 1, 10) * 10 - 9);
            // The chapter is the title; the act and its theme are the kicker.
            ScreenHeader($"{Campaign.Campaign.ActNames[ch.act - 1]}  ·  CHAPTER {ch.number}  ·  {ch.theme.ToUpperInvariant()}", ch.name);
            const float colW = 560f;
            for (var c = 0; c < 2; c++)
            {
                var col = UiKit.Rect(_screenRoot, "Col" + c, new Vector2(0.5f, 1f),
                    new Vector2((c - 0.5f) * (colW + 24f), -132), new Vector2(colW, 480), new Vector2(0.5f, 1f));
                var y = -6f;
                for (var i = 0; i < 5; i++)
                {
                    var id = ch.firstMission + c * 5 + i;
                    var level = System.Array.Find(Session.Story, l => l.id == id);
                    if (level == null) continue;
                    MissionRow(col, level, y, colW);
                    y -= 78f;
                }
            }
            BackButton(() => SetScreen(Screen.Story));
        }

        private void MissionRow(RectTransform col, LevelDef level, float y, float w)
        {
            var unlocked = level.id <= Session.StoryUnlocked;
            var stars = Session.Stars(level.id);
            var rt = UiKit.Rect(col, "Level" + level.id, new Vector2(0, 1), new Vector2(0, y),
                new Vector2(w, 72), new Vector2(0, 1));
            var img = UiKit.Img(rt, null, new Color(1, 1, 1, unlocked ? 0.025f : 0.0f));
            img.raycastTarget = unlocked;
            if (unlocked)
            {
                var idx = System.Array.IndexOf(Session.Story, level);
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var colors = btn.colors; colors.highlightedColor = new Color(1, 1, 1, 0.08f) * 4f;
                colors.pressedColor = new Color(1, 1, 1, 0.12f) * 6f; btn.colors = colors;
                btn.onClick.AddListener(() => { Sfx3D.Confirm(); _gm.LaunchStory(idx); });
            }
            UiKit.Label(rt, level.id.ToString("00"), 20, unlocked ? UiKit.Ember : UiKit.Faint,
                new Vector2(0, 1), new Vector2(8, -14), new Vector2(40, 26), display: true,
                align: TextAnchor.MiddleLeft);
            UiKit.Label(rt, unlocked ? level.name : "LOCKED", 15, unlocked ? UiKit.Pale : UiKit.Faint,
                new Vector2(0, 1), new Vector2(54, -12), new Vector2(w - 170, 20), align: TextAnchor.MiddleLeft)
                .characterSpacing = 2f;
            var desc = unlocked ? level.story : "Clear the previous mission.";
            if (desc.Length > 64) desc = desc.Substring(0, 61).TrimEnd() + "…";
            UiKit.Paragraph(rt, desc, 12, unlocked ? UiKit.Dim : UiKit.Faint,
                new Vector2(0, 1), new Vector2(54, -34), new Vector2(w - 170, 30), TextAnchor.UpperLeft);
            for (var i = 0; i < 3; i++)
            {
                var sRt = UiKit.Rect(rt, "star", new Vector2(1, 1), new Vector2(-88 + i * 22f, -18),
                    new Vector2(15, 15));
                UiKit.Img(sRt, i < stars ? UiKit.Star : UiKit.StarOutline,
                    i < stars ? UiKit.EmberBright : new Color(1, 1, 1, unlocked ? 0.18f : 0.05f));
            }
            var state = !unlocked ? "" : stars >= 3 ? "MASTERED" : stars > 0 ? "CLEARED" : "NEW";
            UiKit.Label(rt, state, 10, stars > 0 ? UiKit.Dim : UiKit.Ember, new Vector2(1, 1),
                new Vector2(-66, -42), new Vector2(90, 14), align: TextAnchor.MiddleRight)
                .characterSpacing = 3f;
            UiKit.Hairline(rt, new Vector2(0, 0), 0.06f);
        }

        private static string ToRoman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", _ => n.ToString() };

        /// <summary>Kicker + title, top-left, on every secondary screen.</summary>
        private void ScreenHeader(string kicker, string title)
        {
            UiKit.Kicker(_screenRoot, kicker, new Vector2(0, 1), new Vector2(104, -52),
                new Vector2(600, 18), align: TextAnchor.MiddleLeft);
            UiKit.Label(_screenRoot, title, 34, UiKit.Pale, new Vector2(0, 1), new Vector2(100, -86),
                new Vector2(900, 44), display: true, align: TextAnchor.MiddleLeft);
            UiKit.Accent(_screenRoot, new Vector2(0, 1), new Vector2(122, -114), 36);
        }

        private void BuildFightSelect()
        {
            BuildEmberLayer();
            ScreenHeader("DUELS", "CHOOSE YOUR OPPONENT");
            UiKit.Label(_screenRoot, "One life. Full strength. No mercy.", 14, UiKit.Dim,
                new Vector2(0, 1), new Vector2(104, -140), new Vector2(700, 20), align: TextAnchor.MiddleLeft);

            var y = -186f;
            for (var i = 0; i < Session.Duels.Length; i++)
            {
                var duel = Session.Duels[i];
                var unlocked = duel.id <= Session.DuelsUnlocked;
                var won = Session.DuelWon(duel.id);
                var rt = UiKit.Rect(_screenRoot, "Duel" + duel.id, new Vector2(0, 1), new Vector2(100, y),
                    new Vector2(720, 66), new Vector2(0, 1));
                var img = UiKit.Img(rt, null, new Color(1, 1, 1, unlocked ? 0.025f : 0f));
                img.raycastTarget = unlocked;
                if (unlocked)
                {
                    var idx = i;
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    btn.onClick.AddListener(() => { Sfx3D.Confirm(); _gm.LaunchDuel(idx); });
                }
                UiKit.Label(rt, unlocked ? duel.name : "LOCKED", 20, unlocked ? UiKit.Pale : UiKit.Faint,
                    new Vector2(0, 1), new Vector2(14, -16), new Vector2(500, 26), display: true,
                    align: TextAnchor.MiddleLeft);
                UiKit.Label(rt, unlocked ? duel.title : "Defeat the previous opponent", 12,
                    unlocked ? UiKit.Dim : UiKit.Faint, new Vector2(0, 1), new Vector2(14, -42),
                    new Vector2(500, 16), align: TextAnchor.MiddleLeft).characterSpacing = 3f;
                if (won) UiKit.Label(rt, "WON", 11, UiKit.Ember, new Vector2(1, 0.5f), new Vector2(-24, 0),
                    new Vector2(80, 16), align: TextAnchor.MiddleRight).characterSpacing = 3f;
                UiKit.Hairline(rt, new Vector2(0, 0), 0.07f);
                y -= 72f;
            }
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
            ScreenHeader("THE MARCH", "THE ROAD NORTH");
            UiKit.Label(_screenRoot, "SEVEN COUNTRIES. NO TWO MARCHES THE SAME.", 11, UiKit.Dim,
                new Vector2(1f, 1f), new Vector2(-104, -86), new Vector2(600, 18),
                align: TextAnchor.MiddleRight).characterSpacing = 3f;

            // Record book.
            var recs = $"BEST {Endless.RunStats.BestScore} PTS   ·   DEPTH {Endless.RunStats.BestDepth}"
                       + $"   ·   {Endless.RunStats.TimeText(Endless.RunStats.BestTime)}"
                       + $"   ·   {Endless.RunStats.BestKills} KILLS"
                       + $"   ·   ×{Endless.RunStats.BestComboEver} THREAD";
            UiKit.Label(_screenRoot, recs, 12, UiKit.Ember,
                new Vector2(0, 1f), new Vector2(104, -134), new Vector2(1000, 18),
                align: TextAnchor.MiddleLeft).characterSpacing = 2f;
            UiKit.Label(_screenRoot,
                $"{Endless.RunStats.TotalRuns} MARCHES   ·   {Endless.RunStats.TotalKills} DEAD"
                + $"   ·   ¤ {Core.Wallet.Ryo} RYO",
                11, UiKit.Faint, new Vector2(0, 1f), new Vector2(104, -152), new Vector2(1000, 16),
                align: TextAnchor.MiddleLeft);

            // Modifier grid: four columns, two rows.
            var all = Endless.RunModifiers.All;
            for (var i = 0; i < all.Length; i++)
            {
                var d = all[i];
                var col = i % 4;
                var row = i / 4;
                var pos = new Vector2((col - 1.5f) * 282f, 30f - row * 148f);
                var card = UiKit.Rect(_screenRoot, "Mod_" + d.Mod, new Vector2(0.5f, 0.5f),
                    pos, new Vector2(270, 136));
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
            ScreenHeader("THE FORGE", "UPGRADES AND CLOTH");
            UiKit.Label(_screenRoot, $"{Core.Wallet.Ryo:N0} RYO", 12, UiKit.EmberBright,
                new Vector2(1f, 1f), new Vector2(-104, -86), new Vector2(400, 18),
                align: TextAnchor.MiddleRight).characterSpacing = 3f;

            var w = Loadout.Current;
            var wid = w != null ? w.id : "katana";
            UiKit.Label(_screenRoot, w != null ? w.displayName : "NO WEAPON", 19, UiKit.Ember,
                new Vector2(0f, 1f), new Vector2(300, -152), new Vector2(460, 24), display: true);

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
                    new Vector2(300, -236 - i * 116), new Vector2(460, 100));
                var img = UiKit.Img(card, UiKit.PanelSprite,
                    maxed ? new Color(0.20f, 0.15f, 0.10f)
                    : afford ? new Color(0.135f, 0.14f, 0.175f)
                    : new Color(0.10f, 0.10f, 0.125f), sliced: true);

                UiKit.Label(card, name, 18, maxed ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0f, 1f), new Vector2(78, -24), new Vector2(160, 24), display: true);
                UiKit.Label(card, blurb, 13, UiKit.Sen,
                    new Vector2(0f, 1f), new Vector2(120, -48), new Vector2(250, 20));
                for (var pip = 0; pip < Core.WeaponUpgrades.MaxLevel; pip++)
                    UiKit.Img(UiKit.Rect(card, "pip", new Vector2(1f, 1f),
                            new Vector2(-150 + pip * 16f, -26), new Vector2(10, 4)),
                        UiKit.White, pip < lv ? UiKit.Ember : new Color(1, 1, 1, 0.12f));
                UiKit.Label(card, maxed ? "MASTERED" : $"{cost} RYO", 12,
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
                new Vector2(1f, 1f), new Vector2(-300, -152), new Vector2(460, 24), display: true);
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
                    new Vector2(-460 + col * 230f, -226 - row * 112f), new Vector2(220, 96));
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
                UiKit.Label(card, equipped ? "WORN" : owned ? "TAP TO WEAR" : $"{set.Cost} RYO", 11,
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

            UiKit.MakeButton(_screenRoot, "THE ROAD", new Vector2(0f, 0f), new Vector2(150, 48),
                new Vector2(140, 46), () => { Sfx3D.Back(); SetScreen(Screen.March); }, 18);
        }

        /// <summary>Filled/empty pips for an upgrade track.</summary>
        private static string Pips(int filled, int max)
        {
            var s = "";
            for (var i = 0; i < max; i++) s += i < filled ? "◆" : "◇";
            return s;
        }

        private void BackButton() => BackButton(() => SetScreen(Screen.MenuRoot));

        private void BackButton(System.Action go) =>
            UiKit.MakeButton(_screenRoot, "BACK", new Vector2(0f, 0f), new Vector2(150, 48),
                new Vector2(140, 46), () => { Sfx3D.Back(); go(); }, 13);

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
            UiKit.Surface(panel, 0.96f);
            UiKit.Label(panel, "SETTINGS", 30, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -42), new Vector2(400, 40), display: true);

            TMP_Text sfxLabel = null, musicLabel = null, gfxLabel = null, diffLabel = null;
            TMP_Text diffBlurb = null, fpsLabel = null;
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

            void Row(float y, System.Func<TMP_Text> label, System.Action minus, System.Action plus)
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
                TMP_Text quitLabel = null;
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
                quitLabel = quit.GetComponentInChildren<TMP_Text>();

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
            BuildEmberLayer();
            string kicker = "", title = "", location = "", objective = "", optional = "";
            var plan = _gm.CurrentPlan;
            if (_gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null)
            {
                var d = _gm.CurrentDuel;
                kicker = "DUEL"; title = d.name;
                location = d.marsh ? "ASHFEN MARSH" : "YORUNE ROOFTOPS";
                objective = "Win the duel. One life each.";
                optional = d.taunt;
            }
            else if (_gm.ModeNow == LaunchMode.Endless)
            {
                kicker = "THE MARCH"; title = "THE ROAD NORTH";
                location = _gm.Run != null ? _gm.Run.RegionName : "THE ROAD NORTH";
                objective = "March north. Survive what bars the way.";
                optional = Endless.RunModifiers.Describe(Endless.RunModifiers.Active);
            }
            else if (_gm.CurrentLevel != null)
            {
                var l = _gm.CurrentLevel;
                var chapter = Campaign.Campaign.ChapterOf(l.id);
                kicker = $"MISSION {l.id:00}  ·  CHAPTER {chapter.number} — {chapter.name}";
                title = l.name;
                location = (l.marsh ? "ASHFEN MARSH" : "YORUNE ROOFTOPS")
                           + (plan != null ? "  ·  " + plan.missionType : "");
                if (plan != null && plan.stages.Length > 0)
                {
                    foreach (var st in plan.stages)
                    {
                        if (!st.optional && objective.Length == 0) objective = st.objective;
                        if (st.optional && optional.Length == 0) optional = st.objective;
                    }
                    // The mission-wide condition is the optional objective for
                    // almost every mission now, and it changes how the whole
                    // thing is played — so it belongs on the sheet, before the
                    // player commits, not on the results screen afterwards.
                    if (optional.Length == 0 && plan.challenge != Missions.MissionChallenge.None)
                        optional = plan.challenge switch
                        {
                            Missions.MissionChallenge.NoAlarm =>
                                $"Finish without raising the alarm.  ◆ +{plan.challengeShards}",
                            Missions.MissionChallenge.SaveAllPrisoners =>
                                $"Free every prisoner you find.  ◆ +{plan.challengeShards}",
                            Missions.MissionChallenge.NoCivilianDeaths =>
                                $"No villager dies.  ◆ +{plan.challengeShards}",
                            Missions.MissionChallenge.SilentKill =>
                                $"Kill the target before it ever sees you.  ◆ +{plan.challengeShards}",
                            Missions.MissionChallenge.UnderTime =>
                                $"Finish inside {Mathf.RoundToInt(plan.challengeSeconds)} seconds.  ◆ +{plan.challengeShards}",
                            _ => optional,
                        };
                }
                if (objective.Length == 0)
                    objective = l.objective switch
                    {
                        MissionObjective.Escort => "Walk Yotsu to the temple. If the flame goes out, the night is lost.",
                        MissionObjective.Stealth => "Cut them down unseen. An alarm costs the rank.",
                        MissionObjective.Chase => "Run them down before they scatter.",
                        _ => l.holdSeconds > 0f
                            ? $"Hold the road for {Mathf.RoundToInt(l.holdSeconds)} seconds."
                            : $"Clear {l.waves.Length} wave{(l.waves.Length > 1 ? "s" : "")}.",
                    };
            }

            // Left: the operation sheet. Right: dialogue, if any. Bottom: the way in.
            var sheet = UiKit.Rect(_screenRoot, "Sheet", new Vector2(0, 1), new Vector2(100, -52),
                new Vector2(640, 420), new Vector2(0, 1));
            UiKit.Kicker(sheet, kicker, new Vector2(0, 1), new Vector2(0, -8), new Vector2(640, 18),
                align: TextAnchor.MiddleLeft);
            UiKit.Label(sheet, title, 40, UiKit.Pale, new Vector2(0, 1), new Vector2(-2, -44),
                new Vector2(640, 52), display: true, align: TextAnchor.MiddleLeft);
            UiKit.Accent(sheet, new Vector2(0, 1), new Vector2(18, -76), 36);

            var y = -104f;
            void Line(string k, string v, Color? colour = null)
            {
                if (string.IsNullOrEmpty(v)) return;
                UiKit.Kicker(sheet, k, new Vector2(0, 1), new Vector2(0, y), new Vector2(200, 16),
                    color: UiKit.Faint, align: TextAnchor.MiddleLeft);
                UiKit.Paragraph(sheet, UiKit.Clean(v), 16, colour ?? UiKit.Pale, new Vector2(0, 1),
                    new Vector2(0, y - 18), new Vector2(620, 40), TextAnchor.UpperLeft);
                UiKit.Hairline(sheet, new Vector2(0, 1), 0.07f).rectTransform.anchoredPosition = new Vector2(0, y - 60);
                y -= 72f;
            }
            Line("MISSION", _gm.CurrentLevel != null ? _gm.CurrentLevel.story : "");
            Line("LOCATION", location);
            Line("OBJECTIVE", objective, UiKit.EmberBright);
            Line("OPTIONAL OBJECTIVE", string.IsNullOrEmpty(optional) ? "—" : optional, UiKit.Dim);

            if (_gm.ModeNow == LaunchMode.Story && _gm.CurrentLevel != null
                && _gm.CurrentLevel.dialogue.Length > 0)
                DialogueBox.Show(_screenRoot, _gm.CurrentLevel.dialogue,
                    () => StoryMemory.MarkDialogueSeen(_gm.CurrentLevel.id));

            if (_gm.ModeNow == LaunchMode.Duel)
            {
                var mod = Session.CurrentDuelModifier;
                var bonus = mod.bonusShards > 0 ? $"  +{mod.bonusShards} shards" : "";
                UiKit.MakeButton(_screenRoot, $"TERMS — {mod.name}{bonus}", new Vector2(1, 0),
                    new Vector2(-360, 48), new Vector2(360, 48), () =>
                    {
                        Session.DuelModifierIndex =
                            (Session.DuelModifierIndex + 1) % Session.DuelModifiers.Length;
                        SetScreen(Screen.Briefing);
                    }, 13);
            }

            UiKit.MakeButton(_screenRoot, "MARCH", new Vector2(1, 0), new Vector2(-150, 48),
                new Vector2(220, 54), () => { Sfx3D.Confirm(); _gm.BeginMission(); }, 20,
                display: true, primary: true);
            UiKit.MakeButton(_screenRoot, "MENU", new Vector2(0, 0), new Vector2(150, 48),
                new Vector2(140, 48), () => _gm.OpenMenu(), 13);
        }

        // ---------------------------------------------------------------- hud

        private void BuildHud()
        {
            // Health + Sen, top-left.
            _hpLabel = UiKit.Label(_screenRoot, "LIFE", 10, UiKit.Faint, new Vector2(0, 1),
                new Vector2(64, -26), new Vector2(100, 16), align: TextAnchor.MiddleLeft);
            _hpLabel.characterSpacing = 4f;
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
            _waveLabel = UiKit.Label(_screenRoot, "", 12, UiKit.Dim, new Vector2(1, 1),
                new Vector2(-160, -28), new Vector2(300, 18), align: TextAnchor.MiddleRight);
            _waveLabel.characterSpacing = 3f;

            // Pause. The settings overlay is the pause menu, but it was only
            // reachable from the main menu — so during a mission there was no way
            // to stop the game at all. Small and top-left of the clock, where it
            // cannot be hit by accident while the thumbs are on the controls.
            UiKit.MakeButton(_screenRoot, "II", new Vector2(1, 1),
                new Vector2(-330, -34), new Vector2(64, 56), ToggleSettings, 20);

            // Gyro camera toggle, under the clock (devices with a gyroscope only).
            if (SystemInfo.supportsGyroscope)
            {
                TMP_Text gyroText = null;
                var gyroBtn = UiKit.MakeButton(_screenRoot,
                    EmberInput.GyroOn ? "GYRO ON" : "GYRO OFF", new Vector2(1, 1),
                    new Vector2(-76, -74), new Vector2(118, 56), () =>
                    {
                        EmberInput.GyroOn = !EmberInput.GyroOn;
                        if (gyroText != null)
                            gyroText.text = EmberInput.GyroOn ? "GYRO ON" : "GYRO OFF";
                    }, 14);
                gyroText = gyroBtn.GetComponentInChildren<TMP_Text>();
            }

            // Combo, upper-center-right.
            var comboRt = UiKit.Rect(_screenRoot, "Combo", new Vector2(0.5f, 1f),
                new Vector2(310, -78), new Vector2(300, 60));
            _comboGroup = comboRt.gameObject.AddComponent<CanvasGroup>();
            // Small and quiet: a thread count is a readout, not a headline.
            _comboText = UiKit.Label(comboRt, "", 15, UiKit.EmberBright, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300, 60));
            _comboText.characterSpacing = 3f;

            // Boss bar, top-center.
            _bossBar = UiKit.Rect(_screenRoot, "BossBar", new Vector2(0.5f, 1f),
                new Vector2(0, -46), new Vector2(560, 46));
            _bossLabel = UiKit.Label(_bossBar, "", 12, UiKit.Pale, new Vector2(0.5f, 1f),
                new Vector2(0, -8), new Vector2(560, 18));
            _bossLabel.characterSpacing = 4f;
            _bossFill = UiKit.MakeBar(_bossBar, new Vector2(0.5f, 0f), new Vector2(0, 8),
                new Vector2(520, 14), UiKit.Ember);
            _bossBar.gameObject.SetActive(false);

            // Objective + banner.
            _objectiveText = UiKit.Label(_screenRoot, "", 12, UiKit.EmberBright,
                new Vector2(0, 1), new Vector2(300, -130), new Vector2(560, 34), align: TextAnchor.UpperLeft);
            _objectiveText.characterSpacing = 3f;
            _objectiveText.lineSpacing = 12f; // room for the optional condition under it
            var bannerRt = UiKit.Rect(_screenRoot, "Banner", new Vector2(0.5f, 1f),
                new Vector2(0, -150), new Vector2(900, 44));
            _bannerGroup = bannerRt.gameObject.AddComponent<CanvasGroup>();
            _bannerText = UiKit.Label(bannerRt, "", 20, UiKit.Pale, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900, 44), display: true);
            _hintText = UiKit.Label(_screenRoot, "", 13, UiKit.Dim, new Vector2(0.5f, 0f),
                new Vector2(0, 230), new Vector2(900, 20));
            _hintText.characterSpacing = 3f;

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
                var scale = 1f + _comboPop * 0.12f;
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
            // The optional condition rides under the objective: it is a promise
            // the player is keeping, so it has to be visible while it can still
            // be broken, not revealed on the results screen.
            var challenge = _gm.ChallengeLine;
            _objectiveText.text = UiKit.Clean(string.IsNullOrEmpty(challenge)
                ? _gm.Objective
                : $"{_gm.Objective}\n<size=70%>{challenge}</size>");
            if (_gm.BannerTimer > 0)
            {
                _bannerText.text = UiKit.Clean(_gm.Banner);
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
                var scale = 1f / _canvas.scaleFactor;
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
            var scale = 1f / _canvas.scaleFactor;
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
            var t = Mathf.RoundToInt(_gm.MissionTime);
            var duel = _gm.ModeNow == LaunchMode.Duel && _gm.CurrentDuel != null;

            UiKit.Kicker(_screenRoot, duel ? "DUEL" : "MISSION", new Vector2(0.5f, 1), new Vector2(0, -70),
                new Vector2(600, 18));
            UiKit.Label(_screenRoot, duel ? "VICTORY" : "MISSION COMPLETE", 40, UiKit.Pale,
                new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(900, 52), display: true);
            UiKit.Accent(_screenRoot, new Vector2(0.5f, 1), new Vector2(0, -140), 36);

            // Stars: quiet reveal, no fireworks.
            var stars = duel ? 0 : _gm.StarsEarned;
            if (!duel)
                for (var i = 0; i < 3; i++)
                {
                    var s = UiKit.Rect(_screenRoot, "star", new Vector2(0.5f, 1), new Vector2((i - 1) * 44f, -186),
                        new Vector2(28, 28));
                    var img = UiKit.Img(s, i < stars ? UiKit.Star : UiKit.StarOutline,
                        i < stars ? UiKit.EmberBright : new Color(1, 1, 1, 0.18f));
                    if (i < stars) StartCoroutine(StarFlyIn(s, img, 0.35f + i * 0.18f));
                }

            var opt = _gm.OptionalObjectives;
            var rows = new System.Collections.Generic.List<(string k, string v)>
            {
                ("TIME", $"{t / 60}:{t % 60:00}"),
                ("RANK", r.rank),
                ("ENEMIES DEFEATED", _gm.Kills.ToString()),
                ("OBJECTIVE", duel ? $"{_gm.CurrentDuel.name} falls" : "Complete"),
            };
            if (opt.total > 0) rows.Add(("OPTIONAL OBJECTIVES", $"{opt.done} of {opt.total}"));
            var reward = $"+{_gm.ShardsEarned} shards";
            if (_gm.BonusShardsEarned > 0) reward += $"  ·  {_gm.BonusShardsEarned} from optional";
            rows.Add(("REWARD", reward));

            var y = -236f;
            foreach (var (k, v) in rows)
            {
                UiKit.Kicker(_screenRoot, k, new Vector2(0.5f, 1), new Vector2(-150, y), new Vector2(280, 16),
                    color: UiKit.Faint, align: TextAnchor.MiddleRight);
                UiKit.Label(_screenRoot, UiKit.Clean(v), 15, k == "RANK" ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0.5f, 1), new Vector2(160, y), new Vector2(300, 18), align: TextAnchor.MiddleLeft)
                    .characterSpacing = 2f;
                UiKit.Separator(_screenRoot, new Vector2(0.5f, 1), new Vector2(0, y - 14), 600, 0.06f);
                y -= 30f;
            }

            var nextLabel = "NEXT MISSION";
            if (!duel && _gm.CurrentLevel != null && !string.IsNullOrEmpty(_gm.CurrentLevel.debrief))
            {
                var debrief = UiKit.Paragraph(_screenRoot, _gm.CurrentLevel.debrief, 13, UiKit.Dim,
                    new Vector2(0.5f, 1), new Vector2(0, y - 6), new Vector2(680, 40));
                debrief.fontStyle = FontStyles.Italic;

                // Never "mission complete" and then some other level. The ending
                // is the reason the next mission exists, so it is said here, and
                // the button names where that reason leads.
                var mission = Campaign.Campaign.Get(_gm.CurrentLevel.id);
                var following = Campaign.Campaign.Get(_gm.CurrentLevel.id + 1);
                if (mission != null && !string.IsNullOrEmpty(mission.nextReason))
                {
                    UiKit.Kicker(_screenRoot, following != null ? $"NEXT — {following.name}" : "THE END",
                        new Vector2(0.5f, 1), new Vector2(0, y - 46), new Vector2(680, 14),
                        color: UiKit.Ember, align: TextAnchor.MiddleCenter);
                    UiKit.Paragraph(_screenRoot, UiKit.Clean(mission.nextReason), 12, UiKit.Pale,
                        new Vector2(0.5f, 1), new Vector2(0, y - 62), new Vector2(680, 34), TextAnchor.UpperCenter);
                }
                if (following != null) nextLabel = $"{following.id:00} {following.name}";
                else nextLabel = "RETURN TO YORUNE";
            }

            if (duel)
                ResultButtons(("NEXT OPPONENT", () => _gm.NextDuel()), ("REMATCH", () => _gm.Retry()),
                    ("MENU", () => _gm.OpenMenu()));
            else
                ResultButtons((nextLabel, () => _gm.NextStoryLevel()), ("REPLAY", () => _gm.Retry()),
                    ("MENU", () => _gm.OpenMenu()));
        }

        private void BuildDefeat()
        {
            UiKit.Kicker(_screenRoot, "THE LANTERN GUTTERS", new Vector2(0.5f, 1), new Vector2(0, -70),
                new Vector2(600, 18));
            UiKit.Label(_screenRoot, "BUT IT DOES NOT GO OUT", 32, UiKit.Pale, new Vector2(0.5f, 1),
                new Vector2(0, -108), new Vector2(900, 44), display: true);
            UiKit.Accent(_screenRoot, new Vector2(0.5f, 1), new Vector2(0, -140), 36);
            if (_gm.ModeNow == LaunchMode.Endless)
            {
                BuildRunReport();
                ResultButtons(("RISE AGAIN", () => _gm.Retry()),
                    ("THE FORGE", () => SetScreen(Screen.Forge)),
                    ("MENU", () => _gm.OpenMenu()));
                return;
            }
            var t = Mathf.RoundToInt(_gm.MissionTime);
            UiKit.Label(_screenRoot,
                $"{t / 60}:{t % 60:00}   ·   {_gm.Kills} defeated   ·   {Mathf.RoundToInt(_gm.DamageTaken)} damage taken",
                14, UiKit.Dim, new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(800, 20));
            ResultButtons(("RISE AGAIN", () => _gm.Retry()), ("MENU", () => _gm.OpenMenu()));
        }

        /// <summary>
        /// The run report. Score is the headline because score is what the
        /// modifiers were wagered on; everything else is the evidence for it.
        /// </summary>
        private void BuildRunReport()
        {
            UiKit.Label(_screenRoot, $"{Endless.RunStats.Score:N0}", 48, UiKit.EmberBright,
                new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(600, 56), display: true);
            UiKit.Kicker(_screenRoot, Endless.RunStats.NewScoreRecord ? "A NEW BEST" : "POINTS",
                new Vector2(0.5f, 1), new Vector2(0, -236), new Vector2(600, 16),
                color: Endless.RunStats.NewScoreRecord ? UiKit.Ember : UiKit.Faint);

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
                var x = (i - (stats.Length - 1) * 0.5f) * 150f;
                UiKit.Label(_screenRoot, value, 22, record ? UiKit.EmberBright : UiKit.Pale,
                    new Vector2(0.5f, 1), new Vector2(x, -278), new Vector2(140, 26));
                UiKit.Kicker(_screenRoot, label, new Vector2(0.5f, 1), new Vector2(x, -300), new Vector2(140, 14),
                    color: record ? UiKit.Ember : UiKit.Faint);
            }
            UiKit.Separator(_screenRoot, new Vector2(0.5f, 1), new Vector2(0, -318), 760, 0.08f);
            UiKit.Label(_screenRoot,
                $"WAGER ×{Endless.RunModifiers.ActiveScoreMultiplier:0.00}   ·   "
                + Endless.RunModifiers.Describe(Endless.RunModifiers.Active),
                12, UiKit.Dim, new Vector2(0.5f, 1), new Vector2(0, -334), new Vector2(1000, 16));
            UiKit.Label(_screenRoot,
                $"+{Endless.RunStats.RyoEarned:N0} ryo"
                + (Endless.RunStats.ShardsEarned > 0 ? $"   ·   +{Endless.RunStats.ShardsEarned} shards" : ""),
                15, UiKit.EmberBright, new Vector2(0.5f, 1), new Vector2(0, -358), new Vector2(800, 20));
        }

        private void DailyLine()
        {
            var lines = new System.Collections.Generic.List<string>();
            if (_gm.DailyShards > 0) lines.Add($"Daily challenge complete  +{_gm.DailyShards}");
            if (_gm.WeeklyShards > 0) lines.Add($"Weekly challenge complete  +{_gm.WeeklyShards}");
            if (!string.IsNullOrEmpty(_gm.FeatsLine)) lines.Add(UiKit.Clean(_gm.FeatsLine));
            for (var i = 0; i < lines.Count; i++)
                UiKit.Label(_screenRoot, lines[i], 12, UiKit.Ember, new Vector2(0.5f, 0),
                    new Vector2(0, 118 + i * 18), new Vector2(900, 16)).characterSpacing = 2f;
        }

        private void ResultButtons(params (string label, System.Action action)[] buttons)
        {
            DailyLine();
            var w = 200f;
            for (var i = 0; i < buttons.Length; i++)
            {
                var x = (i - (buttons.Length - 1) * 0.5f) * (w + 16f);
                var (label, action) = buttons[i];
                UiKit.MakeButton(_screenRoot, label, new Vector2(0.5f, 0), new Vector2(x, 60),
                    new Vector2(w, 50), () => { Sfx3D.Confirm(); action(); }, 13, primary: i == 0);
            }
        }

        private IEnumerator RankReveal(TMP_Text rankText, string finalRank)
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
