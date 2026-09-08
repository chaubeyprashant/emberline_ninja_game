using UnityEngine;

namespace Emberline.Combat
{
    /// <summary>
    /// The line a blow travels along. Read from the blade's actual motion during
    /// the active window, so it costs nothing to author and cannot disagree with
    /// what the animation shows.
    ///
    /// <para>
    /// Defence uses it to decide whether a guard was facing the right way, and
    /// reactions use it to throw a body along the blow rather than always
    /// backwards. It is deliberately coarse: six readable cases beat a continuous
    /// angle nobody can see.
    /// </para>
    /// </summary>
    public enum AttackDirection
    {
        Forward,   // unclassified / straight ahead
        Left,      // horizontal, travelling to the attacker's left
        Right,     // horizontal, travelling to the attacker's right
        High,      // descending: overhead
        Low,       // rising or sweeping at the legs
        Thrust,    // along the attacker's facing, little lateral travel
        Back,      // struck from behind the defender
    }

    public static class AttackDirections
    {
        /// <summary>
        /// Classify a swing from the direction its edge was travelling, expressed
        /// in the attacker's own frame.
        /// </summary>
        public static AttackDirection FromSwing(Vector3 worldSwingDir, Transform attacker)
        {
            if (attacker == null || worldSwingDir.sqrMagnitude < 1e-6f) return AttackDirection.Forward;
            var v = attacker.InverseTransformDirection(worldSwingDir.normalized);
            var ax = Mathf.Abs(v.x);
            var ay = Mathf.Abs(v.y);
            var az = Mathf.Abs(v.z);

            // Forward travel dominating both other axes is a thrust: the edge is
            // going where the attacker is looking rather than across it.
            if (az > ax && az > ay && v.z > 0f) return AttackDirection.Thrust;
            if (ay > ax) return v.y < 0f ? AttackDirection.High : AttackDirection.Low;
            return v.x < 0f ? AttackDirection.Left : AttackDirection.Right;
        }

        /// <summary>
        /// Was the blow struck from outside the defender's guard? A guard covers
        /// the front <paramref name="coverDeg"/> degrees; anything wider than that
        /// is unguarded no matter what the defender is holding down.
        /// </summary>
        public static bool WithinGuard(Transform defender, Vector3 attackerPos, float coverDeg)
        {
            if (defender == null) return false;
            var to = attackerPos - defender.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f) return true;
            return Vector3.Angle(defender.forward, to) <= coverDeg * 0.5f;
        }
    }
}
