using Emberline.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Emberline.UI
{
    /// <summary>
    /// Greys out an "(AD)" button while no rewarded ad is loaded, so the player
    /// never taps an offer that answers "not ready". Polls on unscaled time: the
    /// defeat card stops the clock.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AdReadyGate : MonoBehaviour
    {
        private Button _button;
        private CanvasGroup _group;
        private float _poll;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Refresh();
        }

        private void Update()
        {
            if ((_poll -= Time.unscaledDeltaTime) > 0f) return;
            _poll = 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            var ready = MonetizationManager.Instance.RewardedReady;
            _button.interactable = ready;
            _group.alpha = ready ? 1f : 0.4f;
        }
    }
}
