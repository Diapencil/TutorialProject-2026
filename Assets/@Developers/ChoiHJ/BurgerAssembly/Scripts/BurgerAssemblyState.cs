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
    public sealed class SauceStrokeData
    {
        public IngredientType type;
        public List<Vector2> points = new List<Vector2>();
        public int layerOrder;

        public SauceStrokeData(IngredientType type, IEnumerable<Vector2> source, int layerOrder)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.type = type;
            points = source.ToList();
            this.layerOrder = layerOrder;
        }

        public SauceStrokeData Clone()
        {
            return new SauceStrokeData(type, points, layerOrder);
        }
    }

    [Serializable]
    public sealed class BurgerData
    {
        public List<IngredientPlacement> ingredients = new List<IngredientPlacement>();
        public List<SauceStrokeData> sauceStrokes = new List<SauceStrokeData>();

        public BurgerData()
        {
        }

        public BurgerData(IEnumerable<IngredientPlacement> source)
            : this(source, Array.Empty<SauceStrokeData>())
        {
        }

        public BurgerData(
            IEnumerable<IngredientPlacement> source,
            IEnumerable<SauceStrokeData> sauceSource)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (sauceSource == null)
            {
                throw new ArgumentNullException(nameof(sauceSource));
            }

            ingredients = source
                .OrderBy(placement => placement.layerOrder)
                .Select(placement => placement.Clone())
                .ToList();
            sauceStrokes = sauceSource
                .OrderBy(stroke => stroke.layerOrder)
                .Select(stroke => stroke.Clone())
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

        public bool HasBottomBun { get; private set; }

        public bool HasTopBun { get; private set; }

        public bool CanPlace(IngredientType type)
        {
            if (IsCompleted)
            {
                return false;
            }

            if (type == IngredientType.BunBottom)
            {
                return !HasBottomBun;
            }

            if (!HasBottomBun || (type == IngredientType.BunTop && HasTopBun))
            {
                return false;
            }

            return !IsTopping(type) || ToppingCount < MaximumToppings;
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
            else if (type == IngredientType.BunBottom)
            {
                HasBottomBun = true;
            }
            else if (type == IngredientType.BunTop)
            {
                HasTopBun = true;
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
            if (IsCompleted || !HasBottomBun || !HasTopBun)
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
            HasBottomBun = false;
            HasTopBun = false;
            nextLayerOrder = 0;
        }

        public static bool IsTopping(IngredientType type)
        {
            return BurgerIngredientCatalog.IsTopping(type);
        }
    }
}
