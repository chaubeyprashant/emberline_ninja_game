using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// One thing in the world that the action button can use: a shrine to pray
    /// at, a fire to rest by, a signpost to read. Registers itself; the HUD asks
    /// <see cref="Nearest"/> ten times a second and turns the JUMP button into the
    /// verb. Everything contextual goes through this one class so a new kind of
    /// interaction is a component with a verb and a callback, not a new button.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> Active = new();

        [Tooltip("What the button says: PRAY, REST, READ, OPEN, TALK.")]
        public string verb = "USE";
        [Tooltip("Metres from the player's feet.")]
        public float radius = 2.4f;
        [Tooltip("Where the prompt anchors and distance is measured from (defaults to this transform).")]
        public Transform anchor;

        public System.Action<Interactable> OnUse;

        /// <summary>The one the player is standing at, or null.</summary>
        public static Interactable Current { get; private set; }

        private static float _pollT;

        private void OnEnable() => Active.Add(this);
        private void OnDisable() { Active.Remove(this); if (Current == this) Current = null; }

        public Vector3 Point => anchor != null ? anchor.position : transform.position;

        /// <summary>Refresh <see cref="Current"/>; cheap enough to call every frame, throttled inside.</summary>
        public static Interactable Nearest(Vector3 playerPos)
        {
            _pollT -= Time.deltaTime;
            if (_pollT > 0f) return Current;
            _pollT = 0.1f;
            Interactable best = null;
            var bestD = float.MaxValue;
            for (var i = 0; i < Active.Count; i++)
            {
                var it = Active[i];
                if (it == null) continue;
                var d = it.Point - playerPos;
                d.y *= 0.5f; // a shrine on a step still counts
                var sq = d.sqrMagnitude;
                if (sq > it.radius * it.radius || sq >= bestD) continue;
                bestD = sq;
                best = it;
            }
            Current = best;
            return best;
        }

        public void Use() => OnUse?.Invoke(this);
    }
}
