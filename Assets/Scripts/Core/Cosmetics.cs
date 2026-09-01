using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Cosmetic equipment: dyes for Renzo's cloth, bought with Ryo.
    ///
    /// These are multiplicative tints over the character's albedo atlas, not
    /// per-garment recolours. The models carry one material and one atlas with no
    /// garment mask (see docs/ART_DIRECTION.md §4), so tinting is the only honest
    /// option available today — which is also why every dye here darkens or shifts
    /// hue rather than lightening: a multiply cannot brighten what it is given.
    /// Pairs with <see cref="BladeFinish"/>, which does the same job for the weapon.
    /// </summary>
    public static class Cosmetics
    {
        public readonly struct Set
        {
            public readonly string Id, Name;
            public readonly Color Dye;    // multiplied over the character atlas
            public readonly Color Accent; // trail and scarf
            public readonly int Cost;

            public Set(string id, string name, Color dye, Color accent, int cost)
            {
                Id = id; Name = name; Dye = dye; Accent = accent; Cost = cost;
            }
        }

        public static readonly Set[] All =
        {
            new("ash",   "ASHFALL",      Color.white,                    new Color(0.62f, 0.22f, 0.16f), 0),
            new("slate", "SLATE RAIN",   new Color(0.72f, 0.78f, 0.92f), new Color(0.40f, 0.55f, 0.78f), 400),
            new("moss",  "GREENWOOD",    new Color(0.74f, 0.86f, 0.64f), new Color(0.45f, 0.62f, 0.28f), 400),
            new("plum",  "PLUM RAIN",    new Color(0.86f, 0.68f, 0.90f), new Color(0.70f, 0.35f, 0.52f), 700),
            new("gold",  "LANTERN GOLD", new Color(1.00f, 0.86f, 0.60f), new Color(0.92f, 0.72f, 0.30f), 1100),
            new("drown", "DROWNED",      new Color(0.60f, 0.90f, 0.84f), new Color(0.35f, 0.78f, 0.62f), 1400),
        };

        public static bool IsOwned(Set s) => s.Cost <= 0 || PlayerPrefs.GetInt("cos_" + s.Id, 0) == 1;

        public static bool TryBuy(Set s)
        {
            if (IsOwned(s)) return false;
            if (!Wallet.TrySpend(s.Cost)) return false;
            PlayerPrefs.SetInt("cos_" + s.Id, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static Set Current
        {
            get
            {
                var id = PlayerPrefs.GetString("cos_sel", "ash");
                foreach (var s in All)
                    if (s.Id == id && IsOwned(s)) return s;
                return All[0];
            }
        }

        public static void Select(Set s)
        {
            if (!IsOwned(s)) return;
            PlayerPrefs.SetString("cos_sel", s.Id);
            PlayerPrefs.Save();
        }

        public static int OwnedCount
        {
            get
            {
                var n = 0;
                foreach (var s in All) if (IsOwned(s)) n++;
                return n;
            }
        }

        /// <summary>
        /// Tint the character's own renderers. Prop renderers are skipped — the
        /// blade finish owns those, and two systems writing one property block
        /// would mean whichever ran last won.
        /// </summary>
        public static void ApplyTo(GameObject character)
        {
            if (character == null) return;
            var set = Current;
            if (set.Dye == Color.white) return;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", set.Dye);
            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
            {
                if (IsProp(r.transform)) continue;
                r.SetPropertyBlock(mpb);
            }
        }

        private static bool IsProp(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.StartsWith("Prop_")) return true;
            return false;
        }
    }
}
