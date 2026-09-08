using UnityEngine;

namespace Emberline.Combat
{
    /// <summary>
    /// Where a swing is in its own life. Before this, an attack was a single
    /// instant: the damage landed on the frame the button went down and the
    /// animation played afterwards as decoration. A swing now has a shape, and
    /// the blade only threatens anyone during <see cref="Active"/>.
    /// </summary>
    public enum AttackPhase
    {
        Startup,        // committed, weapon travelling, nothing can be hit yet
        Active,         // the blade is dangerous; this is when tracing runs
        FollowThrough,  // the blade has passed through; still committed
        Recovery,       // returning to guard; cancellable by the rules table
        Done,
    }

    /// <summary>
    /// The phase boundaries of one swing, in seconds from its start.
    ///
    /// <para>
    /// The numbers are not invented. This project has no animation events — clips
    /// are harvested from the source FBX by the character factory and the
    /// generated controller has one layer and no event tracks — but
    /// <c>SkeletalRig.PlayOneShot(pose, duration)</c> time-scales the clip to a
    /// duration the code chooses. Because the code already decides how long the
    /// animation takes, phases expressed as fractions of that duration are locked
    /// to the animation by construction. Animation events would be strictly worse
    /// here: they would have to be re-authored on every retargeted clip and they
    /// would drift the moment a swing is time-scaled.
    /// </para>
    ///
    /// <para>
    /// An attack may still state any boundary in absolute seconds. Authored
    /// seconds win; the fractions are only the default shape of a swing.
    /// </para>
    /// </summary>
    public readonly struct AttackWindow
    {
        public readonly float StartupEnd;
        public readonly float ActiveEnd;
        public readonly float FollowEnd;
        public readonly float Total;

        public float ActiveLength => ActiveEnd - StartupEnd;

        /// <summary>Default share of a swing spent winding up before contact.</summary>
        public const float DefaultStartupFrac = 0.34f;

        /// <summary>Default share the blade spends dangerous.</summary>
        public const float DefaultActiveFrac = 0.30f;

        /// <summary>Default share spent completing the arc after contact.</summary>
        public const float DefaultFollowFrac = 0.16f;

        public AttackWindow(float animTime, float startupSeconds, float activeSeconds,
            float recoverySeconds)
        {
            var swing = Mathf.Max(0.05f, animTime);
            var startup = startupSeconds > 0f ? startupSeconds : swing * DefaultStartupFrac;
            var active = activeSeconds > 0f ? activeSeconds : swing * DefaultActiveFrac;

            // A swing that would spend its whole length winding up can never hit.
            // Clamp rather than reject: an authored 0.5 s startup on a 0.28 s jab
            // is a tuning mistake, not a reason to drop the attack.
            startup = Mathf.Min(startup, swing * 0.6f);
            active = Mathf.Max(active, 0.04f);

            StartupEnd = startup;
            ActiveEnd = startup + active;
            FollowEnd = Mathf.Max(ActiveEnd, swing * (1f - DefaultFollowFrac) < ActiveEnd
                ? ActiveEnd + swing * DefaultFollowFrac
                : swing);
            Total = FollowEnd + Mathf.Max(0f, recoverySeconds);
        }

        public AttackPhase PhaseAt(float elapsed)
        {
            if (elapsed < StartupEnd) return AttackPhase.Startup;
            if (elapsed < ActiveEnd) return AttackPhase.Active;
            if (elapsed < FollowEnd) return AttackPhase.FollowThrough;
            return elapsed < Total ? AttackPhase.Recovery : AttackPhase.Done;
        }

        public override string ToString() =>
            $"startup {StartupEnd * 1000f:F0}ms active {StartupEnd * 1000f:F0}–{ActiveEnd * 1000f:F0}ms total {Total * 1000f:F0}ms";
    }
}
