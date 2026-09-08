using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Marks a scene as carrying the valley. Placed by the scene builder; its only
    /// job is to turn <see cref="Ground"/> on for the lifetime of that scene, so a
    /// scene without terrain — the endless Road North corridor, the opening —
    /// keeps the old flat floor at y = 0.
    ///
    /// <para>
    /// In its own file for the same reason as <see cref="GroundHug"/>: Unity maps
    /// a MonoBehaviour to its script asset by file name, so a component whose
    /// class shares a file with something else cannot be serialised into a scene.
    /// </para>
    /// </summary>
    public class ZoneWorld : MonoBehaviour
    {
        private void Awake() => Ground.ZoneActive = true;
        private void OnDestroy() => Ground.ZoneActive = false;
    }
}
