using System.Collections.Generic;

namespace Lee.Counter
{
    public enum ServiceResult { Perfect, Good, Bad, Timeout }

    /// <summary>재료를 개수까지 비교하는 순수 판정 클래스입니다.</summary>
    public static class OrderJudge
    {
        public static ServiceResult Judge(OrderData order, BurgerData burger)
        {
            if (burger == null) return ServiceResult.Bad;
            var counts = new Dictionary<IngredientType, int>();
            foreach (var ingredient in order.RequestedRecipe.Ingredients) Add(counts, ingredient, 1);
            foreach (var ingredient in burger.Ingredients) Add(counts, ingredient, -1);
            var difference = 0;
            foreach (var count in counts.Values) difference += System.Math.Abs(count);
            return difference == 0 ? ServiceResult.Perfect : difference <= 1 ? ServiceResult.Good : ServiceResult.Bad;
        }

        public static int GetReward(OrderData order, ServiceResult result) => result switch
        {
            ServiceResult.Perfect => order.RequestedRecipe.BaseReward,
            ServiceResult.Good => order.RequestedRecipe.BaseReward / 2,
            _ => 0
        };

        private static void Add(Dictionary<IngredientType, int> counts, IngredientType ingredient, int amount)
        {
            counts.TryGetValue(ingredient, out var current);
            counts[ingredient] = current + amount;
        }
    }
}
