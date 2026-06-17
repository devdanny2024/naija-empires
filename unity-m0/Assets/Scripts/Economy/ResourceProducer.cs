using UnityEngine;

namespace NaijaEmpires
{
    /// A unit that generates a resource for its owner while it lives (Scholar → Knowledge, Caravan →
    /// Cowries). Trade income is routed through Economy.AddTrade so it respects the Trade Limit. This is
    /// villager-economy-style (a unit you train and keep alive), NOT a building auto-producing.
    public class ResourceProducer : MonoBehaviour
    {
        public ResourceType type = ResourceType.Knowledge;
        public float perSecond = 1f;
        public bool tradeCapped = false; // Cowries/Caravans go through the Trade Limit

        FactionId _faction;
        float _accum;

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
        }

        void Update()
        {
            var e = Match.Econ(_faction);
            if (e == null) return;
            _accum += perSecond * Time.deltaTime;
            if (_accum < 1f) return;
            int whole = Mathf.FloorToInt(_accum);
            _accum -= whole;
            if (tradeCapped) e.AddTrade(whole);
            else e.Add(type, whole);
        }
    }
}
