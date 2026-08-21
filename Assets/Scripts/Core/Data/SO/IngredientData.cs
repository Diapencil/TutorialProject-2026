using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        public int id;
        public string ingredientName;
        public IngredientType type;
        public int unlockCost;
        public int costPerUse;
        public bool isDefaultUnlocked;
        public bool grillable;
        public float cookTimeMin;
        public float cookTimeMax;
    }
}
