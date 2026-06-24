using UnityEngine;

namespace NaijaEmpires
{
    /// A purely-visual projectile: travels start→end over a duration (optional parabolic arc),
    /// orients its long (+Z) axis along its velocity, and fires a callback + self-destructs on arrival.
    /// Damage is applied instantly by CombatUnit; this only sells the hit visually.
    public class Projectile : MonoBehaviour
    {
        Vector3 _a, _b; float _t, _dur, _arc; System.Action _onHit;

        public static void Launch(GameObject go, Vector3 a, Vector3 b, float speed, float arc, System.Action onHit)
        {
            var p = go.AddComponent<Projectile>();
            p._a = a; p._b = b; p._arc = arc; p._onHit = onHit;
            p._dur = Mathf.Max(0.05f, Vector3.Distance(a, b) / Mathf.Max(0.1f, speed));
            go.transform.position = a;
        }

        void Update()
        {
            _t += Time.deltaTime / _dur;
            if (_t >= 1f) { _onHit?.Invoke(); Destroy(gameObject); return; }
            Vector3 prev = transform.position;
            Vector3 p = Vector3.Lerp(_a, _b, _t);
            p.y += _arc * Mathf.Sin(_t * Mathf.PI);   // parabolic lob
            transform.position = p;
            Vector3 v = p - prev;
            if (v.sqrMagnitude > 1e-5f) transform.rotation = Quaternion.LookRotation(v);
        }
    }

    /// Short-lived grow effect (muzzle flash / spark / explosion): lerps scale then destroys.
    public class FxPuff : MonoBehaviour
    {
        float _life, _max; Vector3 _s0, _s1;

        public static void Spawn(GameObject go, float life, Vector3 s0, Vector3 s1)
        {
            var f = go.AddComponent<FxPuff>();
            f._max = Mathf.Max(0.02f, life); f._s0 = s0; f._s1 = s1;
            go.transform.localScale = s0;
        }

        void Update()
        {
            _life += Time.deltaTime;
            float k = _life / _max;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.Lerp(_s0, _s1, k);
        }
    }

    /// Per-unit-type attack visuals — all generated from primitives (no art assets), per the asset plan.
    /// Melee → hit spark; Archer → arrow; Gunner/Rifleman → muzzle flash + tracer; Catapult/Tank → shell + boom.
    public static class AttackFX
    {
        static readonly Color Muzzle = new Color(1f, 0.82f, 0.35f);
        static readonly Color Spark  = new Color(1f, 0.62f, 0.22f);
        static readonly Color Boom   = new Color(1f, 0.5f, 0.15f);
        static readonly Color Wood   = new Color(0.45f, 0.3f, 0.15f);

        static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col) Object.Destroy(col);
            g.transform.position = pos; g.transform.localScale = scale;
            MaterialUtil.SetColor(g.GetComponent<Renderer>(), c);
            return g;
        }

        /// Spawn the attack effect for `type`, firing from the attacker toward the target.
        public static void Fire(UnitType type, Vector3 from, Vector3 to)
        {
            from.y += 1.1f;  // roughly chest / muzzle height
            to.y   += 0.8f;
            switch (type)
            {
                case UnitType.Archer:               Arrow(from, to); break;
                case UnitType.Gunner:
                case UnitType.Rifleman:             Gun(from, to); break;
                case UnitType.Catapult:             Shell(from, to, false); break;
                case UnitType.Tank:                 Shell(from, to, true); break;
                default:                            Melee(to); break;  // spearman / cavalry / villager
            }
        }

        static void Melee(Vector3 at)
        {
            var g = Prim(PrimitiveType.Sphere, at, Vector3.one * 0.15f, Spark);
            FxPuff.Spawn(g, 0.16f, Vector3.one * 0.15f, Vector3.one * 0.5f);
        }

        static void Arrow(Vector3 a, Vector3 b)
        {
            var g = Prim(PrimitiveType.Cube, a, new Vector3(0.05f, 0.05f, 0.45f), Wood);
            Projectile.Launch(g, a, b, 24f, 0.5f, () => Impact(b, Spark, 0.35f));
        }

        static void Gun(Vector3 a, Vector3 b)
        {
            var flash = Prim(PrimitiveType.Sphere, a, Vector3.one * 0.2f, Muzzle);
            FxPuff.Spawn(flash, 0.07f, Vector3.one * 0.2f, Vector3.one * 0.04f);
            var tracer = Prim(PrimitiveType.Cube, a, new Vector3(0.03f, 0.03f, 0.6f), new Color(1f, 0.9f, 0.5f));
            Projectile.Launch(tracer, a, b, 60f, 0f, () => Impact(b, Muzzle, 0.25f));
        }

        static void Shell(Vector3 a, Vector3 b, bool tank)
        {
            var g = Prim(PrimitiveType.Sphere, a, Vector3.one * 0.18f, new Color(0.18f, 0.18f, 0.2f));
            if (tank)
            {
                var flash = Prim(PrimitiveType.Sphere, a, Vector3.one * 0.25f, Muzzle);
                FxPuff.Spawn(flash, 0.08f, Vector3.one * 0.25f, Vector3.one * 0.05f);
            }
            Projectile.Launch(g, a, b, tank ? 42f : 15f, tank ? 0.6f : 3f, () => Explosion(b));
        }

        static void Impact(Vector3 at, Color c, float size)
        {
            var g = Prim(PrimitiveType.Sphere, at, Vector3.one * size * 0.5f, c);
            FxPuff.Spawn(g, 0.2f, Vector3.one * size * 0.5f, Vector3.one * size * 1.6f);
        }

        static void Explosion(Vector3 at)
        {
            var g = Prim(PrimitiveType.Sphere, at, Vector3.one * 0.4f, Boom);
            FxPuff.Spawn(g, 0.35f, Vector3.one * 0.4f, Vector3.one * 2.2f);
        }
    }
}
