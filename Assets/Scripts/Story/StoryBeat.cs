using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// An ordered run of shots — one cinematic. Authored as an asset so writing
    /// and re-cutting a scene never touches gameplay code.
    ///
    /// Lives in its own file on purpose: Unity only creates a MonoScript for a
    /// ScriptableObject whose class name matches its file name, and a mismatch
    /// silently produces assets that cannot be loaded at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Story Beat")]
    public class StoryBeat : ScriptableObject
    {
        [Tooltip("Stable id — the save key for 'already seen'.")]
        public string id = "beat";

        [Tooltip("Shown only in the editor and the skip prompt.")]
        public string title = "";

        public StoryShot[] shots = System.Array.Empty<StoryShot>();

        /// <summary>Total runtime including the black between shots.</summary>
        public float Duration
        {
            get
            {
                var t = 0f;
                foreach (var s in shots) t += s.duration + s.blackAfter;
                return t;
            }
        }
    }
}
