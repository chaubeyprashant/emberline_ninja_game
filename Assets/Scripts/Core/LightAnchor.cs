using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// A place that gives off light — a stone lantern, a lit shoji, a campfire.
    /// It owns no Light: <see cref="LanternLights"/> lends one of a few pooled
    /// point lights to whichever anchors are nearest the camera.
    /// </summary>
    public class LightAnchor : MonoBehaviour
    {
        public static readonly List<LightAnchor> Active = new();

        public Vector3 offset = new(0f, 1.3f, 0f);
        public Color color = new(1f, 0.62f, 0.32f);
        public float intensity = 1.6f;
        public float range = 7f;
        public bool flicker;

        /// <summary>World position of the light: the offset in metres, turned with the prop but not scaled.</summary>
        public Vector3 Position => transform.position + transform.rotation * offset;

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);
    }
}
