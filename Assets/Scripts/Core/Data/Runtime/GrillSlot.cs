using System;

namespace SheepSheepBurger.Core
{
    [Serializable]
    public class GrillSlot
    {
        public IngredientData ingredient;
        public float cookTimer;
        public bool isFlipped;
        public CookState currentState;
    }
}