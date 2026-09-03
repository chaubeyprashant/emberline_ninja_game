using Emberline.Core;
using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// The situation an attack is the answer to. The attack button is one
    /// button; the context decides what it does. Ordered by priority: when
    /// several apply, the first wins.
    /// </summary>
    public enum AttackContext
    {
        Assassination,   // target unaware and behind it
        StaggerPunish,   // target guard-broken or staggered
        ParryCounter,    // inside the perfect-parry counter window
        DodgeCounter,    // inside the perfect-dodge counter window
        BackAttack,      // behind an aware target
        GuardBreakPunish,// target is blocking
        GapCloser,       // target retreating or just out of reach
        Air,             // airborne
        WallRun,         // on a wall
        Running,         // at full sprint into range
        HeavyFinisher,   // heavy pressed after two lights
        HeavyThrust,     // heavy pressed after one light
        HeavyFollow,     // light pressed straight after a heavy
        Heavy,           // cold heavy
        Chain3,          // third light
        Chain2,          // second light
        Chain1,          // first light
    }

    /// <summary>
    /// One of Renzo's attacks — the data-driven counterpart of the enemy
    /// AttackDefinition. Authored per weapon in a PlayerMoveset. Timings are the
    /// commitment the player buys; the reactions are what the enemy suffers.
    /// </summary>
    [System.Serializable]
    public class PlayerAttackDefinition
    {
        public string id = "";
        public string displayName = "";
        public AttackContext context = AttackContext.Chain1;

        [Header("Timing")]
        [Tooltip("Committed state length. The swing resolves at startup's end.")]
        public float startup = 0.05f;
        public float animTime = 0.28f;
        [Tooltip("Recover state after the swing; 0 = none (chains stay fluid).")]
        public float recovery;
        public float cooldown;

        [Header("Reach")]
        public float rangeMultiplier = 1f;
        public float arcDeg = 130f;
        [Tooltip("Ground covered along facing when the attack starts.")]
        public float lunge = 5.5f;

        [Header("Force")]
        public float damageMultiplier = 1f;
        public float postureMultiplier = 1f;
        public bool crush;
        public bool launch;
        public bool execute;

        [Header("Presentation")]
        public RigPose pose = RigPose.Strike1;
        public bool heavyWhoosh;
        public float cameraImpact;
        public float hitStop = 0.04f;

        [Tooltip("Chain stage this attack advances to (1..n); 0 leaves the chain alone.")]
        public int chainStage;
    }
}
