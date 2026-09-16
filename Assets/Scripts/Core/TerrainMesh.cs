using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Builds one flat-shaded terrain chunk from <see cref="ZoneTerrain"/> and
    /// colours it from the palette atlas. Runtime code because the streamer
    /// builds the outer region's chunks on the fly; the editor bakes the valley's
    /// own chunks through the same function so the two can never disagree.
    ///
    /// <para>
    /// Flat shading is achieved by never sharing a vertex between triangles, so
    /// every triangle gets its own normal and reads as a facet. Colour comes from
    /// a 4x4 palette texture: every vertex of a triangle samples the same cell
    /// centre, so the colour is flat per facet and the transitions crisp.
    /// </para>
    /// </summary>
    public static class TerrainMesh
    {
        /// <summary>Edge length of one terrain quad.</summary>
        public const float Quad = 2.5f;

        /// <summary>Quads per chunk edge: 25 m chunks.</summary>
        public const int ChunkQuads = 10;
        public const float ChunkSize = Quad * ChunkQuads;

        /// <summary>Palette cells, as (column,row) in the 4x4 atlas.</summary>
        public enum Ground
        {
            Grass = 0, Grass2 = 1, Grass3 = 2, Dirt = 3, Road = 4, Rock = 5, Scree = 6, Sand = 7,
            Riverbed = 8, Marsh = 9, Snow = 10, Moss = 11,
        }

        /// <summary>Palette colours, indexed by <see cref="Ground"/>.</summary>
        public static readonly Color[] Palette =
        {
            new(0.170f, 0.235f, 0.180f),  // Grass   — cool night green
            new(0.145f, 0.205f, 0.165f),  // Grass2  — darker patch
            new(0.200f, 0.255f, 0.185f),  // Grass3  — lighter patch
            new(0.245f, 0.215f, 0.170f),  // Dirt    — trodden earth
            new(0.290f, 0.250f, 0.195f),  // Road    — pale packed track
            new(0.215f, 0.225f, 0.245f),  // Rock    — blue-grey stone
            new(0.255f, 0.260f, 0.270f),  // Scree   — lighter broken stone
            new(0.300f, 0.285f, 0.235f),  // Sand    — river shingle
            new(0.130f, 0.165f, 0.180f),  // Riverbed— wet dark
            new(0.150f, 0.190f, 0.150f),  // Marsh   — sodden dark green
            new(0.420f, 0.440f, 0.470f),  // Snow    — the high passes
            new(0.185f, 0.245f, 0.160f),  // Moss    — the temple hill
        };

        private const float RiverWidth = 9f;

        public static Ground GroundAt(float x, float z, float h, Vector3 normal)
        {
            var marsh = ZoneTerrain.MarshMask(x, z);
            if (marsh > 0.5f)
                return h < ZoneTerrain.WaterLevel + 0.1f ? Ground.Riverbed : Ground.Marsh;

            var rv = ZoneTerrain.RiverDistance(x, z);
            if (rv < RiverWidth * 0.55f) return Ground.Riverbed;
            if (rv < RiverWidth + 2.5f) return Ground.Sand;

            var road = ZoneTerrain.RoadDistance(x, z);
            if (road < 3.4f) return Ground.Road;
            if (road < 5.0f) return Ground.Dirt;

            // Steep faces and high ground turn to rock: the mountains read as
            // stone without needing a second material. Above the ridges, snow.
            var slope = 1f - normal.y;
            if (h > 44f && slope < 0.3f) return Ground.Snow;
            if (slope > 0.42f) return Ground.Rock;
            if (h > 16f && slope > 0.12f) return Ground.Scree;
            if (slope > 0.26f) return Ground.Scree;

            // Trodden ground around the settlements.
            if (Dist(x, z, ZoneTerrain.VillageCentre) < ZoneTerrain.VillageRadius * 0.72f) return Ground.Dirt;
            if (Dist(x, z, ZoneTerrain.CampCentre) < ZoneTerrain.CampRadius * 0.8f) return Ground.Dirt;
            if (Dist(x, z, ZoneTerrain.Stronghold) < ZoneTerrain.StrongholdRadius * 0.85f) return Ground.Dirt;
            if (Dist(x, z, ZoneTerrain.Hamlet) < ZoneTerrain.HamletRadius * 0.5f) return Ground.Dirt;
            if (Dist(x, z, ZoneTerrain.TempleHill) < ZoneTerrain.TempleRadius + 20f) return Ground.Moss;

            // Three grass shades, picked by noise, so the meadow is not one flat
            // colour across 800 metres.
            var g = ZoneTerrain.Noise(x * 0.04f + 11f, z * 0.04f - 7f);
            return g > 0.55f ? Ground.Grass2 : g < -0.35f ? Ground.Grass3 : Ground.Grass;
        }

        private static float Dist(float x, float z, Vector3 c)
        {
            var dx = x - c.x; var dz = z - c.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// One chunk whose south-west corner is at (x0, z0). Vertices are in world
        /// space, so the chunk object sits at the origin.
        /// </summary>
        public static Mesh BuildChunk(float x0, float z0, string name)
        {
            var verts = new List<Vector3>(ChunkQuads * ChunkQuads * 6);
            var norms = new List<Vector3>(verts.Capacity);
            var uvs = new List<Vector2>(verts.Capacity);
            var tris = new List<int>(verts.Capacity);

            for (var qz = 0; qz < ChunkQuads; qz++)
            for (var qx = 0; qx < ChunkQuads; qx++)
            {
                var ax = x0 + qx * Quad;
                var az = z0 + qz * Quad;
                var bx = ax + Quad;
                var bz = az + Quad;

                var p00 = new Vector3(ax, ZoneTerrain.HeightAt(ax, az), az);
                var p10 = new Vector3(bx, ZoneTerrain.HeightAt(bx, az), az);
                var p01 = new Vector3(ax, ZoneTerrain.HeightAt(ax, bz), bz);
                var p11 = new Vector3(bx, ZoneTerrain.HeightAt(bx, bz), bz);

                // Split the quad along the shorter diagonal so ridges stay sharp
                // instead of being averaged into a saddle.
                if (Mathf.Abs(p00.y - p11.y) <= Mathf.Abs(p10.y - p01.y))
                {
                    AddTri(verts, norms, uvs, tris, p00, p01, p11);
                    AddTri(verts, norms, uvs, tris, p00, p11, p10);
                }
                else
                {
                    AddTri(verts, norms, uvs, tris, p00, p01, p10);
                    AddTri(verts, norms, uvs, tris, p01, p11, p10);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Appends one flat-shaded triangle: its own three vertices, one shared
        /// face normal, and all three UVs on the same palette cell centre.
        /// </summary>
        private static void AddTri(List<Vector3> verts, List<Vector3> norms,
            List<Vector2> uvs, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            if (n.y < 0f) n = -n;

            var mid = (a + b + c) / 3f;
            var uv = CellUv(GroundAt(mid.x, mid.z, mid.y, n));

            var i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        public static Vector2 CellUv(Ground g)
        {
            const int dim = 4;
            var idx = (int)g;
            var col = idx % dim;
            var row = idx / dim;
            // Centre of the cell, so point filtering can never bleed a neighbour.
            return new Vector2((col + 0.5f) / dim, (row + 0.5f) / dim);
        }

        /// <summary>True when a chunk has any water in it (river, upper river, marsh).</summary>
        public static bool ChunkHasWater(float x0, float z0, out float level)
        {
            level = ZoneTerrain.WaterLevel;
            var any = false;
            for (var qz = 0; qz <= ChunkQuads; qz += 2)
            for (var qx = 0; qx <= ChunkQuads; qx += 2)
            {
                var x = x0 + qx * Quad;
                var z = z0 + qz * Quad;
                var w = ZoneTerrain.WaterLevelAt(x, z);
                if (ZoneTerrain.HeightAt(x, z) < w - 0.05f) { any = true; level = w; }
            }
            return any;
        }
    }
}
