using UnityEngine;

namespace NaijaEmpires
{
    /// The rock-paper-scissors counter system: Spear > Cavalry > Archer > Spear.
    public static class CombatTriangle
    {
        public const float Bonus = 1.6f;

        public static float Multiplier(UnitType attacker, UnitType defender)
        {
            if (attacker == UnitType.Spearman && defender == UnitType.Cavalry) return Bonus;
            if (attacker == UnitType.Cavalry && defender == UnitType.Archer) return Bonus;
            if (attacker == UnitType.Archer && defender == UnitType.Spearman) return Bonus;
            return 1f;
        }
    }

    /// A fighting unit. Auto-acquires the nearest enemy in aggro range, chases to attack range,
    /// and attacks on cooldown applying the counter multiplier. Player can also force a target.
    [RequireComponent(typeof(Health))]
    public class CombatUnit : Unit
    {
        public UnitType Type = UnitType.Spearman;
        public UnitType FxType = UnitType.Spearman; // visual form for attack FX (modern form in the oil age)
        public float damage = 7f;
        public float attackRange = 1.7f;
        public float aggroRange = 9f;
        public float attackInterval = 1.4f; // slower swings → battles last longer (was 1f, felt too fast)

        float _cooldown;
        Health _forcedTarget;
        ModelAnimator _anim;

        protected override void Start()
        {
            base.Start();
            _anim = GetComponent<ModelAnimator>();
        }

        public void AttackTarget(Health t) => _forcedTarget = t;

        public override void MoveTo(Vector3 pos) { _forcedTarget = null; base.MoveTo(pos); }

        protected override void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;

            Health target = (_forcedTarget != null && !_forcedTarget.Dead) ? _forcedTarget : AcquireTarget();

            if (target == null) { MoveStep(); return; }

            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > attackRange)
            {
                SetTarget(target.transform.position);
                MoveStep();
            }
            else
            {
                MoveStop();
                TryAttack(target);
            }
        }

        void TryAttack(Health target)
        {
            if (_cooldown > 0f) return;

            Vector3 dir = target.transform.position - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.forward = dir.normalized;
            if (_anim != null) _anim.Lunge(dir);

            // Attack visual (arrow / muzzle flash + tracer / melee spark / shell + boom) per unit form —
            // FxType is the MODERN form in the oil age, so a modernised archer fires bullets, not arrows.
            AttackFX.Fire(FxType, transform.position, target.transform.position);

            float mult = CombatTriangle.Multiplier(Type, TypeOf(target));
            target.TakeDamage(damage * mult);
            _cooldown = attackInterval;
        }

        Health AcquireTarget()
        {
            Health bestAny = null, bestBuilding = null;
            float baSqr = aggroRange * aggroRange, bbSqr = aggroRange * aggroRange;
            foreach (var f in Faction.All)
            {
                if (f.Id == Owner) continue;
                var h = f.GetComponent<Health>();
                if (h == null || h.Dead) continue;
                float sq = (h.transform.position - transform.position).sqrMagnitude;
                if (sq <= baSqr) { baSqr = sq; bestAny = h; }
                // A building is anything with Health but no Unit (units carry a Unit component).
                if (f.GetComponent<Unit>() == null && sq <= bbSqr) { bbSqr = sq; bestBuilding = h; }
            }
            // Catapults are siege: prefer the nearest enemy building, fall back to any target.
            if (Type == UnitType.Catapult && bestBuilding != null) return bestBuilding;
            return bestAny;
        }

        static UnitType TypeOf(Health h)
        {
            var c = h.GetComponent<CombatUnit>();
            return c != null ? c.Type : UnitType.Villager;
        }
    }

    /// Stationary defensive building (Benin's specialty). Attacks the nearest enemy in range.
    [RequireComponent(typeof(Health))]
    public class Tower : MonoBehaviour
    {
        public float damage = 9f;
        public float range = 8f;
        public float interval = 1.1f;

        FactionId _faction;
        float _cooldown;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
        }

        void Update()
        {
            if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }

            Health best = null;
            float bestSqr = range * range;
            foreach (var f in Faction.All)
            {
                if (f.Id == _faction) continue;
                var h = f.GetComponent<Health>();
                if (h == null || h.Dead) continue;
                float sq = (h.transform.position - transform.position).sqrMagnitude;
                if (sq <= bestSqr) { bestSqr = sq; best = h; }
            }
            if (best != null)
            {
                AttackFX.Fire(UnitType.Archer, transform.position, best.transform.position); // tower looses an arrow
                best.TakeDamage(damage); _cooldown = interval;
            }
        }
    }
}
