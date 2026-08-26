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
            string[] wanted = { "Emberline/Toon", "Emberline/Ghost", "Emberline/Glow" };
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
            gmGo.AddComponent<UI.TouchHud>();

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
                    pool.transform.position = new Vector3(
                        (float)(rng.NextDouble() * 22 - 11), 0.012f, (float)(rng.NextDouble() * 13 - 6.5));
                    pool.transform.localScale = new Vector3(
                        2.2f + (float)rng.NextDouble() * 1.6f, 0.01f, 1.4f + (float)rng.NextDouble() * 1.2f);
                    pool.GetComponent<Renderer>().sharedMaterial = poolMat;
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
                    reed.GetComponent<Renderer>().sharedMaterial = reedMat;
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

            var rig = player.AddComponent<NinjaRig>();
            rig.bodyColor = new Color(0.15f, 0.18f, 0.24f);
            rig.accentColor = new Color(1f, 0.42f, 0.29f);
            rig.hasSword = true;
            rig.hasScarf = true;
            rig.maskStripe = true;

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
            camGo.transform.position = target.position + new Vector3(0, 9f, -7.5f);
        }

        // ----------------------------------------------------- prefabs / data

        private static GameObject[] BuildEnemyPrefabs()
        {
            var prefabs = new GameObject[6];
            prefabs[(int)EnemyKind.Bandit] = EnemyPrefab(EnemyKind.Bandit, "Bandit",
                body: new Color(0.32f, 0.27f, 0.22f), accent: new Color(0.6f, 0.63f, 0.66f),
                scale: 1f, hp: 42, spd: 3.2f, range: 1.8f, dmg: 9, windup: 0.55f,
                sword: true, ghost: false);
            prefabs[(int)EnemyKind.Ranged] = EnemyPrefab(EnemyKind.Ranged, "RangedWeaver",
                body: new Color(0.2f, 0.26f, 0.38f), accent: new Color(0.5f, 0.7f, 0.77f),
                scale: 1f, hp: 30, spd: 2.7f, range: 8f, dmg: 10, windup: 0.7f,
                sword: false, ghost: false);
            prefabs[(int)EnemyKind.Chief] = EnemyPrefab(EnemyKind.Chief, "BanditChief",
                body: new Color(0.28f, 0.18f, 0.16f), accent: new Color(0.95f, 0.25f, 0.19f),
                scale: 1.45f, hp: 270, spd: 2.6f, range: 2.3f, dmg: 15, windup: 0.6f,
                sword: true, ghost: false);
            prefabs[(int)EnemyKind.Shade] = EnemyPrefab(EnemyKind.Shade, "Shade",
                body: new Color(0.16f, 0.19f, 0.26f), accent: new Color(0.35f, 0.42f, 0.52f),
                scale: 0.95f, hp: 26, spd: 4.8f, range: 1.7f, dmg: 12, windup: 0.35f,
                sword: false, ghost: true);
            prefabs[(int)EnemyKind.Kagachi] = EnemyPrefab(EnemyKind.Kagachi, "Kagachi",
                body: new Color(0.13f, 0.28f, 0.30f), accent: new Color(0.92f, 0.9f, 0.86f),
                scale: 1.2f, hp: 300, spd: 3.9f, range: 2f, dmg: 14, windup: 0.5f,
                sword: true, ghost: false);
            prefabs[(int)EnemyKind.Jin] = EnemyPrefab(EnemyKind.Jin, "Jin",
                body: new Color(0.20f, 0.23f, 0.38f), accent: new Color(0.75f, 0.82f, 0.95f),
                scale: 1.05f, hp: 240, spd: 4.6f, range: 2f, dmg: 12, windup: 0.42f,
                sword: true, ghost: false);
            return prefabs;
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
            PlayerSettings.bundleVersion = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_NAME") ?? "1.1.0";
            var codeStr = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_CODE");
            PlayerSettings.Android.bundleVersionCode = int.TryParse(codeStr, out var code) ? code : 1;
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
