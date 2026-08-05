using System;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class GameState
    {
        public int currentDay;
        public int gold;
        public int debtRemaining;
        public int debtDeadline;
        public ShopCondition shopCondition;
        public int chapterNumber;
    }
}
