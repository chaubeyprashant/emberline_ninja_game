using System.IO;
using Emberline.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Builds the open mission zone: one real place, not an arena.
    ///
    /// <para>
    /// The old arenas were a 130 m cube deck ringed by four cube parapets, which
    /// is the rectangular cage the player could see and feel. This scene has no
    /// parapets and no flat deck. Containment comes from the mountain ring that
    /// <see cref="EmberTerrain"/> raises around the bowl, so the edge of the world
    /// is a place rather than a wall.
    /// </para>
    ///
    /// <para>
    /// Layout, north at +Z:
    /// mountains ring everything; forest fills the north and south bands; a road
    /// runs from the north gate through the village at the origin and on to the
    /// enemy camp on its plateau in the south-east; the river runs down the west
    /// side and is crossed at one bridge.
    /// </para>
    /// </summary>
    public static class EmberZone
    {
        public const string ScenePath = "Assets/Scenes/Zone.unity";
        private const string MeshDir = "Assets/Art/Environments/Zone/Meshes";

        [MenuItem("Emberline/Build Zone")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var root = new GameObject("Zone").transform;

            EmberTerrain.Build(root, MeshDir);
            BuildWater(root);
            EmberZoneDressing.BuildAll(root);
            BuildLighting();

            // The same Renzo and the same camera rig the arenas use, so what is
            // being tested here is the environment and nothing else.
            var player = EmberlineBootstrap.BuildPlayer();
            var boot = player.AddComponent<Emberline.Core.ZoneBoot>();
            player.transform.position = new Vector3(boot.spawn.x,
                EmberTerrain.HeightAt(boot.spawn.x, boot.spawn.z) + 0.2f, boot.spawn.z);
            EmberlineBootstrap.BuildCameraFor(player.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var tris = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

            Debug.Log($"[Zone] built {ScenePath} — {tris} triangles");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------- water

        /// <summary>
        /// The river surface: one flat quad spanning the zone at the water line.
        /// The channel itself is carved into the terrain, so all this has to do is
        /// fill it. Kept unlit-ish and slightly translucent through the standard
        /// surface material rather than a bespoke water shader, because a real
        /// water shader is not worth its cost on an A33 for a boundary feature.
        /// </summary>
        private static void BuildWater(Transform parent)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mat_ZoneWater.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Surface"));
                AssetDatabase.CreateAsset(mat, "Assets/Prefabs/Mat_ZoneWater.mat");
            }
            mat.shader = Shader.Find("Emberline/Surface");
            mat.SetColor("_Color", new Color(0.10f, 0.17f, 0.22f));
            mat.SetFloat("_Smoothness", 0.85f);   // the one shiny surface in the zone
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_WearStrength", 0f);
            mat.SetFloat("_RimStrength", 0.5f);
            mat.SetColor("_RimColor", new Color(0.45f, 0.6f, 0.75f));
            EditorUtility.SetDirty(mat);

            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "River";
            go.transform.SetParent(parent, false);
            // Unity's plane is 10x10 m at scale 1. Only the river corridor is
            // covered, not the whole zone: a zone-wide sheet of water is pure
            // overdraw everywhere it is hidden, and it hides bugs like a flooded
            // valley instead of showing them.
            go.transform.localScale = new Vector3(4.4f, 1f, EmberTerrain.Half / 5f);
            go.transform.position = new Vector3(EmberTerrain.RiverCentreX, EmberTerrain.WaterLevel, 0f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            // No collider: the player wades, and a plane collider across the whole
            // zone would sit invisibly above the ground everywhere else.
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        // ---------------------------------------------------------- lighting

        /// <summary>
        /// Same three-point rig and trilight ambient the arenas use, driven from
        /// the shared Forest theme so the zone cannot drift from the game's look.
        /// </summary>
        private static void BuildLighting()
        {
            var env = EnvThemes.Get(EnvThemeId.Forest);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = env.ambientSky;
            RenderSettings.ambientEquatorColor = env.ambientEquator;
            RenderSettings.ambientGroundColor = env.ambientGround;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = env.fogColor;
            // Thinner than an arena's: the whole point of the zone is that you can
            // see the mountain you are going to walk to.
            RenderSettings.fogDensity = env.fogDensity * 0.45f;

            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Sky_Zone.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, "Assets/Prefabs/Sky_Zone.mat");
            }
            sky.shader = Shader.Find("Skybox/Procedural");
            sky.SetFloat("_SunSize", 0f);
            sky.SetFloat("_AtmosphereThickness", 0.45f);
            sky.SetFloat("_Exposure", 0.55f);
            var tint = env.fogColor * 3.5f;
            sky.SetColor("_SkyTint", new Color(
                Mathf.Clamp01(tint.r), Mathf.Clamp01(tint.g), Mathf.Clamp01(tint.b)));
            sky.SetColor("_GroundColor", env.ambientGround * 2f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = env.keyLight;
            key.intensity = env.keyIntensity;
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(38f, 34f, 0f);

            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = env.fillLight;
            fill.intensity = env.keyIntensity * 0.35f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(26f, -140f, 0f);

            var back = new GameObject("BackLight").AddComponent<Light>();
            back.type = LightType.Directional;
            back.color = env.rimLight;
            back.intensity = env.keyIntensity * 0.28f;
            back.shadows = LightShadows.None;
            back.transform.rotation = Quaternion.Euler(8f, 190f, 0f);
        }

        // --------------------------------------------------------- snapshots

        /// <summary>
        /// Renders the zone from a few vantage points so the shape can be judged
        /// without opening the editor. Run WITHOUT -nographics.
        /// </summary>
        [MenuItem("Emberline/Snapshot Zone")]
        public static void Snapshot()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory("Logs");

            foreach (var (pos, rot, file) in new[]
            {
                // Straight down: reads as a map, shows the whole layout at once.
                (new Vector3(0f, 210f, 0f), Quaternion.Euler(90f, 0f, 0f), "Logs/zone_map.png"),
                // Player-height look south down the road toward the village.
                (new Vector3(-4f, 6f, 46f), Quaternion.Euler(8f, 178f, 0f), "Logs/zone_road.png"),
                // Standing off the camp, from the ridge a scout would use.
                (new Vector3(24f, 22f, -28f), Quaternion.Euler(16f, 140f, 0f), "Logs/zone_camp.png"),
                // The village square from above the rooftops.
                (new Vector3(-26f, 20f, -26f), Quaternion.Euler(22f, 42f, 0f), "Logs/zone_village.png"),
                // Across the river to the western mountains.
                (new Vector3(-30f, 8f, 0f), Quaternion.Euler(4f, -80f, 0f), "Logs/zone_river.png"),
            })
                Shoot(pos, rot, file, 1600, 900);

            Debug.Log("[Zone] snapshots written to Logs/");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Shoot(Vector3 pos, Quaternion rot, string file, int w, int h)
        {
            var camGo = new GameObject("SnapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = 60f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;

            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
