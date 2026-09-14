using System.IO;
using Emberline.Core;
using Emberline.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Saves the story cast as runtime-loadable prefabs, so a mission cutscene can
    /// put a real character in front of Renzo.
    ///
    /// <para>
    /// Only the Opening scene ever had cast members, and nothing plays it. In the
    /// mission scenes <see cref="CastStandIn"/> had nothing to load, so every
    /// Father, Aiko, Goro and Kagachi was built at runtime from cubes, and
    /// speaker-only roles like SOLDIER never appeared at all. A player build
    /// cannot instantiate an FBX, so the characters have to exist as saved
    /// prefabs under Resources; this is where they are made.
    /// </para>
    /// </summary>
    public static class EmberCastPrefabs
    {
        public const string OutDir = "Assets/Resources/Prefabs/Cast";

        /// <summary>Story name → the character model that plays it.</summary>
        private static readonly (string cast, System.Func<EmberCharacterFactory.Spec> spec)[] Roles =
        {
            // Realistic Mixamo bodies, not the chibi KayKit story specs: those read
            // as cartoons beside Renzo and the rest of the cast.
            ("FATHER", EmberCharacterFactory.MixamoFather),
            ("AIKO", EmberCharacterFactory.MixamoAiko),
            ("MOTHER", EmberCharacterFactory.MixamoMother),
            ("GORO", EmberCharacterFactory.Goro),
            ("JIN", EmberCharacterFactory.Jin),
            ("KAGACHI", EmberCharacterFactory.Kagachi),
            ("KAGEHIRA", EmberCharacterFactory.EliteWarrior),
            // The Serpent's men: samurai and pike guards, an officer who outranks them.
            ("SOLDIER", EmberCharacterFactory.Samurai),
            ("OFFICER", EmberCharacterFactory.EliteWarrior),
            ("GUARD", EmberCharacterFactory.PikeGuard),
            ("PATROL", EmberCharacterFactory.PikeGuard),
            ("SEARCHER", EmberCharacterFactory.Bandit),
            ("RUNNER", EmberCharacterFactory.RogueNinja),
            ("VISITOR", EmberCharacterFactory.MixamoVisitor),
            // Chapter 2: the two companions, and the ruin's owner before he is a fight.
            ("SUZU", EmberCharacterFactory.MixamoSuzu),
            ("FUMI", EmberCharacterFactory.MixamoFumi),
            ("SCAVENGER KING", () => EmberCharacterFactory.NamedFoe("raiderleader")),
            // Chapter 3: the thing in the trees, and the last of the Three Blades.
            ("PALE SHADE", EmberCharacterFactory.Shade),
            ("BLADE", () => EmberCharacterFactory.NamedFoe("threeblades")),
            ("TSURU", EmberCharacterFactory.MixamoTsuru),
        };

        public static readonly string[] CastNames = System.Array.ConvertAll(Roles, r => r.cast);

        /// <summary>Build every cast prefab. Called by the scene setup.</summary>
        public static int Build()
        {
            Directory.CreateDirectory(OutDir);
            var made = 0;
            foreach (var (cast, spec) in Roles)
            {
                var root = new GameObject(cast);
                if (!EmberCharacterFactory.Build(root, spec()))
                {
                    Object.DestroyImmediate(root);
                    Debug.LogWarning($"[Emberline] cast {cast}: model missing, keeps the primitive stand-in");
                    continue;
                }
                root.AddComponent<CastMember>().castName = cast;
                PrefabUtility.SaveAsPrefabAsset(root, $"{OutDir}/{cast}.prefab");
                Object.DestroyImmediate(root);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Emberline] cast prefabs: {made}/{Roles.Length} under Resources/Prefabs/Cast");
            return made;
        }

        [MenuItem("Emberline/Build Cast Prefabs")]
        public static void BuildMenu()
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Every cast prefab in a row beside Renzo's height, so models and sizes
        /// can be checked from one image. Run WITHOUT -nographics.
        /// </summary>
        [MenuItem("Emberline/Snapshot Cast Prefabs")]
        public static void Snapshot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.5f);

            var x = 0f;
            foreach (var name in CastNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/{name}.prefab");
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetPositionAndRotation(new Vector3(x, 0f, 0f), Quaternion.Euler(0f, 180f, 0f));
                x += 1.3f;
            }

            const int W = 2400, H = 700;
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
            cam.fieldOfView = 20f;
            var mid = (x - 1.3f) * 0.5f;
            var dist = (x * 0.5f + 0.6f) / (Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * ((float)W / H));
            cam.transform.position = new Vector3(mid, 1.0f, -dist);
            cam.transform.LookAt(new Vector3(mid, 0.95f, 0f));

            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/cast_prefabs.png", tex.EncodeToPNG());
            Debug.Log($"[Emberline] cast snapshot: {string.Join(" ", CastNames)} → Logs/cast_prefabs.png");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
