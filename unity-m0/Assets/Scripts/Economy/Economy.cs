using System;

namespace NaijaEmpires
{
    /// One faction's economy: resources, population, age, and civ. Plain C# (held by Match).
    public class Economy
    {
        public int Yam, Timber, Iron, Cowries, Knowledge, Oil;
        public int PopUsed;
        public int PopCap;     // supplied by Town Centres + Houses
        public int Age = 1;
        public Civ Civ;

        // Trade Limit: max Cowries that trade (Caravans) may bank per second. Raised by Markets.
        public int TradeLimit = 8;

        public event Action Changed;

        public Economy(int yam, int timber, int iron, Civ civ)
        {
            Yam = yam; Timber = timber; Iron = iron; Civ = civ;
            Cowries = 0; Knowledge = 0;
        }

        public bool CanAfford(Cost c) =>
            GameDebug.TestMode ||
            (Yam >= c.Yam && Timber >= c.Timber && Iron >= c.Iron && Cowries >= c.Cowries && Knowledge >= c.Knowledge && Oil >= c.Oil);

        public bool Spend(Cost c)
        {
            if (GameDebug.TestMode) { Changed?.Invoke(); return true; } // test mode: free, no deduction
            if (!CanAfford(c)) return false;
            Yam -= c.Yam; Timber -= c.Timber; Iron -= c.Iron; Cowries -= c.Cowries; Knowledge -= c.Knowledge; Oil -= c.Oil;
            Changed?.Invoke();
            return true;
        }

        public void Add(ResourceType r, int amount)
        {
            switch (r)
            {
                case ResourceType.Yam: Yam += amount; break;
                case ResourceType.Timber: Timber += amount; break;
                case ResourceType.Iron: Iron += amount; break;
                case ResourceType.Cowries: Cowries += amount; break;
                case ResourceType.Knowledge: Knowledge += amount; break;
                case ResourceType.Oil: Oil += amount; break;
            }
            Changed?.Invoke();
        }

        // Trade income (Caravans) is capped at TradeLimit per second. Returns how much was actually banked.
        int _tradeSec = -1, _tradeUsed;
        public int AddTrade(int want)
        {
            int sec = (int)UnityEngine.Time.time;
            if (sec != _tradeSec) { _tradeSec = sec; _tradeUsed = 0; }
            int give = Math.Min(want, Math.Max(0, TradeLimit - _tradeUsed));
            if (give > 0) { _tradeUsed += give; Cowries += give; Changed?.Invoke(); }
            return give;
        }

        public bool HasPop(int n) => GameDebug.TestMode || PopUsed + n <= PopCap;
        public void AddPop(int n) { PopUsed += n; Changed?.Invoke(); }
        public void AddCap(int n) { PopCap += n; Changed?.Invoke(); }
        public void AddTradeLimit(int n) { TradeLimit += n; Changed?.Invoke(); }
        public void Notify() => Changed?.Invoke();
    }
}
