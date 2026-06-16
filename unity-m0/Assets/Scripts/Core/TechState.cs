using System.Collections.Generic;

namespace NaijaEmpires
{
    /// Tracks which troop upgrades each faction has researched at a University.
    /// Stat overloads (UnitConfig.Hp/Damage with a FactionId) consult this so researched
    /// troops spawn stronger. Plain static state — reset between matches via Reset().
    public static class TechState
    {
        static readonly HashSet<(FactionId, UnitType)> _researched = new();

        public static bool IsResearched(FactionId f, UnitType t) => _researched.Contains((f, t));

        public static void MarkResearched(FactionId f, UnitType t) => _researched.Add((f, t));

        public static void Reset() => _researched.Clear();
    }
}
