// 상점에서 산 것(재료 해금 / 설비 레벨)을 조리·카운터 씬이 읽을 수 있게 중계한다.
using System.Collections.Generic;
using UnityEngine;
using CookingIngredientType = SheepSheepBurger.BurgerAssembly.IngredientType;

namespace SheepSheepBurger.Core
{
    /// <summary>
    /// 조리 씬은 Core와 다른 자체 IngredientType(=id 체계)을 쓴다.
    /// 두 체계를 잇는 대응표를 이 한 곳에만 두고, 다른 코드는 이 클래스만 본다.
    /// </summary>
    public static class ShopProgressBridge
    {
        /// <summary>튀김기 UpgradeData.id (Assets/Data/Shop/Upgrades/Fryer.asset).</summary>
        public const int FryerUpgradeId = 1;

        /// <summary>그릴판 UpgradeData.id (Assets/Data/Shop/Upgrades/Grill.asset).</summary>
        public const int GrillUpgradeId = 2;

        /// <summary>
        /// 조리 씬 IngredientType → Core IngredientData.id (Assets/Data/Ingredients/*.asset).
        /// 두 체계는 숫자가 겹치므로(예: Core Lettuce=10, 조리 Bacon=10) 반드시 이 표를 거쳐야 한다.
        /// </summary>
        private static readonly Dictionary<CookingIngredientType, int> CookingToCoreIngredientId =
            new Dictionary<CookingIngredientType, int>
            {
                { CookingIngredientType.BunBottom, 1 },
                { CookingIngredientType.BunTop, 2 },
                { CookingIngredientType.Patty, 3 },
                { CookingIngredientType.ToppingCheese, 4 },
                { CookingIngredientType.ToppingPickle, 5 },
                { CookingIngredientType.SauceKetchup, 6 },
                { CookingIngredientType.SauceMustard, 7 },
                { CookingIngredientType.ToppingJalapeno, 8 },
                { CookingIngredientType.ToppingOnion, 9 },
                { CookingIngredientType.ToppingLettuce, 10 },
                { CookingIngredientType.ToppingTomato, 11 },
                { CookingIngredientType.Egg, 12 },
                { CookingIngredientType.Bacon, 13 }
            };

        // 상점에서 해금 대상인 재료 목록. 피클은 기본 해금 재료다.
        // 나머지는 기본 해금이라 상점 구매 여부와 무관하게 항상 사용할 수 있다.
        private static readonly HashSet<CookingIngredientType> ShopUnlockableIngredients =
            new HashSet<CookingIngredientType>
            {
                CookingIngredientType.Bacon,
                CookingIngredientType.Egg,
                CookingIngredientType.ToppingJalapeno
            };

        private static GameState State => GameManager.Instance != null ? GameManager.Instance.State : null;

        /// <summary>조리 씬의 재료가 지금 쓸 수 있는 상태인지. 상점 해금 대상이 아니면 항상 true.</summary>
        public static bool IsCookingIngredientUnlocked(CookingIngredientType type)
        {
            if (!ShopUnlockableIngredients.Contains(type))
            {
                return true;
            }

            GameState state = State;
            if (state == null)
            {
                // GameManager 없이 조리 씬만 단독 실행하는 경우 개발 편의상 막지 않는다.
                return true;
            }

            return TryGetCoreIngredientId(type, out int coreId) && state.IsIngredientUnlocked(coreId);
        }

        public static bool IsShopUnlockableIngredient(CookingIngredientType type)
        {
            return ShopUnlockableIngredients.Contains(type);
        }

        public static bool IsCoreIngredientUnlocked(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            if (ingredient.isDefaultUnlocked)
            {
                return true;
            }

            GameState state = State;
            return state == null || state.IsIngredientUnlocked(ingredient.id);
        }

        public static bool TryGetCoreIngredientId(CookingIngredientType type, out int coreIngredientId)
        {
            return CookingToCoreIngredientId.TryGetValue(type, out coreIngredientId);
        }

        public static int GetFryerLevel()
        {
            GameState state = State;
            return state != null ? state.GetUpgradeLevel(FryerUpgradeId) : 0;
        }

        public static int GetGrillLevel()
        {
            GameState state = State;
            return state != null ? state.GetUpgradeLevel(GrillUpgradeId) : 0;
        }

        public static float GetGrillCookTimeMultiplier()
        {
            ShopCatalog catalog = ShopCatalog.LoadDefault();
            UpgradeData grill = catalog != null ? catalog.GetUpgrade(GrillUpgradeId) : null;
            return GetCookTimeMultiplier(grill, GetGrillLevel());
        }

        public static float GetGrillBurnChance()
        {
            ShopCatalog catalog = ShopCatalog.LoadDefault();
            UpgradeData grill = catalog != null ? catalog.GetUpgrade(GrillUpgradeId) : null;
            return GetBurnChance(grill, GetGrillLevel());
        }

        /// <summary>
        /// 설비 레벨에 따른 조리 시간 배율. 해당 UpgradeData의 timeReduction 표에서 읽는다.
        /// 표가 없거나 레벨을 못 찾으면 1(=배율 없음)을 준다.
        /// </summary>
        public static float GetCookTimeMultiplier(UpgradeData upgrade, int level)
        {
            if (upgrade == null || upgrade.timeReduction == null || upgrade.timeReduction.Count == 0)
            {
                return 1f;
            }

            int index = Mathf.Clamp(level, 0, upgrade.timeReduction.Count - 1);
            float multiplier = upgrade.timeReduction[index];

            return multiplier > 0f ? multiplier : 1f;
        }

        /// <summary>
        /// 그릴판 레벨에 따른 탄 확률. 표가 없으면 0을 준다.
        /// </summary>
        public static float GetBurnChance(UpgradeData grillUpgrade, int level)
        {
            if (grillUpgrade == null || grillUpgrade.burnChancePerLevel == null ||
                grillUpgrade.burnChancePerLevel.Count == 0)
            {
                return 0f;
            }

            int index = Mathf.Clamp(level, 0, grillUpgrade.burnChancePerLevel.Count - 1);
            return Mathf.Clamp01(grillUpgrade.burnChancePerLevel[index]);
        }
    }
}
