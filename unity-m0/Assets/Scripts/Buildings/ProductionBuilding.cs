using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Trains units from a queue. Used by Town Centre (villagers), Barracks (spear/archer),
    /// and Stable (cavalry). Costs resources + population + train time, gated by Age.
    public class ProductionBuilding : MonoBehaviour
    {
        public readonly List<UnitType> Trainable = new();
        public float TrainTime = 3f;
        public Vector3 RallyOffset = new Vector3(2.5f, 0f, -2.5f);

        FactionId _faction;
        readonly Queue<UnitType> _queue = new();
        float _timer;

        public int QueueCount => _queue.Count;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
        }

        public bool CanTrain(UnitType t)
        {
            var e = Match.Econ(_faction);
            return e != null && Trainable.Contains(t) && e.Age >= UnitConfig.AgeRequired(t);
        }

        public bool Train(UnitType t)
        {
            var e = Match.Econ(_faction);
            if (e == null || !CanTrain(t) || !e.HasPop(1)) return false;
            if (!e.Spend(UnitConfig.CostOf(t))) return false;
            e.AddPop(1);                 // reserve population immediately
            _queue.Enqueue(t);
            return true;
        }

        void Update()
        {
            if (_queue.Count == 0) return;
            _timer += Time.deltaTime;
            if (_timer >= TrainTime)
            {
                _timer = 0f;
                UnitFactory.Spawn(_queue.Dequeue(), transform.position + RallyOffset, _faction);
            }
        }
    }

    /// Adds population capacity while alive; removes it on death.
    public class PopCapProvider : MonoBehaviour
    {
        public int amount = 5;
        FactionId _faction;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
            var e = Match.Econ(_faction);
            if (e != null) e.AddCap(amount);

            var h = GetComponent<Health>();
            if (h != null) h.Died += _ => { var ee = Match.Econ(_faction); if (ee != null) ee.AddCap(-amount); };
        }
    }
}
