using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// One batch entry point for the whole campaign pipeline: author the plans,
    /// then run every validator, then write the document. Used from the command
    /// line so a campaign change is verified without opening the editor.
    /// </summary>
    public static class EmberCampaignBatch
    {
        /// <summary>Set while the validators are run back to back, so the ones
        /// that normally end a batch session let the next one run.</summary>
        public static bool Chained;

        [MenuItem("Emberline/Rebuild And Check Campaign")]
        public static void All()
        {
            Chained = true;
            EmberMissions.BuildMissions();
            EmberCampaignCheck.Run();
            EmberMissionCheck.Run();
            EmberDesignCheck.Run();
            EmberCampaignDoc.Write();
            var fail = EmberCampaignCheck.Failures + EmberMissionCheck.Failures + EmberDesignCheck.Failures;
            Debug.Log(fail == 0
                ? "[Emberline] Campaign pipeline finished: everything passed."
                : $"[Emberline] Campaign pipeline finished: {fail} problem(s).");
            Chained = false;
            if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
        }
    }
}
