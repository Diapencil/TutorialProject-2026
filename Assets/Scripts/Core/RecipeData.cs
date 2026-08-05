using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Recipe")]
    public class RecipeData : ScriptableObject
    {
        public int id;
        public string name;
        public int basePrice;
        public int difficulty;
        public string unlockCondition;
    }
}
