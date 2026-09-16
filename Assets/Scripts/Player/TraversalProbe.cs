using UnityEngine;

namespace Emberline.Player
{
    /// <summary>What the geometry in front of the player allows.</summary>
    public enum TraversalKind
    {
        None,
        Vault,   // a thin, waist-to-chest-high obstacle: go over it and land beyond
        Mantle,  // a ledge up to head height: pull up and stand on it
        Grab,    // a ledge above head height: jump, catch it, hang
    }

    /// <summary>One classified obstacle ahead. `kind == None` means keep walking.</summary>
    public struct TraversalHit
    {
        public TraversalKind kind;
        public Vector3 wallPoint;   // where the face was touched
        public Vector3 wallNormal;  // face normal, horizontal
        public Vector3 ledge;       // a point on the top surface just past the edge
        public Vector3 landing;     // where the feet end up (on the ledge, or beyond a vault)
        public float height;        // ledge height above the feet

        public bool HasValue => kind != TraversalKind.None;
    }

    /// <summary>
    /// Reads real colliders instead of the arena's obstacle circles: a capsule sweep
    /// finds a face, a ray from above finds its top, a capsule check proves there is
    /// room to stand there. Pure and allocation-free, so an edit-mode check can build
    /// a course of boxes and assert every classification without play mode
    /// (EmberTraversalCheck).
    /// </summary>
    public static class TraversalProbe
    {
        /// <summary>The CharacterController steps this on its own.</summary>
        public const float StepMax = 0.45f;
        /// <summary>Up to here a thin obstacle is vaulted; a deep one is mantled.</summary>
        public const float VaultMax = 1.35f;
        /// <summary>Up to here the ledge is pulled onto from the ground.</summary>
        public const float MantleMax = 2.45f;
        /// <summary>Up to here the ledge can be jumped for and caught.</summary>
        public const float GrabMax = 3.6f;
        /// <summary>A vault needs the far side to drop at least this much below the top.</summary>
        public const float VaultDrop = 0.5f;
        /// <summary>How far past the face the top is sampled: a hand's width, so a
        /// 0.3 m garden wall still has a top to find.</summary>
        public const float LidInset = 0.12f;

        private static int _mask = -1;
        private static readonly RaycastHit[] Lids = new RaycastHit[8];

        /// <summary>Everything solid: no water plane, no UI, nothing marked Ignore Raycast.</summary>
        public static int Mask
        {
            get
            {
                if (_mask == -1)
                    _mask = ~((1 << LayerMask.NameToLayer("Water"))
                              | (1 << LayerMask.NameToLayer("Ignore Raycast"))
                              | (1 << LayerMask.NameToLayer("UI")));
                return _mask;
            }
        }

        /// <summary>
        /// Classify the obstacle in `dir` from a body standing (or flying) at `feet`.
        /// Grounded: height buckets pick vault / mantle / grab. Airborne: anything
        /// whose top is within arm's reach above the feet is a grab.
        /// </summary>
        public static TraversalHit Scan(Vector3 feet, Vector3 dir, float radius, float bodyHeight,
            float reach, bool airborne)
        {
            var result = default(TraversalHit);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return result;
            dir.Normalize();

            // The sweep starts a little above the feet so a step never reads as a
            // wall, and stays inside the body's own height so an overhang is not
            // mistaken for a face.
            var r = radius * 0.8f;
            var p1 = feet + Vector3.up * (StepMax + r);
            var p2 = feet + Vector3.up * Mathf.Max(StepMax + r + 0.05f, bodyHeight - r);
            if (!Physics.CapsuleCast(p1, p2, r, dir, out var face, reach, Mask,
                    QueryTriggerInteraction.Ignore))
                return result;
            if (face.normal.y > 0.45f) return result;       // a ramp, walk it
            var normal = face.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.01f) return result;  // ceiling or degenerate
            normal.Normalize();
            if (Vector3.Dot(normal, dir) > -0.3f) return result; // glancing, not ahead

            // The top: drop a ray from just above the tallest grab, a hand's width
            // past the face, and take the LOWEST surface in range with room to
            // stand — a ledge under a balcony is the ledge, unless the balcony
            // leaves no headroom, in which case the balcony's own lip is the grab.
            // A ray that starts inside a collider does not report it, so a wall
            // taller than GrabMax finds the ground instead and fails on height.
            var over = face.point - normal * LidInset;
            var top = feet.y + GrabMax + 0.25f;
            var n = Physics.RaycastNonAlloc(new Vector3(over.x, top, over.z), Vector3.down, Lids,
                GrabMax + 0.25f + StepMax, Mask, QueryTriggerInteraction.Ignore);
            var found = false;
            RaycastHit lid = default;
            for (var pass = 0; pass < n && !found; pass++)
            {
                // Lowest candidate first (largest distance from the ray origin).
                var best = -1;
                for (var i = 0; i < n; i++)
                {
                    if (Lids[i].distance < 0f) continue;
                    if (best < 0 || Lids[i].distance > Lids[best].distance) best = i;
                }
                if (best < 0) break;
                var c = Lids[best];
                Lids[best].distance = -1f; // consumed
                var ch = c.point.y - feet.y;
                if (c.normal.y < 0.7f || ch < StepMax || ch > GrabMax) continue;
                if (!Standable(c.point, radius, bodyHeight)) continue;
                lid = c;
                found = true;
            }
            if (!found) return result;
            var h = lid.point.y - feet.y;

            result.wallPoint = face.point;
            result.wallNormal = normal;
            result.ledge = lid.point;
            result.height = h;

            if (airborne)
            {
                // Hands reach about a body height above the feet; below waist height
                // the fall would simply land on it.
                if (h < 0.5f || h > bodyHeight + 0.55f) return result;
                result.kind = TraversalKind.Grab;
                result.landing = lid.point - normal * (radius + 0.25f);
                return result;
            }

            if (h <= VaultMax)
            {
                // Thin enough to go over? Sample the far side a stride past the top.
                var beyond = face.point - normal * (LidInset + 1.0f);
                if (Physics.Raycast(new Vector3(beyond.x, lid.point.y + 0.1f, beyond.z), Vector3.down,
                        out var far, h + 0.1f + 2.5f, Mask, QueryTriggerInteraction.Ignore)
                    && lid.point.y - far.point.y > VaultDrop
                    && Standable(far.point, radius, bodyHeight))
                {
                    result.kind = TraversalKind.Vault;
                    result.landing = far.point;
                    return result;
                }
            }

            result.landing = lid.point - normal * (radius + 0.25f);
            result.kind = h <= MantleMax ? TraversalKind.Mantle : TraversalKind.Grab;
            return result;
        }

        /// <summary>Room for the body to stand at `floor`, allowing a crouch if it must.</summary>
        public static bool Standable(Vector3 floor, float radius, float bodyHeight)
        {
            var r = radius * 0.9f;
            var a = floor + Vector3.up * (r + 0.08f);
            if (!Physics.CheckCapsule(a, floor + Vector3.up * (bodyHeight - r), r, Mask,
                    QueryTriggerInteraction.Ignore))
                return true;
            return !Physics.CheckCapsule(a, floor + Vector3.up * (1.25f - r), r, Mask,
                QueryTriggerInteraction.Ignore);
        }
    }
}
