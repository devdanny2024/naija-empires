namespace NaijaEmpires
{
    // Up to 4 factions for free-for-all. Player = the local human seat; the rest are opponents
    // (human in multiplayer, AI bots in single-player / empty MP seats).
    public enum FactionId { Player, Enemy, Faction3, Faction4 }

    public enum Civ { Benin, Oyo, Sokoto, KanemBornu }

    public enum UnitType { Villager, Spearman, Archer, Cavalry, Scholar, Caravan }

    public enum BuildingKind { TownCentre, House, Barracks, Stable, Tower, Wall, Farm, University, Market }

    /// Resource cost bundle (5 resources). The 3-arg constructor keeps Cowries/Knowledge at 0 so all
    /// existing costs stay valid; use the 5-arg constructor when a cost involves trade-wealth/research.
    public struct Cost
    {
        public int Yam, Timber, Iron, Cowries, Knowledge;
        public Cost(int yam, int timber, int iron) { Yam = yam; Timber = timber; Iron = iron; Cowries = 0; Knowledge = 0; }
        public Cost(int yam, int timber, int iron, int cowries, int knowledge)
        { Yam = yam; Timber = timber; Iron = iron; Cowries = cowries; Knowledge = knowledge; }
    }
}
