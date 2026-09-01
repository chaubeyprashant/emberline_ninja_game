using Emberline.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberline.Story
{
    /// <summary>
    /// Scene-side entry point for a cinematic scene: finds its beat, plays it, and
    /// loads whatever comes next. Keeps scene wiring to a single component so the
    /// opening can be re-cut entirely in assets.
    /// </summary>
    public class StoryRunner : MonoBehaviour
    {
        [Tooltip("Beat id under Resources/Story.")]
        public string beatId = "opening";

        [Tooltip("Scene to load when the beat ends. Empty stays put.")]
        public string nextScene = "Rooftop";

        [Tooltip("Set dressing before the first shot runs.")]
        public SetState openingState = SetState.Peace;

        private void Start()
        {
            // Captured before the director runs, because finishing the beat marks
            // it seen and would make every run look like a repeat.
            _wasFirstRun = !StoryFlags.Seen(beatId);

            var set = VillageSet.Active;
            set?.Apply(openingState);

            Sfx3D.Init(gameObject);

            // Already seen: go straight to the menu. Offering a skip button is not
            // enough — a returning player should not have to dismiss the opening
            // every single launch to reach their save.
            if (!_wasFirstRun) { Advance(); return; }

            var beat = Resources.Load<StoryBeat>("Story/" + beatId);
            if (beat == null)
            {
                Debug.LogWarning($"[Story] Beat '{beatId}' missing — skipping to {nextScene}.");
                Advance();
                return;
            }

            var rig = SceneRefs.Cam != null ? SceneRefs.Cam.GetComponent<CameraRig>() : null;
            CinematicDirector.Play(beat, null, rig, Advance);
            _playing = true;
        }

        private void Advance()
        {
            if (string.IsNullOrEmpty(nextScene)) return;

            // First time through, the opening hands straight to the first mission:
            // the brief asks for no menu and no fighting between the cinematic and
            // the player's first step as adult Renzo.
            //
            // Every launch after that must reach the menu instead. The opening is
            // build index 0, so without this a returning player replays the
            // cinematic and is dumped into level 1 forever, with no way to reach
            // their save, the armoury or the Road North.
            if (_wasFirstRun)
            {
                Session.Mode = LaunchMode.Story;
                Session.LevelIndex = 0;
            }
            else
            {
                Session.Mode = LaunchMode.None; // GameManager opens the main menu
            }
            SceneManager.LoadScene(nextScene);
        }

        private bool _wasFirstRun, _playing;

        private void Update()
        {
            // Back is the universal Android "get me out of here". During a beat the
            // player has already seen, it skips; on a first run it is ignored, so
            // the opening cannot be missed by accident.
            if (!_playing || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (_wasFirstRun) return;
            CinematicDirector.Active?.Skip();
        }
    }
}
