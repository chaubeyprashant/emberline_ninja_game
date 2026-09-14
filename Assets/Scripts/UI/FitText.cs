using TMPro;
using UnityEngine;

namespace Emberline.UI
{
    /// <summary>
    /// Keeps a label inside its own box. UiKit labels never wrap and let text
    /// spill past their rect, so a long mission or chapter name ran into the card
    /// beside it ("ASHES OF YORUNE" over chapter 2's LOCKED). This shrinks the
    /// font until the text fits: by width for single-line labels, by height for
    /// wrapped labels that opt in with <see cref="fitHeight"/>. It never goes
    /// below <see cref="minScale"/> of the authored size; past that the text ends
    /// in an ellipsis instead of overlapping anything.
    ///
    /// It re-fits whenever the text, spacing, wrap mode or box changes, because
    /// callers adjust labels after UiKit builds them (Kicker widens the spacing,
    /// MakeButton uppercases the text) and some labels change text at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FitText : MonoBehaviour
    {
        [Range(0.3f, 1f)] public float minScale = 0.6f;

        /// <summary>Wrapped labels only: shrink until the lines fit the box height.</summary>
        public bool fitHeight;

        private TMP_Text _t;
        private float _authored, _fitted = -1f;
        private string _text;
        private float _spacing = float.NaN;
        private bool _wrap, _fitHeight;
        private Vector2 _size;

        private void Awake() => _t = GetComponent<TMP_Text>();

        private void OnEnable() => _text = null;

        private void LateUpdate()
        {
            if (_t == null) return;

            // A font size we did not set is a caller's new authored size.
            if (_fitted < 0f || Mathf.Abs(_t.fontSize - _fitted) > 0.01f)
            {
                _authored = _t.fontSize;
                _text = null;
            }

            var size = ((RectTransform)transform).rect.size;
            var text = _t.text;
            if (text == _text && _t.characterSpacing == _spacing && _t.enableWordWrapping == _wrap
                && fitHeight == _fitHeight && size == _size)
                return;

            _text = text;
            _spacing = _t.characterSpacing;
            _wrap = _t.enableWordWrapping;
            _fitHeight = fitHeight;
            _size = size;
            Fit(size);
            _fitted = _t.fontSize;
        }

        private void Fit(Vector2 box)
        {
            _t.fontSize = _authored;
            _t.overflowMode = TextOverflowModes.Overflow;
            if (box.x < 8f || string.IsNullOrEmpty(_t.text)) return;

            var byHeight = fitHeight && _t.enableWordWrapping;
            // Paragraphs keep the flow they were authored with.
            if (_t.enableWordWrapping && !byHeight) return;

            var floor = _authored * Mathf.Clamp(minScale, 0.3f, 1f);
            for (var i = 0; i < 16; i++)
            {
                var pref = _t.GetPreferredValues(_t.text,
                    byHeight ? box.x : float.PositiveInfinity, float.PositiveInfinity);
                var over = byHeight ? pref.y > box.y + 0.5f : pref.x > box.x + 0.5f;
                if (!over) return;

                var next = _t.fontSize * 0.93f;
                if (next <= floor)
                {
                    _t.fontSize = floor;
                    _t.overflowMode = TextOverflowModes.Ellipsis;
                    return;
                }
                _t.fontSize = next;
            }
        }
    }
}
