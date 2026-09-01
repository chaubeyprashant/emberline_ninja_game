using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Marks the single subtree that holds everything visual about a character:
    /// renderers, the Animator, the skeleton and the weapon sockets. Gameplay
    /// components — brain, health, locomotion, combat — live on the parent and
    /// never reach past this boundary by name.
    ///
    /// This exists so replacing a character model is one subtree swap rather than
    /// an archaeology exercise. Nothing about combat depends on the mesh: there
    /// are no hit colliders on characters, targeting works from the root
    /// transform by distance and angle, and animation is addressed through the
    /// RigPose table rather than clip names in code. See
    /// docs/ASSET_SPECIFICATIONS.md §0 for the full contract.
    /// </summary>
    public class VisualRoot : MonoBehaviour
    {
        [Tooltip("Which character spec produced this subtree. Diagnostic only.")]
        public string modelId = "";

        [Tooltip("Right-hand weapon socket, resolved when the model was built.")]
        public Transform socketRight;

        [Tooltip("Left-hand socket: off-hand blades, bombs, the crossbow.")]
        public Transform socketLeft;

        [Tooltip("Height the model was normalised to, in metres.")]
        public float normalisedHeight;

        /// <summary>
        /// The visual subtree of a character, or null. Prefer this over
        /// transform.Find("...") — the boundary is a component, not a name.
        /// </summary>
        public static VisualRoot Of(GameObject character) =>
            character == null ? null : character.GetComponentInChildren<VisualRoot>(true);
    }
}
