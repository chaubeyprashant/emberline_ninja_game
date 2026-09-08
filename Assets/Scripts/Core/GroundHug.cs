using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Keeps a transform-driven mover sitting on the ground.
    ///
    /// <para>
    /// Villagers, prisoners and lantern bearers all walk by writing XZ straight to
    /// their transform, which was correct when the floor was a plane at y = 0.
    /// One component in LateUpdate is cheaper and far less error-prone than
    /// finding every position write in three movers.
    /// </para>
    ///
    /// <para>
    /// This lives in its own file because Unity resolves a MonoBehaviour by
    /// matching the class name to the file name. Sharing a file with
    /// <see cref="ZoneTerrain"/> made the class unattachable in a saved scene: the
    /// component serialised with no script reference and the Android build failed
    /// with "Script attached to 'Zone' is missing or no valid script is attached".
    /// </para>
    /// </summary>
    public class GroundHug : MonoBehaviour
    {
        [Tooltip("Metres above the surface to sit. Negative sinks the model in.")]
        public float offset;

        private void LateUpdate()
        {
            if (!Ground.ZoneActive) return;
            var p = transform.position;
            p.y = Ground.HeightAt(p.x, p.z) + offset;
            transform.position = p;
        }
    }
}
