using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Runtime setup for the open mission zone.
    ///
    /// <para>
    /// The arenas were contained by four cube parapets with colliders — a literal
    /// rectangular cage 129 m across. This scene has none. Containment is the
    /// mountain ring the terrain raises around the valley, and
    /// <see cref="MissionBounds"/> is configured just inside it so that its soft
    /// push-back only ever engages where the player is already walking into rock.
    /// The edge of the world is a place, not a wall.
    /// </para>
    /// </summary>
    public class ZoneBoot : MonoBehaviour
    {
        [Tooltip("Where Renzo starts: the north gate, on the road.")]
        public Vector3 spawn = new(3f, 0f, 68f);

        [Tooltip("Play radius. Sits inside the mountain ring's inner edge.")]
        public float radius = 76f;

        private void Awake()
        {
            // Round, soft, and generous: the valley floor is roughly 156 m across,
            // against the old arena's effective 120 m.
            MissionBounds.Configure(Vector3.zero, radius, radius);
        }

        private void Start()
        {
            var motor = SceneRefs.Motor;
            if (motor == null) return;

            // Drop Renzo onto the ground rather than trusting an authored Y: the
            // terrain is generated, so the only reliable surface height is the one
            // physics reports.
            var from = new Vector3(spawn.x, 200f, spawn.z);
            var y = Physics.Raycast(from, Vector3.down, out var hit, 400f)
                ? hit.point.y + 0.1f
                : spawn.y;

            var cc = motor.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            motor.transform.position = new Vector3(spawn.x, y, spawn.z);
            if (cc != null) cc.enabled = true;
        }
    }
}
