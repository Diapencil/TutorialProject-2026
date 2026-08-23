// 한 판(세이브 슬롯)의 모든 진행 상태. ScriptableObject가 아닌 순수 직렬화 클래스다.
using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class GameState
    {
        public int currentDay = 1;
        public int gold = 0;

        /// <summary>실제 금액의 10배로 저장된 잔여 부채. 15000 == 1500.0C</summary>
        public int debtRemaining = 15000;

        public int debtDeadline = 30;
        public ShopCondition shopCondition = ShopCondition.Normal;
        public int chapterNumber;

        public List<int> unlockedIngredientIds = new List<int>();
        public List<int> unlockedRecipeIds = new List<int>();
        public List<int> purchasedDecorationIds = new List<int>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();
        public List<int> encounteredSpecialCustomerIds = new List<int>();
        public int totalCustomersServed = 0;

        public bool IsIngredientUnlocked(int id)
        {
            return unlockedIngredientIds != null && unlockedIngredientIds.Contains(id);
        }

        public void UnlockIngredient(int id)
        {
            if (unlockedIngredientIds == null)
            {
                unlockedIngredientIds = new List<int>();
            }

            if (!unlockedIngredientIds.Contains(id))
            {
                unlockedIngredientIds.Add(id);
            }
        }

        public bool IsDecorationPurchased(int id)
        {
            return purchasedDecorationIds != null && purchasedDecorationIds.Contains(id);
        }

        public void PurchaseDecoration(int id)
        {
            if (purchasedDecorationIds == null)
            {
                purchasedDecorationIds = new List<int>();
            }

            if (!purchasedDecorationIds.Contains(id))
            {
                purchasedDecorationIds.Add(id);
            }
        }

        /// <summary>해당 업그레이드의 현재 레벨. 기록이 없으면 0.</summary>
        public int GetUpgradeLevel(int upgradeId)
        {
            if (upgrades == null)
            {
                return 0;
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i].id == upgradeId)
                {
                    return upgrades[i].currentLevel;
                }
            }

            return 0;
        }

        public void SetUpgradeLevel(int upgradeId, int level)
        {
            if (upgrades == null)
            {
                upgrades = new List<UpgradeState>();
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i].id == upgradeId)
                {
                    upgrades[i].currentLevel = level;
                    return;
                }
            }

            upgrades.Add(new UpgradeState { id = upgradeId, currentLevel = level });
        }
    }
}
