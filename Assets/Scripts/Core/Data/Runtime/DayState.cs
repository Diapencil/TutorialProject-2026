using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class DayState
    {
        public int customersServed;
        public int dailyRevenue;
        public int dailyIngredientCost;
        public List<int> count;
        public bool wasAttackedToday;
    }
}