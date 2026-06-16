namespace NaijaEmpires
{
    // Up to 4 factions for free-for-all. Player = the local human seat; the rest are opponents
    // (human in multiplayer, AI bots in single-player / empty MP seats).
    public enum FactionId { Player, Enemy, Faction3, Faction4 }

    public enum Civ { Benin, Oyo, Sokoto, KanemBornu }

    public enum UnitType { Villager, Spearman, Archer, Cavalry }

    public enum BuildingKind { TownCentre, House, Barracks, Stable, Tower, Wall, Farm, University }

    /// Resource cost bundle.
    public struct Cost
    {
        public int Yam, Timber, Iron;
        public Cost(int yam, int timber, int iron) { Yam = yam; Timber = timber; Iron = iron; }
    }
}
