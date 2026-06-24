using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// A rare surface oil seep scattered across the map. Oil Pumps may ONLY be built on a well
    /// (BuildPlacer checks proximity), and wells are plotted on the minimap so the player can find
    /// them by scouting. Registers itself in a static list for cheap lookups.
    public class OilWell : MonoBehaviour
    {
        public const float BuildRadius = 4.5f; // an Oil Pump must be placed within this of a well

        public static readonly List<OilWell> All = new();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// Nearest well to p within maxDist, or null. Used to gate Oil Pump placement.
        public static OilWell Nearest(Vector3 p, float maxDist)
        {
            OilWell best = null;
            float bestSqr = maxDist * maxDist;
            foreach (var w in All)
            {
                if (w == null) continue;
                float d = (w.transform.position - p).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = w; }
            }
            return best;
        }
    }
}
