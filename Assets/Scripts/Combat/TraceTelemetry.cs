using UnityEngine;

namespace Emberline.Combat
{
    /// <summary>
    /// Counters proving the swept trace is the thing resolving hits, not the arc
    /// fallback quietly carrying the fight. Without this it is impossible to tell
    /// a working trace from one that never resolves a blade and silently degrades
    /// to the old behaviour — which would look identical in play.
    /// </summary>
    public static class TraceTelemetry
    {
        public static int Swings;        // swings that opened an active window
        public static int Traced;        // …of those, ones with a live blade
        public static int TraceHits;     // targets hit by the blade itself
        public static int FallbackArcs;  // swings resolved by the legacy cone
        public static int FrozenBlades;  // blades that never moved (culled rig)

        public static void Reset()
        {
            Swings = Traced = TraceHits = FallbackArcs = FrozenBlades = 0;
        }

        public static string Summary =>
            $"swings={Swings} traced={Traced} traceHits={TraceHits} " +
            $"arcFallback={FallbackArcs} frozen={FrozenBlades}";
    }
}
