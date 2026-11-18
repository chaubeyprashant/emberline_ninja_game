#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Emberline.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberline.UI
{
    /// <summary>
    /// The feel pass on the phone: a FEEL button at the bottom centre opens the
    /// <see cref="FeelTuning"/> dials, and each tap applies at once, mid-fight, with
    /// no rebuild. LOG writes the settled values to logcat as one
    /// <c>[FEEL]</c> line and shows it on the panel, ready to be baked into the
    /// authored defaults. Development builds only; hidden during a perf run.
    /// </summary>
    public class FeelPanel : MonoBehaviour
    {
        private RectTransform _panel;
        private RectTransform _toggle;
        private readonly TMP_Text[] _values = new TMP_Text[FeelTuning.Dials.Length];
        private TMP_Text _summary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isBatchMode) return;
            var go = new GameObject("FeelPanel");
            DontDestroyOnLoad(go);
            go.AddComponent<FeelPanel>();
        }

        private void Start()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210; // above EmberHud (100) and PerfOverlay (200)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 720);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Bottom centre is the one strip the stick and the verb cluster leave free.
            _toggle = (RectTransform)UiKit.MakeButton(transform, "Feel", new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(120f, 52f), Toggle, 16).transform;

            var rows = FeelTuning.Dials.Length;
            var height = 96f + rows * 60f + 70f;
            _panel = UiKit.Rect(transform, "FeelDials", new Vector2(0.5f, 0f),
                new Vector2(0f, 82f + height * 0.5f), new Vector2(600f, height));
            // Opaque to taps, so a miss between dials never swings the sword underneath.
            UiKit.Img(_panel, null, new Color(0.03f, 0.035f, 0.045f, 0.94f)).raycastTarget = true;
            UiKit.Label(_panel, "FEEL PASS", 18, UiKit.Ember, new Vector2(0f, 1f),
                new Vector2(20f, -26f), new Vector2(300f, 30f), align: TextAnchor.MiddleLeft);

            for (var i = 0; i < rows; i++)
            {
                var y = -70f - i * 60f;
                var d = FeelTuning.Dials[i];
                UiKit.Label(_panel, d.label, 18, UiKit.Pale, new Vector2(0f, 1f),
                    new Vector2(20f, y), new Vector2(240f, 40f), align: TextAnchor.MiddleLeft);
                _values[i] = UiKit.Label(_panel, "", 18, UiKit.Pale, new Vector2(0f, 1f),
                    new Vector2(370f, y), new Vector2(110f, 40f), align: TextAnchor.MiddleRight);
                var index = i;
                UiKit.MakeButton(_panel, "-", new Vector2(1f, 1f), new Vector2(-100f, y),
                    new Vector2(64f, 52f), () => Nudge(index, -1), 22);
                UiKit.MakeButton(_panel, "+", new Vector2(1f, 1f), new Vector2(-34f, y),
                    new Vector2(64f, 52f), () => Nudge(index, +1), 22);
            }

            var by = -70f - rows * 60f;
            UiKit.MakeButton(_panel, "Reset", new Vector2(0f, 1f), new Vector2(80f, by),
                new Vector2(130f, 52f), () => { FeelTuning.ResetAll(); Refresh(); }, 16);
            UiKit.MakeButton(_panel, "Log", new Vector2(0f, 1f), new Vector2(220f, by),
                new Vector2(130f, 52f), LogValues, 16);
            _summary = UiKit.Label(_panel, "", 13, UiKit.Dim, new Vector2(0f, 0f),
                new Vector2(20f, 26f), new Vector2(560f, 30f), align: TextAnchor.MiddleLeft);

            _panel.gameObject.SetActive(false);
            Refresh();
        }

        private void Update()
        {
            // A perf run measures the game, not this panel.
            var show = !DebugTools.PerfRun.Running;
            if (_toggle != null && _toggle.gameObject.activeSelf != show) _toggle.gameObject.SetActive(show);
            if (!show && _panel != null && _panel.gameObject.activeSelf) _panel.gameObject.SetActive(false);
        }

        private void Toggle() => _panel.gameObject.SetActive(!_panel.gameObject.activeSelf);

        private void Nudge(int i, int dir)
        {
            FeelTuning.Set(i, FeelTuning.Get(i) + dir * FeelTuning.Dials[i].step);
            Refresh();
        }

        private void LogValues()
        {
            var line = FeelTuning.Summary();
            Debug.Log(line);
            _summary.text = line;
        }

        private void Refresh()
        {
            for (var i = 0; i < _values.Length; i++)
            {
                var d = FeelTuning.Dials[i];
                var v = FeelTuning.Get(i);
                var mark = Mathf.Approximately(v, d.def) ? "" : " *";
                _values[i].text = d.unit == "deg" ? $"{v:+0;-0;0} deg{mark}" : $"{v:0.##}x{mark}";
            }
        }
    }
}
#endif
