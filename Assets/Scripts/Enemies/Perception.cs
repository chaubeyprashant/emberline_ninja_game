using System.Collections.Generic;
using UnityEngine;
using Emberline.Core;

namespace Emberline.Enemies
{
    /// <summary>
    /// World noise events. The player is not only seen — they are heard. Anything
    /// loud (a landing, a swing, a body hitting the deck) drops an event here and
    /// nearby enemies investigate the position rather than the player.
    ///
    /// A tiny ring buffer, not a subscription system: enemies poll it on their own
    /// slice, so no per-frame callbacks and no allocation.
    /// </summary>
    public static class NoiseSystem
    {
        public struct Noise
        {
            public Vector3 position;
            public float radius;
            public float time;      // unscaled
        }

        private const int Capacity = 16;
        private static readonly Noise[] Ring = new Noise[Capacity];
        private static int _next;

        /// <summary>How long a sound stays investigable.</summary>
        public const float Memory = 3.5f;

        public static void Emit(Vector3 position, float radius)
        {
            Ring[_next] = new Noise
            {
                position = position, radius = radius, time = Time.unscaledTime,
            };
            _next = (_next + 1) % Capacity;
        }

        /// <summary>Loudest recent sound audible from `ear`, or false if silence.</summary>
        public static bool Hear(Vector3 ear, out Vector3 where)
        {
            where = default;
            var best = -1f;
            var now = Time.unscaledTime;
            for (var i = 0; i < Capacity; i++)
            {
                var n = Ring[i];
                if (n.radius <= 0f || now - n.time > Memory) continue;
                var d = Vector3.Distance(ear, n.position);
                if (d > n.radius) continue;
                // Closer to the source and louder both win.
                var score = n.radius - d;
                if (score <= best) continue;
                best = score;
                where = n.position;
            }
            return best >= 0f;
        }

        public static void Clear()
        {
            for (var i = 0; i < Capacity; i++) Ring[i] = default;
            _next = 0;
        }
    }

    /// <summary>
    /// A body left on the deck. An unaware enemy that sees one raises the alarm —
    /// killing quietly is not enough if you leave the evidence in a lit corridor.
    /// </summary>
    public static class BodyWatch
    {
        private const float Lifetime = 20f;

        private struct Body { public Vector3 pos; public float time; public bool found; }

        private static readonly List<Body> Bodies = new();

        public static void Report(Vector3 pos) =>
            Bodies.Add(new Body { pos = pos, time = Time.unscaledTime });

        public static void Clear() => Bodies.Clear();

        /// <summary>Nearest un-discovered body visible from `eye`, if any.</summary>
        public static bool Spot(Vector3 eye, float range, out Vector3 where)
        {
            where = default;
            var now = Time.unscaledTime;
            for (var i = Bodies.Count - 1; i >= 0; i--)
            {
                var b = Bodies[i];
                if (now - b.time > Lifetime) { Bodies.RemoveAt(i); continue; }
                if (b.found) continue;
                if (Vector3.Distance(eye, b.pos) > range) continue;
                if (ArenaMarkers.Blocked(eye + Vector3.up, b.pos + Vector3.up)) continue;
                b.found = true;
                Bodies[i] = b;
                where = b.pos;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// How visible the player currently is, 0..1. Crouching, distance and standing
    /// in smoke all reduce it; standing in lantern light raises it. Enemies scale
    /// their detection rate by this rather than treating sight as binary.
    /// </summary>
    public static class Visibility
    {
        /// <summary>Lantern practicals register the player as lit.</summary>
        private static readonly List<Vector3> LightSources = new();

        public static void RegisterLight(Vector3 pos) => LightSources.Add(pos);
        public static void ClearLights() => LightSources.Clear();

        public static float Of(Vector3 playerPos, bool crouched)
        {
            var v = crouched ? 0.45f : 1f;

            // Smoke hides you outright — it already blinds attacks, so it should
            // hide you from detection too.
            if (Player.SmokeCloud.Inside(playerPos)) v *= 0.25f;

            // Standing in a pool of lamplight makes you obvious.
            for (var i = 0; i < LightSources.Count; i++)
            {
                var d = Vector3.Distance(playerPos, LightSources[i]);
                if (d < 5f) { v *= 1.35f; break; }
            }
            return Mathf.Clamp(v, 0.08f, 1.6f);
        }
    }
}
