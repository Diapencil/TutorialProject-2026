using System.Collections.Generic;
using SheepSheepBurger.Core;
using UnityEngine;

namespace Core.Data
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Data/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<IngredientData> ingredients;
        [SerializeField] private List<SheepSheepBurger.Core.RecipeData> recipes;
        [SerializeField] private List<OrderData> orders;
        [SerializeField] private List<SheepSheepBurger.Core.CustomerData> customers;
        [SerializeField] private List<SpecialCustomerData> specialCustomers;

        public IngredientData GetIngredient(int id)
        {
            return ingredients.Find(x => x.id == id);
        }

        public SheepSheepBurger.Core.RecipeData GetRecipe(int id)
        {
            return recipes.Find(x => x.id == id);
        }

        public OrderData GetOrder(int id)
        {
            return orders.Find(x => x.id == id);
        }

        public SheepSheepBurger.Core.CustomerData GetCustomer(int id)
        {
            return customers.Find(x => x.id == id);
        }

        public SpecialCustomerData GetSpecialCustomer(int id)
        {
            return specialCustomers.Find(x => x.id == id);
        }

        public IReadOnlyList<IngredientData> GetAllIngredients() => ingredients;
        public IReadOnlyList<SheepSheepBurger.Core.RecipeData> GetAllRecipes() => recipes;
        public IReadOnlyList<OrderData> GetAllOrders() => orders;
    }
}
