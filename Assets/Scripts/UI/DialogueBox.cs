using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Emberline.Core;

namespace Emberline.UI
{
    /// <summary>
    /// Briefing dialogue: portrait emblem + speaker name + typewriter text.
    /// Lines are "SPEAKER|text". Tap the panel to complete or advance; a SKIP
    /// button jumps to the end. Portraits are generated emblems (framed initial
    /// in the speaker's color) until real 2D art lands.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        private string[] _lines;
        private int _index;
        private float _chars;
        private TMP_Text _nameText, _bodyText, _counter;
        private TMP_Text _portraitInitial;
        private Image _portraitFrame;
        private System.Action _onDone;
        private bool _done;

        public static DialogueBox Show(Transform parent, string[] lines, System.Action onDone = null)
        {
            var rt = UiKit.Rect(parent, "DialogueBox", new Vector2(0.5f, 0f),
                new Vector2(0, 196), new Vector2(860, 150), new Vector2(0.5f, 0f));
            var box = rt.gameObject.AddComponent<DialogueBox>();
            box._lines = lines;
            box._onDone = onDone;
            box.Build(rt);
            box.SetLine(0);
            return box;
        }

        private static Color SpeakerColor(string speaker) => speaker switch
        {
            "RENZO" => UiKit.Ember,
            "YOTSU" => new Color(0.9f, 0.78f, 0.45f),
            "GORO" => new Color(0.9f, 0.3f, 0.22f),
            "WHISPER" => new Color(0.6f, 0.75f, 0.95f),
            "KAGACHI" => new Color(0.35f, 0.8f, 0.6f),
            "JIN" => new Color(0.6f, 0.68f, 0.95f),
            _ => UiKit.Pale,
        };

        private void Build(RectTransform rt)
        {
            var panel = UiKit.Surface(rt, 0.92f);
            panel.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = panel;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Advance);

            var pFrame = UiKit.Rect(rt, "Portrait", new Vector2(0f, 0.5f), new Vector2(78, 0),
                new Vector2(96, 96));
            _portraitFrame = UiKit.Img(pFrame, null, UiKit.PanelHi);
            UiKit.Hairline(pFrame, new Vector2(0, 0), 0.35f);
            _portraitInitial = UiKit.Label(pFrame, "?", 44, UiKit.Pale, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(90, 90), display: true);

            _nameText = UiKit.Label(rt, "", 19, UiKit.Ember, new Vector2(0f, 1f),
                new Vector2(320, -26), new Vector2(400, 26), align: TextAnchor.MiddleLeft);
            _bodyText = UiKit.Paragraph(rt, "", 19, new Color(0.88f, 0.89f, 0.9f),
                new Vector2(0.5f, 0.5f), new Vector2(60, -12), new Vector2(660, 90));
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
            _counter = UiKit.Label(rt, "", 14, UiKit.Dim, new Vector2(1f, 0f),
                new Vector2(-52, 18), new Vector2(80, 20));

            UiKit.MakeButton(rt, "SKIP", new Vector2(1f, 1f), new Vector2(-58, -28),
                new Vector2(88, 44), () =>
                {
                    _done = true;
                    SetLine(_lines.Length - 1);
                    _chars = float.MaxValue;
                    _onDone?.Invoke();
                }, 15);
        }

        private void SetLine(int i)
        {
            _index = Mathf.Clamp(i, 0, _lines.Length - 1);
            _chars = 0f;
            var parts = _lines[_index].Split('|');
            var speaker = parts.Length > 1 ? parts[0] : "";
            _nameText.text = speaker;
            _nameText.color = SpeakerColor(speaker);
            _portraitInitial.text = speaker.Length > 0 ? speaker.Substring(0, 1) : "?";
            _portraitInitial.color = SpeakerColor(speaker);
            _portraitFrame.color = Color.Lerp(UiKit.Panel, SpeakerColor(speaker), 0.25f);
            _counter.text = $"{_index + 1}/{_lines.Length}";
        }

        private string Body(int i)
        {
            var parts = _lines[i].Split('|');
            return parts.Length > 1 ? parts[1] : parts[0];
        }

        private void Advance()
        {
            Sfx3D.Ui();
            var body = Body(_index);
            if (_chars < body.Length) { _chars = body.Length; return; } // finish typing
            if (_index < _lines.Length - 1) { SetLine(_index + 1); return; }
            if (!_done) { _done = true; _onDone?.Invoke(); }
        }

        private void Update()
        {
            var body = Body(_index);
            _chars = Mathf.Min(body.Length, _chars + Time.deltaTime * 40f);
            _bodyText.text = body.Substring(0, Mathf.Min(body.Length, Mathf.FloorToInt(_chars)));
        }
    }
}
