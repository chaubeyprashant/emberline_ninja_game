using UnityEngine;

namespace Emberline.Player
{
    /// <summary>
    /// Plants the feet on the ground under them. Two short rays a frame, weights
    /// eased so a foot mid-stride is never dragged, and off entirely while the
    /// motor is airborne, dashing or inside a traversal move. Lives on the
    /// Animator's own object because OnAnimatorIK only fires there; the factory's
    /// gait controllers switch the IK pass on for it. Skipped below graphics tier 1.
    /// </summary>
    public class PlayerFootIk : MonoBehaviour
    {
        [SerializeField] private float rayUp = 0.45f;
        [SerializeField] private float rayDown = 0.6f;
        [SerializeField] private float footHeight = 0.08f;
        [SerializeField] private float weightLerp = 10f;

        private Animator _anim;
        private PlayerLocomotion _motor;
        private float _weight;
        private bool _enabledByTier;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _motor = GetComponentInParent<PlayerLocomotion>();
            _enabledByTier = PlayerPrefs.GetInt("gfx_tier", 1) >= 1;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_anim == null || !_enabledByTier) return;
            var want = _motor != null && _motor.Grounded && !_motor.Traversing
                       && !_motor.Invulnerable && !_motor.WallRunning && !_motor.Swimming ? 1f : 0f;
            _weight = Mathf.Lerp(_weight, want, 1f - Mathf.Exp(-weightLerp * Time.deltaTime));
            if (_weight < 0.01f)
            {
                _anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                _anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                return;
            }
            Plant(AvatarIKGoal.LeftFoot);
            Plant(AvatarIKGoal.RightFoot);
        }

        private void Plant(AvatarIKGoal goal)
        {
            var pos = _anim.GetIKPosition(goal);
            var rot = _anim.GetIKRotation(goal);
            // A foot lifted well above the body's floor is mid-stride: leave it.
            var lift = pos.y - transform.position.y;
            var w = _weight * Mathf.Clamp01(1f - (lift - footHeight) / 0.25f);
            if (Physics.Raycast(pos + Vector3.up * rayUp, Vector3.down, out var hit, rayUp + rayDown,
                    TraversalProbe.Mask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.point + Vector3.up * footHeight;
                // Only ever lower or lift a little: never plant a foot on a wall.
                if (Mathf.Abs(target.y - pos.y) < 0.35f)
                {
                    _anim.SetIKPositionWeight(goal, w);
                    _anim.SetIKRotationWeight(goal, w * 0.6f);
                    _anim.SetIKPosition(goal, target);
                    _anim.SetIKRotation(goal, Quaternion.FromToRotation(Vector3.up, hit.normal) * rot);
                    return;
                }
            }
            _anim.SetIKPositionWeight(goal, 0f);
            _anim.SetIKRotationWeight(goal, 0f);
        }
    }
}
