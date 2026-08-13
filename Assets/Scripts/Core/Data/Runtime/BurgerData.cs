using System;
using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class BurgerData
    {
        public List<PlacedIngredient> placedIngredients;
        public List<int> appliedSauceIds;
        public List<Vector2> sauceTrailPoints;
        public bool isComplete;
    }
}