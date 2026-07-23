using System.Collections.Generic;
using UnityEngine;

namespace Lee.Counter
{
    [CreateAssetMenu(menuName = "Lee/Counter/Recipe Data", fileName = "Recipe_")]
    public sealed class RecipeData : ScriptableObject
    {
        [SerializeField] private string recipeName;
        [TextArea, SerializeField] private string customerRequest;
        [Min(0), SerializeField] private int baseReward = 100;
        [SerializeField] private List<IngredientType> ingredients = new();

        public string RecipeName => recipeName;
        public string CustomerRequest => customerRequest;
        public int BaseReward => baseReward;
        public IReadOnlyList<IngredientType> Ingredients => ingredients;
    }
}
