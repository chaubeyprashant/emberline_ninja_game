using System.IO;
using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Draws the whole region from the height function as a PNG — one pixel a
    /// metre, palette colour by ground type, water in blue, roads and landmark
    /// rings on top — so the layout can be judged without play mode or a build.
    /// Logs/region_map.png.
    /// </summary>
    public static class EmberRegionMap
    {
        [MenuItem("Emberline/Render Region Map")]
        public static void Render()
        {
            const int size = (int)(ZoneTerrain.RegionHalf * 2f);
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            var px = new Color32[size * size];
            for (var py = 0; py < size; py++)
            for (var pxi = 0; pxi < size; pxi++)
            {
                var x = pxi - ZoneTerrain.RegionHalf + 0.5f;
                var z = py - ZoneTerrain.RegionHalf + 0.5f;
                var h = ZoneTerrain.HeightAt(x, z);
                var hx = ZoneTerrain.HeightAt(x + 1f, z) - ZoneTerrain.HeightAt(x - 1f, z);
                var hz = ZoneTerrain.HeightAt(x, z + 1f) - ZoneTerrain.HeightAt(x, z - 1f);
                var n = new Vector3(-hx, 2f, -hz).normalized;
                Color c;
                var water = ZoneTerrain.WaterLevelAt(x, z);
                if (h < water) c = Color.Lerp(new Color(0.12f, 0.25f, 0.42f), new Color(0.05f, 0.12f, 0.25f),
                    Mathf.Clamp01((water - h) / 3f));
                else c = TerrainMesh.Palette[(int)TerrainMesh.GroundAt(x, z, h, n)];
                // Shade by height so the ring, the hills and the edge read.
                var shade = Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01((h + 4f) / 70f)) * Mathf.Lerp(0.7f, 1.1f, n.y);
                c *= shade * 2.2f;
                px[py * size + pxi] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b));
            }
            // Roads, then landmark rings.
            foreach (var road in ZoneTerrain.RoadLines)
                for (var i = 0; i < road.Length - 1; i++)
                    Line(px, size, road[i], road[i + 1], new Color32(255, 225, 160, 255));
            foreach (var l in WorldLandmarks.All)
                Ring(px, size, new Vector2(l.centre.x, l.centre.z), l.radius, new Color32(255, 110, 60, 255));
            tex.SetPixels32(px);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/region_map.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Region] Logs/region_map.png written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Plot(Color32[] px, int size, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= size || y >= size) return;
            px[y * size + x] = c;
        }

        private static void Line(Color32[] px, int size, Vector2 a, Vector2 b, Color32 c)
        {
            var n = Mathf.CeilToInt(Vector2.Distance(a, b));
            for (var i = 0; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, n == 0 ? 0f : i / (float)n);
                var x = Mathf.RoundToInt(p.x + ZoneTerrain.RegionHalf);
                var y = Mathf.RoundToInt(p.y + ZoneTerrain.RegionHalf);
                Plot(px, size, x, y, c); Plot(px, size, x + 1, y, c); Plot(px, size, x, y + 1, c);
            }
        }

        private static void Ring(Color32[] px, int size, Vector2 centre, float r, Color32 c)
        {
            var n = Mathf.CeilToInt(r * 6.3f);
            for (var i = 0; i < n; i++)
            {
                var a = i / (float)n * Mathf.PI * 2f;
                Plot(px, size, Mathf.RoundToInt(centre.x + Mathf.Cos(a) * r + ZoneTerrain.RegionHalf),
                    Mathf.RoundToInt(centre.y + Mathf.Sin(a) * r + ZoneTerrain.RegionHalf), c);
            }
        }
    }
}
