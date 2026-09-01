using System.Collections.Generic;
using Emberline.Core;
using Emberline.Enemies;
using Emberline.UI;
using UnityEngine;

namespace Emberline.Endless
{
    /// <summary>
    /// The Road North, rebuilt as a run of discrete encounters through changing
    /// country. The old march scaled two numbers and spawned more bodies; this
    /// picks a different *kind* of problem each time, hands it a roster composed
    /// for the current depth, and lets the region it happens in change how the
    /// ground behaves.
    ///
    /// Owned and pumped by <see cref="GameManager"/> in endless mode. It decides
    /// what happens; the GameManager still owns spawning and the road itself.
    /// </summary>
    public class EndlessDirector
    {
        /// <summary>The seven places a run can pass through.</summary>
        private static readonly EnvThemeId[] Regions =
        {
            EnvThemeId.Village, EnvThemeId.Forest, EnvThemeId.Temple, EnvThemeId.Castle,
            EnvThemeId.Mountain, EnvThemeId.RainyBattlefield, EnvThemeId.Graveyard,
        };

        /// <summary>Encounters in a region before the road moves on.</summary>
        private const int RegionLength = 3;

        private readonly GameManager _gm;
        private readonly List<EnemyKind> _roster = new(16);
        private readonly List<EnvThemeId> _bag = new(8);

        private EncounterDef _def;
        private EncounterKind? _last;
        private bool _active;
        private float _timer, _holdProgress, _sinceStart;
        private float _nextZ, _escapeGoalZ;
        private Transform _objectiveMarker;
        private Vector3 _defendPoint;
        private string _bossModifier = "";

        public EnvThemeId Region { get; private set; } = EnvThemeId.Village;
        public string RegionName { get; private set; } = "";
        public int Depth => RunStats.Depth;
        public bool EncounterActive => _active;
        public string BossModifier => _bossModifier;

        public EndlessDirector(GameManager gm)
        {
            _gm = gm;
            RunModifiers.BeginRun();
            RunStats.Begin();
            RefillBag();
            EnterRegion(NextRegion(), announce: false);
        }

        // ------------------------------------------------------------ objective

        /// <summary>HUD objective line for the current encounter.</summary>
        public string Objective
        {
            get
            {
                if (!_active) return "MARCH NORTH";
                var alive = AliveCount();
                var clock = _def.TimeLimit > 0f ? $"   {Mathf.CeilToInt(Mathf.Max(0, _timer))}s" : "";
                return _def.Kind switch
                {
                    EncounterKind.Rescue => $"{_def.Objective}{clock}",
                    EncounterKind.Defense =>
                        $"HOLD THE GROUND — {Mathf.RoundToInt(_holdProgress * 100f)}%{clock}",
                    EncounterKind.Escape => _gm.PlayerT != null
                        ? $"OUTRUN THEM — {Mathf.Max(0, Mathf.RoundToInt(_escapeGoalZ - _gm.PlayerT.position.z))}m{clock}"
                        : _def.Objective,
                    EncounterKind.Duel => _def.Objective,
                    _ => $"{_def.Objective} — {alive} LEFT",
                };
            }
        }

        // ----------------------------------------------------------------- tick

        public void Tick(float dt)
        {
            RunStats.Tick(dt, _gm.DistanceNorth);
            _sinceStart += dt;

            // Speed pressure comes from the modifier and the region, not from an
            // ever-climbing multiplier — an endlessly rising number stops being
            // a difficulty curve and becomes a wall.
            GameManager.SetEnemySpeedMul(
                1f + Mathf.Min(0.25f, Depth * 0.012f)
                   + (RunModifiers.On(RunMod.FasterEnemies) ? 0.25f : 0f));

            if (!_active)
            {
                var pt = _gm.PlayerT;
                if (pt != null && pt.position.z >= _nextZ) Begin();
                return;
            }

            if (_def.TimeLimit > 0f) _timer -= dt;
            UpdateEncounter(dt);
        }

        // ---------------------------------------------------------------- begin

