using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Combat
{
    /// <summary>
    /// Continuous weapon tracing: the blade's path between two frames, tested
    /// against character capsules.
    ///
    /// <para>
    /// The old test asked "is anyone inside a cone in front of me, right now".
    /// This asks "did my edge pass through anyone since the last frame", which is
    /// a different question and the reason a weapon starts to feel like it
    /// occupies space. A fast weapon at a low frame rate cannot step over a body,
    /// because the blade is sub-stepped between its previous and current pose.
    /// </para>
    ///
    /// <para>
    /// Deliberately free of physics bodies. Characters in this project are
    /// transform-driven and carry no colliders — the floor is a height function
    /// and targeting has always worked from the root transform — so there is
    /// nothing for a <c>Physics.CapsuleCast</c> to hit. Everything here is closed
    /// form: segment-to-segment distance, no queries, no allocation, no garbage.
    /// </para>
    /// </summary>
    public class WeaponTrace
    {
        /// <summary>Longest distance a blade sample may jump before sub-stepping.</summary>
        private const float MaxStep = 0.22f;

        /// <summary>Ceiling on sub-steps: past this the extra fidelity is invisible.</summary>
        private const int MaxSubSteps = 6;

        private Vector3 _prevRoot, _prevTip;
        private Vector3 _nowRoot, _nowTip;
        private float _radius = 0.07f;
        private bool _open;

        /// <summary>Targets already hit by this swing — one hit per target.</summary>
        private readonly List<Object> _hit = new(8);

        /// <summary>Total distance the tip has travelled while the window was open.</summary>
        public float TipTravel { get; private set; }

        /// <summary>Contact point of the last successful sweep.</summary>
        public Vector3 LastContact { get; private set; }

        /// <summary>Blade direction of travel at the last contact — the swing's line.</summary>
        public Vector3 LastSwingDir { get; private set; }

        /// <summary>
        /// True when the blade has not moved since the window opened. An enemy
        /// whose renderer is off screen stops updating its bone transforms
        /// (<c>AnimatorCullingMode.CullUpdateTransforms</c>), so its blade is
        /// frozen and tracing it would silently never hit. Callers fall back to
        /// the arc test when this is true.
        /// </summary>
        public bool Frozen => _open && TipTravel < 0.02f;

        public void Begin(BladePoints blade)
        {
            blade.Sample();
            _prevRoot = _nowRoot = blade.Root;
            _prevTip = _nowTip = blade.Tip;
            _radius = blade.Radius;
            _hit.Clear();
            TipTravel = 0f;
            _open = true;
        }

        /// <summary>Advance to where the animation has put the blade this frame.</summary>
        public void Step(BladePoints blade)
        {
            if (!_open) return;
            _prevRoot = _nowRoot;
            _prevTip = _nowTip;
            blade.Sample();
            _nowRoot = blade.Root;
            _nowTip = blade.Tip;
            TipTravel += Vector3.Distance(_prevTip, _nowTip);
        }

        public void End() => _open = false;

        /// <summary>Has this swing already scored on that target?</summary>
        public bool AlreadyHit(Object target) => _hit.Contains(target);

        /// <summary>Record a target so one swing cannot hit it twice.</summary>
        public void MarkHit(Object target) => _hit.Add(target);

        /// <summary>Number of targets this swing has connected with.</summary>
        public int HitCount => _hit.Count;

        /// <summary>
        /// Did the blade sweep through the upright capsule at <paramref name="feet"/>?
        /// The blade is interpolated from its previous pose to its current one in
        /// as many sub-steps as the tip's travel demands, so a swing that crosses
        /// a body between two frames still connects.
        /// </summary>
        public bool Sweep(Vector3 feet, float height, float radius)
        {
            if (!_open) return false;

            var axisA = feet + Vector3.up * (height * 0.12f);
            var axisB = feet + Vector3.up * (height * 0.88f);
            var reach = radius + _radius;
            var reachSq = reach * reach;

            var steps = Mathf.Clamp(
                Mathf.CeilToInt(Vector3.Distance(_prevTip, _nowTip) / MaxStep), 1, MaxSubSteps);

            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var root = Vector3.Lerp(_prevRoot, _nowRoot, t);
                var tip = Vector3.Lerp(_prevTip, _nowTip, t);
                if (SegmentDistanceSq(root, tip, axisA, axisB, out var onBlade) > reachSq) continue;

                LastContact = onBlade;
                var travel = _nowTip - _prevTip;
                LastSwingDir = travel.sqrMagnitude > 1e-6f ? travel.normalized : (tip - root).normalized;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Squared distance between two segments, plus the closest point on the
        /// first. Standard clamped-parameter solve; branches on the degenerate
        /// cases so a zero-length blade or a zero-height capsule cannot divide by
        /// zero. No allocation — this runs per candidate per active frame.
        /// </summary>
        private static float SegmentDistanceSq(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,
            out Vector3 closestOnFirst)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);

            float s, t;
            const float eps = 1e-6f;

            if (a <= eps && e <= eps)
            {
                closestOnFirst = p1;
                return (p1 - p2).sqrMagnitude;
            }
            if (a <= eps) { s = 0f; t = Mathf.Clamp01(f / e); }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= eps) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else
                {
                    var b = Vector3.Dot(d1, d2);
                    var denom = a * e - b * b;
                    s = denom > eps ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                }
            }

            var c1 = p1 + d1 * s;
            var c2 = p2 + d2 * t;
            closestOnFirst = c1;
            return (c1 - c2).sqrMagnitude;
        }
    }
}
