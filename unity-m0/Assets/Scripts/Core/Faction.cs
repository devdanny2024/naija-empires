using System.Collections.Generic;
using UnityEngine;

namespace NaijaEmpires
{
    /// Ownership tag on every unit and building. Also a global registry used for
    /// combat target acquisition (find enemies).
    public class Faction : MonoBehaviour
    {
        public FactionId Id = FactionId.Player;

        public static readonly List<Faction> All = new();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
