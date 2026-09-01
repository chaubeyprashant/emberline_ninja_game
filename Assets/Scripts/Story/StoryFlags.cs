using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// Story progression, saved. Mirrors the PlayerPrefs pattern the rest of the
    /// game already uses (skills, checkpoints, feats) rather than introducing a
    /// second save mechanism for one feature.
    /// </summary>
    public static class StoryFlags
    {
        /// <summary>Has this cinematic played to the end at least once? Gates skip.</summary>
        public static bool Seen(string beatId) =>
            !string.IsNullOrEmpty(beatId) && PlayerPrefs.GetInt("beat_" + beatId, 0) == 1;

        public static void MarkSeen(string beatId)
        {
            if (string.IsNullOrEmpty(beatId)) return;
            PlayerPrefs.SetInt("beat_" + beatId, 1);
            PlayerPrefs.Save();
        }

        /// <summary>True before the opening has ever run — drives the fresh-install flow.</summary>
        public static bool IsFreshInstall => !Seen("opening");

        /// <summary>Named story facts, for flashbacks and NPC lines that react.</summary>
        public static bool Flag(string id) => PlayerPrefs.GetInt("sf_" + id, 0) == 1;

        public static void SetFlag(string id)
        {
            PlayerPrefs.SetInt("sf_" + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Wipe story progress only — used by the fresh-install flow test.</summary>
        public static void ResetAll()
        {
            foreach (var id in new[] { "opening", "village", "aiko", "snow" })
                PlayerPrefs.DeleteKey("beat_" + id);
            PlayerPrefs.Save();
        }
    }
}
