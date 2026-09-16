using UnityEngine;

namespace Emberline.UI
{
    /// <summary>
    /// Keeps a full-stretch RectTransform inside <see cref="Screen.safeArea"/>, so
    /// nothing anchored to a corner sits under a punch-hole camera or the gesture
    /// bar. Re-applied when the screen or the safe area changes (rotation,
    /// split-screen). The project renders outside the safe area on purpose — the
    /// world fills the notch — so only the control layer is inset.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _applied;
        private Vector2Int _size;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _size.x || Screen.height != _size.y)
                Apply();
        }

        private void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;
            _size = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var min = area.position;
            var max = area.position + area.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            // A bogus or empty area (editor, previews) falls back to the full screen.
            if (min.x < 0f || min.y < 0f || max.x > 1f || max.y > 1f || max.x - min.x < 0.5f)
            {
                min = Vector2.zero;
                max = Vector2.one;
            }
            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
