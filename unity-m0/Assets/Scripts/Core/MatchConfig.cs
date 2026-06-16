namespace NaijaEmpires
{
    /// What match to build. The menu (Match Setup) writes this; Bootstrap reads it. Defaults to a
    /// 4-empire free-for-all so the Skirmish scene still runs on its own (no menu required).
    /// Seat 0 = the local human (FactionId.Player); seats 1..N = opponents (AI in single-player).
    public static class MatchConfig
    {
        public static Civ PlayerCiv = Civ.Benin;
        public static Civ[] Opponents = { Civ.Oyo, Civ.Sokoto, Civ.KanemBornu };

        /// Fixed seat ids in play order. Index 0 is the human.
        public static readonly FactionId[] Seats =
            { FactionId.Player, FactionId.Enemy, FactionId.Faction3, FactionId.Faction4 };

        /// Total participating factions (human + opponents), capped at 4.
        public static int Count
        {
            get { int n = 1 + Opponents.Length; return n < 2 ? 2 : (n > 4 ? 4 : n); }
        }

        public static FactionId SeatId(int seat) => Seats[seat];

        public static Civ CivFor(int seat) => seat == 0 ? PlayerCiv : Opponents[(seat - 1) % Opponents.Length];

        public static bool IsAI(int seat) => seat != 0; // single-player: every non-human seat is a bot
    }
}
