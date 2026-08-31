using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Input hub: EmberHud writes into it on device; in the editor/desktop it
    /// falls back to keyboard+mouse. Keeps gameplay scripts input-agnostic.
    /// </summary>
    public static class EmberInput
    {
        /// <summary>Set each frame by EmberHud when a stick drag is active.</summary>
        public static Vector2 TouchMove;
        public static bool TouchActive;

        private static bool _strike, _cleave, _flicker, _surge, _kunai;

        public static void PressStrike() => _strike = true;
        public static void PressCleave() => _cleave = true;
        public static void PressFlicker() => _flicker = true;
        public static void PressSurge() => _surge = true;
        public static void PressKunai() => _kunai = true;

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
        public static bool ConsumeKunai() =>
            Consume(ref _kunai) || Input.GetKeyDown(KeyCode.E);

        private static float _camYaw, _camPitch;

        /// <summary>Gyro camera: tilt the device to aim the view. Persisted, off by default.</summary>
        public static bool GyroOn
        {
            get => PlayerPrefs.GetInt("gyro_on", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("gyro_on", value ? 1 : 0);
                if (SystemInfo.supportsGyroscope) Input.gyro.enabled = value;
            }
        }

        /// <summary>Per-frame gyro (yaw, pitch) degrees. Landscape-left mapping.</summary>
        private static Vector2 GyroDelta()
        {
            if (!GyroOn || !SystemInfo.supportsGyroscope) return Vector2.zero;
            if (!Input.gyro.enabled) Input.gyro.enabled = true;
            var r = Input.gyro.rotationRateUnbiased; // rad/s in device axes
            // Landscape-left: device +X ≈ world up (turn = yaw), +Y spans the
            // screen width (tilt = pitch). Deadzone kills hand-tremor drift.
            var yaw = Mathf.Abs(r.x) > 0.03f ? -r.x * Mathf.Rad2Deg * Time.deltaTime : 0f;
            var pitch = Mathf.Abs(r.y) > 0.03f ? -r.y * Mathf.Rad2Deg * Time.deltaTime : 0f;
            return new Vector2(yaw, pitch) * 1.1f;
        }

        /// <summary>HUD camera-drag writes yaw degrees here each frame.</summary>
        public static void AddCamYaw(float degrees) => _camYaw += degrees;

        /// <summary>HUD camera-drag writes pitch degrees here (+ = tilt more top-down).</summary>
        public static void AddCamPitch(float degrees) => _camPitch += degrees;

        /// <summary>Pending orbit yaw (degrees). Editor: middle-mouse drag or , / . keys.</summary>
        public static float ConsumeCamYaw()
        {
            var v = _camYaw + GyroDelta().x;
            _camYaw = 0f;
            if (Input.GetMouseButton(2)) v += Input.GetAxisRaw("Mouse X") * 3f;
            if (Input.GetKey(KeyCode.Comma)) v -= 130f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Period)) v += 130f * Time.deltaTime;
            return v;
        }

        /// <summary>Pending orbit pitch (degrees). Editor: middle-mouse vertical drag.</summary>
        public static float ConsumeCamPitch()
        {
            var v = _camPitch + GyroDelta().y;
            _camPitch = 0f;
            if (Input.GetMouseButton(2)) v -= Input.GetAxisRaw("Mouse Y") * 2.5f;
            return v;
        }

        private static bool Consume(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }
    }
}
