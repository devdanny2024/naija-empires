using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Resource drop-off point and the faction's heart: when all of a faction's Town Centres
    /// are destroyed, that faction loses (Match handles the result).
    [RequireComponent(typeof(Faction))]
    [RequireComponent(typeof(Health))]
    public class TownCentre : MonoBehaviour
    {
        static readonly List<TownCentre> All = new();
        FactionId _faction;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var f = GetComponent<Faction>();
            _faction = f != null ? f.Id : FactionId.Player;
            Match.AddTownCentre(_faction);
            var h = GetComponent<Health>();
            if (h != null) h.Died += _ => Match.RemoveTownCentre(_faction);
        }

        public static TownCentre Nearest(FactionId faction, Vector3 p)
        {
            TownCentre best = null;
            float bestSqr = float.MaxValue;
            foreach (var tc in All)
            {
                if (tc._faction != faction) continue;
                float d = (tc.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = tc; }
            }
            return best;
        }
    }
}
