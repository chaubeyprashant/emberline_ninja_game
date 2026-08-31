using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Emberline.Core;

namespace Emberline.UI
{
    /// <summary>
    /// 3-second boss intro: gameplay freezes, the camera sweeps to a low hero
    /// shot of the boss (who plays a taunt), letterbox bars slide in, and a name
    /// card (Shojumaru display font + Kenney panel) types out the taunt line
    /// with an audio sting. Everything is built in code — no scene wiring.
    /// </summary>
    public class BossIntroDirector : MonoBehaviour
    {
        private const float Duration = 3.4f;

        public static void Play(GameManager gm, CameraRig cam, Transform boss,
            CharacterRig bossRig, string bossName, string title, string taunt)
        {
            var host = new GameObject("BossIntro");
            var d = host.AddComponent<BossIntroDirector>();
            d.StartCoroutine(d.Run(gm, cam, boss, bossRig, bossName, title, taunt));
        }

        private Canvas _canvas;
        private Text _nameText, _titleText, _tauntText;
        private RectTransform _barTop, _barBottom;
        private CanvasGroup _cardGroup;

        private IEnumerator Run(GameManager gm, CameraRig cam, Transform boss,
            CharacterRig bossRig, string bossName, string title, string taunt)
        {
            gm.SetCinematic(true);
            cam?.PlayCinematic(boss, Duration);
            bossRig?.PlayOneShot(RigPose.Taunt, Duration * 0.8f);
            BuildOverlay(bossName, title);
            Sfx3D.Sting();

            var t = 0f;
            taunt ??= "";
            while (t < Duration)
            {
                t += Time.deltaTime;
                // Letterbox slide-in over the first half second.
                var slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
                var barH = Screen.height * 0.11f * slide;
                _barTop.sizeDelta = new Vector2(0, barH);
                _barBottom.sizeDelta = new Vector2(0, barH);
                // Card fades in, then the taunt types out.
                _cardGroup.alpha = Mathf.Clamp01((t - 0.35f) / 0.4f);
                var chars = Mathf.FloorToInt(Mathf.Clamp01((t - 0.9f) / 1.3f) * taunt.Length);
                _tauntText.text = taunt.Substring(0, Mathf.Clamp(chars, 0, taunt.Length));
                if (boss == null) break; // boss somehow died/despawned — bail out
                yield return null;
            }

            // Fade everything out fast.
            var fade = 0f;
            while (fade < 0.25f)
            {
                fade += Time.deltaTime;
                var a = 1f - fade / 0.25f;
                _cardGroup.alpha = a;
                _barTop.GetComponent<Image>().color = new Color(0, 0, 0, 0.92f * a);
                _barBottom.GetComponent<Image>().color = new Color(0, 0, 0, 0.92f * a);
                yield return null;
            }

            cam?.StopCinematic();
            gm.SetCinematic(false);
            Destroy(gameObject);
        }

        private void BuildOverlay(string bossName, string title)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            _barTop = Bar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f));
            _barBottom = Bar(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f));

            // Name card, lower third.
            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(transform, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.anchorMin = new Vector2(0.5f, 0f);
            cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.pivot = new Vector2(0.5f, 0f);
            cardRt.anchoredPosition = new Vector2(0, 96);
            cardRt.sizeDelta = new Vector2(680, 150);
            _cardGroup = card.AddComponent<CanvasGroup>();
            _cardGroup.alpha = 0f;

            var panel = card.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("Art/UI/Borders/panel-000");
            if (sprite != null)
            {
                panel.sprite = sprite;
                panel.type = Image.Type.Sliced;
                panel.color = new Color(0.14f, 0.10f, 0.10f, 0.88f);
            }
            else panel.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            var display = Resources.Load<Font>("Art/Fonts/Shojumaru-Regular");
            var body = Resources.Load<Font>("Art/Fonts/Rajdhani-Medium");
            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _nameText = MakeText(card.transform, bossName, display != null ? display : fallback,
                40, new Color(1f, 0.62f, 0.35f), new Vector2(0, 42));
            _titleText = MakeText(card.transform, title, body != null ? body : fallback,
                20, new Color(0.9f, 0.89f, 0.86f), new Vector2(0, 8));
            _tauntText = MakeText(card.transform, "", body != null ? body : fallback,
                19, new Color(0.78f, 0.8f, 0.84f), new Vector2(0, -30));
            _tauntText.fontStyle = FontStyle.Italic;
        }

        private RectTransform Bar(Vector2 min, Vector2 max, Vector2 pivot)
        {
            var go = new GameObject("Bar", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(0, 0);
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.92f);
            img.raycastTarget = false;
            return rt;
        }

        private static Text MakeText(Transform parent, string content, Font font,
            int size, Color color, Vector2 pos)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-40, 46);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
