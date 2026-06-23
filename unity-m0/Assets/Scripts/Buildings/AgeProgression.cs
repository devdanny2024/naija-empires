using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Watches every empire's age and, the moment one advances, visibly upgrades that empire's standing
    /// buildings to the new age's tier (grander model + more HP). Makes "advance age" a visible event,
    /// not just a number ticking up. Added to the scene by Bootstrap.BuildManagers.
    public class AgeProgression : MonoBehaviour
    {
        static readonly FactionId[] Factions =
            { FactionId.Player, FactionId.Enemy, FactionId.Faction3, FactionId.Faction4 };

        readonly Dictionary<FactionId, int> _seen = new();

        void Update()
        {
            foreach (var id in Factions)
            {
                var e = Match.Econ(id);
                if (e == null) continue;
                if (!_seen.TryGetValue(id, out int last)) { _seen[id] = e.Age; continue; }
                if (e.Age > last)
                {
                    _seen[id] = e.Age;
                    UpgradeBuildings(id, e.Age);
                }
            }
        }

        static void UpgradeBuildings(FactionId id, int age)
        {
            foreach (var up in FindObjectsByType<Upgradeable>(FindObjectsSortMode.None))
            {
                var f = up.GetComponent<Faction>();
                if (f != null && f.Id == id) { up.SetAgeTier(age); up.RefreshModern(age); }
            }
        }
    }
}
