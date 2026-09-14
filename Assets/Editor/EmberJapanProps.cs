using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Emberline.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Imports the Japanese storage props — rice bales, sake barrels, baskets, a
    /// tansu, a well bucket, a standing lantern — that replace the KayKit dungeon
    /// crates and the Kenney survival boxes.
    ///
    /// <para>
    /// Those crates were the last European thing standing in an Edo valley. Every
    /// camp and every mission clue was dressed with them, so a "rice store" was a
    /// stack of pine shipping crates. These models are Sketchfab CC BY 4.0
    /// downloads, converted from GLB to OBJ by <c>Tools/glb_parts.py</c>; licence
    /// records live next to each pack and in <c>Documentation/AssetLicenses</c>.
    /// </para>
    ///
    /// <para>
    /// <b>Every prefab is sized in metres and stands on its origin.</b> The source
    /// models arrive in whatever unit and pivot their authors used (the barrels
    /// are in centimetres, the rice bales sit a metre off-centre), so each is
    /// wrapped: a root at the footprint centre, the mesh scaled to a real height
    /// under it. Callers then place them like any other prop with scale 1.
    /// </para>
    ///
    /// <para>
    /// <b>Textures are graded on import</b>, the same idea as the Kenney atlas
    /// repaint but gentler: these are photographic PBR textures rather than
    /// cartoon swatches, so they only need pulling toward the cool night palette.
    /// </para>
    /// </summary>
    public static class EmberJapanProps
    {
        private const string SrcRoot = "Assets/Art/Environments/Japan";
        private const string OutDir = "Assets/Resources/Props/Zone";
        private const string MatDir = "Assets/Prefabs/EnvMaterials";
        private const string StackName = "jp_ricebale_stack";

        private readonly struct Item
        {
            public readonly string name;
            public readonly float height;      // metres, after fitting
            public readonly Surface surface;
            public readonly float yaw;         // turns the authored front toward -Z
            public readonly string[] parts;    // OBJ paths under SrcRoot, sharing one space

            public Item(string name, float height, Surface surface, params string[] parts)
                : this(name, height, surface, 0f, parts) { }

            public Item(string name, float height, Surface surface, float yaw, params string[] parts)
            {
                this.name = name; this.height = height; this.surface = surface;
                this.yaw = yaw; this.parts = parts;
            }
        }

        private static readonly Item[] Items =
        {
            // Straw: tawara rice bales and a rice bag, the camp's stores.
            new("jp_ricebale", 0.50f, Surface.Rope, "RiceBales_kcisameta/ricebales_kcisa.obj"),
            new("jp_ricebag", 0.26f, Surface.Rope, "RiceBag_KHSAsset/ricebag_mado.obj"),
            new("jp_basket", 0.34f, Surface.Rope, "Baskets_ahmagh2e/wicker_baskets_0.obj"),
            new("jp_basket_b", 0.40f, Surface.Rope, "Baskets_ahmagh2e/wicker_baskets_6.obj"),
            new("jp_basket_tall", 0.85f, Surface.Rope, "Baskets_ahmagh2e/wicker_baskets_4.obj"),

            // Coopered wood. The barrels and tubs share one centimetre scale in the
            // source (283 and 118 tall), so the tubs are sized to keep that ratio.
            new("jp_barrel_a", 0.95f, Surface.Wood, "Barrels_ahmagh2e/wood_barrels_3.obj"),
            new("jp_barrel_b", 0.95f, Surface.Wood, "Barrels_ahmagh2e/wood_barrels_1.obj"),
            new("jp_tub_a", 0.40f, Surface.Wood, "Barrels_ahmagh2e/wood_barrels_2.obj"),
            new("jp_tub_b", 0.40f, Surface.Wood, "Barrels_ahmagh2e/wood_barrels_0.obj"),
            new("jp_bucket", 0.55f, Surface.Wood, "Bucket_LuisVidal/well_bucket.obj"),

            // An Edo drawer chest in place of the iron-banded treasure chest. Its
            // drawers face +X in the source, which the OBJ importer mirrors to -X.
            new("jp_tansu", 0.90f, Surface.Wood, -90f,
                "Tansu_GiyoP/drawer_chest_0.obj", "Tansu_GiyoP/drawer_chest_1.obj",
                "Tansu_GiyoP/drawer_chest_2.obj"),

            // A standing andon lantern in place of the dungeon wall torch.
            new("jp_lantern_stand", 1.70f, Surface.Wood, "Lantern_Obeonix/standing_lantern.obj"),
        };

        [MenuItem("Emberline/Import Japanese Props")]
        public static void Import()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(MatDir);

            var materials = new Dictionary<string, Material>();
            var made = 0;
            foreach (var item in Items)
                if (BuildItem(item, materials)) made++;
            if (BuildStack()) made++;

            AssetDatabase.SaveAssets();
            Report();
            Debug.Log($"[Japan] {made} prefabs into {OutDir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static bool BuildItem(Item item, Dictionary<string, Material> materials)
        {
            var root = new GameObject(item.name);
            var meshes = new List<GameObject>();

            foreach (var part in item.parts)
            {
                var path = $"{SrcRoot}/{part}";
                ConfigureModel(path);
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var mat = MaterialFor(path, item.surface, materials);
                if (src == null || mat == null)
                {
                    Debug.LogWarning($"[Japan] {item.name}: missing model or texture for {path}");
                    Object.DestroyImmediate(root);
                    return false;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                go.name = meshes.Count == 0 ? "Mesh" : $"Mesh{meshes.Count}";
                go.transform.SetParent(root.transform, false);
                go.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                    r.gameObject.isStatic = true;
                }
                meshes.Add(go);
            }

            // Fit: footprint centre on the origin, base at y = 0, real height.
            if (!Bounds(root, out var b))
            {
                Object.DestroyImmediate(root);
                return false;
            }
            var scale = item.height / Mathf.Max(b.size.y, 1e-5f);
            foreach (var m in meshes)
            {
                m.transform.localScale = Vector3.one * scale;
                m.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) * scale;
            }

            root.isStatic = true;
            PrefabUtility.SaveAsPrefabAsset(root, $"{OutDir}/{item.name}.prefab");
            Object.DestroyImmediate(root);
            return true;
        }

        /// <summary>
        /// Two bales side by side and one across the top — what a store looks like
        /// when someone expects to come back for it. Replaces <c>crates_stacked</c>.
        /// </summary>
        private static bool BuildStack()
        {
            var bale = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/jp_ricebale.prefab");
            if (bale == null) return false;

            var root = new GameObject(StackName);
            void Put(Vector3 at, float yaw)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(bale);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = at;
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
            Put(new Vector3(-0.53f, 0f, 0f), 2f);
            Put(new Vector3(0.53f, 0f, 0.04f), -3f);
            Put(new Vector3(0f, 0.46f, 0.02f), 88f);

            root.isStatic = true;
            PrefabUtility.SaveAsPrefabAsset(root, $"{OutDir}/{StackName}.prefab");
            Object.DestroyImmediate(root);
            return true;
        }

        // ---------------------------------------------------------- materials

        /// <summary>
        /// One shared material per source texture. The OBJ's own <c>usemtl</c> name
        /// finds the texture, because glb_parts.py names textures after it.
        /// </summary>
        private static Material MaterialFor(string objPath, Surface surface,
            Dictionary<string, Material> cache)
        {
            var key = File.ReadLines(objPath).FirstOrDefault(l => l.StartsWith("usemtl "))?[7..].Trim();
            if (key == null) return null;
            var dir = Path.GetDirectoryName(objPath);
            var texPath = Directory.GetFiles(dir, $"*_{key}_basecolor.png").FirstOrDefault()?.Replace('\\', '/');
            if (texPath == null) return null;
            if (cache.TryGetValue(texPath, out var cached)) return cached;

            var stem = Path.GetFileNameWithoutExtension(texPath).Replace("_basecolor", "");
            var matPath = $"{MatDir}/Env_jp_{stem}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(SurfaceKit.SurfaceShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = SurfaceKit.SurfaceShader;
            SurfaceKit.Apply(mat, surface, Color.white);   // the grade already carries the tint
            mat.mainTexture = GradedTexture(texPath);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            cache[texPath] = mat;
            return mat;
        }

        // The first pass (0.45 and a 0.66 tint) turned golden straw and warm wood
        // into grey plaster; the valley's night light already cools them.
        private const float Desaturate = 0.20f;
        private static readonly Color Tint = new(0.82f, 0.82f, 0.86f);

        /// <summary>Pull a photographic texel toward the night palette.</summary>
        private static Color Grade(Color c)
        {
            var lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            var keep = 1f - Desaturate;
            var o = new Color(Mathf.Lerp(lum, c.r, keep), Mathf.Lerp(lum, c.g, keep),
                              Mathf.Lerp(lum, c.b, keep));
            // Same highlight ceiling as the Kenney repaint, so sun-bleached straw
            // does not become the brightest thing in the valley.
            const float ceiling = 0.62f;
            var v = Mathf.Max(o.r, Mathf.Max(o.g, o.b));
            if (v > ceiling) o *= Mathf.Lerp(1f, ceiling / v, 0.85f);
            return new Color(o.r * Tint.r, o.g * Tint.g, o.b * Tint.b, 1f);
        }

        private static Texture2D GradedTexture(string srcPath)
        {
            var outPath = srcPath.Replace("_basecolor.png", "_ember.png");

            if (AssetImporter.GetAtPath(srcPath) is TextureImporter si &&
                (!si.isReadable || si.textureCompression != TextureImporterCompression.Uncompressed))
            {
                si.isReadable = true;
                si.textureCompression = TextureImporterCompression.Uncompressed;
                si.maxTextureSize = 1024;
                si.SaveAndReimport();
            }
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (src == null) return null;

            // The rice bag's straw fringe is alpha-blended in the source, but
            // Emberline/Surface is opaque, so transparent texels would show
            // whatever colour the author left under them. Fill them with the
            // texture's mean colour instead.
            var px = src.GetPixels();
            var mean = Color.black;
            var opaque = 0;
            foreach (var p in px)
                if (p.a >= 0.5f) { mean += p; opaque++; }
            if (opaque > 0) mean /= opaque;
            for (var i = 0; i < px.Length; i++)
                px[i] = Grade(px[i].a >= 0.5f ? px[i] : mean);

            var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            dst.SetPixels(px);
            dst.Apply();
            File.WriteAllBytes(outPath, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(outPath);

            if (AssetImporter.GetAtPath(outPath) is TextureImporter oi)
            {
                oi.mipmapEnabled = true;
                oi.alphaSource = TextureImporterAlphaSource.None;
                oi.maxTextureSize = 512;     // a prop a metre tall, seen from a phone camera
                oi.textureCompression = TextureImporterCompression.Compressed;
                oi.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static void ConfigureModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) return;
            mi.globalScale = 1f;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.isReadable = false;
            mi.SaveAndReimport();
        }

        private static bool Bounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        // ------------------------------------------------------------- checks

        private static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"prefab",-20} {"w",6} {"h",6} {"d",6} {"minY",6} {"tris",6}");
            foreach (var n in Items.Select(i => i.name).Append(StackName))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/{n}.prefab");
                if (prefab == null) { sb.AppendLine($"{n,-20} MISSING"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds(inst, out var b);
                var tris = inst.GetComponentsInChildren<MeshFilter>(true)
                    .Where(mf => mf.sharedMesh != null).Sum(mf => mf.sharedMesh.triangles.Length / 3);
                sb.AppendLine($"{n,-20} {b.size.x,6:F2} {b.size.y,6:F2} {b.size.z,6:F2} {b.min.y,6:F2} {tris,6}");
                Object.DestroyImmediate(inst);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/jp_props.txt", sb.ToString());
            Debug.Log("[Japan] report\n" + sb);
        }

        /// <summary>
        /// Every prop in a row beside a 1.8 m figure, lit like the valley, so size
        /// and grade can be judged from one image. Run WITHOUT -nographics.
        /// </summary>
        [MenuItem("Emberline/Snapshot Japanese Props")]
        public static void Snapshot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(0.86f, 0.9f, 1f);
            sun.transform.rotation = Quaternion.Euler(38f, 35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.44f);

            var figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            figure.transform.position = new Vector3(0.3f, 0.9f, 0f);
            figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            figure.GetComponent<Renderer>().sharedMaterial =
                SurfaceKit.Make(Surface.Cloth, new Color(0.30f, 0.32f, 0.36f));

            var x = 1.0f;
            foreach (var n in Items.Select(i => i.name).Append(StackName))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/{n}.prefab");
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                Bounds(go, out var b);
                go.transform.position = new Vector3(x - b.min.x, 0f, 0f);
                x += b.size.x + 0.45f;
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.position = new Vector3(x * 0.5f, 0f, 1f);
            floor.transform.localScale = new Vector3(x + 4f, 8f, 1f);
            floor.GetComponent<Renderer>().sharedMaterial =
                SurfaceKit.Make(Surface.Stone, new Color(0.22f, 0.21f, 0.19f));

            const int W = 2400, H = 800;
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            cam.fieldOfView = 22f;
            var half = x * 0.5f + 0.3f;
            var dist = half / (Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * ((float)W / H));
            var look = new Vector3(x * 0.5f, 0.7f, 0f);
            cam.transform.position = look + new Vector3(0f, dist * 0.25f, -dist);
            cam.transform.LookAt(look);

            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/jp_props.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Debug.Log("[Japan] snapshot written to Logs/jp_props.png");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
