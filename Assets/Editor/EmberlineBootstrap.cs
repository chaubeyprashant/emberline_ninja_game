using System.Collections.Generic;
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
        private const string OpeningScene = "Assets/Scenes/Opening.unity";

        /// <summary>
        /// The shipped scene list, in load order. Both build entry points read this
        /// rather than repeating a literal array: they previously hardcoded
        /// {Rooftop, Marsh}, so a scene added to EditorBuildSettings was registered
        /// in the editor and silently missing from every APK.
        /// </summary>
        private static readonly string[] ShippedScenes =
            { OpeningScene, RooftopScene, MarshScene };

        private enum Theme { Rooftop, Marsh }

        public static void SetupAndBuild()
        {
            SetupScenes();
            BuildAndroid();
        }

        [MenuItem("Emberline/Setup Scenes")]
        public static void SetupScenes()
        {
            ConfigureAndroidPlayerSettings();

            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Missions");

            EnsureAlwaysIncludedShaders();
            ConfigureTextureBudgets();
            EmberArtImport.ConfigureCharacterClips();
            BuildWeaponAssets();
            BuildKunaiPrefab();
            EmberDressing.Build();   // runtime props for mission set dressing
            EmberCombatAssets.BuildPlayerMovesets();

            var prefabs = BuildEnemyPrefabs();
            var rooftop = RooftopMission();
            var serpent = SerpentMission();

            BuildScene(Theme.Rooftop, RooftopScene, rooftop, prefabs, "Marsh", "THE SERPENT'S TOLL");
            BuildScene(Theme.Marsh, MarshScene, serpent, prefabs, "Rooftop", "ROOFTOP RATS");

            BuildOpeningScene();

            // Opening first: a fresh install boots into the cinematic, which hands
            // straight to the first mission when it ends.
            EditorBuildSettings.scenes = System.Array.ConvertAll(
                ShippedScenes, p => new EditorBuildSettingsScene(p, true));
            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Scenes generated: Rooftop + Marsh");
        }

        /// <summary>
        /// Android player settings that define what ships. Called by SetupScenes as
        /// well as by the build entry points, so the project on disk is always in
        /// the shipping configuration rather than only after a build has run.
        /// </summary>
        private static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "Ergebins Technologies";
            PlayerSettings.productName = "Emberline";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ergebins.emberline3d");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // Pinned, not Automatic: Automatic takes the highest installed platform,
            // which here is a preview (android-37.0). 36 is installed and meets the
            // current Play Store target-API requirement.
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            // Seven skinned characters were skinning on the CPU every frame; the GPU
            // does it for free and it is the single cheapest win available here.
            PlayerSettings.gpuSkinning = true;
        }

        [MenuItem("Emberline/Build Android APK")]
        public static void BuildAndroid()
        {
            ConfigureAndroidPlayerSettings();
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Bumped for the traversal / combat-depth / mission-type work; a store
            // upload must always exceed the highest code already published.
            PlayerSettings.bundleVersion = "1.2.0";
            PlayerSettings.Android.bundleVersionCode = 8;

            Directory.CreateDirectory("Builds");
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                ShippedScenes, "Builds/emberline3d.apk",
                BuildTarget.Android, BuildOptions.None);

            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[Emberline] Android build {(ok ? "SUCCEEDED" : "FAILED")}: " +
                      $"{report.summary.totalErrors} errors, {report.summary.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        // -------------------------------------------------------------- setup

        private static void EnsureAlwaysIncludedShaders()
        {
            string[] wanted = { "Emberline/Surface", "Emberline/Grade", "Emberline/Toon",
                "Emberline/Ghost", "Emberline/Glow", "Emberline/GlowTex" };
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

            // Environment theme: light, fog, palette, weather and ambient life come
            // from the theme table rather than from this builder, so a new place
            // is a row in EnvThemes, not another branch here.
            var themeId = theme == Theme.Marsh ? EnvThemeId.Graveyard : EnvThemeId.Village;
            var env = EnvThemes.Get(themeId);

            BuildLighting(theme, env);
            BuildArena(theme);
            var player = BuildPlayer();
            BuildCamera(player.transform, theme);

            var atmoGo = new GameObject("AtmosphereSpawner");
            var spawner = atmoGo.AddComponent<UI.AtmosphereSpawner>();
            spawner.themeId = themeId;

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.mission = mission;
            gm.enemyPrefabs = prefabs;
            var named = BuildNamedFoePrefabs(prefabs);
            gm.namedVisualIds = named.ids;
            gm.namedVisualPrefabs = named.prefabs;
            gm.arenaHalfExtents = new Vector2(13f, 8f);
            gm.otherSceneName = otherScene;
            gm.otherMissionLabel = otherLabel;
            gm.isMarshScene = theme == Theme.Marsh;
            gmGo.AddComponent<AttackTokenPool>();
            // Group-level combat direction: decides who attacks, who circles and
            // who waits, so a pack does not all commit at once.
            gmGo.AddComponent<SquadCoordinator>();
            var hudGo = new GameObject("EmberHud");
            hudGo.AddComponent<UI.EmberHud>();
            // Still of the arena behind opaque menus; lets the camera switch off.
            hudGo.AddComponent<UI.MenuBackdrop>();
            // Dev perf readout: own object so its canvas is independent of the
            // HUD's. Hidden unless toggled (F3 / four-finger tap).
            new GameObject("PerfOverlay").AddComponent<UI.PerfOverlay>();

            MarkArenaStatic(player);
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>
        /// Flags the arena's fixed geometry as batching-static. The arenas are
        /// generated in code and nothing was ever marked static, so Unity's static
        /// batcher had nothing to work with and every wall, ridge, chimney and prop
        /// issued its own draw call. The player and anything under it are excluded —
        /// they move, and a moving static-batched renderer is a correctness bug.
        /// </summary>
        private static void MarkArenaStatic(GameObject player)
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
                        | StaticEditorFlags.OccludeeStatic;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var go = r.gameObject;
                if (player != null && go.transform.IsChildOf(player.transform)) continue;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
            }
        }

        /// <summary>
        /// Import budgets for the art, applied here so the pipeline stays
        /// reproducible rather than depending on hand-set importer values.
        ///
        /// Character/prop atlases go 1024 → 512 (4× less texture memory). They are
        /// NOT flat palettes — measured at ~2.7-3.9k unique colours with real shading
        /// baked in, so 256 visibly degraded them; 512 is the safe rung. The Kenney
        /// VFX sheets are soft radial blobs with no fine detail and hold up at 256.
        /// </summary>
        private static void ConfigureTextureBudgets()
        {
            void Budget(string path, int maxSize)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) return;
                var android = importer.GetPlatformTextureSettings("Android");
                var needs = importer.maxTextureSize != maxSize
                            || !android.overridden
                            || android.maxTextureSize != maxSize;
                if (!needs) return;
                importer.maxTextureSize = maxSize;
                // Mips stay on for world art — characters and props are viewed at
                // range and alias badly without them. Only the always-camera-facing
                // particle sheets can safely drop the chain.
                importer.mipmapEnabled = !path.Contains("/VFX/");
                android.overridden = true;
                android.maxTextureSize = maxSize;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]
                     {
                         "Assets/Art/Characters", "Assets/Art/Environments",
                         "Assets/Resources/Art/VFX",
                     }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Soft particle blobs tolerate 256; anything a character or prop
                // actually wears stays at 512.
                var size = path.Contains("/VFX/") ? 256 : 512;
                Budget(path, size);
            }
        }

        /// <summary>
        /// The mountain village: one set carrying scenes 1-3 as three dressing
        /// states. Built from the same primitive-and-KayKit vocabulary as the
        /// arenas, so it costs no new art and obeys the same surface shader.
        /// </summary>
        private static void BuildOpeningScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var env = EnvThemes.Get(EnvThemeId.VillageDawn);
            BuildLighting(Theme.Rooftop, env);

            var root = new GameObject("VillageSet");
            var set = root.AddComponent<Story.VillageSet>();

            // Ground: packed earth, wide enough that the wide shots never see an edge.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial =
                Mat("VillageGround", new Color(0.20f, 0.18f, 0.15f), Surface.Stone);

            // Ridge line. A mountain village needs mountains, and without them the
            // ground plane meets the sky in a hard straight line that reads as an
            // unfinished level. Eight low, dark, faceted shapes at distance: they
            // never move, never animate, and cost one draw call each.
            var ridgeMat = Mat("VillageRidge", new Color(0.20f, 0.21f, 0.23f), Surface.Stone);
            // Seven peaks on a 135m ring. The count and the width are chosen so the
            // total silhouette is narrower than the circle: at nine peaks 68m out
            // they overlapped into a solid wall around the village.
            for (var i = 0; i < 7; i++)
            {
                var a = i / 7f * Mathf.PI * 2f;
                var r = 132f + (i % 3) * 18f;
                var peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                peak.name = "Ridge";
                peak.transform.SetParent(root.transform);
                // Yaw only. Rotating a cube about Z tilts it in elevation and it
                // reads as a diamond hanging over the village; rotating in plan
                // turns a corner toward the camera, which reads as a peak.
                // Sunk below the ground so only the peak stands above the street.
                peak.transform.position = new Vector3(Mathf.Sin(a) * r, -10f, Mathf.Cos(a) * r);
                peak.transform.localScale = new Vector3(72f + i * 4f, 68f + (i % 4) * 16f, 72f);
                peak.transform.localRotation = Quaternion.Euler(0, 45f + i * 17f, 0);
                peak.GetComponent<Renderer>().sharedMaterial = ridgeMat;
            }

            var peace = new GameObject("Peace");
            peace.transform.SetParent(root.transform);
            var ruin = new GameObject("Ruin");
            ruin.transform.SetParent(root.transform);
            var fire = new GameObject("Fire");
            fire.transform.SetParent(root.transform);

            // Six houses in a loose street. Intact in Peace; their rubble twins
            // stand in the same footprints for Attack and Ruin, so the geometry
            // reads as the same village rather than a different place.
            var wall = Mat("VillageWall", new Color(0.30f, 0.26f, 0.21f), Surface.Wood);
            var roof = Mat("VillageRoof", new Color(0.17f, 0.16f, 0.16f), Surface.Stone);
            Vector3[] plots =
            {
                new(-8f, 0, 6f), new(8f, 0, 7f), new(-10f, 0, -3f),
                new(9f, 0, -4f), new(-3f, 0, 12f), new(4f, 0, -11f),
            };
            for (var i = 0; i < plots.Length; i++)
            {
                var yaw = (i * 37f) % 90f - 45f;
                House(peace.transform, plots[i], yaw, wall, roof);
                Ruin(ruin.transform, plots[i], yaw, wall);
                DungeonProp("torch_lit", plots[i] + new Vector3(1.6f, 0, 1.6f), yaw, 1f)
                    ?.transform.SetParent(fire.transform, true);
            }

            // Dressing shared by both states — the cart the child hides under, the
            // training ground, the well. Parented to the set, not to a state.
            // Dressing sits off the playing area. The middle of the street is the
            // stage: father and son train there, and the camera works there.
            DungeonProp("crates_stacked", new Vector3(-6.4f, 0, -5.8f), 25f, 1.1f)
                ?.transform.SetParent(root.transform, true);
            DungeonProp("barrel_large", new Vector3(6.2f, 0, -4.6f), -15f, 1f)
                ?.transform.SetParent(root.transform, true);
            // The cart the child hides under — placed clear of the training ground
            // but inside the shot the over-the-shoulder uses.
            DungeonProp("crates_stacked", new Vector3(-4.6f, 0, 2.6f), 70f, 1f)
                ?.transform.SetParent(root.transform, true);
            DungeonProp("rubble_large", new Vector3(-2.6f, 0, -3.4f), 0f, 1.2f)
                ?.transform.SetParent(ruin.transform, true);
            DungeonProp("rubble_half", new Vector3(4.4f, 0, -2.6f), 40f, 1.1f)
                ?.transform.SetParent(ruin.transform, true);

            set.peaceGroup = peace;
            set.ruinGroup = ruin;
            set.fireGroup = fire;

            BuildStoryCast();

            // Camera: no follow target — every shot in a cinematic is scripted.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 56f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UI.CinematicGrade>();
            camGo.AddComponent<CameraRig>();
            camGo.transform.position = new Vector3(0f, 2f, -6f);

            var runner = new GameObject("StoryRunner").AddComponent<Story.StoryRunner>();
            runner.beatId = "opening";
            runner.nextScene = "Rooftop";
            runner.openingState = Story.SetState.Peace;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            AssertNoMissingScripts(scene);
            EditorSceneManager.SaveScene(scene, OpeningScene);
        }

        /// <summary>
        /// A component whose script cannot be resolved serialises with a null
        /// m_Script and corrupts the scene in a player build — it shows up only as
        /// "level0 is corrupted" on device, with no editor warning. The usual cause
        /// is a MonoBehaviour whose class name does not match its file name, which
        /// Unity needs in order to create the MonoScript the reference points at.
        /// Fail the build here rather than ship a scene that cannot load.
        /// </summary>
        private static void AssertNoMissingScripts(UnityEngine.SceneManagement.Scene scene)
        {
            var bad = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    foreach (var c in t.GetComponents<Component>())
                        if (c == null)
                        {
                            Debug.LogError($"[Emberline] Missing script on '{t.name}' in "
                                           + $"{scene.name} — the scene will not load in a build.");
                            bad++;
                        }
            if (bad > 0)
                Debug.LogError($"[Emberline] {scene.name} has {bad} unresolved script(s).");
        }

        private static void House(Transform parent, Vector3 at, float yaw,
            Material wall, Material roof)
        {
            var go = new GameObject("House");
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(4.2f, 2.8f, 4.6f);
            body.transform.localPosition = new Vector3(0, 1.4f, 0);
            body.GetComponent<Renderer>().sharedMaterial = wall;

            // Gable roof from two leaning slabs. Each is tilted 55 degrees off
            // vertical so the pair meets at the ridge and its feet land on the
            // wall line — a single rotated cube reads as debris, not a roof.
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1f : 1f;
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.transform.SetParent(go.transform, false);
                slab.transform.localScale = new Vector3(0.32f, 2.6f, 5.4f);
                slab.transform.localPosition = new Vector3(side * 1.06f, 3.35f, 0);
                slab.transform.localRotation = Quaternion.Euler(0, 0, side * 55f);
                slab.GetComponent<Renderer>().sharedMaterial = roof;
            }
        }

        private static void Ruin(Transform parent, Vector3 at, float yaw, Material wall)
        {
            var go = new GameObject("Ruin");
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));

            // Two leaning stubs where four walls were. The gap is the point.
            for (var i = 0; i < 2; i++)
            {
                var stub = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stub.transform.SetParent(go.transform, false);
                stub.transform.localScale = new Vector3(3.4f, 1.1f + i * 0.5f, 0.5f);
                stub.transform.localPosition = new Vector3(i == 0 ? -1.6f : 1.5f, 0.6f, i == 0 ? -1.9f : 2f);
                stub.transform.localRotation = Quaternion.Euler(i == 0 ? 6f : -9f, i * 90f, i == 0 ? -4f : 7f);
                stub.GetComponent<Renderer>().sharedMaterial = wall;
            }
        }

        /// <summary>
        /// Places the story cast. Each is a PLACEHOLDER stand-in (see the specs in
        /// EmberCharacterFactory) tagged with a CastMember so shots can name it.
        /// </summary>
        private static void BuildStoryCast()
        {
            void Actor(string castName, EmberCharacterFactory.Spec spec, Vector3 at, float yaw)
            {
                var go = new GameObject("Cast_" + castName);
                go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
                if (!EmberCharacterFactory.Build(go, spec))
                    Debug.LogWarning($"[Story] Cast '{castName}' has no model — shots will skip it.");
                go.AddComponent<Story.CastMember>().castName = castName;
            }

            // Blocked as a scene, not scattered: the two who are training face each
            // other across the street, the two who are watching stand together.
            Actor("REN", EmberCharacterFactory.YoungRen(), new Vector3(-0.9f, 0f, 0f), 75f);
            Actor("FATHER", EmberCharacterFactory.Father(), new Vector3(2.4f, 0f, 0.6f), 255f);
            Actor("MOTHER", EmberCharacterFactory.Mother(), new Vector3(-2.6f, 0f, 4.4f), 150f);
            Actor("AIKO", EmberCharacterFactory.AikoChild(), new Vector3(-1.5f, 0f, 4.0f), 165f);
            Actor("CHILD", EmberCharacterFactory.VillageChild(), new Vector3(-4.2f, 0f, 2.2f), 40f);
        }

        private static void BuildLighting(Theme theme, EnvTheme env)
        {
            var night = theme == Theme.Rooftop;
            // Trilight ambient, not flat: a cool sky above and a warm bounce below
            // is what stops shadowed surfaces reading as dead grey. The surface
            // shader samples sky/ground directly. All three come from the theme.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = env.ambientSky;
            RenderSettings.ambientEquatorColor = env.ambientEquator;
            RenderSettings.ambientGroundColor = env.ambientGround;

            // Exponential-squared fog reads as real atmosphere; linear fog has a
            // visible "wall" that gives away the draw distance.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = env.fogColor;
            RenderSettings.fogDensity = env.fogDensity;

            // Night skybox (procedural, tinted per theme).
            // One sky asset per theme, so the peace and ruin states of the
            // village do not have to share a horizon.
            var skyPath = $"Assets/Prefabs/Sky_{env.displayName.Replace(" ", "_").Replace(",", "")}.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.shader = Shader.Find("Skybox/Procedural");
            sky.SetFloat("_SunSize", 0.0f);
            sky.SetFloat("_AtmosphereThickness", 0.45f);
            sky.SetFloat("_Exposure",
                env.skyExposure > 0f ? env.skyExposure : night ? 0.45f : 0.4f);
            // Tint the sky from the theme's own fog and ambient so the horizon and
            // the fog meet without a seam.
            // The procedural sky darkens what it is given, so the tint needs to sit
            // well above the fog value or the horizon reads a different colour
            // from the fog it is supposed to meet.
            // Clamped: a bright daylight fog scaled 3.5x drives the tint past 1
            // and the procedural sky turns red — which is what a morning looked like.
            var tint = env.fogColor * 3.5f;
            sky.SetColor("_SkyTint", new Color(
                Mathf.Clamp01(tint.r), Mathf.Clamp01(tint.g), Mathf.Clamp01(tint.b)));
            sky.SetColor("_GroundColor", env.ambientGround * 2f);
            RenderSettings.skybox = sky;

            // Three-point cinematic rig. One directional light is what made the
            // old arenas read flat; a key that actually casts, a cool fill from the
            // opposite side, and a low back-light to separate silhouettes from the
            // fog is the cheapest possible upgrade to "shot" rather than "lit".
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = env.keyLight;
            key.intensity = env.keyIntensity;
            key.transform.rotation = Quaternion.Euler(48f, -140f, 0);
            key.shadows = LightShadows.Soft;      // tier drops this to Hard/Off
            key.shadowStrength = 0.72f;           // full-black shadows look CG
            key.shadowBias = 0.03f;
            key.shadowNormalBias = 0.5f;

            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = env.fillLight;
            fill.intensity = 0.35f;
            fill.transform.rotation = Quaternion.Euler(28f, 40f, 0);
            fill.shadows = LightShadows.None;     // fills must never cost a map

            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = env.rimLight;
            rim.intensity = 0.5f;
            rim.transform.rotation = Quaternion.Euler(12f, 25f, 0);
            rim.shadows = LightShadows.None;
        }

        /// <summary>Small warm flame light for a wall torch. Vertex-lit: cheap filler.</summary>
        private static void TorchLight(Vector3 pos)
        {
            var light = new GameObject("TorchLight").AddComponent<Light>();
            light.transform.position = pos;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.58f, 0.28f);
            light.intensity = 2.2f;
            light.range = 7.5f;
            light.renderMode = LightRenderMode.ForceVertex;
        }

        /// <summary>Place a KayKit Dungeon prop with the shared toon atlas material.</summary>
        /// <summary>
        /// Place a KayKit prop. A prop given a collider also registers itself as
        /// an arena obstacle: a solid object that navigation does not know about
        /// blocks enemies and players against something no code can steer around,
        /// and every such prop had to remember to add itself by hand. One of them
        /// did not, and a marsh barrel quietly walled off part of the arena.
        /// </summary>
        private static GameObject DungeonProp(string fbxName, Vector3 pos, float yaw,
            float scale = 1f, bool collider = false, ArenaMarkers markers = null,
            float obstacleRadius = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Art/Environments/Dungeon/{fbxName}.fbx");
            if (asset == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;

            // Every KayKit environment prop was still on Emberline/Toon — the cel
            // shader with the black outline — while characters and geometry moved
            // to Emberline/Surface in the visual overhaul. The props were the last
            // thing in the game drawing an outline, which is why crates and barrels
            // read as cartoon stickers against PBR-lit stone.
            var matPath = "Assets/Prefabs/Mat_DungeonAtlas.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(SurfaceKit.SurfaceShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = SurfaceKit.SurfaceShader;
            SurfaceKit.Apply(mat, Surface.Wood, Color.white);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Environments/Dungeon/dungeon_texture.png");
            if (tex != null) mat.mainTexture = tex;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            if (collider && markers != null)
            {
                var already = markers.obstacles.Exists(o =>
                    Mathf.Abs(o.x - pos.x) < 0.4f && Mathf.Abs(o.z - pos.z) < 0.4f);
                if (!already)
                    markers.obstacles.Add(new Vector4(pos.x, 0f, pos.z, obstacleRadius));
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
            ground.GetComponent<Renderer>().sharedMaterial = Mat($"Deck{theme}", deckCol, night ? Surface.WetStone : Surface.Stone);

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
                wall.GetComponent<Renderer>().sharedMaterial = Mat($"Parapet{theme}", trimCol, Surface.Stone);
            }

            var markers = new GameObject("ArenaMarkers").AddComponent<ArenaMarkers>();

            if (theme == Theme.Rooftop)
            {
                // Roof-tile ridge bars give the deck visual rhythm.
                var ridgeMat = Mat("Ridge", new Color(0.12f, 0.15f, 0.2f), Surface.WetStone);
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
                var brickMat = Mat("Chimney", new Color(0.21f, 0.17f, 0.19f), Surface.Stone);
                var capMat = Mat("ChimneyCap", new Color(0.13f, 0.11f, 0.13f), Surface.DarkMetal);
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
                DungeonProp("crates_stacked", new Vector3(-9.5f, 0, -4.5f), 20f, 1.15f, collider: true,
                    markers: markers, obstacleRadius: 1.1f);
                DungeonProp("box_large", new Vector3(9f, 0, 4.8f), -35f, 1.1f, collider: true,
                    markers: markers, obstacleRadius: 0.9f);
                DungeonProp("box_small", new Vector3(-5f, 0, -3.9f), 65f, 1f);
                DungeonProp("keg", new Vector3(0.6f, 0, 1.9f), 10f, 1f);
                DungeonProp("banner_red", new Vector3(-6f, 1.55f, 8.45f), 180f, 1.2f);
                DungeonProp("banner_thin_red", new Vector3(6f, 1.55f, 8.45f), 180f, 1.2f);
                DungeonProp("torch_lit", new Vector3(-12.9f, 0.8f, 0f), 90f, 1.3f);
                DungeonProp("torch_lit", new Vector3(12.9f, 0.8f, 0f), -90f, 1.3f);
                // Wall torches carry their own small flame lights so the parapets
                // aren't silhouettes against nothing.
                TorchLight(new Vector3(-12.4f, 1.6f, 0f));
                TorchLight(new Vector3(12.4f, 1.6f, 0f));

                // Dressing: the deck read as an empty grey box, so it now carries
                // the debris of a place people actually use.
                DungeonProp("barrel_large", new Vector3(-11.2f, 0, 6.2f), 15f, 1.05f, collider: true,
                    markers: markers, obstacleRadius: 0.8f);
                DungeonProp("barrel_small", new Vector3(-10.2f, 0, 5.2f), -40f, 1f);
                DungeonProp("barrel_small", new Vector3(11.4f, 0, -5.6f), 60f, 1f);
                DungeonProp("crates_stacked", new Vector3(10.6f, 0, 1.4f), -15f, 1f, collider: true,
                    markers: markers, obstacleRadius: 1f);
                DungeonProp("box_small", new Vector3(3.4f, 0, -6.4f), 25f, 1f);
                DungeonProp("keg", new Vector3(-3.2f, 0, 5.4f), -30f, 1f);
                DungeonProp("chest", new Vector3(-8.4f, 0, 0.4f), 55f, 1f);
                DungeonProp("table_small", new Vector3(6.8f, 0, 6.6f), 10f, 1f);
                DungeonProp("torch_lit", new Vector3(-6f, 0.8f, -8.3f), 0f, 1.15f);
                DungeonProp("torch_lit", new Vector3(6f, 0.8f, -8.3f), 0f, 1.15f);
                TorchLight(new Vector3(-6f, 1.7f, -7.9f));
                TorchLight(new Vector3(6f, 1.7f, -7.9f));

                // Distant skyline silhouettes beyond the parapet — depth, no colliders.
                var skylineMat = Mat("Skyline", new Color(0.08f, 0.10f, 0.15f), Surface.Stone);
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
                var poolMat = Mat("MarshPool", new Color(0.16f, 0.26f, 0.24f), Surface.Water);
                var reedMat = Mat("Reed", new Color(0.2f, 0.32f, 0.22f), Surface.Foliage);
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
                var cartMat = Mat("Cart", new Color(0.23f, 0.18f, 0.13f), Surface.Wood);
                var wheelMat = Mat("CartWheel", new Color(0.15f, 0.12f, 0.09f), Surface.Wood);
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
                DungeonProp("barrel_large", new Vector3(-6.2f, -0.18f, 3.1f), 30f, 1.1f, collider: true,
                    markers: markers, obstacleRadius: 0.9f);
                DungeonProp("barrel_small", new Vector3(-5.3f, 0, 2.2f), 70f, 1f);
                DungeonProp("barrel_small", new Vector3(7.4f, -0.1f, -2.8f), -15f, 1f);
                DungeonProp("chest", new Vector3(6.0f, 0, -1.6f), -140f, 1.05f);
                DungeonProp("rubble_large", new Vector3(-10.5f, 0, -5f), 45f, 1.3f, collider: true,
                    markers: markers, obstacleRadius: 1.3f);
                DungeonProp("rubble_half", new Vector3(10.8f, 0, 5.4f), -70f, 1.2f);
                DungeonProp("column", new Vector3(11.6f, 0, -6.2f), 15f, 1.1f, collider: true,
                    markers: markers, obstacleRadius: 0.8f);

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
                post.GetComponent<Renderer>().sharedMaterial = Mat($"Post{theme}", trimCol, Surface.Wood);

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
                // Warmer and further-reaching now that the toon shader actually
                // receives point lights; these are what give the deck its pools
                // of firelight instead of an even grey wash.
                light.intensity = 2.9f;
                light.range = 12f;
                light.renderMode = LightRenderMode.ForcePixel;

                // Destructible: break for a health pickup at the cost of the light.
                // Lit ground makes the player easier to spot.
                Visibility.RegisterLight(corner);
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

            // Skeletal Renzo (EmberCharacterFactory.PlayerSpec); NinjaRig primitives as fallback.
            var playerSpec = EmberCharacterFactory.PlayerSpec();
            if (EmberCharacterFactory.Build(player, playerSpec))
            {
                // Pre-attach every prop the weapon catalogue can ask for. A player
                // build cannot instantiate FBX assets, so CombatController swaps
                // weapons by enabling one of these and hiding the others.
                // Hidden until chosen: CombatController.ApplyWeapon enables the
                // right pair on Start, but the scene should not ship with Renzo
                // holding the entire armoury at once.
                foreach (var prop in WeaponPropsRight)
                {
                    if (prop == playerSpec.propRight) continue;
                    // Pass the spec so every catalogue weapon gets the same grip
                    // correction and scale as the one the spec attaches itself —
                    // without it a Mixamo rig hangs them off the bare wrist joint.
                    var go = EmberCharacterFactory.AttachProp(player, prop, "r", true, "RenzoModel",
                        playerSpec.propScale, playerSpec.socketRight, playerSpec);
                    if (go != null) go.SetActive(false);
                }
                foreach (var prop in WeaponPropsLeft)
                {
                    var go = EmberCharacterFactory.AttachProp(player, prop, "l", false, "RenzoModel",
                        playerSpec.propScale, playerSpec.socketLeft, playerSpec);
                    if (go != null) go.SetActive(false);
                }
            }
            else
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
            cam.fieldOfView = 56f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UI.CinematicGrade>();
            var rig = camGo.AddComponent<CameraRig>();
            rig.SetTarget(target);
            // Seed at the close over-the-shoulder placement so the first frame is
            // already framed; CameraRig owns it from there.
            camGo.transform.position = target.position + new Vector3(0.55f, 2.8f, -3.8f);
        }

        /// <summary>
        /// Bakes the KayKit dagger into a Resources prefab so the runtime kunai
        /// pool can load a real blade model. Root +Z = blade point (flight axis).
        /// </summary>
        private static void BuildKunaiPrefab()
        {
            // Every thrown type is the same recipe: a KayKit prop, normalised to a
            // sane length and pointed along +Z (its flight axis).
            ThrownPrefab("Kunai", "dagger", 0.55f, new Color(0.62f, 0.68f, 0.78f));
            ThrownPrefab("Bolt", "dagger", 0.38f, new Color(0.85f, 0.72f, 0.38f));
            ThrownPrefab("Bomb", "smokebomb", 0.34f, new Color(0.42f, 0.44f, 0.48f));
        }

        private static void ThrownPrefab(string assetName, string propName, float length, Color tint)
        {
            Directory.CreateDirectory("Assets/Resources/Props");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Art/Characters/Props/{propName}.fbx");
            if (source == null)
            {
                Debug.LogWarning($"[Emberline] {propName}.fbx missing — {assetName} keeps primitive look");
                return;
            }
            var root = new GameObject(assetName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
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
            if (maxDim > 0.01f) model.transform.localScale = Vector3.one * (length / maxDim);
            model.transform.localPosition -= B().center;

            var mat = Mat($"Thrown{assetName}", tint, Surface.Steel);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Props/{assetName}.prefab");
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
            katana.archetype = WeaponArchetype.Blade;
            katana.propRight = "sword_1handed";
            katana.propLeft = "";
            katana.strikeChainLength = 3;
            katana.cleaveStyle = CleaveStyle.Slash;
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
            tanto.archetype = WeaponArchetype.Blade;
            tanto.propRight = "dagger";
            tanto.propLeft = "";
            tanto.strikeChainLength = 3;
            tanto.cleaveStyle = CleaveStyle.Slash;
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
            hook.archetype = WeaponArchetype.Blade;
            hook.propRight = "axe_2handed";
            hook.propLeft = "";
            hook.strikeChainLength = 3;
            hook.cleaveStyle = CleaveStyle.Slash;
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

            // --- non-sword kit ------------------------------------------------

            // Twin Daggers: a long, fast chain that trades reach for pressure. The
            // five-hit chain is the point — it rewards staying committed.
            var daggers = W("TwinDaggers");
            daggers.id = "daggers";
            daggers.displayName = "TWIN DAGGERS";
            daggers.blurb = "Five-hit chain, no reach. Spin cleave hits everything around you.";
            daggers.unlockLevel = 2;
            daggers.archetype = WeaponArchetype.Daggers;
            daggers.propRight = "dagger";
            daggers.propLeft = "dagger";
            daggers.strikeChainLength = 5;
            daggers.strikeDamage = new[] { 6f, 6f, 8f, 8f, 14f };
            daggers.strikeRange = 2.0f;
            daggers.strikeArcDeg = 120f;
            daggers.chainWindow = 0.95f;
            daggers.strikeAnimTime = 0.17f;
            daggers.lungeSpeed = 6.8f;
            daggers.cleaveStyle = CleaveStyle.Spin;
            daggers.cleaveDamage = 18f;
            daggers.cleaveRange = 2.8f;
            daggers.cleaveArcDeg = 360f;
            daggers.cleaveWindup = 0.16f;
            daggers.cleaveCooldown = 1.2f;
            daggers.trailColor = new Color(0.75f, 0.85f, 1f);
            EditorUtility.SetDirty(daggers);

            // Smoke Bomb: barely a weapon in the hand. The cleave is the kit — a
            // ground burst centred on Renzo, and the throw slot becomes bombs.
            var bomb = W("SmokeBomb");
            bomb.id = "bomb";
            bomb.displayName = "SMOKE BOMB";
            bomb.blurb = "Weak in the hand. The ground burst clears a circle and the throw does the work.";
            bomb.unlockLevel = 5;
            bomb.archetype = WeaponArchetype.Thrown;
            bomb.propRight = "smokebomb";
            bomb.propLeft = "";
            bomb.strikeChainLength = 2;
            bomb.strikeDamage = new[] { 7f, 9f };
            bomb.strikeRange = 2.1f;
            bomb.strikeArcDeg = 110f;
            bomb.chainWindow = 0.7f;
            bomb.strikeAnimTime = 0.24f;
            bomb.lungeSpeed = 4.2f;
            bomb.cleaveStyle = CleaveStyle.Ground;
            bomb.cleaveDamage = 22f;
            bomb.cleaveRange = 4.2f;
            bomb.cleaveArcDeg = 360f;
            bomb.cleaveWindup = 0.3f;
            bomb.cleaveCooldown = 1.7f;
            bomb.replacesKunaiWithThrown = true;
            bomb.thrownId = "Bomb";
            bomb.trailColor = new Color(0.6f, 0.62f, 0.66f);
            EditorUtility.SetDirty(bomb);

            // Hand Crossbow: a ranged option that still has to survive up close.
            // Quiver on the off hand so the silhouette reads as a shooter.
            var bow = W("HandCrossbow");
            bow.id = "crossbow";
            bow.displayName = "HAND CROSSBOW";
            bow.blurb = "Poor in melee. Fan-shot cleave and a bolt on the throw.";
            bow.unlockLevel = 8;
            bow.archetype = WeaponArchetype.Ranged;
            bow.propRight = "crossbow_1handed";
            bow.propLeft = "quiver";
            bow.strikeChainLength = 2;
            bow.strikeDamage = new[] { 6f, 8f };
            bow.strikeRange = 1.9f;
            bow.strikeArcDeg = 100f;
            bow.chainWindow = 0.65f;
            bow.strikeAnimTime = 0.26f;
            bow.lungeSpeed = 3.6f;
            bow.cleaveStyle = CleaveStyle.FanShot;
            bow.cleaveDamage = 14f;      // per bolt
            bow.cleaveRange = 3f;
            bow.cleaveArcDeg = 60f;
            bow.cleaveWindup = 0.22f;
            bow.cleaveCooldown = 1.6f;
            bow.replacesKunaiWithThrown = true;
            bow.thrownId = "Bolt";
            bow.trailColor = new Color(0.9f, 0.8f, 0.55f);
            EditorUtility.SetDirty(bow);
        }

        /// <summary>Every hand prop any weapon can ask for, so runtime swaps have targets.</summary>
        private static readonly string[] WeaponPropsRight =
            { "sword_1handed", "dagger", "axe_2handed", "smokebomb", "crossbow_1handed" };

        private static readonly string[] WeaponPropsLeft = { "dagger", "quiver" };

        // ----------------------------------------------------- prefabs / data

        private static GameObject[] BuildEnemyPrefabs()
        {
            var prefabs = new GameObject[13];
            // Skeletal characters (KayKit) for the converted roster. Mooks were
            // dying inside a single chain and barely trading damage; these values
            // make every one of them cost you at least one exchange.
            prefabs[(int)EnemyKind.Bandit] = SkeletalEnemyPrefab(EnemyKind.Bandit, "Bandit",
                EmberCharacterFactory.Bandit(),
                hp: 72, spd: 3.2f, range: 1.8f, dmg: 13, windup: 0.48f, spawn: 1.1f,
                EnemyWeapon.Daggers);
            prefabs[(int)EnemyKind.Chief] = SkeletalEnemyPrefab(EnemyKind.Chief, "BanditChief",
                EmberCharacterFactory.Goro(),
                hp: 380, spd: 2.6f, range: 2.3f, dmg: 15, windup: 0.6f, spawn: 1.2f,
                EnemyWeapon.Axe);
            prefabs[(int)EnemyKind.Shade] = SkeletalEnemyPrefab(EnemyKind.Shade, "Shade",
                EmberCharacterFactory.Shade(),
                hp: 48, spd: 4.8f, range: 1.7f, dmg: 15, windup: 0.35f, spawn: 1.0f,
                EnemyWeapon.Claws);
            prefabs[(int)EnemyKind.Ranged] = SkeletalEnemyPrefab(EnemyKind.Ranged, "RangedWeaver",
                EmberCharacterFactory.Archer(),
                hp: 55, spd: 2.7f, range: 8f, dmg: 12, windup: 0.7f, spawn: 1.0f,
                EnemyWeapon.Crossbow);
            prefabs[(int)EnemyKind.Kagachi] = SkeletalEnemyPrefab(EnemyKind.Kagachi, "Kagachi",
                EmberCharacterFactory.Kagachi(),
                hp: 420, spd: 3.9f, range: 2f, dmg: 14, windup: 0.5f, spawn: 1.8f,
                EnemyWeapon.Sword);
            prefabs[(int)EnemyKind.Jin] = SkeletalEnemyPrefab(EnemyKind.Jin, "Jin",
                EmberCharacterFactory.Jin(),
                hp: 340, spd: 4.6f, range: 2f, dmg: 12, windup: 0.42f, spawn: 1.1f,
                EnemyWeapon.Sword);

            // --- weapon-defined raiders -------------------------------------
            // Stats deliberately sit in the existing mook band; W3 is about how
            // they fight, not about another HP pass.
            prefabs[(int)EnemyKind.RaiderAxe] = SkeletalEnemyPrefab(EnemyKind.RaiderAxe, "RaiderAxe",
                EmberCharacterFactory.RaiderAxe(),
                hp: 95, spd: 2.9f, range: 2.2f, dmg: 17, windup: 0.62f, spawn: 1.1f,
                EnemyWeapon.Axe);
            prefabs[(int)EnemyKind.PikeGuard] = SkeletalEnemyPrefab(EnemyKind.PikeGuard, "PikeGuard",
                EmberCharacterFactory.PikeGuard(),
                hp: 80, spd: 3f, range: 3.6f, dmg: 14, windup: 0.55f, spawn: 1.1f,
                EnemyWeapon.Spear);
            prefabs[(int)EnemyKind.Bomber] = SkeletalEnemyPrefab(EnemyKind.Bomber, "Bomber",
                EmberCharacterFactory.Bomber(),
                hp: 52, spd: 2.8f, range: 9f, dmg: 16, windup: 0.7f, spawn: 1.0f,
                EnemyWeapon.Bomb);

            // --- def-driven roster ------------------------------------------
            // These four carry an EnemyDef; their stats come from the asset, so the
            // numbers passed here are only the pre-def fallback.
            prefabs[(int)EnemyKind.Assassin] = SkeletalEnemyPrefab(EnemyKind.Assassin, "Assassin",
                EmberCharacterFactory.Assassin(),
                hp: 62, spd: 4.6f, range: 1.7f, dmg: 11, windup: 0.34f, spawn: 1.0f,
                EnemyWeapon.Daggers);
            prefabs[(int)EnemyKind.Samurai] = SkeletalEnemyPrefab(EnemyKind.Samurai, "Samurai",
                EmberCharacterFactory.Samurai(),
                hp: 130, spd: 2.9f, range: 2.4f, dmg: 18, windup: 0.62f, spawn: 1.2f,
                EnemyWeapon.Sword);
            prefabs[(int)EnemyKind.RogueNinja] = SkeletalEnemyPrefab(EnemyKind.RogueNinja, "RogueNinja",
                EmberCharacterFactory.RogueNinja(),
                hp: 70, spd: 5.2f, range: 1.9f, dmg: 13, windup: 0.38f, spawn: 1.0f,
                EnemyWeapon.Daggers);
            prefabs[(int)EnemyKind.EliteWarrior] = SkeletalEnemyPrefab(EnemyKind.EliteWarrior,
                "EliteWarrior", EmberCharacterFactory.EliteWarrior(),
                hp: 210, spd: 3.2f, range: 2.5f, dmg: 20, windup: 0.55f, spawn: 1.3f,
                EnemyWeapon.Axe);

            AssignEnemyDefs(prefabs);
            return prefabs;
        }

        /// <summary>
        /// Authors the enemy roster as ScriptableObjects under Resources/Enemies and
        /// binds each to its prefab. Every difference between two enemies —
        /// silhouette scale, weapon, speed, reach, moveset, defence, weakness —
        /// lives in these assets. Adding an eleventh enemy is a block here plus a
        /// character Spec; no new code in EnemyBrain.
        /// </summary>
        private static void AssignEnemyDefs(GameObject[] prefabs)
        {
            Directory.CreateDirectory("Assets/Resources/Enemies");

            EnemyDef D(string file)
            {
                var path = $"Assets/Resources/Enemies/{file}.asset";
                var d = AssetDatabase.LoadAssetAtPath<EnemyDef>(path);
                if (d == null)
                {
                    d = ScriptableObject.CreateInstance<EnemyDef>();
                    AssetDatabase.CreateAsset(d, path);
                }
                return d;
            }

            static AttackDefinition A(AttackKind kind, float min, float max, float weight,
                float dmg = 1f, float cd = 1.5f, float windup = 0f,
                bool red = false, float ring = 1f, string id = "",
                AttackCategory? category = null) => new()
            {
                kind = kind, minRange = min, maxRange = max, weight = weight,
                damageMultiplier = dmg, cooldown = cd, windupOverride = windup,
                redTelegraph = red, telegraphScale = ring,
                id = string.IsNullOrEmpty(id) ? kind.ToString().ToLowerInvariant() : id,
                displayName = string.IsNullOrEmpty(id) ? kind.ToString() : id.ToUpperInvariant(),
                category = category ?? AttackDefinition.CategoryOf(kind, windup),
            };

            // 1 — BANDIT: the baseline. One honest swing, no tricks.
            var bandit = D("Bandit");
            bandit.id = "bandit"; bandit.displayName = "RAIDER"; bandit.kind = EnemyKind.Bandit;
            bandit.weapon = EnemyWeapon.Daggers; bandit.rank = EnemyRank.Mook;
            bandit.modelSpec = "BanditModel"; bandit.scale = 1f;
            bandit.maxHp = 72; bandit.moveSpeed = 3.2f; bandit.attackRange = 1.8f;
            bandit.damage = 13; bandit.windupTime = 0.48f; bandit.spawnTime = 1.1f;
            bandit.movement = MovementStyle.Flank; bandit.preferredRange = 2f;
            bandit.attacks = new[] { A(AttackKind.Flurry, 0f, 2.4f, 1f, 1f, 1.5f) };
            bandit.poise = 0f; bandit.backstabMultiplier = 1.2f;
            bandit.staggerDecay = 0.7f;
            bandit.codexLine = "Road raiders. Quick knives, no discipline.";
            EditorUtility.SetDirty(bandit);

            // 2 — ASSASSIN: fastest thing on the field, folds if you catch it.
            var assassin = D("Assassin");
            assassin.id = "assassin"; assassin.displayName = "ASSASSIN";
            assassin.kind = EnemyKind.Assassin; assassin.weapon = EnemyWeapon.Daggers;
            assassin.rank = EnemyRank.Mook; assassin.modelSpec = "AssassinModel";
            assassin.scale = 0.96f;
            assassin.maxHp = 62; assassin.moveSpeed = 4.6f; assassin.attackRange = 1.7f;
            assassin.damage = 11; assassin.windupTime = 0.34f; assassin.spawnTime = 1.0f;
            assassin.movement = MovementStyle.Erratic; assassin.preferredRange = 2.2f;
            assassin.attacks = new[]
            {
                A(AttackKind.Flurry, 0f, 2.2f, 2f, 0.9f, 1.2f),
                A(AttackKind.DashStrike, 3.5f, 8f, 1f, 1f, 2.2f, 0.4f, true, 1.1f),
            };
            assassin.poise = 0f; assassin.crushMultiplier = 1.35f; assassin.backstabMultiplier = 1.3f;
            assassin.punishesExposure = true; assassin.dodgeChance = 0.35f;
            assassin.retreatBelowHp = 0.3f; assassin.staggerDecay = 0.6f;
            assassin.codexLine = "Paid knives. They open with the dash and never trade twice.";
            EditorUtility.SetDirty(assassin);

            // 3 — SPEARMAN: owns the space you want to stand in.
            var spear = D("Spearman");
            spear.id = "spearman"; spear.displayName = "PIKE GUARD";
            spear.kind = EnemyKind.PikeGuard; spear.weapon = EnemyWeapon.Spear;
            spear.rank = EnemyRank.Mook; spear.modelSpec = "PikeGuardModel";
            spear.maxHp = 80; spear.moveSpeed = 3f; spear.attackRange = 3.6f;
            spear.damage = 14; spear.windupTime = 0.55f; spear.spawnTime = 1.1f;
            spear.movement = MovementStyle.Reach; spear.preferredRange = 4f;
            spear.attacks = new[] { A(AttackKind.Thrust, 1.4f, 4.2f, 1f, 1f, 1.9f, 0f, false, 1.5f) };
            spear.poise = 0.2f; spear.armor = 1f;
            // Long weapon, slow recovery: getting inside the shaft is the answer.
            spear.backstabMultiplier = 1.5f;
            spear.punishesExposure = true; spear.protectsRanged = true;
            spear.readsHeavies = true; spear.blockChance = 0.2f;
            spear.codexLine = "Toll guards. Their reach is the whole fight — close it.";
            EditorUtility.SetDirty(spear);

            // 4 — ARCHER: harmless in melee, punishing at range.
            var archer = D("Archer");
            archer.id = "archer"; archer.displayName = "WEAVER";
            archer.kind = EnemyKind.Ranged; archer.weapon = EnemyWeapon.Crossbow;
            archer.rank = EnemyRank.Mook; archer.modelSpec = "ArcherModel";
            archer.maxHp = 55; archer.moveSpeed = 2.7f; archer.attackRange = 8f;
            archer.damage = 12; archer.windupTime = 0.7f; archer.spawnTime = 1.0f;
            archer.movement = MovementStyle.Kite; archer.preferredRange = 7f;
            archer.attacks = new[]
            {
                A(AttackKind.ChargedShot, 4f, 9f, 1f, 2f, 2.6f, 1.2f, true, 1.25f),
                A(AttackKind.QuickShot, 3f, 6f, 0.6f, 0.8f, 1.4f, 0.4f),
                // Emergency only: a weak jab when the player is already on it.
                A(AttackKind.Slash, 0f, 1.7f, 0.25f, 0.6f, 1.6f, 0.4f),
            };
            archer.poise = 0f; archer.backstabMultiplier = 2f; // the classic weak point
            archer.panicRange = 3.2f; archer.retreatBelowHp = 0.5f;
            archer.codexLine = "Bolt-weavers. Break the line of sight or get behind them.";
            EditorUtility.SetDirty(archer);

            // 5 — HEAVY WARRIOR: armoured, slow, immune to chip damage.
            var heavy = D("HeavyWarrior");

            heavy.id = "heavy"; heavy.displayName = "AXE RAIDER";
            heavy.kind = EnemyKind.RaiderAxe; heavy.weapon = EnemyWeapon.Axe;
            heavy.rank = EnemyRank.Elite; heavy.modelSpec = "RaiderAxeModel"; heavy.scale = 1.05f;
            heavy.maxHp = 150; heavy.moveSpeed = 2.6f; heavy.attackRange = 2.2f;
            heavy.damage = 19; heavy.windupTime = 0.68f; heavy.spawnTime = 1.1f;
            heavy.movement = MovementStyle.Direct; heavy.preferredRange = 2f;
            heavy.attacks = new[]
            {
                A(AttackKind.Slash, 0f, 2.8f, 1.4f, 1f, 1.9f, 0f, true, 1.6f),
                A(AttackKind.HeavySlam, 0f, 3.6f, 0.7f, 1.2f, 3f, 0.85f, true, 2f),
            };
            heavy.armor = 4f; heavy.poise = 0.7f;      // shrugs off light hits
            heavy.crushMultiplier = 1.3f;              // but crush gets through
            heavy.backstabMultiplier = 1.25f;
            heavy.readsHeavies = true; heavy.guardsWhenPostureLow = true;
            heavy.blockChance = Mathf.Max(heavy.blockChance, 0.3f); heavy.staggerDecay = 0.55f;
            heavy.codexLine = "Armoured raiders. Light hits bounce; commit or go around.";
            EditorUtility.SetDirty(heavy);

            // 6 — SAMURAI: defensive. Blocks, then punishes the greedy.
            var samurai = D("Samurai");
            samurai.id = "samurai"; samurai.displayName = "RONIN";
            samurai.kind = EnemyKind.Samurai; samurai.weapon = EnemyWeapon.Sword;
            samurai.rank = EnemyRank.Elite; samurai.modelSpec = "SamuraiModel"; samurai.scale = 1.03f;
            samurai.maxHp = 130; samurai.moveSpeed = 2.9f; samurai.attackRange = 2.4f;
            samurai.damage = 18; samurai.windupTime = 0.62f; samurai.spawnTime = 1.2f;
            samurai.movement = MovementStyle.Spacing; samurai.preferredRange = 2.6f;
            samurai.attacks = new[]
            {
                A(AttackKind.Parry, 0f, 3.2f, 1.1f, 0f, 2.2f, 0.35f),
                A(AttackKind.Slash, 0f, 2.8f, 1.3f, 1.1f, 1.7f, 0f, false, 1.2f),
                A(AttackKind.DashStrike, 3.5f, 7f, 0.5f, 1f, 2.6f, 0.5f, true),
            };
            samurai.blockChance = 0.4f;   // light hits turned aside
            samurai.poise = 0.5f; samurai.armor = 2f;
            samurai.crushMultiplier = 1.5f;  // the guard does not survive a crush
            samurai.readsHeavies = true; samurai.counterChance = 0.6f;
            samurai.punishesExposure = true; samurai.guardsWhenPostureLow = true;
            samurai.dodgeChance = 0.1f;
            samurai.codexLine = "Masterless blades. They will block anything you throw lightly.";
            EditorUtility.SetDirty(samurai);

            // 7 — ROGUE NINJA: your own kit, used against you.
            var rogue = D("RogueNinja");
            rogue.id = "rogueninja"; rogue.displayName = "ROGUE NINJA";
            rogue.kind = EnemyKind.RogueNinja; rogue.weapon = EnemyWeapon.Daggers;
            rogue.rank = EnemyRank.Elite; rogue.modelSpec = "RogueNinjaModel";
            rogue.maxHp = 70; rogue.moveSpeed = 5.2f; rogue.attackRange = 1.9f;
            rogue.damage = 13; rogue.windupTime = 0.38f; rogue.spawnTime = 1.0f;
            rogue.movement = MovementStyle.Ambush; rogue.preferredRange = 2.4f;
            rogue.attacks = new[]
            {
                A(AttackKind.DashStrike, 3f, 9f, 1.4f, 1f, 1.9f, 0.35f, true, 1.1f),
                A(AttackKind.Slash, 0f, 2.2f, 1f, 1f, 1.3f),
                A(AttackKind.ThrowBomb, 5f, 11f, 0.5f, 0.8f, 4f, 0.6f, true, 1.35f),
            };
            rogue.poise = 0.1f; rogue.crushMultiplier = 1.3f; rogue.thrownMultiplier = 1.2f;
            rogue.dodgeChance = 0.45f; rogue.punishesExposure = true;
            rogue.retreatBelowHp = 0.35f; rogue.staggerDecay = 0.6f;
            rogue.codexLine = "Trained where you were. Fights the way you do — from behind.";
            EditorUtility.SetDirty(rogue);

            // 8 — ELITE WARRIOR: the full moveset, no single answer.
            var elite = D("EliteWarrior");
            elite.id = "elite"; elite.displayName = "ELITE WARRIOR";
            elite.kind = EnemyKind.EliteWarrior; elite.weapon = EnemyWeapon.Axe;
            elite.rank = EnemyRank.Elite; elite.modelSpec = "EliteWarriorModel"; elite.scale = 1.12f;
            elite.maxHp = 210; elite.moveSpeed = 3.2f; elite.attackRange = 2.5f;
            elite.damage = 20; elite.windupTime = 0.55f; elite.spawnTime = 1.3f;
            elite.movement = MovementStyle.Direct; elite.preferredRange = 2.2f;
            elite.attacks = new[]
            {
                A(AttackKind.Slash, 0f, 2.9f, 1.2f, 1f, 1.6f),
                A(AttackKind.SpinCleave, 0f, 4.5f, 0.6f, 1.2f, 3.2f, 0.9f, true, 2.4f),
                A(AttackKind.HeavySlam, 0f, 3.8f, 0.6f, 1.15f, 3f, 0.8f, true, 2f),
                A(AttackKind.DashStrike, 4f, 10f, 0.8f, 1f, 2.4f, 0.45f, true, 1.2f),
            };
            elite.armor = 5f; elite.poise = 0.75f; elite.blockChance = 0.15f;
            elite.crushMultiplier = 1.25f; elite.backstabMultiplier = 1.2f;
            elite.readsHeavies = true; elite.blockChance = Mathf.Max(elite.blockChance, 0.3f);
            elite.dodgeChance = 0.15f; elite.counterChance = 0.4f;
            elite.punishesExposure = true; elite.guardsWhenPostureLow = true;
            elite.protectsRanged = true; elite.staggerDecay = 0.5f;
            elite.codexLine = "Captains of the road. They have an answer for every range.";
            EditorUtility.SetDirty(elite);

            // 9 — MINI BOSS: Goro. Slam plus the enraged spin.
            var mini = D("MiniBoss");
            mini.id = "goro"; mini.displayName = "GORO";
            mini.kind = EnemyKind.Chief; mini.weapon = EnemyWeapon.Axe;
            mini.rank = EnemyRank.MiniBoss; mini.modelSpec = "GoroModel";
            mini.maxHp = 380; mini.moveSpeed = 2.6f; mini.attackRange = 2.3f;
            mini.damage = 15; mini.windupTime = 0.6f; mini.spawnTime = 1.2f;
            mini.movement = MovementStyle.Direct; mini.preferredRange = 2.4f;
            mini.attacks = new[]
            {
                A(AttackKind.HeavySlam, 0f, 4.4f, 1.3f, 1f, 1.8f, 0f, true, 2f),
                A(AttackKind.SpinCleave, 0f, 5f, 0.5f, 1.3f, 2.4f, 0.95f, true, 2.6f),
                A(AttackKind.DashStrike, 5f, 11f, 0.4f, 1f, 2.6f, 0.7f, true),
            };
            mini.armor = 5f; mini.poise = 0.85f; mini.crushMultiplier = 1.2f;
            mini.staggerDecay = 0.45f; mini.readsHeavies = true; mini.blockChance = 0.2f;
            mini.codexLine = "The toll-captain. Everything he does is telegraphed — and lands anyway.";
            EditorUtility.SetDirty(mini);

            // 10 — BOSS: Kagachi. Phases handled in the brain; kit lives here.
            var boss = D("Boss");
            boss.id = "kagachi"; boss.displayName = "KAGACHI";
            boss.kind = EnemyKind.Kagachi; boss.weapon = EnemyWeapon.Sword;
            boss.rank = EnemyRank.Boss; boss.modelSpec = "KagachiModel";
            boss.maxHp = 420; boss.moveSpeed = 3.9f; boss.attackRange = 2f;
            boss.damage = 14; boss.windupTime = 0.5f; boss.spawnTime = 1.8f;
            boss.movement = MovementStyle.Spacing; boss.preferredRange = 3f;
            boss.attacks = new[]
            {
                A(AttackKind.Slash, 0f, 2.6f, 1.4f, 1f, 1.3f),
                A(AttackKind.PoisonSpit, 4.5f, 12f, 1f, 0.5f, 4f, 0.55f, true, 1.3f),
                A(AttackKind.DashStrike, 5f, 11f, 0.9f, 1f, 1.8f, 0.55f, true),
            };
            boss.armor = 3f; boss.poise = 0.8f; boss.elementalMultiplier = 1f;
            boss.staggerDecay = 0.45f; boss.punishesExposure = true; boss.dodgeChance = 0.2f;
            boss.codexLine = "The Marsh Serpent. Three lives, and it spends them slowly.";
            EditorUtility.SetDirty(boss);

            // 11 — SHADE: marsh-born. Fast, fragile, and soft to smoke.
            // Stats mirror the hardcoded prefab it used to run on, so adding the
            // def gives it posture and weakness rules without moving its balance.
            var shade = D("Shade");
            shade.id = "shade"; shade.displayName = "SHADE";
            shade.kind = EnemyKind.Shade; shade.weapon = EnemyWeapon.Claws;
            shade.rank = EnemyRank.Mook; shade.modelSpec = "ShadeModel";
            shade.maxHp = 48; shade.moveSpeed = 4.8f; shade.attackRange = 1.7f;
            shade.damage = 15; shade.windupTime = 0.35f; shade.spawnTime = 1.0f;
            shade.movement = MovementStyle.Ambush; shade.preferredRange = 1.9f;
            shade.attacks = new[] { A(AttackKind.Flurry, 0f, 2.2f, 1f, 1f, 1.25f) };
            shade.poise = 0f; shade.maxPosture = 26f;
            shade.backstabMultiplier = 1.2f;
            // The smoke bomb is its counter — the brain already reads this.
            shade.elementalMultiplier = 2f;
            shade.codexLine = "What the marsh breathes out. Smoke takes them apart.";
            EditorUtility.SetDirty(shade);

            // 12 — BOMBER: the reason you cannot stand still.
            var bomber = D("Bomber");
            bomber.id = "bomber"; bomber.displayName = "POWDER CARRIER";
            bomber.kind = EnemyKind.Bomber; bomber.weapon = EnemyWeapon.Bomb;
            bomber.rank = EnemyRank.Mook; bomber.modelSpec = "BomberModel";
            bomber.maxHp = 52; bomber.moveSpeed = 2.8f; bomber.attackRange = 9f;
            bomber.damage = 16; bomber.windupTime = 0.7f; bomber.spawnTime = 1.0f;
            bomber.movement = MovementStyle.Kite; bomber.preferredRange = 7.5f;
            bomber.attacks = new[] { A(AttackKind.ThrowBomb, 4f, 10f, 1f, 1f, 3.2f, 0.7f, true, 1.4f) };
            bomber.poise = 0f; bomber.maxPosture = 24f;
            bomber.backstabMultiplier = 2f; // carrying powder; hit it from behind
            bomber.panicRange = 3.5f;
            bomber.codexLine = "Powder carriers. Kill them first or fight on burning ground.";
            EditorUtility.SetDirty(bomber);

            // 13 — JIN: the duelist boss. Fast, no armour, punishes patience.
            var jin = D("Jin");
            jin.id = "jin"; jin.displayName = "JIN";
            jin.kind = EnemyKind.Jin; jin.weapon = EnemyWeapon.Sword;
            jin.rank = EnemyRank.Boss; jin.modelSpec = "JinModel";
            jin.maxHp = 340; jin.moveSpeed = 4.6f; jin.attackRange = 2f;
            jin.damage = 12; jin.windupTime = 0.42f; jin.spawnTime = 1.1f;
            jin.movement = MovementStyle.Spacing; jin.preferredRange = 2.6f;
            jin.attacks = new[]
            {
                A(AttackKind.Slash, 0f, 2.6f, 1.5f, 1f, 1.1f),
                A(AttackKind.DashStrike, 4f, 10f, 1f, 1f, 1.7f, 0.45f, true),
                A(AttackKind.SpinCleave, 0f, 3.4f, 0.6f, 1.2f, 2.6f, 0.8f, true, 2.2f),
            };
            // No armour on purpose: Jin is answered by reading him, not by trading.
            jin.armor = 0f; jin.poise = 0.7f; jin.blockChance = 0.15f;
            jin.dodgeChance = 0.35f; jin.punishesExposure = true; jin.readsHeavies = true;
            jin.counterChance = 0.5f; jin.staggerDecay = 0.45f;
            jin.codexLine = "The storm blade. He never blocks twice the same way.";
            EditorUtility.SetDirty(jin);

            // Combat 2.0: kits and personalities, after every base def exists
            // and before the named foes copy their bases.
            AssetDatabase.SaveAssets();
            EmberCombatKits.Apply();

            // ---- Named foes. Boss-ranked defs on common bodies: the campaign
            // needs a captain, a guardian and a pale thing in the marsh without a
            // model each. A named foe is its base kind's def, heavier, with a card.
            EnemyDef Named(string id, EnemyDef baseDef, string display, string title, string taunt,
                float hpMul, float dmgMul, EnemyRank rank)
            {
                var d = D(id);
                EditorUtility.CopySerialized(baseDef, d);
                d.id = id; d.displayName = display; d.rank = rank;
                d.maxHp = baseDef.maxHp * hpMul; d.damage = baseDef.damage * dmgMul;
                d.maxPosture = baseDef.maxPosture * 1.5f;
                d.bossTitle = title; d.bossTaunt = taunt;
                d.staggerDecay = Mathf.Min(d.staggerDecay, 0.5f);
                d.readsHeavies = true; d.punishesExposure = true;
                d.codexLine = $"{display}. {baseDef.codexLine}";
                EditorUtility.SetDirty(d);
                return d;
            }
            Named("convoycaptain", samurai, "THE CONVOY CAPTAIN", "KEEPER OF THE LANTERN ROAD",
                "“Everything on this road is counted. You were not.”", 1.6f, 1.15f, EnemyRank.MiniBoss);
            Named("raiderleader", heavy, "THE SCAVENGER KING", "WHAT THE VILLAGE LEFT",
                "“They searched too. They found me.”", 1.5f, 1.1f, EnemyRank.MiniBoss);
            Named("paleshade", shade, "THE PALE SHADE", "WHAT THE FOREST KEPT",
                "“…she went north… so will you…”", 4.5f, 1.3f, EnemyRank.MiniBoss);
            Named("drownedguardian", elite, "THE DROWNED GUARDIAN", "WARDEN OF THE SECOND KEY",
                "“Your father set me here. He did not say you would come.”", 1.8f, 1.2f, EnemyRank.MiniBoss);
            Named("ironguard", elite, "THE IRON GUARD", "KAGEHIRA'S SHIELD",
                "“The warlord does not see you. I make sure of it.”", 1.7f, 1.2f, EnemyRank.MiniBoss);
            Named("finalcommander", samurai, "COMMANDER HOSHU", "THE INNER GATE",
                "“He said you would reach this door. He did not say you would open it.”", 1.9f, 1.25f, EnemyRank.MiniBoss);
            Named("threeblades", assassin, "THE THREE BLADES", "SISTERS OF THE SILENT FOREST",
                "“One for the throat. One for the heart. One to watch.”", 2.2f, 1.2f, EnemyRank.Elite);

            // The named foes exist now: their own kits and personalities.
            EmberCombatKits.ApplyNamed();

            // Bind each def to its prefab so spawned instances carry their data.
            void Bind(EnemyKind k, EnemyDef d)
            {
                var prefab = prefabs[(int)k];
                if (prefab == null) return;
                var brain = prefab.GetComponent<EnemyBrain>();
                if (brain == null) return;
                brain.def = d;
                EditorUtility.SetDirty(prefab);
            }

            Bind(EnemyKind.Bandit, bandit);
            Bind(EnemyKind.Assassin, assassin);
            Bind(EnemyKind.PikeGuard, spear);
            Bind(EnemyKind.Ranged, archer);
            Bind(EnemyKind.RaiderAxe, heavy);
            Bind(EnemyKind.Samurai, samurai);
            Bind(EnemyKind.RogueNinja, rogue);
            Bind(EnemyKind.EliteWarrior, elite);
            Bind(EnemyKind.Chief, mini);
            Bind(EnemyKind.Kagachi, boss);
            Bind(EnemyKind.Shade, shade);
            Bind(EnemyKind.Bomber, bomber);
            Bind(EnemyKind.Jin, jin);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Prefabs for the named duel foes that declare their own body. They copy
        /// their base kind's stats at spawn through SetDef, so only the visual and
        /// the EnemyBrain identity need to exist here.
        /// </summary>
        private static (string[] ids, GameObject[] prefabs) BuildNamedFoePrefabs(GameObject[] byKind)
        {
            var ids = new List<string>();
            var made = new List<GameObject>();
            foreach (var id in EmberCharacterFactory.NamedFoeIds)
            {
                var spec = EmberCharacterFactory.NamedFoe(id);
                if (spec == null) continue;
                var def = AssetDatabase.LoadAssetAtPath<EnemyDef>($"Assets/Resources/Enemies/{id}.asset");
                if (def == null) { Debug.LogWarning($"[Emberline] named foe {id}: no def"); continue; }

                var root = new GameObject($"Named_{id}");
                if (!EmberCharacterFactory.Build(root, spec))
                {
                    Object.DestroyImmediate(root);
                    Debug.LogWarning($"[Emberline] named foe {id}: model missing, inherits its kind");
                    continue;
                }
                var brain = root.AddComponent<EnemyBrain>();
                brain.kind = def.kind;
                brain.weapon = def.weapon;
                brain.maxHp = def.maxHp;
                brain.speed = def.moveSpeed;
                brain.attackRange = def.attackRange;
                brain.damage = def.damage;
                brain.windupTime = def.windupTime;
                brain.spawnTime = def.spawnTime;
                brain.arenaHalfExtents = new Vector2(13f, 8f);
                brain.def = def;

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Named_{id}.prefab");
                Object.DestroyImmediate(root);
                ids.Add(id);
                made.Add(prefab);
            }
            Debug.Log($"[Emberline] named foe visuals: {string.Join(", ", ids)}");
            return (ids.ToArray(), made.ToArray());
        }

        private static GameObject SkeletalEnemyPrefab(EnemyKind kind, string name,
            EmberCharacterFactory.Spec spec, float hp, float spd, float range,
            float dmg, float windup, float spawn, EnemyWeapon weapon = EnemyWeapon.Sword)
        {
            var root = new GameObject(name);
            if (!EmberCharacterFactory.Build(root, spec))
            {
                Object.DestroyImmediate(root);
                // Model missing — keep the old primitive prefab path alive.
                return EnemyPrefab(kind, name,
                    body: new Color(0.32f, 0.27f, 0.22f), accent: new Color(0.6f, 0.63f, 0.66f),
                    scale: 1f, hp: hp, spd: spd, range: range, dmg: dmg, windup: windup,
                    sword: true, ghost: false, weapon: weapon);
            }

            var brain = root.AddComponent<EnemyBrain>();
            brain.kind = kind;
            brain.weapon = weapon;
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
            bool sword, bool ghost, EnemyWeapon weapon = EnemyWeapon.Sword)
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
            brain.weapon = weapon;
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
                ShippedScenes, "Builds/emberline.aab",
                BuildTarget.Android, BuildOptions.None);
            EditorUserBuildSettings.buildAppBundle = false;

            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[Emberline] AAB build {(ok ? "SUCCEEDED" : "FAILED")}: {report.summary.outputPath}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void ApplyReleaseIdentity()
        {
            ConfigureAndroidPlayerSettings();
            // The store bundle carries both ABIs; the loose APK is ARM64 only.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.bundleVersion = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_NAME") ?? "1.2.0";
            var codeStr = System.Environment.GetEnvironmentVariable("EMBERLINE_VERSION_CODE");
            PlayerSettings.Android.bundleVersionCode = int.TryParse(codeStr, out var code) ? code : 7;
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
        /// <summary>
        /// Binds the launcher icon. Assets/Icon.png is authored key art — a
        /// portrait of Renzo — not something the bootstrap draws, so this only
        /// wires it up. It is the one art asset in the project that is not
        /// generated, because an icon is a marketing surface rather than a
        /// gameplay one.
        /// </summary>
        private static void EnsureIcon()
        {
            const string path = "Assets/Icon.png";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null)
            {
                Debug.LogWarning($"[Emberline] No launcher icon at {path} — the build will ship Unity's default.");
                return;
            }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        /// <summary>
        /// Material by physical surface type. Everything routes through SurfaceKit
        /// so the look lives in one table rather than in per-object tweaks, and the
        /// authored colour is graded down on the way in — the old palette was far
        /// too saturated for a night exterior.
        /// </summary>
        private static Material Mat(string name, Color color, Surface surface = Surface.Stone)
        {
            var shader = SurfaceKit.SurfaceShader;
            var path = $"Assets/Prefabs/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            SurfaceKit.Apply(mat, surface, SurfaceKit.Grade(color));
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
