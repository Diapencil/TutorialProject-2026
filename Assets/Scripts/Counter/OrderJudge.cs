using System.Collections.Generic;
using SheepSheepBurger.Core;

namespace Lee.Counter
{
    public static class OrderJudge
    {
        public static Grade Judge(OrderData order, BurgerData burger)
        {
            if (order == null || order.recipe == null || burger == null) return Grade.Bad;

            var counts = new Dictionary<int, int>();
            if (order.recipe.layers != null)
                foreach (var layer in order.recipe.layers)
                    if (layer?.ingredient != null) Add(counts, layer.ingredient.id, layer.quantity);
            if (burger.placedIngredients != null)
                foreach (var placed in burger.placedIngredients)
                    if (placed?.ingredient != null) Add(counts, placed.ingredient.id, -1);

            var difference = 0;
            foreach (var count in counts.Values) difference += System.Math.Abs(count);
            return difference == 0 ? Grade.Perfect : difference <= 1 ? Grade.Good : Grade.Bad;
        }

        public static int GetReward(OrderData order, Grade grade) => grade switch
        {
            Grade.Perfect => order.recipe.basePrice,
            Grade.Good => order.recipe.basePrice / 2,
            _ => 0
        };

        private static void Add(Dictionary<int, int> counts, int ingredientId, int amount)
        {
            counts.TryGetValue(ingredientId, out var current);
            counts[ingredientId] = current + amount;
        }
    }
}
