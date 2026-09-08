using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// A minimal touch stick and a way out, for the open-zone test area.
    ///
    /// <para>
    /// The zone is not a mission: it has no GameManager, no objectives and no
    /// enemies, so pulling in the full <c>EmberHud</c> would drag the whole menu
    /// and mission-flow stack into a scene that exists to test terrain, movement
    /// and framerate. This writes the same two fields the HUD's stick writes —
    /// <see cref="EmberInput.TouchMove"/> and <see cref="EmberInput.TouchActive"/>
    /// — and nothing else.
    /// </para>
    ///
    /// <para>
    /// Left half of the screen drags to walk; a tap on the top-left corner
    /// returns to the menu. Drawn with IMGUI because it is a debug affordance and
    /// should not cost a canvas.
    /// </para>
    /// </summary>
    public class ZoneControls : MonoBehaviour
    {
        [Tooltip("Drag distance, in fractions of screen height, for full speed.")]
        public float travel = 0.09f;

        private int _finger = -1;
        private Vector2 _origin;

        private void Update()
        {
            var active = false;

            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);

                if (t.phase == TouchPhase.Began)
                {
                    // Left half only, so the right half stays free for a look
                    // control later without reworking this.
                    if (_finger < 0 && t.position.x < Screen.width * 0.5f)
                    {
                        _finger = t.fingerId;
                        _origin = t.position;
                    }
                    continue;
                }

                if (t.fingerId != _finger) continue;

                if (t.phase is TouchPhase.Ended or TouchPhase.Canceled)
                {
                    _finger = -1;
                    continue;
                }

                var delta = (t.position - _origin) / (Screen.height * travel);
                EmberInput.TouchMove = Vector2.ClampMagnitude(delta, 1f);
                active = true;
            }

            EmberInput.TouchActive = active;
            if (!active) EmberInput.TouchMove = Vector2.zero;

            // Desktop fallback so the zone can be walked in the editor too.
            if (Input.touchCount == 0) _finger = -1;
        }

        private void OnGUI()
        {
            var w = Mathf.Max(120f, Screen.width * 0.08f);
            var h = Mathf.Max(48f, Screen.height * 0.07f);
            if (GUI.Button(new Rect(16f, 16f, w, h), "MENU"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("Rooftop");

            // Framerate, because "does it hold up on an A33" is the whole reason
            // this scene exists.
            var fps = 1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f);
            GUI.Label(new Rect(16f + w + 12f, 16f, 220f, h), $"{fps:F0} fps");
        }
    }
}
