using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Recipe")]
    public class RecipeData : ScriptableObject
    {
        public int id;
        public string recipeName;
        public List<RecipeLayer> layers;
        public int basePrice;
        // public int difficulty;
        public string unlockCondition;
    }
}
