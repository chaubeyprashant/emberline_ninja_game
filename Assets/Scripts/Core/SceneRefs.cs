using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Cached lookups for the handful of objects that exist exactly once per
    /// scene. FindFirstObjectByType walks every loaded object, and the enemy
    /// spawn path, the chief's slam and the floating damage numbers were each
    /// paying for it repeatedly — per enemy, per attack, per frame.
    ///
    /// Destroyed references compare equal to null (Unity's fake-null), so a
    /// scene load silently invalidates every field and the next access re-finds
    /// it. That keeps the cache correct without a scene-load hook.
    /// </summary>
    public static class SceneRefs
    {
        private static Player.PlayerLocomotion _motor;
        private static GameManager _gm;
        private static CameraRig _cameraRig;
        private static Enemies.AttackTokenPool _tokens;
        private static Camera _cam;

        public static Player.PlayerLocomotion Motor =>
            _motor != null ? _motor : _motor = Object.FindAnyObjectByType<Player.PlayerLocomotion>();

        public static GameManager Game =>
            _gm != null ? _gm : _gm = Object.FindAnyObjectByType<GameManager>();

        public static CameraRig Rig =>
            _cameraRig != null ? _cameraRig : _cameraRig = Object.FindAnyObjectByType<CameraRig>();

        public static Enemies.AttackTokenPool Tokens =>
            _tokens != null ? _tokens : _tokens = Object.FindAnyObjectByType<Enemies.AttackTokenPool>();

        /// <summary>Camera.main is a tagged search; the damage numbers hit it every frame.</summary>
        public static Camera Cam => _cam != null ? _cam : _cam = Camera.main;
    }
}
