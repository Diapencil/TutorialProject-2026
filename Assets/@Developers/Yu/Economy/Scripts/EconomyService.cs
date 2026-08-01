using System;
using System.Collections.Generic;
using SheepSheepBurger.BurgerAssembly;

namespace SheepSheepBurger.Economy
{
    public sealed class EconomyService
    {
        private readonly ShopCatalog shopCatalog;
        private readonly RecipeCatalog recipeCatalog;

        public EconomyService(ShopCatalog shopCatalog = null, RecipeCatalog recipeCatalog = null)
        {
            this.shopCatalog = shopCatalog ?? ShopCatalog.CreateDefault();
            this.recipeCatalog = recipeCatalog ?? RecipeCatalog.CreateDefault();
        }

        public OrderEvaluation EvaluateBurger(BurgerRecipeId recipeId, BurgerData burger)
        {
            return EvaluateBurger(recipeCatalog.GetRecipe(recipeId), burger);
        }

        public OrderEvaluation EvaluateBurger(RecipeData recipe, BurgerData burger)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            int mismatchCount = CountIngredientMismatches(recipe.requiredIngredients, burger);
            float ingredientCost = CalculateBurgerIngredientCost(burger);
            return EconomyRules.CreatePayout(mismatchCount, recipe.price, ingredientCost);
        }

        public bool TryRecordServedOrder(PlayerEconomyState state, OrderEvaluation evaluation)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (evaluation == null)
            {
                throw new ArgumentNullException(nameof(evaluation));
            }

            state.Sanitize();
            if (!state.CanServeMoreCustomers())
            {
                return false;
            }

            state.customersServedToday++;
            state.money = EconomyRules.RoundMoney(state.money + evaluation.totalPayment);
            state.revenueToday = EconomyRules.RoundMoney(state.revenueToday + evaluation.totalPayment);
            state.tipsToday = EconomyRules.RoundMoney(state.tipsToday + evaluation.tip);
            state.materialCostToday = EconomyRules.RoundMoney(state.materialCostToday + evaluation.ingredientCost);
            if (!evaluation.customerPays)
            {
                state.unpaidCustomersToday++;
            }

