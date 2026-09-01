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

        // Positional one-shot pool. The 2D _src stays for UI and player-centric
        // cues; anything that happens in the world goes through this instead so
        // distance and direction carry information during a fight.
        private const int PoolSize = 12;
        private static readonly List<AudioSource> Pool = new(PoolSize);
        private static int _poolNext;
        private static AudioSource _ambience, _musicB;
        private static float _fade, _fadeDur;
        private static bool _fadeToB;
        private static MusicState _musicState = MusicState.None;

        /// <summary>What the music layer should be playing. Drives crossfades.</summary>
        public enum MusicState { None, Exploration, Combat, Boss }

        /// <summary>Impact character — picks the bank, not the volume.</summary>
        public enum ImpactKind { Flesh, Blade, Guard, Heavy }

        /// <summary>Enemy vocal cues. Synthesized; no VO assets required.</summary>
        public enum Voice { Alert, Attack, Hurt, Death, Whisper, Roar }

        // Asset-backed variant banks (empty when Resources are missing).
        private static readonly List<AudioClip> SlashBank = new();
        private static readonly List<AudioClip> HitBank = new();
        private static readonly List<AudioClip> CrushBank = new();
        private static readonly List<AudioClip> UiBank = new();
        private static readonly List<AudioClip> FootGrassBank = new();
        private static readonly List<AudioClip> FootWoodBank = new();
        private static readonly List<AudioClip> ClothBank = new();
        private static readonly List<AudioClip> WhooshBank = new();
        private static readonly List<AudioClip> BladeBank = new();
        private static readonly List<AudioClip> GuardBank = new();
        private static readonly List<AudioClip> HeavyBank = new();
        private static readonly List<AudioClip> CreakBank = new();
        private static AudioClip _confirm, _back, _error, _stingBell, _stingDraw;
        private static AudioClip _breathIn, _breathHard, _alertBark, _attackBark;

        // Last index played per bank, so the same variant never fires twice in a
        // row. Repetition is what makes a small SFX library sound small.
        private static readonly Dictionary<List<AudioClip>, int> LastPick = new();

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

            // Init runs again after every scene load: the previous _src is a
            // destroyed object, which compares equal to null. Everything static
            // that Init fills must therefore be reset first, or the pool keeps
            // twelve destroyed sources per load and the banks double in size.
            ResetStatics();
            _src = host.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;

            _musicB = host.AddComponent<AudioSource>();
            _musicB.playOnAwake = false;
            _musicB.loop = true;
            _musicB.spatialBlend = 0f;
            _musicB.volume = 0f;

            _ambience = host.AddComponent<AudioSource>();
            _ambience.playOnAwake = false;
            _ambience.loop = true;
            _ambience.spatialBlend = 0f;

            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Sfx3D_" + i);
                go.transform.SetParent(host.transform, false);
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 1f;
                a.rolloffMode = AudioRolloffMode.Linear;
                a.minDistance = 3f;
                a.maxDistance = 34f;
                a.dopplerLevel = 0f;
                Pool.Add(a);
            }

            if (host.GetComponent<SfxDriver>() == null) host.AddComponent<SfxDriver>();

            LoadBanks();
            SynthesizeFallbacks();
            SynthesizeBarks();
            SynthesizeBreath();
        }

        /// <summary>Drop everything Init rebuilds, so a reload starts clean.</summary>
        private static void ResetStatics()
        {
            Pool.Clear();
            _poolNext = 0;
            LastPick.Clear();
            foreach (var bank in new[]
                     {
                         SlashBank, HitBank, CrushBank, UiBank, FootGrassBank, FootWoodBank,
                         ClothBank, WhooshBank, BladeBank, GuardBank, HeavyBank, CreakBank,
                     })
                bank.Clear();
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

            Fill(FootGrassBank, "footstep_grass_000", "footstep_grass_001",
                "footstep_grass_002", "footstep_grass_003", "footstep_grass_004");
            Fill(FootWoodBank, "footstep_wood_000", "footstep_wood_001",
                "footstep_wood_002", "footstep_wood_003", "footstep_wood_004");
            Fill(ClothBank, "cloth1", "cloth2", "cloth3", "cloth4");
            // Blade draws double as whooshes when pitched up and played quiet.
            Fill(WhooshBank, "drawKnife1", "drawKnife2", "drawKnife3");
            Fill(BladeBank, "impactMetal_medium_000", "impactMetal_medium_001",
                "impactMetal_medium_002", "impactMetal_medium_003", "impactMetal_medium_004");
            Fill(GuardBank, "impactMetal_heavy_000", "impactMetal_heavy_001",
                "impactMetal_heavy_002", "impactMetal_heavy_003", "impactMetal_heavy_004");
            Fill(HeavyBank, "impactSoft_heavy_000", "impactSoft_heavy_001",
                "impactSoft_heavy_002", "impactSoft_heavy_003", "impactSoft_heavy_004");
            Fill(CreakBank, "creak1", "creak2", "creak3");
        }

        // ------------------------------------------------------- world one-shots

        /// <summary>Footstep at a world position. Wood decks vs. grass/earth.</summary>
        public static void Footstep(Vector3 pos, bool wood, float vol = 0.45f)
        {
            var bank = wood ? FootWoodBank : FootGrassBank;
            if (bank.Count == 0) bank = FootGrassBank;
            PlayAtFromBank(bank, pos, vol, 0.88f + (float)Rng.NextDouble() * 0.24f);
        }

        /// <summary>Cloth rustle — dodges, landings, and the start of a heavy swing.</summary>
        public static void Cloth(Vector3 pos, float vol = 0.35f) =>
            PlayAtFromBank(ClothBank, pos, vol, 0.9f + (float)Rng.NextDouble() * 0.3f);

        /// <summary>Blade whoosh. Heavy swings are slower and lower.</summary>
        public static void Whoosh(Vector3 pos, bool heavy = false) =>
            PlayAtFromBank(WhooshBank, pos, heavy ? 0.5f : 0.35f,
                (heavy ? 0.72f : 1.15f) + (float)Rng.NextDouble() * 0.12f);

        /// <summary>Positional impact. Layered: every hit is body plus material.</summary>
        public static void ImpactAt(Vector3 pos, ImpactKind kind, float strength = 1f)
        {
            var v = Mathf.Clamp(strength, 0.4f, 1.6f);
            switch (kind)
            {
                case ImpactKind.Blade:
                    PlayAtFromBank(BladeBank, pos, 0.65f * v, RandomPitch());
                    PlayAtFromBank(HitBank, pos, 0.4f * v, RandomPitch());
                    break;
                case ImpactKind.Guard:
                    PlayAtFromBank(GuardBank, pos, 0.8f * v, RandomPitch());
                    break;
                case ImpactKind.Heavy:
                    PlayAtFromBank(HeavyBank, pos, 0.9f * v, 0.85f + (float)Rng.NextDouble() * 0.1f);
                    PlayAtFromBank(CrushBank, pos, 0.5f * v, RandomPitch());
                    break;
                default:
                    PlayAtFromBank(HitBank, pos, 0.75f * v, RandomPitch());
                    break;
            }
        }

        /// <summary>Wood/rope stress — vaults, ropes, breaking cover.</summary>
        public static void Creak(Vector3 pos, float vol = 0.5f) =>
            PlayAtFromBank(CreakBank, pos, vol, 0.9f + (float)Rng.NextDouble() * 0.2f);

        /// <summary>Player breathing. Hard breathing kicks in at low health.</summary>
        public static void Breath(bool hard = false) =>
            Play(hard ? _breathHard : _breathIn, hard ? 0.5f : 0.3f, RandomPitch());

        /// <summary>Positional enemy vocal. Kept synthesized so it costs no assets.</summary>
        public static void EnemyVoice(Vector3 pos, Voice voice)
        {
            var clip = voice switch
            {
                Voice.Alert => _alertBark,
                Voice.Attack => _attackBark,
                Voice.Hurt => Rng.NextDouble() < 0.5 ? _hurtBarkA : _hurtBarkB,
                Voice.Death => _deathCry,
                Voice.Whisper => _whisper,
                _ => _roar,
            };
            var vol = voice switch { Voice.Roar => 1f, Voice.Whisper => 0.55f, _ => 0.7f };
            PlayAt(clip, pos, vol, RandomPitch());
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

        /// <summary>
        /// Loop an environmental ambience bed (rain, wind, fire) independently of
        /// music, so a theme change does not interrupt the score.
        /// </summary>
        public static void PlayAmbience(string resourceName, float volume = 0.4f)
        {
            if (_ambience == null) return;
            if (string.IsNullOrEmpty(resourceName)) { _ambience.Stop(); return; }
            var clip = Resources.Load<AudioClip>("Art/Audio/Music/" + resourceName);
            if (clip == null) clip = Resources.Load<AudioClip>("Art/Audio/SFX/" + resourceName);
            if (clip == null || _ambience.clip == clip) return;
            _ambience.clip = clip;
            _ambience.volume = volume * MusicVolume;
            _ambience.Play();
        }

        /// <summary>
        /// Request a music state. Crossfades between the two music sources rather
        /// than cutting. A state whose track is missing from Resources simply
        /// leaves the current bed playing — see docs/CHANGELOG.md for the gap.
        /// </summary>
        public static void SetMusicState(MusicState state, float fade = 1.5f)
        {
            if (state == _musicState) return;
            var name = state switch
            {
                MusicState.Exploration => "explore_theme",
                MusicState.Combat => "combat_theme",
                MusicState.Boss => "boss_theme",
                _ => null,
            };
            _musicState = state;
            var clip = string.IsNullOrEmpty(name)
                ? null : Resources.Load<AudioClip>("Art/Audio/Music/" + name);
            if (clip == null) return; // track not authored yet — keep the current bed

            var to = _fadeToB ? _music : _musicB;
            if (to.clip == clip) return;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            _fadeToB = !_fadeToB;
            _fade = 0f;
            _fadeDur = Mathf.Max(0.05f, fade);
        }

        /// <summary>Advance music crossfades. Driven by <see cref="SfxDriver"/>.</summary>
        internal static void Tick(float dt)
        {
            if (_fadeDur <= 0f || _music == null) return;
            _fade += dt;
            var k = Mathf.Clamp01(_fade / _fadeDur);
            var target = MusicVolume * 0.5f;
            var into = _fadeToB ? _musicB : _music;
            var outOf = _fadeToB ? _music : _musicB;
            into.volume = target * k;
            outOf.volume = target * (1f - k);
            if (k < 1f) return;
            outOf.Stop();
            _fadeDur = 0f;
        }

        // ------------------------------------------------------------- internals

        private static bool PlayFromBank(List<AudioClip> bank, float vol, float pitch = 1f)
        {
            var clip = Pick(bank);
            if (clip == null) return false;
            Play(clip, vol, pitch);
            return true;
        }

        private static void PlayAtFromBank(List<AudioClip> bank, Vector3 pos, float vol, float pitch)
        {
            var clip = Pick(bank);
            if (clip != null) PlayAt(clip, pos, vol, pitch);
        }

        /// <summary>Random bank pick that never repeats the previous variant.</summary>
        private static AudioClip Pick(List<AudioClip> bank)
        {
            if (bank.Count == 0) return null;
            if (bank.Count == 1) return bank[0];
            LastPick.TryGetValue(bank, out var last);
            var i = Rng.Next(bank.Count - 1);
            if (i >= last) i++;
            LastPick[bank] = i;
            return bank[i];
        }

        private static void PlayAt(AudioClip clip, Vector3 pos, float vol, float pitch)
        {
            if (clip == null || Pool.Count == 0) { Play(clip, vol, pitch); return; }
            var a = Pool[_poolNext];
            _poolNext = (_poolNext + 1) % Pool.Count;
            // A destroyed source here used to throw out of PlayerLocomotion.Update,
            // which skipped jump, wall-run and velocity for that frame — a footstep
            // could stop the player moving. Fall back to the 2D source instead.
            if (a == null) { Play(clip, vol, pitch); return; }
            a.transform.position = pos;
            a.pitch = pitch;
            a.PlayOneShot(clip, vol * SfxVolume);
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

        /// <summary>Breath and alert barks: filtered noise with a vocal formant.</summary>
        private static void SynthesizeBreath()
        {
            _breathIn = Make("breath_in", 0.42f, (tn, ts) =>
            {
                var n = Noise() * 0.5f;
                var mod = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2.4f * ts);
                return n * mod * Mathf.Sin(Mathf.PI * tn) * 0.7f;
            });

            _breathHard = Make("breath_hard", 0.30f, (tn, ts) =>
            {
                var n = Noise() * 0.7f;
                var tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(150f, 110f, tn) * ts) * 0.25f;
                return (n + tone) * Mathf.Sin(Mathf.PI * tn);
            });

            // Sharp rising grunt — reads as "there!" without words.
            _alertBark = Make("bark_alert", 0.20f, (tn, ts) =>
                Grunt(ts, tn, 160, 300) * 0.9f + Noise() * 0.25f * Env(tn, 5f));

            // Short downward shout on commit.
            _attackBark = Make("bark_attack", 0.24f, (tn, ts) =>
                Grunt(ts, tn, 300, 140) + Noise() * 0.3f * Env(tn, 3.5f));
        }

        private static float Noise() => (float)(Rng.NextDouble() * 2 - 1);
    }

    /// <summary>Pumps <see cref="Sfx3D.Tick"/>; a static class cannot get Update.</summary>
    public sealed class SfxDriver : MonoBehaviour
    {
        private void Update() => Sfx3D.Tick(Time.unscaledDeltaTime);
    }
}
