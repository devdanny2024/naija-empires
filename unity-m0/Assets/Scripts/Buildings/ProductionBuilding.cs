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
            if (!enabled) return false; // still a construction site — can't train yet
            var e = Match.Econ(_faction);
            return e != null && Trainable.Contains(t) && e.Age >= UnitConfig.AgeRequired(t);
        }

        public bool Train(UnitType t)
        {
            if (!enabled) return false; // under construction
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
                var type = _queue.Dequeue();

                // Scholars aren't spawned as walking units — they're added to the University's pool (a
                // count that produces Knowledge). Click the University to see how many scholars it holds.
                if (type == UnitType.Scholar)
                {
                    var pool = GetComponent<ScholarPool>();
                    if (pool != null) { pool.Add(1); return; }
                }

                // Scatter a little so trained units don't stack on one spot — easier to pick each one.
                Vector3 spread = new Vector3(Random.Range(-1.6f, 1.6f), 0f, Random.Range(-1.6f, 1.6f));
                UnitFactory.Spawn(type, transform.position + RallyOffset + spread, _faction);
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

    /// Holds the University's scholars as a COUNT (not spawned units) and produces Knowledge for the
    /// owner: count × KnowledgePerScholar per second. The HUD shows the count when you select the
    /// University. Scholars persist with the building (lost if it's destroyed).
    public class ScholarPool : MonoBehaviour
    {
        public int Count { get; private set; }
        FactionId _faction;
        float _accum;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
        }

        public void Add(int n) { if (n > 0) Count += n; }

        void Update()
        {
            if (Count <= 0) return;
            var e = Match.Econ(_faction);
            if (e == null) return;
            _accum += Count * UnitConfig.KnowledgePerScholar * Time.deltaTime;
            if (_accum < 1f) return;
            int whole = Mathf.FloorToInt(_accum);
            _accum -= whole;
            e.Add(ResourceType.Knowledge, whole);
        }
    }

    /// Raises the owner's Trade Limit while alive (Markets); removes it on death.
    public class TradeLimitProvider : MonoBehaviour
    {
        public int amount = 6;
        FactionId _faction;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
            var e = Match.Econ(_faction);
            if (e != null) e.AddTradeLimit(amount);

            var h = GetComponent<Health>();
            if (h != null) h.Died += _ => { var ee = Match.Econ(_faction); if (ee != null) ee.AddTradeLimit(-amount); };
        }
    }
}
