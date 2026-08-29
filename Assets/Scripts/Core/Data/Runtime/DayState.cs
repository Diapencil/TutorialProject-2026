using System;
using System.Collections.Generic;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class DayState
    {
        public int dayNumber = 1;
        public int customersServed;
        public int dailyRevenue;
        public int dailyIngredientCost;
        public int dailyProfit;
        public int averageReward;
        public int totalIngredientUses;
        public int ordersWithHint;
        public int perfectCount;
        public int goodCount;
        public int normalCount;
        public int badCount;
        public List<int> count;
        public List<GradeCountRecord> gradeCounts = new List<GradeCountRecord>();
        public List<IngredientUsageRecord> ingredientUsages = new List<IngredientUsageRecord>();
        public List<OrderResultRecord> orderResults = new List<OrderResultRecord>();
        public bool wasAttackedToday;
        public bool isComplete;

        public static DayState CreateForDay(int day)
        {
            DayState state = new DayState();
            state.ResetForDay(day);
            return state;
        }

        public void EnsureInitialized(int fallbackDayNumber)
        {
            if (dayNumber <= 0)
            {
                dayNumber = Math.Max(1, fallbackDayNumber);
            }

            EnsureLists();
            SyncDerivedValues();
            SyncGradeCounts();
        }

        public void ResetForDay(int day)
        {
            dayNumber = Math.Max(1, day);
            customersServed = 0;
            dailyRevenue = 0;
            dailyIngredientCost = 0;
            dailyProfit = 0;
            averageReward = 0;
            totalIngredientUses = 0;
            ordersWithHint = 0;
            perfectCount = 0;
            goodCount = 0;
            normalCount = 0;
            badCount = 0;
            wasAttackedToday = false;
            isComplete = false;

            EnsureLists();
            count.Clear();
            gradeCounts.Clear();
            ingredientUsages.Clear();
            orderResults.Clear();
            SyncGradeCounts();
        }

        public void RecordServedOrder(OrderInstance orderInstance,
                                      BurgerData burger,
                                      Grade grade,
                                      int reward,
                                      bool hintUsed,
                                      int ingredientErrors,
                                      int cookStateErrors)
        {
            EnsureInitialized(dayNumber);

            customersServed++;
            dailyRevenue += Math.Max(0, reward);

            if (hintUsed)
            {
                ordersWithHint++;
            }

            AddGrade(grade);

            OrderResultRecord orderRecord = CreateOrderResultRecord(orderInstance,
                                                                    burger,
                                                                    grade,
                                                                    reward,
                                                                    hintUsed,
                                                                    ingredientErrors,
                                                                    cookStateErrors);
            int ingredientCost = RecordIngredientUsages(burger, orderRecord.consumedIngredients);
            orderRecord.ingredientCost = ingredientCost;

            dailyIngredientCost += ingredientCost;
            orderResults.Add(orderRecord);

            SyncDerivedValues();
            SyncGradeCounts();
            SyncLegacyCountList();
        }

        public int GetGradeCount(Grade grade)
        {
            return grade switch
            {
                Grade.Perfect => perfectCount,
                Grade.Good => goodCount,
                Grade.Normal => normalCount,
                Grade.Bad => badCount,
                _ => 0
            };
        }

        public void MarkComplete()
        {
            isComplete = true;
            SyncDerivedValues();
            SyncGradeCounts();
        }

        private void EnsureLists()
        {
            count ??= new List<int>();
            gradeCounts ??= new List<GradeCountRecord>();
            ingredientUsages ??= new List<IngredientUsageRecord>();
            orderResults ??= new List<OrderResultRecord>();
        }

        private OrderResultRecord CreateOrderResultRecord(OrderInstance orderInstance,
                                                          BurgerData burger,
                                                          Grade grade,
                                                          int reward,
                                                          bool hintUsed,
                                                          int ingredientErrors,
                                                          int cookStateErrors)
        {
            OrderData order = orderInstance?.order;
            RecipeData recipe = order != null ? order.recipe : null;
            CustomerData customer = orderInstance?.customer;

            return new OrderResultRecord
            {
                sequence = customersServed,
                customerId = customer != null ? customer.id : 0,
                customerName = customer != null ? customer.customerName : string.Empty,
                orderId = order != null ? order.id : 0,
                recipeId = recipe != null ? recipe.id : 0,
                recipeName = recipe != null ? recipe.recipeName : string.Empty,
                grade = grade,
                reward = Math.Max(0, reward),
                hintUsed = hintUsed,
                requestedIngredientCount = CountRequestedIngredients(order),
                submittedIngredientCount = CountSubmittedIngredients(burger),
                appliedSauceCount = burger?.appliedSauceIds != null ? burger.appliedSauceIds.Count : 0,
                burgerCompleted = burger != null && burger.isComplete,
                ingredientErrors = Math.Max(0, ingredientErrors),
                cookStateErrors = Math.Max(0, cookStateErrors),
                totalErrors = Math.Max(0, ingredientErrors) + Math.Max(0, cookStateErrors),
                consumedIngredients = new List<IngredientUsageRecord>()
            };
        }

        private int RecordIngredientUsages(BurgerData burger, List<IngredientUsageRecord> orderUsages)
        {
            if (burger == null || burger.placedIngredients == null)
            {
                return 0;
            }

            int ingredientCost = 0;

            for (int i = 0; i < burger.placedIngredients.Count; i++)
            {
                PlacedIngredient placed = burger.placedIngredients[i];

                if (placed?.ingredient == null)
                {
                    continue;
                }

                IngredientData ingredient = placed.ingredient;
                int unitCost = Math.Max(0, ingredient.costPerUse);

                AddIngredientUsage(ingredientUsages, ingredient.id, ingredient.ingredientName, unitCost, 1);
                AddIngredientUsage(orderUsages, ingredient.id, ingredient.ingredientName, unitCost, 1);

                totalIngredientUses++;
                ingredientCost += unitCost;
            }

            return ingredientCost;
        }

        private void AddGrade(Grade grade)
        {
            switch (grade)
            {
                case Grade.Perfect:
                    perfectCount++;
                    break;
                case Grade.Good:
                    goodCount++;
                    break;
                case Grade.Normal:
                    normalCount++;
                    break;
                case Grade.Bad:
                    badCount++;
                    break;
            }
        }

        private void SyncDerivedValues()
        {
            dailyProfit = dailyRevenue - dailyIngredientCost;
            averageReward = customersServed > 0 ? dailyRevenue / customersServed : 0;
        }

        private void SyncGradeCounts()
        {
            EnsureLists();
            SyncGradeCount(Grade.Perfect, perfectCount);
            SyncGradeCount(Grade.Good, goodCount);
            SyncGradeCount(Grade.Normal, normalCount);
            SyncGradeCount(Grade.Bad, badCount);
        }

        private void SyncGradeCount(Grade grade, int value)
        {
            GradeCountRecord record = FindGradeCountRecord(grade);

            if (record == null)
            {
                record = new GradeCountRecord { grade = grade };
                gradeCounts.Add(record);
            }

            record.count = Math.Max(0, value);
        }

        private GradeCountRecord FindGradeCountRecord(Grade grade)
        {
            for (int i = 0; i < gradeCounts.Count; i++)
            {
                GradeCountRecord record = gradeCounts[i];

                if (record != null && record.grade == grade)
                {
                    return record;
                }
            }

            return null;
        }

        private void SyncLegacyCountList()
        {
            EnsureLists();
            count.Clear();

            for (int i = 0; i < ingredientUsages.Count; i++)
            {
                IngredientUsageRecord usage = ingredientUsages[i];

                if (usage != null)
                {
                    count.Add(usage.count);
                }
            }
        }

        private static void AddIngredientUsage(List<IngredientUsageRecord> usages,
                                               int ingredientId,
                                               string ingredientName,
                                               int unitCost,
                                               int amount)
        {
            if (usages == null || amount <= 0)
            {
                return;
            }

            IngredientUsageRecord usage = FindIngredientUsage(usages, ingredientId);

            if (usage == null)
            {
                usage = new IngredientUsageRecord
                {
                    ingredientId = ingredientId,
                    ingredientName = ingredientName,
                    unitCost = Math.Max(0, unitCost)
                };
                usages.Add(usage);
            }

            usage.count += amount;
            usage.totalCost = usage.count * usage.unitCost;
        }

        private static IngredientUsageRecord FindIngredientUsage(List<IngredientUsageRecord> usages,
                                                                 int ingredientId)
        {
            for (int i = 0; i < usages.Count; i++)
            {
                IngredientUsageRecord usage = usages[i];

                if (usage != null && usage.ingredientId == ingredientId)
                {
                    return usage;
                }
            }

            return null;
        }

        private static int CountRequestedIngredients(OrderData order)
        {
            if (order == null || order.recipe == null || order.recipe.layers == null)
            {
                return 0;
            }

            int result = 0;

            for (int i = 0; i < order.recipe.layers.Count; i++)
            {
                RecipeLayer layer = order.recipe.layers[i];

                if (layer?.ingredient != null)
                {
                    result += Math.Max(0, layer.quantity);
                }
            }

            return result;
        }

        private static int CountSubmittedIngredients(BurgerData burger)
        {
            return burger?.placedIngredients != null ? burger.placedIngredients.Count : 0;
        }
    }

    [Serializable]
    public class GradeCountRecord
    {
        public Grade grade;
        public int count;
    }

    [Serializable]
    public class IngredientUsageRecord
    {
        public int ingredientId;
        public string ingredientName;
        public int count;
        public int unitCost;
        public int totalCost;
    }

    [Serializable]
    public class OrderResultRecord
    {
        public int sequence;
        public int customerId;
        public string customerName;
        public int orderId;
        public int recipeId;
        public string recipeName;
        public Grade grade;
        public int reward;
        public int ingredientCost;
        public bool hintUsed;
        public int requestedIngredientCount;
        public int submittedIngredientCount;
        public int appliedSauceCount;
        public bool burgerCompleted;
        public int ingredientErrors;
        public int cookStateErrors;
        public int totalErrors;
        public List<IngredientUsageRecord> consumedIngredients = new List<IngredientUsageRecord>();
    }
}
