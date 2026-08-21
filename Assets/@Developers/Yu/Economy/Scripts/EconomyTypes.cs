using System;
using System.Collections.Generic;
using SheepSheepBurger.BurgerAssembly;

namespace SheepSheepBurger.Economy
{
    public enum CustomerSatisfaction
    {
        Excellent,
        Good,
        Normal,
        Terrible
    }

    public enum DebtStatus
    {
        InProgress,
        DueToday,
        Cleared,
        Failed
    }

    public enum ShopCategory
    {
        Topping,
        Upgrade,
        Repair,
        Debt,
        Decoration
    }

    public enum ShopItemId
    {
        Bacon,
        FriedEgg,
        Pickle,
        Jalapeno,
        Tomato,
        Onion,
        FryerUpgrade,
        GrillPlateUpgrade,
        StoreRepair,
        MedicalCare,
        DecorationSmall,
        DecorationMedium,
        DecorationLarge,
        Lettuce
    }

    public enum RepairDamageSeverity
    {
        None,
        Minor,
        Moderate,
        Major,
        Severe
    }

    public enum ToolUpgradeType
    {
        Fryer,
        GrillPlate
    }

    public enum BurgerRecipeId
    {
        Hamburger,
        SoopSoopBurger,
        ThreePattyBurger,
        MiaBurger,
        VegetarianBurger,
        VeganBurger,
        JalakingBurger,
        Hotdog,
        DujjonkuBurger,
        WildBurger
    }

    public enum CustomerId
    {
        Lion,
        Wolf,
        Elephant,
        Giraffe
    }

    [Serializable]
    public sealed class RecipeData
    {
        public BurgerRecipeId id;
        public string recipeId;
        public string displayName;
        public float price;
        public List<IngredientType> requiredIngredients = new List<IngredientType>();

        public RecipeData()
        {
        }

        public RecipeData(BurgerRecipeId id, string displayName, float price, IEnumerable<IngredientType> ingredients)
        {
            this.id = id;
            this.recipeId = id.ToString();
            this.displayName = displayName;
            this.price = price;
            this.requiredIngredients = new List<IngredientType>(ingredients);
        }

        public RecipeData(string recipeId, IEnumerable<IngredientType> ingredients)
        {
            this.id = BurgerRecipeId.Hamburger;
            this.recipeId = recipeId;
            this.displayName = recipeId;
            this.price = EconomyRules.BasicBurgerPrice;
            this.requiredIngredients = new List<IngredientType>(ingredients);
        }

        public static RecipeData CreateBasicBurger()
        {
            return RecipeCatalog.CreateDefault().GetRecipe(BurgerRecipeId.Hamburger);
        }
    }

    [Serializable]
    public sealed class CustomerPreferenceData
    {
        public CustomerId id;
        public string displayName;
        public int appearanceWeight;
        public List<BurgerRecipeId> preferredRecipes = new List<BurgerRecipeId>();

        public CustomerPreferenceData()
        {
        }

        public CustomerPreferenceData(CustomerId id, string displayName, int appearanceWeight, IEnumerable<BurgerRecipeId> preferredRecipes)
        {
            this.id = id;
            this.displayName = displayName;
            this.appearanceWeight = appearanceWeight;
            this.preferredRecipes = new List<BurgerRecipeId>(preferredRecipes);
        }
    }

    [Serializable]
    public sealed class OrderEvaluation
    {
        public int mismatchCount;
        public CustomerSatisfaction satisfaction;
        public float basePayment;
        public float tip;
        public float ingredientCost;
        public float totalPayment;
        public bool customerPays;
    }

    [Serializable]
    public sealed class DaySettlement
    {
        public int dayNumber;
        public int customersServed;
        public float revenueEarned;
        public float tipsEarned;
        public int unpaidCustomers;
        public float materialCost;
        public float repairCost;
        public int repairIncidents;
        public float medicalCost;
        public int medicalIncidents;
        public float debtPaid;
        public float moneyAfterSettlement;
        public float debtRemaining;
        public DebtStatus debtStatus;
        public bool canContinue;
    }

    [Serializable]
    public sealed class RepairDamageEvent
    {
        public RepairDamageSeverity severity;
        public float cost;
        public string description;

        public RepairDamageEvent()
        {
        }

        public RepairDamageEvent(RepairDamageSeverity severity, float cost, string description)
        {
            this.severity = severity;
            this.cost = cost;
            this.description = description;
        }
    }

    [Serializable]
    public sealed class MedicalBillEvent
    {
        public float cost;
        public string description;

        public MedicalBillEvent()
        {
        }

        public MedicalBillEvent(float cost, string description)
        {
            this.cost = cost;
            this.description = description;
        }
    }

    [Serializable]
    public sealed class ToolUpgradeData
    {
        public ToolUpgradeType type;
        public int level;
        public int maxLevel;
        public float nextCost;
        public float speedMultiplier;
        public float burnChance;
    }

    [Serializable]
    public sealed class ShopItemData
    {
        public ShopItemId id;
        public ShopCategory category;
        public string displayName;
        public string flavorText;
        public float price;
        public bool unlocksIngredient;
        public IngredientType ingredientType;

        public ShopItemData()
        {
        }

        public ShopItemData(
            ShopItemId id,
            ShopCategory category,
            string displayName,
            string flavorText,
            float price,
            bool unlocksIngredient,
            IngredientType ingredientType)
        {
            this.id = id;
            this.category = category;
            this.displayName = displayName;
            this.flavorText = flavorText;
            this.price = price;
            this.unlocksIngredient = unlocksIngredient;
            this.ingredientType = ingredientType;
        }
    }

    [Serializable]
    public sealed class ShopPurchaseResult
    {
        public bool success;
        public string message;
        public ShopItemId itemId;
        public float moneyAfterPurchase;

        public static ShopPurchaseResult Succeeded(ShopItemData item, float moneyAfterPurchase)
        {
            return new ShopPurchaseResult
            {
                success = true,
                message = item.displayName + " unlocked",
                itemId = item.id,
                moneyAfterPurchase = moneyAfterPurchase
            };
        }

        public static ShopPurchaseResult Failed(ShopItemId itemId, string message, float moneyAfterPurchase)
        {
            return new ShopPurchaseResult
            {
                success = false,
                message = message,
                itemId = itemId,
                moneyAfterPurchase = moneyAfterPurchase
            };
        }
    }
}
