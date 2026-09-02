namespace Emberline.Missions
{
    /// <summary>
    /// An optional objective that is a *condition on the whole mission* rather
    /// than a stage you can skip. A skippable stage is a chore: it adds length,
    /// not decisions. A condition changes how you play the mission you are
    /// already in, which is what an optional objective is for.
    /// </summary>
    public enum MissionChallenge
    {
        None,
        NoAlarm,           // never let the alarm go up
        SaveAllPrisoners,  // every prisoner freed before the mission ends
        NoCivilianDeaths,  // no villager dies, whoever killed them
        SilentKill,        // the marked target dies without having noticed you
        UnderTime,         // finish inside the plan's time limit
    }
}
