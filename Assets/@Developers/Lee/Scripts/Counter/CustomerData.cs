using System.Collections.Generic;
using UnityEngine;

namespace Lee.Counter
{
    /// <summary>손님 종류와 이 손님이 주문할 수 있는 레시피를 정의합니다.</summary>
    [CreateAssetMenu(menuName = "Lee/Counter/Customer Data", fileName = "Customer_")]
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField] private string customerName;
        [Tooltip("손님은 이 목록에 있는 레시피만 주문합니다.")]
        [SerializeField] private List<RecipeData> preferredRecipes = new();

        public string CustomerName => customerName;
        public IReadOnlyList<RecipeData> PreferredRecipes => preferredRecipes;

        public bool TryGetRandomPreferredRecipe(out RecipeData recipe)
        {
            var validRecipes = new List<RecipeData>();
            foreach (var preferredRecipe in preferredRecipes)
            {
                if (preferredRecipe != null) validRecipes.Add(preferredRecipe);
            }

            if (validRecipes.Count == 0)
            {
                recipe = null;
                return false;
            }

            recipe = validRecipes[Random.Range(0, validRecipes.Count)];
            return true;
        }
    }
}
