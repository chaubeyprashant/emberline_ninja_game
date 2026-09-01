using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Emberline.UI
{
    /// <summary>
    /// Factory helpers over the imported Kenney UI sprites + OFL fonts so the
    /// whole interface can be built in code with a consistent ember-and-ink
    /// look: dark panels, ember accents, Shojumaru display type.
    /// </summary>
    public static class UiKit
    {
        // Palette.
        public static readonly Color Ink = new(0.055f, 0.07f, 0.10f);
        public static readonly Color Panel = new(0.11f, 0.115f, 0.15f);
        public static readonly Color Ember = new(1f, 0.42f, 0.29f);
        public static readonly Color EmberBright = new(1f, 0.62f, 0.35f);
        public static readonly Color Pale = new(0.92f, 0.90f, 0.86f);
        public static readonly Color Dim = new(0.62f, 0.66f, 0.72f);
        public static readonly Color Sen = new(0.5f, 0.7f, 0.77f);
        public static readonly Color Blood = new(0.88f, 0.33f, 0.27f);

        private static Font _display, _body;
        private static Sprite _panel, _panelThin, _button, _buttonRound, _star, _starOutline, _circle;

        public static Font Display =>
            _display ??= Resources.Load<Font>("Art/Fonts/Shojumaru-Regular")
                         ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Font Body =>
            _body ??= Resources.Load<Font>("Art/Fonts/Rajdhani-Bold")
                      ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Sprite PanelSprite => _panel ??= Resources.Load<Sprite>("Art/UI/Borders/panel-000");
        public static Sprite PanelThin => _panelThin ??= Resources.Load<Sprite>("Art/UI/Borders/panel-015");
        public static Sprite ButtonSprite => _button ??= Resources.Load<Sprite>("Art/UI/Kit/button_rectangle_depth_gradient");
        public static Sprite ButtonRound => _buttonRound ??= Resources.Load<Sprite>("Art/UI/Kit/button_round_depth_gradient");
        public static Sprite Star => _star ??= Resources.Load<Sprite>("Art/UI/Kit/star");
        public static Sprite StarOutline => _starOutline ??= Resources.Load<Sprite>("Art/UI/Kit/star_outline");
        public static Sprite Circle => _circle ??= Resources.Load<Sprite>("Art/UI/Kit/icon_circle");

        // ------------------------------------------------------- action icons

        private static readonly Dictionary<string, Sprite> Icons = new();

        /// <summary>
        /// Combat-verb glyphs, rasterised in code. The Kenney kit ships no action
        /// icons and the project draws its own art anyway (app icon, vignette), so
        /// these are painted from line/arc distance fields at load and cached.
        /// Known names: strike, cleave, flicker, surge, kunai, jump, target.
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
            // SetPixels once rather than per-pixel SetPixel: seven icons build at
            // HUD construction and the per-call overhead is the expensive part.
            var pixels = new Color32[S * S];
            for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                // Normalised to -1..1 so the glyph maths reads geometrically.
                var p = new Vector2(x / (S - 1f) * 2f - 1f, y / (S - 1f) * 2f - 1f);
                var d = GlyphDistance(name, p);
                // 1.5px of feather keeps the strokes clean without looking fuzzy.
                var a = Mathf.Clamp01(1f - d * (S * 0.5f) / 1.5f);
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, S, S),
                new Vector2(0.5f, 0.5f));
            Icons[name] = sprite;
            return sprite;
        }

        /// <summary>Signed distance (0 = on the stroke) for one glyph at point p.</summary>
        private static float GlyphDistance(string name, Vector2 p)
        {
            switch (name)
            {
                case "strike": // one heavy diagonal slash (cleave is two)
                    return Seg(p, new Vector2(-0.66f, -0.52f), new Vector2(0.66f, 0.52f), 0.155f);
                case "cleave": // two heavy parallel slashes
                    return Mathf.Min(
                        Seg(p, new Vector2(-0.72f, -0.30f), new Vector2(0.52f, 0.74f), 0.13f),
                        Seg(p, new Vector2(-0.52f, -0.74f), new Vector2(0.72f, 0.30f), 0.13f));
                case "flicker": // speed chevrons
                    return Mathf.Min(Mathf.Min(
                            Chevron(p, new Vector2(-0.55f, 0f), 0.34f, 0.11f),
                            Chevron(p, new Vector2(-0.05f, 0f), 0.34f, 0.11f)),
                        Chevron(p, new Vector2(0.45f, 0f), 0.34f, 0.11f));
                case "surge": // burst ring with spokes
                {
                    var d = Mathf.Abs(p.magnitude - 0.34f) - 0.10f;
                    for (var i = 0; i < 6; i++)
                    {
                        var a = i * 60f * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        d = Mathf.Min(d, Seg(p, dir * 0.58f, dir * 0.92f, 0.09f));
                    }
                    return d;
                }
                case "kunai": // diamond blade over a stem
                {
                    var blade = Diamond(p - new Vector2(0f, 0.24f), new Vector2(0.30f, 0.52f), 0.075f);
                    var stem = Seg(p, new Vector2(0f, -0.30f), new Vector2(0f, -0.82f), 0.075f);
                    var grip = Seg(p, new Vector2(-0.20f, -0.34f), new Vector2(0.20f, -0.34f), 0.075f);
                    return Mathf.Min(blade, Mathf.Min(stem, grip));
                }
                case "jump": // up chevron over a ground line
                    return Mathf.Min(
                        Chevron(RotateCW(p + new Vector2(0f, -0.18f)), Vector2.zero, 0.48f, 0.12f),
                        Seg(p, new Vector2(-0.5f, -0.62f), new Vector2(0.5f, -0.62f), 0.10f));
                case "sword": // blade with a crossguard
                    return Mathf.Min(Mathf.Min(
                            Seg(p, new Vector2(0f, -0.30f), new Vector2(0f, 0.80f), 0.085f),
                            Seg(p, new Vector2(-0.28f, -0.30f), new Vector2(0.28f, -0.30f), 0.085f)),
                        Seg(p, new Vector2(0f, -0.34f), new Vector2(0f, -0.80f), 0.06f));
                case "axe": // shaft with a wedge head
                {
                    var shaft = Seg(p, new Vector2(0.18f, -0.84f), new Vector2(0.18f, 0.76f), 0.085f);
                    // Three strokes closing a triangle read as a blade; an arc here
                    // just looked like a pennant.
                    var top = new Vector2(0.18f, 0.74f);
                    var edge = new Vector2(-0.58f, 0.40f);
                    var low = new Vector2(0.18f, 0.06f);
                    var head = Mathf.Min(Mathf.Min(
                            Seg(p, top, edge, 0.10f), Seg(p, edge, low, 0.10f)),
                        Seg(p, low, top, 0.16f));
                    return Mathf.Min(shaft, head);
                }
                case "spear": // long shaft, small leaf tip
                    return Mathf.Min(
                        Seg(p, new Vector2(0f, -0.86f), new Vector2(0f, 0.42f), 0.07f),
                        Diamond(p - new Vector2(0f, 0.60f), new Vector2(0.20f, 0.34f), 0.065f));
                case "bow": // limb arc, opening right, with the string as its chord
                    return Mathf.Min(
                        Arc(p - new Vector2(0.25f, 0f), 0.75f, 118f, 242f, 0.10f),
                        Seg(p, new Vector2(-0.10f, -0.66f), new Vector2(-0.10f, 0.66f), 0.05f));
                case "claws": // three raking lines
                    return Mathf.Min(Mathf.Min(
                            Seg(p, new Vector2(-0.62f, -0.42f), new Vector2(-0.18f, 0.72f), 0.085f),
                            Seg(p, new Vector2(-0.14f, -0.58f), new Vector2(0.14f, 0.72f), 0.085f)),
                        Seg(p, new Vector2(0.34f, -0.42f), new Vector2(0.62f, 0.60f), 0.085f));
                case "crouch": // a low, coiled figure under a ceiling line
                {
                    var head = (p - new Vector2(-0.10f, 0.10f)).magnitude - 0.20f;
                    var back = Seg(p, new Vector2(-0.02f, -0.02f), new Vector2(0.40f, -0.24f), 0.11f);
                    var leg = Seg(p, new Vector2(0.40f, -0.24f), new Vector2(0.16f, -0.62f), 0.10f);
                    var foot = Seg(p, new Vector2(0.16f, -0.62f), new Vector2(-0.42f, -0.62f), 0.10f);
                    var ceil = Seg(p, new Vector2(-0.62f, 0.62f), new Vector2(0.62f, 0.62f), 0.07f);
                    return Mathf.Min(Mathf.Min(Mathf.Min(head, back), Mathf.Min(leg, foot)), ceil);
                }
                case "skull": // cranium, sockets, jaw — the drowned
                {
                    var dome = Mathf.Abs((p - new Vector2(0f, 0.16f)).magnitude - 0.46f) - 0.10f;
                    var jaw = Seg(p, new Vector2(-0.26f, -0.46f), new Vector2(0.26f, -0.46f), 0.11f);
                    var sockets = Mathf.Min(
                        (p - new Vector2(-0.17f, 0.20f)).magnitude - 0.115f,
                        (p - new Vector2(0.17f, 0.20f)).magnitude - 0.115f);
                    // Sockets are cut out of the dome, so union the rim with the jaw
                    // and let the eye discs paint over as solid marks.
                    return Mathf.Min(Mathf.Min(dome, jaw), sockets);
                }
                case "bomb": // sphere with a lit fuse
                    return Mathf.Min(
                        (p - new Vector2(0f, -0.18f)).magnitude - 0.46f,
                        Seg(p, new Vector2(0.18f, 0.26f), new Vector2(0.52f, 0.76f), 0.075f));
                default: // target: ring, ticks, pip
                {
                    var d = Mathf.Abs(p.magnitude - 0.52f) - 0.085f;
                    d = Mathf.Min(d, p.magnitude - 0.12f);
                    for (var i = 0; i < 4; i++)
                    {
                        var a = i * 90f * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        d = Mathf.Min(d, Seg(p, dir * 0.66f, dir * 0.94f, 0.085f));
                    }
                    return d;
                }
            }
        }

        private static Vector2 RotateCW(Vector2 p) => new(p.y, -p.x);

        private static float Seg(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t) - halfWidth;
        }

        /// <summary>Wedge of an annulus — used for the curved slash.</summary>
        private static float Arc(Vector2 p, float radius, float fromDeg, float toDeg, float halfWidth)
        {
            var ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            var inSweep = fromDeg <= toDeg ? ang >= fromDeg && ang <= toDeg
                : ang >= fromDeg || ang <= toDeg;
            if (inSweep) return Mathf.Abs(p.magnitude - radius) - halfWidth;
            // Outside the sweep: fall back to the nearer cap so the ends round off.
            var f = new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * radius;
            var t2 = new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad)) * radius;
            return Mathf.Min(Vector2.Distance(p, f), Vector2.Distance(p, t2)) - halfWidth;
        }

        private static float Chevron(Vector2 p, Vector2 centre, float size, float halfWidth)
        {
            var a = centre + new Vector2(-size * 0.5f, size);
            var b = centre + new Vector2(size * 0.5f, 0f);
            var c = centre + new Vector2(-size * 0.5f, -size);
            return Mathf.Min(Seg(p, a, b, halfWidth), Seg(p, b, c, halfWidth));
        }

        private static float Diamond(Vector2 p, Vector2 half, float halfWidth)
        {
            var top = new Vector2(0f, half.y);
            var right = new Vector2(half.x, 0f);
            var bottom = new Vector2(0f, -half.y);
            var left = new Vector2(-half.x, 0f);
            return Mathf.Min(
                Mathf.Min(Seg(p, top, right, halfWidth), Seg(p, right, bottom, halfWidth)),
                Mathf.Min(Seg(p, bottom, left, halfWidth), Seg(p, left, top, halfWidth)));
        }

        private static Sprite _vignette;

        /// <summary>
        /// Radial falloff for the damage vignette: clear in the middle, opaque at
        /// the corners. Generated once in code — the Kenney kit has no such sprite
        /// and a flat red overlay reads as a bug rather than a hit.
        /// </summary>
        public static Sprite Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int S = 64;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                var c = (S - 1) * 0.5f;
                for (var y = 0; y < S; y++)
                for (var x = 0; x < S; x++)
                {
                    // 0 at centre → 1 at the edge, eased so the middle stays clear.
                    var d = Mathf.Clamp01(new Vector2(x - c, y - c).magnitude / c);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(d, 2.6f)));
                }
                tex.Apply();
                _vignette = Sprite.Create(tex, new UnityEngine.Rect(0, 0, S, S),
                    new Vector2(0.5f, 0.5f));
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

        public static Image Img(RectTransform rt, Sprite sprite, Color color, bool sliced = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sliced && sprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string content, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box, bool display = false,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, "Label", anchor, pos, box);
            var text = rt.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = display ? Display : Body;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Text Paragraph(Transform parent, string content, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var t = Label(parent, content, size, color, anchor, pos, box);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.alignment = TextAnchor.UpperCenter;
            return t;
        }

        /// <summary>Kenney-skinned button with press SFX. Min touch target enforced.</summary>
        public static Button MakeButton(Transform parent, string label, Vector2 anchor,
            Vector2 pos, Vector2 size, System.Action onClick, int fontSize = 22,
            bool display = false, Color? tint = null)
        {
            size = new Vector2(Mathf.Max(size.x, 64), Mathf.Max(size.y, 56)); // ≥48dp at 720p
            var rt = Rect(parent, "Btn_" + label, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ButtonSprite;
            img.color = tint ?? Panel;
            if (ButtonSprite != null) img.type = Image.Type.Sliced;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(() => { Core.Sfx3D.Ui(); onClick?.Invoke(); });
            Label(rt, label, fontSize, Pale, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size.x - 12, size.y), display);
            return btn;
        }

        /// <summary>
        /// Squash-and-settle on press. Combat buttons had no press feedback at all,
        /// which on a touchscreen (no cursor, no hover) left you unsure a tap
        /// registered until the swing came out.
        /// </summary>
        public class ButtonPunch : MonoBehaviour
        {
            private float _t;

            public void Punch() => _t = 1f;

            private void Update()
            {
                if (_t <= 0f) return;
                // Unscaled: hit-stop must not freeze the button mid-squash.
                _t = Mathf.Max(0f, _t - Time.unscaledDeltaTime * 5.5f);
                var s = 1f - 0.16f * _t * (1f + Mathf.Sin(_t * 9f) * 0.25f);
                transform.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>Filled bar: dark backing + colored fill, returns the fill image.</summary>
        public static Image MakeBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Color fillColor, Vector2? pivot = null)
        {
            var back = Rect(parent, "BarBack", anchor, pos, size, pivot);
            Img(back, null, new Color(0, 0, 0, 0.55f));
            var fillRt = Rect(back, "Fill", new Vector2(0, 0.5f), new Vector2(1, 0),
                new Vector2(size.x - 2, size.y - 2), new Vector2(0, 0.5f));
            var fill = Img(fillRt, null, fillColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            // Filled images need a sprite to fill; a plain white works.
            fill.sprite = Sprite.Create(Texture2D.whiteTexture,
                new UnityEngine.Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return fill;
        }
    }
}
