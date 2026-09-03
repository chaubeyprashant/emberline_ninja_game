using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Emberline.UI
{
    /// <summary>
    /// The design system. Every screen is built from these factories, so the
    /// look is decided here once: near-black charcoal, one warm ember accent,
    /// off-white type, hairline separators, flat translucent panels and no
    /// decorative frames. Text is TextMeshPro on three generated font assets —
    /// a display face for titles, a heading face, and a body face.
    ///
    /// The previous kit drew Kenney arcade sprites (gradient buttons, an ornate
    /// nine-slice frame). Those are gone; a panel is now a colour and a hairline.
    /// </summary>
    public static class UiKit
    {
        // ------------------------------------------------------------ palette
        public static readonly Color Ink = new(0.040f, 0.045f, 0.055f);        // page black
        public static readonly Color Panel = new(0.085f, 0.09f, 0.105f);       // raised surface
        public static readonly Color PanelHi = new(0.125f, 0.13f, 0.15f);      // hovered/selected
        public static readonly Color Line = new(1f, 1f, 1f, 0.09f);            // hairline
        public static readonly Color Ember = new(0.93f, 0.44f, 0.22f);         // accent
        public static readonly Color EmberBright = new(1.00f, 0.62f, 0.34f);   // accent, lit
        public static readonly Color EmberDeep = new(0.55f, 0.22f, 0.12f);     // accent, shadowed
        public static readonly Color Pale = new(0.90f, 0.88f, 0.84f);          // primary text
        public static readonly Color Dim = new(0.58f, 0.60f, 0.64f);           // secondary text
        public static readonly Color Faint = new(0.36f, 0.38f, 0.42f);         // tertiary text
        public static readonly Color Sen = new(0.50f, 0.70f, 0.77f);           // resource blue
        public static readonly Color Blood = new(0.80f, 0.26f, 0.22f);         // health

        // -------------------------------------------------------------- fonts
        private static TMP_FontAsset _displayFont, _headingFont, _bodyFont;

        /// <summary>Titles only. Loud by design; never used for body copy.</summary>
        public static TMP_FontAsset DisplayFont =>
            _displayFont ??= Resources.Load<TMP_FontAsset>("Art/Fonts/TMP/Emberline-Display")
                             ?? HeadingFont;

        public static TMP_FontAsset HeadingFont =>
            _headingFont ??= Resources.Load<TMP_FontAsset>("Art/Fonts/TMP/Emberline-Heading")
                             ?? TMP_Settings.defaultFontAsset;

        public static TMP_FontAsset BodyFont =>
            _bodyFont ??= Resources.Load<TMP_FontAsset>("Art/Fonts/TMP/Emberline-Body")
                          ?? TMP_Settings.defaultFontAsset;

        // ------------------------------------------------------------ sprites
        private static Sprite _star, _starOutline, _circle, _white;

        public static Sprite Star => _star ??= Resources.Load<Sprite>("Art/UI/Kit/star");
        public static Sprite StarOutline => _starOutline ??= Resources.Load<Sprite>("Art/UI/Kit/star_outline");
        public static Sprite Circle => _circle ??= Resources.Load<Sprite>("Art/UI/Kit/icon_circle");

        /// <summary>Flat panels: no sprite. Kept as a property so old call sites
        /// that pass it still compile and simply get a flat fill.</summary>
        public static Sprite PanelSprite => null;
        public static Sprite PanelThin => null;
        public static Sprite ButtonSprite => null;

        /// <summary>Round touch controls use the flat circle, not the gradient disc.</summary>
        public static Sprite ButtonRound => Circle;

        /// <summary>A 4x4 white sprite for filled bars and hairlines.</summary>
        public static Sprite White =>
            _white ??= Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        // ---------------------------------------------------------- text glyphs

        /// <summary>
        /// The generated fonts do not carry ★ ◆ ◇ ¤ → ←. Legacy Text fell back to
        /// a system font for them; TMP renders a box. Strip them — the words that
        /// followed ("7 / 30", "18 SHARDS", "340 RYO") already say it, and the
        /// brief asks for fewer icons anyway. Star ratings use the star sprite.
        /// </summary>
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            if (s.IndexOfAny(Glyphs) < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '★': case '☆': case '◆': case '◇': case '¤': break;
                    case '→': case '←': sb.Append('-'); break;
                    default: sb.Append(c); break;
                }
            }
            // Collapse the doubled spaces the removals leave behind.
            return sb.ToString().Replace("  ", " ").Replace("( ", "(").Trim();
        }

        private static readonly char[] Glyphs = { '★', '☆', '◆', '◇', '¤', '→', '←' };

        // ------------------------------------------------------- action icons

        private static readonly Dictionary<string, Sprite> Icons = new();

        /// <summary>
        /// Combat-verb glyphs, rasterised in code from distance fields and cached.
        /// Known names: strike, cleave, flicker, surge, kunai, jump, target, sword,
        /// axe, spear, bow, claws, crouch, skull, bomb.
        /// </summary>
        public static Sprite Icon(string name)
        {
            if (Icons.TryGetValue(name, out var cached) && cached != null) return cached;

            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[S * S];
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var p = new Vector2(x / (S - 1f) * 2f - 1f, y / (S - 1f) * 2f - 1f);
                var d = GlyphDistance(name, p);
                var a = Mathf.Clamp01(0.5f - d * (S * 0.5f) / 1.5f);
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            Icons[name] = sprite;
            return sprite;
        }

        private static float GlyphDistance(string name, Vector2 p)
        {
            switch (name)
            {
                case "strike": // one heavy diagonal slash (cleave is two)
                    return Seg(p, new Vector2(-0.55f, -0.55f), new Vector2(0.55f, 0.55f), 0.11f);
                case "cleave": // two heavy parallel slashes
                    return Mathf.Min(
                        Seg(p, new Vector2(-0.7f, -0.35f), new Vector2(0.35f, 0.7f), 0.1f),
                        Seg(p, new Vector2(-0.35f, -0.7f), new Vector2(0.7f, 0.35f), 0.1f));
                case "flicker": // speed chevrons
                {
                    var d = float.MaxValue;
                    for (var i = 0; i < 3; i++)
                    {
                        var ox = -0.55f + i * 0.4f;
                        d = Mathf.Min(d, Seg(p, new Vector2(ox, 0.5f), new Vector2(ox + 0.3f, 0f), 0.08f));
                        d = Mathf.Min(d, Seg(p, new Vector2(ox + 0.3f, 0f), new Vector2(ox, -0.5f), 0.08f));
                    }
                    return d;
                }
                case "surge": // burst ring with spokes
                {
                    var d = Mathf.Abs(p.magnitude - 0.35f) - 0.07f;
                    for (var i = 0; i < 8; i++)
                    {
                        var a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        d = Mathf.Min(d, Seg(p, dir * 0.5f, dir * 0.8f, 0.06f));
                    }
                    return d;
                }
                case "kunai": // diamond blade over a stem
                {
                    var q = new Vector2(Mathf.Abs(p.x), p.y);
                    var blade = Mathf.Max(q.x * 2.2f + (q.y - 0.15f) * 0.9f - 0.5f, -(q.y + 0.6f));
                    var stem = Seg(p, new Vector2(0f, -0.35f), new Vector2(0f, -0.85f), 0.07f);
                    return Mathf.Min(blade, stem);
                }
                case "jump": // up chevron over a ground line
                    return Mathf.Min(
                        Mathf.Min(Seg(p, new Vector2(-0.45f, 0.05f), new Vector2(0f, 0.55f), 0.1f),
                            Seg(p, new Vector2(0f, 0.55f), new Vector2(0.45f, 0.05f), 0.1f)),
                        Seg(p, new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), 0.07f));
                case "sword": // blade with a crossguard
                    return Mathf.Min(
                        Seg(p, new Vector2(0f, -0.3f), new Vector2(0f, 0.8f), 0.09f),
                        Mathf.Min(Seg(p, new Vector2(-0.35f, -0.3f), new Vector2(0.35f, -0.3f), 0.07f),
                            Seg(p, new Vector2(0f, -0.35f), new Vector2(0f, -0.8f), 0.08f)));
                case "axe": // shaft with a wedge head
                {
                    var shaft = Seg(p, new Vector2(-0.35f, -0.8f), new Vector2(0.35f, 0.75f), 0.08f);
                    var q = p - new Vector2(0.25f, 0.4f);
                    var head = Mathf.Max(Mathf.Max(-q.x - 0.05f, q.x - 0.45f),
                        Mathf.Max(Mathf.Abs(q.y) - (0.35f - q.x * 0.5f), -0.3f));
                    return Mathf.Min(shaft, head);
                }
                case "spear": // long shaft, small leaf tip
                {
                    var shaft = Seg(p, new Vector2(-0.6f, -0.75f), new Vector2(0.45f, 0.55f), 0.06f);
                    var tip = Seg(p, new Vector2(0.45f, 0.55f), new Vector2(0.7f, 0.85f), 0.12f);
                    return Mathf.Min(shaft, tip);
                }
                case "bow": // limb arc with the string as its chord
                {
                    var arc = Mathf.Abs((p - new Vector2(-0.45f, 0f)).magnitude - 0.85f) - 0.07f;
                    arc = Mathf.Max(arc, -p.x - 0.35f);
                    var str = Seg(p, new Vector2(0.15f, -0.75f), new Vector2(0.15f, 0.75f), 0.035f);
                    return Mathf.Min(arc, str);
                }
                case "claws": // three raking lines
                {
                    var d = float.MaxValue;
                    for (var i = -1; i <= 1; i++)
                        d = Mathf.Min(d, Seg(p, new Vector2(-0.5f + i * 0.28f, 0.6f),
                            new Vector2(0.15f + i * 0.28f, -0.6f), 0.07f));
                    return d;
                }
                case "crouch": // a low, coiled figure under a ceiling line
                {
                    var ceiling = Seg(p, new Vector2(-0.7f, 0.65f), new Vector2(0.7f, 0.65f), 0.05f);
                    var head = (p - new Vector2(0.28f, 0.05f)).magnitude - 0.17f;
                    var body = Seg(p, new Vector2(0.15f, -0.1f), new Vector2(-0.35f, -0.35f), 0.13f);
                    var leg = Seg(p, new Vector2(-0.35f, -0.35f), new Vector2(0.05f, -0.6f), 0.11f);
                    return Mathf.Min(Mathf.Min(ceiling, head), Mathf.Min(body, leg));
                }
                case "skull": // cranium, sockets, jaw — the drowned
                {
                    var cranium = (p - new Vector2(0f, 0.15f)).magnitude - 0.5f;
                    var jaw = Mathf.Max(Mathf.Abs(p.x) - 0.28f, Mathf.Abs(p.y + 0.45f) - 0.2f);
                    var shape = Mathf.Min(cranium, jaw);
                    var eyeL = (p - new Vector2(-0.2f, 0.2f)).magnitude - 0.13f;
                    var eyeR = (p - new Vector2(0.2f, 0.2f)).magnitude - 0.13f;
                    return Mathf.Max(shape, -Mathf.Min(eyeL, eyeR));
                }
                case "bomb": // sphere with a lit fuse
                {
                    var sphere = (p - new Vector2(0f, -0.15f)).magnitude - 0.45f;
                    var fuse = Seg(p, new Vector2(0.2f, 0.28f), new Vector2(0.5f, 0.7f), 0.06f);
                    return Mathf.Min(sphere, fuse);
                }
                default: // target: ring, ticks, pip
                {
                    var d = Mathf.Abs(p.magnitude - 0.55f) - 0.06f;
                    d = Mathf.Min(d, p.magnitude - 0.09f);
                    for (var i = 0; i < 4; i++)
                    {
                        var a = i * Mathf.PI / 2f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        d = Mathf.Min(d, Seg(p, dir * 0.7f, dir * 0.9f, 0.06f));
                    }
                    return d;
                }
            }
        }

        private static float Seg(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t) - halfWidth;
        }

        // ------------------------------------------------------------ vignette

        private static Sprite _vignette;

        /// <summary>Radial darkening, generated once. Used for damage and for the
        /// cinematic edge falloff behind menus.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int S = 128;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                var px = new Color32[S * S];
                for (var y = 0; y < S; y++)
                for (var x = 0; x < S; x++)
                {
                    var u = (x / (S - 1f) - 0.5f) * 2f;
                    var v = (y / (S - 1f) - 0.5f) * 2f;
                    var r = Mathf.Sqrt(u * u + v * v);
                    var a = Mathf.Clamp01((r - 0.55f) / 0.7f);
                    a = a * a;
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                tex.SetPixels32(px);
                tex.Apply();
                _vignette = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
                return _vignette;
            }
        }

        // ------------------------------------------------------------ elements

        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name,
            Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>
        /// A rect stretched between two anchors — the responsive primitive. Edge
        /// insets are in reference pixels. Prefer this over Rect for anything that
        /// must survive a different aspect ratio.
        /// </summary>
        public static RectTransform Stretch(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, float left = 0, float right = 0,
            float top = 0, float bottom = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(RectTransform rt, Sprite sprite, Color color, bool sliced = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sliced && sprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// A surface: flat translucent fill with a one-pixel hairline at the
        /// top and bottom. This is the whole panel language — no frames.
        /// </summary>
        public static Image Surface(RectTransform rt, float alpha = 0.78f, bool hairlines = true)
        {
            var img = Img(rt, null, new Color(Panel.r, Panel.g, Panel.b, alpha));
            if (hairlines)
            {
                Hairline(rt, new Vector2(0, 1));
                Hairline(rt, new Vector2(0, 0));
            }
            return img;
        }

        /// <summary>One-pixel line across the full width at a vertical anchor.</summary>
        public static Image Hairline(RectTransform parent, Vector2 anchorY, float alpha = 0.09f)
        {
            var rt = Stretch(parent, "Line", new Vector2(0, anchorY.y), new Vector2(1, anchorY.y));
            rt.sizeDelta = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, anchorY.y > 0.5f ? -0.5f : 0.5f);
            return Img(rt, White, new Color(1, 1, 1, alpha));
        }

        /// <summary>A short horizontal rule, centred, for separating sections.</summary>
        public static Image Separator(Transform parent, Vector2 anchor, Vector2 pos, float width,
            float alpha = 0.12f)
        {
            var rt = Rect(parent, "Sep", anchor, pos, new Vector2(width, 1));
            return Img(rt, White, new Color(1, 1, 1, alpha));
        }

        /// <summary>Small ember dash used under headings — the one flourish permitted.</summary>
        public static Image Accent(Transform parent, Vector2 anchor, Vector2 pos, float width = 36f)
        {
            var rt = Rect(parent, "Accent", anchor, pos, new Vector2(width, 2));
            return Img(rt, White, Ember);
        }

        // ---------------------------------------------------------------- text

        private static TextAlignmentOptions Align(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };

        /// <summary>
        /// Single-line label. `display` selects the title face; otherwise the
        /// heading face. Body copy goes through Paragraph. The TextAnchor
        /// parameter is kept so existing call sites read unchanged.
        /// </summary>
        public static TMP_Text Label(Transform parent, string content, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box, bool display = false,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            // A left-aligned label pivots on its left edge, a right-aligned one on
            // its right, so `pos` means "where the text starts". Centre-pivoting a
            // left-aligned label hung half of it off the left of the column.
            var px = align is TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft ? 0f
                : align is TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight ? 1f
                : 0.5f;
            // Same rule vertically: an Upper* label pivots on its top edge, so a
            // paragraph placed "below the kicker" starts below it instead of
            // straddling it.
            var py = align is TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight ? 1f
                : align is TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight ? 0f
                : 0.5f;
            var rt = Rect(parent, "Label", anchor, pos, box, new Vector2(px, py));
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = Clean(content);
            text.font = display ? DisplayFont : HeadingFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = Align(align);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.characterSpacing = display ? 4f : 1.5f;
            return text;
        }

        /// <summary>Wrapping body copy in the body face.</summary>
        public static TMP_Text Paragraph(Transform parent, string content, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.UpperCenter)
        {
            // Alignment is passed through so the pivot follows it: a left-aligned
            // paragraph whose alignment was set afterwards kept a centre pivot and
            // hung half its width off the left of the row.
            var t = Label(parent, content, size, color, anchor, pos, box, align: align);
            t.font = BodyFont;
            t.characterSpacing = 0f;
            t.lineSpacing = 4f;
            t.enableWordWrapping = true;
            return t;
        }

        /// <summary>Uppercase tracking label — section kickers ("ACT I", "OBJECTIVE").</summary>
        public static TMP_Text Kicker(Transform parent, string content, Vector2 anchor, Vector2 pos,
            Vector2 box, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter, int size = 13)
        {
            var t = Label(parent, content.ToUpperInvariant(), size, color ?? Ember, anchor, pos, box,
                align: align);
            t.characterSpacing = 8f;
            return t;
        }

        // ------------------------------------------------------------- buttons

        /// <summary>
        /// Flat button: translucent surface, hairline, off-white label, press
        /// squash. Minimum 48dp touch target enforced. `display` puts the title
        /// face on it — for the one primary action on a screen, never for rows.
        /// </summary>
        public static Button MakeButton(Transform parent, string label, Vector2 anchor,
            Vector2 pos, Vector2 size, System.Action onClick, int fontSize = 18,
            bool display = false, Color? tint = null, bool primary = false)
        {
            size = new Vector2(Mathf.Max(size.x, 64), Mathf.Max(size.y, 52));
            var rt = Rect(parent, "Btn_" + label, anchor, pos, size);
            var fill = tint ?? (primary ? EmberDeep : Panel);
            var img = Img(rt, null, new Color(fill.r, fill.g, fill.b, primary ? 0.92f : 0.66f));
            img.raycastTarget = true;
            Hairline(rt, new Vector2(0, 1), primary ? 0.22f : 0.10f);
            Hairline(rt, new Vector2(0, 0), 0.06f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(1.25f, 1.2f, 1.15f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            btn.onClick.AddListener(() => { Core.Sfx3D.Ui(); onClick?.Invoke(); });

            var t = Label(rt, label, fontSize, primary ? Pale : Pale, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size.x - 16, size.y), display);
            t.characterSpacing = display ? 4f : 3f;
            if (!display) t.text = Clean(label).ToUpperInvariant();

            // Press feedback on touch-down, not release.
            var punch = rt.gameObject.AddComponent<ButtonPunch>();
            var trig = rt.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => punch.Punch());
            trig.triggers.Add(down);
            return btn;
        }

        /// <summary>
        /// Squash-and-settle on press. On a touchscreen there is no hover, so
        /// this is the only feedback that a tap registered before the action lands.
        /// </summary>
        public class ButtonPunch : MonoBehaviour
        {
            private float _t;

            public void Punch() => _t = 1f;

            private void Update()
            {
                if (_t <= 0f) return;
                _t = Mathf.Max(0f, _t - Time.unscaledDeltaTime * 6f);
                var s = 1f - 0.06f * _t;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>Filled bar: hairline-framed track + fill. Returns the fill image.</summary>
        public static Image MakeBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Color fillColor, Vector2? pivot = null)
        {
            var back = Rect(parent, "BarBack", anchor, pos, size, pivot);
            Img(back, White, new Color(0, 0, 0, 0.45f));
            var fillRt = Rect(back, "Fill", new Vector2(0, 0.5f), new Vector2(1, 0),
                new Vector2(size.x - 2, size.y - 2), new Vector2(0, 0.5f));
            var fill = Img(fillRt, White, fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        // ---------------------------------------------------------- transitions

        private static Runner _runner;

        private static Runner GetRunner()
        {
            if (_runner != null) return _runner;
            var go = new GameObject("UiRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
            return _runner;
        }

        /// <summary>Coroutine host for transitions; a static class cannot own them.</summary>
        public class Runner : MonoBehaviour { }

        /// <summary>
        /// Fade a screen root in, with a small upward settle. Fast: 180ms. A
        /// transition the player waits for is a transition that was too long.
        /// </summary>
        public static void Enter(RectTransform root, float seconds = 0.18f, float rise = 10f)
        {
            if (root == null || !Application.isPlaying) return;
            // Not `??`: C#'s null-coalescing operator bypasses Unity's overloaded
            // equality, so a CanvasGroup destroyed by an earlier screen rebuild
            // comes back as a live-looking wrapper around a dead native object and
            // every access to it throws. `== null` is the check that sees that.
            var g = root.GetComponent<CanvasGroup>();
            if (g == null) g = root.gameObject.AddComponent<CanvasGroup>();
            GetRunner().StartCoroutine(EnterRoutine(root, g, seconds, rise));
        }

        private static IEnumerator EnterRoutine(RectTransform root, CanvasGroup g, float seconds, float rise)
        {
            var from = root.anchoredPosition - new Vector2(0, rise);
            var to = root.anchoredPosition;
            var t = 0f;
            if (g == null) yield break;
            g.alpha = 0f;
            // A screen can be rebuilt mid-fade, which destroys the CanvasGroup
            // while the root survives. Touching it then throws and the coroutine
            // dies, leaving the screen stranded at whatever alpha it reached.
            while (t < seconds && root != null && g != null)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
                g.alpha = k;
                root.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }
            if (root == null) yield break;
            if (g != null) g.alpha = 1f;
            root.anchoredPosition = to;
        }

        /// <summary>Full-screen black plate that fades out — for scene and screen cuts.</summary>
        public static void FadeFromBlack(Transform parent, float seconds = 0.35f)
        {
            if (!Application.isPlaying) return;
            var rt = Group(parent, "FadePlate");
            var img = Img(rt, null, Color.black);
            img.raycastTarget = false;
            rt.SetAsLastSibling();
            GetRunner().StartCoroutine(FadeRoutine(img, seconds));
        }

        private static IEnumerator FadeRoutine(Image img, float seconds)
        {
            var t = 0f;
            while (t < seconds && img != null)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }
    }
}
