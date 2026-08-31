using UnityEngine;

namespace Emberline.Core
{
    /// <summary>Device vibration on big hits (player hurt/death). No-op in editor.</summary>
    public static class Haptics
    {
        public static void Buzz()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
