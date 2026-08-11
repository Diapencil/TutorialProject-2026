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
                Topping(ShopItemId.Bacon, "Bacon", "Sorry, pig!", IngredientType.ToppingBacon),
                Topping(ShopItemId.FriedEgg, "Fried Egg", "Sorry, chicken!", IngredientType.ToppingFriedEgg),
                Topping(ShopItemId.Pickle, "Pickle", "Not sorry, pickle!", IngredientType.ToppingPickle),
                Topping(ShopItemId.Jalapeno, "Jalapeno", "Not sorry, jalapeno!", IngredientType.ToppingJalapeno),
                Topping(ShopItemId.Tomato, "Tomato", "Unlock tomato.", IngredientType.ToppingTomato),
                Topping(ShopItemId.Onion, "Onion", "Unlock onion.", IngredientType.ToppingOnion),
                Upgrade(ShopItemId.FryerUpgrade, ToolUpgradeType.Fryer, "Fryer Upgrade", "Faster fried items, four levels."),
                Upgrade(ShopItemId.GrillPlateUpgrade, ToolUpgradeType.GrillPlate, "Grill Plate Upgrade", "Faster grill and lower burn chance."),
                RepairItem(ShopItemId.StoreRepair, "Store Repair", "Full shop repair.", EconomyRules.FullStoreRepairCost),
                RepairItem(ShopItemId.MedicalCare, "Medical Care", "Treatment fee.", EconomyRules.MedicalCareCost),
                Decoration(ShopItemId.DecorationSmall, "Small Decoration", "For debt-free players.", 100f),
                Decoration(ShopItemId.DecorationMedium, "Medium Decoration", "For debt-free players.", 300f),
                Decoration(ShopItemId.DecorationLarge, "Large Decoration", "For debt-free players.", 500f)
            });
        }

        private static ShopItemData Topping(
            ShopItemId id,
            string displayName,
            string flavorText,
            IngredientType ingredientType,
            float price = EconomyRules.DefaultShopItemPrice)
        {
            return new ShopItemData(id, ShopCategory.Topping, displayName, flavorText, price, true, ingredientType);
        }

        private static ShopItemData Upgrade(
            ShopItemId id,
            ToolUpgradeType type,
            string displayName,
            string flavorText)
        {
            return new ShopItemData(
                id,
                ShopCategory.Upgrade,
                displayName,
                flavorText,
                EconomyRules.GetToolUpgradeCost(type, 0),
                false,
                IngredientType.Patty);
        }

        private static ShopItemData RepairItem(
            ShopItemId id,
            string displayName,
            string flavorText,
            float price)
        {
            return new ShopItemData(id, ShopCategory.Repair, displayName, flavorText, price, false, IngredientType.Patty);
        }

        private static ShopItemData Decoration(
            ShopItemId id,
            string displayName,
            string flavorText,
            float price)
        {
            return new ShopItemData(id, ShopCategory.Decoration, displayName, flavorText, price, false, IngredientType.Patty);
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
