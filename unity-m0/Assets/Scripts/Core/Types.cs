namespace NaijaEmpires
{
    public enum FactionId { Player, Enemy }

    public enum Civ { Benin, Oyo }

    public enum UnitType { Villager, Spearman, Archer, Cavalry }

    public enum BuildingKind { TownCentre, House, Barracks, Stable, Tower }

    /// Resource cost bundle.
    public struct Cost
    {
        public int Yam, Timber, Iron;
        public Cost(int yam, int timber, int iron) { Yam = yam; Timber = timber; Iron = iron; }
    }
}
