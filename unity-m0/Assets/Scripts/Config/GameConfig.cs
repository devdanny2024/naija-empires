using UnityEngine;

namespace NaijaEmpires
{
    /// Tunable stats for units. All balance numbers live here.
    public static class UnitConfig
    {
        public static Cost CostOf(UnitType t) => t switch
        {
            UnitType.Villager => new Cost(50, 0, 0),
            UnitType.Spearman => new Cost(40, 0, 20),
            UnitType.Archer => new Cost(30, 20, 0),
            UnitType.Cavalry => new Cost(60, 0, 40),
            _ => new Cost(0, 0, 0),
        };

        public static int AgeRequired(UnitType t) => t switch
        {
            UnitType.Spearman => 2,
            UnitType.Archer => 2,
            UnitType.Cavalry => 3,
            _ => 1,
        };

        public static float Hp(UnitType t) => t switch
        {
            UnitType.Villager => 40f,
            UnitType.Spearman => 65f,
            UnitType.Archer => 45f,
            UnitType.Cavalry => 90f,
            _ => 40f,
        };

        public static float Damage(UnitType t) => t switch
        {
            UnitType.Spearman => 7f,
            UnitType.Archer => 6f,
            UnitType.Cavalry => 9f,
            _ => 0f,
        };

        public static float Range(UnitType t) => t == UnitType.Archer ? 6f : 1.7f;
        public static float Speed(UnitType t) => t == UnitType.Cavalry ? 6.5f : (t == UnitType.Villager ? 4f : 4.6f);

        public static Color BodyColor(FactionId f) =>
            f == FactionId.Player ? new Color(0.2f, 0.55f, 0.95f) : new Color(0.9f, 0.32f, 0.26f);

        public static Color TypeColor(UnitType t) => t switch
        {
            UnitType.Spearman => new Color(0.88f, 0.88f, 0.92f),
            UnitType.Archer => new Color(0.25f, 0.85f, 0.3f),
            UnitType.Cavalry => new Color(0.95f, 0.75f, 0.12f),
            _ => new Color(0.92f, 0.92f, 0.96f),
        };
    }

    /// Tunable stats for buildings. Benin civ gets cheaper, tougher defences (GDD §5).
    public static class BuildingConfig
    {
        public static Cost CostOf(BuildingKind k, Civ civ)
        {
            Cost c = k switch
            {
                BuildingKind.House => new Cost(0, 50, 0),
                BuildingKind.Barracks => new Cost(0, 120, 0),
                BuildingKind.Stable => new Cost(0, 120, 40),
                BuildingKind.Tower => new Cost(0, 80, 40),
                _ => new Cost(0, 0, 0),
            };
            // Benin: walls & towers 30% cheaper.
            if (civ == Civ.Benin && k == BuildingKind.Tower)
                c = new Cost(c.Yam, Mathf.RoundToInt(c.Timber * 0.7f), Mathf.RoundToInt(c.Iron * 0.7f));
            return c;
        }

        public static int AgeRequired(BuildingKind k) => k switch
        {
            BuildingKind.Barracks => 2,
            BuildingKind.Tower => 2,
            BuildingKind.Stable => 3,
            _ => 1,
        };

        public static float Hp(BuildingKind k, Civ civ)
        {
            float hp = k switch
            {
                BuildingKind.TownCentre => 600f,
                BuildingKind.House => 150f,
                BuildingKind.Barracks => 350f,
                BuildingKind.Stable => 350f,
                BuildingKind.Tower => 300f,
                _ => 100f,
            };
            // Benin: defences +50% HP.
            if (civ == Civ.Benin && k == BuildingKind.Tower) hp *= 1.5f;
            return hp;
        }

        public static int PopCapBonus(BuildingKind k) => k switch
        {
            BuildingKind.TownCentre => 5,
            BuildingKind.House => 5,
            _ => 0,
        };

        public static Color ColorOf(BuildingKind k, FactionId f)
        {
            // Slight team tint so enemy buildings read as hostile.
            Color tint = f == FactionId.Player ? new Color(1f, 1f, 1f) : new Color(1f, 0.7f, 0.65f);
            Color baseC = k switch
            {
                BuildingKind.TownCentre => new Color(0.78f, 0.62f, 0.22f),
                BuildingKind.House => new Color(0.55f, 0.4f, 0.25f),
                BuildingKind.Barracks => new Color(0.5f, 0.32f, 0.3f),
                BuildingKind.Stable => new Color(0.45f, 0.38f, 0.5f),
                BuildingKind.Tower => new Color(0.6f, 0.6f, 0.66f),
                _ => Color.gray,
            };
            return baseC * tint;
        }

        public static Vector3 Size(BuildingKind k) => k switch
        {
            BuildingKind.TownCentre => new Vector3(2.4f, 1.5f, 2.4f),
            BuildingKind.Barracks => new Vector3(2f, 1.2f, 2f),
            BuildingKind.Stable => new Vector3(2f, 1.2f, 2f),
            BuildingKind.Tower => new Vector3(1f, 2.4f, 1f),
            _ => new Vector3(1.4f, 1f, 1.4f),
        };
    }

    /// Age advancement costs (max age 3 in the MVP).
    public static class Ages
    {
        public const int Max = 3;

        public static Cost CostFor(int nextAge) => nextAge switch
        {
            2 => new Cost(150, 50, 0),
            3 => new Cost(250, 100, 50),
            _ => new Cost(99999, 99999, 99999),
        };

        public static bool TryAdvance(FactionId f)
        {
            var e = Match.Econ(f);
            if (e == null || e.Age >= Max) return false;
            if (!e.Spend(CostFor(e.Age + 1))) return false;
            e.Age++;
            e.Notify();
            return true;
        }
    }
}
