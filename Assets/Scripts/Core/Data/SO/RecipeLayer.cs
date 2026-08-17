using System;

namespace SheepSheepBurger.Core
{
    /// <summary>
    /// Connects a recipe to one required ingredient. layerOrder is zero-based.
    /// </summary>
    [Serializable]
    public class RecipeLayer
    {
        public IngredientData ingredient;
        public int quantity = 1;
        public CookState cookRequirement;
    }
}