        private void Begin()
        {
            _active = true;
            _sinceStart = 0f;
            _holdProgress = 0f;
            _bossModifier = "";
            _timer = 0f;

            if (Depth > 0 && Depth % RegionLength == 0) EnterRegion(NextRegion(), announce: true);

            var kind = Encounters.Pick(Depth, _last);
            _last = kind;
            _def = Encounters.Get(kind);
            _timer = _def.TimeLimit;

            var pt = _gm.PlayerT;
            var pz = pt != null ? pt.position.z : 0f;

            // Escape is the one encounter that must not seal the road — the whole
            // problem is getting up it.
            if (kind != EncounterKind.Escape) _gm.Road?.RaiseBarrier(pz + 24f);

            Encounters.Compose(kind, Depth, _roster);
            SpawnRoster(pz, kind);
            SetupSpecial(kind, pz);

            RunHazard.Populate(Region, pz + 6f, pz + 22f, RoadNorth.HalfWidth, Depth);

            _gm.Announce(_def.Banner + (_bossModifier.Length > 0 ? " — " + _bossModifier : ""));
            if (kind is EncounterKind.Boss or EncounterKind.MiniBoss)
                _gm.ShowBossIntro(_roster.ToArray());
        }

        private void SpawnRoster(float pz, EncounterKind kind)
        {
            // Stats rise, but gently and with a ceiling: past roughly depth 25 a
            // fight should be won by knowing the encounter, not by out-scaling it.
            var hpMul = 1f + Mathf.Min(1.6f, Depth * 0.07f);
            var dmgMul = (1f + Mathf.Min(0.9f, Depth * 0.04f))
                         * (RunModifiers.On(RunMod.DoubleDamage) ? 2f : 1f);

            for (var i = 0; i < _roster.Count; i++)
            {
                var kd = _roster[i];
                var side = Random.value < 0.5f ? -1f : 1f;
                Vector3 at;
                switch (kind)
                {
                    case EncounterKind.Assassins:
                        // Assassins open from the flanks and behind, not from up
                        // the road; being surrounded is the encounter.
                        at = new Vector3(side * (RoadNorth.HalfWidth - 1.2f), 0f,
                            pz + Random.Range(-6f, 10f));
                        break;
                    case EncounterKind.Escape:
                        at = new Vector3(Random.Range(-RoadNorth.HalfWidth + 1f, RoadNorth.HalfWidth - 1f),
                            0f, pz - Random.Range(2f, 9f)); // behind, chasing
                        break;
                    case EncounterKind.Duel:
                        at = new Vector3(0f, 0f, pz + 9f);
                        break;
                    default:
                        at = new Vector3(Random.Range(-RoadNorth.HalfWidth + 1f, RoadNorth.HalfWidth - 1f),
                            0f, pz + Random.Range(9f, 18f));
                        break;
                }

                var brain = _gm.SpawnRunEnemy(kd, at, hpMul, dmgMul);
                if (brain == null) continue;
                if (kind is EncounterKind.Boss or EncounterKind.MiniBoss && i == 0)
                    ApplyBossModifier(brain);
            }
        }

        /// <summary>
        /// A boss modifier changes how the fight is fought, not how long it takes.
        /// Rolled per boss so the same boss is a different problem on the next run.
        /// </summary>
        private void ApplyBossModifier(EnemyBrain boss)
        {
            switch (Random.Range(0, 4))
            {
                case 0:
                    _bossModifier = "ARMOURED";
                    boss.maxHp *= 1.35f;
                    boss.SyncHpToMax();
                    break;
                case 1:
                    _bossModifier = "SWIFT";
                    boss.speed *= 1.35f;
                    boss.windupTime *= 0.75f; // reads the same, lands sooner
                    break;
                case 2:
                    _bossModifier = "CRUEL";
                    boss.damage *= 1.5f;
                    break;
                default:
                    _bossModifier = "ESCORTED";
                    var pz = boss.transform.position.z;
                    for (var i = 0; i < 3; i++)
                        _gm.SpawnRunEnemy(EnemyKind.PikeGuard,
                            new Vector3(Random.Range(-4f, 4f), 0f, pz + Random.Range(-2f, 3f)),
                            1f, 1f);
                    break;
            }
        }

