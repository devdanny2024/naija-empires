using System;
using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A simple but functional opponent: keeps villagers gathering, advances ages, builds houses
    /// and a barracks, trains an army, and attacks the player's Town Centre once strong enough.
    public class EnemyAI : MonoBehaviour
    {
        public FactionId Owner = FactionId.Enemy;
        public Vector3 basePos;
        public Transform enemyTarget;       // the player's Town Centre

        public int attackThreshold = 6;

        float _think;
        const float Interval = 1.5f;
        bool _attacking;

        void Update()
        {
            if (Match.Over) return;
            _think -= Time.deltaTime;
            if (_think > 0f) return;
            _think = Interval;

            var e = Match.Econ(Owner);
            if (e == null) return;

            AssignIdleVillagers();
            ManageBuild(e);
            TrainArmy();
            MaybeAttack();
        }

        void AssignIdleVillagers()
        {
            foreach (var v in FindObjectsByType<Villager>(FindObjectsSortMode.None))
            {
                if (!Mine(v) || !v.Idle) continue;
                var n = NearestNode(v.transform.position);
                if (n != null) v.Gather(n);
            }
        }

        void ManageBuild(Economy e)
        {
            if (e.Age < Ages.Max && e.CanAfford(Ages.CostFor(e.Age + 1)))
                Ages.TryAdvance(Owner);

            if (e.PopUsed >= e.PopCap - 1 && e.CanAfford(BuildingConfig.CostOf(BuildingKind.House, e.Civ)))
                Build(BuildingKind.House, e);

            bool hasBarracks = FindMine<ProductionBuilding>(b => b.Trainable.Contains(UnitType.Spearman)) != null;
            if (e.Age >= 2 && !hasBarracks && e.CanAfford(BuildingConfig.CostOf(BuildingKind.Barracks, e.Civ)))
                Build(BuildingKind.Barracks, e);
        }

        void TrainArmy()
        {
            var barracks = FindMine<ProductionBuilding>(b => b.Trainable.Contains(UnitType.Spearman));
            if (barracks == null) return;
            barracks.Train(UnityEngine.Random.value < 0.5f ? UnitType.Spearman : UnitType.Archer);
        }

        void MaybeAttack()
        {
            var army = new List<CombatUnit>();
            foreach (var c in FindObjectsByType<CombatUnit>(FindObjectsSortMode.None))
                if (Mine(c)) army.Add(c);

            if (army.Count >= attackThreshold) _attacking = true;
            if (_attacking && enemyTarget != null)
                foreach (var c in army) c.MoveTo(enemyTarget.position);
        }

        // --- helpers ---

        bool Mine(Component c)
        {
            var f = c.GetComponent<Faction>();
            return f != null && f.Id == Owner;
        }

        ResourceNode NearestNode(Vector3 p)
        {
            ResourceNode best = null;
            float bestSqr = float.MaxValue;
            foreach (var n in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                float d = (n.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = n; }
            }
            return best;
        }

        void Build(BuildingKind k, Economy e)
        {
            if (!e.Spend(BuildingConfig.CostOf(k, e.Civ))) return;
            Vector3 pos = basePos + new Vector3(UnityEngine.Random.Range(-6f, 6f), 0f, UnityEngine.Random.Range(-6f, 6f));
            BuildingFactory.Spawn(k, pos, Owner);
        }

        T FindMine<T>(Func<T, bool> pred) where T : Component
        {
            foreach (var b in FindObjectsByType<T>(FindObjectsSortMode.None))
                if (Mine(b) && pred(b)) return b;
            return null;
        }
    }
}
