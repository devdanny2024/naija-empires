using UnityEngine;

namespace NaijaEmpires
{
    /// Procedural animation for a unit/building's "Model" child — no rigs or art needed.
    /// Walk bob, gather chop, attack lunge, carry indicator, and a hit punch.
    public class ModelAnimator : MonoBehaviour
    {
        Transform _model;
        Vector3 _basePos, _baseScale;
        Transform _carry;

        Vector3 _lastPos;
        float _phase;
        bool _gathering;
        float _lungeT;
        Vector3 _lungeDir;
        float _hitT;

        void Awake()
        {
            _model = transform.Find("Model");
            if (_model != null) { _basePos = _model.localPosition; _baseScale = _model.localScale; }
            _lastPos = transform.position;
        }

        public void SetGathering(bool on) => _gathering = on;
        public void Lunge(Vector3 worldDir)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude > 0.0001f) { _lungeDir = worldDir.normalized; _lungeT = 1f; }
        }
        public void Hit() => _hitT = 1f;

        public void SetCarrying(bool on, Color color)
        {
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
            _model.localRotation = rot;
        }
    }
}
