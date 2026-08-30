using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Data/Recipe")]
    public class RecipeData : ScriptableObject
    {
        public int id;
        public string recipeName;
        [Tooltip("도감 카드와 상세창에 표시할 완성 음식 이미지")]
        public Sprite illustration;
        public List<RecipeLayer> layers;
        public int basePrice;
        // public int difficulty;
        public string unlockCondition;
    }
}
