using System;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Zero-asset audio: all clips are synthesized into PCM buffers at startup.
    /// Call Init once (GameManager does it), then fire the static one-shots.
    /// Generators receive (tNorm 0..1, timeSeconds).
    /// </summary>
    public static class Sfx3D
    {
        private const int Rate = 22050;
        private static AudioSource _src;
        private static AudioClip _slash, _hit, _hurt, _surge, _death, _ui, _win, _lose;
        private static readonly System.Random Rng = new(7);

        public static void Init(GameObject host)
        {
            if (_src != null) return;
            _src = host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;

            _slash = Make("slash", 0.09f, (tn, ts) =>
                Noise() * 0.8f * Env(tn, 2f)
                + Mathf.Sin(SweepPhase(ts, 350, 900, 0.09f)) * 0.25f * Env(tn, 2.5f));

            _hit = Make("hit", 0.08f, (tn, ts) =>
                Mathf.Sin(SweepPhase(ts, 160, 70, 0.08f)) * Env(tn, 5f)
                + Noise() * 0.4f * Env(tn, 9f));

            _hurt = Make("hurt", 0.12f, (tn, ts) =>
                Mathf.Sin(SweepPhase(ts, 220, 90, 0.12f)) * 0.9f * Env(tn, 4f)
                + Noise() * 0.3f * Env(tn, 6f));

            _surge = Make("surge", 0.45f, (tn, ts) =>
                Mathf.Sin(SweepPhase(ts, 90, 34, 0.45f)) * Env(tn, 2.5f)
                + Noise() * 0.5f * Env(tn, 3f));

            _death = Make("death", 0.22f, (tn, ts) =>
                Noise() * 0.9f * Env(tn, 2.5f)
                + Mathf.Sin(SweepPhase(ts, 220, 60, 0.22f)) * 0.35f * Env(tn, 3.5f));

            _ui = Make("ui", 0.04f, (tn, ts) =>
                Mathf.Sin(SweepPhase(ts, 700, 500, 0.04f)) * 0.7f * Env(tn, 2f));

            // Rising four-note stinger.
            _win = Make("win", 0.9f, (tn, ts) =>
            {
                float[] freqs = { 440f, 554.37f, 659.25f, 880f };
                var step = Mathf.Min(3, (int)(tn * 4f));
                var lt = tn * 4f - step;
                return (Mathf.Sin(2f * Mathf.PI * freqs[step] * ts) * 0.6f
                        + Mathf.Sin(2f * Mathf.PI * freqs[step] * 2f * ts) * 0.12f) * Env(lt, 2f);
            });

            // Two falling tones.
            _lose = Make("lose", 0.8f, (tn, ts) =>
            {
                var f = tn < 0.5f ? 392f : 261.63f;
                var lt = tn < 0.5f ? tn * 2f : (tn - 0.5f) * 2f;
                return Mathf.Sin(2f * Mathf.PI * f * ts) * 0.6f * Env(lt, 2f);
            });
        }

        public static void Slash() => Play(_slash, 0.4f, RandomPitch());
        public static void Hit() => Play(_hit, 0.75f, RandomPitch());
        public static void Hurt() => Play(_hurt, 0.85f);
        public static void Surge() => Play(_surge, 0.95f);
        public static void Death() => Play(_death, 0.6f);
        public static void Ui() => Play(_ui, 0.6f);
        public static void Win() => Play(_win, 0.85f);
        public static void Lose() => Play(_lose, 0.85f);

        private static void Play(AudioClip clip, float vol, float pitch = 1f)
        {
            if (_src == null || clip == null) return;
            _src.pitch = pitch;
            _src.PlayOneShot(clip, vol);
        }

        private static float RandomPitch() => 0.92f + (float)Rng.NextDouble() * 0.16f;

        private static AudioClip Make(string name, float dur, Func<float, float, float> gen)
        {
            var n = (int)(Rate * dur);
            var data = new float[n];
            var peak = 1e-5f;
            for (var i = 0; i < n; i++)
            {
                data[i] = gen(i / (float)n, i / (float)Rate);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            for (var i = 0; i < n; i++) data[i] = data[i] / peak * 0.7f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Phase for a linear frequency sweep f0→f1 over dur seconds.</summary>
        private static float SweepPhase(float time, float f0, float f1, float dur) =>
            2f * Mathf.PI * (f0 * time + (f1 - f0) * time * time / (2f * dur));

        private static float Env(float t, float curve) => Mathf.Pow(Mathf.Clamp01(1f - t), curve);

        private static float Noise() => (float)(Rng.NextDouble() * 2 - 1);
    }
}
