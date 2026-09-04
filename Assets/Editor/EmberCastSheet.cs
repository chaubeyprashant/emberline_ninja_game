#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emberline.Core;

namespace Emberline.EditorTools
{
    /// <summary>
    /// A contact sheet of the whole combat cast, close enough to judge whether
    /// two characters read as the same person. Two rows, front-lit, each posed
    /// through its own clip map. Writes Logs/cast_sheet.png.
    /// </summary>
    public static class EmberCastSheet
    {
        /// <summary>Which row to render, from -row on the command line (default 0).</summary>
        private static int args()
        {
            var a = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < a.Length - 1; i++) if (a[i] == "-row") return int.Parse(a[i + 1]);
            return 0;
        }

        public static void Run()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.color = new Color(1f, 0.95f, 0.9f);
            sun.transform.rotation = Quaternion.Euler(35f, 10f, 0f);
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.52f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 6f;
            ground.transform.position = new Vector3(0, 0, 0);
            ground.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Emberline/Toon")) { color = new Color(0.17f, 0.20f, 0.25f) };

            // row 2 = the nine-rung duel ladder, in the order the player fights it.
            var duels = new (string, EmberCharacterFactory.Spec, RigPose)[]
            {
                ("GORO", EmberCharacterFactory.Goro(), RigPose.Idle),
                ("PALE SHADE", EmberCharacterFactory.Shade(), RigPose.Idle),
                ("JIN", EmberCharacterFactory.Jin(), RigPose.Idle),
                ("KAGACHI", EmberCharacterFactory.Kagachi(), RigPose.Idle),
                ("CONVOY CAPT", EmberCharacterFactory.Samurai(), RigPose.Idle),
                ("THREE BLADES", EmberCharacterFactory.NamedFoe("threeblades"), RigPose.Idle),
                ("DROWNED GRD", EmberCharacterFactory.NamedFoe("drownedguardian"), RigPose.Idle),
                ("IRON GUARD", EmberCharacterFactory.NamedFoe("ironguard"), RigPose.Idle),
                ("HOSHU", EmberCharacterFactory.NamedFoe("finalcommander"), RigPose.Idle),
            };

            var cast = new (string, EmberCharacterFactory.Spec, RigPose)[]
            {
                ("RENZO", EmberCharacterFactory.PlayerSpec(), RigPose.Idle),
                ("RAIDER", EmberCharacterFactory.Bandit(), RigPose.Idle),
                ("ARCHER", EmberCharacterFactory.Archer(), RigPose.Windup),
                ("ASSASSIN", EmberCharacterFactory.Assassin(), RigPose.Idle),
                ("ROGUE NINJA", EmberCharacterFactory.RogueNinja(), RigPose.Idle),
                ("BOMBER", EmberCharacterFactory.Bomber(), RigPose.Idle),
                ("SHADE", EmberCharacterFactory.Shade(), RigPose.Idle),
                ("PIKE GUARD", EmberCharacterFactory.PikeGuard(), RigPose.Idle),
                ("SAMURAI", EmberCharacterFactory.Samurai(), RigPose.Idle),
                ("ELITE", EmberCharacterFactory.EliteWarrior(), RigPose.Idle),
                ("AXE RAIDER", EmberCharacterFactory.RaiderAxe(), RigPose.Idle),
                ("GORO", EmberCharacterFactory.Goro(), RigPose.Idle),
                ("JIN", EmberCharacterFactory.Jin(), RigPose.Idle),
                ("KAGACHI", EmberCharacterFactory.Kagachi(), RigPose.Idle),
            };

            var half = args();
            var perRow = half == 2 ? 9 : 7;
            var source = half == 2 ? duels : cast;
            for (var i = 0; i < source.Length; i++)
            {
                var (label, spec, pose) = source[i];
                if (half != 2 && i / perRow != half) continue;
                var col = i % perRow;
                var root = new GameObject(label);
                root.transform.position = new Vector3(col * (half == 2 ? 1.55f : 1.7f) - (half == 2 ? 6.2f : 5.1f), 0, 0);
                root.transform.rotation = Quaternion.identity;   // +Z faces the camera
                if (!EmberCharacterFactory.Build(root, spec)) { Debug.Log($"[SHEET] {label} FAILED"); continue; }
                var anim = root.GetComponentInChildren<Animator>();
                var clip = EmberCharacterFactory.ResolveClip(spec, pose);
                if (anim != null && clip != null) clip.SampleAnimation(anim.gameObject, clip.length * 0.3f);

                // SkeletalRig pushes spec.tint into a MaterialPropertyBlock in
                // Awake, which never runs in edit mode. Without this the sheet
                // shows every variant of a shared body as identical — which is
                // exactly the thing the sheet exists to catch.
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_Color", spec.ghost
                    ? new Color(spec.tint.r, spec.tint.g, spec.tint.b, spec.ghostAlpha)
                    : spec.tint);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    if (!r.name.StartsWith("Prop_")) r.SetPropertyBlock(mpb);
            }

            Shoot(new Vector3(0, 1.25f, half == 2 ? -9.0f : -7.4f), Quaternion.Euler(3f, 0, 0),
                $"Logs/cast_sheet{half}.png", 2400, 900);
            Debug.Log($"[SHEET] wrote Logs/cast_sheet{half}.png");
            EditorApplication.Exit(0);
        }

        private static void Shoot(Vector3 pos, Quaternion rot, string file, int w, int h)
        {
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = 46f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.14f);
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
        }
    }
}
#endif
