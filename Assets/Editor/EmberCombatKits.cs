using System.IO;
using Emberline.Enemies;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Combat 2.0 kits and personalities. Runs after the enemy defs are
    /// authored: replaces each archetype's moveset with the set the brief
    /// specifies (ids, categories, timings) and gives it a combat profile.
    /// Stats, weaknesses and the Phase 3 knobs on the defs are untouched.
    /// </summary>
    public static class EmberCombatKits
    {
        private const string ProfileDir = "Assets/Resources/Combat";

        public static void Apply()
        {
            Directory.CreateDirectory(ProfileDir);

            // ---------------------------------------------------------- kits
            Kit("Bandit",
                A("quick_slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.4f, w: 1f, windup: 0.4f),
                A("quick_slash_2", AttackKind.Slash, AttackCategory.Quick, 0f, 2.2f, w: 0.8f, dmg: 0.9f, windup: 0.36f, cd: 1.2f),
                A("heavy_overhead", AttackKind.Slash, AttackCategory.Heavy, 0f, 2.6f, w: 0.6f, dmg: 1.6f, windup: 0.75f, red: true, cd: 2.2f),
                A("wide_swing", AttackKind.Sweep, AttackCategory.Sweep, 0f, 2.8f, w: 0.6f, dmg: 1.1f, windup: 0.55f, cd: 1.8f, ring: 1.4f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.2f, w: 0.4f, dmg: 1.2f, windup: 0.6f, red: true, cd: 2.6f));

            Kit("Assassin",
                A("quick_cut", AttackKind.Slash, AttackCategory.Quick, 0f, 2.2f, w: 1f, windup: 0.34f, cd: 1.1f),
                A("backstab", AttackKind.Slash, AttackCategory.Quick, 0f, 2.0f, w: 1f, dmg: 1.8f, windup: 0.34f, cd: 2f, req: TargetStateRequirement.BackTurned),
                A("dash_strike", AttackKind.DashStrike, AttackCategory.GapCloser, 3.5f, 8f, w: 0.9f, windup: 0.4f, red: true, cd: 2.2f, ring: 1.1f),
                A("delayed_slash", AttackKind.Slash, AttackCategory.Delayed, 0f, 2.4f, w: 0.6f, dmg: 1.2f, windup: 0.5f, cd: 2f),
                A("retreat_slash", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.2f, w: 0.8f, dmg: 0.9f, windup: 0.36f, cd: 1.4f),
                A("throwing_kunai", AttackKind.QuickShot, AttackCategory.Ranged, 3f, 9f, w: 0.6f, dmg: 0.7f, windup: 0.45f, cd: 1.8f));

            Kit("Spearman",
                A("long_thrust", AttackKind.Thrust, AttackCategory.Thrust, 1.4f, 4.2f, w: 1f, windup: 0.55f, cd: 1.9f, ring: 1.5f),
                A("double_thrust", AttackKind.Thrust, AttackCategory.Heavy, 1.4f, 4f, w: 0.6f, dmg: 1.35f, windup: 0.72f, red: true, cd: 2.4f, ring: 1.5f),
                A("sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3.2f, w: 0.7f, dmg: 1.1f, windup: 0.55f, cd: 1.9f, ring: 1.5f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.8f, w: 0.5f, dmg: 1.2f, windup: 0.62f, red: true, cd: 2.6f),
                A("retreat_thrust", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 3.4f, w: 0.8f, dmg: 1f, windup: 0.42f, cd: 1.5f));

            Kit("Archer",
                A("charged_shot", AttackKind.ChargedShot, AttackCategory.Ranged, 4f, 9f, w: 1f, dmg: 2f, windup: 1.2f, red: true, cd: 2.6f, ring: 1.25f),
                A("quick_shot", AttackKind.QuickShot, AttackCategory.Ranged, 3f, 6f, w: 0.6f, dmg: 0.8f, windup: 0.4f, cd: 1.4f),
                A("panic_jab", AttackKind.Slash, AttackCategory.Quick, 0f, 1.7f, w: 0.25f, dmg: 0.6f, windup: 0.4f, cd: 1.6f));

            Kit("HeavyWarrior",
                A("overhead_smash", AttackKind.Slash, AttackCategory.Heavy, 0f, 2.8f, w: 1.2f, dmg: 1.5f, windup: 0.7f, red: true, cd: 1.9f, ring: 1.6f),
                A("horizontal_sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3.2f, w: 0.9f, dmg: 1.2f, windup: 0.6f, red: true, cd: 2f, ring: 1.6f),
                A("delayed_smash", AttackKind.HeavySlam, AttackCategory.Delayed, 0f, 3.6f, w: 0.5f, dmg: 1.2f, windup: 0.85f, red: true, cd: 3f, ring: 2f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.4f, w: 0.7f, dmg: 1.3f, windup: 0.65f, red: true, cd: 2.4f),
                A("charge", AttackKind.DashStrike, AttackCategory.GapCloser, 4f, 9f, w: 0.6f, windup: 0.55f, red: true, cd: 2.8f, ring: 1.2f),
                A("ground_shock", AttackKind.HeavySlam, AttackCategory.Heavy, 0f, 3.6f, w: 0.6f, dmg: 1.2f, windup: 0.85f, red: true, cd: 3f, ring: 2f));

            Kit("Samurai",
                A("quick_slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.8f, w: 1.1f, windup: 0.45f, cd: 1.4f),
                A("heavy_slash", AttackKind.Slash, AttackCategory.Heavy, 0f, 2.8f, w: 0.7f, dmg: 1.4f, windup: 0.7f, red: true, cd: 2.2f, ring: 1.2f),
                A("thrust", AttackKind.Thrust, AttackCategory.Thrust, 1f, 3.6f, w: 0.7f, dmg: 1.1f, windup: 0.5f, cd: 1.8f, ring: 1.3f),
                A("parry", AttackKind.Parry, AttackCategory.Counter, 0f, 3.2f, w: 1.1f, dmg: 0f, windup: 0.35f, cd: 2.2f),
                A("sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3f, w: 0.6f, dmg: 1.1f, windup: 0.55f, cd: 2f, ring: 1.3f),
                A("delayed_slash", AttackKind.Slash, AttackCategory.Delayed, 0f, 2.8f, w: 0.5f, dmg: 1.25f, windup: 0.5f, cd: 2.2f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.4f, w: 0.5f, dmg: 1.2f, windup: 0.6f, red: true, cd: 2.6f),
                A("feint", AttackKind.Slash, AttackCategory.Feint, 0f, 2.8f, w: 0.5f, dmg: 1f, windup: 0.62f, cd: 2.8f),
                A("dash", AttackKind.DashStrike, AttackCategory.GapCloser, 3.5f, 7f, w: 0.5f, windup: 0.5f, red: true, cd: 2.6f));

            Kit("RogueNinja",
                A("dash_strike", AttackKind.DashStrike, AttackCategory.GapCloser, 3f, 9f, w: 1.2f, windup: 0.35f, red: true, cd: 1.9f, ring: 1.1f),
                A("slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.2f, w: 1f, windup: 0.38f, cd: 1.3f),
                A("kunai", AttackKind.QuickShot, AttackCategory.Ranged, 3f, 9f, w: 0.6f, dmg: 0.7f, windup: 0.45f, cd: 1.8f),
                A("bomb", AttackKind.ThrowBomb, AttackCategory.Ranged, 5f, 11f, w: 0.4f, dmg: 0.8f, windup: 0.6f, red: true, cd: 4f, ring: 1.35f),
                A("retreat_slash", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.2f, w: 0.8f, dmg: 0.9f, windup: 0.36f, cd: 1.4f),
                A("backstab", AttackKind.Slash, AttackCategory.Quick, 0f, 2.0f, w: 1f, dmg: 1.7f, windup: 0.36f, cd: 2f, req: TargetStateRequirement.BackTurned));

            Kit("EliteWarrior",
                A("slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.9f, w: 1f, windup: 0.45f, cd: 1.6f),
                A("spin", AttackKind.SpinCleave, AttackCategory.Sweep, 0f, 4.5f, w: 0.5f, dmg: 1.2f, windup: 0.9f, red: true, cd: 3.2f, ring: 2.4f),
                A("slam", AttackKind.HeavySlam, AttackCategory.Heavy, 0f, 3.8f, w: 0.6f, dmg: 1.15f, windup: 0.8f, red: true, cd: 3f, ring: 2f),
                A("dash", AttackKind.DashStrike, AttackCategory.GapCloser, 4f, 10f, w: 0.7f, windup: 0.45f, red: true, cd: 2.4f, ring: 1.2f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.6f, w: 0.7f, dmg: 1.3f, windup: 0.6f, red: true, cd: 2.4f),
                A("feint", AttackKind.Slash, AttackCategory.Feint, 0f, 2.9f, w: 0.6f, dmg: 1f, windup: 0.6f, cd: 2.6f),
                A("delayed_slash", AttackKind.Slash, AttackCategory.Delayed, 0f, 2.9f, w: 0.5f, dmg: 1.2f, windup: 0.5f, cd: 2.2f),
                A("retreat_slash", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.6f, w: 0.5f, dmg: 1f, windup: 0.4f, cd: 1.6f),
                A("thrust", AttackKind.Thrust, AttackCategory.Thrust, 1f, 3.2f, w: 0.6f, dmg: 1.1f, windup: 0.5f, cd: 1.8f, ring: 1.2f));

            Kit("Bomber",
                A("throw_bomb", AttackKind.ThrowBomb, AttackCategory.Ranged, 4f, 10f, w: 1f, windup: 0.7f, red: true, cd: 3.2f, ring: 1.4f),
                A("powder_drop", AttackKind.ThrowBomb, AttackCategory.RetreatAttack, 0f, 3.5f, w: 0.8f, dmg: 0.8f, windup: 0.5f, red: true, cd: 3.5f, ring: 1.2f),
                A("panic_jab", AttackKind.Slash, AttackCategory.Quick, 0f, 1.7f, w: 0.2f, dmg: 0.5f, windup: 0.4f, cd: 1.6f));

            Kit("Shade",
                A("claws", AttackKind.Flurry, AttackCategory.Quick, 0f, 2.2f, w: 1f, windup: 0.35f, cd: 1.25f),
                A("lunge", AttackKind.DashStrike, AttackCategory.GapCloser, 3f, 7f, w: 0.6f, windup: 0.4f, red: true, cd: 2.2f),
                A("retreat_swipe", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2f, w: 0.6f, dmg: 0.9f, windup: 0.34f, cd: 1.3f));

            Kit("MiniBoss",
                A("heavy_slam", AttackKind.HeavySlam, AttackCategory.Heavy, 0f, 4.4f, w: 1.3f, windup: 0.6f, red: true, cd: 1.8f, ring: 2f),
                A("spin", AttackKind.SpinCleave, AttackCategory.Sweep, 0f, 5f, w: 0.5f, dmg: 1.3f, windup: 0.95f, red: true, cd: 2.4f, ring: 2.6f),
                A("charge", AttackKind.DashStrike, AttackCategory.GapCloser, 5f, 11f, w: 0.5f, windup: 0.7f, red: true, cd: 2.6f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.6f, w: 0.7f, dmg: 1.3f, windup: 0.65f, red: true, cd: 2.4f),
                A("delayed_smash", AttackKind.HeavySlam, AttackCategory.Delayed, 0f, 4f, w: 0.6f, dmg: 1.2f, windup: 0.8f, red: true, cd: 3f, ring: 2f),
                A("sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3.4f, w: 0.6f, dmg: 1.1f, windup: 0.55f, cd: 2f, ring: 1.5f));

            Kit("Boss",
                A("slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.6f, w: 1.2f, windup: 0.45f, cd: 1.3f),
                A("spit", AttackKind.PoisonSpit, AttackCategory.Ranged, 4.5f, 12f, w: 0.9f, dmg: 0.5f, windup: 0.55f, red: true, cd: 4f, ring: 1.3f),
                A("dash", AttackKind.DashStrike, AttackCategory.GapCloser, 5f, 11f, w: 0.9f, windup: 0.55f, red: true, cd: 1.8f),
                A("feint", AttackKind.Slash, AttackCategory.Feint, 0f, 2.6f, w: 0.7f, dmg: 1f, windup: 0.6f, cd: 2.4f),
                A("guard_break", AttackKind.GuardBreak, AttackCategory.GuardBreak, 0f, 2.4f, w: 0.7f, dmg: 1.2f, windup: 0.6f, red: true, cd: 2.4f),
                A("thrust", AttackKind.Thrust, AttackCategory.Thrust, 1f, 3.4f, w: 0.7f, dmg: 1.1f, windup: 0.5f, cd: 1.8f, ring: 1.3f),
                A("delayed_slash", AttackKind.Slash, AttackCategory.Delayed, 0f, 2.6f, w: 0.6f, dmg: 1.25f, windup: 0.5f, cd: 2.2f),
                A("retreat_slash", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.4f, w: 0.6f, dmg: 1f, windup: 0.4f, cd: 1.6f),
                A("sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3f, w: 0.6f, dmg: 1.1f, windup: 0.55f, cd: 2f, ring: 1.3f),
                A("parry", AttackKind.Parry, AttackCategory.Counter, 0f, 3f, w: 0.8f, dmg: 0f, windup: 0.35f, cd: 2.4f));

            Kit("Jin",
                A("quick_slash", AttackKind.Slash, AttackCategory.Quick, 0f, 2.6f, w: 1f, windup: 0.42f, cd: 1.3f),
                A("thrust", AttackKind.Thrust, AttackCategory.Thrust, 1f, 3.6f, w: 0.8f, dmg: 1.15f, windup: 0.48f, cd: 1.7f, ring: 1.3f),
                A("parry", AttackKind.Parry, AttackCategory.Counter, 0f, 3.2f, w: 1.2f, dmg: 0f, windup: 0.35f, cd: 2f),
                A("counter_dash", AttackKind.DashStrike, AttackCategory.Counter, 1f, 6f, w: 1f, windup: 0.35f, red: true, cd: 2f, req: TargetStateRequirement.Attacking),
                A("feint", AttackKind.Slash, AttackCategory.Feint, 0f, 2.6f, w: 0.8f, dmg: 1f, windup: 0.6f, cd: 2.4f),
                A("delayed_slash", AttackKind.Slash, AttackCategory.Delayed, 0f, 2.6f, w: 0.6f, dmg: 1.3f, windup: 0.5f, cd: 2.2f),
                A("retreat_slash", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.4f, w: 0.7f, dmg: 1f, windup: 0.4f, cd: 1.6f),
                A("sweep", AttackKind.Sweep, AttackCategory.Sweep, 0f, 3f, w: 0.5f, dmg: 1.1f, windup: 0.55f, cd: 2f, ring: 1.3f),
                A("storm_dash", AttackKind.DashStrike, AttackCategory.GapCloser, 4f, 10f, w: 0.7f, windup: 0.55f, red: true, cd: 2.4f, ring: 1.2f));


            // ------------------------------------------------------ profiles
            Profile("Bandit", "raider", ag: .8f, br: .4f, af: .8f, df: .1f, dg: .05f, pa: 0f, rt: .15f, fe: .03f, co: .05f, gb: .25f, tw: .3f,
                pref: 1.8f, min: 1.1f, max: 2.6f, low: LowHealthBehaviour.Desperate, ally: AllyDeathReaction.Hesitate,
                combos: new[] { C("rush", "quick_slash", "quick_slash_2", "heavy_overhead") });
            Profile("Assassin", "assassin", ag: .5f, br: .3f, af: .5f, df: .3f, dg: .5f, pa: .1f, rt: .6f, fe: .05f, co: .3f, gb: .1f, tw: .4f,
                pref: 2.2f, min: 1.3f, max: 3.2f, low: LowHealthBehaviour.Retreat, ally: AllyDeathReaction.Isolate, adapt: .2f,
                combos: new[] { C("in_and_out", "dash_strike", "quick_cut", "retreat_slash") });
            Profile("Spearman", "pike", ag: .5f, br: .6f, af: .6f, df: .4f, dg: .1f, pa: .2f, rt: .5f, fe: .04f, co: .2f, gb: .35f, tw: .8f,
                pref: 3.6f, min: 2.2f, max: 4.4f, low: LowHealthBehaviour.Guard, ally: AllyDeathReaction.Aggress,
                combos: new[] { C("keep_away", "long_thrust", "retreat_thrust", "long_thrust") });
            Profile("Archer", "archer", ag: .2f, br: .2f, af: .5f, df: .1f, dg: .3f, pa: 0f, rt: .9f, fe: 0f, co: 0f, gb: 0f, tw: .7f,
                pref: 7f, min: 4f, max: 9f, low: LowHealthBehaviour.Retreat, ally: AllyDeathReaction.Hesitate);
            Profile("HeavyWarrior", "axe_raider", ag: .7f, br: .8f, af: .5f, df: .3f, dg: 0f, pa: .05f, rt: .05f, fe: .05f, co: .1f, gb: .6f, tw: .3f,
                pref: 2f, min: 1.2f, max: 3f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Aggress,
                combos: new[] { C("bruise", "overhead_smash", "horizontal_sweep", "ground_shock") });
            Profile("Samurai", "samurai", ag: .55f, br: .8f, af: .5f, df: .7f, dg: .15f, pa: .8f, rt: .2f, fe: .09f, co: .7f, gb: .4f, tw: .5f,
                pref: 2.6f, min: 1.6f, max: 3.4f, low: LowHealthBehaviour.Guard, ally: AllyDeathReaction.Aggress, adapt: .4f,
                combos: new[] { C("measured", "quick_slash", "delayed_slash", "heavy_slash") });
            Profile("RogueNinja", "rogue", ag: .6f, br: .4f, af: .6f, df: .3f, dg: .6f, pa: .1f, rt: .6f, fe: .05f, co: .3f, gb: .15f, tw: .4f,
                pref: 2.4f, min: 1.4f, max: 3.6f, low: LowHealthBehaviour.Retreat, ally: AllyDeathReaction.Isolate, adapt: .2f,
                combos: new[] { C("angles", "dash_strike", "slash", "retreat_slash") });
            Profile("EliteWarrior", "elite", ag: .65f, br: .9f, af: .6f, df: .6f, dg: .2f, pa: .6f, rt: .15f, fe: .14f, co: .5f, gb: .5f, tw: .7f,
                pref: 2.2f, min: 1.4f, max: 3.2f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Aggress, adapt: .9f,
                combos: new[] { C("adaptive", "slash", "feint", "guard_break", "retreat_slash") });
            Profile("Bomber", "powder_carrier", ag: .2f, br: .1f, af: .4f, df: .1f, dg: .2f, pa: 0f, rt: .9f, fe: 0f, co: 0f, gb: 0f, tw: .8f,
                pref: 7.5f, min: 4.5f, max: 10f, low: LowHealthBehaviour.Retreat, ally: AllyDeathReaction.Hesitate);
            Profile("Shade", "shade", ag: .8f, br: .9f, af: .7f, df: .1f, dg: .3f, pa: 0f, rt: .3f, fe: .04f, co: .1f, gb: 0f, tw: .2f,
                pref: 1.9f, min: 1.1f, max: 2.6f, low: LowHealthBehaviour.Desperate, ally: AllyDeathReaction.Ignore);
            Profile("MiniBoss", "goro", ag: .8f, br: 1f, af: .6f, df: .3f, dg: 0f, pa: .1f, rt: 0f, fe: .16f, co: .2f, gb: .6f, tw: .2f,
                pref: 2.4f, min: 1.4f, max: 4f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Ignore, adapt: .3f, interval: .3f,
                combos: new[] { C("power", "heavy_slam", "sweep", "guard_break"), C("run_down", "charge", "heavy_slam") });
            Profile("Boss", "kagachi", ag: .7f, br: 1f, af: .7f, df: .7f, dg: .35f, pa: .8f, rt: .3f, fe: .2f, co: .8f, gb: .5f, tw: .3f,
                pref: 3f, min: 1.6f, max: 4f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Ignore, adapt: 1f, interval: .2f,
                combos: new[] { C("mastery", "feint", "thrust", "guard_break"), C("press", "slash", "delayed_slash", "sweep"), C("range", "retreat_slash", "spit", "dash") });
            Profile("Jin", "jin", ag: .6f, br: .9f, af: .6f, df: .8f, dg: .5f, pa: .9f, rt: .3f, fe: .15f, co: .8f, gb: .3f, tw: .1f,
                pref: 2.8f, min: 1.6f, max: 3.8f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Ignore, adapt: .9f, interval: .2f,
                combos: new[] { C("technique", "thrust", "feint", "quick_slash"), C("storm", "storm_dash", "sweep", "retreat_slash") });

            ApplyNamed();

            // ------------------------------------------------- boss phases
            // Goro: POWER. Measured, then the red mist — faster decisions, more
            // guard-breaks and sweeps, nothing held back.
            Phase("MiniBoss", 2, "goro_enraged", ag: 1f, br: 1f, af: .9f, df: .1f, dg: 0f, pa: 0f, rt: 0f, fe: .12f, co: .3f, gb: .8f, tw: .1f,
                pref: 2.2f, min: 1.2f, max: 4.4f, low: LowHealthBehaviour.Berserk, adapt: .3f, interval: .22f,
                combos: new[] { C("rage", "sweep", "heavy_slam", "guard_break"), C("run_down", "charge", "spin") });
            // Kagachi: MASTERY. 1 the swordsman, 2 the warlord, 3 the marsh, 4 the exhausted duel.
            Phase("Boss", 2, "kagachi_warlord", ag: .9f, br: 1f, af: .9f, df: .5f, dg: .3f, pa: .7f, rt: .1f, fe: .22f, co: .7f, gb: .7f, tw: .3f,
                pref: 2.2f, min: 1.4f, max: 3.6f, low: LowHealthBehaviour.Berserk, adapt: 1f, interval: .16f,
                combos: new[] { C("press", "slash", "guard_break", "sweep"), C("break", "feint", "thrust", "delayed_slash") });
            Phase("Boss", 3, "kagachi_marsh", ag: .6f, br: 1f, af: .6f, df: .6f, dg: .5f, pa: .6f, rt: .6f, fe: .2f, co: .6f, gb: .4f, tw: .6f,
                pref: 4f, min: 2f, max: 8f, low: LowHealthBehaviour.CallAllies, adapt: 1f, interval: .2f,
                combos: new[] { C("range", "retreat_slash", "spit", "dash"), C("trap", "delayed_slash", "sweep") });
            Phase("Boss", 4, "kagachi_exhausted", ag: .5f, br: .6f, af: .45f, df: .8f, dg: .2f, pa: .9f, rt: .4f, fe: .15f, co: .9f, gb: .3f, tw: 0f,
                pref: 2.6f, min: 1.6f, max: 3.2f, low: LowHealthBehaviour.Desperate, adapt: 1f, interval: .3f,
                combos: new[] { C("last", "parry", "thrust"), C("last_feint", "feint", "quick_slash") });
            // Jin: TECHNIQUE, then the storm.
            Phase("Jin", 2, "jin_storm", ag: .85f, br: 1f, af: .8f, df: .7f, dg: .6f, pa: .9f, rt: .2f, fe: .18f, co: .9f, gb: .4f, tw: 0f,
                pref: 2.4f, min: 1.4f, max: 4f, low: LowHealthBehaviour.Berserk, adapt: 1f, interval: .15f,
                combos: new[] { C("storm", "storm_dash", "sweep", "thrust"), C("edge", "feint", "counter_dash") });

            AssetDatabase.SaveAssets();
            Debug.Log("[Emberline] Combat 2.0 kits and profiles applied");
        }

        /// <summary>
        /// The campaign's named foes are copies of a base def made after the
        /// bases are kitted; this gives each its own personality on top.
        /// Safe to call before they exist (it skips) and again after.
        /// </summary>
        public static void ApplyNamed()
        {
            if (Def("paleshade") != null)
            {
                Kit("paleshade",
                    A("claws", AttackKind.Flurry, AttackCategory.Quick, 0f, 2.3f, w: 1f, windup: 0.32f, cd: 1.1f),
                    A("lunge", AttackKind.DashStrike, AttackCategory.GapCloser, 3f, 8f, w: 0.9f, windup: 0.38f, red: true, cd: 1.8f),
                    A("feint", AttackKind.Flurry, AttackCategory.Feint, 0f, 2.3f, w: 0.8f, windup: 0.6f, cd: 2.2f),
                    A("delayed_claws", AttackKind.Flurry, AttackCategory.Delayed, 0f, 2.3f, w: 0.6f, dmg: 1.2f, windup: 0.45f, cd: 2f),
                    A("spit", AttackKind.PoisonSpit, AttackCategory.Ranged, 4f, 10f, w: 0.7f, dmg: 0.5f, windup: 0.5f, red: true, cd: 3.2f, ring: 1.3f),
                    A("retreat_swipe", AttackKind.RetreatSlash, AttackCategory.RetreatAttack, 0f, 2.1f, w: 0.8f, dmg: 0.9f, windup: 0.32f, cd: 1.3f));
                Profile("paleshade", "pale_shade", ag: .7f, br: .8f, af: .8f, df: .2f, dg: .6f, pa: 0f, rt: .6f, fe: .2f, co: .3f, gb: 0f, tw: .2f,
                    pref: 2f, min: 1.1f, max: 3f, low: LowHealthBehaviour.Desperate, ally: AllyDeathReaction.Ignore, adapt: .3f, interval: .18f,
                    combos: new[] { C("speed", "lunge", "claws", "retreat_swipe"), C("bait", "feint", "delayed_claws") });
            }
            // Mini-boss rank: the brief's boss band starts at 10 %.
            if (Def("convoycaptain") != null)
                Profile("convoycaptain", "convoy_captain", ag: .6f, br: .9f, af: .55f, df: .7f, dg: .15f, pa: .8f, rt: .2f, fe: .11f, co: .7f, gb: .45f, tw: .6f,
                    pref: 2.6f, min: 1.6f, max: 3.4f, low: LowHealthBehaviour.CallAllies, ally: AllyDeathReaction.Aggress, adapt: .5f, interval: .22f,
                    combos: new[] { C("count", "quick_slash", "thrust", "guard_break") });
            if (Def("finalcommander") != null)
                Profile("finalcommander", "commander_hoshu", ag: .7f, br: 1f, af: .6f, df: .75f, dg: .2f, pa: .85f, rt: .15f, fe: .12f, co: .8f, gb: .5f, tw: .5f,
                    pref: 2.6f, min: 1.6f, max: 3.4f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Aggress, adapt: .8f, interval: .2f,
                    combos: new[] { C("gate", "feint", "guard_break", "heavy_slash"), C("hold", "parry", "thrust") });
            if (Def("raiderleader") != null)
                Profile("raiderleader", "scavenger_king", ag: .85f, br: .9f, af: .6f, df: .3f, dg: 0f, pa: .05f, rt: .05f, fe: .1f, co: .15f, gb: .65f, tw: .3f,
                    pref: 2f, min: 1.2f, max: 3f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Aggress, adapt: .3f, interval: .26f,
                    combos: new[] { C("king", "overhead_smash", "horizontal_sweep", "guard_break") });
            if (Def("drownedguardian") != null)
                Profile("drownedguardian", "drowned_guardian", ag: .6f, br: 1f, af: .55f, df: .7f, dg: .1f, pa: .7f, rt: 0f, fe: .12f, co: .6f, gb: .5f, tw: 0f,
                    pref: 2.4f, min: 1.4f, max: 3.4f, low: LowHealthBehaviour.Berserk, ally: AllyDeathReaction.Ignore, adapt: .6f, interval: .24f,
                    combos: new[] { C("warden", "slam", "delayed_slash", "guard_break") });
            if (Def("ironguard") != null)
                Profile("ironguard", "iron_guard", ag: .65f, br: 1f, af: .6f, df: .8f, dg: .15f, pa: .8f, rt: .1f, fe: .13f, co: .7f, gb: .6f, tw: .9f,
                    pref: 2.2f, min: 1.4f, max: 3.2f, low: LowHealthBehaviour.Guard, ally: AllyDeathReaction.Aggress, adapt: .9f, interval: .2f,
                    combos: new[] { C("shield", "guard_break", "thrust", "retreat_slash"), C("wall", "spin", "slam") });
            if (Def("threeblades") != null)
                Profile("threeblades", "three_blades", ag: .6f, br: .5f, af: .7f, df: .3f, dg: .6f, pa: .2f, rt: .5f, fe: .05f, co: .4f, gb: .1f, tw: .9f,
                    pref: 2.2f, min: 1.3f, max: 3.2f, low: LowHealthBehaviour.Retreat, ally: AllyDeathReaction.Aggress, adapt: .4f, interval: .2f,
                    combos: new[] { C("sisters", "dash_strike", "quick_cut", "backstab") });
        }

        // ------------------------------------------------------------ helpers

        private static EnemyDef Def(string file) => AssetDatabase.LoadAssetAtPath<EnemyDef>($"Assets/Resources/Enemies/{file}.asset");

        private static void Kit(string file, params AttackDefinition[] attacks)
        {
            var d = Def(file);
            if (d == null) { Debug.LogWarning($"[Combat] no def '{file}'"); return; }
            d.attacks = attacks;
            EditorUtility.SetDirty(d);
        }

        private static AttackDefinition A(string id, AttackKind kind, AttackCategory cat, float min, float max,
            float w = 1f, float dmg = 1f, float windup = 0f, bool red = false, float cd = 1.5f, float ring = 1f,
            TargetStateRequirement req = TargetStateRequirement.Any) => new()
        {
            id = id, displayName = id.ToUpperInvariant().Replace('_', ' '), kind = kind, category = cat,
            minRange = min, maxRange = max, weight = w, damageMultiplier = dmg, windupOverride = windup,
            redTelegraph = red, cooldown = cd, telegraphScale = ring, requires = req,
            parryable = kind != AttackKind.GuardBreak, interruptible = cat != AttackCategory.Heavy,
            canFeint = cat == AttackCategory.Feint, canChain = true,
        };

        private static ComboChain C(string name, params string[] steps) => new() { name = name, steps = steps };

        /// <summary>A later phase's personality for a boss def.</summary>
        private static void Phase(string file, int phase, string id, float ag, float br, float af, float df, float dg,
            float pa, float rt, float fe, float co, float gb, float tw, float pref, float min, float max,
            LowHealthBehaviour low, float adapt, float interval, ComboChain[] combos = null)
        {
            var d = Def(file);
            if (d == null) return;
            var path = $"{ProfileDir}/Profile_{id}.asset";
            var p = AssetDatabase.LoadAssetAtPath<EnemyCombatProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<EnemyCombatProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.id = id; p.aggression = ag; p.bravery = br; p.attackFrequency = af; p.defenseFrequency = df;
            p.dodgeFrequency = dg; p.parryAbility = pa; p.retreatTendency = rt; p.feintFrequency = fe;
            p.counterFrequency = co; p.guardBreakFrequency = gb; p.teamwork = tw;
            p.preferredDistance = pref; p.minDistance = min; p.maxDistance = max;
            p.retreatDistance = min * 1.1f; p.approachDistance = max * 1.3f;
            p.lowHealth = low; p.allyDeath = AllyDeathReaction.Ignore; p.adaptation = adapt;
            p.reactionToPlayerAggression = adapt; p.decisionInterval = interval; p.combos = combos;
            combos ??= System.Array.Empty<ComboChain>();
            p.comboLength = combos.Length > 0 ? Mathf.Clamp(combos[0].steps.Length, 1, 4) : 1;
            EditorUtility.SetDirty(p);
            if (phase == 2) d.phase2Profile = p; else if (phase == 3) d.phase3Profile = p; else d.phase4Profile = p;
            EditorUtility.SetDirty(d);
        }

        private static void Profile(string file, string id, float ag, float br, float af, float df, float dg, float pa,
            float rt, float fe, float co, float gb, float tw, float pref, float min, float max,
            LowHealthBehaviour low, AllyDeathReaction ally, float adapt = 0f, float interval = 0.25f,
            ComboChain[] combos = null)
        {
            var d = Def(file);
            if (d == null) return;
            var path = $"{ProfileDir}/Profile_{id}.asset";
            var p = AssetDatabase.LoadAssetAtPath<EnemyCombatProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<EnemyCombatProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            p.id = id;
            p.aggression = ag; p.bravery = br; p.attackFrequency = af; p.defenseFrequency = df;
            p.dodgeFrequency = dg; p.parryAbility = pa; p.retreatTendency = rt; p.feintFrequency = fe;
            p.counterFrequency = co; p.guardBreakFrequency = gb; p.teamwork = tw;
            p.preferredDistance = pref; p.minDistance = min; p.maxDistance = max;
            p.retreatDistance = min * 1.1f; p.approachDistance = max * 1.3f;
            p.lowHealth = low; p.allyDeath = ally; p.adaptation = adapt; p.decisionInterval = interval;
            p.reactionToPlayerAggression = adapt;
            p.combos = combos ?? System.Array.Empty<ComboChain>();
            p.comboLength = combos != null && combos.Length > 0 ? Mathf.Clamp(combos[0].steps.Length, 1, 4) : 1;
            EditorUtility.SetDirty(p);
            d.profile = p;
            // The profile carries the personality now; the Phase 3 knobs the
            // brain still reads stay as authored, so nothing regresses.
            EditorUtility.SetDirty(d);
        }
    }
}
