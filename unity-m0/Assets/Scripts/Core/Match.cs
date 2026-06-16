using System;
using System.Collections.Generic;

namespace NaijaEmpires
{
    /// Central match state: per-faction economies, Town-Centre counts, and the win/lose result.
    public static class Match
    {
        static readonly Dictionary<FactionId, Economy> Economies = new();
        static readonly Dictionary<FactionId, int> TownCentres = new();
        static readonly List<FactionId> Participants = new();

        public static bool Over { get; private set; }
        public static FactionId Winner { get; private set; }
        public static event Action Ended;

        public static void Reset()
        {
            Economies.Clear();
            TownCentres.Clear();
            Participants.Clear();
            Over = false;
        }

        public static void Register(FactionId id, Economy econ)
        {
            Economies[id] = econ;
            if (!Participants.Contains(id)) Participants.Add(id);
        }

        public static Economy Econ(FactionId id) =>
            Economies.TryGetValue(id, out var e) ? e : null;

        public static void AddTownCentre(FactionId id)
        {
            TownCentres.TryGetValue(id, out int n);
            TownCentres[id] = n + 1;
            if (!Participants.Contains(id)) Participants.Add(id);
        }

        public static void RemoveTownCentre(FactionId id)
        {
            if (!TownCentres.TryGetValue(id, out int n)) return;
            TownCentres[id] = n - 1;
            if (Over) return;

            // Free-for-all: a faction with no Town Centres is eliminated. The match ends when only
            // one empire remains — or immediately (defeat) the moment the human player is wiped out.
            FactionId lastAlive = id;
            int aliveCount = 0;
            foreach (var f in Participants)
            {
                if (TownCentres.TryGetValue(f, out int c) && c > 0) { aliveCount++; lastAlive = f; }
            }

            bool playerAlive = TownCentres.TryGetValue(FactionId.Player, out int pc) && pc > 0;
            if (!playerAlive || aliveCount <= 1)
            {
                Over = true;
                Winner = aliveCount >= 1 ? lastAlive : id;
                Ended?.Invoke();
            }
        }
    }
}
