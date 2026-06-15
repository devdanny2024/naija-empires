using System;
using System.Collections.Generic;

namespace NaijaEmpires
{
    /// Central match state: per-faction economies, Town-Centre counts, and the win/lose result.
    public static class Match
    {
        static readonly Dictionary<FactionId, Economy> Economies = new();
        static readonly Dictionary<FactionId, int> TownCentres = new();

        public static bool Over { get; private set; }
        public static FactionId Winner { get; private set; }
        public static event Action Ended;

        public static void Reset()
        {
            Economies.Clear();
            TownCentres.Clear();
            Over = false;
        }

        public static void Register(FactionId id, Economy econ) => Economies[id] = econ;

        public static Economy Econ(FactionId id) =>
            Economies.TryGetValue(id, out var e) ? e : null;

        public static void AddTownCentre(FactionId id)
        {
            TownCentres.TryGetValue(id, out int n);
            TownCentres[id] = n + 1;
        }

        public static void RemoveTownCentre(FactionId id)
        {
            if (!TownCentres.TryGetValue(id, out int n)) return;
            TownCentres[id] = n - 1;
            if (n - 1 <= 0 && !Over)
            {
                Over = true;
                Winner = id == FactionId.Player ? FactionId.Enemy : FactionId.Player;
                Ended?.Invoke();
            }
        }
    }
}
