#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emberline.Core;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Drives the real CameraRig through the framing scenarios and reports what
    /// the shot actually measures — Renzo's share of screen height, the camera's
    /// distance and its downward tilt. Renders one strip per preset so the three
    /// can be compared side by side. Writes Logs/cam_<preset>_<scenario>.png.
    /// </summary>
    public static class EmberCameraFraming
    {
        private const int W = 1600, H = 720;   // the A33's 20:9, so the test frames what ships

        private static int PresetArg()
        {
            var a = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < a.Length - 1; i++) if (a[i] == "-preset") return int.Parse(a[i + 1]);
            return 0;
        }

        public static void Run()
        {
            var preset = (CameraRig.Framing)PresetArg();
            var stage = new GameObject("Stage");

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(42f, 160f, 0f);
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.48f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 6f;
            ground.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Emberline/Toon")) { color = new Color(0.18f, 0.21f, 0.26f) };

            // The player, built from the live spec — never scaled for the shot.
            var player = new GameObject("Renzo");
            EmberCharacterFactory.Build(player, EmberCharacterFactory.PlayerSpec());
            Pose(player, EmberCharacterFactory.PlayerSpec(), RigPose.Idle);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.11f, 0.15f);
            var rig = camGo.AddComponent<CameraRig>();
            rig.ApplyPreset(preset);
            rig.SetTarget(player.transform);

            Debug.Log($"[CAM] preset={preset} restDistance={rig.RestDistance:F2}m " +
                      $"pitch={rig.RestPitchDegrees:F1}deg fov={cam.fieldOfView:F0}");

            // (label, enemy specs, offsets from the player, boss framing, wall)
            var scenes = new (string label, (EmberCharacterFactory.Spec, Vector3)[] foes, bool boss, bool wall)[]
            {
                ("1_alone", System.Array.Empty<(EmberCharacterFactory.Spec, Vector3)>(), false, false),
                ("4_one_raider", new[] { (EmberCharacterFactory.Bandit(), new Vector3(0.4f, 0, 3.2f)) }, false, false),
                ("5_samurai", new[] { (EmberCharacterFactory.Samurai(), new Vector3(0f, 0, 3.4f)) }, false, false),
                ("6_boss", new[] { (EmberCharacterFactory.Goro(), new Vector3(0f, 0, 4.2f)) }, true, false),
                ("7_surrounded", new[]
                {
                    (EmberCharacterFactory.Bandit(), new Vector3(-2.2f, 0, 2.6f)),
                    (EmberCharacterFactory.Assassin(), new Vector3(2.4f, 0, 2.9f)),
                    (EmberCharacterFactory.Samurai(), new Vector3(0.2f, 0, 4.0f)),
                }, false, false),
                ("8_duel", new[] { (EmberCharacterFactory.NamedFoe("drownedguardian"), new Vector3(0f, 0, 4.0f)) }, true, false),
                ("9_narrow", new[] { (EmberCharacterFactory.Bandit(), new Vector3(0f, 0, 3.0f)) }, false, true),
            };

            foreach (var (label, foes, boss, wall) in scenes)
            {
                var spawned = new System.Collections.Generic.List<GameObject>();
                foreach (var (spec, at) in foes)
                {
                    var e = new GameObject(spec.name);
                    e.transform.position = at;
                    e.transform.rotation = Quaternion.Euler(0, 180f, 0);
                    if (EmberCharacterFactory.Build(e, spec)) Pose(e, spec, RigPose.Idle);
                    spawned.Add(e);
                }
                GameObject blocker = null;
                if (wall)
                {
                    // A wall right where the camera wants to sit, to exercise the
                    // pull-in rather than let it clip.
                    blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blocker.transform.position = new Vector3(0.55f, 1.5f, -2.6f);
                    blocker.transform.localScale = new Vector3(6f, 3f, 0.4f);
                }

                rig.BossFraming = boss;
                // Edit mode does not auto-sync new colliders into the physics
                // scene, so the camera's spherecast would miss the wall entirely.
                Physics.SyncTransforms();
                Settle(rig, camGo);
                var pct = ScreenShare(cam, player);
                var dist = Vector3.Distance(camGo.transform.position, player.transform.position);
                var tilt = -camGo.transform.eulerAngles.x;
                if (tilt < -180f) tilt += 360f;
                Debug.Log($"[CAM] {preset}/{label,-14} renzo={pct * 100f:F0}% of height  " +
                          $"dist={dist:F2}m  tilt={-tilt:F1}deg  camY={camGo.transform.position.y:F2}m");
                Shoot(cam, $"Logs/cam_{(int)preset}_{label}.png");

                foreach (var e in spawned) Object.DestroyImmediate(e);
                if (blocker != null) Object.DestroyImmediate(blocker);
            }

            Object.DestroyImmediate(stage);
            Debug.Log("[CAM] DONE");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Edit mode has no delta time, so the smoothed follow would never move.
        /// Drive the rig's own rest placement instead — the same code path
        /// gameplay uses on the first frame after acquiring a target.
        /// </summary>
        private static void Settle(CameraRig rig, GameObject camGo) => rig.SnapBehindTarget();

        /// <summary>
        /// Fraction of screen height Renzo's *body* occupies — feet to crown, not
        /// the skinned bounds, which include the sword's arc and over-report the
        /// framing by roughly twenty points.
        /// </summary>
        private static float ScreenShare(UnityEngine.Camera cam, GameObject go)
        {
            var feet = cam.WorldToScreenPoint(go.transform.position);
            var head = cam.WorldToScreenPoint(go.transform.position + Vector3.up * 1.8f);
            if (feet.z <= 0f || head.z <= 0f) return 0f;
            return Mathf.Clamp01(Mathf.Abs(head.y - feet.y) / cam.pixelHeight);
        }

        private static void Pose(GameObject go, EmberCharacterFactory.Spec spec, RigPose pose)
        {
            var anim = go.GetComponentInChildren<Animator>();
            var clip = EmberCharacterFactory.ResolveClip(spec, pose);
            if (anim != null && clip != null) clip.SampleAnimation(anim.gameObject, clip.length * 0.3f);
        }

        private static void Shoot(UnityEngine.Camera cam, string file)
        {
            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
        }
    }
}
#endif
