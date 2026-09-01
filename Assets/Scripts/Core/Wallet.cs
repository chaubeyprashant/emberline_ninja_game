using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Ryo: the spending currency, earned by marching and spent on weapon
    /// upgrades and cosmetics. Deliberately separate from Ember Shards, which
    /// are the skill-tree currency — mixing them would mean every cosmetic
    /// purchase competed with a combat unlock, and one of the two always loses.
    /// </summary>
    public static class Wallet
    {
        public static int Ryo
        {
            get => PlayerPrefs.GetInt("ryo", 0);
            private set { PlayerPrefs.SetInt("ryo", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Total ever earned — the number the records screen shows.</summary>
        public static int LifetimeRyo
        {
            get => PlayerPrefs.GetInt("ryo_total", 0);
            private set { PlayerPrefs.SetInt("ryo_total", Mathf.Max(0, value)); }
        }

        public static void Earn(int amount)
        {
            if (amount <= 0) return;
            LifetimeRyo += amount;
            Ryo += amount;
        }

        public static bool CanAfford(int cost) => Ryo >= cost;

        public static bool TrySpend(int cost)
        {
            if (cost < 0 || Ryo < cost) return false;
            Ryo -= cost;
            return true;
        }
    }
}
