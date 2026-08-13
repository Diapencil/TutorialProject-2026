using System;
using System.Collections.Generic;

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
        public List<int> unlockedIngredientIds;
        public List<int> unlockedRecipeIds;
        public List<int> purchasedDecorationIds;
        public List<UpgradeState> upgrades;
        public List<int> encounteredSpecialCustomerIds;
        public int totalCustomersServed;
    }
}
