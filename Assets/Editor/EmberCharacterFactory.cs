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

            /// <summary>
            /// One game-shader material per authored slot, each carrying that slot's
            /// own albedo (`slotTextures`, matched by material-name prefix; unmapped
            /// slots fall back to `texture`). For multi-part Mixamo bodies — hair,
            /// eyes, clothing — where a single palette would paint everything alike.
            /// </summary>
            ConvertAuthored,
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

            /// <summary>
            /// Extra FBX paths to harvest animation clips from, unioned over the
            /// model's own. A Mixamo character ships no animations — the motion
            /// comes from separate downloads that retarget through the humanoid
            /// avatar — so the clip library can no longer be just `fbx`.
            /// </summary>
            public string[] clipSources;

            /// <summary>
            /// Grip correction for a prop. KayKit's handslot empties already
            /// carry the grip pose, so these stay zero there; a Mixamo hand bone
            /// is the wrist joint, so a weapon needs an offset to sit in the palm.
            /// </summary>
            public Vector3 propOffsetPos = Vector3.zero;
            public Vector3 propOffsetRot = Vector3.zero;

            /// <summary>ConvertAuthored: authored material name (prefix match) → albedo path.</summary>
            public Dictionary<string, string> slotTextures;

            /// <summary>
            /// Child renderers to deactivate by name fragment — eyelashes, earrings,
            /// an embedded weapon mesh the prop system replaces. Deactivated, not
            /// merely disabled, so after-image bakes and ghosting skip them too.
            /// </summary>
            public string[] hideRenderers;
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
            // Combat 2.0: one readable clip per attack category.
            [RigPose.Stab] = "1H_Melee_Attack_Stab",
            [RigPose.Sweep] = "1H_Melee_Attack_Slice_Horizontal",
            [RigPose.Block] = "Blocking",
            [RigPose.BlockHit] = "Block_Hit",
            [RigPose.SideStep] = "Dodge_Left",
            [RigPose.Backstep] = "Dodge_Backward",
            [RigPose.Kick] = "Unarmed_Melee_Attack_Kick",
            [RigPose.Throw] = "Throw",
            [RigPose.Jump] = "Jump_Full_Short",
            [RigPose.Charge] = "Running_A",
            [RigPose.Delayed] = "Spellcast_Long",
        };

        /// <summary>Two-handed weapons: the stab and sweep come from the 2H set.</summary>
        private static void TwoHandedCombatClips(Dictionary<RigPose, string> clips)
        {
            clips[RigPose.Stab] = "2H_Melee_Attack_Stab";
            clips[RigPose.Sweep] = "2H_Melee_Attack_Spin";
        }

        /// <summary>Dual wield: the stab from the dual set.</summary>
        private static void DualWieldCombatClips(Dictionary<RigPose, string> clips)
        {
            clips[RigPose.Stab] = "Dualwield_Melee_Attack_Stab";
            clips[RigPose.Sweep] = "Dualwield_Melee_Attack_Slice";
        }

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

        // ------------------------------------------------------- Mixamo (candidate)
        //
        // A replacement-cast candidate built to docs/ART_DIRECTION.md §4.1: adult
        // athletic proportions, one skinned mesh, and a Humanoid rig whose
        // skeleton every other Mixamo character shares — which is the property
        // §4.1 requires so one animation set drives the whole cast.

        private const string MixDir = "Assets/Art/Characters/Mixamo";

        /// <summary>Every Mixamo pose clip, one file per take, keyed by pose name.</summary>
        public static string[] MixamoClipSources()
        {
            var names = new[] { "Idle", "Run", "Strike1", "Strike2", "Strike3", "Cleave", "Stab",
                "Sweep", "Hurt", "Dead", "Block", "BlockHit", "Kick", "Jump", "Windup", "Delayed",
                "Throw", "SideStep", "Backstep", "Spawn", "Taunt" };
            var paths = new string[names.Length];
            for (var i = 0; i < names.Length; i++) paths[i] = $"{MixDir}/Anims/{names[i]}.fbx";
            return paths;
        }

        /// <summary>Pose map for the Mixamo set. Files are named after their pose,
        /// so this is near-identity; the two takes the pack lacks borrow a sibling.</summary>
        public static Dictionary<RigPose, string> MixamoClips() => new()
        {
            [RigPose.Idle] = "Idle",
            [RigPose.Run] = "Run",
            [RigPose.Strike1] = "Strike1",
            [RigPose.Strike2] = "Strike2",
            [RigPose.Strike3] = "Strike3",
            [RigPose.Cleave] = "Cleave",
            [RigPose.Stab] = "Stab",
            [RigPose.Sweep] = "Sweep",
            [RigPose.Hurt] = "Hurt",
            [RigPose.Dead] = "Dead",
            [RigPose.Block] = "Block",
            [RigPose.BlockHit] = "BlockHit",
            [RigPose.Kick] = "Kick",
            [RigPose.Jump] = "Jump",
            [RigPose.Windup] = "Windup",
            [RigPose.Delayed] = "Delayed",
            [RigPose.Throw] = "Throw",
            [RigPose.SideStep] = "SideStep",
            [RigPose.Backstep] = "Backstep",
            [RigPose.Spawn] = "Spawn",
            [RigPose.Taunt] = "Taunt",
            [RigPose.Dash] = "Backstep",   // the pack has no dedicated dodge-forward
            [RigPose.Charge] = "Run",
        };

        /// <summary>
        /// The spec the player is built from. One switch, so the replacement cast
        /// can be tried on device and reverted without touching the bootstrap:
        /// return Renzo() for the KayKit placeholder, MixamoRenzo() for the
        /// realistic body described in docs/ART_DIRECTION.md §4.1.
        /// </summary>
        public static Spec PlayerSpec() => MixamoRenzo();

        /// <summary>Renzo on the Mixamo ninja body — the replacement-cast candidate.</summary>
        public static Spec MixamoRenzo() => new()
        {
            name = "MixamoRenzoModel",
            fbx = $"{MixDir}/MixamoNinja.fbx",
            height = 1.8f,
            lantern = true,
            trail = true,
            propRight = "sword_1handed",
            // Render through the game's own surface shader, not Unity's Standard,
            // so the character takes the same lighting as everything else in the
            // scene. On Standard it read washed out against the night arenas.
            // The model has a single material slot and one albedo, so the
            // one-material path fits it exactly.
            texture = $"{MixDir}/Textures/Ch24_1001_Diffuse.png",
            materialMode = MaterialMode.PaletteOverride,
            socketRight = new[] { "mixamorig:RightHand" },
            socketLeft = new[] { "mixamorig:LeftHand" },
            // The wrist joint is not the palm: nudge the grip forward along the
            // hand and roll the blade upright.
            // The wrist joint is not a grip: rotate the blade out of the fist and
            // lift it into the palm. Chosen from the sweep in Logs/grip_sweep.png.
            propOffsetPos = new Vector3(0f, 0.02f, 0f),
            propOffsetRot = new Vector3(0f, 0f, -90f),
            // The KayKit sword is sized for chibi hands; shrink it for a realistic one.
            propScale = new Vector3(0.62f, 0.62f, 0.62f),
            clipSources = MixamoClipSources(),
            clips = MixamoClips(),
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

        // ------------------------------------------------------------- enemy cast
        //
        // Ten Mixamo bodies cover the player and thirteen enemy kinds, all on the
        // one Mixamo skeleton so the 21-clip set drives every one of them
        // (docs/ENEMY_CHARACTER_SELECTION.md). The rule is that no two enemies
        // which can share a wave share a body. Where a body is reused it is only
        // between characters separated by something stronger than colour:
        //
        //   Ninja      Renzo / Rogue Ninja   — a deliberate mirror, cold vs warm
        //   Akai       Raider / Shade        — the Shade is translucent
        //   Kachujin   Samurai / Pike Guard  — greatsword vs spear silhouette
        //   Brute      Goro / Axe Raider     — 2.25 m bare-chested vs 1.98 m sooted
        //
        // Tints multiply the albedo in Emberline/Surface, so a value above 1 is a
        // real brightening, not a wash. The earlier pass used 0.5-0.8 greys, which
        // only darkened already-dark textures and left the cast looking alike.

        private static string MixTex(string body, string file) => $"{MixDir}/Textures/{body}/{file}";

        /// <summary>The fields every Mixamo-bodied spec shares: rig, clips, sockets, grip.</summary>
        private static Spec Mixamo(Spec s, string body, string albedo, float propScale = 0.62f)
        {
            s.fbx = $"{MixDir}/{body}.fbx";
            s.texture = MixTex(body, albedo);
            s.materialMode = s.slotTextures != null ? MaterialMode.ConvertAuthored : MaterialMode.PaletteOverride;
            s.socketRight = new[] { "mixamorig:RightHand" };
            s.socketLeft = new[] { "mixamorig:LeftHand" };
            s.propOffsetPos = new Vector3(0f, 0.02f, 0f);
            s.propOffsetRot = new Vector3(0f, 0f, -90f);
            s.propScale = Vector3.one * propScale;
            s.clipSources = MixamoClipSources();
            s.clips ??= MixamoClips();
            return s;
        }

        /// <summary>Brute's parts worth drawing; the rest is jewellery and an embedded axe.</summary>
        private static Dictionary<string, string> BruteSlots() => new()
        {
            ["Body_MAT"] = MixTex("MixamoBrute", "MaleBruteA_Body_diffuse.png"),
            ["EyeSpec"] = MixTex("MixamoBrute", "MaleBruteA_Body_diffuse.png"),
            ["MaleBruteA_Bottom"] = MixTex("MixamoBrute", "MaleBruteA_Bottom_diffuse1.jpg"),
            ["MaleBruteA_Hair"] = MixTex("MixamoBrute", "MaleBruteA_Hair_diffuse.png"),
            ["MaleBruteA_Shoes"] = MixTex("MixamoBrute", "MaleBruteA_Shoes_diffuse1.jpg"),
        };
        private static readonly string[] BruteHide = { "BattleAxe", "Earrings", "Eyelashes", "Moustache" };

        private static Dictionary<string, string> KachujinSlots() => new()
        {
            ["kachujin_MAT"] = MixTex("MixamoKachujin", "Kachujin_diffuse.png"),
            ["kachujin_MAT_"] = MixTex("MixamoKachujin", "Kachujin_diffuse_body.png"),
        };

        /// <summary>
        /// Erika's mesh names are shuffled against her materials — the renderer
        /// called "Eyelashes_Mesh" carries 12.5k triangles. Map by what each
        /// material means, not by the mesh it sits on: `Body_MAT` is skin and the
        /// leftover `Akai_MAT` is the leather. Getting this pair the wrong way
        /// round paints the armour with the skin atlas and she renders nude.
        /// </summary>
        private static Dictionary<string, string> ErikaSlots() => new()
        {
            ["Akai_MAT"] = MixTex("MixamoErika", "Erika_Archer_Clothes_diffuse.png"),
            ["Body_MAT"] = MixTex("MixamoErika", "FemaleFitA_Body_diffuse.png"),
            ["eyelash_MAT"] = MixTex("MixamoErika", "FemaleFitA_eyelash_diffuse.png"),
            ["EyeSpec_MAT"] = MixTex("MixamoErika", "FemaleFitA_Body_diffuse.png"),
        };

        private static Dictionary<string, string> VampireSlots() => new()
        {
            ["Vampire_MAT1"] = MixTex("MixamoVampire", "Vampire_diffuse.png"),
            ["Vampire_MAT_Transparent"] = MixTex("MixamoVampire", "Vampire_diffuse_transparent.png"),
        };

        /// <summary>Raider: the common low-rank warrior — hooded, worn leather, twin daggers.</summary>
        public static Spec Bandit() => Mixamo(new Spec
        {
            name = "BanditModel",
            height = 1.72f,
            tint = new Color(1.05f, 0.72f, 0.45f),   // warm tanned leather
            propRight = "dagger",
            propLeft = "dagger",
        }, "MixamoAkai", "akai_diffuse.png");

        /// <summary>Goro: the toll-captain — the biggest body on the roof, bare-chested, greataxe.</summary>
        public static Spec Goro() => Mixamo(new Spec
        {
            name = "GoroModel",
            height = 2.25f,
            tint = new Color(1.20f, 0.82f, 0.68f),   // firelit, warm — the Axe Raider is cold
            slotTextures = BruteSlots(),
            hideRenderers = BruteHide,
            propRight = "axe_2handed",
        }, "MixamoBrute", "MaleBruteA_Body_diffuse.png", propScale: 0.78f);

        /// <summary>Kagachi / Kagehira: the warlord — spiked plate and a long coat, unique body.</summary>
        public static Spec Kagachi() => Mixamo(new Spec
        {
            name = "KagachiModel",
            height = 2.1f,
            trail = true,
            propRight = "sword_1handed",
        }, "MixamoGanfaul", "Ganfaul_diffuse.png");

        /// <summary>Jin Kurogane: the storm blade — horned ornate armour, greatsword, unique body.</summary>
        public static Spec Jin() => Mixamo(new Spec
        {
            name = "JinModel",
            height = 1.85f,
            trail = true,
            propRight = "sword_2handed",
        }, "MixamoNightshade", "Nightshade_diffuse.png", propScale: 0.7f);

        /// <summary>Weaver / archer: a hooded archer in dark leather, quiver on her back.</summary>
        public static Spec Archer()
        {
            var clips = MixamoClips();
            clips[RigPose.Windup] = "Windup";     // the aim
            clips[RigPose.Strike2] = "Throw";     // EnemyBrain's shoot pose
            clips[RigPose.Dash] = "Backstep";     // archers retreat
            return Mixamo(new Spec
            {
                name = "ArcherModel",
                height = 1.68f,
                slotTextures = ErikaSlots(),
                propRight = "crossbow_1handed",
                clips = clips,
            }, "MixamoErika", "Erika_Archer_Clothes_diffuse.png");
        }

        /// <summary>Axe raider: Goro's body, shorter and sooted, swinging the same greataxe.</summary>
        public static Spec RaiderAxe() => Mixamo(new Spec
        {
            name = "RaiderAxeModel",
            height = 1.98f,
            tint = new Color(0.42f, 0.45f, 0.58f),   // soot and ash, cold against Goro's warmth
            slotTextures = BruteSlots(),
            hideRenderers = BruteHide,
            propRight = "axe_2handed",
        }, "MixamoBrute", "MaleBruteA_Body_diffuse.png", propScale: 0.78f);

        /// <summary>Pike guard: the ronin in garrison steel-blue, greatsword stretched to a spear.</summary>
        public static Spec PikeGuard()
        {
            var clips = MixamoClips();
            clips[RigPose.Strike1] = "Stab";
            clips[RigPose.Strike2] = "Stab";
            var s = Mixamo(new Spec
            {
                name = "PikeGuardModel",
                height = 1.95f,
                tint = new Color(0.40f, 0.66f, 1.30f),   // cold garrison steel against the Samurai's red
                slotTextures = KachujinSlots(),
                propRight = "sword_2handed",
                clips = clips,
            }, "MixamoKachujin", "Kachujin_diffuse.png");
            s.propScale = new Vector3(0.36f, 1.35f, 0.36f);   // sword → spear shaft
            return s;
        }

        /// <summary>Powder carrier: a cloaked, hooded saboteur with the charge in hand.</summary>
        public static Spec Bomber()
        {
            var clips = MixamoClips();
            clips[RigPose.Strike1] = "Throw";
            clips[RigPose.Strike2] = "Throw";
            clips[RigPose.Windup] = "Windup";
            return Mixamo(new Spec
            {
                name = "BomberModel",
                height = 1.70f,
                tint = new Color(0.40f, 0.38f, 0.48f),   // the pale cloak taken down to night slate
                propRight = "smokebomb",
                clips = clips,
            }, "MixamoPirate", "void_diffuse.png");
        }

        /// <summary>Assassin: a hooded cutthroat in a long coat, twin daggers.</summary>
        public static Spec Assassin()
        {
            var clips = MixamoClips();
            clips[RigPose.Dash] = "SideStep";
            return Mixamo(new Spec
            {
                name = "AssassinModel",
                height = 1.76f,
                // The model carries its own blades; the prop system supplies ours.
                hideRenderers = new[] { "Weapons_Geo" },
                propRight = "dagger",
                propLeft = "dagger",
                clips = clips,
            }, "MixamoArissa", "Arissa_DIFF_diffuse.png");
        }

        /// <summary>Samurai: the red-and-white ronin as authored, greatsword, deliberate.</summary>
        public static Spec Samurai() => Mixamo(new Spec
        {
            name = "SamuraiModel",
            height = 1.88f,
            trail = true,
            slotTextures = KachujinSlots(),
            propRight = "sword_2handed",
        }, "MixamoKachujin", "Kachujin_diffuse.png", propScale: 0.7f);

        /// <summary>Rogue Ninja: Renzo's body gone cold — the mirror, deliberately.</summary>
        public static Spec RogueNinja()
        {
            var clips = MixamoClips();
            clips[RigPose.Strike2] = "Stab";
            return Mixamo(new Spec
            {
                name = "RogueNinjaModel",
                height = 1.78f,
                tint = new Color(0.70f, 0.88f, 1.25f),   // moonlit steel: lighter and colder than Renzo's navy
                propRight = "dagger",
                clips = clips,
            }, "MixamoNinja", "Ch24_1001_Diffuse.png");
        }

        /// <summary>Elite Warrior: ornate gilded plate — the captain's kit, unique body.</summary>
        public static Spec EliteWarrior() => Mixamo(new Spec
        {
            name = "EliteWarriorModel",
            height = 2.05f,
            trail = true,
            propRight = "axe_2handed",
        }, "MixamoUriel", "Uriel_diffuse.png", propScale: 0.78f);

        /// <summary>Shade: the hooded rogue as a ghost — unarmed, translucent, pale blue.</summary>
        public static Spec Shade()
        {
            var clips = MixamoClips();
            clips[RigPose.Strike1] = "Kick";
            clips[RigPose.Strike2] = "Stab";
            clips[RigPose.Strike3] = "Kick";
            return Mixamo(new Spec
            {
                name = "ShadeModel",
                height = 1.62f,
                tint = new Color(0.55f, 0.72f, 0.88f),
                ghost = true,
                ghostAlpha = 0.55f,
                clips = clips,
            }, "MixamoAkai", "akai_diffuse.png");
        }

        // ------------------------------------------------------- named duel foes
        //
        // The seven named foes are copies of a base kind's def, so before this
        // they also inherited its body — which put the Drowned Guardian and the
        // Iron Guard, and the Convoy Captain and Commander Hoshu, in identical
        // skins two rungs apart on the duel ladder. Each now declares its own
        // visual. They are fought one at a time, so a body may repeat across the
        // ladder provided the pair differs in colour, height and weapon.

        /// <summary>Visual for a named foe, or null to inherit its base kind's body.</summary>
        public static Spec NamedFoe(string defId) => defId switch
        {
            // Kagehira's shield: the Elite's gilded plate gone cold iron.
            "ironguard" => Mixamo(new Spec
            {
                name = "IronGuardModel",
                height = 2.10f,
                tint = new Color(0.55f, 0.62f, 0.74f),
                trail = true,
                propRight = "axe_2handed",
            }, "MixamoUriel", "Uriel_diffuse.png", propScale: 0.78f),

            // Warden of a drowned road: an antlered marsh thing in hide and bone.
            // It was Goro's body recoloured green, which read as the same man.
            "drownedguardian" => Mixamo(new Spec
            {
                name = "DrownedGuardianModel",
                height = 2.30f,
                propRight = "axe_2handed",
            }, "MixamoMaw", "MAW_diffuse.png", propScale: 0.85f),

            // The inner gate: full dark plate and a great helm — the last door.
            // It was Jin's armour in bronze, which read as Jin.
            "finalcommander" => Mixamo(new Spec
            {
                name = "CommanderHoshuModel",
                height = 1.98f,
                trail = true,
                propRight = "sword_2handed",
            }, "MixamoPaladin", "Paladin_diffuse.png", propScale: 0.72f),

            // What the village left: a scavenger in a rusted cloak.
            "raiderleader" => Mixamo(new Spec
            {
                name = "ScavengerKingModel",
                height = 1.95f,
                tint = new Color(1.10f, 0.62f, 0.40f),
                propRight = "axe_2handed",
            }, "MixamoPirate", "void_diffuse.png", propScale: 0.78f),

            // Sisters of the silent forest: the pale red-hooded wraith.
            "threeblades" => Mixamo(new Spec
            {
                name = "ThreeBladesModel",
                height = 1.74f,
                slotTextures = VampireSlots(),
                propRight = "dagger",
                propLeft = "dagger",
            }, "MixamoVampire", "Vampire_diffuse.png"),

            _ => null,   // convoycaptain and paleshade read correctly as their kind
        };

        /// <summary>The named foes that declare a visual of their own.</summary>
        public static readonly string[] NamedFoeIds =
            { "ironguard", "drownedguardian", "finalcommander", "raiderleader", "threeblades" };

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
                if (spec.hideRenderers != null && spec.hideRenderers.Any(h =>
                        r.name.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    r.gameObject.SetActive(false);
                    continue;
                }
                if (mat != null)
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                }
                else if (spec.materialMode == MaterialMode.ConvertAuthored)
                {
                    var mats = r.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                        mats[i] = SlotMaterial(spec, mats[i] != null ? mats[i].name : $"slot{i}");
                    r.sharedMaterials = mats;
                }
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            // Animator + generated controller.
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildController(spec);
            animator.applyRootMotion = false;
            // A humanoid model carries an Avatar sub-asset; without it every
            // retargeted clip binds to nothing and the character stands still.
            var srcAvatar = AssetDatabase.LoadAllAssetsAtPath(spec.fbx)
                .OfType<Avatar>().FirstOrDefault();
            if (srcAvatar != null) animator.avatar = srcAvatar;

            visual.socketRight = FindSocket(instance, "r", spec.socketRight, spec);
            visual.socketLeft = FindSocket(instance, "l", spec.socketLeft, spec);

            AttachProps(instance, spec);

            var rig = root.AddComponent<SkeletalRig>();
            rig.tint = spec.tint;
            rig.ghost = spec.ghost;
            rig.ghostAlpha = spec.ghostAlpha;
            rig.hasLantern = spec.lantern;
            var poseCount = System.Enum.GetValues(typeof(RigPose)).Length;
            rig.poseStates = new string[poseCount];
            rig.poseClipLengths = new float[poseCount];
            var clipLib = Clips(spec);
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

        /// <summary>FNV-1a over the slot name — stable across runs, unlike GetHashCode.</summary>
        private static string StableTag(string s)
        {
            uint h = 2166136261;
            foreach (var c in s) { h ^= c; h *= 16777619; }
            return (h & 0xFFFF).ToString("x4");
        }

        /// <summary>
        /// A game-shader material for one authored slot of a multi-part body. The
        /// slot's albedo comes from `spec.slotTextures` by prefix on the authored
        /// material name, else `spec.texture`. Cached per spec+slot as
        /// `Mat_{spec}_{slot}.mat` beside the other generated materials.
        /// </summary>
        private static Material SlotMaterial(Spec spec, string slotName)
        {
            System.IO.Directory.CreateDirectory(OutDir);
            // Two distinct slot names can sanitise to the same string — Kachujin's
            // "kachujin_MAT" and "kachujin_MAT_" both lose their underscores — and
            // the second would then overwrite the first's material asset, silently
            // dropping one of the two textures. A deterministic tag keeps them
            // apart; it must not be string.GetHashCode, which is randomised per
            // process and would churn asset names on every regeneration.
            var safe = new string(slotName.Where(char.IsLetterOrDigit).ToArray());
            if (safe.Length == 0) safe = "slot";
            var path = $"{OutDir}/Mat_{spec.name}_{safe}{StableTag(slotName)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = spec.ghost ? Shader.Find("Emberline/Ghost") : SurfaceKit.SurfaceShader;
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;

            // Longest matching prefix wins, so "kachujin_MAT_" beats "kachujin_MAT".
            string texPath = null; var best = -1;
            if (spec.slotTextures != null)
                foreach (var kv in spec.slotTextures)
                    if (kv.Key.Length > best &&
                        slotName.StartsWith(kv.Key, System.StringComparison.OrdinalIgnoreCase))
                    { texPath = kv.Value; best = kv.Key.Length; }
            texPath ??= spec.texture;
            var tex = string.IsNullOrEmpty(texPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null && mat.HasProperty("_MainTex")) mat.mainTexture = tex;
            if (spec.ghost) mat.color = new Color(spec.tint.r, spec.tint.g, spec.tint.b, spec.ghostAlpha);
            else SurfaceKit.Apply(mat, Surface.Cloth, Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
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
                spec.socketRight, spec);
            AttachProp(instance, spec.propLeft, "l", spec.trail, spec.name, spec.propScale,
                spec.socketLeft, spec);
        }

        /// <summary>
        /// Resolve a weapon socket. A model declares its own bone names; when it
        /// declares none we fall back to the KayKit-era search so the existing
        /// placeholder roster keeps building unchanged.
        /// </summary>
        /// <summary>
        /// KayKit rigs carry purpose-built `handslot` empties that already hold the
        /// grip pose, so a prop parents to them with an identity transform. A
        /// Mixamo rig has only the wrist joint, so the weapon needs a declared
        /// grip correction — `Spec.propOffsetPos` / `propOffsetRot`, expressed in
        /// the hand bone's own space. Mirrored for the left hand.
        /// </summary>
        private static Transform GripAnchor(Transform hand, string side, Spec spec)
        {
            if (hand == null) return null;
            var name = "GripAnchor_" + side;
            var existing = hand.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            go.transform.SetParent(hand, false);
            var pos = spec?.propOffsetPos ?? Vector3.zero;
            var rot = spec?.propOffsetRot ?? Vector3.zero;
            if (side == "l") { pos.x = -pos.x; rot.y = -rot.y; rot.z = -rot.z; }
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rot);
            return go.transform;
        }

        private static Transform FindSocket(GameObject instance, string side, string[] declared, Spec spec = null)
        {
            var all = instance.GetComponentsInChildren<Transform>(true);

            if (declared != null)
                foreach (var want in declared)
                {
                    if (string.IsNullOrEmpty(want)) continue;
                    var hit = all.FirstOrDefault(t =>
                        string.Equals(t.name, want, System.StringComparison.OrdinalIgnoreCase));
                    if (hit == null) continue;
                    // A declared bone that is the hand itself still needs a grip frame.
                    return hit.name.ToLowerInvariant().Contains("handslot") ? hit : GripAnchor(hit, side, spec);
                }

            var slot = all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot." + side))
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("handslot" + side));
            if (slot != null) return slot;

            // No purpose-built slot: fall back to the hand bone and derive a grip.
            var mixamo = side == "r" ? "righthand" : "lefthand";
            var hand = all.FirstOrDefault(t => t.name.ToLowerInvariant().Contains("hand." + side))
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant() == "hand" + side)
                       // Mixamo: mixamorig:RightHand / mixamorig:LeftHand. Ends-with, so
                       // the finger bones (RightHandIndex1 …) can never win the match.
                       ?? all.FirstOrDefault(t => t.name.ToLowerInvariant().EndsWith(mixamo));
            return hand != null ? GripAnchor(hand, side, spec) : null;
        }

        /// <summary>
        /// Hangs one KayKit prop off a hand slot as `Prop_{name}_{side}`. Public so
        /// the bootstrap can pre-attach the whole weapon catalogue on the player —
        /// runtime weapon swapping just enables one set and disables the rest,
        /// since a build can't instantiate FBX assets on device.
        /// </summary>
        public static GameObject AttachProp(GameObject instance, string propName, string side,
            bool trail = false, string ownerName = "", Vector3? scale = null,
            string[] declaredSocket = null, Spec gripSpec = null)
        {
            if (string.IsNullOrEmpty(propName)) return null;
            var slot = FindSocket(instance, side, declaredSocket, gripSpec);
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
            Harvest(dict, fbxPath);
            return dict;
        }

        /// <summary>The clip a pose resolves to on this spec, or null — for tooling that poses a built body.</summary>
        public static AnimationClip ResolveClip(Spec spec, RigPose pose)
        {
            var lib = Clips(spec);
            if (spec.clips != null && spec.clips.TryGetValue(pose, out var n) && lib.TryGetValue(n, out var c)) return c;
            return lib.TryGetValue("Idle", out var idle) ? idle : null;
        }

        /// <summary>Clip library for a spec: the model's own takes plus any declared sources.</summary>
        private static Dictionary<string, AnimationClip> Clips(Spec spec)
        {
            var dict = Clips(spec.fbx);
            if (spec.clipSources != null)
                foreach (var src in spec.clipSources) Harvest(dict, src);
            return dict;
        }

        private static void Harvest(Dictionary<string, AnimationClip> dict, string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath)) return;
            var file = System.IO.Path.GetFileNameWithoutExtension(fbxPath);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (o is not AnimationClip clip || clip.name.StartsWith("__preview")) continue;
                // A single-take file whose clip still carries Mixamo's placeholder
                // name is keyed by its filename instead, so the pose map can find it
                // even when the importer rename has not been applied.
                var key = clip.name == "mixamo.com" ? file : clip.name;
                dict[key] = clip;
            }
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

            var lib = Clips(spec);
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