        private void SetupSpecial(EncounterKind kind, float pz)
        {
            switch (kind)
            {
                case EncounterKind.Rescue:
                    _objectiveMarker = Beacon(new Vector3(
                        Random.Range(-RoadNorth.HalfWidth + 2f, RoadNorth.HalfWidth - 2f),
                        0f, pz + 26f), new Color(0.55f, 0.9f, 0.6f));
                    break;
                case EncounterKind.Defense:
                    _defendPoint = new Vector3(0f, 0f, pz + 5f);
                    _objectiveMarker = Beacon(_defendPoint, new Color(0.95f, 0.75f, 0.35f));
                    break;
                case EncounterKind.Escape:
                    _escapeGoalZ = pz + 60f;
                    break;
            }
        }

        // --------------------------------------------------------------- update

        private void UpdateEncounter(float dt)
        {
            var pt = _gm.PlayerT;

            switch (_def.Kind)
            {
                case EncounterKind.Rescue:
                    if (pt != null && _objectiveMarker != null
                        && Vector3.Distance(pt.position, _objectiveMarker.position) < 3f)
                    {
                        Clear("THE PRISONER LIVES");
                        return;
                    }
                    if (_timer <= 0f) { Fail("TOO LATE"); return; }
                    break;

                case EncounterKind.Defense:
                    if (pt != null && Vector3.Distance(pt.position, _defendPoint) < 6f)
                        _holdProgress = Mathf.Clamp01(_holdProgress + dt / _def.TimeLimit);
                    else
                        // Stepping out bleeds progress rather than resetting it:
                        // a reset punishes one mistake with the whole encounter.
                        _holdProgress = Mathf.Max(0f, _holdProgress - dt * 0.5f / _def.TimeLimit);
                    if (_holdProgress >= 1f) { Clear("THE GROUND HELD"); return; }
                    if (_timer <= -_def.TimeLimit) { Fail("DRIVEN OFF"); return; }
                    break;

                case EncounterKind.Escape:
                    if (pt != null && pt.position.z >= _escapeGoalZ) { Clear("CLEAR"); return; }
                    if (_timer <= 0f) { Fail("RUN DOWN"); return; }
                    break;

                default:
                    if (AliveCount() == 0) { Clear(""); return; }
                    break;
            }

            // Every encounter still resolves if the field empties — a rescue whose
            // escort is dead should not hold the player to a timer for nothing.
            if (_def.Kind is EncounterKind.Rescue or EncounterKind.Defense
                && AliveCount() == 0 && _sinceStart > 3f)
                Clear("");
        }

        // ---------------------------------------------------------------- close

        private void Clear(string banner)
        {
            RunStats.OnEncounterCleared(_def);
            Reward();
            EndEncounter();
            _gm.Announce(banner.Length > 0 ? banner : "THE ROAD OPENS — NORTH");
        }

        /// <summary>
        /// A failed objective costs the reward and some health, but never the run.
        /// A timed side-objective that can end a march would make the good play
        /// "ignore it", which is the opposite of what it is there for.
        /// </summary>
        private void Fail(string banner)
        {
            RunStats.Depth.ToString(); // depth still advances; the road moves on
            RunStats.OnEncounterCleared(new EncounterDef(_def.Kind, _def.Banner,
                _def.Objective, _def.MinDepth, _def.Weight, 0f, _def.ScoreValue / 4));
            _gm.PlayerHealth?.Damage(18f, _gm.PlayerT != null ? _gm.PlayerT.position : Vector3.zero);
            EndEncounter();
            _gm.Announce(banner);
        }

        private void EndEncounter()
        {
            _active = false;
            _bossModifier = "";
            RunHazard.ClearAll();
            _gm.Road?.ClearBarrier();
            if (_objectiveMarker != null) Object.Destroy(_objectiveMarker.gameObject);
            _objectiveMarker = null;
            var pt = _gm.PlayerT;
            if (pt != null) _nextZ = pt.position.z + Random.Range(20f, 30f);
        }

