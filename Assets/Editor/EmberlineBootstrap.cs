using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.Missions;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Batch-mode bootstrap: generates BOTH mission scenes (Rooftop + Marsh),
    /// rigged enemy prefabs, mission assets, toon-shaded arenas, and builds the
    /// Android APK — no manual editor work required.
    /// </summary>
    public static class EmberlineBootstrap
    {
        private const string RooftopScene = "Assets/Scenes/Rooftop.unity";
        private const string MarshScene = "Assets/Scenes/Marsh.unity";

        private enum Theme { Rooftop, Marsh }

        public static void SetupAndBuild()
        {
            SetupScenes();
            BuildAndroid();
        }

        [MenuItem("Emberline/Setup Scenes")]
        public static void SetupScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Missions");

            EnsureAlwaysIncludedShaders();
            EmberArtImport.ConfigureCharacterClips();
            BuildWeaponAssets();
            BuildKunaiPrefab();

            var prefabs = BuildEnemyPrefabs();
            var rooftop = RooftopMission();
            var serpent = SerpentMission();

            BuildScene(Theme.Rooftop, RooftopScene, rooftop, prefabs, "Marsh", "THE SERPENT'S TOLL");
            BuildScene(Theme.Marsh, MarshScene, serpent, prefabs, "Rooftop", "ROOFTOP RATS");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(RooftopScene, true),
                new EditorBuildSettingsScene(MarshScene, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Scenes generated: Rooftop + Marsh");
        }

        [MenuItem("Emberline/Build Android APK")]
        public static void BuildAndroid()
        {
            PlayerSettings.companyName = "Ergebins";
            PlayerSettings.productName = "Emberline 3D";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ergebins.emberline3d");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.Android.bundleVersionCode = 6; // device/store already saw code 5

            Directory.CreateDirectory("Builds");
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                new[] { RooftopScene, MarshScene }, "Builds/emberline3d.apk",
                BuildTarget.Android, BuildOptions.None);

            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[Emberline] Android build {(ok ? "SUCCEEDED" : "FAILED")}: " +
                      $"{report.summary.totalErrors} errors, {report.summary.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        // -------------------------------------------------------------- setup

        private static void EnsureAlwaysIncludedShaders()
        {
            string[] wanted = { "Emberline/Toon", "Emberline/Ghost", "Emberline/Glow", "Emberline/GlowTex" };
            var settings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in wanted)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("[Emberline] Missing shader " + name); continue; }
                var already = false;
                for (var i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) { already = true; break; }
                if (!already)
                {
                    arr.InsertArrayElementAtIndex(arr.arraySize);
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildScene(Theme theme, string path, MissionDef mission,
            GameObject[] prefabs, string otherScene, string otherLabel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting(theme);
            BuildArena(theme);
            var player = BuildPlayer();
            BuildCamera(player.transform, theme);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.mission = mission;
            gm.enemyPrefabs = prefabs;
            gm.arenaHalfExtents = new Vector2(13f, 8f);
            gm.otherSceneName = otherScene;
            gm.otherMissionLabel = otherLabel;
            gm.isMarshScene = theme == Theme.Marsh;
            gmGo.AddComponent<AttackTokenPool>();
            var hudGo = new GameObject("EmberHud");
            hudGo.AddComponent<UI.EmberHud>();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildLighting(Theme theme)
        {
            var night = theme == Theme.Rooftop;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = night
                ? new Color(0.19f, 0.22f, 0.30f)
                : new Color(0.18f, 0.24f, 0.21f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = night
                ? new Color(0.07f, 0.09f, 0.13f)
                : new Color(0.07f, 0.11f, 0.09f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 42f;

            // Night skybox (procedural, tinted per theme).
            var skyPath = $"Assets/Prefabs/Sky{theme}.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.shader = Shader.Find("Skybox/Procedural");
            sky.SetFloat("_SunSize", 0.0f);
            sky.SetFloat("_AtmosphereThickness", 0.45f);
            sky.SetFloat("_Exposure", night ? 0.45f : 0.4f);
            sky.SetColor("_SkyTint", night ? new Color(0.10f, 0.13f, 0.24f) : new Color(0.10f, 0.18f, 0.14f));
            sky.SetColor("_GroundColor", night ? new Color(0.06f, 0.08f, 0.12f) : new Color(0.06f, 0.10f, 0.08f));
            RenderSettings.skybox = sky;

            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = night ? new Color(0.62f, 0.7f, 0.85f) : new Color(0.6f, 0.75f, 0.68f);
            moon.intensity = 0.8f;
            moon.transform.rotation = Quaternion.Euler(55f, -140f, 0);
            moon.shadows = LightShadows.Hard; // soft shadows cost too much on mobile
        }

        /// <summary>Place a KayKit Dungeon prop with the shared toon atlas material.</summary>
        private static GameObject DungeonProp(string fbxName, Vector3 pos, float yaw,
            float scale = 1f, bool collider = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Art/Environments/Dungeon/{fbxName}.fbx");
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;

            var toon = Shader.Find("Emberline/Toon");
            var matPath = "Assets/Prefabs/Mat_DungeonAtlas.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(toon);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = toon;
            mat.color = Color.white;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Environments/Dungeon/dungeon_texture.png");
            if (tex != null) mat.mainTexture = tex;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            if (collider)
            {
                var bounds = new Bounds(pos, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(r.bounds);
                var box = go.AddComponent<BoxCollider>();
                box.center = go.transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size / Mathf.Max(0.001f, scale);
            }
            return go;
        }

        private static void BuildArena(Theme theme)
        {
            var night = theme == Theme.Rooftop;
            var deckCol = night ? new Color(0.15f, 0.18f, 0.23f) : new Color(0.13f, 0.18f, 0.15f);
            var trimCol = night ? new Color(0.2f, 0.24f, 0.3f) : new Color(0.17f, 0.23f, 0.19f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Deck";
            ground.transform.position = new Vector3(0, -0.25f, 0);
            ground.transform.localScale = new Vector3(27f, 0.5f, 17f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat($"Deck{theme}", deckCol);

            foreach (var (pos, scale) in new[]
            {
                (new Vector3(0, 0.4f, 8.6f), new Vector3(27f, 0.8f, 0.6f)),
                (new Vector3(0, 0.4f, -8.6f), new Vector3(27f, 0.8f, 0.6f)),
                (new Vector3(13.6f, 0.4f, 0), new Vector3(0.6f, 0.8f, 17f)),
                (new Vector3(-13.6f, 0.4f, 0), new Vector3(0.6f, 0.8f, 17f)),
            })
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Parapet";
                wall.transform.position = pos;
                wall.transform.localScale = scale;
                wall.GetComponent<Renderer>().sharedMaterial = Mat($"Parapet{theme}", trimCol);
            }

            var markers = new GameObject("ArenaMarkers").AddComponent<ArenaMarkers>();

            if (theme == Theme.Rooftop)
            {
                // Roof-tile ridge bars give the deck visual rhythm.
                var ridgeMat = Mat("Ridge", new Color(0.12f, 0.15f, 0.2f));
                for (var x = -12f; x <= 12f; x += 3f)
                {
                    var ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(ridge.GetComponent<Collider>());
                    ridge.name = "Ridge";
                    ridge.transform.position = new Vector3(x, 0.015f, 0);
                    ridge.transform.localScale = new Vector3(0.12f, 0.04f, 16.4f);
                    ridge.GetComponent<Renderer>().sharedMaterial = ridgeMat;
                }

                // Chimney clusters: real cover — they block archer bolts (ArenaMarkers),
                // steer melee AI around, and the player can vault their lips mid-Flicker.
                var brickMat = Mat("Chimney", new Color(0.21f, 0.17f, 0.19f));
                var capMat = Mat("ChimneyCap", new Color(0.13f, 0.11f, 0.13f));
                foreach (var (cx, cz) in new[] { (-6f, 3.2f), (5.5f, -3.4f), (0.5f, 0.6f) })
                {
                    var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "Chimney";
                    body.transform.position = new Vector3(cx, 0.8f, cz);
                    body.transform.localScale = new Vector3(1.5f, 1.6f, 1.5f);
                    body.GetComponent<Renderer>().sharedMaterial = brickMat;
                    var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(cap.GetComponent<Collider>());
                    cap.name = "ChimneyCap";
                    cap.transform.position = new Vector3(cx, 1.7f, cz);
                    cap.transform.localScale = new Vector3(1.8f, 0.22f, 1.8f);
                    cap.GetComponent<Renderer>().sharedMaterial = capMat;
                    markers.obstacles.Add(new Vector4(cx, 0, cz, 1.15f));
                }

                // KayKit props: crate cover, supply boxes, banners on the parapet.
                DungeonProp("crates_stacked", new Vector3(-9.5f, 0, -4.5f), 20f, 1.15f, collider: true);
                markers.obstacles.Add(new Vector4(-9.5f, 0, -4.5f, 1.1f));
                DungeonProp("box_large", new Vector3(9f, 0, 4.8f), -35f, 1.1f, collider: true);
                markers.obstacles.Add(new Vector4(9f, 0, 4.8f, 0.9f));
                DungeonProp("box_small", new Vector3(-5f, 0, -3.9f), 65f, 1f);
                DungeonProp("keg", new Vector3(0.6f, 0, 1.9f), 10f, 1f);
                DungeonProp("banner_red", new Vector3(-6f, 1.55f, 8.45f), 180f, 1.2f);
                DungeonProp("banner_thin_red", new Vector3(6f, 1.55f, 8.45f), 180f, 1.2f);
                DungeonProp("torch_lit", new Vector3(-12.9f, 0.8f, 0f), 90f, 1.3f);
                DungeonProp("torch_lit", new Vector3(12.9f, 0.8f, 0f), -90f, 1.3f);

                // Distant skyline silhouettes beyond the parapet — depth, no colliders.
                var skylineMat = Mat("Skyline", new Color(0.08f, 0.10f, 0.15f));
                var rng2 = new System.Random(11);
                for (var i = 0; i < 10; i++)
                {
                    var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(roof.GetComponent<Collider>());
                    roof.name = "SkylineRoof";
                    var side = i % 2 == 0 ? 1f : -1f;
                    roof.transform.position = new Vector3(
                        (float)(rng2.NextDouble() * 44 - 22),
                        (float)(rng2.NextDouble() * 2.5 - 2.5),
                        side * (12f + (float)rng2.NextDouble() * 8f));
                    roof.transform.localScale = new Vector3(
                        3.5f + (float)rng2.NextDouble() * 5f, 2.5f + (float)rng2.NextDouble() * 3f,
                        3f + (float)rng2.NextDouble() * 3f);
                    roof.transform.rotation = Quaternion.Euler(0, (float)rng2.NextDouble() * 20 - 10, 0);
                    var roofR = roof.GetComponent<Renderer>();
                    roofR.sharedMaterial = skylineMat;
                    roofR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            else
            {
                // Still-water pools + reed clumps for the Ashfen crossing.
                var poolMat = Mat("MarshPool", new Color(0.16f, 0.26f, 0.24f));
                var reedMat = Mat("Reed", new Color(0.2f, 0.32f, 0.22f));
                var rng = new System.Random(7);
                for (var i = 0; i < 6; i++)
                {
                    var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(pool.GetComponent<Collider>());
                    pool.name = "Pool";
                    var px = (float)(rng.NextDouble() * 22 - 11);
                    var pz = (float)(rng.NextDouble() * 13 - 6.5);
                    var sx = 2.2f + (float)rng.NextDouble() * 1.6f;
                    pool.transform.position = new Vector3(px, 0.012f, pz);
                    pool.transform.localScale = new Vector3(sx, 0.01f, 1.4f + (float)rng.NextDouble() * 1.2f);
                    pool.GetComponent<Renderer>().sharedMaterial = poolMat;
                    // Knee-deep water: slows anyone wading through it.
                    markers.waters.Add(new Vector4(px, 0, pz, sx * 0.5f));
                }
                for (var i = 0; i < 14; i++)
                {
                    var reed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(reed.GetComponent<Collider>());
                    reed.name = "Reed";
                    var x = (float)(rng.NextDouble() * 25 - 12.5);
                    var z = (float)(rng.NextDouble() < 0.5 ? -7.6 + rng.NextDouble() * 1.5 : 6.1 + rng.NextDouble() * 1.5);
                    reed.transform.position = new Vector3(x, 0.45f, z);
                    reed.transform.localScale = new Vector3(0.06f, 0.9f, 0.06f);
                    reed.transform.rotation = Quaternion.Euler(
                        (float)(rng.NextDouble() * 10 - 5), 0, (float)(rng.NextDouble() * 10 - 5));
                    var reedR = reed.GetComponent<Renderer>();
                    reedR.sharedMaterial = reedMat;
                    reedR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // Reed clumps are where shades come from.
                    if (i % 2 == 0) markers.shadeSpawns.Add(new Vector3(x, 0, Mathf.Clamp(z, -7.4f, 7.4f)));
                }

                // Sunken merchant carts — environmental storytelling from Act II.
                var cartMat = Mat("Cart", new Color(0.23f, 0.18f, 0.13f));
                var wheelMat = Mat("CartWheel", new Color(0.15f, 0.12f, 0.09f));
                foreach (var (cx, cz, angle) in new[] { (-7f, 2.5f, 24f), (6.5f, -2f, -18f) })
                {
                    var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bed.name = "SunkenCart";
                    bed.transform.position = new Vector3(cx, 0.28f, cz);
                    bed.transform.localScale = new Vector3(2.4f, 0.5f, 1.3f);
                    bed.transform.rotation = Quaternion.Euler(-7f, angle, 6f);
                    bed.GetComponent<Renderer>().sharedMaterial = cartMat;
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(wheel.GetComponent<Collider>());
                    wheel.name = "CartWheel";
                    wheel.transform.position = new Vector3(cx + 1.1f, 0.5f, cz + 0.6f);
                    wheel.transform.localScale = new Vector3(1.1f, 0.06f, 1.1f);
                    wheel.transform.rotation = Quaternion.Euler(80f, angle, 0);
                    wheel.GetComponent<Renderer>().sharedMaterial = wheelMat;
                    markers.obstacles.Add(new Vector4(cx, 0, cz, 1.5f));
                }

                // KayKit props: drowned cargo — barrels, a chest, toppled ruins.
                DungeonProp("barrel_large", new Vector3(-6.2f, -0.18f, 3.1f), 30f, 1.1f, collider: true);
                DungeonProp("barrel_small", new Vector3(-5.3f, 0, 2.2f), 70f, 1f);
                DungeonProp("barrel_small", new Vector3(7.4f, -0.1f, -2.8f), -15f, 1f);
                DungeonProp("chest", new Vector3(6.0f, 0, -1.6f), -140f, 1.05f);
                DungeonProp("rubble_large", new Vector3(-10.5f, 0, -5f), 45f, 1.3f, collider: true);
                markers.obstacles.Add(new Vector4(-10.5f, 0, -5f, 1.3f));
                DungeonProp("rubble_half", new Vector3(10.8f, 0, 5.4f), -70f, 1.2f);
                DungeonProp("column", new Vector3(11.6f, 0, -6.2f), 15f, 1.1f, collider: true);
                markers.obstacles.Add(new Vector4(11.6f, 0, -6.2f, 0.8f));

                // Ghost lanterns drifting over the reeds (emissive quads, no lights).
                var ghostGlow = Mat("GhostLantern", new Color(0.5f, 0.95f, 0.75f));
                for (var i = 0; i < 5; i++)
                {
                    var wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.DestroyImmediate(wisp.GetComponent<Collider>());
                    wisp.name = "GhostLantern";
                    wisp.transform.position = new Vector3(
                        (float)(rng.NextDouble() * 22 - 11), 1.6f + (float)rng.NextDouble(),
                        (float)(rng.NextDouble() < 0.5 ? -7.9 : 7.9));
                    wisp.transform.localScale = Vector3.one * 0.22f;
                    var wispR = wisp.GetComponent<Renderer>();
                    wispR.sharedMaterial = ghostGlow;
                    wispR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // Corner lanterns: warm (rooftop) or cool (marsh) glow.
            var flame = night ? new Color(1f, 0.62f, 0.35f) : new Color(0.55f, 0.9f, 0.75f);
            var lightCol = night ? new Color(1f, 0.55f, 0.3f) : new Color(0.45f, 0.85f, 0.65f);
            foreach (var corner in new[]
            {
                new Vector3(12f, 0, 7f), new Vector3(-12f, 0, 7f),
                new Vector3(12f, 0, -7f), new Vector3(-12f, 0, -7f),
            })
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "LanternPost";
                post.transform.position = corner + Vector3.up * 0.9f;
                post.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
                post.GetComponent<Renderer>().sharedMaterial = Mat($"Post{theme}", trimCol);

                var bulb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(bulb.GetComponent<Collider>());
                bulb.name = "Lantern";
                bulb.transform.position = corner + Vector3.up * 1.9f;
                bulb.transform.localScale = Vector3.one * 0.35f;
                bulb.GetComponent<Renderer>().sharedMaterial = Mat($"Lantern{theme}", flame);

                var light = new GameObject("LanternLight").AddComponent<Light>();
                light.transform.position = corner + Vector3.up * 2.1f;
                light.type = LightType.Point;
                light.color = lightCol;
                light.intensity = 2.2f;
                light.range = 9f;

                // Destructible: break for a health pickup at the cost of the light.
                var postComp = post.AddComponent<LanternPost>();
                postComp.bulb = bulb;
                postComp.glow = light;
            }
        }

        private static GameObject BuildPlayer()
        {
            var player = new GameObject("Renzo");
            player.transform.position = new Vector3(0, 0, -3f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.95f, 0);
            cc.radius = 0.35f;

            // Skeletal Renzo (KayKit RogueHooded); NinjaRig primitives as fallback.
            if (!EmberCharacterFactory.Build(player, EmberCharacterFactory.Renzo()))
            {
                var rig = player.AddComponent<NinjaRig>();
                rig.bodyColor = new Color(0.15f, 0.18f, 0.24f);
                rig.accentColor = new Color(1f, 0.42f, 0.29f);
                rig.hasSword = true;
                rig.hasScarf = true;
                rig.maskStripe = true;
            }

            player.AddComponent<Health>();
            player.AddComponent<SenGates>();
            player.AddComponent<Player.PlayerLocomotion>();
            player.AddComponent<Player.CombatController>();
            return player;
        }

        private static void BuildCamera(Transform target, Theme theme)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.1f);
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.SetTarget(target);
            camGo.transform.position = target.position + new Vector3(0, 8.2f, -6.9f);
        }

        /// <summary>
        /// Bakes the KayKit dagger into a Resources prefab so the runtime kunai
        /// pool can load a real blade model. Root +Z = blade point (flight axis).
        /// </summary>
        private static void BuildKunaiPrefab()
        {
            Directory.CreateDirectory("Assets/Resources/Props");
            var dagger = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Characters/Props/dagger.fbx");
            if (dagger == null)
            {
                Debug.LogWarning("[Emberline] dagger.fbx missing — kunai keeps primitive look");
                return;
            }
            var root = new GameObject("Kunai");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(dagger);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(90f, 0, 0); // +Y blade → +Z

            // KayKit props are sized for scaled hand slots — normalize to a real
            // kunai (~0.55m along the flight axis) and center it on the root.
            Bounds B()
            {
                var b = new Bounds(root.transform.position, Vector3.zero);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    b.Encapsulate(r.bounds);
                return b;
            }
            var raw = B();
            var maxDim = Mathf.Max(raw.size.x, Mathf.Max(raw.size.y, raw.size.z));
            if (maxDim > 0.01f) model.transform.localScale = Vector3.one * (0.55f / maxDim);
            model.transform.localPosition -= B().center;

            var steel = Mat("KunaiSteel", new Color(0.62f, 0.68f, 0.78f));
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = steel;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Props/Kunai.prefab");
            Object.DestroyImmediate(root);
        }

        /// <summary>The three blades as ScriptableObjects under Resources/Weapons.</summary>
        private static void BuildWeaponAssets()
        {
            Directory.CreateDirectory("Assets/Resources/Weapons");

            WeaponDef W(string file)
            {
                var path = $"Assets/Resources/Weapons/{file}.asset";
                var w = AssetDatabase.LoadAssetAtPath<WeaponDef>(path);
                if (w == null)
                {
                    w = ScriptableObject.CreateInstance<WeaponDef>();
                    AssetDatabase.CreateAsset(w, path);
                }
                return w;
            }

            var katana = W("EmberKatana");
            katana.id = "katana";
            katana.displayName = "EMBER KATANA";
            katana.blurb = "Balanced. The blade that guards the lantern.";
            katana.unlockLevel = 0;
            katana.strikeDamage = new[] { 10f, 12f, 18f };
            katana.strikeRange = 2.8f;
            katana.chainWindow = 0.75f;
            katana.strikeAnimTime = 0.28f;
            katana.cleaveDamage = 26f;
            katana.cleaveCooldown = 1.4f;
            katana.trailColor = new Color(0.85f, 0.93f, 1f);
            EditorUtility.SetDirty(katana);

            var tanto = W("StormTanto");
            tanto.id = "tanto";
            tanto.displayName = "STORM TANTO";
            tanto.blurb = "Fast and close. Perfect dodges become parries.";
            tanto.unlockLevel = 4;
            tanto.strikeDamage = new[] { 7f, 8f, 13f };
            tanto.strikeRange = 2.2f;
            tanto.chainWindow = 0.9f;
            tanto.strikeAnimTime = 0.2f;
            tanto.lungeSpeed = 6.5f;
            tanto.cleaveDamage = 20f;
            tanto.cleaveCooldown = 1.1f;
            tanto.cleaveWindup = 0.18f;
            tanto.parryOnPerfectDodge = true;
            tanto.trailColor = new Color(0.7f, 0.82f, 1f);
            EditorUtility.SetDirty(tanto);

            var hook = W("MarshHook");
            hook.id = "hook";
            hook.displayName = "MARSH HOOK";
            hook.blurb = "Slow and cruel. The third strike drags them in; cleave poisons the ground.";
            hook.unlockLevel = 7;
            hook.strikeDamage = new[] { 12f, 14f, 22f };
            hook.strikeRange = 3.1f;
            hook.chainWindow = 0.62f;
            hook.strikeAnimTime = 0.36f;
            hook.lungeSpeed = 4.5f;
            hook.cleaveDamage = 30f;
            hook.cleaveCooldown = 1.8f;
            hook.cleaveWindup = 0.32f;
            hook.pullOnThirdHit = true;
            hook.poisonCleave = true;
            hook.trailColor = new Color(0.5f, 0.95f, 0.55f);
            EditorUtility.SetDirty(hook);
        }

        // ----------------------------------------------------- prefabs / data

        private static GameObject[] BuildEnemyPrefabs()
        {
            var prefabs = new GameObject[6];
            // Skeletal characters (KayKit) for the converted roster…
            prefabs[(int)EnemyKind.Bandit] = SkeletalEnemyPrefab(EnemyKind.Bandit, "Bandit",
                EmberCharacterFactory.Bandit(),
                hp: 42, spd: 3.2f, range: 1.8f, dmg: 9, windup: 0.55f, spawn: 1.1f);
            prefabs[(int)EnemyKind.Chief] = SkeletalEnemyPrefab(EnemyKind.Chief, "BanditChief",
                EmberCharacterFactory.Goro(),
                hp: 270, spd: 2.6f, range: 2.3f, dmg: 15, windup: 0.6f, spawn: 1.2f);
            prefabs[(int)EnemyKind.Shade] = SkeletalEnemyPrefab(EnemyKind.Shade, "Shade",
                EmberCharacterFactory.Shade(),
                hp: 26, spd: 4.8f, range: 1.7f, dmg: 12, windup: 0.35f, spawn: 1.0f);
            prefabs[(int)EnemyKind.Ranged] = SkeletalEnemyPrefab(EnemyKind.Ranged, "RangedWeaver",
                EmberCharacterFactory.Archer(),
                hp: 30, spd: 2.7f, range: 8f, dmg: 10, windup: 0.7f, spawn: 1.0f);
            prefabs[(int)EnemyKind.Kagachi] = SkeletalEnemyPrefab(EnemyKind.Kagachi, "Kagachi",
                EmberCharacterFactory.Kagachi(),
                hp: 300, spd: 3.9f, range: 2f, dmg: 14, windup: 0.5f, spawn: 1.8f);
            prefabs[(int)EnemyKind.Jin] = SkeletalEnemyPrefab(EnemyKind.Jin, "Jin",
                EmberCharacterFactory.Jin(),
                hp: 240, spd: 4.6f, range: 2f, dmg: 12, windup: 0.42f, spawn: 1.1f);
            return prefabs;
        }

        private static GameObject SkeletalEnemyPrefab(EnemyKind kind, string name,
            EmberCharacterFactory.Spec spec, float hp, float spd, float range,
            float dmg, float windup, float spawn)
        {
            var root = new GameObject(name);
            if (!EmberCharacterFactory.Build(root, spec))
            {
                Object.DestroyImmediate(root);
                // Model missing — keep the old primitive prefab path alive.
                return EnemyPrefab(kind, name,
                    body: new Color(0.32f, 0.27f, 0.22f), accent: new Color(0.6f, 0.63f, 0.66f),
                    scale: 1f, hp: hp, spd: spd, range: range, dmg: dmg, windup: windup,
                    sword: true, ghost: false);
            }

            var brain = root.AddComponent<EnemyBrain>();
            brain.kind = kind;
            brain.maxHp = hp;
            brain.speed = spd;
            brain.attackRange = range;
            brain.damage = dmg;
            brain.windupTime = windup;
            brain.spawnTime = spawn;
            brain.arenaHalfExtents = new Vector2(13f, 8f);

            var path = $"Assets/Prefabs/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnemyPrefab(EnemyKind kind, string name, Color body, Color accent,
            float scale, float hp, float spd, float range, float dmg, float windup,
            bool sword, bool ghost)
        {
            var root = new GameObject(name);

            var rig = root.AddComponent<NinjaRig>();
            rig.bodyColor = body;
            rig.accentColor = accent;
            rig.rigScale = scale;
            rig.hasSword = sword;
            rig.hasScarf = false;
            rig.maskStripe = true;
            rig.ghost = ghost;

            var brain = root.AddComponent<EnemyBrain>();
            brain.kind = kind;
            brain.maxHp = hp;
            brain.speed = spd;
            brain.attackRange = range;
            brain.damage = dmg;
            brain.windupTime = windup;
            brain.arenaHalfExtents = new Vector2(13f, 8f);

            var path = $"Assets/Prefabs/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static MissionDef RooftopMission()
        {
            var m = MissionAsset("RooftopRats", 1, "ROOFTOP RATS",
                "Bandits raid the Yorune terraces at nightfall.");
            m.waves = new[]
            {
                Wave("ROOFTOP RATS", EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Bandit),
                Wave("EYES IN THE MIST", EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Bandit,
                    EnemyKind.Bandit, EnemyKind.Ranged, EnemyKind.Ranged),
                Wave("CORDS FOR HIRE", EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Bandit,
                    EnemyKind.Bandit, EnemyKind.Ranged, EnemyKind.Ranged, EnemyKind.Ranged),
                Wave("THE BANDIT CHIEF", EnemyKind.Chief, EnemyKind.Bandit, EnemyKind.Bandit,
                    EnemyKind.Bandit, EnemyKind.Ranged),
            };
            EditorUtility.SetDirty(m);
            return m;
        }

        private static MissionDef SerpentMission()
        {
            var m = MissionAsset("SerpentsToll", 2, "THE SERPENT'S TOLL",
                "Ashfen Marsh. Something is hunting the crossing.");
            m.waves = new[]
            {
                Wave("SHADES IN THE REEDS", EnemyKind.Shade, EnemyKind.Shade, EnemyKind.Shade, EnemyKind.Ranged),
                Wave("THE AMBUSH", EnemyKind.Bandit, EnemyKind.Bandit, EnemyKind.Bandit,
                    EnemyKind.Shade, EnemyKind.Shade, EnemyKind.Ranged, EnemyKind.Ranged),
                Wave("THE MARSH SERPENT", EnemyKind.Kagachi),
            };
            EditorUtility.SetDirty(m);
            return m;
        }

        private static MissionDef MissionAsset(string file, int id, string name, string subtitle)
        {
            var path = $"Assets/Missions/{file}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MissionDef>(path);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<MissionDef>();
                AssetDatabase.CreateAsset(m, path);
            }
            m.id = id;
            m.missionName = name;
            m.subtitle = subtitle;
            return m;
        }

        private static MissionDef.Wave Wave(string title, params EnemyKind[] enemies) =>
            new() { title = title, enemies = enemies };


        // ------------------------------------------------------- Play Store

        public static void SetupAndBuildAab()
        {
            SetupScenes();
            BuildPlayStoreAab();
        }

        [MenuItem("Emberline/Build Play Store AAB")]
        public static void BuildPlayStoreAab()
        {
            ApplyReleaseIdentity();
            // Unity 6 batch builds reject scripted keystore passwords; the AAB is
            // built debug-signed here and re-signed with jarsigner in release.sh.
            PlayerSettings.Android.useCustomKeystore = false;

            EditorUserBuildSettings.buildAppBundle = true;
            Directory.CreateDirectory("Builds");
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                new[] { RooftopScene, MarshScene }, "Builds/emberline.aab",
                BuildTarget.Android, BuildOptions.None);
            EditorUserBuildSettings.buildAppBundle = false;

            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[Emberline] AAB build {(ok ? "SUCCEEDED" : "FAILED")}: {report.summary.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void ApplyReleaseIdentity()
        {
            PlayerSettings.companyName = "Ergebins Technologies";
            PlayerSettings.productName = "Emberline";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ergebins.emberline3d");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.bundleVersion = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_NAME") ?? "1.0.1";
            var codeStr = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_CODE");
            PlayerSettings.Android.bundleVersionCode = int.TryParse(codeStr, out var code) ? code : 6;
            EnsureIcon();
        }

        private static void ApplySigningFromEnv()
        {
            var propsPath = System.Environment.GetEnvironmentVariable("EMBERLINE_SIGNING");
            if (string.IsNullOrEmpty(propsPath) || !File.Exists(propsPath))
            {
                Debug.LogWarning("[Emberline] No EMBERLINE_SIGNING properties — building debug-signed.");
                return;
            }
            string keystore = null, alias = null, storepass = null, keypass = null;
            foreach (var line in File.ReadAllLines(propsPath))
            {
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;
                var key = line.Substring(0, idx).Trim();
                var val = line.Substring(idx + 1).Trim();
                switch (key)
                {
                    case "keystore": keystore = val; break;
                    case "alias": alias = val; break;
                    case "storepass": storepass = val; break;
                    case "keypass": keypass = val; break;
                }
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storepass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keypass;
            Debug.Log("[Emberline] Release signing configured from " + propsPath);
        }

        /// <summary>Procedural app icon: ember Gate diamond on ink, generated in code.</summary>
        private static void EnsureIcon()
        {
            const string path = "Assets/Icon.png";
            if (!File.Exists(path))
            {
                const int S = 512;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var ink = new Color(0.075f, 0.09f, 0.13f);
                var inkTop = new Color(0.10f, 0.125f, 0.18f);
                var ember = new Color(1f, 0.42f, 0.29f);
                var pale = new Color(0.92f, 0.9f, 0.86f);
                var c = S / 2f;
                for (var y = 0; y < S; y++)
                for (var x = 0; x < S; x++)
                {
                    var col = Color.Lerp(ink, inkTop, y / (float)S);
                    var man = Mathf.Abs(x - c) + Mathf.Abs(y - c);       // diamond metric
                    var rad = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    // warm glow
                    var glow = Mathf.Pow(Mathf.Clamp01(1f - rad / (S * 0.45f)), 2.2f) * 0.55f;
                    col = Color.Lerp(col, ember, glow * 0.5f);
                    // diamond body with pale outline
                    if (man < S * 0.30f) col = Color.Lerp(ember, new Color(1f, 0.62f, 0.4f), 1f - man / (S * 0.30f));
                    else if (man < S * 0.315f) col = pale;
                    // crack line through the gate
                    var crack = Mathf.Abs((x - c) * 0.85f - (y - c));
                    if (man < S * 0.29f && crack < S * 0.012f) col = ink;
                    tex.SetPixel(x, S - 1 - y, col);
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        private static Material Mat(string name, Color color)
        {
            var toon = Shader.Find("Emberline/Toon") ?? Shader.Find("Standard");
            var path = $"Assets/Prefabs/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(toon);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = toon;
            mat.color = color;
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0.02f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
