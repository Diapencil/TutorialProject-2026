# SheepSheep Burger Economy and Shop

This module implements the current economy notes.

## Confirmed Rules

- Debt: 1500C due by day 30.
- The player can repay any chosen amount of debt from the shop before day 30.
- Customers per day: 8.
- Burger price range: 5C to 15C.
- Hamburger price: 5C.
- Satisfaction payout:
  - Excellent, exact recipe: burger price + 5C tip.
  - Good, one mismatch: burger price + 3C tip.
  - Normal, two mismatches: burger price only.
  - Terrible, three or more mismatches: customer does not pay.
- Ingredient costs are charged by actual use:
  - Cooked ingredient: 0.3C.
  - Fresh or non-cooked ingredient: 0.2C.
- Ingredients stay unlocked permanently after purchase.
- Store repair can cost up to 500C depending on damage severity.
- Medical treatment costs 100C.
- Fryer and grill plate upgrades each have four levels.
- Grill plate burn chance moves from 30% to 20%, 10%, then 0% as upgraded.
- Decorations cost 100C to 500C and are blocked until debt is cleared.

## Included Catalogs

- `RecipeCatalog` contains the 10 burger recipes from the provided table.
- `CustomerCatalog` contains Lion, Wolf, Elephant, and Giraffe preferences and appearance weights.
- `ShopCatalog` contains topping unlocks, tool upgrades, repair/medical entries, and decorations.
- `ShopScreenController` contains a Debt tab with a numeric repayment input, Pay Debt button, and Max button.

## Integration Points

- Use `EconomyService.EvaluateBurger(recipeId, burgerData)` after `BurgerAssemblyController.OnBurgerCompleted`.
- Use `EconomyService.TryRecordServedOrder(state, evaluation)` after each served customer.
- Use `EconomyService.RecordRepairDamage(state, severity)` when a customer breaks something.
- Use `EconomyService.RecordMedicalBill(state)` when treatment cost should be charged.
- Use `EconomyService.CloseDay(state)` after the eighth customer or the day-end flow.
- Use `EconomyService.TryBuyItem(state, itemId)` from shop buttons for ingredients, repairs, medical care, and decorations.
- Use `EconomyService.TryBuyToolUpgrade(state, type, out data)` for fryer and grill upgrades.
- Use `EconomyService.TryPayDebt(state, amount, out paid)` when the player enters a debt repayment amount.
- Use `PlayerPrefsEconomyStore` for a simple local save, or replace it with a project save system later.

## Editor Verification

- `Sheep Sheep Burger > Verify Economy System` runs model assertions.
- `Sheep Sheep Burger > Build Shop Prototype Scene` creates `Assets/@Developers/Yu/Economy/Scenes/ShopPrototype.unity`.
