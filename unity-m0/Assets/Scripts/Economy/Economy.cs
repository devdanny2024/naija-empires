using System;

namespace NaijaEmpires
{
    /// One faction's economy: resources, population, age, and civ. Plain C# (held by Match).
    public class Economy
    {
        public int Yam, Timber, Iron;
        public int PopUsed;
        public int PopCap;     // supplied by Town Centres + Houses
        public int Age = 1;
        public Civ Civ;

        public event Action Changed;

        public Economy(int yam, int timber, int iron, Civ civ)
        {
            Yam = yam; Timber = timber; Iron = iron; Civ = civ;
        }

        public bool CanAfford(Cost c) => Yam >= c.Yam && Timber >= c.Timber && Iron >= c.Iron;

        public bool Spend(Cost c)
        {
            if (!CanAfford(c)) return false;
            Yam -= c.Yam; Timber -= c.Timber; Iron -= c.Iron;
            Changed?.Invoke();
            return true;
        }

        public void Add(ResourceType r, int amount)
        {
            switch (r)
            {
                case ResourceType.Yam: Yam += amount; break;
                case ResourceType.Timber: Timber += amount; break;
                default: Iron += amount; break;
            }
            Changed?.Invoke();
        }

        public bool HasPop(int n) => PopUsed + n <= PopCap;
        public void AddPop(int n) { PopUsed += n; Changed?.Invoke(); }
        public void AddCap(int n) { PopCap += n; Changed?.Invoke(); }
        public void Notify() => Changed?.Invoke();
    }
}
