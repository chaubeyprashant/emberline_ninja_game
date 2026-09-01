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

        /// <summary>Whether the builder overrides materials or trusts the model.</summary>
        public enum MaterialMode
        {
            /// <summary>One shared palette material on every renderer (placeholders).</summary>
            PaletteOverride,

            /// <summary>Keep the materials authored on the FBX (realistic PBR).</summary>
            KeepAuthored,
        }

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
            public Vector3 propScale = Vector3.one; // reshape a prop (sword → spear shaft)
            public string[] keepEmbedded;         // embedded hand-slot meshes to keep visible
            public System.Func<Color, Color> recolor; // palette remap for identity
            public Dictionary<RigPose, string> clips;

            /// <summary>
            /// How the model's surfaces are authored. The KayKit placeholder set
            /// shares one palette texture, so every renderer is forced onto a
            /// single material. A realistic PBR character carries its own
            /// albedo/normal/ORM per slot and must keep them — see
            /// docs/ASSET_SPECIFICATIONS.md §9 step 2.
            /// </summary>
            public MaterialMode materialMode = MaterialMode.PaletteOverride;

            /// <summary>
            /// Socket bone names, in preference order. Empty falls back to the
            /// KayKit-era search (handslot.r / handslot_r / hand.r / handr), so
            /// existing specs keep working and a new rig declares its own.
            /// </summary>
            public string[] socketRight, socketLeft;
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

        // ------------------------------------------------------------- story cast
        //
        // PLACEHOLDER CAST. Every spec below is an existing KayKit adventurer
        // recoloured and rescaled to stand in for a story character. They are
        // marked as placeholders on purpose (rule 10): the meshes are chibi, have
        // no face rig, and children are adults scaled down, which reads wrong in
        // any shot that holds on a face. Replacement specifications are in
        // docs/ART_DIRECTION.md §5 — until those land, the opening is staged in
        // silhouette and over-the-shoulder so the stand-ins are never scrutinised.

        /// <summary>Ren at seventeen — unhooded, lighter, no lantern yet.</summary>
        public static Spec YoungRen() => new()
        {
            name = "YoungRenModel",       // PLACEHOLDER: Rogue, scaled and recoloured
            fbx = $"{AdvDir}/Rogue.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.66f,
            propRight = "sword_1handed",
            recolor = c => Dominant(c.g, c.r, c.b) ? Toward(c, new Color(0.28f, 0.31f, 0.36f)) : c,
            clips = OneHanded(),
        };

        /// <summary>The village swordmaster. Broader, warmer, carries the sword.</summary>
        public static Spec Father() => new()
        {
            name = "FatherModel",         // PLACEHOLDER: Knight
            fbx = $"{AdvDir}/Knight.fbx",
            texture = $"{AdvDir}/knight_texture.png",
            height = 1.86f,
            propRight = "sword_1handed",
            trail = true,
            recolor = c => Toward(c, new Color(0.38f, 0.30f, 0.24f)),
            clips = OneHanded(),
        };

        /// <summary>The healer. Robed, pale, unarmed.</summary>
        public static Spec Mother() => new()
        {
            name = "MotherModel",         // PLACEHOLDER: Rogue, pale
            // Not the Mage mesh: its pointed hat reads as a wizard, which is a
            // different character from a village healer.
            fbx = $"{AdvDir}/Rogue.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.72f,
            recolor = c => Toward(c, new Color(0.72f, 0.68f, 0.60f)),
            clips = OneHanded(),
        };

        /// <summary>Aiko as a child. Small, bright, unafraid.</summary>
        public static Spec AikoChild() => new()
        {
            name = "AikoChildModel",      // PLACEHOLDER: Rogue at child scale
            fbx = $"{AdvDir}/Rogue.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.24f,
            recolor = c => Toward(c, new Color(0.78f, 0.42f, 0.38f)),
            clips = OneHanded(),
        };

        /// <summary>Aiko years later. The same red, drained out of her.</summary>
        public static Spec AikoOlder() => new()
        {
            name = "AikoOlderModel",      // PLACEHOLDER: Rogue, desaturated
            fbx = $"{AdvDir}/Rogue.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.62f,
            recolor = c => Toward(c, new Color(0.44f, 0.36f, 0.36f)),
            clips = OneHanded(),
        };

        /// <summary>The child under the cart. Smallest silhouette in the game.</summary>
        public static Spec VillageChild() => new()
        {
            name = "VillageChildModel",   // PLACEHOLDER: Rogue at child scale
            fbx = $"{AdvDir}/Rogue.fbx",
            texture = $"{AdvDir}/rogue_texture.png",
            height = 1.12f,
            recolor = c => Toward(c, new Color(0.62f, 0.58f, 0.50f)),
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

        /// <summary>Axe raider: a heavier bandit built on the Knight frame.</summary>
        public static Spec RaiderAxe()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Chop";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Slice";
            clips[RigPose.Strike3] = "2H_Melee_Attack_Spin";
            clips[RigPose.Hurt] = "Hit_B";
            return new Spec
            {
                name = "RaiderAxeModel",
                fbx = $"{AdvDir}/Knight.fbx",
                texture = $"{AdvDir}/knight_texture.png",
                height = 1.95f,
                propRight = "axe_2handed",
                // Raider colours: rust and soot. Saturation test, not a channel
                // test — the knight palette is mostly desaturated steel, so
                // "blue is dominant" almost never fires on it.
                recolor = c => Sat(c) > 0.18f
                    ? Toward(c, new Color(0.46f, 0.24f, 0.15f)) : c,
                clips = clips,
            };
        }

        /// <summary>
        /// Pike guard: the two-handed sword stretched into a spear shaft, which is
        /// the closest the KayKit set gets to a polearm without new art.
        /// </summary>
        public static Spec PikeGuard()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Stab";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Stab";
            clips[RigPose.Strike3] = "2H_Melee_Attack_Chop";
            return new Spec
            {
                name = "PikeGuardModel",
                fbx = $"{AdvDir}/Knight.fbx",
                texture = $"{AdvDir}/knight_texture.png",
                height = 1.9f,
                propRight = "sword_2handed",
                propScale = new Vector3(0.55f, 2.1f, 0.55f), // sword → spear shaft
                // Toll-guard livery: cold green over the knight's steel.
                recolor = c => Sat(c) > 0.18f
                    ? Toward(c, new Color(0.20f, 0.40f, 0.30f)) : c,
                clips = clips,
            };
        }

        /// <summary>Bomber: a mage frame that lobs charges instead of casting.</summary>
        public static Spec Bomber()
        {
            var clips = OneHanded();
            clips[RigPose.Strike1] = "Spellcast_Shoot";
            clips[RigPose.Strike2] = "Spellcast_Shoot";
            clips[RigPose.Windup] = "Spellcast_Raise";
            return new Spec
            {
                name = "BomberModel",
                fbx = $"{AdvDir}/Mage.fbx",
                texture = $"{AdvDir}/mage_texture.png",
                height = 1.7f,
                propRight = "smokebomb",
                // Powder-stained: the mage's robe goes dull ochre.
                recolor = c => Dominant(c.b, c.r, c.g)
                    ? Toward(c, new Color(0.44f, 0.36f, 0.18f)) : c,
                clips = clips,
            };
        }

        /// <summary>Assassin: light, fast, twin blades. Skeleton rogue frame.</summary>
        public static Spec Assassin()
        {
            var clips = OneHanded();
            clips[RigPose.Strike1] = "Dualwield_Melee_Attack_Slice";
            clips[RigPose.Strike2] = "Dualwield_Melee_Attack_Stab";
            clips[RigPose.Strike3] = "Dualwield_Melee_Attack_Chop";
            clips[RigPose.Dash] = "Dodge_Left";
            return new Spec
            {
                name = "AssassinModel",
                fbx = $"{SkelDir}/Skeleton_Rogue.fbx",
                texture = $"{SkelDir}/skeleton_texture.png",
                height = 1.74f,
                propRight = "dagger",
                propLeft = "dagger",
                // Ash-grey wraps with a bruised violet edge — reads fast and cold.
                recolor = c => Sat(c) > 0.2f ? Toward(c, new Color(0.28f, 0.24f, 0.34f)) : c,
                clips = clips,
            };
        }

        /// <summary>Samurai: heavy guard, two-handed blade, deliberate.</summary>
        public static Spec Samurai()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Chop";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Slice";
            clips[RigPose.Windup] = "2H_Melee_Idle";
            clips[RigPose.Hurt] = "Hit_B";
            return new Spec
            {
                name = "SamuraiModel",
                fbx = $"{AdvDir}/Knight.fbx",
                texture = $"{AdvDir}/knight_texture.png",
                height = 1.92f,
                trail = true,
                propRight = "sword_2handed",
                // Lacquered oxblood over dark steel.
                recolor = c => Sat(c) > 0.18f ? Toward(c, new Color(0.38f, 0.12f, 0.13f)) : c,
                clips = clips,
            };
        }

        /// <summary>Rogue Ninja: the mirror of Renzo — hooded, quick, single blade.</summary>
        public static Spec RogueNinja()
        {
            var clips = OneHanded();
            clips[RigPose.Dash] = "Dodge_Forward";
            clips[RigPose.Strike2] = "1H_Melee_Attack_Stab";
            return new Spec
            {
                name = "RogueNinjaModel",
                fbx = $"{AdvDir}/RogueHooded.fbx",
                texture = $"{AdvDir}/rogue_texture.png",
                height = 1.78f,
                propRight = "dagger",
                // Near-black with a cold cast: Renzo's silhouette, hostile palette.
                recolor = c => Sat(c) > 0.15f ? Toward(c, new Color(0.13f, 0.15f, 0.19f)) : c,
                clips = clips,
            };
        }

        /// <summary>Elite Warrior: the biggest non-boss silhouette on the field.</summary>
        public static Spec EliteWarrior()
        {
            var clips = OneHanded();
            clips[RigPose.Idle] = "2H_Melee_Idle";
            clips[RigPose.Strike1] = "2H_Melee_Attack_Chop";
            clips[RigPose.Strike2] = "2H_Melee_Attack_Spinning";
            clips[RigPose.Strike3] = "2H_Melee_Attack_Spin";
            clips[RigPose.Hurt] = "Hit_B";
            clips[RigPose.Dead] = "Death_B";
            return new Spec
            {
                name = "EliteWarriorModel",
                fbx = $"{SkelDir}/Skeleton_Warrior.fbx",
                texture = $"{SkelDir}/skeleton_texture.png",
                height = 2.2f,
                trail = true,
                propRight = "axe_2handed",
                // Tarnished brass over bone — a captain's kit, long unpolished.
                recolor = c => Sat(c) > 0.2f ? Toward(c, new Color(0.42f, 0.33f, 0.14f)) : c,
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
            // The visual swap boundary. Everything the artist owns lives under
            // here; gameplay components stay on the parent and address this by
            // component, never by name.
            instance.name = "VisualRoot";
            instance.transform.SetParent(root.transform, false);
            var visual = instance.AddComponent<VisualRoot>();
            visual.modelId = spec.name;

            // Normalize height.
            var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned.Length > 0)
            {
                var bounds = skinned[0].bounds;
                foreach (var r in skinned) bounds.Encapsulate(r.bounds);
                if (bounds.size.y > 0.01f)
                    instance.transform.localScale = Vector3.one * (spec.height / bounds.size.y);
            }
            visual.normalisedHeight = spec.height;
            {
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

            // Placeholder models share one palette texture, so every renderer is
            // forced onto a single material. A model authored to
            // docs/ASSET_SPECIFICATIONS.md brings its own PBR set and keeps it —
            // overriding would throw away the normal and ORM maps that are the
            // entire point of the replacement.
            var mat = spec.materialMode == MaterialMode.PaletteOverride
                ? CharacterMaterial(spec) : null;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (mat != null)
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                }
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            // Animator + generated controller.
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildController(spec);
            animator.applyRootMotion = false;

            visual.socketRight = FindSocket(instance, "r", spec.socketRight);
            visual.socketLeft = FindSocket(instance, "l", spec.socketLeft);

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
            // Characters render on the PBR surface shader now; ghosts keep the
            // translucent path since they are not meant to read as solid matter.
            var shader = spec.ghost ? Shader.Find("Emberline/Ghost") : SurfaceKit.SurfaceShader;
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
            if (spec.ghost)
            {
                mat.color = new Color(spec.tint.r, spec.tint.g, spec.tint.b, spec.ghostAlpha);
            }
            else
            {
                // Cloth is the dominant surface on every character; skin and metal
                // ride the same atlas, so one profile has to serve the whole body.
                SurfaceKit.Apply(mat, Surface.Cloth, Color.white);
            }
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
            AttachProp(instance, spec.propRight, "r", spec.trail, spec.name, spec.propScale,
                spec.socketRight);
            AttachProp(instance, spec.propLeft, "l", spec.trail, spec.name, spec.propScale,
                spec.socketLeft);
        }

        /// <summary>
        /// Resolve a weapon socket. A model declares its own bone names; when it
        /// declares none we fall back to the KayKit-era search so the existing
        /// placeholder roster keeps building unchanged.
        /// </summary>
        private static Transform FindSocket(GameObject instance, string side, string[] declared)
        {
            var all = instance.GetComponentsInChildren<Transform>(true);

            if (declared != null)
                foreach (var want in declared)
                {
                    if (string.IsNullOrEmpty(want)) continue;
                    var hit = all.FirstOrDefault(t =>
                        string.Equals(t.name, want, System.StringComparison.OrdinalIgnoreCase));
                    if (hit != null) return hit;
                }

            return all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot." + side))
                   ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot" + side))
                   ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("hand." + side))
                   ?? all.FirstOrDefault(t => t.name.ToLowerInvariant() == "hand" + side);
        }

        /// <summary>
        /// Hangs one KayKit prop off a hand slot as `Prop_{name}_{side}`. Public so
        /// the bootstrap can pre-attach the whole weapon catalogue on the player —
        /// runtime weapon swapping just enables one set and disables the rest,
        /// since a build can't instantiate FBX assets on device.
        /// </summary>
        public static GameObject AttachProp(GameObject instance, string propName, string side,
            bool trail = false, string ownerName = "", Vector3? scale = null,
            string[] declaredSocket = null)
        {
            if (string.IsNullOrEmpty(propName)) return null;
            var slot = FindSocket(instance, side, declaredSocket);
            if (slot == null)
            {
                var all = instance.GetComponentsInChildren<Transform>(true);
                Debug.LogWarning($"[EmberFactory] {ownerName}: no hand slot '{side}' — bones: "
                    + string.Join(",", all.Where(t => t.name.ToLowerInvariant().Contains("hand"))
                        .Select(t => t.name)));
                return null;
            }

            var propAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropDir}/{propName}.fbx");
            if (propAsset == null) return null;
            var prop = (GameObject)PrefabUtility.InstantiatePrefab(propAsset);
            prop.name = $"Prop_{propName}_{side}";
            prop.transform.SetParent(slot, false);
            if (scale.HasValue) prop.transform.localScale = scale.Value;
            var propMat = PropMaterial();
            foreach (var r in prop.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = propMat;

            if (!trail || side != "r") return prop;
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(prop.transform, false);
            trailGo.transform.localPosition = new Vector3(0, 0.55f, 0);
            var trailR = trailGo.AddComponent<TrailRenderer>();
            trailR.time = 0.14f;
            trailR.startWidth = 0.34f;
            trailR.endWidth = 0.02f;
            trailR.material = new Material(Shader.Find("Emberline/Glow"));
            trailR.startColor = new Color(0.85f, 0.93f, 1f, 0.8f);
            trailR.endColor = new Color(1f, 0.45f, 0.28f, 0f);
            trailR.emitting = false;
            return prop;
        }

        private static Material PropMaterial()
        {
            var path = $"{OutDir}/Mat_Prop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(SurfaceKit.SurfaceShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = SurfaceKit.SurfaceShader;
            SurfaceKit.Apply(mat, Surface.Steel, new Color(0.55f, 0.58f, 0.64f));
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
