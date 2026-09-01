using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// Tags a scene object so shots can name it. Keeps story assets free of scene
    /// references, which is what lets a beat be authored without the scene open.
    /// </summary>
    public class CastMember : MonoBehaviour
    {
        [Tooltip("Name a StoryShot's `subject` refers to, e.g. REN, AIKO, FATHER.")]
        public string castName = "";

        private void OnEnable() => Cast.Register(this);
        private void OnDisable() => Cast.Unregister(this);
    }
}
