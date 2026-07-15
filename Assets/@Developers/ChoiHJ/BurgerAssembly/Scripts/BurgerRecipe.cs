using System;
using System.Collections.Generic;

namespace SheepSheepBurger.BurgerAssembly
{
    public sealed class BurgerRecipe
    {
        private readonly BurgerIngredientId[] ingredients;

        public BurgerRecipe(string name, params BurgerIngredientId[] ingredients)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Recipe name is required.", nameof(name));
            }

            if (ingredients == null || ingredients.Length == 0)
            {
                throw new ArgumentException("A recipe needs at least one ingredient.", nameof(ingredients));
            }

            Name = name;
            this.ingredients = (BurgerIngredientId[])ingredients.Clone();
        }

        public string Name { get; }

        public IReadOnlyList<BurgerIngredientId> Ingredients => ingredients;

        public bool Matches(IReadOnlyList<BurgerIngredientId> candidate)
        {
            if (candidate == null || candidate.Count != ingredients.Length)
            {
                return false;
            }

            var remaining = new Dictionary<BurgerIngredientId, int>();
            foreach (BurgerIngredientId ingredient in ingredients)
            {
                remaining.TryGetValue(ingredient, out int count);
                remaining[ingredient] = count + 1;
            }

            for (int index = 0; index < candidate.Count; index++)
            {
                BurgerIngredientId ingredient = candidate[index];
                if (!remaining.TryGetValue(ingredient, out int count) || count == 0)
                {
                    return false;
                }

                remaining[ingredient] = count - 1;
            }

            return true;
        }
    }

    public static class BurgerRecipeCatalog
    {
        public static readonly BurgerRecipe Classic = new BurgerRecipe(
            "클래식 버거",
            BurgerIngredientId.Ketchup,
            BurgerIngredientId.Patty,
            BurgerIngredientId.Cheese,
            BurgerIngredientId.Lettuce,
            BurgerIngredientId.Tomato);

        public static readonly BurgerRecipe Cheeseburger = new BurgerRecipe(
            "치즈 버거",
            BurgerIngredientId.Ketchup,
            BurgerIngredientId.Patty,
            BurgerIngredientId.Cheese);

        public static readonly BurgerRecipe Veggie = new BurgerRecipe(
            "채식 버거",
            BurgerIngredientId.Mustard,
            BurgerIngredientId.Lettuce,
            BurgerIngredientId.Tomato,
            BurgerIngredientId.Onion,
            BurgerIngredientId.Pickle);

        public static readonly IReadOnlyList<BurgerRecipe> All = new[]
        {
            Classic,
            Cheeseburger,
            Veggie
        };

        public static BurgerRecipe FindMatch(IReadOnlyList<BurgerIngredientId> ingredients)
        {
            foreach (BurgerRecipe recipe in All)
            {
                if (recipe.Matches(ingredients))
                {
                    return recipe;
                }
            }

            return null;
        }
    }
}
