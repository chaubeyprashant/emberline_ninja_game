using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// Audio hub. v3: real clips (Kenney CC0 packs under Resources/Art/Audio)
    /// with the original synthesized PCM clips as fallback, plus synthesized
    /// voice barks (grunts/whispers/roars — no VO assets needed), an ambient
    /// music channel, and persisted SFX/music volume settings.
    /// Call Init once (GameManager does it), then fire the static one-shots.
    /// </summary>
    public static class Sfx3D
    {
        private const int Rate = 22050;
        private static AudioSource _src, _music;
        private static AudioClip _slash, _hit, _hurt, _surge, _death, _ui, _win, _lose;
        private static AudioClip _dodgeBark, _hurtBarkA, _hurtBarkB, _deathCry, _whisper, _roar;
        private static readonly System.Random Rng = new(7);

        // Asset-backed variant banks (empty when Resources are missing).
        private static readonly List<AudioClip> SlashBank = new();
        private static readonly List<AudioClip> HitBank = new();
        private static readonly List<AudioClip> CrushBank = new();
        private static readonly List<AudioClip> UiBank = new();
        private static AudioClip _confirm, _back, _error, _stingBell, _stingDraw;

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("vol_sfx", 1f);
            set { PlayerPrefs.SetFloat("vol_sfx", Mathf.Clamp01(value)); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("vol_music", 0.8f);
            set
            {
                PlayerPrefs.SetFloat("vol_music", Mathf.Clamp01(value));
                if (_music != null) _music.volume = value * 0.5f;
            }
        }

        public static void Init(GameObject host)
        {
            if (_src != null) return;
            _src = host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;

            LoadBanks();
            SynthesizeFallbacks();
            SynthesizeBarks();
        }

        private static void LoadBanks()
        {
            void Fill(List<AudioClip> bank, params string[] names)
            {
                foreach (var n in names)
                {
                    var clip = Resources.Load<AudioClip>("Art/Audio/SFX/" + n);
                    if (clip != null) bank.Add(clip);
                }
            }

            Fill(SlashBank, "knifeSlice", "knifeSlice2", "chop");
            Fill(HitBank, "impactPunch_medium_000", "impactPunch_medium_001",
                "impactPunch_medium_002", "impactPunch_medium_003", "impactPunch_medium_004");
            Fill(CrushBank, "impactPunch_heavy_000", "impactPunch_heavy_001",
                "impactPunch_heavy_002", "impactPunch_heavy_003", "impactPunch_heavy_004");
            Fill(UiBank, "click_001", "click_002", "click_003");
            _confirm = Resources.Load<AudioClip>("Art/Audio/SFX/confirmation_001");
            _back = Resources.Load<AudioClip>("Art/Audio/SFX/back_001");
            _error = Resources.Load<AudioClip>("Art/Audio/SFX/error_001");
            _stingBell = Resources.Load<AudioClip>("Art/Audio/SFX/bong_001");
            _stingDraw = Resources.Load<AudioClip>("Art/Audio/SFX/drawKnife2");
        }

        // ------------------------------------------------------------- one-shots

        public static void Slash()
        {
            if (!PlayFromBank(SlashBank, 0.5f, RandomPitch())) Play(_slash, 0.4f, RandomPitch());
        }

        public static void Hit()
        {
            if (!PlayFromBank(HitBank, 0.8f, RandomPitch())) Play(_hit, 0.75f, RandomPitch());
        }

        public static void HitCrush()
        {
            if (!PlayFromBank(CrushBank, 0.9f, RandomPitch())) Play(_hit, 0.9f, 0.85f);
        }

        public static void Hurt()
        {
            Play(_hurt, 0.7f);
            Play(Rng.NextDouble() < 0.5 ? _hurtBarkA : _hurtBarkB, 0.75f, RandomPitch());
        }

        public static void Surge() => Play(_surge, 0.95f);

        public static void Death()
        {
            Play(_death, 0.6f);
        }

        public static void PlayerDeath() => Play(_deathCry, 0.9f);

        public static void Ui()
        {
            if (!PlayFromBank(UiBank, 0.6f)) Play(_ui, 0.6f);
        }

        public static void Confirm() => PlayOrFallback(_confirm, _ui, 0.7f);
        public static void Back() => PlayOrFallback(_back, _ui, 0.6f);
        public static void Error() => PlayOrFallback(_error, _ui, 0.6f);
        public static void Win() => Play(_win, 0.85f);
        public static void Lose() => Play(_lose, 0.85f);

        /// <summary>Boss intro sting: temple bell + blade draw.</summary>
        public static void Sting()
        {
            PlayOrFallback(_stingBell, _lose, 0.95f, 0.7f);
            PlayOrFallback(_stingDraw, _slash, 0.8f, 0.9f);
        }

        // ---------------------------------------------------------------- barks

        public static void DodgeBark() => Play(_dodgeBark, 0.45f, RandomPitch());
        public static void ShadeWhisper() => Play(_whisper, 0.55f, 0.9f + (float)Rng.NextDouble() * 0.2f);
        public static void BossRoar() => Play(_roar, 0.95f);

        // -------------------------------------------------------------- ambient

        /// <summary>Loop a music/ambience clip from Resources/Art/Audio/Music (null stops).</summary>
        public static void PlayAmbient(string resourceName)
        {
            if (_music == null) return;
            if (string.IsNullOrEmpty(resourceName)) { _music.Stop(); return; }
            var clip = Resources.Load<AudioClip>("Art/Audio/Music/" + resourceName);
            if (clip == null || _music.clip == clip) return;
            _music.clip = clip;
            _music.volume = MusicVolume * 0.5f;
            _music.Play();
        }

        // ------------------------------------------------------------- internals

        private static bool PlayFromBank(List<AudioClip> bank, float vol, float pitch = 1f)
        {
            if (bank.Count == 0) return false;
            Play(bank[Rng.Next(bank.Count)], vol, pitch);
            return true;
        }

        private static void PlayOrFallback(AudioClip clip, AudioClip fallback, float vol, float pitch = 1f) =>
            Play(clip != null ? clip : fallback, vol, pitch);

        private static void Play(AudioClip clip, float vol, float pitch = 1f)
        {
            if (_src == null || clip == null) return;
            _src.pitch = pitch;
            _src.PlayOneShot(clip, vol * SfxVolume);
        }

        private static float RandomPitch() => 0.92f + (float)Rng.NextDouble() * 0.16f;

        // ----------------------------------------------------------- synthesis

        private static void SynthesizeFallbacks()
        {
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

        /// <summary>Short vocal grunts: pitch-dropped tone + formant-ish noise burst.</summary>
        private static void SynthesizeBarks()
        {
            _dodgeBark = Make("bark_dodge", 0.10f, (tn, ts) =>
                Grunt(ts, tn, 190, 150) * 0.6f + Noise() * 0.35f * Env(tn, 6f));

            _hurtBarkA = Make("bark_hurtA", 0.16f, (tn, ts) =>
                Grunt(ts, tn, 220, 130) + Noise() * 0.25f * Env(tn, 4f));

            _hurtBarkB = Make("bark_hurtB", 0.13f, (tn, ts) =>
                Grunt(ts, tn, 250, 170) + Noise() * 0.3f * Env(tn, 5f));

            _deathCry = Make("bark_death", 0.55f, (tn, ts) =>
                Grunt(ts, tn, 260, 70) * (1f - tn * 0.4f) + Noise() * 0.2f * Env(tn, 2f));

            // Shade: breathy band-limited whisper, no tone.
            _whisper = Make("bark_whisper", 0.7f, (tn, ts) =>
            {
                var n = Noise() * 0.5f;
                var mod = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (3.2f + tn * 4f) * ts);
                return n * mod * Mathf.Sin(Mathf.PI * tn); // swell in and out
            });

            // Goro/Jin: layered low roar with vibrato.
            _roar = Make("bark_roar", 0.8f, (tn, ts) =>
            {
                var vib = 1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 11f * ts);
                var f = Mathf.Lerp(95f, 55f, tn) * vib;
                var body = Mathf.Sin(2f * Mathf.PI * f * ts) * 0.5f
                           + Mathf.Sin(2f * Mathf.PI * f * 2.02f * ts) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * f * 2.98f * ts) * 0.2f;
                return (body + Noise() * 0.3f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, tn * 1.3f));
            });
        }

        /// <summary>Vocal-ish tone: fundamental sweep + odd harmonics + tremolo.</summary>
        private static float Grunt(float ts, float tn, float f0, float f1)
        {
            var f = Mathf.Lerp(f0, f1, tn);
            var s = Mathf.Sin(2f * Mathf.PI * f * ts) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * f * 3.1f * ts) * 0.2f;
            return s * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 27f * ts)) * Env(tn, 2.2f);
        }

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
