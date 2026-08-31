using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Emberline.Core;
using Emberline.Enemies;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Builds skeletal character prefabs from the imported KayKit FBX files:
    /// generated AnimatorController (RigPose-named states over the embedded
    /// clip library), toon material with palette texture, weapon props on hand
    /// slots, sword trail, normalized height, SkeletalRig wiring. Falls back to
    /// null when an FBX is missing so the bootstrap can keep NinjaRig instead.
    /// </summary>
    public static class EmberCharacterFactory
    {
        private const string AdvDir = "Assets/Art/Characters/Adventurers";
        private const string SkelDir = "Assets/Art/Characters/Skeletons";
        private const string PropDir = "Assets/Art/Characters/Props";
        private const string OutDir = "Assets/Prefabs/Characters";

        public class Spec
        {
            public string name;
            public string fbx;                    // model path
            public string texture;                // palette texture path
            public Color tint = Color.white;
            public float height = 1.8f;
            public bool ghost;
            public float ghostAlpha = 0.5f;
            public bool lantern;
            public bool trail;                    // weapon slash trail
            public string propRight, propLeft;    // external prop FBX names (no extension)
            public string[] keepEmbedded;         // embedded hand-slot meshes to keep visible
            public System.Func<Color, Color> recolor; // palette remap for identity
            public Dictionary<RigPose, string> clips;
        }

        /// <summary>Is `c` the dominant channel by a clear margin (cloth detection)?</summary>
        private static bool Dominant(float c, float a, float b) => c > a * 1.12f && c > b * 1.12f;

        /// <summary>Replace a pixel's hue keeping its relative brightness.</summary>
        private static Color Toward(Color src, Color target)
        {
            var lum = (src.r + src.g + src.b) / 3f;
            var scale = Mathf.Clamp(lum * 2.6f, 0.35f, 1.45f);
            return new Color(target.r * scale, target.g * scale, target.b * scale, src.a);
        }

        /// <summary>Saturation 0..1 — separates cloth/armor pixels from bone/skin/steel.</summary>
        private static float Sat(Color c)
        {
            var max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            var min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max <= 0.001f ? 0f : (max - min) / max;
        }

        private static Dictionary<RigPose, string> OneHanded() => new()
        {
            [RigPose.Idle] = "Idle",
            [RigPose.Run] = "Running_A",
            [RigPose.Strike1] = "1H_Melee_Attack_Slice_Diagonal",
            [RigPose.Strike2] = "1H_Melee_Attack_Slice_Horizontal",
            [RigPose.Strike3] = "1H_Melee_Attack_Chop",
            [RigPose.Cleave] = "2H_Melee_Attack_Spinning",
            [RigPose.Windup] = "Spellcast_Raise",
            [RigPose.Hurt] = "Hit_A",
            [RigPose.Dash] = "Dodge_Forward",
            [RigPose.Dead] = "Death_A",
            [RigPose.Spawn] = "Spawn_Ground",
            [RigPose.Taunt] = "Cheer",
        };

        public static Spec Renzo() => new()
        {
            name = "RenzoModel",
            fbx = $"{AdvDir}/RogueHooded.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.8f,
            lantern = true,
            trail = true,
            propRight = "sword_1handed",
            // Dark ninja: the rogue's green hood/tunic becomes ink-blue charcoal.
            recolor = c => Dominant(c.g, c.r, c.b) ? Toward(c, new Color(0.17f, 0.20f, 0.30f)) : c,
            clips = OneHanded(),
        };

        public static Spec Bandit()
        {
            var clips = OneHanded();
            clips[RigPose.Strike1] = "Dualwield_Melee_Attack_Slice";
            clips[RigPose.Strike2] = "Dualwield_Melee_Attack_Stab";
            clips[RigPose.Strike3] = "Dualwield_Melee_Attack_Chop";
            return new Spec
            {
                name = "BanditModel",
                fbx = $"{AdvDir}/Rogue.fbx",
                texture = $"{AdvDir}/rogue_texture.png",
                height = 1.72f,
                keepEmbedded = new[] { "Knife", "Knife_Offhand" }, // dual daggers, palette-matched
                // Ragged raider: greens become worn maroon leather.
                recolor = c => Dominant(c.g, c.r, c.b) ? Toward(c, new Color(0.42f, 0.22f, 0.15f)) : c,
                clips = clips,
            };
        }

        public static Spec Goro()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Chop";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Slice";
            clips[RigPose.Strike3] = "2H_Melee_Attack_Spinning";
            clips[RigPose.Cleave] = "2H_Melee_Attack_Spin";
            clips[RigPose.Hurt] = "Hit_B";
            clips[RigPose.Dead] = "Death_B";
            return new Spec
            {
                name = "GoroModel",
                fbx = $"{AdvDir}/Barbarian.fbx",
                texture = $"{AdvDir}/barbarian_texture.png",
                height = 2.45f,
                keepEmbedded = new[] { "2H_Axe" }, // palette-matched greataxe
                // Red-armored chief: blue/grey cloth becomes lacquered red.
                recolor = c => Dominant(c.b, c.r, c.g) || (c.b > 0.3f && Mathf.Abs(c.r - c.b) < 0.08f && c.r < 0.55f)
                    ? Toward(c, new Color(0.52f, 0.13f, 0.10f)) : c,
                clips = clips,
            };
        }

        public static Spec Kagachi()
        {
            var clips = OneHanded();
            clips[RigPose.Spawn] = "Skeletons_Awaken_Floor";
            clips[RigPose.Taunt] = "Taunt_Longer";
            clips[RigPose.Cleave] = "2H_Melee_Attack_Spinning";
            return new Spec
            {
                name = "KagachiModel",
                fbx = $"{SkelDir}/Skeleton_Warrior.fbx",
                texture = $"{SkelDir}/skeleton_texture.png",
                height = 2.1f,
                trail = true,
                propRight = "sword_1handed",
                // Serpent armor: anything with color becomes venom teal; bone stays bone.
                recolor = c => Sat(c) > 0.22f ? Toward(c, new Color(0.13f, 0.42f, 0.34f)) : c,
                clips = clips,
            };
        }

        public static Spec Jin()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Slice";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Chop";
            clips[RigPose.Strike3] = "2H_Melee_Attack_Spinning";
            clips[RigPose.Cleave] = "2H_Melee_Attack_Spin";
            return new Spec
            {
                name = "JinModel",
                fbx = $"{AdvDir}/Knight.fbx",
                texture = $"{AdvDir}/knight_texture.png",
                height = 1.85f,
                trail = true,
                keepEmbedded = new[] { "2H_Sword" }, // the storm blade itself
                // Storm-forged: colored plate turns thunderhead indigo.
                recolor = c => Sat(c) > 0.2f ? Toward(c, new Color(0.26f, 0.30f, 0.55f)) : c,
                clips = clips,
            };
        }

        public static Spec Archer()
        {
            var clips = OneHanded();
            clips[RigPose.Strike1] = "1H_Melee_Attack_Chop";     // panic bash
            clips[RigPose.Strike2] = "1H_Ranged_Shoot";          // EnemyBrain's shoot pose
            clips[RigPose.Windup] = "1H_Ranged_Aiming";
            clips[RigPose.Dash] = "Dodge_Backward";              // archers retreat
            return new Spec
            {
                name = "ArcherModel",
                fbx = $"{AdvDir}/Mage.fbx",
                texture = $"{AdvDir}/mage_texture.png",
                height = 1.7f,
                propRight = "crossbow_1handed",
                // Lantern Archer: robes go night-indigo, hood-dark.
                recolor = c => Sat(c) > 0.2f ? Toward(c, new Color(0.22f, 0.18f, 0.36f)) : c,
                clips = clips,
            };
        }

        public static Spec Shade()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "Unarmed_Idle";
            clips[RigPose.Strike1] = "Unarmed_Melee_Attack_Punch_A";
            clips[RigPose.Strike2] = "Unarmed_Melee_Attack_Punch_B";
            clips[RigPose.Strike3] = "Unarmed_Melee_Attack_Kick";
            clips[RigPose.Spawn] = "Skeletons_Awaken_Standing";
            clips[RigPose.Taunt] = "Taunt_Longer";
            return new Spec
            {
                name = "ShadeModel",
                fbx = $"{SkelDir}/Skeleton_Minion.fbx",
                texture = $"{SkelDir}/skeleton_texture.png",
                tint = new Color(0.55f, 0.72f, 0.88f),
                height = 1.62f,
                ghost = true,
                ghostAlpha = 0.55f,
                clips = clips,
            };
        }

        /// <summary>Builds the character visual under `root`. False → FBX missing, use NinjaRig.</summary>
        public static bool Build(GameObject root, Spec spec)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.fbx);
            if (model == null)
            {
                Debug.LogWarning($"[EmberFactory] Missing FBX {spec.fbx} — NinjaRig fallback for {spec.name}");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Model";
            instance.transform.SetParent(root.transform, false);

            // Normalize height.
            var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned.Length > 0)
            {
                var bounds = skinned[0].bounds;
                foreach (var r in skinned) bounds.Encapsulate(r.bounds);
                if (bounds.size.y > 0.01f)
                    instance.transform.localScale = Vector3.one * (spec.height / bounds.size.y);
            }

            // Embedded hand-slot weapon options: keep only the ones the spec asks for.
            var keep = spec.keepEmbedded ?? System.Array.Empty<string>();
            foreach (var mr in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                var underSlot = false;
                for (var t = mr.transform; t != null && t != instance.transform; t = t.parent)
                    if (t.name.ToLowerInvariant().Contains("handslot")) { underSlot = true; break; }
                if (underSlot && !keep.Contains(mr.gameObject.name))
                    mr.gameObject.SetActive(false);
            }

            // Toon material with the (optionally recolored) palette texture; capes,
            // hats and kept weapons share the same palette UVs as the body.
            var mat = CharacterMaterial(spec);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            // Animator + generated controller.
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildController(spec);
            animator.applyRootMotion = false;

            AttachProps(instance, spec);

            var rig = root.AddComponent<SkeletalRig>();
            rig.tint = spec.tint;
            rig.ghost = spec.ghost;
            rig.ghostAlpha = spec.ghostAlpha;
            rig.hasLantern = spec.lantern;
            var poseCount = System.Enum.GetValues(typeof(RigPose)).Length;
            rig.poseStates = new string[poseCount];
            rig.poseClipLengths = new float[poseCount];
            var clipLib = Clips(spec.fbx);
            for (var i = 0; i < poseCount; i++)
            {
                var pose = (RigPose)i;
                rig.poseStates[i] = pose.ToString();
                if (spec.clips.TryGetValue(pose, out var clipName)
                    && clipLib.TryGetValue(clipName, out var clip))
                    rig.poseClipLengths[i] = clip.length;
            }
            return true;
        }

        private static Material CharacterMaterial(Spec spec)
        {
            System.IO.Directory.CreateDirectory(OutDir);
            var path = $"{OutDir}/Mat_{spec.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(spec.ghost ? "Emberline/Ghost" : "Emberline/Toon");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            var tex = spec.recolor != null
                ? RecoloredTexture(spec.texture, spec.name, spec.recolor)
                : AssetDatabase.LoadAssetAtPath<Texture2D>(spec.texture);
            if (tex != null && mat.HasProperty("_MainTex")) mat.mainTexture = tex;
            mat.color = spec.ghost
                ? new Color(spec.tint.r, spec.tint.g, spec.tint.b, spec.ghostAlpha)
                : Color.white;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Writes a hue-remapped copy of a KayKit palette texture.</summary>
        private static Texture2D RecoloredTexture(string srcPath, string name,
            System.Func<Color, Color> map)
        {
            var outPath = $"{OutDir}/Tex_{name}.png";
            var importer = AssetImporter.GetAtPath(srcPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (src == null) return null;
            var pixels = src.GetPixels();
            for (var i = 0; i < pixels.Length; i++) pixels[i] = map(pixels[i]);
            var outTex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            outTex.SetPixels(pixels);
            outTex.Apply();
            System.IO.File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
            var ti = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (ti != null)
            {
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 256;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static void AttachProps(GameObject instance, Spec spec)
        {
            var all = instance.GetComponentsInChildren<Transform>(true);
            Transform Slot(string side)
            {
                return all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot." + side))
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot" + side))
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("hand." + side))
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant() == "hand" + side);
            }

            void Attach(string propName, string side)
            {
                if (string.IsNullOrEmpty(propName)) return;
                var slot = Slot(side);
                if (slot == null)
                {
                    Debug.LogWarning($"[EmberFactory] {spec.name}: no hand slot '{side}' — bones: "
                        + string.Join(",", all.Where(t => t.name.ToLowerInvariant().Contains("hand"))
                            .Select(t => t.name)));
                    return;
                }
                var propAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{propName}.fbx");
                if (propAsset == null) return;
                var prop = (GameObject)PrefabUtility.InstantiatePrefab(propAsset);
                prop.name = $"Prop_{propName}_{side}";
                prop.transform.SetParent(slot, false);
                var propMat = PropMaterial();
                foreach (var r in prop.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = propMat;

                if (spec.trail && side == "r")
                {
                    var trailGo = new GameObject("Trail");
                    trailGo.transform.SetParent(prop.transform, false);
                    trailGo.transform.localPosition = new Vector3(0, 0.55f, 0);
                    var trail = trailGo.AddComponent<TrailRenderer>();
                    trail.time = 0.14f;
                    trail.startWidth = 0.34f;
                    trail.endWidth = 0.02f;
                    trail.material = new Material(Shader.Find("Emberline/Glow"));
                    trail.startColor = new Color(0.85f, 0.93f, 1f, 0.8f);
                    trail.endColor = new Color(1f, 0.45f, 0.28f, 0f);
                    trail.emitting = false;
                }
            }

            Attach(spec.propRight, "r");
            Attach(spec.propLeft, "l");
        }

        private static Material PropMaterial()
        {
            var path = $"{OutDir}/Mat_Prop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Emberline/Toon"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = new Color(0.55f, 0.58f, 0.64f); // steel
            return mat;
        }

        private static Dictionary<string, AnimationClip> Clips(string fbxPath)
        {
            var dict = new Dictionary<string, AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview"))
                    dict[clip.name] = clip;
            return dict;
        }

        private static RuntimeAnimatorController BuildController(Spec spec)
        {
            System.IO.Directory.CreateDirectory(OutDir);
            var path = $"{OutDir}/Anim_{spec.name}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Move", AnimatorControllerParameterType.Float);
            controller.AddParameter("AtkSpeed", AnimatorControllerParameterType.Float);
            var sm = controller.layers[0].stateMachine;

            var lib = Clips(spec.fbx);
            AnimationClip Find(RigPose pose)
            {
                if (spec.clips.TryGetValue(pose, out var n) && lib.TryGetValue(n, out var c)) return c;
                return lib.TryGetValue("Idle", out var idle) ? idle : null;
            }

            // Locomotion blend tree (Idle ↔ Run on Move).
            var locomotion = sm.AddState("Locomotion");
            var tree = new BlendTree
            {
                name = "LocomotionTree",
                blendParameter = "Move",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Find(RigPose.Idle), 0f);
            tree.AddChild(Find(RigPose.Run), 1f);
            locomotion.motion = tree;
            sm.defaultState = locomotion;

            // One state per action pose, speed driven by AtkSpeed (0 = scrubbed).
            foreach (RigPose pose in System.Enum.GetValues(typeof(RigPose)))
            {
                if (pose is RigPose.Idle or RigPose.Run) continue;
                var clip = Find(pose);
                if (clip == null) continue;
                var state = sm.AddState(pose.ToString());
                state.motion = clip;
                state.speedParameterActive = true;
                state.speedParameter = "AtkSpeed";
            }
            return controller;
        }
    }
}
