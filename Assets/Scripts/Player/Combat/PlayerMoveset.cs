using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// A weapon's full set of contextual attacks. One asset per weapon; the
    /// controller resolves the button through it.
    /// </summary>
    [CreateAssetMenu(menuName = "Emberline/Player Moveset")]
    public class PlayerMoveset : ScriptableObject
    {
        public string weaponId = "katana";
        public PlayerAttackDefinition[] attacks = System.Array.Empty<PlayerAttackDefinition>();

        /// <summary>The attack for a context, or null when this weapon has none.</summary>
        public PlayerAttackDefinition For(AttackContext ctx)
        {
            for (var i = 0; i < attacks.Length; i++)
                if (attacks[i].context == ctx) return attacks[i];
            return null;
        }

        private static PlayerMoveset[] _all;

        public static PlayerMoveset ForWeapon(string weaponId)
        {
            _all ??= Resources.LoadAll<PlayerMoveset>("Attacks");
            for (var i = 0; i < _all.Length; i++)
                if (_all[i] != null && _all[i].weaponId == weaponId) return _all[i];
            return null;
        }
    }
}
