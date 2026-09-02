using Emberline.Core;
using UnityEngine;

namespace Emberline.Missions
{
    /// <summary>
    /// Watches a mission's optional condition. Kept apart from the director so
    /// the flow logic does not grow a branch per challenge, and so the HUD has
    /// one thing to read.
    ///
    /// A challenge can fail early and irreversibly (an alarm, a dead villager);
    /// that is the point of a condition. It is *reported* the moment it breaks,
    /// so the player knows the run is no longer clean while there is still a
    /// mission to finish rather than discovering it on the results screen.
    /// </summary>
    public class ChallengeTracker
    {
        public MissionChallenge Kind { get; }
        public float Limit { get; }
        public int Reward { get; }
        public bool Broken { get; private set; }

        private readonly GameManager _gm;
        private float _elapsed;
        private bool _announcedBreak;

        public ChallengeTracker(MissionPlan plan, GameManager gm)
        {
            _gm = gm;
            Kind = plan != null ? plan.challenge : MissionChallenge.None;
            Limit = plan != null ? plan.challengeSeconds : 0f;
            Reward = plan != null ? plan.challengeShards : 0;
        }

        public bool Active => Kind != MissionChallenge.None;

        /// <summary>True while the challenge can still be earned.</summary>
        public bool Intact => Active && !Broken;

        /// <summary>One line for the HUD; empty when there is no challenge.</summary>
        public string Line
        {
            get
            {
                if (!Active) return "";
                var head = Kind switch
                {
                    MissionChallenge.NoAlarm => "UNSEEN",
                    MissionChallenge.SaveAllPrisoners => "FREE EVERY PRISONER",
                    MissionChallenge.NoCivilianDeaths => "NO VILLAGER DIES",
                    MissionChallenge.SilentKill => "SILENCE THE TARGET",
                    MissionChallenge.UnderTime => "BEAT THE CLOCK",
                    _ => "",
                };
                if (Broken) return head + " — FAILED";
                return Kind switch
                {
                    MissionChallenge.SaveAllPrisoners =>
                        $"{head} — {Prisoner.Freed}/{Prisoner.Total}",
                    MissionChallenge.UnderTime =>
                        $"{head} — {Mathf.Max(0, Mathf.CeilToInt(Limit - _elapsed))}s",
                    _ => head,
                };
            }
        }

        public void Tick(float dt)
        {
            if (!Active || Broken) return;
            _elapsed += dt;

            switch (Kind)
            {
                case MissionChallenge.NoAlarm:
                    if (_gm != null && _gm.AlarmRaised) Break("SEEN");
                    break;
                case MissionChallenge.NoCivilianDeaths:
                    if (Villager.Died > 0) Break("A VILLAGER IS DEAD");
                    break;
                case MissionChallenge.UnderTime:
                    if (_elapsed > Limit) Break("TOO SLOW");
                    break;
                case MissionChallenge.SilentKill:
                    if (SilentKillFailed) Break("HE SAW YOU COMING");
                    break;
            }
        }

        /// <summary>Set by the director when a marked target dies having seen you.</summary>
        public bool SilentKillFailed { get; set; }

        /// <summary>Evaluated once, when the mission ends.</summary>
        public bool Earned()
        {
            if (!Active || Broken) return false;
            return Kind switch
            {
                MissionChallenge.SaveAllPrisoners => Prisoner.Total > 0 && Prisoner.Freed >= Prisoner.Total,
                MissionChallenge.NoCivilianDeaths => Villager.Died == 0,
                MissionChallenge.UnderTime => _elapsed <= Limit,
                _ => true,
            };
        }

        private void Break(string why)
        {
            Broken = true;
            if (_announcedBreak) return;
            _announcedBreak = true;
            _gm?.Announce($"OPTIONAL LOST — {why}");
        }
    }
}
