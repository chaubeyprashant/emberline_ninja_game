using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Per-weapon upgrade levels bought with Ryo. Upgrades are multipliers applied
    /// on top of the WeaponDef rather than edits to it, so the asset stays the
    /// single source of a weapon's identity and a reset costs nothing.
    /// </summary>
    public static class WeaponUpgrades
    {
        public const int MaxLevel = 5;

        /// <summary>What a track improves. One enum keeps the buy screen generic.</summary>
        public enum Track { Damage, Reach, Speed }

        public static int Level(string weaponId, Track track) =>
            Mathf.Clamp(PlayerPrefs.GetInt(Key(weaponId, track), 0), 0, MaxLevel);

        /// <summary>Rising cost, so the last point of a track is a real decision.</summary>
        public static int Cost(string weaponId, Track track)
        {
            var lv = Level(weaponId, track);
            return lv >= MaxLevel ? 0 : 120 + lv * 110;
        }

        public static bool TryBuy(string weaponId, Track track)
        {
            var lv = Level(weaponId, track);
            if (lv >= MaxLevel) return false;
            if (!Wallet.TrySpend(Cost(weaponId, track))) return false;
            PlayerPrefs.SetInt(Key(weaponId, track), lv + 1);
            PlayerPrefs.Save();
            return true;
        }

        // ---- multipliers read by CombatController when it applies a weapon ----

        /// <summary>+6% strike and cleave damage per level.</summary>
        public static float DamageMul(string id) => 1f + 0.06f * Level(id, Track.Damage);

        /// <summary>+4% reach per level. Small on purpose: reach is the strongest
        /// stat in this combat model, and doubling it would erase spacing play.</summary>
        public static float RangeMul(string id) => 1f + 0.04f * Level(id, Track.Reach);

        /// <summary>Up to 20% off cleave cooldown and strike wind-up.</summary>
        public static float SpeedMul(string id) => 1f - 0.04f * Level(id, Track.Speed);

        public static int TotalLevels(string id) =>
            Level(id, Track.Damage) + Level(id, Track.Reach) + Level(id, Track.Speed);

        private static string Key(string id, Track t) => $"wup_{id}_{(int)t}";
    }
}
