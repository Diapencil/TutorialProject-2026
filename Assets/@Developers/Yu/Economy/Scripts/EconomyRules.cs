using System;
using SheepSheepBurger.BurgerAssembly;

namespace SheepSheepBurger.Economy
{
    public static class EconomyRules
    {
        public const int DebtDeadlineDays = 30;
        public const float StartingDebt = 1500f;
        public const int CustomersPerDay = 8;
        public const float MinBurgerPrice = 5f;
        public const float MaxBurgerPrice = 15f;
        public const float BasicBurgerPrice = 5f;
        public const float ExcellentTip = 5f;
        public const float GoodTip = 3f;
        public const float CookedIngredientCost = 0.3f;
        public const float FreshIngredientCost = 0.2f;
        public const float DefaultShopItemPrice = 500f;
        public const float FullStoreRepairCost = 500f;
        public const float MedicalCareCost = 100f;
        public const float DecorationMinCost = 100f;
        public const float DecorationMaxCost = 500f;
        public const int DefaultCustomerDamageChancePercent = 10;
        public const int MaxToolUpgradeLevel = 4;

        public static CustomerSatisfaction GetSatisfaction(int mismatchCount)
        {
            if (mismatchCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mismatchCount));
            }

            if (mismatchCount == 0)
            {
                return CustomerSatisfaction.Excellent;
            }

            if (mismatchCount == 1)
            {
                return CustomerSatisfaction.Good;
            }

            if (mismatchCount == 2)
            {
                return CustomerSatisfaction.Normal;
            }

            return CustomerSatisfaction.Terrible;
        }

        public static OrderEvaluation CreatePayout(int mismatchCount, float recipePrice, float ingredientCost)
        {
            CustomerSatisfaction satisfaction = GetSatisfaction(mismatchCount);
            float basePayment = satisfaction == CustomerSatisfaction.Terrible ? 0f : ClampBurgerPrice(recipePrice);
            float tip = 0f;
            if (satisfaction == CustomerSatisfaction.Excellent)
            {
                tip = ExcellentTip;
            }
            else if (satisfaction == CustomerSatisfaction.Good)
            {
                tip = GoodTip;
            }

            return new OrderEvaluation
            {
                mismatchCount = mismatchCount,
                satisfaction = satisfaction,
                basePayment = basePayment,
                tip = tip,
                ingredientCost = RoundMoney(ingredientCost),
                totalPayment = RoundMoney(basePayment + tip),
                customerPays = satisfaction != CustomerSatisfaction.Terrible
            };
        }

        public static float CalculateRecipePrice(int ingredientCount)
        {
            float price = BasicBurgerPrice + Math.Max(0, ingredientCount - 7);
            return ClampBurgerPrice(price);
        }

        public static float ClampBurgerPrice(float price)
        {
            return Math.Min(MaxBurgerPrice, Math.Max(MinBurgerPrice, price));
        }

        public static float GetIngredientCost(IngredientType ingredient)
        {
            return IsCookedIngredient(ingredient) ? CookedIngredientCost : FreshIngredientCost;
        }

        public static bool IsCookedIngredient(IngredientType ingredient)
        {
            switch (ingredient)
            {
                case IngredientType.Patty:
                case IngredientType.PattyRaw:
                case IngredientType.PattyBurnt:
                case IngredientType.ToppingBacon:
                case IngredientType.ToppingFriedEgg:
                    return true;
                default:
                    return false;
            }
        }

        public static float GetRepairCost(RepairDamageSeverity severity)
        {
            switch (severity)
            {
                case RepairDamageSeverity.None:
                    return 0f;
                case RepairDamageSeverity.Minor:
                    return 125f;
                case RepairDamageSeverity.Moderate:
                    return 250f;
                case RepairDamageSeverity.Major:
                    return 375f;
                case RepairDamageSeverity.Severe:
                    return FullStoreRepairCost;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity));
            }
        }

        public static string GetRepairDescription(RepairDamageSeverity severity)
        {
            switch (severity)
            {
                case RepairDamageSeverity.None:
                    return "No damage";
                case RepairDamageSeverity.Minor:
                    return "Minor shop damage";
                case RepairDamageSeverity.Moderate:
                    return "Moderate shop damage";
                case RepairDamageSeverity.Major:
                    return "Major shop damage";
                case RepairDamageSeverity.Severe:
                    return "Full store repair";
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity));
            }
        }

        public static float GetToolUpgradeCost(ToolUpgradeType type, int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= MaxToolUpgradeLevel)
            {
                return 0f;
            }

            return 150f + (currentLevel * 100f);
        }

        public static float GetFryerSpeedMultiplier(int level)
        {
            return 1f + (Math.Min(MaxToolUpgradeLevel, Math.Max(0, level)) * 0.15f);
        }

        public static float GetGrillSpeedMultiplier(int level)
        {
            return 1f + (Math.Min(MaxToolUpgradeLevel, Math.Max(0, level)) * 0.1f);
        }

        public static float GetGrillBurnChance(int level)
        {
            switch (Math.Min(MaxToolUpgradeLevel, Math.Max(0, level)))
            {
                case 0: return 0.3f;
                case 1: return 0.2f;
                case 2: return 0.1f;
                default: return 0f;
            }
        }

        public static float RoundMoney(float value)
        {
            return (float)Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }

        public static DebtStatus GetDebtStatus(PlayerEconomyState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.debtRemaining <= 0f)
            {
                return DebtStatus.Cleared;
            }

            if (state.dayNumber > DebtDeadlineDays)
            {
                return DebtStatus.Failed;
            }

            return state.dayNumber == DebtDeadlineDays ? DebtStatus.DueToday : DebtStatus.InProgress;
        }
    }
}
