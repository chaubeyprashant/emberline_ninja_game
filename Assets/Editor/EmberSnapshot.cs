using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Batch-mode visual verification: renders the character roster (posed via
    /// clip sampling) and a HUD-less arena shot to PNGs under Logs/ so art
    /// changes can be reviewed without opening the editor.
    /// </summary>
    public static class EmberSnapshot
    {
        [MenuItem("Emberline/Snapshot Characters")]
        public static void RenderLineup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.45f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -140f, 0);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 4f;
            var groundMat = new Material(Shader.Find("Emberline/Toon")) { color = new Color(0.16f, 0.19f, 0.24f) };
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // Roster: (spec, sampled clip, time) — a pose that sells the character.
            var entries = new (EmberCharacterFactory.Spec spec, string clip, float t)[]
            {
                (EmberCharacterFactory.Renzo(), "1H_Melee_Attack_Slice_Diagonal", 0.45f),
                (EmberCharacterFactory.Bandit(), "Dualwield_Melee_Attack_Slice", 0.5f),
                (EmberCharacterFactory.Goro(), "2H_Melee_Attack_Chop", 0.55f),
                (EmberCharacterFactory.Shade(), "Skeletons_Awaken_Standing", 0.8f),
                (EmberCharacterFactory.Kagachi(), "Taunt_Longer", 0.5f),
                (EmberCharacterFactory.Jin(), "2H_Melee_Attack_Slice", 0.5f),
                (EmberCharacterFactory.Archer(), "1H_Ranged_Aiming", 0.5f),
            };

            var x = -7.5f;
            foreach (var (spec, clipName, t) in entries)
            {
                var root = new GameObject(spec.name);
                root.transform.position = new Vector3(x, 0, 0);
                root.transform.rotation = Quaternion.Euler(0, 180f, 0); // face camera
                if (EmberCharacterFactory.Build(root, spec))
                {
                    var model = root.transform.Find("Model")?.gameObject;
                    var clip = FindClip(spec.fbx, clipName);
                    if (model != null && clip != null) clip.SampleAnimation(model, clip.length * t);
                }
                x += 2.6f;
            }

            Shoot(new Vector3(0, 2.3f, -9.5f), Quaternion.Euler(8f, 0, 0), "Logs/lineup.png", 1920, 900);
            Debug.Log("[Emberline] Snapshot written to Logs/lineup.png");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Emberline/Snapshot Arenas")]
        public static void RenderArenas()
        {
            foreach (var (scene, file) in new[]
            {
                ("Assets/Scenes/Rooftop.unity", "Logs/arena_rooftop.png"),
                ("Assets/Scenes/Marsh.unity", "Logs/arena_marsh.png"),
            })
            {
                EditorSceneManager.OpenScene(scene);
                Shoot(new Vector3(0, 10f, -13f), Quaternion.Euler(38f, 0, 0), file, 1600, 900);
            }
            Debug.Log("[Emberline] Arena snapshots written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Visual check of the Road North march mode: streams the runtime
        /// corridor into the Rooftop scene (edit mode, unsaved) and shoots the
        /// road mouth + a stretch of causeway with the mist barrier raised.
        /// </summary>
        [MenuItem("Emberline/Snapshot Road North")]
        public static void RenderRoad()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");
            var renzo = GameObject.Find("Renzo");
            if (renzo == null)
            {
                Debug.LogError("[Emberline] No Renzo in Rooftop scene");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            var road = RoadNorth.Begin(renzo.transform);
            road.RaiseBarrier(renzo.transform.position.z + 28f);

            // Kunai prefab close-up: blade must point along +Z (its flight axis).
            var kunaiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Props/Kunai.prefab");
            if (kunaiPrefab != null)
            {
                var kunai = (GameObject)PrefabUtility.InstantiatePrefab(kunaiPrefab);
                kunai.transform.position = new Vector3(8f, 1.4f, 0f);
                kunai.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                // Side profile: handle → blade point should read left-to-right.
                Shoot(new Vector3(7.1f, 1.5f, 0f), Quaternion.Euler(6f, 90f, 0),
                    "Logs/kunai_closeup.png", 900, 700);
            }

            // Gameplay-ish angle from behind the player looking up the road…
            Shoot(new Vector3(0, 8.2f, -9.9f), Quaternion.Euler(38f, 0, 0),
                "Logs/road_north.png", 1600, 900);
            // …and a high side view of the arena mouth opening into the corridor.
            Shoot(new Vector3(-20f, 14f, 14f), Quaternion.Euler(35f, 55f, 0),
                "Logs/road_north_side.png", 1600, 900);

            // Never save — the scene stays the authored theme shell.
            Debug.Log("[Emberline] Road snapshots written");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static AnimationClip FindClip(string fbxPath, string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is AnimationClip c && c.name == name) return c;
            return null;
        }

        private static void Shoot(Vector3 pos, Quaternion rot, string file, int w, int h)
        {
            var camGo = new GameObject("SnapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.11f, 0.16f);
            cam.fieldOfView = 45f;

            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? "Logs");
            File.WriteAllBytes(file, tex.EncodeToPNG());
        }
    }
}
