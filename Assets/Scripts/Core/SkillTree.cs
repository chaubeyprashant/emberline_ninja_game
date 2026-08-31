using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Meta progression: nine nodes across three branches, bought with Ember
    /// Shards earned from mission ranks. Persisted in PlayerPrefs; effects are
    /// read at their use sites (CombatController, PlayerLocomotion, SenGates,
    /// GameManager) so gameplay code stays the source of truth for behavior.
    /// </summary>
    public static class SkillTree
    {
        public class Node
        {
            public string id, branch, title, desc;
            public int cost;
        }

        public static readonly List<Node> Nodes = new()
        {
            new Node { id = "cleave_dmg", branch = "COMBAT", title = "HEAVY EMBER",
                desc = "Cleave deals +25% damage.", cost = 2 },
            new Node { id = "combo_window", branch = "COMBAT", title = "LONG THREAD",
                desc = "Strike chain window +0.2s.", cost = 2 },
            new Node { id = "finisher_power", branch = "COMBAT", title = "THREAD BURST",
                desc = "10-combo ember bursts deal +50%.", cost = 3 },
            new Node { id = "flicker_haste", branch = "DEFENSE", title = "SECOND STEP",
                desc = "Flicker Step recharges 35% faster.", cost = 3 },
            new Node { id = "dodge_heal", branch = "DEFENSE", title = "EMBER SALVE",
                desc = "Perfect dodges restore 5 HP.", cost = 2 },
            new Node { id = "gate_mend", branch = "DEFENSE", title = "STEADY GATES",
                desc = "Clearing a wave mends 2 Gates.", cost = 2 },
            new Node { id = "surge_radius", branch = "EMBER", title = "WIDE NOVA",
                desc = "Surge radius +20%.", cost = 2 },
            new Node { id = "sen_flow", branch = "EMBER", title = "SEN FLOW",
                desc = "Sen regenerates 30% faster.", cost = 2 },
            new Node { id = "lantern_burn", branch = "EMBER", title = "LANTERN'S WRATH",
                desc = "Every 8s the lantern scorches nearby foes.", cost = 3 },
        };

        public static int Shards
        {
            get => PlayerPrefs.GetInt("ember_shards", 0);
            set { PlayerPrefs.SetInt("ember_shards", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static bool Has(string id) => PlayerPrefs.GetInt("skill_" + id, 0) == 1;

        public static bool TryBuy(Node node)
        {
            if (Has(node.id) || Shards < node.cost) return false;
            Shards -= node.cost;
            PlayerPrefs.SetInt("skill_" + node.id, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int OwnedCount
        {
            get
            {
                var n = 0;
                foreach (var node in Nodes) if (Has(node.id)) n++;
                return n;
            }
        }
    }
}
