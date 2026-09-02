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
        public static float LogEvery = 5f;

        private static float _next;

        public static void Reset()
        {
            Attacks = OutOfTurnPunishes = ReactiveBlocks = Dodges = Ripostes = 0;
            Retreats = GuardHolds = ProtectMoves = StaggersShortened = 0;
            MaxSimultaneousAttackers = 0;
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
              .Append(" shortStaggers=").Append(StaggersShortened);
            Debug.Log(sb.ToString());
        }
    }
}
