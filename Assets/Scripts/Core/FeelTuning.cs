using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// The dials of the feel pass: multipliers (and one offset) on the values
    /// that decide whether combat and the camera feel right on a phone. Each sits
    /// on top of the authored value at its single point of use, so 1 (or 0)
    /// changes nothing.
    ///
    /// Development builds read them from PlayerPrefs and <see cref="UI.FeelPanel"/>
    /// changes them live; release builds compile them to the defaults, so a
    /// tuning session can never leak into a store build. Once a session settles
    /// on values, bake them into the authored fields and leave these at 1.
    /// </summary>
    public static class FeelTuning
    {
        public struct Dial
        {
            public string key, label, unit;
            public float def, min, max, step;
        }

        public static readonly Dial[] Dials =
        {
            new() { key = "parry",    label = "Parry window",   unit = "x",   def = 1f, min = 0.5f, max = 2f,   step = 0.1f },
            new() { key = "iframes",  label = "Dodge i-frames", unit = "x",   def = 1f, min = 0.5f, max = 2f,   step = 0.1f },
            new() { key = "hitstop",  label = "Hit-stop",       unit = "x",   def = 1f, min = 0f,   max = 3f,   step = 0.25f },
            new() { key = "camdist",  label = "Camera distance", unit = "x",  def = 1f, min = 0.7f, max = 1.5f, step = 0.05f },
            new() { key = "camfov",   label = "Camera FOV",     unit = "deg", def = 0f, min = -8f,  max = 10f,  step = 1f },
            new() { key = "camfollow", label = "Camera follow", unit = "x",   def = 1f, min = 0.4f, max = 2f,   step = 0.1f },
        };

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly float[] Values = new float[Dials.Length];
        private static bool _loaded;

        public static float Get(int i)
        {
            if (!_loaded)
            {
                _loaded = true;
                for (var d = 0; d < Dials.Length; d++)
                    Values[d] = PlayerPrefs.GetFloat("feel_" + Dials[d].key, Dials[d].def);
            }
            return Values[i];
        }

        public static void Set(int i, float v)
        {
            Get(i);
            var d = Dials[i];
            Values[i] = Mathf.Clamp(Mathf.Round(v / d.step) * d.step, d.min, d.max);
            PlayerPrefs.SetFloat("feel_" + d.key, Values[i]);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            for (var i = 0; i < Dials.Length; i++) Set(i, Dials[i].def);
        }
#else
        public static float Get(int i) => Dials[i].def;
#endif

        public static float ParryWindow => Get(0);
        public static float DodgeIFrames => Get(1);
        public static float HitStop => Get(2);
        public static float CamDistance => Get(3);
        public static float CamFov => Get(4);
        public static float CamFollow => Get(5);

        /// <summary>One line to paste back after a session: <c>[FEEL] parry=1.2 iframes=1 …</c>.</summary>
        public static string Summary()
        {
            var sb = new System.Text.StringBuilder("[FEEL]");
            for (var i = 0; i < Dials.Length; i++)
                sb.Append(' ').Append(Dials[i].key).Append('=')
                  .Append(Get(i).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
