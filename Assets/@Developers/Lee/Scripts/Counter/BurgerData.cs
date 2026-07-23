using System;
using System.Collections.Generic;

namespace Lee.Counter
{
    /// <summary>조리 씬에서 완성한 버거. ScriptableObject가 아닌 주문별 런타임 값입니다.</summary>
    [Serializable]
    public sealed class BurgerData
    {
        public List<IngredientType> Ingredients = new();

        public BurgerData() { }
        public BurgerData(IEnumerable<IngredientType> ingredients) => Ingredients = new List<IngredientType>(ingredients);
    }
}
