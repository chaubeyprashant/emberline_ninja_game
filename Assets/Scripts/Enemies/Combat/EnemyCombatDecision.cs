namespace Emberline.Enemies
{
    /// <summary>
    /// One scored candidate. Kept as a struct so the selector allocates nothing
    /// and the debug overlay can show exactly why an attack won.
    /// </summary>
    public struct EnemyCombatDecision
    {
        public AttackDefinition attack;
        public float distance;     // fit to the attack's band and preferred range
        public float position;     // front / side / back / back-turned
        public float playerState;  // attacking, guarding, dodging, recovering, staggered, retreating, circling
        public float tactical;     // allies, tokens, roles, surrounded
        public float personality;  // what this enemy likes
        public float adaptation;   // what the player has been doing
        public float repetition;   // history multiplier
        public float cooldownOk;   // 0 or 1

        public float Total => (distance + position + playerState + tactical + personality + adaptation)
                              * repetition * cooldownOk;

        public override string ToString() => attack == null ? "-"
            : $"{attack.id} {Total:0.00} (d{distance:0.0} p{position:0.0} s{playerState:0.0} t{tactical:0.0} " +
              $"pe{personality:0.0} a{adaptation:0.0} ×r{repetition:0.00})";
    }

    /// <summary>What the selector saw when it decided — for the overlay and tests.</summary>
    public enum ObservedPlayerState
    {
        Neutral, Attacking, Guarding, Dodging, Recovering, Staggered, Retreating, Circling, BackTurned,
    }
}
