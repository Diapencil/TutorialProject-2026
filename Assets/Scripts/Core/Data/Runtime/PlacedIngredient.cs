using System;
using UnityEngine;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class PlacedIngredient
    {
        public IngredientData ingredient;
        public Vector2 position;
        public int layerOrder;
        public CookState cookState;
    }
}