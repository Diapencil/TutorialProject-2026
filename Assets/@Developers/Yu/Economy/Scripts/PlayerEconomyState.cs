using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Economy
{
    [Serializable]
    public sealed class PlayerEconomyState
    {
        public int dayNumber = 1;
        public float money;
        public float debtRemaining = EconomyRules.StartingDebt;
        public int customersServedToday;
        public float revenueToday;
        public float tipsToday;
        public int unpaidCustomersToday;
        public float materialCostToday;
        public float repairCostToday;
        public int repairIncidentsToday;
        public float medicalCostToday;
        public int medicalIncidentsToday;
        public RepairDamageSeverity worstRepairDamageToday;
        public int fryerUpgradeLevel;
        public int grillPlateUpgradeLevel;
        public bool dayClosed;
        public List<ShopItemId> purchasedItems = new List<ShopItemId>();

        public static PlayerEconomyState CreateNewGame(float startingMoney = 0f)
        {
            var state = new PlayerEconomyState
            {
                dayNumber = 1,
                money = startingMoney,
                debtRemaining = EconomyRules.StartingDebt
            };
            state.Sanitize();
            return state;
        }

        public void Sanitize()
        {
            if (dayNumber < 1)
            {
                dayNumber = 1;
            }

            money = EconomyRules.RoundMoney(money);
            debtRemaining = Math.Max(0f, EconomyRules.RoundMoney(debtRemaining));
            materialCostToday = Math.Max(0f, EconomyRules.RoundMoney(materialCostToday));
            repairCostToday = Math.Max(0f, EconomyRules.RoundMoney(repairCostToday));
            medicalCostToday = Math.Max(0f, EconomyRules.RoundMoney(medicalCostToday));
            fryerUpgradeLevel = Math.Min(EconomyRules.MaxToolUpgradeLevel, Math.Max(0, fryerUpgradeLevel));
            grillPlateUpgradeLevel = Math.Min(EconomyRules.MaxToolUpgradeLevel, Math.Max(0, grillPlateUpgradeLevel));

            if (purchasedItems == null)
            {
                purchasedItems = new List<ShopItemId>();
            }

            for (int index = purchasedItems.Count - 1; index >= 0; index--)
            {
                if (purchasedItems.IndexOf(purchasedItems[index]) != index)
                {
                    purchasedItems.RemoveAt(index);
                }
            }
        }

        public bool HasPurchased(ShopItemId itemId)
        {
            return purchasedItems != null && purchasedItems.Contains(itemId);
        }

        public void MarkPurchased(ShopItemId itemId)
        {
            if (purchasedItems == null)
            {
                purchasedItems = new List<ShopItemId>();
            }

            if (!purchasedItems.Contains(itemId))
            {
                purchasedItems.Add(itemId);
            }
        }

        public bool CanServeMoreCustomers()
        {
            return !dayClosed && customersServedToday < EconomyRules.CustomersPerDay;
        }

        public int GetToolUpgradeLevel(ToolUpgradeType type)
        {
            return type == ToolUpgradeType.Fryer ? fryerUpgradeLevel : grillPlateUpgradeLevel;
        }

        public void SetToolUpgradeLevel(ToolUpgradeType type, int level)
        {
            if (type == ToolUpgradeType.Fryer)
            {
                fryerUpgradeLevel = level;
            }
            else
            {
                grillPlateUpgradeLevel = level;
            }

            Sanitize();
        }

        public void BeginNextDay()
        {
            if (!dayClosed)
            {
                throw new InvalidOperationException("Cannot begin the next day before closing the current day.");
            }

            dayNumber++;
            customersServedToday = 0;
            revenueToday = 0f;
            tipsToday = 0f;
            unpaidCustomersToday = 0;
            materialCostToday = 0f;
            repairCostToday = 0f;
            repairIncidentsToday = 0;
            medicalCostToday = 0f;
            medicalIncidentsToday = 0;
            worstRepairDamageToday = RepairDamageSeverity.None;
            dayClosed = false;
        }
    }
}
