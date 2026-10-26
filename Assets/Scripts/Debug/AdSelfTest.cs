#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System.Collections;
using Emberline.Core;
using UnityEngine;

namespace Emberline.DebugTools
{
    /// <summary>
    /// Development builds only. Exercises the rewarded-ad pipeline without playing
    /// to a defeat screen: launched with <c>am start … --es adtest 1</c>, it waits
    /// for consent and a loaded rewarded ad, shows it, and logs one
    /// <c>[ADTEST]</c> line per step (ready, reward granted / not granted,
    /// timescale and audio restored). Close the ad on the phone to finish.
    /// </summary>
    public class AdSelfTest : MonoBehaviour
    {
        private const string Tag = "[ADTEST]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string extra = null;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                extra = intent.Call<string>("getStringExtra", "adtest");
            }
            catch (System.Exception e) { Debug.LogWarning($"{Tag} intent unreadable: {e.Message}"); }
            if (string.IsNullOrEmpty(extra)) return;
            var go = new GameObject("AdSelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<AdSelfTest>();
        }

        private IEnumerator Start()
        {
            var m = MonetizationManager.Instance;
            Debug.Log($"{Tag} start removedAds={m.HasRemovedAds}");
            var t0 = Time.realtimeSinceStartup;
            while (!m.RewardedReady && Time.realtimeSinceStartup - t0 < 90f) yield return new WaitForSecondsRealtime(1f);
            Debug.Log($"{Tag} rewarded ready={m.RewardedReady} after {Time.realtimeSinceStartup - t0:F0}s");
            if (!m.RewardedReady) { Debug.Log($"{Tag} done result=never-loaded"); yield break; }

            var before = (Time.timeScale, AudioListener.pause);
            var finished = false;
            m.ShowRewardedAd(
                () => { Debug.Log($"{Tag} reward granted"); finished = true; },
                () => { Debug.Log($"{Tag} reward NOT granted (closed early or failed)"); finished = true; },
                "selftest");
            yield return null;
            Debug.Log($"{Tag} during ad timeScale={Time.timeScale} audioPaused={AudioListener.pause}");
            while (!finished) yield return null;
            yield return null;
            var restored = Time.timeScale == before.Item1 && AudioListener.pause == before.Item2;
            Debug.Log($"{Tag} after ad timeScale={Time.timeScale} audioPaused={AudioListener.pause} restored={restored}");
            Debug.Log($"{Tag} done result=ok");
        }
    }
}
#endif
