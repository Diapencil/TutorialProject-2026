using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    [Serializable]
    public sealed class IngredientPlacement
    {
        public IngredientType type;
        public Vector2 position;
        public int layerOrder;

        public IngredientPlacement(IngredientType type, Vector2 position, int layerOrder)
        {
            this.type = type;
            this.position = position;
            this.layerOrder = layerOrder;
        }

        public IngredientPlacement Clone()
        {
            return new IngredientPlacement(type, position, layerOrder);
        }
    }

    [Serializable]
    public sealed class BurgerData
    {
        public List<IngredientPlacement> ingredients = new List<IngredientPlacement>();

        public BurgerData()
        {
        }

        public BurgerData(IEnumerable<IngredientPlacement> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ingredients = source
                .OrderBy(placement => placement.layerOrder)
                .Select(placement => placement.Clone())
                .ToList();
        }
    }

    public sealed class BurgerAssemblyState
    {
        private int nextLayerOrder;

        public BurgerAssemblyState(int maximumToppings = CookingPrototypeRules.MaximumToppings)
        {
            if (maximumToppings < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumToppings));
            }

            MaximumToppings = maximumToppings;
        }

        public int MaximumToppings { get; }

        public int ToppingCount { get; private set; }

        public bool IsCompleted { get; private set; }

        public bool CanPlace(IngredientType type)
        {
            return !IsCompleted && (!IsTopping(type) || ToppingCount < MaximumToppings);
        }

        public bool TryRegisterPlacement(IngredientType type, out int layerOrder)
        {
            layerOrder = -1;
            if (!CanPlace(type))
            {
                return false;
            }

            if (IsTopping(type))
            {
                ToppingCount++;
            }

            layerOrder = nextLayerOrder++;
            return true;
        }

        public int BringToFront()
        {
            if (IsCompleted)
            {
                return -1;
            }

            return nextLayerOrder++;
        }

        public bool TryComplete(IEnumerable<IngredientPlacement> placements, out BurgerData burgerData)
        {
            burgerData = null;
            if (IsCompleted)
            {
                return false;
            }

            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            IsCompleted = true;
            burgerData = new BurgerData(placements);
            return true;
        }

        public void Reset()
        {
            ToppingCount = 0;
            IsCompleted = false;
            nextLayerOrder = 0;
        }

        public static bool IsTopping(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.ToppingLettuce:
                case IngredientType.ToppingTomato:
                case IngredientType.ToppingCheese:
                case IngredientType.ToppingOnion:
                case IngredientType.ToppingPickle:
                    return true;
                default:
                    return false;
            }
        }
    }
}
