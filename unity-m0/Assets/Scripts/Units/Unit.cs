using UnityEngine;

namespace NaijaEmpires
{
    /// Base unit: kinematic movement across the flat ground, faction ownership, and
    /// population bookkeeping on death.
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Health))]
    public class Unit : MonoBehaviour
    {
        public float speed = 4.5f;
        protected Vector3? moveTarget;
        protected FactionId Owner;

        protected virtual void Start()
        {
            var f = GetComponent<Faction>();
            if (f != null) Owner = f.Id;
            var h = GetComponent<Health>();
            if (h != null) h.Died += OnDied;
        }

        void OnDied(Health h)
        {
            var e = Match.Econ(Owner);
            if (e != null) e.AddPop(-1);
        }

        public virtual void MoveTo(Vector3 pos) => SetTarget(pos);

        protected void SetTarget(Vector3 pos)
        {
            pos.y = transform.position.y;
            moveTarget = pos;
        }

        protected void MoveStop() => moveTarget = null;

        protected bool MoveStep()
        {
            if (moveTarget == null) return true;
            Vector3 delta = moveTarget.Value - transform.position;
            delta.y = 0f;
            if (delta.magnitude < 0.15f) { moveTarget = null; return true; }
            Vector3 dir = delta.normalized;
            transform.position += dir * speed * Time.deltaTime;
            transform.forward = Vector3.Slerp(transform.forward, dir, 10f * Time.deltaTime);
            return false;
        }

        protected virtual void Update() => MoveStep();
    }
}
