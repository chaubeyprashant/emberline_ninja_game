using UnityEngine;

namespace Emberline.Core
{
    public enum RigMood { Calm, Focused, Enraged }

    /// <summary>Which locomotion family the mover is in. Only skeletal player rigs
    /// carry all three; others treat everything as Ground.</summary>
    public enum LocoMode { Ground, Crouch, Swim }

    /// <summary>
    /// Contract between gameplay (CombatController, EnemyBrain, PlayerLocomotion,
    /// GameManager) and whatever renders the character. Two implementations:
    /// NinjaRig (procedural primitives, legacy fallback) and SkeletalRig
    /// (imported skinned mesh + Animator, KayKit characters).
    /// </summary>
    public abstract class CharacterRig : MonoBehaviour
    {
        /// <summary>
        /// Locomotion blend 0..1, written every frame by the mover. For enemies it
        /// is Idle→Run; for the player's gait tree 0.4 is a walk, 0.75 a run and
        /// 1 a sprint (PlayerLocomotion.GaitBlend).
        /// </summary>
        [System.NonSerialized] public float move01;

        /// <summary>Ground, crouch or swim locomotion. Ignored by rigs without the states.</summary>
        public virtual void SetLocoMode(LocoMode mode) { }

        /// <summary>Play a pose once over `duration`, then return to locomotion.</summary>
        public abstract void PlayOneShot(RigPose pose, float duration);

        /// <summary>AI states call this every frame with explicit phase; wins over one-shots.</summary>
        public abstract void ForcePose(RigPose pose, float phase);

        /// <summary>Brief white hit-flash.</summary>
        public abstract void Flash();

        /// <summary>Turn the rig translucent (shades, Kagachi mirror clones).</summary>
        public virtual void MakeGhost(float alpha) { }

        public virtual void SetGhostAlpha(float a) { }

        public virtual void SetBaseColor(Color c) { }

        /// <summary>Flicker Step after-image snapshot.</summary>
        public virtual void SpawnAfterImage() { }

        /// <summary>Emotional state: calm idle → focused combat → enraged at low HP.</summary>
        public virtual void SetMood(RigMood mood) { }

        /// <summary>
        /// Restore the authored look, undoing runtime recolours (boss enrage) so a
        /// recycled enemy doesn't spawn wearing the last fight's state.
        /// </summary>
        public virtual void ResetVisuals() { }
    }
}
