using System.IO;
using Emberline.Core;
using Emberline.Player;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Authors the player movesets: one PlayerMoveset asset per weapon under
    /// Resources/Attacks, each a full set of contextual attacks. Weapons differ
    /// in *behaviour* here — which contexts they answer, how hard, how fast,
    /// how far — not only in the numbers on the WeaponDef.
    /// </summary>
    public static class EmberCombatAssets
    {
        private const string Dir = "Assets/Resources/Attacks";

        [MenuItem("Emberline/Build Player Movesets")]
        public static void BuildPlayerMovesets()
        {
            Directory.CreateDirectory(Dir);
            Author("katana", Katana());
            Author("tanto", Tanto());
            Author("hook", Hook());
            Author("daggers", Daggers());
            Author("bomb", Bomb());
            Author("crossbow", Crossbow());
            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Player movesets authored: 6 under Resources/Attacks");
        }

        private static void Author(string weaponId, PlayerAttackDefinition[] attacks)
        {
            var path = $"{Dir}/Moveset_{weaponId}.asset";
            var m = AssetDatabase.LoadAssetAtPath<PlayerMoveset>(path);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<PlayerMoveset>();
                AssetDatabase.CreateAsset(m, path);
            }
            m.weaponId = weaponId;
            m.attacks = attacks;
            EditorUtility.SetDirty(m);
        }

        private static PlayerAttackDefinition P(AttackContext ctx, string id, RigPose pose,
            float dmg = 1f, float range = 1f, float arc = 130f, float lunge = 5.5f,
            float anim = 0.28f, float recovery = 0f, float cooldown = 0f, float posture = 1f,
            bool crush = false, bool launch = false, bool execute = false, bool heavy = false,
            float cam = 0f, float hitStop = 0.04f, int stage = 0) => new()
        {
            context = ctx, id = id, displayName = id.ToUpperInvariant().Replace('_', ' '),
            pose = pose, damageMultiplier = dmg, rangeMultiplier = range, arcDeg = arc,
            lunge = lunge, animTime = anim, recovery = recovery, cooldown = cooldown,
            postureMultiplier = posture, crush = crush, launch = launch, execute = execute,
            heavyWhoosh = heavy, cameraImpact = cam, hitStop = hitStop, chainStage = stage,
        };

        // ------------------------------------------------------------- katana
        // Balanced: medium reach, honest speed, the strongest counters on the
        // roster and posture damage a guard cannot sit through.
        private static PlayerAttackDefinition[] Katana() => new[]
        {
            P(AttackContext.Chain1, "cut", RigPose.Strike1, stage: 1),
            P(AttackContext.Chain2, "return_cut", RigPose.Strike2, dmg: 1.2f, stage: 2),
            P(AttackContext.Chain3, "falling_cut", RigPose.Strike3, dmg: 1.8f, crush: true, launch: true,
                recovery: 0.22f, cam: 0.4f, hitStop: 0.06f, stage: 3),
            P(AttackContext.Heavy, "cleave", RigPose.Cleave, dmg: 2.6f, arc: 170f, range: 1.18f,
                lunge: 3.3f, heavy: true, crush: true, recovery: 0.3f, cam: 0.6f, hitStop: 0.06f),
            P(AttackContext.HeavyThrust, "lunge_thrust", RigPose.Stab, dmg: 2.2f, arc: 40f, range: 1.6f,
                lunge: 8f, heavy: true, crush: true, posture: 1.4f, recovery: 0.32f, cam: 0.5f),
            P(AttackContext.HeavyFinisher, "crescent", RigPose.Sweep, dmg: 3f, arc: 200f, range: 1.2f,
                lunge: 2.5f, heavy: true, crush: true, launch: true, recovery: 0.4f, cam: 0.8f, hitStop: 0.08f),
            P(AttackContext.HeavyFollow, "recover_cut", RigPose.Strike2, dmg: 1.1f, anim: 0.24f, stage: 1),
            P(AttackContext.GuardBreakPunish, "guard_splitter", RigPose.Kick, dmg: 1.4f, arc: 90f,
                posture: 3f, crush: true, heavy: true, recovery: 0.25f, cam: 0.5f),
            P(AttackContext.ParryCounter, "riposte", RigPose.Strike3, dmg: 2.4f, arc: 100f, lunge: 6.5f,
                crush: true, posture: 2f, cam: 0.7f, hitStop: 0.08f),
            P(AttackContext.DodgeCounter, "flicker_counter", RigPose.Strike2, dmg: 1.8f, lunge: 8.5f,
                arc: 110f, posture: 1.5f, cam: 0.5f, hitStop: 0.06f),
            P(AttackContext.BackAttack, "back_cut", RigPose.Strike3, dmg: 1.6f, posture: 2f, crush: true,
                cam: 0.4f),
            P(AttackContext.StaggerPunish, "posture_punisher", RigPose.Cleave, dmg: 2f, crush: true,
                execute: true, cam: 0.7f, hitStop: 0.08f),
            P(AttackContext.Assassination, "silent_end", RigPose.Stab, dmg: 1f, execute: true, arc: 90f),
            P(AttackContext.GapCloser, "closing_strike", RigPose.Charge, dmg: 1.3f, lunge: 11f, range: 1.15f,
                arc: 90f, stage: 1),
            P(AttackContext.Running, "running_slash", RigPose.Strike2, dmg: 1.4f, lunge: 8f, arc: 120f, stage: 1),
            P(AttackContext.Air, "falling_blade", RigPose.Jump, dmg: 1.9f, arc: 360f, range: 0.9f, lunge: 1f,
                crush: true, recovery: 0.3f, cam: 0.6f, hitStop: 0.06f),
            P(AttackContext.WallRun, "wall_dive", RigPose.Jump, dmg: 2.2f, arc: 360f, range: 1f, lunge: 6f,
                crush: true, recovery: 0.3f, cam: 0.6f),
        };

        // -------------------------------------------------------------- tanto
        // Fast and short. Every swing recovers quickly and the heavy is weak;
        // the perfect dodge is where the tanto is dangerous.
        private static PlayerAttackDefinition[] Tanto() => new[]
        {
            P(AttackContext.Chain1, "quick_cut", RigPose.Strike1, dmg: 1f, anim: 0.2f, lunge: 6.5f, stage: 1),
            P(AttackContext.Chain2, "quick_cut_2", RigPose.Strike2, dmg: 1.1f, anim: 0.2f, lunge: 6.5f, stage: 2),
            P(AttackContext.Chain3, "storm_cut", RigPose.Strike3, dmg: 1.7f, anim: 0.22f, crush: true,
                recovery: 0.15f, cam: 0.3f, stage: 3),
            P(AttackContext.Heavy, "short_cleave", RigPose.Cleave, dmg: 2f, arc: 150f, range: 1f, lunge: 4f,
                heavy: true, crush: true, recovery: 0.2f, cam: 0.4f),
            P(AttackContext.HeavyThrust, "needle", RigPose.Stab, dmg: 1.7f, arc: 35f, range: 1.4f, lunge: 9f,
                heavy: true, posture: 0.8f, recovery: 0.22f),
            P(AttackContext.HeavyFinisher, "storm_finish", RigPose.Sweep, dmg: 2.2f, arc: 180f, crush: true,
                launch: true, recovery: 0.28f, cam: 0.6f, hitStop: 0.07f),
            P(AttackContext.HeavyFollow, "flow_cut", RigPose.Strike1, dmg: 1f, anim: 0.18f, stage: 1),
            P(AttackContext.GuardBreakPunish, "shoulder", RigPose.Kick, dmg: 1f, arc: 80f, posture: 2f,
                crush: true, recovery: 0.2f),
            P(AttackContext.ParryCounter, "storm_riposte", RigPose.Strike3, dmg: 2f, lunge: 7f, crush: true,
                posture: 1.6f, cam: 0.6f, hitStop: 0.07f),
            P(AttackContext.DodgeCounter, "flicker_fang", RigPose.Stab, dmg: 2.2f, lunge: 10f, arc: 60f,
                posture: 1.8f, cam: 0.6f, hitStop: 0.08f),
            P(AttackContext.BackAttack, "kidney_cut", RigPose.Strike2, dmg: 1.8f, posture: 2f, crush: true),
            P(AttackContext.StaggerPunish, "opening_taken", RigPose.Strike3, dmg: 1.8f, crush: true, execute: true,
                cam: 0.6f),
            P(AttackContext.Assassination, "silent_end", RigPose.Stab, dmg: 1f, execute: true, arc: 90f),
            P(AttackContext.GapCloser, "dart", RigPose.Charge, dmg: 1.2f, lunge: 12f, arc: 80f, anim: 0.2f, stage: 1),
            P(AttackContext.Running, "running_cut", RigPose.Strike1, dmg: 1.2f, lunge: 9f, anim: 0.2f, stage: 1),
            P(AttackContext.Air, "dropping_cut", RigPose.Jump, dmg: 1.5f, arc: 360f, range: 0.8f, lunge: 1f,
                recovery: 0.2f, cam: 0.4f),
            P(AttackContext.WallRun, "wall_cut", RigPose.Jump, dmg: 1.7f, arc: 360f, range: 0.9f, lunge: 6f,
                recovery: 0.2f),
        };

        // --------------------------------------------------------------- hook
        // Control: the longest reach, slow, and the third hit drags. The heavy
        // and the gap closer both pull people where the hook wants them.
        private static PlayerAttackDefinition[] Hook() => new[]
        {
            P(AttackContext.Chain1, "hook_swing", RigPose.Strike1, dmg: 1f, anim: 0.36f, lunge: 4.5f, stage: 1),
            P(AttackContext.Chain2, "hook_return", RigPose.Sweep, dmg: 1.15f, anim: 0.36f, arc: 160f, lunge: 4f, stage: 2),
            P(AttackContext.Chain3, "hook_drag", RigPose.Strike3, dmg: 1.8f, anim: 0.4f, crush: true, launch: true,
                recovery: 0.32f, cam: 0.5f, hitStop: 0.07f, stage: 3),
            P(AttackContext.Heavy, "hook_cleave", RigPose.Cleave, dmg: 2.5f, arc: 180f, range: 1.15f, lunge: 2.5f,
                heavy: true, crush: true, recovery: 0.42f, cam: 0.7f, hitStop: 0.07f),
            P(AttackContext.HeavyThrust, "long_reach", RigPose.Stab, dmg: 2f, arc: 34f, range: 1.9f, lunge: 5f,
                heavy: true, crush: true, posture: 1.5f, recovery: 0.4f, cam: 0.5f),
            P(AttackContext.HeavyFinisher, "reaper", RigPose.Sweep, dmg: 3.1f, arc: 220f, range: 1.25f, lunge: 2f,
                heavy: true, crush: true, launch: true, recovery: 0.5f, cam: 0.9f, hitStop: 0.09f),
            P(AttackContext.HeavyFollow, "short_hook", RigPose.Strike1, dmg: 1f, anim: 0.3f, stage: 1),
            P(AttackContext.GuardBreakPunish, "hook_pull", RigPose.Kick, dmg: 1.2f, arc: 80f, posture: 3.2f,
                crush: true, heavy: true, recovery: 0.35f, cam: 0.5f),
            P(AttackContext.ParryCounter, "hooked_riposte", RigPose.Strike3, dmg: 2.2f, crush: true, posture: 2.2f,
                lunge: 5f, cam: 0.7f, hitStop: 0.08f),
            P(AttackContext.DodgeCounter, "catch", RigPose.Sweep, dmg: 1.6f, arc: 160f, lunge: 6f, posture: 1.4f,
                cam: 0.5f),
            P(AttackContext.BackAttack, "spine_hook", RigPose.Strike3, dmg: 1.7f, posture: 2.2f, crush: true),
            P(AttackContext.StaggerPunish, "dragged_down", RigPose.Cleave, dmg: 2.2f, crush: true, execute: true,
                cam: 0.7f, hitStop: 0.08f),
            P(AttackContext.Assassination, "silent_hook", RigPose.Stab, dmg: 1f, execute: true, arc: 90f),
            P(AttackContext.GapCloser, "cast", RigPose.Charge, dmg: 1.3f, lunge: 9f, range: 1.3f, arc: 60f,
                anim: 0.34f, stage: 1),
            P(AttackContext.Running, "running_hook", RigPose.Strike2, dmg: 1.4f, lunge: 6.5f, anim: 0.34f, stage: 1),
            P(AttackContext.Air, "falling_hook", RigPose.Jump, dmg: 2f, arc: 360f, range: 0.95f, lunge: 1f,
                crush: true, recovery: 0.36f, cam: 0.6f),
            P(AttackContext.WallRun, "wall_hook", RigPose.Jump, dmg: 2.3f, arc: 360f, range: 1f, lunge: 5f,
                crush: true, recovery: 0.36f),
        };

        // ------------------------------------------------------------ daggers
        // Aggressive: extremely fast, low per-hit, five-stage chain, poor at
        // opening a guard — the counters and the back attack are the way in.
        private static PlayerAttackDefinition[] Daggers() => new[]
        {
            P(AttackContext.Chain1, "flick", RigPose.Strike1, dmg: 1f, anim: 0.17f, lunge: 6.8f, arc: 120f, stage: 1),
            P(AttackContext.Chain2, "flick_2", RigPose.Strike2, dmg: 1f, anim: 0.17f, lunge: 6.8f, arc: 120f, stage: 2),
            P(AttackContext.Chain3, "twin_fang", RigPose.Strike3, dmg: 1.75f, anim: 0.2f, crush: true, launch: true,
                recovery: 0.12f, cam: 0.3f, stage: 3),
            P(AttackContext.Heavy, "whirl", RigPose.Cleave, dmg: 1.8f, arc: 360f, range: 0.85f, lunge: 2f,
                heavy: true, crush: true, posture: 0.6f, recovery: 0.18f, cam: 0.4f),
            P(AttackContext.HeavyThrust, "double_stab", RigPose.Stab, dmg: 1.6f, arc: 40f, range: 1.2f, lunge: 8f,
                heavy: true, posture: 0.6f, recovery: 0.18f),
            P(AttackContext.HeavyFinisher, "whirl_finish", RigPose.Sweep, dmg: 2.1f, arc: 360f, range: 0.9f,
                crush: true, launch: true, posture: 0.7f, recovery: 0.24f, cam: 0.5f, hitStop: 0.06f),
            P(AttackContext.HeavyFollow, "flow", RigPose.Strike1, dmg: 1f, anim: 0.15f, stage: 1),
            P(AttackContext.GuardBreakPunish, "hilt_strike", RigPose.Kick, dmg: 0.8f, arc: 80f, posture: 1.6f,
                crush: true, recovery: 0.18f),
            P(AttackContext.ParryCounter, "twin_riposte", RigPose.Strike3, dmg: 2f, crush: true, posture: 1.4f,
                lunge: 7f, cam: 0.6f, hitStop: 0.07f),
            P(AttackContext.DodgeCounter, "shadow_fang", RigPose.Stab, dmg: 2.2f, lunge: 10f, arc: 60f,
                posture: 1.4f, cam: 0.6f, hitStop: 0.07f),
            P(AttackContext.BackAttack, "twin_backstab", RigPose.Stab, dmg: 2.2f, posture: 2.4f, crush: true, cam: 0.5f),
            P(AttackContext.StaggerPunish, "throat", RigPose.Strike3, dmg: 1.8f, crush: true, execute: true, cam: 0.6f),
            P(AttackContext.Assassination, "silent_end", RigPose.Stab, dmg: 1f, execute: true, arc: 90f),
            P(AttackContext.GapCloser, "blink_in", RigPose.Charge, dmg: 1.1f, lunge: 12f, arc: 70f, anim: 0.17f, stage: 1),
            P(AttackContext.Running, "running_flick", RigPose.Strike1, dmg: 1.1f, lunge: 9f, anim: 0.17f, stage: 1),
            P(AttackContext.Air, "dropping_fangs", RigPose.Jump, dmg: 1.5f, arc: 360f, range: 0.8f, lunge: 1f,
                recovery: 0.18f, cam: 0.4f),
            P(AttackContext.WallRun, "wall_fangs", RigPose.Jump, dmg: 1.7f, arc: 360f, range: 0.9f, lunge: 6f,
                recovery: 0.18f),
        };

        // --------------------------------------------------------------- bomb
        // Utility: the blade is short and simple; the value is the throw. The
        // heavy is the ground burst that already exists.
        private static PlayerAttackDefinition[] Bomb() => new[]
        {
            P(AttackContext.Chain1, "short_cut", RigPose.Strike1, dmg: 1f, anim: 0.24f, lunge: 4.2f, arc: 110f, stage: 1),
            P(AttackContext.Chain2, "short_cut_2", RigPose.Strike3, dmg: 1.3f, anim: 0.24f, crush: true, launch: true,
                recovery: 0.2f, stage: 2),
            P(AttackContext.Heavy, "ground_burst", RigPose.Cleave, dmg: 2.4f, arc: 360f, range: 1.27f, lunge: 1f,
                heavy: true, crush: true, recovery: 0.34f, cam: 0.7f, hitStop: 0.06f),
            P(AttackContext.ParryCounter, "riposte", RigPose.Strike3, dmg: 2f, crush: true, posture: 1.6f, cam: 0.6f),
            P(AttackContext.DodgeCounter, "flicker_counter", RigPose.Strike2, dmg: 1.6f, lunge: 8f, cam: 0.5f),
            P(AttackContext.BackAttack, "back_cut", RigPose.Strike3, dmg: 1.5f, posture: 1.8f, crush: true),
            P(AttackContext.StaggerPunish, "finish", RigPose.Strike3, dmg: 1.8f, crush: true, execute: true, cam: 0.6f),
            P(AttackContext.Assassination, "silent_end", RigPose.Stab, dmg: 1f, execute: true, arc: 90f),
            P(AttackContext.GuardBreakPunish, "shove", RigPose.Kick, dmg: 0.9f, arc: 80f, posture: 2f, crush: true,
                recovery: 0.2f),
        };

        // ----------------------------------------------------------- crossbow
        // Ranged utility: the strike is a shot. Melee contexts fall back to a
        // stock-strike, so the crossbow is never helpless up close.
        private static PlayerAttackDefinition[] Crossbow() => new[]
        {
            P(AttackContext.Chain1, "bolt", RigPose.Strike2, dmg: 1f, anim: 0.3f, lunge: 0f, stage: 1),
            P(AttackContext.Chain2, "bolt_2", RigPose.Strike2, dmg: 1.1f, anim: 0.3f, lunge: 0f, stage: 2),
            P(AttackContext.Chain3, "heavy_bolt", RigPose.Strike2, dmg: 1.6f, anim: 0.34f, lunge: 0f, crush: true,
                recovery: 0.25f, stage: 3),
            P(AttackContext.Heavy, "volley", RigPose.Cleave, dmg: 1.4f, arc: 40f, lunge: 0f, heavy: true,
                recovery: 0.3f, cam: 0.4f),
            P(AttackContext.GuardBreakPunish, "stock_strike", RigPose.Kick, dmg: 0.9f, arc: 80f, posture: 2.2f,
                crush: true, lunge: 3f, recovery: 0.22f),
            P(AttackContext.ParryCounter, "point_blank", RigPose.Strike2, dmg: 2.2f, lunge: 0f, crush: true, cam: 0.6f),
            P(AttackContext.DodgeCounter, "snap_shot", RigPose.Strike2, dmg: 1.7f, lunge: 0f, cam: 0.4f),
            P(AttackContext.StaggerPunish, "finish", RigPose.Strike3, dmg: 1.8f, crush: true, execute: true, lunge: 4f),
            P(AttackContext.Assassination, "silent_end", RigPose.Stab, dmg: 1f, execute: true, arc: 90f, lunge: 2f),
            P(AttackContext.BackAttack, "back_shot", RigPose.Strike2, dmg: 1.6f, lunge: 0f, posture: 1.6f),
        };
    }
}
