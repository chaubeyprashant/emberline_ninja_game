using Emberline.Core;
using Emberline.Story;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// Someone ahead on the path, seen and never met.
    ///
    /// <para>
    /// Deliberately built on the cast stand-in rather than on the adult Aiko
    /// placeholder. The stand-in is a featureless primitive: it has no face to
    /// show, so it cannot accidentally confirm what the mission is supposed to
    /// leave uncertain. At forty metres in fog that is exactly the right amount
    /// of information — a shape, dark clothing, and one red thread.
    /// </para>
    ///
    /// <para>
    /// It walks its waypoints only while the player is far enough away, keeps its
    /// back turned, and is gone before anyone can close on it. It is not an enemy,
    /// it has no brain, and nothing in the combat or AI stack knows it exists.
    /// </para>
    /// </summary>
    public class DistantFigure : MonoBehaviour
    {
        /// <summary>Closer than this and she has already moved on.</summary>
        private const float KeepAway = 13f;

        private Vector3[] _path;
        private int _next;
        private float _speed = 3.4f;
        private Transform _body;
        private float _fade = -1f;

        public static DistantFigure Play(Vector3[] path)
        {
            if (path == null || path.Length == 0) return null;
            var go = new GameObject("DistantFigure");
            var f = go.AddComponent<DistantFigure>();
            f._path = path;

            // The marked stand-in, moved out to the first waypoint and turned away.
            var t = CastStandIn.Ensure("AIKO");
            if (t == null) { Destroy(go); return null; }
            f._body = t;
            t.position = Ground.Snap(path[0]);
            return f;
        }

        private void Update()
        {
            if (_body == null) { Destroy(gameObject); return; }

            if (_fade >= 0f)
            {
                // Gone around the bend: shrink out rather than pop, then release
                // the stand-in so nothing is left standing in the world.
                _fade += Time.deltaTime;
                var k = Mathf.Clamp01(1f - _fade / 0.6f);
                _body.localScale = Vector3.one * k;
                if (k <= 0.01f) { Destroy(_body.gameObject); Destroy(gameObject); }
                return;
            }

            var motor = SceneRefs.Motor;
            if (motor == null) return;

            var target = _path[Mathf.Min(_next, _path.Length - 1)];
            var to = target - _body.position;
            to.y = 0f;

            // She only moves while the player is at a distance. Walking her on a
            // fixed clock either loses the player or lets them catch her.
            var gap = Vector3.Distance(motor.transform.position, _body.position);
            if (gap > KeepAway * 0.75f && to.magnitude > 0.6f)
            {
                var step = to.normalized * (_speed * Time.deltaTime);
                _body.position = Ground.Snap(_body.position + step);
                _body.rotation = Quaternion.Slerp(_body.rotation,
                    Quaternion.LookRotation(to.normalized), 6f * Time.deltaTime);
            }
            else if (to.magnitude <= 0.6f)
            {
                _next++;
                if (_next >= _path.Length) _fade = 0f;   // the last waypoint is the bend
            }

            // Too close, too fast: she is already gone.
            if (gap < KeepAway * 0.55f) _fade = 0f;
        }
    }
}
