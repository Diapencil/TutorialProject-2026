using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class DayState
    {
        public int customersServed;
        public int dailyRevenue;
        public List<int> count;
        public bool wasAttackedToday;
    }
}