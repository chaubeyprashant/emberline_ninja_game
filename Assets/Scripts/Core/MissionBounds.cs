using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Configurable mission boundary system that replaces the old hard rectangular
    /// clamp (arenaHalfExtents). Defines an elliptical play area with soft push-back
    /// at the edges rather than an invisible wall.
    ///
    /// <para>
    /// <b>Default</b>: 34 m radius circle centered at the origin.
    /// <b>Per-mission</b>: MissionPlan can set radius, ellipse, and center.
    /// <b>Endless mode</b>: defers to RoadNorth's corridor — callers check
    /// <c>RoadNorth.Instance</c> first, exactly as they did before.
    /// </para>
    ///
    /// The boundary has three zones:
    /// <list type="number">
    ///   <item>Inner (0 → softEdge): free movement, no force.</item>
    ///   <item>Soft band (softEdge → hardEdge): gentle push-back that increases
    ///         with penetration depth, so it feels like wind resistance.</item>
    ///   <item>Safety net (beyond hardEdge + margin): hard teleport back inside,
    ///         only for physics glitches.</item>
    /// </list>
    /// </summary>
    public static class MissionBounds
    {
        // ---------------------------------------------------- configuration

        private static Vector3 _center = Vector3.zero;
        private static float _radiusX = 60f;
        private static float _radiusZ = 60f;

        /// <summary>Fraction of radius where soft push-back begins (0.85 = 85%).</summary>
        private const float SoftStart = 0.85f;

        /// <summary>Maximum push-back force (m/s²) at the hard edge.</summary>
        private const float MaxForce = 18f;

        /// <summary>Beyond this multiplier of radius, safety-net teleport fires.</summary>
        private const float SafetyMul = 1.35f;

        /// <summary>Has a mission explicitly configured bounds this session?</summary>
        public static bool Configured { get; private set; }

        public static float RadiusX => _radiusX;
        public static float RadiusZ => _radiusZ;
        public static Vector3 Center => _center;

        // ---------------------------------------------------- setup

        /// <summary>
        /// Configure bounds for a mission. Called by MissionDirector.Begin or
        /// GameManager.ConfigureFromSession. Pass 0 for either radius to use the
        /// default (60 m).
        /// </summary>
        public static void Configure(Vector3 center, float radiusX, float radiusZ)
        {
            _center = center;
            // 34 m, not the old 60. On the flat deck a 60 m circle cost nothing:
            // it was empty in every direction. In the valley a 60 m circle reaches
            // deep into the treeline, so enemies spawned among the trunks and a
            // fight became a scramble through a thicket. The clearing around the
            // village is the arena; the forest is the wall around it.
            _radiusX = radiusX > 0f ? radiusX : 34f;
            _radiusZ = radiusZ > 0f ? radiusZ : _radiusX;
            Configured = true;
        }

        /// <summary>Reset to defaults (scene load).</summary>
        public static void Reset()
        {
            _center = Vector3.zero;
            _radiusX = 60f;
            _radiusZ = 60f;
            Configured = false;
        }

        // ---------------------------------------------------- queries

        /// <summary>
        /// Normalised distance from the center in ellipse space.
        /// 0 = center, 1 = on the boundary, >1 = outside.
        /// </summary>
        public static float NormalisedDist(Vector3 pos)
        {
            var dx = (pos.x - _center.x) / _radiusX;
            var dz = (pos.z - _center.z) / _radiusZ;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>True if the position is inside the mission area.</summary>
        public static bool Contains(Vector3 pos) => NormalisedDist(pos) <= 1f;

        /// <summary>
        /// True if the position is so far outside that a safety teleport should fire.
        /// This is only for physics glitches, not normal gameplay.
        /// </summary>
        public static bool IsFarOutOfBounds(Vector3 pos) => NormalisedDist(pos) > SafetyMul;

        // ---------------------------------------------------- containment

        /// <summary>
        /// Soft push-back force for the player. Returns Vector3.zero when inside the
        /// soft zone. Between softEdge and hardEdge, returns an inward force that
        /// ramps up smoothly. The caller adds this to the player's velocity.
        /// </summary>
        public static Vector3 ContainForce(Vector3 pos)
        {
            var nd = NormalisedDist(pos);
            if (nd <= SoftStart) return Vector3.zero;

            // Direction toward center in XZ.
            var toward = _center - pos;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.001f) toward = Vector3.forward;
            toward.Normalize();

            // Ramp: 0 at softStart → 1 at nd == 1 → continues growing past 1.
            var pen = (nd - SoftStart) / (1f - SoftStart);
            // Quadratic ramp for a natural feel.
            var strength = Mathf.Min(pen * pen, 4f) * MaxForce;
            return toward * strength;
        }

        /// <summary>
        /// Hard clamp for enemies (transform-driven, no soft treatment needed).
        /// Clamps the XZ position to the ellipse boundary. Preserves Y.
        /// </summary>
        public static Vector3 Clamp(Vector3 pos)
        {
            var nd = NormalisedDist(pos);
            if (nd <= 1f) return pos;

            // Project back onto the ellipse boundary.
            var dx = pos.x - _center.x;
            var dz = pos.z - _center.z;
            var scale = 1f / nd;
            pos.x = _center.x + dx * scale;
            pos.z = _center.z + dz * scale;
            return pos;
        }

        /// <summary>
        /// Clamp for safety net: if far out of bounds, bring back to the boundary
        /// with a small inset. Returns the fixed position and true if a teleport occurred.
        /// </summary>
        public static (Vector3 pos, bool teleported) SafetyClamp(Vector3 pos)
        {
            if (!IsFarOutOfBounds(pos)) return (pos, false);
            var clamped = Clamp(pos);
            // Pull a bit further inside so we don't immediately re-trigger.
            var toward = _center - clamped;
            toward.y = 0f;
            if (toward.sqrMagnitude > 0.001f) clamped += toward.normalized * 2f;
            // Not Max(y, 0.5): on a height field the floor is wherever the ground
            // is, and 0.5 can be several metres underneath it.
            clamped.y = Mathf.Max(clamped.y, Ground.HeightAt(clamped.x, clamped.z) + 0.5f);
            return (clamped, true);
        }

        // ---------------------------------------------------- spawn helpers

        /// <summary>
        /// A random point on the boundary perimeter (or just inside it), for spawning
        /// enemies at the edge of the play area. Replaces the old 4-edge switch.
        /// </summary>
        public static Vector3 RandomPerimeterPoint(float inset = 1f)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var r = 1f - inset / Mathf.Max(_radiusX, _radiusZ);
            return new Vector3(
                _center.x + Mathf.Cos(angle) * _radiusX * r,
                0f,
                _center.z + Mathf.Sin(angle) * _radiusZ * r);
        }

        /// <summary>
        /// A random point inside the play area, for objectives and dressing.
        /// Stays away from the center by at least <paramref name="minDist01"/>
        /// of the radius and from the edge by <paramref name="edgeInset01"/>.
        /// </summary>
        public static Vector3 RandomInteriorPoint(float minDist01 = 0f, float edgeInset01 = 0.1f)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var maxR = 1f - edgeInset01;
            var r = Mathf.Sqrt(Random.Range(minDist01 * minDist01, maxR * maxR));
            return new Vector3(
                _center.x + Mathf.Cos(angle) * _radiusX * r,
                0f,
                _center.z + Mathf.Sin(angle) * _radiusZ * r);
        }

        /// <summary>
        /// Legacy compatibility: returns half-extents that approximate the ellipse as
        /// a rectangle. Used by systems that haven't been migrated yet.
        /// </summary>
        public static Vector2 LegacyHalfExtents => new(_radiusX, _radiusZ);
    }
}
