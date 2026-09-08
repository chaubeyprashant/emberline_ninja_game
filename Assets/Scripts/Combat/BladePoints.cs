using Emberline.Core;
using UnityEngine;

namespace Emberline.Combat
{
    /// <summary>
    /// Where a character's weapon actually is, right now, in world space.
    ///
    /// <para>
    /// The weapon wrapper prefabs already carry the skeleton this needs:
    /// <c>BladeRoot</c> at the guard and <c>HitPoint</c> at the tip, authored per
    /// weapon by <c>EmberWeaponImport</c> and parented — through
    /// <c>GripAnchor_{side}</c> — to the animated hand bone. So the points below
    /// are the real blade, moved by the real animation, not a cone guessed from
    /// the character's facing.
    /// </para>
    ///
    /// <para>
    /// Legacy KayKit props (the greataxe, the crossbow, the plain dagger) have no
    /// markers. For those the blade is derived once from the prop's mesh bounds
    /// along its local +Y — the axis every prop in this project is authored
    /// along — and cached. A character with no weapon at all resolves to nothing
    /// and its attacks fall back to the arc test they have always used.
    /// </para>
    /// </summary>
    public class BladePoints
    {
        /// <summary>Guard end of the cutting edge.</summary>
        public Vector3 Root { get; private set; }

        /// <summary>Tip.</summary>
        public Vector3 Tip { get; private set; }

        /// <summary>True once a weapon was found and the points are meaningful.</summary>
        public bool Valid => _blade != null;

        /// <summary>Half-thickness of the blade, used as the sweep radius.</summary>
        public float Radius { get; private set; } = 0.07f;

        private Transform _blade;      // the prop root the points are expressed in
        private Vector3 _localRoot, _localTip;
        private Transform _resolvedFor; // the socket we last searched

        /// <summary>
        /// Find the live weapon under a character and cache its blade axis.
        /// Cheap to call repeatedly: it re-searches only when the socket changes
        /// or the cached prop was disabled by a weapon swap.
        /// </summary>
        public bool Resolve(GameObject character)
        {
            if (character == null) return false;
            if (_blade != null && _blade.gameObject.activeInHierarchy) return true;

            var visual = VisualRoot.Of(character);
            if (visual == null) return false;

            // Right hand first: every two-handed weapon in this project parents to
            // it, and an off-hand-only weapon (the yumi) is not a cutting edge.
            var prop = FindProp(visual.socketRight) ?? FindProp(visual.socketLeft);
            if (prop == null) { _blade = null; return false; }

            _blade = prop;
            // From the hand, not from the guard. The wrapper's BladeRoot marks
            // where the cutting edge begins, which is 1.6 m from the body on a
            // katana — trace only that and anyone standing closer than the guard
            // is swept straight past, which is how the first pass managed to miss
            // enemies who were all but touching Renzo. A weapon occupies the whole
            // span from the fist outward, and at point-blank range you are struck
            // by the guard and the pommel rather than the tip.
            var grip = prop.Find("PrimaryGrip");
            var tip = prop.Find("HitPoint");
            if (tip != null)
            {
                _localRoot = grip != null ? prop.InverseTransformPoint(grip.position) : Vector3.zero;
                _localTip = prop.InverseTransformPoint(tip.position);
                // The edge threatens a volume, not a mathematical line. A hair-thin
                // segment reads as a miss on blows that visually connect.
                Radius = 0.12f;
            }
            else if (!DeriveFromBounds(prop)) { _blade = null; return false; }

            // A blade shorter than a hand is a marker mistake; treat it as absent
            // rather than tracing a point.
            if ((_localTip - _localRoot).sqrMagnitude < 0.0004f) { _blade = null; return false; }
            return true;
        }

        /// <summary>Sample the blade where the animation has put it this frame.</summary>
        public void Sample()
        {
            if (_blade == null) return;
            Root = _blade.TransformPoint(_localRoot);
            Tip = _blade.TransformPoint(_localTip);
        }

        /// <summary>
        /// The prop currently in a hand. Renzo carries the whole catalogue
        /// pre-attached with all but one disabled — a build cannot instantiate
        /// FBX assets — so the active child is the weapon he is holding.
        /// </summary>
        private static Transform FindProp(Transform socket)
        {
            if (socket == null) return null;
            for (var i = 0; i < socket.childCount; i++)
            {
                var c = socket.GetChild(i);
                if (c.gameObject.activeSelf && c.name.StartsWith("Prop_")) return c;
            }
            return null;
        }

        /// <summary>
        /// No markers: take the mesh bounds in the prop's own space and call the
        /// long axis the blade. Every prop in the project — KayKit and wrapper
        /// alike — is authored with the weapon pointing along local +Y.
        /// </summary>
        private bool DeriveFromBounds(Transform prop)
        {
            var any = false;
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            var filters = prop.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null) continue;
                var b = mesh.bounds;
                // Corners through the child's transform into prop space.
                for (var c = 0; c < 8; c++)
                {
                    var corner = new Vector3(
                        (c & 1) == 0 ? b.min.x : b.max.x,
                        (c & 2) == 0 ? b.min.y : b.max.y,
                        (c & 4) == 0 ? b.min.z : b.max.z);
                    var p = prop.InverseTransformPoint(filters[i].transform.TransformPoint(corner));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            if (!any) return false;

            var mid = (min + max) * 0.5f;
            // The grip sits at the origin by the prop convention, so the weapon is
            // the span from there to whichever end reaches furthest.
            _localRoot = Vector3.zero;
            _localTip = new Vector3(mid.x, Mathf.Abs(max.y) >= Mathf.Abs(min.y) ? max.y : min.y, mid.z);
            var size = max - min;
            Radius = Mathf.Clamp(Mathf.Max(size.x, size.z) * 0.5f, 0.10f, 0.22f);
            return true;
        }
    }
}
