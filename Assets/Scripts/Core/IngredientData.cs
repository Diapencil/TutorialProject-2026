using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Ingredient")]
    public class IngredientData : ScriptableObject
    {
        public int id;
        public string name;
        public IngredientType type;
        public int unlockCost;
        public bool isUnlocked;
        public float cookTimeMin;
        public float cookTimeMax;
    }
}
