using System;
using System.Collections.Generic;
using SheepSheepBurger.BurgerAssembly;

namespace SheepSheepBurger.Economy
{
    public sealed class RecipeCatalog
    {
        private readonly List<RecipeData> recipes;

        public RecipeCatalog(IEnumerable<RecipeData> source)
        {
            recipes = new List<RecipeData>(source ?? throw new ArgumentNullException(nameof(source)));
        }

        public IList<RecipeData> Recipes => recipes.AsReadOnly();

        public static RecipeCatalog CreateDefault()
        {
            return new RecipeCatalog(new[]
            {
                Recipe(BurgerRecipeId.Hamburger, "Hamburger", 5f,
                    IngredientType.BunBottom, IngredientType.Patty, IngredientType.ToppingCheese, IngredientType.ToppingPickle, IngredientType.SauceKetchup, IngredientType.SauceMustard, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.SoopSoopBurger, "Soop Soop Burger",
                    IngredientType.BunBottom, IngredientType.Patty, IngredientType.ToppingCheese, IngredientType.ToppingLettuce, IngredientType.ToppingTomato, IngredientType.ToppingOnion, IngredientType.ToppingPickle, IngredientType.SauceKetchup, IngredientType.SauceMustard, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.ThreePattyBurger, "Three Patty Burger",
                    IngredientType.BunBottom, IngredientType.Patty, IngredientType.Patty, IngredientType.Patty, IngredientType.SauceKetchup, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.MiaBurger, "Mia Burger",
                    IngredientType.BunBottom, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.VegetarianBurger, "Vegetarian Burger",
                    IngredientType.BunBottom, IngredientType.ToppingLettuce, IngredientType.ToppingTomato, IngredientType.ToppingOnion, IngredientType.ToppingPickle, IngredientType.ToppingCheese, IngredientType.SauceKetchup, IngredientType.SauceMustard, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.VeganBurger, "Vegan Burger",
                    IngredientType.BunBottom, IngredientType.ToppingLettuce, IngredientType.ToppingTomato, IngredientType.ToppingOnion, IngredientType.ToppingPickle, IngredientType.SauceKetchup, IngredientType.SauceMustard, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.JalakingBurger, "Jalaking Burger",
                    IngredientType.BunBottom, IngredientType.ToppingJalapeno, IngredientType.ToppingJalapeno, IngredientType.ToppingOnion, IngredientType.ToppingOnion, IngredientType.SauceKetchup, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.Hotdog, "Hotdog",
                    IngredientType.BunBottom, IngredientType.Patty, IngredientType.SauceKetchup, IngredientType.SauceMustard, IngredientType.BunTop),
                RecipeAuto(BurgerRecipeId.DujjonkuBurger, "Dujjonku Burger",
                    IngredientType.PattyBurnt),
                RecipeAuto(BurgerRecipeId.WildBurger, "Wild Burger",
                    IngredientType.BunBottom, IngredientType.PattyRaw, IngredientType.ToppingLettuce, IngredientType.BunTop)
            });
        }

        public RecipeData GetRecipe(BurgerRecipeId id)
        {
            if (TryGetRecipe(id, out RecipeData recipe))
            {
                return recipe;
            }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public bool TryGetRecipe(BurgerRecipeId id, out RecipeData recipe)
        {
            for (int index = 0; index < recipes.Count; index++)
            {
                if (recipes[index].id == id)
                {
                    recipe = recipes[index];
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        private static RecipeData RecipeAuto(BurgerRecipeId id, string displayName, params IngredientType[] ingredients)
        {
            return Recipe(id, displayName, EconomyRules.CalculateRecipePrice(ingredients.Length), ingredients);
        }

        private static RecipeData Recipe(BurgerRecipeId id, string displayName, float price, params IngredientType[] ingredients)
        {
            return new RecipeData(id, displayName, price, ingredients);
        }
    }
}
