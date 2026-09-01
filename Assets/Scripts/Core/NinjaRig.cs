using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    public enum RigPose { Idle, Run, Strike1, Strike2, Cleave, Windup, Hurt, Dash, Dead, Strike3, Spawn, Taunt }

    /// <summary>
    /// Articulated ninja built from primitives at runtime and animated procedurally —
    /// no models, no animation clips. Legacy fallback CharacterRig for enemy kinds
    /// that have no imported skeletal character yet. Owners drive it with
    /// move01 (locomotion blend), PlayOneShot (attacks), ForcePose (AI states).
    /// Fields are public so the editor bootstrap can configure prefabs in batch mode.
    /// </summary>
    public class NinjaRig : CharacterRig
    {
        [Header("Style")]
        public Color bodyColor = new(0.16f, 0.19f, 0.25f);
        public Color accentColor = new(1f, 0.42f, 0.29f);
        public float rigScale = 1f;
        public bool ghost;             // translucent (shades, clones)
        public float ghostAlpha = 0.5f;
        public bool hasSword = true;
        public bool hasScarf;
        public bool maskStripe = true;

        // Joints.
        private Transform _body, _pelvis, _torso, _head;
        private Transform _armL, _armR, _foreL, _foreR;
        private Transform _legL, _legR, _shinL, _shinR;
        private Transform _swordJoint, _scarf;
        private TrailRenderer _trail;

        private Material _mat, _accentMat;
        private readonly List<Renderer> _renderers = new();
        private float _flashT;
        private float _cycle;

        private bool _forcedThisFrame;
        private RigPose _forcedPose;
        private float _forcedPhase;

        private bool _oneActive;
        private RigPose _onePose;
        private float _oneT, _oneDur;

        private static Material _afterImageBase;
        private readonly List<AfterImageFader> _images = new();
        private Transform[] _sourceBones;

        // ------------------------------------------------------------- build

        private void Awake()
        {
            var existing = transform.Find("BodyRig");
            if (existing != null) Destroy(existing.gameObject); // clone guard
            Build();
        }

        private void Build()
        {
            var shader = Shader.Find(ghost ? "Emberline/Ghost" : "Emberline/Toon");
            _mat = new Material(shader);
            _accentMat = new Material(shader);
            SetMatColor(_mat, bodyColor, ghost ? ghostAlpha : 1f);
            SetMatColor(_accentMat, accentColor, ghost ? ghostAlpha : 1f);

            _body = new GameObject("BodyRig").transform;
            _body.SetParent(transform, false);
            _body.localScale = Vector3.one * rigScale;

            _pelvis = Joint(_body, "Pelvis", new Vector3(0, 0.95f, 0));
            _torso = Joint(_pelvis, "Torso", Vector3.zero);
            Part(_torso, PrimitiveType.Cube, new Vector3(0, 0.28f, 0), new Vector3(0.34f, 0.52f, 0.22f), _mat);
            // Belt accent.
            Part(_torso, PrimitiveType.Cube, new Vector3(0, 0.04f, 0), new Vector3(0.36f, 0.07f, 0.24f), _accentMat);

            _head = Joint(_torso, "Head", new Vector3(0, 0.62f, 0));
            Part(_head, PrimitiveType.Sphere, new Vector3(0, 0.1f, 0), Vector3.one * 0.34f, _mat);
            if (maskStripe)
                Part(_head, PrimitiveType.Cube, new Vector3(0, 0.12f, 0.155f), new Vector3(0.3f, 0.07f, 0.05f), _accentMat);

            // Arms: upper arm swings from the shoulder, forearm from the elbow.
            _armL = Joint(_torso, "ArmL", new Vector3(-0.24f, 0.5f, 0));
            _foreL = BuildLimb(_armL, 0.28f, 0.10f, out _, 0.26f, 0.085f);
            _armR = Joint(_torso, "ArmR", new Vector3(0.24f, 0.5f, 0));
            _foreR = BuildLimb(_armR, 0.28f, 0.10f, out _, 0.26f, 0.085f);

            // Legs.
            _legL = Joint(_pelvis, "LegL", new Vector3(-0.12f, 0, 0));
            _shinL = BuildLimb(_legL, 0.46f, 0.13f, out _, 0.45f, 0.11f);
            _legR = Joint(_pelvis, "LegR", new Vector3(0.12f, 0, 0));
            _shinR = BuildLimb(_legR, 0.46f, 0.13f, out _, 0.45f, 0.11f);

            if (hasSword)
            {
                _swordJoint = Joint(_foreR, "Sword", new Vector3(0, -0.26f, 0));
                _swordJoint.localRotation = Quaternion.Euler(-15f, 0, 0);
                Part(_swordJoint, PrimitiveType.Cube, new Vector3(0, -0.42f, 0), new Vector3(0.045f, 0.85f, 0.045f), _accentMat);
                Part(_swordJoint, PrimitiveType.Cube, new Vector3(0, -0.02f, 0), new Vector3(0.12f, 0.04f, 0.12f), _mat);

                var trailGo = new GameObject("Trail");
                trailGo.transform.SetParent(_swordJoint, false);
                trailGo.transform.localPosition = new Vector3(0, -0.8f, 0);
                _trail = trailGo.AddComponent<TrailRenderer>();
                _trail.time = 0.14f;
                _trail.startWidth = 0.32f * rigScale;
                _trail.endWidth = 0.02f;
                _trail.material = new Material(Shader.Find("Emberline/Glow"));
                _trail.startColor = new Color(0.85f, 0.93f, 1f, 0.8f);
                _trail.endColor = new Color(1f, 0.45f, 0.28f, 0f);
                _trail.emitting = false;
            }

            if (hasScarf)
            {
                _scarf = Joint(_torso, "Scarf", new Vector3(0, 0.52f, -0.13f));
                Part(_scarf, PrimitiveType.Cube, new Vector3(0, -0.02f, -0.1f), new Vector3(0.2f, 0.06f, 0.22f), _accentMat);
                Part(_scarf, PrimitiveType.Cube, new Vector3(0, -0.05f, -0.24f), new Vector3(0.14f, 0.05f, 0.16f), _accentMat);
            }
        }

        private Transform Joint(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        /// <summary>Upper segment from `joint`, returns the lower joint (elbow/knee child).</summary>
        private Transform BuildLimb(Transform joint, float upperLen, float upperThick,
            out Transform lower, float lowerLen, float lowerThick)
        {
            Part(joint, PrimitiveType.Cube, new Vector3(0, -upperLen / 2, 0),
                new Vector3(upperThick, upperLen, upperThick), _mat);
            lower = Joint(joint, "Lower", new Vector3(0, -upperLen, 0));
            Part(lower, PrimitiveType.Cube, new Vector3(0, -lowerLen / 2, 0),
                new Vector3(lowerThick, lowerLen, lowerThick), _mat);
            return lower;
        }

        private void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _renderers.Add(r);
        }

        private static void SetMatColor(Material m, Color c, float a) =>
            m.color = new Color(c.r, c.g, c.b, a);

        // --------------------------------------------------------------- api

        public override void PlayOneShot(RigPose pose, float duration)
        {
            _oneActive = true;
            _onePose = pose;
            _oneT = 0f;
            _oneDur = Mathf.Max(0.05f, duration);
        }

        /// <summary>AI states call this every frame; wins over everything else.</summary>
        public override void ForcePose(RigPose pose, float phase)
        {
            _forcedThisFrame = true;
            _forcedPose = pose;
            _forcedPhase = phase;
        }

        public override void Flash() => _flashT = 0.13f;

        public override void MakeGhost(float alpha)
        {
            ghost = true;
            ghostAlpha = alpha;
            // Materials were built opaque in Awake — swap to the ghost shader.
            var shader = Shader.Find("Emberline/Ghost");
            if (_mat != null) _mat.shader = shader;
            if (_accentMat != null) _accentMat.shader = shader;
            SetGhostAlpha(alpha);
            foreach (var r in _renderers)
                if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public override void SetGhostAlpha(float a)
        {
            if (!ghost) return;
            SetMatColor(_mat, bodyColor, a);
            SetMatColor(_accentMat, accentColor, Mathf.Min(1f, a + 0.15f));
        }

        public override void SetBaseColor(Color c)
        {
            bodyColor = c;
            if (_flashT <= 0f) SetMatColor(_mat, c, ghost ? ghostAlpha : 1f);
        }

        /// <summary>
        /// Pooled ghost trail. A Flicker Step fires one of these every 55ms, so
        /// the old Instantiate-clone-then-Destroy cost a full primitive hierarchy
        /// plus a Material per image. Clones are built once and re-posed from the
        /// live rig on reuse — the same approach SkeletalRig already takes.
        /// </summary>
        public override void SpawnAfterImage()
        {
            if (_body == null) return;
            if (_afterImageBase == null)
                _afterImageBase = new Material(Shader.Find("Emberline/Ghost"))
                    { color = new Color(1f, 0.5f, 0.3f, 0.4f) };

            AfterImageFader img = null;
            foreach (var candidate in _images)
                if (candidate != null && !candidate.gameObject.activeSelf) { img = candidate; break; }

            if (img == null)
            {
                if (_images.Count >= 6) return; // cap — beyond this they overlap anyway
                var clone = Instantiate(_body.gameObject, _body.position, _body.rotation);
                clone.name = "AfterImage";
                foreach (var tr in clone.GetComponentsInChildren<TrailRenderer>(true)) Destroy(tr);
                var mat = new Material(_afterImageBase);
                foreach (var r in clone.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                img = clone.AddComponent<AfterImageFader>();
                img.mat = mat;
                img.bones = clone.GetComponentsInChildren<Transform>(true);
                _images.Add(img);
            }

            img.Capture(_body, _sourceBones ??= _body.GetComponentsInChildren<Transform>(true));
        }

        public override void ResetVisuals()
        {
            _flashT = 0f;
            _oneActive = false;
            _forcedThisFrame = false;
            SetMatColor(_mat, bodyColor, ghost ? ghostAlpha : 1f);
            foreach (var img in _images)
                if (img != null) img.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ animate

        private void LateUpdate()
        {
            var dt = Time.deltaTime;

            if (_flashT > 0f)
            {
                _flashT -= dt;
                var white = _flashT > 0f;
                SetMatColor(_mat, white ? Color.white : bodyColor, ghost ? ghostAlpha : 1f);
            }

            RigPose pose;
            float phase;
            if (_forcedThisFrame)
            {
                pose = _forcedPose; phase = _forcedPhase;
                _forcedThisFrame = false;
                _oneActive = false;
            }
            else if (_oneActive)
            {
                _oneT += dt;
                phase = Mathf.Clamp01(_oneT / _oneDur);
                pose = _onePose;
                if (_oneT >= _oneDur) _oneActive = false;
            }
            else
            {
                pose = move01 > 0.12f ? RigPose.Run : RigPose.Idle;
                phase = 0f;
            }

            _cycle += dt * (7f + 6f * move01);
            Apply(pose, phase, dt);

            if (_trail != null)
                _trail.emitting = (pose is RigPose.Strike1 or RigPose.Strike2 or RigPose.Strike3 or RigPose.Cleave)
                                  && phase > 0.1f && phase < 0.85f;
        }

        /// <summary>Fast-out easing so attacks snap to extension early.</summary>
        private static float Snap(float p)
        {
            var t = Mathf.Min(1f, p * 2.6f);
            return t * t * (3f - 2f * t);
        }

        private void Apply(RigPose pose, float phase, float dt)
        {
            // Poses added for skeletal rigs map onto the closest primitive pose.
            if (pose == RigPose.Strike3) pose = RigPose.Strike1;
            if (pose == RigPose.Spawn) pose = RigPose.Idle;
            if (pose == RigPose.Taunt) pose = RigPose.Windup;
            // Target angles: X = forward swing (negative lifts limb forward),
            // torsoYaw sweeps sword strikes, pitch leans the torso.
            float armL = 8, armR = 8, foreL = 25, foreR = 25;
            float legL = 3, legR = -3, shinL = 5, shinR = 5;
            float pitch = 0, yaw = 0, bodyPitch = 0, pelvisY = 0.95f;

            var run = Mathf.Sin(_cycle);
            switch (pose)
            {
                case RigPose.Idle:
                    var breathe = Mathf.Sin(Time.time * 2.2f) * 2f;
                    pitch = 2 + breathe * 0.5f;
                    foreL = 30 + breathe; foreR = 30 + breathe;
                    break;

                case RigPose.Run:
                    legL = 42 * run; legR = -42 * run;
                    shinL = 30 + 22 * Mathf.Max(0, -run);
                    shinR = 30 + 22 * Mathf.Max(0, run);
                    armL = -34 * run; armR = 34 * run;
                    foreL = 55; foreR = 55;
                    pitch = 10;
                    pelvisY = 0.95f + Mathf.Abs(run) * 0.035f;
                    break;

                case RigPose.Strike1:
                {
                    var p = Snap(phase);
                    armR = Mathf.Lerp(35, 95, p); foreR = Mathf.Lerp(70, 5, p);
                    armL = Mathf.Lerp(10, -35, p); foreL = 40;
                    yaw = Mathf.Lerp(-28, 42, p);
                    pitch = 8 + 8 * p;
                    legL = 14; legR = -18; shinR = 16;
                    break;
                }
                case RigPose.Strike2:
                {
                    var p = Snap(phase);
                    armR = Mathf.Lerp(135, 25, p); foreR = Mathf.Lerp(25, 8, p);
                    armL = -25; foreL = 45;
                    yaw = Mathf.Lerp(30, -35, p);
                    pitch = 6 + 10 * p;
                    legL = -14; legR = 16; shinL = 14;
                    break;
                }
                case RigPose.Cleave:
                {
                    // First half raise overhead, second half slam.
                    var p = phase < 0.5f ? phase * 2f : 1f;
                    var slam = phase < 0.5f ? 0f : Snap((phase - 0.5f) * 2f);
                    armR = Mathf.Lerp(35, 155, p) - slam * 135;
                    armL = Mathf.Lerp(10, 140, p) - slam * 120;
                    foreR = 12; foreL = 20;
                    pitch = Mathf.Lerp(-6, -14, p) + slam * 34;
                    pelvisY = 0.95f - slam * 0.08f;
                    break;
                }
                case RigPose.Windup:
                    var tremble = Mathf.Sin(Time.time * 26f) * 3f;
                    armL = 150 + tremble; armR = 152 - tremble;
                    foreL = 15; foreR = 15;
                    pitch = -12;
                    break;

                case RigPose.Hurt:
                    pitch = -24; armL = 55; armR = -45; foreL = 30; foreR = 40;
                    legL = 18; shinL = 20;
                    break;

                case RigPose.Dash:
                    pitch = 26; armL = -55; armR = -55; foreL = 20; foreR = 20;
                    legL = 35; legR = -30; shinR = 40;
                    break;

                case RigPose.Dead:
                {
                    var p = Mathf.Clamp01(phase);
                    bodyPitch = -88 * p;
                    pelvisY = Mathf.Lerp(0.95f, 0.30f, p);
                    armL = 70; armR = 40; foreL = 30; foreR = 25;
                    legL = 25; legR = 10; shinL = 30; shinR = 20;
                    break;
                }
            }

            var s = 1f - Mathf.Exp(-22f * dt);
            Slerp(_armL, -armL, s); Slerp(_armR, -armR, s);
            Slerp(_foreL, -foreL, s); Slerp(_foreR, -foreR, s);
            Slerp(_legL, -legL, s); Slerp(_legR, -legR, s);
            Slerp(_shinL, shinL, s); Slerp(_shinR, shinR, s); // knees bend backward
            _torso.localRotation = Quaternion.Slerp(_torso.localRotation,
                Quaternion.Euler(pitch, yaw, 0), s);
            _body.localRotation = Quaternion.Slerp(_body.localRotation,
                Quaternion.Euler(bodyPitch, 0, 0), s);
            _pelvis.localPosition = Vector3.Lerp(_pelvis.localPosition,
                new Vector3(0, pelvisY, 0), s);

            if (_scarf != null)
            {
                var flutter = Mathf.Sin(Time.time * 8f) * 8f + move01 * 25f;
                _scarf.localRotation = Quaternion.Slerp(_scarf.localRotation,
                    Quaternion.Euler(-flutter, 0, Mathf.Sin(Time.time * 5f) * 6f), s);
            }
        }

        private static void Slerp(Transform joint, float xDeg, float s)
        {
            if (joint == null) return;
            joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(xDeg, 0, 0), s);
        }
    }

    /// <summary>Fades and destroys a Flicker after-image.</summary>
    /// <summary>
    /// One pooled ghost of the primitive rig. Re-posed from the live hierarchy on
    /// each reuse, then faded out and parked inactive for the next dash.
    /// </summary>
    public class AfterImageFader : MonoBehaviour
    {
        private const float Life = 0.28f;

        public Material mat;
        public Transform[] bones;

        private float _life;

        /// <summary>Snap this ghost onto the source rig's current pose and show it.</summary>
        public void Capture(Transform source, Transform[] sourceBones)
        {
            transform.SetPositionAndRotation(source.position, source.rotation);
            transform.localScale = source.lossyScale;
            // Index 0 is the root on both sides, already placed above.
            var n = Mathf.Min(bones.Length, sourceBones.Length);
            for (var i = 1; i < n; i++)
            {
                if (bones[i] == null || sourceBones[i] == null) continue;
                bones[i].localPosition = sourceBones[i].localPosition;
                bones[i].localRotation = sourceBones[i].localRotation;
                bones[i].localScale = sourceBones[i].localScale;
            }
            _life = Life;
            if (mat != null) mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.4f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { gameObject.SetActive(false); return; }
            if (mat != null)
            {
                var c = mat.color;
                mat.color = new Color(c.r, c.g, c.b, 0.4f * (_life / Life));
            }
        }
    }
}
