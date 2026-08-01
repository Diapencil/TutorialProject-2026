using System;
using System.Collections.Generic;
using SheepSheepBurger.BurgerAssembly;

namespace SheepSheepBurger.Economy
{
    public sealed class ShopCatalog
    {
        private readonly List<ShopItemData> items;

        public ShopCatalog(IEnumerable<ShopItemData> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            items = new List<ShopItemData>(source);
        }

        public IList<ShopItemData> Items => items.AsReadOnly();

        public static ShopCatalog CreateDefault()
        {
            return new ShopCatalog(new[]
            {
                new ShopItemData(ShopItemId.Bacon, ShopCategory.Topping, "Bacon", "Sorry, pig!", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingBacon),
                new ShopItemData(ShopItemId.FriedEgg, ShopCategory.Topping, "Fried Egg", "Sorry, chicken!", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingFriedEgg),
                new ShopItemData(ShopItemId.Pickle, ShopCategory.Topping, "Pickle", "Not sorry, pickle!", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingPickle),
                new ShopItemData(ShopItemId.Jalapeno, ShopCategory.Topping, "Jalapeno", "Not sorry, jalapeno!", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingJalapeno),
                new ShopItemData(ShopItemId.Tomato, ShopCategory.Topping, "Tomato", "Unlock tomato.", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingTomato),
                new ShopItemData(ShopItemId.Onion, ShopCategory.Topping, "Onion", "Unlock onion.", EconomyRules.DefaultShopItemPrice, true, IngredientType.ToppingOnion),
                new ShopItemData(ShopItemId.FryerUpgrade, ShopCategory.Upgrade, "Fryer Upgrade", "Faster fried items, four levels.", EconomyRules.GetToolUpgradeCost(ToolUpgradeType.Fryer, 0), false, IngredientType.Patty),
                new ShopItemData(ShopItemId.GrillPlateUpgrade, ShopCategory.Upgrade, "Grill Plate Upgrade", "Faster grill and lower burn chance.", EconomyRules.GetToolUpgradeCost(ToolUpgradeType.GrillPlate, 0), false, IngredientType.Patty),
                new ShopItemData(ShopItemId.StoreRepair, ShopCategory.Repair, "Store Repair", "Full shop repair.", EconomyRules.FullStoreRepairCost, false, IngredientType.Patty),
                new ShopItemData(ShopItemId.MedicalCare, ShopCategory.Repair, "Medical Care", "Treatment fee.", EconomyRules.MedicalCareCost, false, IngredientType.Patty),
                new ShopItemData(ShopItemId.DecorationSmall, ShopCategory.Decoration, "Small Decoration", "For debt-free players.", 100f, false, IngredientType.Patty),
                new ShopItemData(ShopItemId.DecorationMedium, ShopCategory.Decoration, "Medium Decoration", "For debt-free players.", 300f, false, IngredientType.Patty),
                new ShopItemData(ShopItemId.DecorationLarge, ShopCategory.Decoration, "Large Decoration", "For debt-free players.", 500f, false, IngredientType.Patty)
            });
        }

        public bool TryGetItem(ShopItemId itemId, out ShopItemData item)
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (items[index].id == itemId)
                {
                    item = items[index];
                    return true;
                }
            }

            item = null;
            return false;
        }

        public List<ShopItemData> GetItems(ShopCategory category)
        {
            var result = new List<ShopItemData>();
            for (int index = 0; index < items.Count; index++)
            {
                if (items[index].category == category)
                {
                    result.Add(items[index]);
                }
            }

            return result;
        }

        public List<IngredientType> GetUnlockedIngredients(PlayerEconomyState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var unlocked = new List<IngredientType>
            {
                IngredientType.BunBottom,
                IngredientType.BunTop,
                IngredientType.Patty,
                IngredientType.ToppingCheese,
                IngredientType.ToppingPickle,
                IngredientType.ToppingLettuce,
                IngredientType.SauceKetchup,
                IngredientType.SauceMustard
            };

            for (int index = 0; index < items.Count; index++)
            {
                ShopItemData item = items[index];
                if (item.unlocksIngredient && state.HasPurchased(item.id) && !unlocked.Contains(item.ingredientType))
                {
                    unlocked.Add(item.ingredientType);
                }
            }

            return unlocked;
        }
    }
}