            return true;
        }

        public RepairDamageEvent RecordRepairDamage(
            PlayerEconomyState state,
            RepairDamageSeverity severity,
            string description = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Sanitize();
            float cost = EconomyRules.GetRepairCost(severity);
            var damageEvent = new RepairDamageEvent(
                severity,
                cost,
                string.IsNullOrEmpty(description) ? EconomyRules.GetRepairDescription(severity) : description);

            if (severity == RepairDamageSeverity.None || cost <= 0f)
            {
                return damageEvent;
            }

            state.repairCostToday = EconomyRules.RoundMoney(state.repairCostToday + cost);
            state.repairIncidentsToday++;
            if (severity > state.worstRepairDamageToday)
            {
                state.worstRepairDamageToday = severity;
            }

            return damageEvent;
        }

        public RepairDamageEvent RollAndRecordCustomerDamage(
            PlayerEconomyState state,
            System.Random random,
            int chancePercent = EconomyRules.DefaultCustomerDamageChancePercent)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (chancePercent < 0 || chancePercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(chancePercent));
            }

            if (random.Next(0, 100) >= chancePercent)
            {
                return RecordRepairDamage(state, RepairDamageSeverity.None);
            }

            int roll = random.Next(0, 100);
            RepairDamageSeverity severity;
            if (roll < 55)
            {
                severity = RepairDamageSeverity.Minor;
            }
            else if (roll < 82)
            {
                severity = RepairDamageSeverity.Moderate;
            }
            else if (roll < 96)
            {
                severity = RepairDamageSeverity.Major;
            }
            else
            {
                severity = RepairDamageSeverity.Severe;
            }

            return RecordRepairDamage(state, severity);
        }

        public MedicalBillEvent RecordMedicalBill(PlayerEconomyState state, string description = "Treatment fee")
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Sanitize();
            state.medicalCostToday = EconomyRules.RoundMoney(state.medicalCostToday + EconomyRules.MedicalCareCost);
            state.medicalIncidentsToday++;
            return new MedicalBillEvent(EconomyRules.MedicalCareCost, description);
        }

        public DaySettlement CloseDay(PlayerEconomyState state, bool payDebtAutomaticallyAtDeadline = true)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Sanitize();
            float materialCost = state.materialCostToday;
            float repairCost = state.repairCostToday;
            float medicalCost = state.medicalCostToday;
            state.money = EconomyRules.RoundMoney(state.money - materialCost - repairCost - medicalCost);

            float debtPaid = 0f;
            if (payDebtAutomaticallyAtDeadline &&
                state.dayNumber >= EconomyRules.DebtDeadlineDays &&
                state.debtRemaining > 0f &&
                state.money >= state.debtRemaining)
            {
                debtPaid = state.debtRemaining;
                state.money = EconomyRules.RoundMoney(state.money - state.debtRemaining);
                state.debtRemaining = 0f;
            }

            state.dayClosed = true;
            DebtStatus debtStatus = EconomyRules.GetDebtStatus(state);
            bool canContinue = debtStatus != DebtStatus.Failed;
            return new DaySettlement
            {
                dayNumber = state.dayNumber,
                customersServed = state.customersServedToday,
                revenueEarned = state.revenueToday,
                tipsEarned = state.tipsToday,
                unpaidCustomers = state.unpaidCustomersToday,
                materialCost = materialCost,
                repairCost = repairCost,
                repairIncidents = state.repairIncidentsToday,
                medicalCost = medicalCost,
                medicalIncidents = state.medicalIncidentsToday,
                debtPaid = debtPaid,
                moneyAfterSettlement = state.money,
                debtRemaining = state.debtRemaining,
                debtStatus = debtStatus,
                canContinue = canContinue
            };
        }

        public bool TryPayDebt(PlayerEconomyState state, float amount, out float paid)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            state.Sanitize();
            paid = EconomyRules.RoundMoney(Math.Min(amount, Math.Min(state.money, state.debtRemaining)));
            if (paid <= 0f)
            {
                return false;
            }

            state.money = EconomyRules.RoundMoney(state.money - paid);
            state.debtRemaining = EconomyRules.RoundMoney(state.debtRemaining - paid);
            return true;
        }

        public ShopPurchaseResult TryBuyItem(PlayerEconomyState state, ShopItemId itemId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Sanitize();
            if (!shopCatalog.TryGetItem(itemId, out ShopItemData item))
            {
                return ShopPurchaseResult.Failed(itemId, "Shop item not found.", state.money);
            }

            if (item.unlocksIngredient && state.HasPurchased(itemId))
            {
                return ShopPurchaseResult.Failed(itemId, "Already unlocked.", state.money);
            }

            if (item.category == ShopCategory.Decoration && state.debtRemaining > 0f)
            {
                return ShopPurchaseResult.Failed(itemId, "Decorations unlock after the debt is cleared.", state.money);
            }

            if (state.money < item.price)
            {
                return ShopPurchaseResult.Failed(itemId, "Not enough money.", state.money);
            }

            state.money = EconomyRules.RoundMoney(state.money - item.price);
            if (item.unlocksIngredient)
            {
                state.MarkPurchased(itemId);
            }

            return ShopPurchaseResult.Succeeded(item, state.money);
        }

        public bool TryBuyToolUpgrade(PlayerEconomyState state, ToolUpgradeType type, out ToolUpgradeData result)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Sanitize();
            int currentLevel = state.GetToolUpgradeLevel(type);
            float cost = EconomyRules.GetToolUpgradeCost(type, currentLevel);
            if (currentLevel >= EconomyRules.MaxToolUpgradeLevel || state.money < cost)
            {
                result = CreateToolUpgradeData(state, type);
                return false;
            }

            state.money = EconomyRules.RoundMoney(state.money - cost);
            state.SetToolUpgradeLevel(type, currentLevel + 1);
            result = CreateToolUpgradeData(state, type);
            return true;
        }

        public ToolUpgradeData CreateToolUpgradeData(PlayerEconomyState state, ToolUpgradeType type)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int level = state.GetToolUpgradeLevel(type);
            return new ToolUpgradeData
            {
                type = type,
                level = level,
                maxLevel = EconomyRules.MaxToolUpgradeLevel,
                nextCost = EconomyRules.GetToolUpgradeCost(type, level),
                speedMultiplier = type == ToolUpgradeType.Fryer
                    ? EconomyRules.GetFryerSpeedMultiplier(level)
                    : EconomyRules.GetGrillSpeedMultiplier(level),
                burnChance = type == ToolUpgradeType.GrillPlate
                    ? EconomyRules.GetGrillBurnChance(level)
                    : 0f
            };
        }

        public float CalculateBurgerIngredientCost(BurgerData burger)
        {
            float total = 0f;
            Dictionary<IngredientType, int> counts = BuildCostCounts(burger);
            foreach (KeyValuePair<IngredientType, int> pair in counts)
            {
                total += EconomyRules.GetIngredientCost(pair.Key) * pair.Value;
            }

            return EconomyRules.RoundMoney(total);
        }

        public int CountIngredientMismatches(IEnumerable<IngredientType> requiredIngredients, BurgerData burger)
        {
            Dictionary<IngredientType, int> required = BuildCounts(requiredIngredients);
            Dictionary<IngredientType, int> served = BuildCounts(burger);
            int mismatches = 0;

            foreach (KeyValuePair<IngredientType, int> pair in required)
            {
                served.TryGetValue(pair.Key, out int servedCount);
                mismatches += Math.Abs(pair.Value - servedCount);
            }

            foreach (KeyValuePair<IngredientType, int> pair in served)
            {
                if (!required.ContainsKey(pair.Key))
                {
                    mismatches += pair.Value;
                }
            }

            return mismatches;
        }

        private static Dictionary<IngredientType, int> BuildCounts(IEnumerable<IngredientType> ingredients)
        {
            var counts = new Dictionary<IngredientType, int>();
            if (ingredients == null)
            {
                return counts;
            }

            foreach (IngredientType ingredient in ingredients)
            {
                AddCount(counts, NormalizeIngredient(ingredient));
            }

            return counts;
        }

        private static Dictionary<IngredientType, int> BuildCounts(BurgerData burger)
        {
            var counts = new Dictionary<IngredientType, int>();
            if (burger == null || burger.ingredients == null)
            {
                return counts;
            }

            var sauceTypesAlreadyCounted = new HashSet<IngredientType>();
            for (int index = 0; index < burger.ingredients.Count; index++)
            {
                IngredientPlacement placement = burger.ingredients[index];
                if (placement == null)
                {
                    continue;
                }

                IngredientType normalized = NormalizeIngredient(placement.type);
                if (IsSauce(normalized))
                {
                    if (sauceTypesAlreadyCounted.Contains(normalized))
                    {
                        continue;
                    }

                    sauceTypesAlreadyCounted.Add(normalized);
                }

                AddCount(counts, normalized);
            }

            return counts;
        }

        private static Dictionary<IngredientType, int> BuildCostCounts(BurgerData burger)
        {
            var counts = new Dictionary<IngredientType, int>();
            if (burger == null || burger.ingredients == null)
            {
                return counts;
            }

            var sauceTypesAlreadyCounted = new HashSet<IngredientType>();
            for (int index = 0; index < burger.ingredients.Count; index++)
            {
                IngredientPlacement placement = burger.ingredients[index];
                if (placement == null)
                {
                    continue;
                }

                IngredientType normalized = NormalizeIngredient(placement.type);
                if (IsSauce(normalized))
                {
                    if (sauceTypesAlreadyCounted.Contains(normalized))
                    {
                        continue;
                    }

                    sauceTypesAlreadyCounted.Add(normalized);
                }

                AddCount(counts, normalized);
            }

            return counts;
        }

        private static void AddCount(Dictionary<IngredientType, int> counts, IngredientType ingredient)
        {
            counts.TryGetValue(ingredient, out int current);
            counts[ingredient] = current + 1;
        }

        private static IngredientType NormalizeIngredient(IngredientType ingredient)
        {
            return ingredient;
        }

        private static bool IsSauce(IngredientType ingredient)
        {
            return ingredient == IngredientType.SauceKetchup || ingredient == IngredientType.SauceMustard;
        }
    }
}
