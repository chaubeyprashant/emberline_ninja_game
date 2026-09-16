using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>What a landmark is, which picks the dressing the streamer builds there.</summary>
    public enum LandmarkKind
    {
        Village, Camp, Shrine, Temple, Stronghold, Cave, Marsh, Hamlet, Hideout, Waterfall, Pass, Ruin,
    }

    /// <summary>One named place in the region.</summary>
    public class Landmark
    {
        public string id;
        public string name;
        public string blurb;
        public LandmarkKind kind;
        public Vector3 centre;
        public float radius;     // discovery radius, and roughly the dressing footprint
        public float yaw;        // dressing facing

        public float Height => ZoneTerrain.HeightAt(centre.x, centre.z);
        public Vector3 Ground => new(centre.x, Height, centre.z);
    }

    /// <summary>
    /// The region's named places, in one table, and which of them Renzo has
    /// found. Discovery is the explore loop's smallest reward: walk somewhere
    /// new, get its name, and it stays on the record. The positions come from
    /// <see cref="ZoneTerrain"/> so the terrain shape, the dressing and the name
    /// can never disagree about where a place is.
    /// </summary>
    public static class WorldLandmarks
    {
        public static readonly Landmark[] All =
        {
            new() { id = "yorune", name = "YORUNE", blurb = "What the fire left of home.",
                    kind = LandmarkKind.Village, centre = ZoneTerrain.VillageCentre, radius = 30f },
            new() { id = "camp", name = "THE RAIDERS' CAMP", blurb = "A palisade on the plateau, and the pen behind it.",
                    kind = LandmarkKind.Camp, centre = ZoneTerrain.CampCentre, radius = 24f },
            new() { id = "wayshrine", name = "THE WAYSHRINE", blurb = "A roof, a lantern, a place to breathe.",
                    kind = LandmarkKind.Shrine, centre = new Vector3(-33f, 0f, 40f), radius = 8f },
            new() { id = "pass_north", name = "THE NORTH PASS", blurb = "The road out, and the road Kagehira's men came in by.",
                    kind = LandmarkKind.Pass, centre = ZoneTerrain.PassNorth, radius = 22f, yaw = 0f },
            new() { id = "pass_east", name = "THE PILGRIM PASS", blurb = "The old way to the temple.",
                    kind = LandmarkKind.Pass, centre = ZoneTerrain.PassEast, radius = 22f, yaw = 80f },
            new() { id = "pass_south", name = "THE TOLL GAP", blurb = "Goro's road to the marsh. Everyone pays.",
                    kind = LandmarkKind.Pass, centre = ZoneTerrain.PassSouth, radius = 22f, yaw = 140f },
            new() { id = "waterfall", name = "THE FALLS", blurb = "Where the river enters the valley, loud enough to hide a step.",
                    kind = LandmarkKind.Waterfall, centre = ZoneTerrain.Waterfall, radius = 24f },
            new() { id = "temple", name = "THE HILL TEMPLE", blurb = "The monks left when the soldiers came. The roof is still theirs.",
                    kind = LandmarkKind.Temple, centre = ZoneTerrain.TempleHill, radius = 40f, yaw = 180f },
            new() { id = "stronghold", name = "KAGEHIRA'S STRONGHOLD", blurb = "Walls on a plateau, and every road watched from them.",
                    kind = LandmarkKind.Stronghold, centre = ZoneTerrain.Stronghold, radius = 60f, yaw = 200f },
            new() { id = "cave", name = "THE HOLLOW", blurb = "A mouth in the ridge. Someone has camped here recently.",
                    kind = LandmarkKind.Cave, centre = ZoneTerrain.CaveMouth, radius = 20f },
            new() { id = "marsh", name = "THE DROWNED MARSH", blurb = "Kagachi's country. The water keeps what it takes.",
                    kind = LandmarkKind.Marsh, centre = ZoneTerrain.MarshCentre, radius = 90f },
            new() { id = "hamlet", name = "KAWAI HAMLET", blurb = "Three farms and a well, and people who still bow to the toll-men.",
                    kind = LandmarkKind.Hamlet, centre = ZoneTerrain.Hamlet, radius = 44f, yaw = 30f },
            new() { id = "hideout", name = "THE SHINOBI HOLLOW", blurb = "A clearing no road reaches. Suzu's people knew it.",
                    kind = LandmarkKind.Hideout, centre = ZoneTerrain.Hideout, radius = 22f, yaw = 300f },
        };

        private static readonly Dictionary<string, Landmark> ById = new();

        public static Landmark Find(string id)
        {
            if (ById.Count == 0) foreach (var l in All) ById[l.id] = l;
            return ById.TryGetValue(id, out var l2) ? l2 : null;
        }

        // ---------------------------------------------------------- discovery

        public static readonly HashSet<string> Discovered = new();

        /// <summary>Fires with the landmark the player just walked into for the first time.</summary>
        public static event System.Action<Landmark> OnDiscovered;

        private static float _pollT;

        /// <summary>Call once a frame from whoever owns the explore loop.</summary>
        public static void Poll(Vector3 playerPos)
        {
            _pollT -= Time.deltaTime;
            if (_pollT > 0f) return;
            _pollT = 0.5f;
            foreach (var l in All)
            {
                if (Discovered.Contains(l.id)) continue;
                var dx = playerPos.x - l.centre.x;
                var dz = playerPos.z - l.centre.z;
                if (dx * dx + dz * dz > l.radius * l.radius) continue;
                Discovered.Add(l.id);
                OnDiscovered?.Invoke(l);
            }
        }

        /// <summary>The nearest place not yet found, for the explore objective line.</summary>
        public static Landmark NearestUndiscovered(Vector3 playerPos)
        {
            Landmark best = null;
            var bestD = float.MaxValue;
            foreach (var l in All)
            {
                if (Discovered.Contains(l.id)) continue;
                var dx = playerPos.x - l.centre.x;
                var dz = playerPos.z - l.centre.z;
                var d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = l; }
            }
            return best;
        }

        /// <summary>Compass bearing text for the HUD: "NE 240 m".</summary>
        public static string Bearing(Vector3 from, Vector3 to)
        {
            var dx = to.x - from.x;
            var dz = to.z - from.z;
            var dist = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dz * dz) / 10f) * 10;
            var ang = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            var dirs = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            var dir = dirs[Mathf.RoundToInt(ang / 45f) % 8];
            return $"{dir} {dist} m";
        }
    }
}
