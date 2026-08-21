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
        public bool hasGrillState;
        public PattyGrillPhase grillPhase;
        public float grillPhaseElapsed;

        public IngredientPlacement(IngredientType type, Vector2 position, int layerOrder)
            : this(type, position, layerOrder, null)
        {
        }

        public IngredientPlacement(
            IngredientType type,
            Vector2 position,
            int layerOrder,
            PattyGrillState grillState)
        {
            this.type = type;
            this.position = position;
            this.layerOrder = layerOrder;
            hasGrillState = grillState != null;
            grillPhase = grillState != null ? grillState.Phase : PattyGrillPhase.RawDough;
            grillPhaseElapsed = grillState != null ? grillState.PhaseElapsed : 0f;
        }

        public IngredientPlacement Clone()
        {
            return new IngredientPlacement(type, position, layerOrder)
            {
                hasGrillState = hasGrillState,
                grillPhase = grillPhase,
                grillPhaseElapsed = grillPhaseElapsed
            };
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
        private IngredientType? lastLayerType;
        private int lastLayerOrder = -1;

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

            if (lastLayerType.HasValue && lastLayerType.Value == type)
            {
                layerOrder = lastLayerOrder;
            }
            else
            {
                layerOrder = nextLayerOrder++;
                lastLayerType = type;
                lastLayerOrder = layerOrder;
            }
            return true;
        }

        public bool TryUnregisterPlacement(IngredientType type)
        {
            if (IsCompleted || type == IngredientType.BunBottom)
            {
                return false;
            }

            if (IsTopping(type))
            {
                ToppingCount = Math.Max(0, ToppingCount - 1);
            }
            else if (type == IngredientType.BunTop)
            {
                HasTopBun = false;
            }

            // Moving an ingredient out of the stack interrupts the placement
            // sequence. A later placement therefore starts a fresh layer even
            // when it has the same type as the previous visible ingredient.
            lastLayerType = null;
            lastLayerOrder = -1;

            return true;
        }

        public int BringToFront()
        {
            if (IsCompleted)
            {
                return -1;
            }

            lastLayerType = null;
            lastLayerOrder = -1;
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
            lastLayerType = null;
            lastLayerOrder = -1;
        }

        public static bool IsTopping(IngredientType type)
        {
            return BurgerIngredientCatalog.IsTopping(type);
        }
    }
}
