using System;
using System.Collections.Generic;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum BurgerAssemblyPhase
    {
        WaitingForBottomBun,
        Assembling,
        Completed
    }

    public sealed class BurgerAssemblyState
    {
        private readonly List<BurgerIngredientId> layers = new List<BurgerIngredientId>();

        public BurgerAssemblyState(int maximumLayers = 10)
        {
            if (maximumLayers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLayers));
            }

            MaximumLayers = maximumLayers;
        }

        public int MaximumLayers { get; }

        public BurgerAssemblyPhase Phase { get; private set; } = BurgerAssemblyPhase.WaitingForBottomBun;

        public IReadOnlyList<BurgerIngredientId> Layers => layers;

        public bool TryStart()
        {
            if (Phase != BurgerAssemblyPhase.WaitingForBottomBun)
            {
                return false;
            }

            Phase = BurgerAssemblyPhase.Assembling;
            return true;
        }

        public bool TryAdd(BurgerIngredientId ingredient)
        {
            if (Phase != BurgerAssemblyPhase.Assembling || layers.Count >= MaximumLayers)
            {
                return false;
            }

            layers.Add(ingredient);
            return true;
        }

        public bool TryFinish()
        {
            if (Phase != BurgerAssemblyPhase.Assembling || layers.Count == 0)
            {
                return false;
            }

            Phase = BurgerAssemblyPhase.Completed;
            return true;
        }

        public void Reset()
        {
            layers.Clear();
            Phase = BurgerAssemblyPhase.WaitingForBottomBun;
        }
    }
}
