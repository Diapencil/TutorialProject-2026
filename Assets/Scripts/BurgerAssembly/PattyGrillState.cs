using System;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum PattyGrillPhase
    {
        RawDough,
        Flattened,
        CookingSide1,
        ReadyToFlip,
        Flipping,
        CookingSide2,
        Done,
        Overcooked
    }

    public sealed class PattyGrillState
    {
        private float phaseElapsed;
        private readonly float cookTimeMultiplier;
        private readonly float burnChance;
        private bool burnProtected;

        public PattyGrillState(
            IngredientType ingredientType = IngredientType.Patty,
            float cookTimeMultiplier = 1f,
            float burnChance = 1f)
        {
            if (!BurgerIngredientCatalog.IsGrillIngredient(ingredientType))
            {
                throw new ArgumentOutOfRangeException(nameof(ingredientType));
            }

            IngredientType = ingredientType;
            this.cookTimeMultiplier = Mathf.Max(0.05f, cookTimeMultiplier);
            this.burnChance = Mathf.Clamp01(burnChance);
        }

        public IngredientType IngredientType { get; }

        public bool RequiresFlip => BurgerIngredientCatalog.RequiresFlip(IngredientType);

        public PattyGrillPhase Phase { get; private set; } = PattyGrillPhase.RawDough;

        public float PhaseElapsed => phaseElapsed;

        public float FirstSideCookDuration => CookingPrototypeRules.FirstSideCookSeconds * cookTimeMultiplier;

        public float SecondSideCookDuration => CookingPrototypeRules.SecondSideCookSeconds * cookTimeMultiplier;

        public bool CanDragToBoard => true;

        public event Action<PattyGrillPhase> PhaseChanged;

        /// <summary>
        /// Temporarily prevents this ingredient from transitioning to the burnt state.
        /// Cooking progress continues normally.
        /// </summary>
        public void SetBurnProtection(bool isProtected)
        {
            burnProtected = isProtected;
        }

        public bool TryPressDough()
        {
            if (Phase != PattyGrillPhase.RawDough)
            {
                return false;
            }

            TransitionTo(PattyGrillPhase.Flattened);
            return true;
        }

        public bool TryFlip()
        {
            if (Phase != PattyGrillPhase.ReadyToFlip)
            {
                return false;
            }

            TransitionTo(PattyGrillPhase.Flipping);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            float remaining = deltaTime;
            int safety = 0;
            while (safety++ < 8)
            {
                switch (Phase)
                {
                    case PattyGrillPhase.Flattened:
                        TransitionTo(PattyGrillPhase.CookingSide1);
                        continue;
                    case PattyGrillPhase.CookingSide1:
                        {
                            PattyGrillPhase firstSideResult = RequiresFlip
                                ? PattyGrillPhase.ReadyToFlip
                                : PattyGrillPhase.Done;
                            if (!AdvanceTimedPhase(ref remaining, FirstSideCookDuration, firstSideResult))
                            {
                                return;
                            }
                            continue;
                        }
                    case PattyGrillPhase.ReadyToFlip:
                        if (!ConsumeDuration(ref remaining, CookingPrototypeRules.ReadyToFlipBurnSeconds))
                        {
                            return;
                        }

                        if (ShouldBurn())
                        {
                            TransitionTo(PattyGrillPhase.Overcooked);
                        }
                        else
                        {
                            phaseElapsed = 0f;
                        }
                        return;
                    case PattyGrillPhase.Flipping:
                        if (!AdvanceTimedPhase(ref remaining, CookingPrototypeRules.FlipAnimationSeconds, PattyGrillPhase.CookingSide2))
                        {
                            return;
                        }
                        continue;
                    case PattyGrillPhase.CookingSide2:
                        if (!AdvanceTimedPhase(ref remaining, SecondSideCookDuration, PattyGrillPhase.Done))
                        {
                            return;
                        }
                        continue;
                    case PattyGrillPhase.Done:
                        if (!ConsumeDuration(ref remaining, CookingPrototypeRules.DoneToOvercookedSeconds))
                        {
                            return;
                        }

                        if (ShouldBurn())
                        {
                            TransitionTo(PattyGrillPhase.Overcooked);
                        }
                        else
                        {
                            phaseElapsed = 0f;
                        }
                        return;
                    default:
                        return;
                }
            }
        }

        public float GetNormalizedProgress()
        {
            switch (Phase)
            {
                case PattyGrillPhase.RawDough:
                case PattyGrillPhase.Flattened:
                    return 0f;
                case PattyGrillPhase.CookingSide1:
                    return 0.45f * Math.Min(1f, phaseElapsed / FirstSideCookDuration);
                case PattyGrillPhase.ReadyToFlip:
                case PattyGrillPhase.Flipping:
                    return 0.5f;
                case PattyGrillPhase.CookingSide2:
                    return 0.5f + 0.45f * Math.Min(1f, phaseElapsed / SecondSideCookDuration);
                case PattyGrillPhase.Done:
                case PattyGrillPhase.Overcooked:
                    return 1f;
                default:
                    return 0f;
            }
        }

        public float GetDoneTimeRemaining()
        {
            return Phase == PattyGrillPhase.Done
                ? Math.Max(0f, CookingPrototypeRules.DoneToOvercookedSeconds - phaseElapsed)
                : 0f;
        }

        public float GetFlipTimeRemaining()
        {
            return Phase == PattyGrillPhase.ReadyToFlip
                ? Math.Max(0f, CookingPrototypeRules.ReadyToFlipBurnSeconds - phaseElapsed)
                : 0f;
        }

        public void Reset()
        {
            TransitionTo(PattyGrillPhase.RawDough);
        }

        private bool AdvanceTimedPhase(ref float remaining, float duration, PattyGrillPhase nextPhase)
        {
            if (!ConsumeDuration(ref remaining, duration))
            {
                return false;
            }

            TransitionTo(nextPhase);
            return true;
        }

        private bool ConsumeDuration(ref float remaining, float duration)
        {
            float needed = Math.Max(0f, duration - phaseElapsed);
            float consumed = Math.Min(remaining, needed);
            phaseElapsed += consumed;
            remaining -= consumed;
            return phaseElapsed + 0.0001f >= duration;
        }

        private bool ShouldBurn()
        {
            return !burnProtected && burnChance > 0f && UnityEngine.Random.value <= burnChance;
        }

        private void TransitionTo(PattyGrillPhase nextPhase)
        {
            Phase = nextPhase;
            phaseElapsed = 0f;
            PhaseChanged?.Invoke(Phase);
        }
    }
}
