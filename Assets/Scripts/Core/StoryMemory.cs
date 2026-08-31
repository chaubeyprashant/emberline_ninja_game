using UnityEngine;
using Emberline.Enemies;

namespace Emberline.Core
{
    /// <summary>
    /// What the player has seen and learned, persisted in PlayerPrefs: lore
    /// codex unlocks (first kill of each enemy kind) and per-level dialogue
    /// history. Feeds the Codex screen and Endless-mode lore drops.
    /// </summary>
    public static class StoryMemory
    {
        public static void UnlockLore(EnemyKind kind)
        {
            if (HasLore(kind)) return;
            PlayerPrefs.SetInt("lore_" + kind, 1);
            PlayerPrefs.Save();
        }

        public static bool HasLore(EnemyKind kind) => PlayerPrefs.GetInt("lore_" + kind, 0) == 1;

        public static int LoreCount
        {
            get
            {
                var n = 0;
                foreach (EnemyKind k in System.Enum.GetValues(typeof(EnemyKind)))
                    if (HasLore(k)) n++;
                return n;
            }
        }

        public static void MarkDialogueSeen(int levelId)
        {
            PlayerPrefs.SetInt("dlg_seen_" + levelId, 1);
            PlayerPrefs.Save();
        }

        public static bool DialogueSeen(int levelId) => PlayerPrefs.GetInt("dlg_seen_" + levelId, 0) == 1;

        /// <summary>Codex entry per enemy kind: name, story, weakness tip.</summary>
        public static (string name, string story, string tip) Lore(EnemyKind kind) => kind switch
        {
            EnemyKind.Bandit => ("BANDIT RAIDER",
                "Hired knives from the toll roads. They fight in packs and die alone.",
                "TIP — Their rush is greedy: dodge through it and the counter window is yours."),
            EnemyKind.Ranged => ("LANTERN ARCHER",
                "Hooded weavers who fire stolen lantern-light. They fear the dark they serve.",
                "TIP — Strikes from behind hit twice as hard. Flicker past their volley."),
            EnemyKind.Shade => ("MARSH SHADE",
                "Drowned merchants who still remember warmth. They reach for the flame, not for you.",
                "TIP — They are half-formed while appearing: strike early for double damage."),
            EnemyKind.Chief => ("GORO, THE TOLL-CAPTAIN",
                "A rooftop tyrant in lacquered red. He collects for the Serpent — and skims the rest.",
                "TIP — His enrage trades armor for fury. Crush hits still stagger him."),
            EnemyKind.Kagachi => ("KAGACHI, THE MARSH SERPENT",
                "The gatherer beneath Ashfen. It has collected a hundred lights, and wants one more.",
                "TIP — The mirrors are thin: one crush shatters a clone."),
            EnemyKind.Jin => ("JIN KUROGANE, THE STORM BLADE",
                "A duelist who cut away everything that slowed the sword — including mercy.",
                "TIP — He answers every wide swing. Dodge his red dash and punish the recovery."),
            _ => ("?", "", ""),
        };
    }
}
