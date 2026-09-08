using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Measures imported weapon meshes so their grip and orientation can be
    /// corrected before they go anywhere near a hand.
    ///
    /// <para>
    /// The KayKit props the pipeline was built on share one convention: pivot at
    /// the grip, blade along +Y, so <c>AttachProp</c> parents them to the hand
    /// anchor with an identity transform. A marketplace model has whatever pivot
    /// and axis its author left it with — usually centred, usually along Z — and
    /// attaching that raw puts the guard in the palm and the blade out of the
    /// wrist. This prints what each mesh actually is, so the wrapper can fix it
    /// with numbers rather than guesses.
    /// </para>
    /// </summary>
    public static class EmberWeaponImport
    {
        public const string WeaponRoot = "Assets/Art/Weapons";

        [MenuItem("Emberline/Measure Weapon Meshes")]
        public static void Measure()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"mesh",-34} {"size x y z",-24} {"min y",7} {"max y",7} {"centre",-24} {"tris",6}  long-axis");

            var paths = Directory.GetFiles(WeaponRoot, "*.fbx", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Art/Characters/Props", "*.fbx"))
                .OrderBy(p => p);

            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is ModelImporter mi &&
                    (mi.materialImportMode != ModelImporterMaterialImportMode.None || mi.importAnimation))
                {
                    mi.materialImportMode = ModelImporterMaterialImportMode.None;
                    mi.importAnimation = false;
                    mi.animationType = ModelImporterAnimationType.None;
                    mi.SaveAndReimport();
                }

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                inst.transform.localScale = Vector3.one;

                var any = false; var b = new Bounds(); var tris = 0;
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

                if (any)
                {
                    var s = b.size;
                    var axis = s.x >= s.y && s.x >= s.z ? "X" : s.y >= s.z ? "Y" : "Z";
                    sb.AppendLine($"{Path.GetFileNameWithoutExtension(path),-34} " +
                                  $"{s.x,7:F3} {s.y,7:F3} {s.z,7:F3}  {b.min.y,7:F3} {b.max.y,7:F3} " +
                                  $"({b.center.x,6:F3},{b.center.y,6:F3},{b.center.z,6:F3}) {tris,6}  {axis}");
                }
                Object.DestroyImmediate(inst);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/weapon_meshes.txt", sb.ToString());
            Debug.Log("[Weapons] measured\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Renders every weapon mesh under Assets/Art/Weapons from the side with a
        /// red cube on its origin, so the grip position and blade direction can be
        /// read off an image instead of inferred from bounds. Run WITHOUT -nographics.
        /// </summary>
        [MenuItem("Emberline/Snapshot Weapon Meshes")]
        public static void SnapshotMeshes()
        {
            Directory.CreateDirectory("Logs");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var light = new GameObject("L").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, 30f, 0f);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);

            foreach (var path in Directory.GetFiles(WeaponRoot, "*.fbx", SearchOption.AllDirectories)
                         .Concat(Directory.GetFiles("Assets/Art/Characters/Props", "*.fbx")).OrderBy(p => p))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var mat = new Material(Shader.Find("Standard")) { color = new Color(0.7f, 0.72f, 0.78f) };
                var any = false; var b = new Bounds();
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.sharedMaterial = mat;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (!any) { Object.DestroyImmediate(inst); continue; }

                var ext = Mathf.Max(b.size.x, b.size.y, b.size.z);
                var origin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                origin.transform.localScale = Vector3.one * ext * 0.04f;
                origin.GetComponent<Renderer>().sharedMaterial =
                    new Material(Shader.Find("Standard")) { color = Color.red };
                // +Y marker green, +Z marker blue, so the axes are readable in the image.
                var yM = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                yM.transform.position = Vector3.up * ext * 0.15f;
                yM.transform.localScale = Vector3.one * ext * 0.03f;
                yM.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.green };
                var zM = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                zM.transform.position = Vector3.forward * ext * 0.15f;
                zM.transform.localScale = Vector3.one * ext * 0.03f;
                zM.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.blue };

                var camGo = new GameObject("Cam");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = ext * 0.6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                // Look along -X so the YZ plane is the image: Z horizontal, Y vertical.
                cam.transform.position = b.center + Vector3.right * (ext * 3f);
                cam.transform.LookAt(b.center);

                var rt = new RenderTexture(1600, 800, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 800, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 800), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"Logs/mesh_{Path.GetFileNameWithoutExtension(path)}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo); Object.DestroyImmediate(origin);
                Object.DestroyImmediate(yM); Object.DestroyImmediate(zM); Object.DestroyImmediate(inst);
            }
            Debug.Log("[Weapons] mesh snapshots written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------ wrappers

        public const string PrefabDir = "Assets/Art/Weapons/Prefabs";

        /// <summary>
        /// One imported weapon and the numbers that make it hold correctly.
        /// Points are in the source mesh's own space, read off the measurement
        /// snapshot; the builder does the rotation so nobody has to.
        /// </summary>
        public struct WrapSpec
        {
            public string name;        // prop name WeaponDef.propRight refers to
            public string fbx;         // source asset path
            public float scale;        // mesh units -> prop units (KayKit sword is 1.775 long)
            public Vector3 rotEuler;   // rotation that brings the blade onto +Y
            public Vector3 grip;       // where the hand closes; becomes the origin
            public Vector3 guard;      // BladeRoot: where blade leaves the hilt
            public Vector3 tip;        // HitPoint
        }

        /// <summary>
        /// Every marketplace weapon in the project. The KayKit props need no
        /// entry: they were authored to the convention these wrappers reproduce.
        /// </summary>
        public static readonly WrapSpec[] Wraps =
        {
            // Dokazvo katana: authored along Z, tip at +Z, origin sitting on the
            // blade. Guard at z=-19, pommel at z=-35.6, tip at z=+22.5. Grip a
            // third of the way down the tsuka. 1.9 prop units total = 1.18 m on
            // Renzo, a touch longer than the generic sword it replaces.
            new WrapSpec
            {
                name = "katana", fbx = "Assets/Art/Weapons/Katana_Dokazvo/katana_dokazvo.fbx",
                scale = 1.9f / 58.097f, rotEuler = new Vector3(-90f, 0f, 0f),
                grip = new Vector3(0f, 0f, -24f), guard = new Vector3(0f, 0f, -19f),
                tip = new Vector3(0f, 0f, 22.5f),
            },
        };

        /// <summary>
        /// Builds a prefab per <see cref="Wraps"/> entry:
        /// <code>
        /// {name}            identity — what AttachProp parents to the hand
        ///   Mesh            the source model, rotated, scaled, shifted so the grip is at origin
        ///   PrimaryGrip     origin
        ///   BladeRoot       the guard
        ///   TrailOrigin     mid-blade; AttachProp puts the slash trail here
        ///   HitPoint        the tip, for VFX and for reading the reach in the editor
        /// </code>
        /// The blade points along +Y, so a wrapper drops into the same
        /// GripAnchor as a KayKit prop with no per-rig special casing.
        /// </summary>
        [MenuItem("Emberline/Build Weapon Wrappers")]
        public static void BuildWrappers()
        {
            Directory.CreateDirectory(PrefabDir);
            var made = 0;
            foreach (var w in Wraps)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(w.fbx);
                if (src == null) { Debug.LogWarning($"[Weapons] missing {w.fbx}"); continue; }

                var rot = Quaternion.Euler(w.rotEuler);
                Vector3 P(Vector3 meshPoint) => rot * ((meshPoint - w.grip) * w.scale);

                var root = new GameObject(w.name);
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(mesh, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                mesh.name = "Mesh";
                mesh.transform.SetParent(root.transform, false);
                mesh.transform.localRotation = rot;
                mesh.transform.localScale = Vector3.one * w.scale;
                mesh.transform.localPosition = P(Vector3.zero);
                foreach (var c in mesh.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

                Mark(root.transform, "PrimaryGrip", Vector3.zero);
                Mark(root.transform, "BladeRoot", P(w.guard));
                Mark(root.transform, "TrailOrigin", P(Vector3.Lerp(w.guard, w.tip, 0.5f)));
                Mark(root.transform, "HitPoint", P(w.tip));

                var path = $"{PrefabDir}/{w.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                made++;
                Debug.Log($"[Weapons] wrapper {w.name}: tip at y={P(w.tip).y:F2}, pommel side at " +
                          $"y={(rot * ((new Vector3(0, 0, -35.6f) - w.grip) * w.scale)).y:F2}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Weapons] {made} wrappers in {PrefabDir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Mark(Transform parent, string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
        }

        /// <summary>
        /// Renders Renzo holding each weapon from the side of his right hand, so a
        /// blade shows its profile. The stock weapon snapshot looks along the
        /// blade axis, which turns a katana into a two-pixel dot while an axe head
        /// still reads. Run WITHOUT -nographics.
        /// </summary>
        [MenuItem("Emberline/Snapshot Weapons In Hand")]
        public static void SnapshotInHand()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            var renzo = GameObject.Find("Renzo");
            var combat = renzo.GetComponent<Player.CombatController>();
            var swap = typeof(Player.CombatController).GetMethod("SwapHandProps",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Directory.CreateDirectory("Logs");

            foreach (var w in Resources.LoadAll<Core.WeaponDef>("Weapons"))
            {
                swap.Invoke(combat, new object[] { w });
                Transform hand = null;
                foreach (var t in renzo.GetComponentsInChildren<Transform>(true))
                    if (t.name == "GripAnchor_r") { hand = t; break; }
                if (hand == null) continue;

                // Side view of the right hand: camera out along +X, looking at the
                // hand, far enough to frame a 1.2 m blade plus the forearm.
                var look = hand.position;
                Shot(look + new Vector3(2.2f, 0.15f, 0f), look, $"Logs/inhand_{w.id}_side.png", 1200, 700, 1.1f);
                // Front-quarter, to check the grip sits in the palm and not the wrist.
                Shot(look + new Vector3(0.9f, 0.5f, 1.3f), look, $"Logs/inhand_{w.id}_quarter.png", 900, 700, 0.55f);
            }
            Debug.Log("[Weapons] in-hand snapshots written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Shot(Vector3 pos, Vector3 look, string file, int w, int h, float ortho)
        {
            var camGo = new GameObject("SnapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = ortho;
            cam.transform.position = pos; cam.transform.LookAt(look);
            cam.clearFlags = CameraClearFlags.Skybox;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
        }
    }
}
