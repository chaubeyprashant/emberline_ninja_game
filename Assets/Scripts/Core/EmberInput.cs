using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Input hub: TouchHud writes into it on device; in the editor/desktop it
    /// falls back to keyboard+mouse. Keeps gameplay scripts input-agnostic.
    /// </summary>
    public static class EmberInput
    {
        /// <summary>Set each frame by TouchHud when a stick drag is active.</summary>
        public static Vector2 TouchMove;
        public static bool TouchActive;

        private static bool _strike, _cleave, _flicker, _surge;

        public static void PressStrike() => _strike = true;
        public static void PressCleave() => _cleave = true;
        public static void PressFlicker() => _flicker = true;
        public static void PressSurge() => _surge = true;

        public static Vector2 Move
        {
            get
            {
                if (TouchActive) return TouchMove;
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
        }

        public static bool ConsumeStrike() =>
            Consume(ref _strike) || Input.GetMouseButtonDown(0);
        public static bool ConsumeCleave() =>
            Consume(ref _cleave) || Input.GetMouseButtonDown(1);
        public static bool ConsumeFlicker() =>
            Consume(ref _flicker) || Input.GetKeyDown(KeyCode.Space);
        public static bool ConsumeSurge() =>
            Consume(ref _surge) || Input.GetKeyDown(KeyCode.Q);

        private static bool Consume(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }
    }
}
