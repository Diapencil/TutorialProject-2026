using System;
using System.Collections.Generic;

// 레시피 정답표와 순서 무관 비교 로직입니다.
// 붙이는 오브젝트: 없음. CounterUI, CookingUI, GameManager가 코드로 참조합니다.
public enum RecipeId
{
    BasicBurger,
    CheeseBurger,
    VeggieBurger,
    FishBurger,
    SheepSheepBurger
}

[Serializable]
public class Recipe
{
    public RecipeId Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<IngredientType> Ingredients => ingredients;

    private readonly List<IngredientType> ingredients;

    public Recipe(RecipeId id, string displayName, params IngredientType[] ingredientList)
    {
        Id = id;
        DisplayName = displayName;
        ingredients = new List<IngredientType>(ingredientList);
    }
}

public static class RecipeBook
{
    private static readonly List<Recipe> recipes = new List<Recipe>
    {
        new Recipe(RecipeId.BasicBurger, "고기만버거", IngredientType.Bun, IngredientType.Patty),
        new Recipe(RecipeId.CheeseBurger, "치즈버거", IngredientType.Bun, IngredientType.Patty, IngredientType.Cheese),
        new Recipe(RecipeId.VeggieBurger, "채식버거", IngredientType.Bun, IngredientType.Lettuce, IngredientType.Tomato, IngredientType.Onion),
        new Recipe(RecipeId.FishBurger, "물고기버거", IngredientType.Bun, IngredientType.FishFillet),
        new Recipe(RecipeId.SheepSheepBurger, "슆슆버거", IngredientType.Bun, IngredientType.Patty, IngredientType.Lettuce, IngredientType.Cheese, IngredientType.Tomato, IngredientType.Onion, IngredientType.Pickle)
    };

    public static IReadOnlyList<Recipe> All => recipes;

    public static Recipe Get(RecipeId id)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i].Id == id)
            {
                return recipes[i];
            }
        }

        return recipes[0];
    }

    // 현재 조합과 정답표의 종류+개수가 같은지 검사합니다. 드래그 순서는 무시합니다.
    public static bool Matches(IReadOnlyList<IngredientType> current, Recipe recipe)
    {
        if (recipe == null)
        {
            return false;
        }

        return SameIngredientCounts(current, recipe.Ingredients);
    }

    // 현재 조합이 어떤 레시피와 일치하는지 찾습니다.
    public static Recipe FindMatchingRecipe(IReadOnlyList<IngredientType> current)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (Matches(current, recipes[i]))
            {
                return recipes[i];
            }
        }

        return null;
    }

    private static bool SameIngredientCounts(IReadOnlyList<IngredientType> a, IReadOnlyList<IngredientType> b)
    {
        Dictionary<IngredientType, int> counts = new Dictionary<IngredientType, int>();

        for (int i = 0; i < a.Count; i++)
        {
            IngredientType type = a[i];
            counts.TryGetValue(type, out int count);
            counts[type] = count + 1;
        }

        for (int i = 0; i < b.Count; i++)
        {
            IngredientType type = b[i];
            if (!counts.TryGetValue(type, out int count))
            {
                return false;
            }

            if (count == 1)
            {
                counts.Remove(type);
            }
            else
            {
                counts[type] = count - 1;
            }
        }

        return counts.Count == 0;
    }
}
