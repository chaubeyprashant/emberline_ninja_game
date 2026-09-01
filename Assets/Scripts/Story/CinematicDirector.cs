using System.Collections;
using Emberline.Core;
using Emberline.UI;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Emberline.Story
{
    /// <summary>
    /// Plays a <see cref="StoryBeat"/>: drives the camera, the subtitles, the
    /// letterbox, the audio bed and the fades, then hands control back.
    ///
    /// Built on the same shape as BossIntroDirector — freeze gameplay, sweep the
    /// camera, letterbox, type the line, sting — generalised from one hardcoded
    /// 3.4-second sequence into a data-driven one. Gameplay is suppressed through
    /// the existing <see cref="GameManager.CinematicActive"/> flag, which the
    /// locomotion, combat, enemy AI and mission director already respect, so no
    /// new "disable everything" plumbing was needed.
    ///
    /// Runs on scaled time so the existing pause (timeScale 0) freezes a cinematic
    /// for free; the overlay itself fades on unscaled time so it stays responsive.
    /// </summary>
    public class CinematicDirector : MonoBehaviour
    {
        public static CinematicDirector Active { get; private set; }

        private StoryBeat _beat;
        private GameManager _gm;
        private CameraRig _rig;
        private System.Action _onDone;

        private Canvas _canvas;
        private RectTransform _barTop, _barBottom;
        private Image _fadeImg;
        private TMP_Text _cardText;
        private SubtitleView _subs;
        private Button _skipBtn;
        private bool _skipped;

        /// <summary>Start a beat. Returns null and runs onDone immediately if the
        /// beat is missing, so callers never have to null-check a cinematic.</summary>
        public static CinematicDirector Play(StoryBeat beat, GameManager gm,
            CameraRig rig, System.Action onDone = null)
        {
            if (beat == null || beat.shots == null || beat.shots.Length == 0)
            {
                onDone?.Invoke();
                return null;
            }
            var host = new GameObject("Cinematic_" + beat.id);
            var d = host.AddComponent<CinematicDirector>();
            d._beat = beat;
            d._gm = gm;
            d._rig = rig;
            d._onDone = onDone;
            Active = d;
            d.StartCoroutine(d.Run());
            return d;
        }

        /// <summary>Cut to the end. Only offered once the beat has been seen.</summary>
        public void Skip() => _skipped = true;

        private IEnumerator Run()
        {
            _gm?.SetCinematic(true);
            BuildOverlay();

            Transform subject = null;
            foreach (var shot in _beat.shots)
            {
                if (_skipped) break;
                subject = Cast.Find(shot.subject) ?? subject;
                ApplyWorld(shot);
                FrameShot(shot, subject);
                _subs.Show(shot.speaker, shot.line);
                if (shot.voice != null) AudioSource.PlayClipAtPoint(shot.voice, Vector3.zero);
                ShowCard(shot.card);

                var t = 0f;
                while (t < shot.duration && !_skipped)
                {
                    t += Time.deltaTime;
                    var target = shot.letterbox;
                    SetLetterbox(Mathf.MoveTowards(CurrentLetterbox, target, Time.deltaTime * 2f));
                    yield return null;
                }

                if (shot.fadeOutAfter || shot.blackAfter > 0f)
                {
                    yield return Fade(1f, 0.5f);
                    _subs.Clear();
                    ShowCard("");
                    var hold = 0f;
                    while (hold < shot.blackAfter && !_skipped)
                    {
                        hold += Time.deltaTime;
                        yield return null;
                    }
                    yield return Fade(0f, 0.5f);
                }
            }

            yield return Fade(1f, _skipped ? 0.2f : 0.7f);
            StoryFlags.MarkSeen(_beat.id);
            _gm?.SetCinematic(false);
            _rig?.StopCinematic();
            Sfx3D.SetMusicState(Sfx3D.MusicState.Exploration, 1f);
            Active = null;
            _onDone?.Invoke();
            yield return Fade(0f, 0.6f);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------- shot work

        private void FrameShot(StoryShot shot, Transform subject)
        {
            if (_rig == null || subject == null) return;
            // Reuse the rig's cinematic sweep for the moving shots; a Hold is the
            // same call with no arc, which keeps one code path for both.
            CinematicCamera.Apply(shot.camera, subject, shot.duration, _rig);
        }

        private static void ApplyWorld(StoryShot shot)
        {
            // Set dressing before the theme: a shot may ask for both, and the set
            // applies its own theme, which an explicit one should be able to override.
            if (shot.setState != SetState.Unchanged && VillageSet.Active != null)
                VillageSet.Active.Apply(shot.setState);

            if (shot.applyTheme)
            {
                var theme = EnvThemes.Get(shot.theme);
                Atmosphere.Apply(theme, SceneRefs.Cam != null ? SceneRefs.Cam.transform : null);
            }

            switch (shot.audio)
            {
                case ShotAudio.Silence:
                case ShotAudio.MusicOff:
                    Sfx3D.SetMusicState(Sfx3D.MusicState.None, 0.8f);
                    Sfx3D.PlayAmbience(null);
                    break;
                case ShotAudio.Wind: Sfx3D.PlayAmbience("mountain_wind"); break;
                case ShotAudio.Birds: Sfx3D.PlayAmbience("forest_ambience"); break;
                case ShotAudio.Village: Sfx3D.PlayAmbience("village_ambience"); break;
                case ShotAudio.Fire: Sfx3D.PlayAmbience("fire_ambience"); break;
                case ShotAudio.Rain: Sfx3D.PlayAmbience("rain_ambience"); break;
                case ShotAudio.Snow: Sfx3D.PlayAmbience("mountain_wind"); break;
                case ShotAudio.Bells: Sfx3D.Sting(); break;
                case ShotAudio.Sting: Sfx3D.Sting(); break;
                case ShotAudio.MusicSoft:
                    Sfx3D.SetMusicState(Sfx3D.MusicState.Exploration, 2f); break;
                case ShotAudio.MusicDark:
                    Sfx3D.SetMusicState(Sfx3D.MusicState.Combat, 2f); break;
                case ShotAudio.MusicImpact:
                    Sfx3D.SetMusicState(Sfx3D.MusicState.Boss, 0.5f); break;
            }
        }

        // ---------------------------------------------------------------- overlay

        private float CurrentLetterbox =>
            _barTop != null && Screen.height > 0
                ? _barTop.sizeDelta.y / (Screen.height * 0.12f) : 0f;

        private void SetLetterbox(float k)
        {
            var h = Screen.height * 0.12f * Mathf.Clamp01(k);
            _barTop.sizeDelta = new Vector2(0, h);
            _barBottom.sizeDelta = new Vector2(0, h);
        }

        private void ShowCard(string text)
        {
            _cardText.text = Loc.T(text ?? "");
        }

        private IEnumerator Fade(float to, float seconds)
        {
            var from = _fadeImg.color.a;
            var t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                _fadeImg.color = new Color(0, 0, 0, a);
                yield return null;
            }
            _fadeImg.color = new Color(0, 0, 0, to);
        }

        private void BuildOverlay()
        {
            var go = new GameObject("CinematicCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900; // above the HUD, below nothing
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)go.transform;

            _barTop = UiKit.Rect(root, "BarTop", new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 1f));
            _barTop.anchorMin = new Vector2(0, 1);
            _barTop.anchorMax = new Vector2(1, 1);
            UiKit.Img(_barTop, null, Color.black);

            _barBottom = UiKit.Rect(root, "BarBottom", new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0, 0), new Vector2(0.5f, 0f));
            _barBottom.anchorMin = new Vector2(0, 0);
            _barBottom.anchorMax = new Vector2(1, 0);
            UiKit.Img(_barBottom, null, Color.black);

            _subs = SubtitleView.Build(root);

            // Full-screen card, e.g. THREE YEARS LATER.
            _cardText = UiKit.Label(root, "", 34, new Color(0.92f, 0.92f, 0.9f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 60), display: true);

            // Fade plate on top of everything.
            var fadeRt = UiKit.Group(root, "Fade");
            _fadeImg = UiKit.Img(fadeRt, null, new Color(0, 0, 0, 1));
            _fadeImg.raycastTarget = false;
            StartCoroutine(Fade(0f, 1.2f));

            // Skip is offered only once the player has already sat through it.
            if (StoryFlags.Seen(_beat.id))
                _skipBtn = UiKit.MakeButton(root, "SKIP", new Vector2(1f, 0f),
                    new Vector2(-110, 46), new Vector2(150, 48), Skip, 15);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            // A cinematic torn down mid-run (scene load) must not strand the game.
            _gm?.SetCinematic(false);
        }
    }
}
