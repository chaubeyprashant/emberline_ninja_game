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
