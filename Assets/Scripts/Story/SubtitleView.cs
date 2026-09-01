using Emberline.UI;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Emberline.Story
{
    /// <summary>
    /// Subtitle band: speaker above, line below, sitting inside the letterbox.
    /// Deliberately small and low-contrast — the brief asks for no giant text, and
    /// a subtitle that shouts competes with the shot it is supposed to serve.
    /// </summary>
    public class SubtitleView : MonoBehaviour
    {
        private TMP_Text _speaker, _line;
        private CanvasGroup _group;
        private float _fade;

        public static SubtitleView Build(Transform parent)
        {
            var rt = UiKit.Rect(parent, "Subtitles", new Vector2(0.5f, 0f),
                new Vector2(0, 118), new Vector2(1400, 96));
            var v = rt.gameObject.AddComponent<SubtitleView>();
            v._group = rt.gameObject.AddComponent<CanvasGroup>();
            v._group.alpha = 0f;
            v._group.blocksRaycasts = false;

            v._speaker = UiKit.Label(rt, "", 15, UiKit.Ember, new Vector2(0.5f, 1f),
                new Vector2(0, -12), new Vector2(1200, 20));
            v._line = UiKit.Label(rt, "", 22, new Color(0.93f, 0.93f, 0.92f),
                new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(1300, 56));
            return v;
        }

        /// <summary>Empty line clears the band.</summary>
        public void Show(string speaker, string line)
        {
            var text = Loc.T(line);
            if (string.IsNullOrEmpty(text)) { _fade = 0f; return; }
            _speaker.text = Loc.T(speaker ?? "").ToUpperInvariant();
            _line.text = text;
            _fade = 1f;
        }

        public void Clear() => _fade = 0f;

        private void Update()
        {
            // Unscaled: subtitles must keep fading while the game is paused.
            _group.alpha = Mathf.MoveTowards(_group.alpha, _fade, Time.unscaledDeltaTime * 4f);
        }
    }
}