        /// <summary>
        /// Healing is a resource, not a refill: it shrinks as the run deepens and
        /// disappears entirely under No Healing. This is what stops a long run
        /// becoming safer than a short one.
        /// </summary>
        private void Reward()
        {
            var hp = _gm.PlayerHealth;
            if (hp != null && !RunModifiers.On(RunMod.NoHealing))
            {
                var heal = Core.Difficulty.ScaleHeal(Mathf.Max(8f, 34f - Depth * 1.5f));
                hp.Heal(heal);
                if (_gm.PlayerT != null)
                    FloatingText.Spawn(_gm.PlayerT.position + Vector3.up * 2.3f,
                        $"+{Mathf.RoundToInt(heal)}", new Color(0.5f, 0.9f, 0.55f), 1.1f);
            }

            // Ryo scales with the encounter's difficulty and the wager taken on.
            RunStats.EarnRyo(Mathf.RoundToInt(_def.ScoreValue * 0.25f
                * (1f + Depth * 0.05f) * RunModifiers.ActiveScoreMultiplier));

            // Skill points stay rare: one every third encounter, plus one for
            // every boss, so the skill tree still fills at story pace.
            if (Depth % 3 == 0 || _def.Kind == EncounterKind.Boss)
            {
                RunStats.EarnShard();
                _gm.Announce("◆ EMBER SHARD EARNED");
            }
        }

        // --------------------------------------------------------------- region

        private void EnterRegion(EnvThemeId id, bool announce)
        {
            Region = id;
            var theme = EnvThemes.Get(id);
            RegionName = theme.displayName;

            // Modifiers override the region's own weather — the player asked for
            // rain or fog and should get it wherever the road goes.
            if (RunModifiers.On(RunMod.HeavyRain)) theme.weather = Weather.Rain;
            if (RunModifiers.On(RunMod.Fog))
            {
                theme.fogDensity = Mathf.Max(theme.fogDensity, 0.075f);
                theme.weather = theme.weather == Weather.Clear ? Weather.Mist : theme.weather;
            }

            ApplyRegionLighting(theme);
            // Follow the camera, not the player: the emitters cover a small volume
            // and it is the view that has to be filled.
            var cam = SceneRefs.Cam;
            Atmosphere.Apply(theme, cam != null ? cam.transform : _gm.PlayerT);
            if (announce) _gm.Announce($"THE ROAD ENTERS — {RegionName}");
        }

        /// <summary>
        /// Runtime equivalent of the bootstrap's BuildLighting: the region table
        /// drives fog, ambient and the three lights that already exist in the scene.
        /// </summary>
        private static void ApplyRegionLighting(EnvTheme theme)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.ambientSky;
            RenderSettings.ambientEquatorColor = theme.ambientEquator;
            RenderSettings.ambientGroundColor = theme.ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type != LightType.Directional) continue;
                switch (l.name)
                {
                    case "KeyLight": l.color = theme.keyLight; l.intensity = theme.keyIntensity; break;
                    case "FillLight": l.color = theme.fillLight; break;
                    case "RimLight": l.color = theme.rimLight; break;
                }
            }
        }

        /// <summary>
        /// Regions are drawn from a shuffled bag rather than rolled independently,
        /// so a run visits varied country instead of landing on the same place
        /// three times by chance.
        /// </summary>
        private EnvThemeId NextRegion()
        {
            if (_bag.Count == 0) RefillBag();
            var i = Random.Range(0, _bag.Count);
            var id = _bag[i];
            _bag.RemoveAt(i);
            return id;
        }

        private void RefillBag()
        {
            _bag.Clear();
            _bag.AddRange(Regions);
        }

        // --------------------------------------------------------------- helper

        private static int AliveCount()
        {
            var n = 0;
            for (var i = 0; i < EnemyBrain.Active.Count; i++)
            {
                var e = EnemyBrain.Active[i];
                if (e != null && !e.Dead) n++;
            }
            return n;
        }

        /// <summary>A pillar of light: the one thing on the road worth walking to.</summary>
        private static Transform Beacon(Vector3 at, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "RunBeacon";
            go.transform.position = new Vector3(at.x, 3f, at.z);
            go.transform.localScale = new Vector3(0.7f, 3f, 0.7f);
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Emberline/Glow"))
            {
                color = new Color(color.r, color.g, color.b, 0.42f),
            };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
