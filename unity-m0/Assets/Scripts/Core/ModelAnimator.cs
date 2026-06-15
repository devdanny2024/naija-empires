using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace NaijaEmpires
{
    /// Drives a unit/building's "Model" child.
    ///
    /// Two modes, chosen automatically:
    ///  • RIGGED  — when InitRig() is handed real animation clips (animated Quaternius characters):
    ///    crossfades Idle / Walk / Run / Walk_Carry / PickUp / SwordSlash / Shoot_OneHanded / Death
    ///    via a tiny 2-input Playables mixer. Picks clips from the unit's role + state.
    ///  • PROCEDURAL — no rig/clips (primitives, buildings): the original walk bob, gather chop,
    ///    attack lunge, carry cube, and hit punch — no art needed.
    public class ModelAnimator : MonoBehaviour
    {
        Transform _model;
        Vector3 _basePos, _baseScale;
        Quaternion _baseRot;
        Transform _carry;

        Vector3 _lastPos;
        float _phase;
        bool _gathering;
        bool _carrying;
        float _lungeT;
        Vector3 _lungeDir;
        float _hitT;

        // --- rigged mode ---
        bool _rigged;
        bool _dead;
        PlayableGraph _graph;
        AnimationPlayableOutput _output;
        AnimationMixerPlayable _mixer;
        AnimationClipPlayable _cur, _prev;   // _cur = incoming (input 1), _prev = outgoing (input 0)
        float _blend;                        // weight of _cur, 0..1
        const float BlendDur = 0.15f;
        readonly Dictionary<string, AnimationClip> _clips = new();
        string _curName;
        float _curLen;
        bool _curLoop;
        float _attackT;
        string _moveClip = "Walk";
        string _attackClip = "SwordSlash";

        void Awake()
        {
            _model = transform.Find("Model");
            if (_model != null) { _basePos = _model.localPosition; _baseScale = _model.localScale; _baseRot = _model.localRotation; }
            _lastPos = transform.position;
        }

        /// Switch this animator to rigged playback using the supplied clips (from the imported FBX).
        /// No-op (stays procedural) if there's no Animator on the model or no usable clips.
        public void InitRig(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            var animator = GetComponentInChildren<Animator>();
            if (animator == null) return;

            // FBX names rigged clips "<Armature>|<Take>" e.g. "CharacterArmature|Idle" -> key "Idle".
            foreach (var c in clips)
            {
                if (c == null) continue;
                string key = c.name;
                int bar = key.LastIndexOf('|');
                if (bar >= 0) key = key.Substring(bar + 1);
                if (!_clips.ContainsKey(key)) _clips[key] = c;
            }

            // Fail-safe: only go rigged if the real named clips are present. If the FBX imported as
            // one merged clip (clip names won't match), stay PROCEDURAL — that keeps the carry cube,
            // gather chop, and visible motion working — and log what DID import so we can map it.
            bool hasIdle = _clips.ContainsKey("Idle");
            bool hasMove = _clips.ContainsKey("Walk") || _clips.ContainsKey("Run");
            if (!hasIdle || !hasMove)
            {
                Debug.LogWarning($"[NaijaEmpires] {name}: rig present but expected clips missing — " +
                                 $"imported clips = [{string.Join(", ", _clips.Keys)}]. " +
                                 "Using procedural animation. Send me that clip list to wire the rig.");
                _clips.Clear();
                return; // _rigged stays false -> procedural fallback
            }

            // Role-specific clip choices (fall back to whatever locomotion clip exists).
            _moveClip = _clips.ContainsKey("Walk") ? "Walk" : "Run";
            var combat = GetComponent<CombatUnit>();
            if (combat != null)
            {
                if (combat.Type == UnitType.Cavalry && _clips.ContainsKey("Run")) _moveClip = "Run";
                _attackClip = combat.Type == UnitType.Archer ? "Shoot_OneHanded" : "SwordSlash";
            }

            try
            {
                animator.applyRootMotion = false;
                _graph = PlayableGraph.Create("UnitAnim_" + name);
                _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                _output = AnimationPlayableOutput.Create(_graph, "out", animator);
                _mixer = AnimationMixerPlayable.Create(_graph, 2);
                _output.SetSourcePlayable(_mixer);
                _rigged = true;
                Play("Idle", true);
                _graph.Play();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NaijaEmpires] {name}: Playables init failed ({ex.Message}) — procedural fallback.");
                if (_graph.IsValid()) _graph.Destroy();
                _rigged = false;
            }
        }

        public void SetGathering(bool on) => _gathering = on;

        public void Lunge(Vector3 worldDir)
        {
            if (_rigged)
            {
                _attackT = ClipLen(_attackClip);
                // Re-attacks land on the same clip name, so Play() would no-op — restart it explicitly
                // so every swing plays (fixes "attack animates only once").
                if (_curName == _attackClip && _cur.IsValid()) _cur.SetTime(0);
                return;
            }
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude > 0.0001f) { _lungeDir = worldDir.normalized; _lungeT = 1f; }
        }

        public void Hit()
        {
            if (_rigged) return; // procedural-only punch; rig flinch deferred (jitters under rapid fire)
            _hitT = 1f;
        }

        /// Play the death clip and freeze further state changes. Returns true if an animated death
        /// started (caller should delay the actual Destroy); false for procedural/buildings.
        public bool Die()
        {
            if (!_rigged || !Has("Death")) return false;
            _dead = true;
            Play("Death", false);
            return true;
        }

        public void SetCarrying(bool on, Color color)
        {
            _carrying = on;
            if (_rigged) return; // rigged uses the Walk_Carry clip, no cube needed

            if (on)
            {
                if (_carry == null)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var col = cube.GetComponent<Collider>(); if (col) Destroy(col);
                    cube.transform.SetParent(transform, false);
                    cube.transform.localPosition = new Vector3(0f, 1.95f, 0f);
                    cube.transform.localScale = Vector3.one * 0.3f;
                    MaterialUtil.SetColor(cube.GetComponent<Renderer>(), color);
                    _carry = cube.transform;
                }
                else _carry.gameObject.SetActive(true);
            }
            else if (_carry != null) _carry.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_rigged) { RiggedUpdate(); return; }
            ProceduralUpdate();
        }

        // ---------------------------------------------------------------- rigged
        void RiggedUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            if (!_dead)
            {
                float speed = (transform.position - _lastPos).magnitude / dt;
                bool moving = speed > 0.5f;

                string desired;
                bool loop;
                if (_attackT > 0f)
                {
                    _attackT -= dt;
                    desired = Has(_attackClip) ? _attackClip : (moving ? _moveClip : "Idle");
                    loop = false;
                }
                else if (_gathering && Has("PickUp")) { desired = "PickUp"; loop = true; }
                else if (moving) { desired = (_carrying && Has("Walk_Carry")) ? "Walk_Carry" : _moveClip; loop = true; }
                else { desired = "Idle"; loop = true; }

                Play(desired, loop);
            }
            _lastPos = transform.position;

            // Advance the crossfade.
            if (_blend < 1f)
            {
                _blend = Mathf.Min(1f, _blend + dt / BlendDur);
                _mixer.SetInputWeight(0, 1f - _blend);
                _mixer.SetInputWeight(1, _blend);
                if (_blend >= 1f && _prev.IsValid()) { _mixer.DisconnectInput(0); _graph.DestroyPlayable(_prev); _prev = default; }
            }

            // Loop the active clip (so we don't depend on each clip's imported wrap mode).
            if (_curLoop && _cur.IsValid() && _curLen > 0f && _cur.GetTime() >= _curLen)
                _cur.SetTime(_cur.GetTime() % _curLen);
        }

        bool Has(string n) => _clips.ContainsKey(n);
        float ClipLen(string n) => _clips.TryGetValue(n, out var c) ? c.length : 0.4f;

        void Play(string clipName, bool loop)
        {
            if (clipName == _curName) { _curLoop = loop; return; }
            if (!_clips.TryGetValue(clipName, out var clip) || clip == null) return;

            // Free both mixer inputs, retire the previous outgoing, then shift current -> outgoing.
            _mixer.DisconnectInput(0);
            _mixer.DisconnectInput(1);
            if (_prev.IsValid()) _graph.DestroyPlayable(_prev);

            _prev = _cur;
            _cur = AnimationClipPlayable.Create(_graph, clip);
            _cur.SetApplyFootIK(false);

            if (_prev.IsValid()) _mixer.ConnectInput(0, _prev, 0);
            _mixer.ConnectInput(1, _cur, 0);

            _blend = _prev.IsValid() ? 0f : 1f; // first clip snaps to full weight
            _mixer.SetInputWeight(0, 1f - _blend);
            _mixer.SetInputWeight(1, _blend);

            _curName = clipName;
            _curLen = clip.length;
            _curLoop = loop;
        }

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        // ---------------------------------------------------------------- procedural
        void ProceduralUpdate()
        {
            if (_model == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = (transform.position - _lastPos).magnitude / dt;
            _lastPos = transform.position;
            bool moving = speed > 0.5f;

            Vector3 pos = _basePos;
            Vector3 scale = _baseScale;
            Quaternion rot = Quaternion.identity;

            if (moving)
            {
                _phase += dt * 12f;
                pos.y += Mathf.Abs(Mathf.Sin(_phase)) * 0.12f;
                rot = Quaternion.Euler(0f, 0f, Mathf.Sin(_phase) * 6f);
            }
            else if (_gathering)
            {
                _phase += dt * 14f;
                rot = Quaternion.Euler(Mathf.Abs(Mathf.Sin(_phase)) * 28f, 0f, 0f);
            }

            if (_lungeT > 0f)
            {
                _lungeT -= dt * 4f;
                float k = Mathf.Clamp01(_lungeT);
                Vector3 local = transform.InverseTransformDirection(_lungeDir);
                pos += local * (Mathf.Sin((1f - k) * Mathf.PI) * 0.5f);
            }

            if (_hitT > 0f)
            {
                _hitT -= dt * 5f;
                scale = _baseScale * (1f + 0.25f * Mathf.Clamp01(_hitT));
            }

            _model.localPosition = pos;
            _model.localScale = scale;
            // Compose the procedural bob/lunge tilt on top of the model's authored facing
            // offset (ModelLibrary.Def.RotY) instead of overwriting it to identity.
            _model.localRotation = _baseRot * rot;
        }
    }
}
