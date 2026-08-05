using System;
using System.Collections.Generic;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum IngredientType
    {
        BunBottom,
        BunTop,
        Patty,
        PattyRaw,
        PattyBurnt,
        ToppingCheese,
        ToppingPickle,
        ToppingLettuce,
        ToppingTomato,
        ToppingOnion,
        ToppingJalapeno,
        ToppingBacon,
        ToppingFriedEgg,
        SauceKetchup,
        SauceMustard
    }

    [Serializable]
    public sealed class IngredientPlacement
    {
        public IngredientType type;
        public Vector2 position;
        public int stackIndex;

        public IngredientPlacement()
        {
        }

        public IngredientPlacement(IngredientType type, Vector2 position, int stackIndex)
        {
            this.type = type;
            this.position = position;
            this.stackIndex = stackIndex;
        }
    }

    [Serializable]
    public sealed class BurgerData
    {
        public List<IngredientPlacement> ingredients = new List<IngredientPlacement>();

        public BurgerData()
        {
        }

        public BurgerData(IEnumerable<IngredientPlacement> ingredients)
        {
            this.ingredients = new List<IngredientPlacement>(ingredients ?? Array.Empty<IngredientPlacement>());
        }
    }
}
