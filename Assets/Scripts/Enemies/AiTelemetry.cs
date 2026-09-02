using System.Text;
using UnityEngine;

namespace Emberline.Enemies
{
    /// <summary>
    /// Counters for judging whether a fight behaved like trained warriors or a
    /// queue. Cheap increments during play; a one-line summary to the log every
    /// few seconds so a device session leaves evidence in logcat rather than an
    /// impression. Reset per mission by the GameManager.
    /// </summary>
    public static class AiTelemetry
    {
        public static int Attacks, OutOfTurnPunishes, ReactiveBlocks, Dodges, Ripostes,
            Retreats, GuardHolds, ProtectMoves, StaggersShortened;
        public static int MaxSimultaneousAttackers;

        // Combat 2.0: what kind of attacks, and how varied.
        public static int GuardBreaks, Delayed, Thrusts, Sweeps, Feints, Backstabs, GapClosers, RetreatAttacks, Counters;
        public static int MissedHeavies, ParryRecoils, AllyReactions, PhaseChanges, GuardBreaksLanded;
        public static int MaxSameAttackRun;
        public static float ArcherMinDistance = float.MaxValue;
        private static readonly System.Collections.Generic.HashSet<string> DistinctIds = new();
        private static string _lastId; private static int _run;

        public static int DistinctAttacks => DistinctIds.Count;

        /// <summary>Record one committed attack: its category, and the run of identical picks.</summary>
        public static void Committed(AttackDefinition a)
        {
            if (a == null) return;
            DistinctIds.Add(a.id);
            _run = a.id == _lastId ? _run + 1 : 1;
            _lastId = a.id;
            if (_run > MaxSameAttackRun) MaxSameAttackRun = _run;
            switch (a.category)
            {
                case AttackCategory.GuardBreak: GuardBreaks++; break;
                case AttackCategory.Delayed: Delayed++; break;
                case AttackCategory.Thrust: Thrusts++; break;
                case AttackCategory.Sweep: Sweeps++; break;
                case AttackCategory.Feint: Feints++; break;
                case AttackCategory.GapCloser: GapClosers++; break;
                case AttackCategory.RetreatAttack: RetreatAttacks++; break;
                case AttackCategory.Counter: Counters++; break;
            }
            if (a.requires == TargetStateRequirement.BackTurned) Backstabs++;
        }
        public static float LogEvery = 5f;

        private static float _next;

        public static void Reset()
        {
            Attacks = OutOfTurnPunishes = ReactiveBlocks = Dodges = Ripostes = 0;
            Retreats = GuardHolds = ProtectMoves = StaggersShortened = 0;
            MaxSimultaneousAttackers = 0;
            GuardBreaks = Delayed = Thrusts = Sweeps = Feints = Backstabs = GapClosers = RetreatAttacks = Counters = 0;
            MissedHeavies = ParryRecoils = AllyReactions = PhaseChanges = GuardBreaksLanded = 0;
            MaxSameAttackRun = 0; ArcherMinDistance = float.MaxValue; DistinctIds.Clear(); _lastId = null; _run = 0;
            _next = Time.time + LogEvery;
        }

        /// <summary>Called once per frame by the coordinator; samples the attacker count.</summary>
        public static void Sample(int attackersNow, int alive)
        {
            if (attackersNow > MaxSimultaneousAttackers) MaxSimultaneousAttackers = attackersNow;
            if (Time.time < _next || alive == 0) return;
            _next = Time.time + LogEvery;
            var sb = new StringBuilder(160);
            sb.Append("[AI] alive=").Append(alive)
              .Append(" attacks=").Append(Attacks)
              .Append(" maxSimul=").Append(MaxSimultaneousAttackers)
              .Append(" punish=").Append(OutOfTurnPunishes)
              .Append(" blocks=").Append(ReactiveBlocks)
              .Append(" dodges=").Append(Dodges)
              .Append(" ripostes=").Append(Ripostes)
              .Append(" retreats=").Append(Retreats)
              .Append(" guards=").Append(GuardHolds)
              .Append(" protects=").Append(ProtectMoves)
              .Append(" shortStaggers=").Append(StaggersShortened)
              .Append(" distinct=").Append(DistinctAttacks).Append(" maxRun=").Append(MaxSameAttackRun)
              .Append(" gb=").Append(GuardBreaks).Append(" delayed=").Append(Delayed)
              .Append(" thrust=").Append(Thrusts).Append(" sweep=").Append(Sweeps).Append(" feint=").Append(Feints);
            Debug.Log(sb.ToString());
        }
    }
}
