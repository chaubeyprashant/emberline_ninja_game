using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Story
{
    /// <summary>Lookup for cinematic subjects, by name.</summary>
    public static class Cast
    {
        private static readonly List<CastMember> Members = new();

        public static void Register(CastMember m)
        {
            if (m != null && !Members.Contains(m)) Members.Add(m);
        }

        public static void Unregister(CastMember m) => Members.Remove(m);

        public static void Clear() => Members.Clear();

        /// <summary>The tagged object, or null. An unknown name is not an error —
        /// a shot with no subject keeps whatever the previous shot was framing.</summary>
        public static Transform Find(string castName)
        {
            if (string.IsNullOrEmpty(castName)) return null;
            for (var i = 0; i < Members.Count; i++)
            {
                var m = Members[i];
                if (m != null && string.Equals(m.castName, castName,
                        System.StringComparison.OrdinalIgnoreCase))
                    return m.transform;
            }

            // Registry miss: fall back to a scene search once, and cache it. The
            // registry is filled by OnEnable, which is reliable at runtime but not
            // in edit mode or for a cast member spawned after the beat starts.
            foreach (var m in Object.FindObjectsByType<CastMember>(FindObjectsSortMode.None))
            {
                if (!string.Equals(m.castName, castName,
                        System.StringComparison.OrdinalIgnoreCase)) continue;
                Register(m);
                return m.transform;
            }
            return null;
        }
    }
}
