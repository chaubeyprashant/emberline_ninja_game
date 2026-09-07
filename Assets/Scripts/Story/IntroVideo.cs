using System.Collections;
using Emberline.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Emberline.Story
{
    /// <summary>
    /// The first-launch intro: a 25-second cut of real gameplay that plays once,
    /// the first time the app is opened, before the opening cinematic.
    ///
    /// The file ships in StreamingAssets (Marketing/Intro builds it), so the
    /// VideoPlayer reads it by URL — on Android that is the jar: path inside the
    /// APK, which the player streams without unpacking. It renders to a 720p
    /// texture on a full-screen RawImage that fits the 16:9 picture inside
    /// whatever the phone's aspect is, black either side.
    ///
    /// The player can tap anywhere to skip once a second has played — long
    /// enough to see it is a video and not a hung splash, short enough that
    /// nobody is held hostage. Every failure path (file missing, decoder error,
    /// prepare timing out) falls through to the callback, so a broken video
    /// can never keep a fresh install from reaching the game.
    /// </summary>
    public class IntroVideo : MonoBehaviour
    {
        public const string FileName = "intro.mp4";

        public static IntroVideo Active { get; private set; }

        /// <summary>Seconds of playback before a tap is allowed to skip.</summary>
        private const float SkipArmTime = 1.0f;

        /// <summary>How long Prepare may take before the video is given up on.</summary>
        private const float PrepareTimeout = 4.0f;

        public static IntroVideo Play(System.Action onDone)
        {
            var host = new GameObject("IntroVideo");
            var v = host.AddComponent<IntroVideo>();
            v._onDone = onDone;
            Active = v;
            v.StartCoroutine(v.Run());
            return v;
        }

        /// <summary>Where the file is read from — shared with the editor check.</summary>
        public static string Url =>
            System.IO.Path.Combine(Application.streamingAssetsPath, FileName);

        private System.Action _onDone;
        private VideoPlayer _player;
        private RenderTexture _rt;
        private RawImage _screen;
        private Image _fade;
        private CanvasGroup _hint;
        private bool _finished, _error, _ending;
        private float _played;

        private IEnumerator Run()
        {
            BuildOverlay();

            _rt = new RenderTexture(1280, 720, 0) { name = "IntroVideoRT" };
            _screen.texture = _rt;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.source = VideoSource.Url;
            _player.url = Url;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _rt;
            _player.aspectRatio = VideoAspectRatio.FitInside;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            _player.isLooping = false;
            _player.skipOnDrop = true;
            _player.errorReceived += (_, msg) =>
            {
                Debug.LogWarning("[Intro] video error: " + msg);
                _error = true;
            };
            _player.loopPointReached += _ => _finished = true;

            _player.Prepare();
            var wait = 0f;
            while (!_player.isPrepared && !_error && wait < PrepareTimeout)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_error || !_player.isPrepared)
            {
                Debug.LogWarning("[Intro] video not playable — skipping to the opening.");
                yield return End(0f);
                yield break;
            }

            _player.Play();
            StartCoroutine(FadePlate(0f, 0.45f));

            while (!_finished && !_error)
            {
                _played += Time.unscaledDeltaTime;
                if (_played >= SkipArmTime)
                {
                    _hint.alpha = Mathf.MoveTowards(_hint.alpha, 1f, Time.unscaledDeltaTime * 2.5f);
                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape))
                        break;
                }
                yield return null;
            }
            yield return End(0.4f);
        }

        private IEnumerator End(float fadeSeconds)
        {
            if (_ending) yield break;
            _ending = true;
            if (fadeSeconds > 0f) yield return FadePlate(1f, fadeSeconds);
            if (_player != null) _player.Stop();
            // Seen means "played or skipped once": either way it never returns.
            StoryFlags.MarkIntroVideoSeen();
            var done = _onDone;
            _onDone = null;
            Active = null;
            Destroy(gameObject);
            done?.Invoke();
        }

        private IEnumerator FadePlate(float target, float seconds)
        {
            var from = _fade.color.a;
            var t = 0f;
            while (t < seconds && _fade != null)
            {
                t += Time.unscaledDeltaTime;
                var a = Mathf.Lerp(from, target, Mathf.Clamp01(t / seconds));
                _fade.color = new Color(0, 0, 0, a);
                yield return null;
            }
            if (_fade != null) _fade.color = new Color(0, 0, 0, target);
        }

        private void BuildOverlay()
        {
            var go = new GameObject("IntroCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; // above the cinematic overlay
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)go.transform;

            // Black behind the picture: the bars either side of 16:9 on a 20:9 phone.
            var back = UiKit.Group(root, "Back");
            UiKit.Img(back, null, Color.black).raycastTarget = false;

            // The picture, kept at 16:9 inside the screen.
            var screen = UiKit.Group(root, "Screen");
            _screen = screen.gameObject.AddComponent<RawImage>();
            _screen.color = Color.white;
            _screen.raycastTarget = false;
            var fit = screen.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;

            // "TAP TO SKIP", quiet, bottom right, only once the skip is armed.
            var hint = UiKit.Label(root, "TAP TO SKIP", 15, UiKit.Dim,
                new Vector2(1f, 0f), new Vector2(-40, 40), new Vector2(300, 24),
                align: TextAnchor.MiddleRight);
            hint.characterSpacing = 3f;
            _hint = hint.gameObject.AddComponent<CanvasGroup>();
            _hint.alpha = 0f;
            _hint.blocksRaycasts = false;

            // Fade plate on top of everything, starting black.
            var fadeRt = UiKit.Group(root, "Fade");
            _fade = UiKit.Img(fadeRt, null, new Color(0, 0, 0, 1));
            _fade.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            if (_rt != null) _rt.Release();
        }
    }
}
