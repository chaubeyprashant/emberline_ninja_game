using System.Collections.Generic;
using System.IO;
using Emberline.Core;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// The valley's own architecture, generated: minka farmhouses, a kura
    /// storehouse, a yagura watchtower, a stake palisade and its gate, kasuga
    /// stone lanterns, a torii and a hokora shrine. They replace the Kenney
    /// Fantasy Town and Castle pieces, which read as low-poly Europe.
    ///
    /// <para>
    /// No CC0 Japanese building kit exists, and anything licensed cannot sit in
    /// a public repository, so the kit is code: flat-shaded meshes in metres on
    /// the same night palette as the repainted Kenney atlases (highlights held
    /// under 0.62), one shared material per surface, one prefab per building
    /// under <c>Resources/Props/Zone/jpk_*</c> so the editor bake and the runtime
    /// landmarks load them by the same name. Pivots sit at ground level at the
    /// footprint centre, front facing −Z.
    /// </para>
    ///
    /// <para>
    /// The look comes from three things a European kit does not have: the deep
    /// hipped roof with eaves that lift at the corners, the raised floor with its
    /// veranda, and the dark timber grid over pale infill.
    /// </para>
    /// </summary>
    public static class EmberJapanKit
    {
        private const string PrefabDir = "Assets/Resources/Props/Zone";
        private const string MeshDir = "Assets/Art/Environments/JapanKit/Meshes";
        private const string MatDir = "Assets/Prefabs/EnvMaterials";

        // Surface slots. Each becomes one shared material and one submesh.
        private enum S { TimberDark, Timber, Plaster, Paper, Thatch, Tile, Stone, Vermilion, Lacquer, Glow, Cloth }

        private static readonly Dictionary<S, (Surface surface, Color color)> Palette = new()
        {
            [S.TimberDark] = (Surface.Wood, new Color(0.19f, 0.14f, 0.10f)),
            [S.Timber] = (Surface.Wood, new Color(0.36f, 0.26f, 0.17f)),
            [S.Plaster] = (Surface.Stone, new Color(0.60f, 0.57f, 0.50f)),
            [S.Paper] = (Surface.Cloth, new Color(0.62f, 0.58f, 0.47f)),
            [S.Thatch] = (Surface.Foliage, new Color(0.47f, 0.39f, 0.25f)),
            [S.Tile] = (Surface.Stone, new Color(0.21f, 0.24f, 0.28f)),
            [S.Stone] = (Surface.Stone, new Color(0.38f, 0.38f, 0.36f)),
            [S.Vermilion] = (Surface.Wood, new Color(0.60f, 0.19f, 0.11f)),
            [S.Lacquer] = (Surface.Wood, new Color(0.08f, 0.08f, 0.09f)),
            [S.Cloth] = (Surface.Cloth, new Color(0.13f, 0.17f, 0.30f)),
            [S.Glow] = (Surface.Emissive, new Color(1f, 0.62f, 0.30f)),
        };

        [MenuItem("Emberline/Build Japan Kit")]
        public static void Build()
        {
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PrefabDir);
            var mats = Materials();

            Save("jpk_minka_s", Minka(6f, 6f, thatch: true, litShoji: false), mats, Body(6f, 6f), BodySize(6f, 6f));
            Save("jpk_minka_s_lit", Minka(6f, 6f, thatch: true, litShoji: true), mats, Body(6f, 6f), BodySize(6f, 6f));
            Save("jpk_minka_m", Minka(9f, 6f, thatch: false, litShoji: true), mats, Body(9f, 6f), BodySize(9f, 6f));
            Save("jpk_minka_l", Minka(12f, 7f, thatch: false, litShoji: false, noren: true), mats, Body(12f, 7f), BodySize(12f, 7f));
            Save("jpk_kura", Kura(4.5f, 5.5f), mats, new Vector3(0f, 2.4f, 0f), new Vector3(4.6f, 4.8f, 5.6f));
            Save("jpk_yagura", Yagura(), mats);
            Save("jpk_saku", Saku(3f), mats);
            Save("jpk_gate", Gate(5f), mats, pillars: (2.5f, 3.8f));
            Save("jpk_toro", Toro(), mats);
            Save("jpk_torii", Torii(4.2f, 4.6f), mats, pillars: (2.1f, 4.6f));
            Save("jpk_hokora", Hokora(), mats);

            AssetDatabase.SaveAssets();
            Debug.Log("[JapanKit] 11 prefabs written to " + PrefabDir);
            if (Application.isBatchMode && !SessionState.GetBool("emberline.setup", false)) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------ buildings

        // A minka's solid: plinth to eave over the walled footprint.
        private static Vector3 Body(float w, float d) => new(0f, 1.5f, 0f);
        private static Vector3 BodySize(float w, float d) => new(w + 0.3f, 3.6f, d + 0.3f);

        /// <summary>
        /// A minka on a stone plinth: raised floor, veranda on the front, a post
        /// every ~1.8 m, board skirting under plaster or shoji, a hipped roof with
        /// lifted corners — thatch with a dark ridge for farmhouses, tile for town.
        /// </summary>
        private static MeshBuilder Minka(float w, float d, bool thatch, bool litShoji, bool noren = false)
        {
            var m = new MeshBuilder();
            const float plinth = 0.45f, floorT = 0.12f, wallH = 2.5f;
            var floorY = plinth + floorT;
            var hx = w * 0.5f; var hz = d * 0.5f;

            // Plinth runs a metre into the ground so a slope never shows daylight under it.
            m.Box(new Vector3(0, (plinth - 1.2f) * 0.5f, 0), new Vector3(w + 0.3f, plinth + 1.2f, d + 0.3f), S.Stone);
            m.Box(new Vector3(0, plinth + floorT * 0.5f, 0), new Vector3(w, floorT, d), S.Timber);
            // Engawa: the veranda along the front, a step lower than the floor.
            m.Box(new Vector3(0, plinth - 0.05f, -hz - 0.55f), new Vector3(w, 0.1f, 1.1f), S.Timber);
            m.Box(new Vector3(0, plinth * 0.5f - 0.3f, -hz - 1.05f), new Vector3(w, plinth + 0.6f, 0.12f), S.TimberDark);

            // Posts on a ~1.8 m grid round the footprint, and the engawa's outer posts.
            var nx = Mathf.Max(2, Mathf.RoundToInt(w / 1.8f));
            var nz = Mathf.Max(2, Mathf.RoundToInt(d / 1.8f));
            for (var i = 0; i <= nx; i++)
            {
                var x = -hx + w * i / nx;
                m.Box(new Vector3(x, floorY + wallH * 0.5f, -hz), new Vector3(0.2f, wallH, 0.2f), S.TimberDark);
                m.Box(new Vector3(x, floorY + wallH * 0.5f, hz), new Vector3(0.2f, wallH, 0.2f), S.TimberDark);
                if (i % 2 == 0)
                    m.Box(new Vector3(x, (plinth + floorY + wallH) * 0.5f, -hz - 1.0f),
                        new Vector3(0.16f, floorY + wallH - plinth, 0.16f), S.TimberDark);
            }
            for (var i = 1; i < nz; i++)
            {
                var z = -hz + d * i / nz;
                m.Box(new Vector3(-hx, floorY + wallH * 0.5f, z), new Vector3(0.2f, wallH, 0.2f), S.TimberDark);
                m.Box(new Vector3(hx, floorY + wallH * 0.5f, z), new Vector3(0.2f, wallH, 0.2f), S.TimberDark);
            }

            // Walls: board skirting below, plaster above; the front is shoji.
            const float skirt = 0.75f;
            m.Box(new Vector3(0, floorY + wallH * 0.5f, hz - 0.04f), new Vector3(w, wallH, 0.08f), S.Plaster);
            m.Box(new Vector3(-hx + 0.04f, floorY + wallH * 0.5f, 0), new Vector3(0.08f, wallH, d), S.Plaster);
            m.Box(new Vector3(hx - 0.04f, floorY + wallH * 0.5f, 0), new Vector3(0.08f, wallH, d), S.Plaster);
            m.Box(new Vector3(0, floorY + skirt * 0.5f, hz + 0.01f), new Vector3(w, skirt, 0.08f), S.Timber);
            m.Box(new Vector3(-hx - 0.01f, floorY + skirt * 0.5f, 0), new Vector3(0.08f, skirt, d), S.Timber);
            m.Box(new Vector3(hx + 0.01f, floorY + skirt * 0.5f, 0), new Vector3(0.08f, skirt, d), S.Timber);
            // Nageshi: the tie beam that runs round the top of the walls.
            m.Box(new Vector3(0, floorY + wallH - 0.15f, -hz), new Vector3(w + 0.2f, 0.22f, 0.24f), S.TimberDark);
            m.Box(new Vector3(0, floorY + wallH - 0.15f, hz), new Vector3(w + 0.2f, 0.22f, 0.24f), S.TimberDark);
            m.Box(new Vector3(-hx, floorY + wallH - 0.15f, 0), new Vector3(0.24f, 0.22f, d), S.TimberDark);
            m.Box(new Vector3(hx, floorY + wallH - 0.15f, 0), new Vector3(0.24f, 0.22f, d), S.TimberDark);

            // Shoji along the front: paper with a lattice; the middle bay is the
            // doorway, hung with a noren on the big house.
            for (var i = 0; i < nx; i++)
            {
                var x0 = -hx + w * i / nx + 0.1f; var x1 = -hx + w * (i + 1) / nx - 0.1f;
                var cx = (x0 + x1) * 0.5f; var pw = x1 - x0;
                var doorway = i == nx / 2;
                if (doorway && noren)
                {
                    m.Box(new Vector3(cx, floorY + wallH - 0.75f, -hz - 0.02f), new Vector3(pw, 0.9f, 0.03f), S.Cloth);
                    m.Box(new Vector3(cx, floorY + (wallH - 1.2f) * 0.5f, -hz + 0.3f), new Vector3(pw, wallH - 1.2f, 0.05f), S.TimberDark);
                    continue;
                }
                var paper = litShoji && !doorway ? S.Glow : S.Paper;
                m.Box(new Vector3(cx, floorY + (wallH - 0.3f) * 0.5f, -hz + 0.02f), new Vector3(pw, wallH - 0.3f, 0.04f), paper);
                for (var k = 1; k < 3; k++)
                    m.Box(new Vector3(x0 + pw * k / 3f, floorY + (wallH - 0.3f) * 0.5f, -hz - 0.02f),
                        new Vector3(0.04f, wallH - 0.3f, 0.03f), S.TimberDark);
                for (var k = 1; k < 5; k++)
                    m.Box(new Vector3(cx, floorY + (wallH - 0.3f) * k / 5f, -hz - 0.02f),
                        new Vector3(pw, 0.04f, 0.03f), S.TimberDark);
            }

            // Roof. Thatch sits steep and thick; tile sits lower with more lift.
            var eaveY = floorY + wallH;
            var over = thatch ? 1.0f : 1.15f;
            var rise = thatch ? Mathf.Min(w, d) * 0.62f : Mathf.Min(w, d) * 0.42f;
            var lift = thatch ? 0.25f : 0.55f;
            m.HipRoof(eaveY, hx + over, hz + over + 0.5f, rise, lift, thatch ? 0.45f : 0.2f,
                thatch ? S.Thatch : S.Tile, S.TimberDark, zShift: -0.25f);
            // Ridge cap and its end blocks.
            var ridgeHalf = Mathf.Max(0.3f, hx + over - (hz + over + 0.5f));
            var ridgeY = eaveY + rise + (thatch ? 0.45f : 0.2f);
            m.Box(new Vector3(0, ridgeY + 0.1f, -0.25f), new Vector3(ridgeHalf * 2f + 0.6f, 0.3f, 0.5f),
                thatch ? S.TimberDark : S.Tile);
            if (!thatch)
                foreach (var sx in new[] { -1f, 1f })
                    m.Box(new Vector3(sx * (ridgeHalf + 0.3f), ridgeY + 0.3f, -0.25f), new Vector3(0.3f, 0.55f, 0.55f), S.Tile);
            return m;
        }

        /// <summary>Kura: thick white walls on a dark stone base, a heavy tile gable roof, one small window.</summary>
        private static MeshBuilder Kura(float w, float d)
        {
            var m = new MeshBuilder();
            const float h = 4.2f;
            m.Box(new Vector3(0, -0.35f, 0), new Vector3(w + 0.3f, 1.3f, d + 0.3f), S.Stone);
            m.Box(new Vector3(0, 0.3f + h * 0.5f, 0), new Vector3(w, h, d), S.Plaster);
            // The dark lower band (namako-kabe) a kura always wears.
            m.Box(new Vector3(0, 0.3f + 0.6f, 0), new Vector3(w + 0.06f, 1.2f, d + 0.06f), S.Lacquer);
            m.Box(new Vector3(0, 0.3f + h - 0.15f, 0), new Vector3(w + 0.1f, 0.3f, d + 0.1f), S.Plaster);
            m.Box(new Vector3(0, 0.3f + 1.2f, -d * 0.5f - 0.05f), new Vector3(1.3f, 2.2f, 0.12f), S.TimberDark);
            m.Box(new Vector3(0, 0.3f + h - 1.0f, -d * 0.5f - 0.05f), new Vector3(0.8f, 0.6f, 0.12f), S.Lacquer);
            m.GableRoof(0.3f + h, w * 0.5f + 0.5f, d * 0.5f + 0.4f, 1.6f, S.Tile, S.TimberDark);
            m.Box(new Vector3(0, 0.3f + h + 1.7f, 0), new Vector3(0.45f, 0.35f, d + 1.0f), S.Tile);
            return m;
        }

        /// <summary>Yagura: four splayed legs, braces, a walled platform at 6 m and a small hipped roof.</summary>
        private static MeshBuilder Yagura()
        {
            var m = new MeshBuilder();
            const float deck = 6f, top = 2.8f, foot = 3.6f;
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var a = new Vector3(sx * foot * 0.5f, -0.5f, sz * foot * 0.5f);
                var b = new Vector3(sx * top * 0.5f, deck + 2.2f, sz * top * 0.5f);
                m.Beam(a, b, 0.24f, S.TimberDark);
            }
            foreach (var y in new[] { 1.6f, 3.8f })
            {
                var t = (y + 0.5f) / (deck + 2.7f);
                var half = Mathf.Lerp(foot, top, t) * 0.5f;
                m.Beam(new Vector3(-half, y, -half), new Vector3(half, y, -half), 0.14f, S.Timber);
                m.Beam(new Vector3(-half, y, half), new Vector3(half, y, half), 0.14f, S.Timber);
                m.Beam(new Vector3(-half, y, -half), new Vector3(-half, y, half), 0.14f, S.Timber);
                m.Beam(new Vector3(half, y, -half), new Vector3(half, y, half), 0.14f, S.Timber);
            }
            m.Beam(new Vector3(-foot * 0.47f, 0f, -foot * 0.47f), new Vector3(top * 0.5f, deck, -top * 0.5f), 0.1f, S.Timber);
            m.Beam(new Vector3(foot * 0.47f, 0f, foot * 0.47f), new Vector3(-top * 0.5f, deck, top * 0.5f), 0.1f, S.Timber);
            m.Box(new Vector3(0, deck, 0), new Vector3(top + 0.6f, 0.18f, top + 0.6f), S.Timber);
            foreach (var (cx, cz, sx, sz) in new[] { (0f, -1f, 1f, 0f), (0f, 1f, 1f, 0f), (-1f, 0f, 0f, 1f), (1f, 0f, 0f, 1f) })
                m.Box(new Vector3(cx * (top * 0.5f + 0.25f), deck + 0.5f, cz * (top * 0.5f + 0.25f)),
                    new Vector3(sx * (top + 0.6f) + 0.1f, 0.85f, sz * (top + 0.6f) + 0.1f), S.Timber);
            m.HipRoof(deck + 2.2f, top * 0.5f + 0.7f, top * 0.5f + 0.7f, 1.3f, 0.3f, 0.15f, S.Thatch, S.TimberDark);
            return m;
        }

        /// <summary>Saku: a run of sharpened stakes on two rails, one segment long.</summary>
        private static MeshBuilder Saku(float length)
        {
            var m = new MeshBuilder();
            const int n = 7;
            for (var i = 0; i < n; i++)
            {
                var x = -length * 0.5f + length * (i + 0.5f) / n;
                var h = 2.3f + ((i * 37) % 5) * 0.08f;
                m.Box(new Vector3(x, (h - 0.6f) * 0.5f, 0), new Vector3(0.26f, h + 0.6f, 0.26f), S.Timber);
                m.Pyramid(new Vector3(x, h - 0.3f, 0), 0.26f, 0.45f, S.Timber);
            }
            foreach (var y in new[] { 0.6f, 1.7f })
                m.Box(new Vector3(0, y, -0.2f), new Vector3(length, 0.14f, 0.12f), S.TimberDark);
            return m;
        }

        /// <summary>Camp gate: two heavy posts, a crossbeam and a small gable roof over the opening.</summary>
        private static MeshBuilder Gate(float span)
        {
            var m = new MeshBuilder();
            foreach (var sx in new[] { -1f, 1f })
                m.Box(new Vector3(sx * span * 0.5f, 1.4f, 0), new Vector3(0.4f, 3.8f, 0.4f), S.TimberDark);
            m.Box(new Vector3(0, 3.0f, 0), new Vector3(span + 0.8f, 0.32f, 0.32f), S.TimberDark);
            m.Box(new Vector3(0, 2.4f, 0), new Vector3(span, 0.2f, 0.24f), S.Timber);
            m.GableRoof(3.25f, span * 0.5f + 0.9f, 0.9f, 0.7f, S.Tile, S.TimberDark, ridgeAlongX: true);
            return m;
        }

        // --------------------------------------------------------------- props

        /// <summary>Kasuga tōrō: hex base, round shaft, hex firebox with lit windows, hex roof, finial.</summary>
        private static MeshBuilder Toro()
        {
            var m = new MeshBuilder();
            m.Prism(new Vector3(0, -0.2f, 0), 0.38f, 0.32f, 0.4f, 6, S.Stone);
            m.Prism(new Vector3(0, 0.2f, 0), 0.13f, 0.13f, 0.75f, 8, S.Stone);
            m.Prism(new Vector3(0, 0.95f, 0), 0.24f, 0.32f, 0.16f, 6, S.Stone);
            m.Prism(new Vector3(0, 1.11f, 0), 0.24f, 0.24f, 0.36f, 6, S.Stone);
            // Two lit windows, front and back, slightly proud of the firebox.
            foreach (var sz in new[] { -1f, 1f })
                m.Quad(new Vector3(-0.12f, 1.16f, sz * 0.215f), new Vector3(0.12f, 1.16f, sz * 0.215f),
                       new Vector3(0.12f, 1.42f, sz * 0.215f), new Vector3(-0.12f, 1.42f, sz * 0.215f), S.Glow, twoSided: true);
            m.Prism(new Vector3(0, 1.47f, 0), 0.46f, 0.08f, 0.32f, 6, S.Stone);
            m.Prism(new Vector3(0, 1.79f, 0), 0.07f, 0.03f, 0.18f, 6, S.Stone);
            return m;
        }

        /// <summary>
        /// Torii: vermilion pillars on black feet, the nuki tie beam through
        /// them, and the kasagi lintel curving up at both ends.
        /// </summary>
        private static MeshBuilder Torii(float span, float height)
        {
            var m = new MeshBuilder();
            foreach (var sx in new[] { -1f, 1f })
            {
                m.Prism(new Vector3(sx * span * 0.5f, -0.4f, 0), 0.2f, 0.18f, height + 0.4f, 10, S.Vermilion);
                m.Prism(new Vector3(sx * span * 0.5f, -0.4f, 0), 0.24f, 0.24f, 0.75f, 10, S.Lacquer);
            }
            m.Box(new Vector3(0, height * 0.72f, 0), new Vector3(span + 0.9f, 0.22f, 0.18f), S.Vermilion);
            m.Box(new Vector3(0, height * 0.86f, 0), new Vector3(0.2f, height * 0.28f, 0.16f), S.Vermilion);
            m.CurvedBeam(height - 0.05f, span * 0.5f + 0.95f, 0.26f, 0.32f, 0.28f, S.Vermilion);
            m.CurvedBeam(height + 0.2f, span * 0.5f + 1.05f, 0.18f, 0.36f, 0.32f, S.Lacquer);
            return m;
        }

        /// <summary>Hokora: a small roadside shrine on a stone plinth, vermilion posts, tile roof.</summary>
        private static MeshBuilder Hokora()
        {
            var m = new MeshBuilder();
            m.Box(new Vector3(0, -0.25f, 0), new Vector3(1.9f, 1.1f, 1.9f), S.Stone);
            m.Box(new Vector3(0, 0.37f, 0), new Vector3(1.5f, 0.14f, 1.5f), S.Timber);
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                m.Box(new Vector3(sx * 0.62f, 0.95f, sz * 0.62f), new Vector3(0.12f, 1.05f, 0.12f), S.Vermilion);
            m.Box(new Vector3(0, 0.95f, 0.6f), new Vector3(1.24f, 1.05f, 0.06f), S.Timber);
            m.Box(new Vector3(-0.6f, 0.95f, 0), new Vector3(0.06f, 1.05f, 1.24f), S.Timber);
            m.Box(new Vector3(0.6f, 0.95f, 0), new Vector3(0.06f, 1.05f, 1.24f), S.Timber);
            m.Box(new Vector3(0, 0.95f, -0.58f), new Vector3(1.0f, 0.95f, 0.04f), S.Paper);
            m.GableRoof(1.48f, 1.05f, 1.15f, 0.55f, S.Tile, S.TimberDark);
            m.Box(new Vector3(0, 2.05f, 0), new Vector3(0.18f, 0.16f, 2.4f), S.Tile);
            return m;
        }

        // ------------------------------------------------------------- assets

        private static Dictionary<S, Material> Materials()
        {
            var mats = new Dictionary<S, Material>();
            foreach (var kv in Palette)
            {
                var path = $"{MatDir}/Env_jpk_{kv.Key}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(SurfaceKit.SurfaceShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                if (kv.Key == S.Glow)
                {
                    // Lantern fireboxes and lit shoji: additive, soft at the edges,
                    // so a window reads as light at night without a light source.
                    mat.shader = Shader.Find("Emberline/Glow");
                    mat.SetColor("_Color", new Color(1f, 0.6f, 0.28f, 0.9f));
                    mat.SetFloat("_Soft", 0.6f);
                }
                else
                {
                    mat.shader = SurfaceKit.SurfaceShader;
                    SurfaceKit.Apply(mat, kv.Value.surface, kv.Value.color);
                    mat.enableInstancing = true;
                }
                EditorUtility.SetDirty(mat);
                mats[kv.Key] = mat;
            }
            return mats;
        }

        private static void Save(string name, MeshBuilder b, Dictionary<S, Material> mats,
            Vector3? solidCentre = null, Vector3? solidSize = null, (float half, float height)? pillars = null)
        {
            var (mesh, used) = b.Build(name);
            var meshPath = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var list = new Material[used.Count];
            for (var i = 0; i < used.Count; i++) list[i] = mats[used[i]];
            r.sharedMaterials = list;
            if (pillars is { } pl)
            {
                // Gates are walked through: only their two pillars are solid.
                foreach (var sx in new[] { -1f, 1f })
                {
                    var post = go.AddComponent<BoxCollider>();
                    post.center = new Vector3(sx * pl.half, pl.height * 0.5f, 0f);
                    post.size = new Vector3(0.6f, pl.height, 0.6f);
                }
            }
            else
            {
                var box = go.AddComponent<BoxCollider>();
                // Buildings collide on their walls, not their eaves.
                box.center = solidCentre ?? mesh.bounds.center;
                box.size = solidSize ?? mesh.bounds.size;
            }
            go.isStatic = true;
            // Where real light comes from: LanternLights lends these a point light
            // when they are among the nearest to the camera.
            if (name == "jpk_toro")
                Anchor(go, new Vector3(0f, 1.3f, 0f), 6.5f, 1.5f);
            else if (name is "jpk_minka_s_lit" or "jpk_minka_m")
                Anchor(go, new Vector3(0f, 1.8f, -4.2f), 8f, 1.1f);
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        private static void Anchor(GameObject go, Vector3 offset, float range, float intensity)
        {
            var a = go.AddComponent<LightAnchor>();
            a.offset = offset;
            a.range = range;
            a.intensity = intensity;
            a.color = new Color(1f, 0.62f, 0.32f);
        }

        // -------------------------------------------------------- mesh builder

        /// <summary>Flat-shaded mesh by surface slot: every face has its own vertices.</summary>
        private class MeshBuilder
        {
            private readonly List<Vector3> _v = new();
            private readonly List<Vector3> _n = new();
            private readonly List<Vector2> _uv = new();
            private readonly Dictionary<S, List<int>> _tris = new();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, S s, bool twoSided = false)
            {
                Face(a, b, c, d, s);
                if (twoSided) Face(d, c, b, a, s);
            }

            private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, S s)
            {
                var n = Vector3.Cross(b - a, d - a).normalized;
                if (n.sqrMagnitude < 0.5f) n = Vector3.Cross(c - b, a - b).normalized;
                var i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
                for (var k = 0; k < 4; k++) _n.Add(n);
                _uv.Add(new Vector2(0, 0)); _uv.Add(new Vector2(1, 0)); _uv.Add(new Vector2(1, 1)); _uv.Add(new Vector2(0, 1));
                var t = Tris(s);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }

            private void Tri(Vector3 a, Vector3 b, Vector3 c, S s)
            {
                var n = Vector3.Cross(b - a, c - a).normalized;
                var i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c);
                for (var k = 0; k < 3; k++) _n.Add(n);
                _uv.Add(new Vector2(0, 0)); _uv.Add(new Vector2(1, 0)); _uv.Add(new Vector2(0.5f, 1));
                var t = Tris(s);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            private List<int> Tris(S s)
            {
                if (!_tris.TryGetValue(s, out var t)) _tris[s] = t = new List<int>();
                return t;
            }

            public void Box(Vector3 c, Vector3 size, S s) => Box(c, size, Quaternion.identity, s);

            private void Box(Vector3 c, Vector3 size, Quaternion rot, S s)
            {
                var h = size * 0.5f;
                Vector3 P(float x, float y, float z) => c + rot * new Vector3(x * h.x, y * h.y, z * h.z);
                // Clockwise from outside, as Unity expects for front faces.
                Face(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), s);   // -Z
                Face(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1), s);       // +Z
                Face(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), s);   // -X
                Face(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), s);       // +X
                Face(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), s);       // +Y
                Face(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), s);   // -Y
            }

            /// <summary>A square timber from a to b.</summary>
            public void Beam(Vector3 a, Vector3 b, float thick, S s)
            {
                var dir = b - a;
                var rot = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                Box((a + b) * 0.5f, new Vector3(thick, dir.magnitude, thick), rot, s);
            }

            /// <summary>A regular prism from y to y+height, base radius r0, top radius r1.</summary>
            public void Prism(Vector3 baseCentre, float r0, float r1, float height, int sides, S s)
            {
                var top = baseCentre + Vector3.up * height;
                for (var i = 0; i < sides; i++)
                {
                    var a0 = Mathf.PI * 2f * i / sides; var a1 = Mathf.PI * 2f * (i + 1) / sides;
                    var d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    Face(baseCentre + d0 * r0, top + d0 * r1, top + d1 * r1, baseCentre + d1 * r0, s);
                    if (r1 > 0.001f) Tri(top + d1 * r1, top + d0 * r1, top, s);
                    Tri(baseCentre + d0 * r0, baseCentre + d1 * r0, baseCentre, s);
                }
            }

            /// <summary>A four-sided spike: the cut top of a stake.</summary>
            public void Pyramid(Vector3 baseCentre, float width, float height, S s)
            {
                var h = width * 0.5f; var apex = baseCentre + Vector3.up * height;
                var p = new[] { new Vector3(-h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h), new Vector3(h, 0, -h) };
                for (var i = 0; i < 4; i++) Tri(baseCentre + p[(i + 1) % 4], apex, baseCentre + p[i], s);
            }

            /// <summary>
            /// Hipped roof over a rectangle (half extents hx, hz incl. overhang),
            /// ridge along X. The eave line lifts toward the corners (the sori);
            /// the surface has a thickness and a timber underside.
            /// </summary>
            public void HipRoof(float eaveY, float hx, float hz, float rise, float lift, float thick,
                S top, S under, float zShift = 0f)
            {
                const int n = 8;
                var rx = Mathf.Max(0.3f, hx - hz);
                var ridgeY = eaveY + rise;
                Vector3 Eave(float x, float z)
                {
                    // Corner lift grows with the square of the distance along each edge.
                    var tx = Mathf.Abs(x) / hx; var tz = Mathf.Abs(z) / hz;
                    var t = Mathf.Max(tx, tz) >= 0.999f ? Mathf.Min(tx, tz) : 0f;
                    return new Vector3(x, eaveY + lift * t * t, z + zShift);
                }
                Vector3 R(float x) => new Vector3(Mathf.Clamp(x, -rx, rx), ridgeY, zShift);
                var up = Vector3.up * thick;

                for (var i = 0; i < n; i++)
                {
                    var x0 = Mathf.Lerp(-hx, hx, (float)i / n); var x1 = Mathf.Lerp(-hx, hx, (float)(i + 1) / n);
                    var rx0 = Mathf.Lerp(-rx, rx, (float)i / n); var rx1 = Mathf.Lerp(-rx, rx, (float)(i + 1) / n);
                    // Front (−Z) and back (+Z) slopes.
                    Slab(Eave(x0, -hz), Eave(x1, -hz), R(rx1), R(rx0));
                    Slab(Eave(x1, hz), Eave(x0, hz), R(rx0), R(rx1));
                    // Hip ends (±X): fan to the ridge end.
                    var z0 = Mathf.Lerp(-hz, hz, (float)i / n); var z1 = Mathf.Lerp(-hz, hz, (float)(i + 1) / n);
                    SlabTri(Eave(-hx, z1), Eave(-hx, z0), R(-rx));
                    SlabTri(Eave(hx, z0), Eave(hx, z1), R(rx));
                }
                // Eave fascia: the visible thickness at the roof's edge.
                for (var i = 0; i < n; i++)
                {
                    var x0 = Mathf.Lerp(-hx, hx, (float)i / n); var x1 = Mathf.Lerp(-hx, hx, (float)(i + 1) / n);
                    var z0 = Mathf.Lerp(-hz, hz, (float)i / n); var z1 = Mathf.Lerp(-hz, hz, (float)(i + 1) / n);
                    Face(Eave(x0, -hz), Eave(x0, -hz) + up, Eave(x1, -hz) + up, Eave(x1, -hz), under);
                    Face(Eave(x1, hz), Eave(x1, hz) + up, Eave(x0, hz) + up, Eave(x0, hz), under);
                    Face(Eave(-hx, z1), Eave(-hx, z1) + up, Eave(-hx, z0) + up, Eave(-hx, z0), under);
                    Face(Eave(hx, z0), Eave(hx, z0) + up, Eave(hx, z1) + up, Eave(hx, z1), under);
                }

                void Slab(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
                {
                    Face(a + up, d + up, c + up, b + up, top);   // outer, faces up and out
                    Face(a, b, c, d, under);                      // the underside of the eaves
                }

                void SlabTri(Vector3 a, Vector3 b, Vector3 c)
                {
                    Tri(a + up, c + up, b + up, top);
                    Tri(a, b, c, under);
                }
            }

            /// <summary>Plain gable roof, ridge along Z (or X), with thickness and an underside.</summary>
            public void GableRoof(float eaveY, float hx, float hz, float rise, S top, S under, bool ridgeAlongX = false)
            {
                Vector3 P(float a, float y, float b) => ridgeAlongX ? new Vector3(b, y, a) : new Vector3(a, y, b);
                var ha = ridgeAlongX ? hz : hx; var hb = ridgeAlongX ? hx : hz;
                var up = Vector3.up * 0.18f;
                foreach (var side in new[] { -1f, 1f })
                {
                    var e0 = P(side * ha, eaveY, -hb); var e1 = P(side * ha, eaveY, hb);
                    var r0 = P(0, eaveY + rise, -hb); var r1 = P(0, eaveY + rise, hb);
                    // Swapping the axes for a ridge along X mirrors the winding,
                    // so these faces are two-sided rather than trusted to one order.
                    Quad(e0 + up, e1 + up, r1 + up, r0 + up, top, twoSided: true);
                    Quad(e0, r0, r1, e1, under, twoSided: true);
                    Quad(e0, e0 + up, e1 + up, e1, under, twoSided: true);
                }
                // Gable ends.
                foreach (var end in new[] { -1f, 1f })
                {
                    var a = P(-ha * 0.85f, eaveY, end * hb * 0.9f); var b = P(ha * 0.85f, eaveY, end * hb * 0.9f);
                    var c = P(0, eaveY + rise * 0.85f, end * hb * 0.9f);
                    if (end < 0) { Tri(a, c, b, under); Tri(b, c, a, under); }
                    else { Tri(b, c, a, under); Tri(a, c, b, under); }
                }
            }

            /// <summary>A lintel along X whose ends sweep up: the torii's kasagi.</summary>
            public void CurvedBeam(float y, float halfLen, float height, float depth, float lift, S s)
            {
                const int n = 10;
                for (var i = 0; i < n; i++)
                {
                    var x0 = Mathf.Lerp(-halfLen, halfLen, (float)i / n);
                    var x1 = Mathf.Lerp(-halfLen, halfLen, (float)(i + 1) / n);
                    var y0 = y + lift * Mathf.Pow(Mathf.Abs(x0) / halfLen, 3f);
                    var y1 = y + lift * Mathf.Pow(Mathf.Abs(x1) / halfLen, 3f);
                    var c = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f + height * 0.5f, 0);
                    var rot = Quaternion.FromToRotation(Vector3.right, new Vector3(x1 - x0, y1 - y0, 0).normalized);
                    Box(c, new Vector3(Vector3.Distance(new Vector3(x0, y0), new Vector3(x1, y1)) + 0.02f, height, depth), rot, s);
                }
            }

            public (Mesh, List<S>) Build(string name)
            {
                var mesh = new Mesh { name = name };
                if (_v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_v);
                mesh.SetNormals(_n);
                mesh.SetUVs(0, _uv);
                var cols = new Color[_v.Count];
                for (var i = 0; i < cols.Length; i++) cols[i] = Color.white;
                mesh.colors = cols;
                var used = new List<S>(_tris.Keys);
                used.Sort();
                mesh.subMeshCount = used.Count;
                for (var i = 0; i < used.Count; i++) mesh.SetTriangles(_tris[used[i]], i);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return (mesh, used);
            }
        }
    }
}
