// 한 판(세이브 슬롯)의 모든 진행 상태. ScriptableObject가 아닌 순수 직렬화 클래스다.
using System;
using System.Collections.Generic;
using UnityEngine;

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
        public List<DecorationPlacementState> decorationPlacements = new List<DecorationPlacementState>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();
        public List<int> encounteredSpecialCustomerIds = new List<int>();
        public int totalCustomersServed = 0;
        public DayState currentDayState = DayState.CreateForDay(1);
        public List<DayState> completedDayStates = new List<DayState>();

        public void EnsureRuntimeCollections()
        {
            unlockedIngredientIds ??= new List<int>();
            unlockedRecipeIds ??= new List<int>();
            purchasedDecorationIds ??= new List<int>();
            decorationPlacements ??= new List<DecorationPlacementState>();
            upgrades ??= new List<UpgradeState>();
            encounteredSpecialCustomerIds ??= new List<int>();
            completedDayStates ??= new List<DayState>();

            GetOrCreateCurrentDayState();
        }

        public DayState GetOrCreateCurrentDayState()
        {
            if (completedDayStates == null)
            {
                completedDayStates = new List<DayState>();
            }

            if (currentDayState == null || currentDayState.dayNumber != currentDay)
            {
                currentDayState = DayState.CreateForDay(currentDay);
            }

            currentDayState.EnsureInitialized(currentDay);
            return currentDayState;
        }

        public void CompleteCurrentDay()
        {
            DayState dayState = GetOrCreateCurrentDayState();
            dayState.MarkComplete();

            if (!HasCompletedDay(dayState.dayNumber))
            {
                completedDayStates.Add(dayState);
            }
        }

        public void BeginNextDay()
        {
            CompleteCurrentDay();
            currentDay++;
            currentDayState = DayState.CreateForDay(currentDay);
        }

        public bool IsIngredientUnlocked(int id)
        {
            EnsureRuntimeCollections();
            return unlockedIngredientIds != null && unlockedIngredientIds.Contains(id);
        }

        public void UnlockIngredient(int id)
        {
            EnsureRuntimeCollections();

            if (unlockedIngredientIds == null)
            {
                unlockedIngredientIds = new List<int>();
            }

            if (!unlockedIngredientIds.Contains(id))
            {
                unlockedIngredientIds.Add(id);
            }
        }

        public bool IsRecipeUnlocked(int id)
        {
            EnsureRuntimeCollections();
            return unlockedRecipeIds != null && unlockedRecipeIds.Contains(id);
        }

        /// <summary>레시피를 도감에 해금한다. 이번에 새로 해금됐으면 true, 이미 있었으면 false.</summary>
        public bool UnlockRecipe(int id)
        {
            EnsureRuntimeCollections();

            if (unlockedRecipeIds == null)
            {
                unlockedRecipeIds = new List<int>();
            }

            if (unlockedRecipeIds.Contains(id))
            {
                return false;
            }

            unlockedRecipeIds.Add(id);
            return true;
        }

        public bool IsDecorationPurchased(int id)
        {
            EnsureRuntimeCollections();
            return purchasedDecorationIds != null && purchasedDecorationIds.Contains(id);
        }

        public void PurchaseDecoration(int id)
        {
            EnsureRuntimeCollections();

            if (purchasedDecorationIds == null)
            {
                purchasedDecorationIds = new List<int>();
            }

            if (!purchasedDecorationIds.Contains(id))
            {
                purchasedDecorationIds.Add(id);
            }
        }

        public bool TryGetDecorationPosition(int id, out Vector2 position)
        {
            EnsureRuntimeCollections();

            if (decorationPlacements != null)
            {
                for (int i = 0; i < decorationPlacements.Count; i++)
                {
                    DecorationPlacementState placement = decorationPlacements[i];
                    if (placement != null && placement.id == id)
                    {
                        position = placement.position;
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        public void SetDecorationPosition(int id, Vector2 position)
        {
            EnsureRuntimeCollections();

            if (decorationPlacements == null)
            {
                decorationPlacements = new List<DecorationPlacementState>();
            }

            for (int i = 0; i < decorationPlacements.Count; i++)
            {
                DecorationPlacementState placement = decorationPlacements[i];
                if (placement != null && placement.id == id)
                {
                    placement.position = position;
                    return;
                }
            }

            decorationPlacements.Add(new DecorationPlacementState
            {
                id = id,
                position = position
            });
        }

        /// <summary>해당 업그레이드의 현재 레벨. 기록이 없으면 0.</summary>
        public int GetUpgradeLevel(int upgradeId)
        {
            EnsureRuntimeCollections();

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
            EnsureRuntimeCollections();

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

        private bool HasCompletedDay(int dayNumber)
        {
            if (completedDayStates == null)
            {
                return false;
            }

            for (int i = 0; i < completedDayStates.Count; i++)
            {
                DayState dayState = completedDayStates[i];

                if (dayState != null && dayState.dayNumber == dayNumber)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class DecorationPlacementState
    {
        public int id;
        public Vector2 position;
    }
}
