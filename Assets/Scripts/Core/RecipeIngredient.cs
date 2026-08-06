using System;

namespace SheepSheepBurger.Core
{
    /// <summary>
    /// Connects a recipe to one required ingredient. layerOrder is zero-based.
    /// </summary>
    [Serializable]
    public class RecipeIngredient
    {
        // public int recipeId;
        public IngredientData ingredient;
        // public int layerOrder;
        public int quantity;
    }
}
