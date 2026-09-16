using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Asserts the region's shape: the campaign's valley floor is unchanged
    /// where missions are fought, every road is walkable, every landmark floor
    /// is flat and above water, the falls fall, the marsh is wet, and the
    /// world's edge is a wall. Pure function checks; no scene, no play mode.
    /// </summary>
    public static class EmberWorldCheck
    {
        private static int _fails;

        [MenuItem("Emberline/Check World")]
        public static void Run()
        {
            _fails = 0;

            // The arena: nothing in the play radius is steeper than a stair.
            var steep = 0;
            for (var a = 0; a < 36; a++)
            for (var r = 4f; r <= 34f; r += 6f)
            {
                var x = Mathf.Cos(a * 10f * Mathf.Deg2Rad) * r;
                var z = Mathf.Sin(a * 10f * Mathf.Deg2Rad) * r;
                if (ZoneTerrain.SlopeAt(x, z) > 0.35f) steep++;
            }
            Report("play radius stays a fighting floor", steep == 0, $"steep samples={steep}");
            Report("village green sits at the meadow floor", Mathf.Abs(ZoneTerrain.HeightAt(0f, 0f) - 2.5f) < 0.6f,
                $"h(0,0)={ZoneTerrain.HeightAt(0f, 0f):0.00}");

            // Roads: walkable along their whole length (the controller's 50°).
            var worst = 0f; Vector2 where = default;
            foreach (var road in ZoneTerrain.RoadLines)
                for (var i = 0; i < road.Length - 1; i++)
                {
                    var n = Mathf.CeilToInt(Vector2.Distance(road[i], road[i + 1]) / 3f);
                    for (var k = 0; k <= n; k++)
                    {
                        var p = Vector2.Lerp(road[i], road[i + 1], n == 0 ? 0f : k / (float)n);
                        var s = ZoneTerrain.SlopeAt(p.x, p.y);
                        if (s > worst) { worst = s; where = p; }
                    }
                }
            var profile = "";
            if (worst >= 0.85f)
                for (var k = -4; k <= 4; k++)
                    profile += $" z{where.y + k:0}={ZoneTerrain.HeightAt(where.x, where.y + k):0.0}";
            Report("every road is walkable", worst < 0.85f, $"worst slope={worst:0.00} at ({where.x:0},{where.y:0}){profile}");

            // Passes: the ring is actually open where the roads cross it.
            foreach (var (name, p) in new[] { ("north", ZoneTerrain.PassNorth), ("east", ZoneTerrain.PassEast), ("south", ZoneTerrain.PassSouth) })
            {
                var h = ZoneTerrain.HeightAt(p.x, p.z);
                Report($"{name} pass is a saddle, not a wall", h < 16f, $"h={h:0.0}");
            }

            // Landmark floors: flat, dry.
            foreach (var l in WorldLandmarks.All)
            {
                // Baked valley places are the zone snapshot's to judge; this checks the streamed ones.
                if (l.kind is LandmarkKind.Marsh or LandmarkKind.Waterfall or LandmarkKind.Pass
                    or LandmarkKind.Village or LandmarkKind.Camp or LandmarkKind.Shrine) continue;
                var s = 0f;
                for (var a = 0; a < 8; a++)
                {
                    var x = l.centre.x + Mathf.Cos(a * 45f * Mathf.Deg2Rad) * 6f;
                    var z = l.centre.z + Mathf.Sin(a * 45f * Mathf.Deg2Rad) * 6f;
                    s = Mathf.Max(s, ZoneTerrain.SlopeAt(x, z));
                }
                var h = l.Height;
                Report($"{l.id}: flat, dry floor", s < 0.25f && h > ZoneTerrain.WaterLevelAt(l.centre.x, l.centre.z) + 0.3f,
                    $"slope={s:0.00} h={h:0.0}");
            }

            // The falls: the upper river is high, the lower river low, at the lip.
            var above = ZoneTerrain.HeightAt(ZoneTerrain.RiverCentreX + Mathf.Sin(130f * 0.032f) * 9f + Mathf.Sin(130f * 0.011f) * 5f, 130f);
            var below = ZoneTerrain.HeightAt(ZoneTerrain.RiverCentreX + Mathf.Sin(106f * 0.032f) * 9f + Mathf.Sin(106f * 0.011f) * 5f, 106f);
            Report("the falls drop", above - below > 5f, $"above={above:0.0} below={below:0.0}");

            // The marsh: wet, with pools.
            var wet = 0; var pools = 0;
            for (var i = 0; i < 400; i++)
            {
                var x = ZoneTerrain.MarshCentre.x + (i % 20 - 10) * 8f;
                var z = ZoneTerrain.MarshCentre.z + (i / 20 - 10) * 8f;
                var h = ZoneTerrain.HeightAt(x, z);
                if (h < ZoneTerrain.WaterLevel + 1.2f) wet++;
                if (h < ZoneTerrain.WaterLevel) pools++;
            }
            Report("the marsh is low and pooled", wet > 250 && pools > 20, $"wet={wet}/400 pools={pools}");

            // The edge: mountains all round.
            var low = 0;
            for (var i = 0; i < 80; i++)
            {
                var a = i / 80f * Mathf.PI * 2f;
                var x = Mathf.Clamp(Mathf.Cos(a) * 600f, -395f, 395f);
                var z = Mathf.Clamp(Mathf.Sin(a) * 600f, -395f, 395f);
                if (ZoneTerrain.HeightAt(x, z) < 35f) low++;
            }
            Report("the world's edge is a wall", low == 0, $"low samples={low}");

            Debug.Log(_fails == 0 ? "[WLD] PASS — the region holds its shape"
                : $"[WLD] FAIL — {_fails} check(s) failed");
            if (Application.isBatchMode && !EmberCampaignBatch.Chained) EditorApplication.Exit(_fails == 0 ? 0 : 1);
        }

        private static void Report(string label, bool ok, string detail)
        {
            if (!ok) _fails++;
            Debug.Log($"[WLD] {(ok ? "pass" : "FAIL")}  {label}  ({detail})");
        }
    }
}
