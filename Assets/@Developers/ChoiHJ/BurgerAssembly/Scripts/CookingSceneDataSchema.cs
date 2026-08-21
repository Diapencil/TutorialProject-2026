using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum Grade
    {
        Perfect = 0,
        Good = 1,
        Normal = 2,
        Bad = 3
    }

    [Serializable]
    public sealed class IngredientData
    {
        public int id;
        public string ingredientName;
        public IngredientType type;
        public float unlockCost;
        public float costPerUse;
        public bool isDefaultUnlocked;
        public bool grillable;
        public float cookTimeMin;
        public float cookTimeMax;

        public IngredientData()
        {
        }

        public IngredientData(
            int id,
            string ingredientName,
            IngredientType type,
            float unlockCost,
            float costPerUse,
            bool isDefaultUnlocked,
            bool grillable,
            float cookTimeMin,
            float cookTimeMax)
        {
            this.id = id;
            this.ingredientName = ingredientName;
            this.type = type;
            this.unlockCost = unlockCost;
            this.costPerUse = costPerUse;
            this.isDefaultUnlocked = isDefaultUnlocked;
            this.grillable = grillable;
            this.cookTimeMin = cookTimeMin;
            this.cookTimeMax = cookTimeMax;
        }

        public IngredientData Clone()
        {
            return new IngredientData(
                id,
                ingredientName,
                type,
                unlockCost,
                costPerUse,
                isDefaultUnlocked,
                grillable,
                cookTimeMin,
                cookTimeMax);
        }

        internal void Validate()
        {
            if (id < 0)
            {
                throw new InvalidOperationException("IngredientData.id must be zero or greater.");
            }
            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                throw new InvalidOperationException("IngredientData.ingredientName must not be empty.");
            }
            if (unlockCost < 0f || costPerUse < 0f)
            {
                throw new InvalidOperationException("Ingredient costs must not be negative.");
            }
            if (cookTimeMin < 0f || cookTimeMax < cookTimeMin)
            {
                throw new InvalidOperationException("Ingredient cooking time range is invalid.");
            }
            if (!grillable && (!Mathf.Approximately(cookTimeMin, 0f) || !Mathf.Approximately(cookTimeMax, 0f)))
            {
                throw new InvalidOperationException("A non-grillable ingredient cannot have a cooking time.");
            }
        }
    }

    [Serializable]
    public sealed class RecipeLayer
    {
        public int layerOrder;
        public int ingredientId;
        public int minimumCount;
        public int maximumCount;

        public RecipeLayer()
        {
        }

        public RecipeLayer(int layerOrder, int ingredientId, int minimumCount, int maximumCount)
        {
            this.layerOrder = layerOrder;
            this.ingredientId = ingredientId;
            this.minimumCount = minimumCount;
            this.maximumCount = maximumCount;
        }

        public RecipeLayer Clone()
        {
            return new RecipeLayer(layerOrder, ingredientId, minimumCount, maximumCount);
        }

        internal void Validate(HashSet<int> ingredientIds)
        {
            if (layerOrder < 0)
            {
                throw new InvalidOperationException("RecipeLayer.layerOrder must be zero or greater.");
            }
            if (!ingredientIds.Contains(ingredientId))
            {
                throw new InvalidOperationException("RecipeLayer refers to an unknown ingredient id: " + ingredientId);
            }
            if (minimumCount < 0 || maximumCount < minimumCount || maximumCount < 1)
            {
                throw new InvalidOperationException("RecipeLayer count range is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class RecipeData
    {
        public int id;
        public string recipeName;
        public List<RecipeLayer> ingredients = new List<RecipeLayer>();
        public float basePrice;
        public string unlockCondition;

        public RecipeData()
        {
        }

        public RecipeData(
            int id,
            string recipeName,
            IEnumerable<RecipeLayer> ingredients,
            float basePrice,
            string unlockCondition)
        {
            this.id = id;
            this.recipeName = recipeName;
            this.ingredients = ingredients == null
                ? throw new ArgumentNullException(nameof(ingredients))
                : ingredients.Select(layer => layer.Clone()).ToList();
            this.basePrice = basePrice;
            this.unlockCondition = unlockCondition ?? string.Empty;
        }

        public RecipeData Clone()
        {
            return new RecipeData(id, recipeName, ingredients, basePrice, unlockCondition);
        }

        internal void Validate(HashSet<int> ingredientIds)
        {
            if (id < 0)
            {
                throw new InvalidOperationException("RecipeData.id must be zero or greater.");
            }
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                throw new InvalidOperationException("RecipeData.recipeName must not be empty.");
            }
            if (basePrice < 0f)
            {
                throw new InvalidOperationException("RecipeData.basePrice must not be negative.");
            }
            if (ingredients == null || ingredients.Count == 0)
            {
                throw new InvalidOperationException("RecipeData must contain at least one RecipeLayer.");
            }

            var layerOrders = new HashSet<int>();
            foreach (RecipeLayer layer in ingredients)
            {
                if (layer == null)
                {
                    throw new InvalidOperationException("RecipeData cannot contain a null RecipeLayer.");
                }
                layer.Validate(ingredientIds);
                if (!layerOrders.Add(layer.layerOrder))
                {
                    throw new InvalidOperationException("RecipeData contains a duplicate layerOrder: " + layer.layerOrder);
                }
            }
        }
    }

    [Serializable]
    public sealed class GradeConfig
    {
        public Grade grade;
        public int maxErrors;
        public float tipAmount;
        public bool paysBasePrice;
        public bool requiresNoHint;

        public GradeConfig()
        {
        }

        public GradeConfig(
            Grade grade,
            int maxErrors,
            float tipAmount,
            bool paysBasePrice,
            bool requiresNoHint)
        {
            this.grade = grade;
            this.maxErrors = maxErrors;
            this.tipAmount = tipAmount;
            this.paysBasePrice = paysBasePrice;
            this.requiresNoHint = requiresNoHint;
        }

        public GradeConfig Clone()
        {
            return new GradeConfig(grade, maxErrors, tipAmount, paysBasePrice, requiresNoHint);
        }

        internal bool Accepts(int errorCount, bool usedHint)
        {
            return errorCount <= maxErrors && (!requiresNoHint || !usedHint);
        }

        internal void Validate()
        {
            if (maxErrors < 0 || tipAmount < 0f)
            {
                throw new InvalidOperationException("GradeConfig contains a negative threshold or tip.");
            }
        }
    }

    [Serializable]
    public sealed class PaymentResult
    {
        public Grade grade;
        public float basePrice;
        public float tip;
        public float ingredientCost;
        public float netIncome;
        public bool wasAttacked;

        public PaymentResult()
        {
        }

        public PaymentResult(
            Grade grade,
            float basePrice,
            float tip,
            float ingredientCost,
            bool wasAttacked)
        {
            this.grade = grade;
            this.basePrice = RoundCurrency(basePrice);
            this.tip = RoundCurrency(tip);
            this.ingredientCost = RoundCurrency(ingredientCost);
            netIncome = RoundCurrency(this.basePrice + this.tip - this.ingredientCost);
            this.wasAttacked = wasAttacked;
        }

        private static float RoundCurrency(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }
    }

    [Serializable]
    public sealed class CookingSceneSchema
    {
        private const int PrototypeRecipeId = 0;
        private const float PrototypeBasePrice = 5f;
        private const float CheapIngredientCost = 0.2f;
        private const float ExpensiveIngredientCost = 0.3f;

        public int defaultRecipeId;
        public List<IngredientData> ingredients = new List<IngredientData>();
        public List<RecipeData> recipes = new List<RecipeData>();
        public List<GradeConfig> gradeConfigs = new List<GradeConfig>();

        public bool IsConfigured =>
            ingredients != null && ingredients.Count > 0 &&
            recipes != null && recipes.Count > 0 &&
            gradeConfigs != null && gradeConfigs.Count > 0;

        public CookingSceneSchema Clone()
        {
            return new CookingSceneSchema
            {
                defaultRecipeId = defaultRecipeId,
                ingredients = ingredients.Select(item => item.Clone()).ToList(),
                recipes = recipes.Select(item => item.Clone()).ToList(),
                gradeConfigs = gradeConfigs.Select(item => item.Clone()).ToList()
            };
        }

        public IngredientData GetIngredient(IngredientType type)
        {
            IngredientData result = ingredients.FirstOrDefault(item => item.type == type);
            if (result == null)
            {
                throw new InvalidOperationException("No IngredientData is registered for " + type + ".");
            }
            return result;
        }

        public RecipeData GetRecipe(int recipeId)
        {
            RecipeData result = recipes.FirstOrDefault(item => item.id == recipeId);
            if (result == null)
            {
                throw new InvalidOperationException("No RecipeData is registered for id " + recipeId + ".");
            }
            return result;
        }

        public float CalculateIngredientCost(BurgerData burgerData)
        {
            if (burgerData == null)
            {
                throw new ArgumentNullException(nameof(burgerData));
            }

            float total = 0f;
            foreach (IngredientPlacement placement in burgerData.ingredients)
            {
                total += GetIngredient(placement.type).costPerUse;
            }
            foreach (SauceStrokeData stroke in burgerData.sauceStrokes)
            {
                total += GetIngredient(stroke.type).costPerUse;
            }
            return Mathf.Round(total * 100f) / 100f;
        }

        public PaymentResult Evaluate(
            BurgerData burgerData,
            int recipeId,
            int errorCount,
            bool usedHint,
            bool wasAttacked)
        {
            if (errorCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(errorCount));
            }

            Validate();
            RecipeData recipe = GetRecipe(recipeId);
            GradeConfig gradeConfig = gradeConfigs
                .OrderBy(config => config.maxErrors)
                .FirstOrDefault(config => config.Accepts(errorCount, usedHint));
            if (gradeConfig == null)
            {
                throw new InvalidOperationException("GradeConfig does not cover this cooking result.");
            }

            float paidBasePrice = gradeConfig.paysBasePrice ? recipe.basePrice : 0f;
            float paidTip = gradeConfig.paysBasePrice ? gradeConfig.tipAmount : 0f;
            return new PaymentResult(
                gradeConfig.grade,
                paidBasePrice,
                paidTip,
                CalculateIngredientCost(burgerData),
                wasAttacked);
        }

        public void Validate()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("CookingSceneSchema is not configured.");
            }

            var ingredientIds = new HashSet<int>();
            var ingredientTypes = new HashSet<IngredientType>();
            foreach (IngredientData ingredient in ingredients)
            {
                if (ingredient == null)
                {
                    throw new InvalidOperationException("CookingSceneSchema cannot contain a null IngredientData.");
                }
                ingredient.Validate();
                if (!ingredientIds.Add(ingredient.id))
                {
                    throw new InvalidOperationException("Duplicate IngredientData.id: " + ingredient.id);
                }
                if (!ingredientTypes.Add(ingredient.type))
                {
                    throw new InvalidOperationException("Duplicate IngredientData.type: " + ingredient.type);
                }
            }

            var recipeIds = new HashSet<int>();
            foreach (RecipeData recipe in recipes)
            {
                if (recipe == null)
                {
                    throw new InvalidOperationException("CookingSceneSchema cannot contain a null RecipeData.");
                }
                recipe.Validate(ingredientIds);
                if (!recipeIds.Add(recipe.id))
                {
                    throw new InvalidOperationException("Duplicate RecipeData.id: " + recipe.id);
                }
            }
            if (!recipeIds.Contains(defaultRecipeId))
            {
                throw new InvalidOperationException("defaultRecipeId does not refer to a registered recipe.");
            }

            var grades = new HashSet<Grade>();
            foreach (GradeConfig config in gradeConfigs)
            {
                if (config == null)
                {
                    throw new InvalidOperationException("CookingSceneSchema cannot contain a null GradeConfig.");
                }
                config.Validate();
                if (!grades.Add(config.grade))
                {
                    throw new InvalidOperationException("Duplicate GradeConfig.grade: " + config.grade);
                }
            }
            foreach (Grade grade in Enum.GetValues(typeof(Grade)))
            {
                if (!grades.Contains(grade))
                {
                    throw new InvalidOperationException("Missing GradeConfig for " + grade + ".");
                }
            }
        }

        public static CookingSceneSchema CreatePrototypeDefaults()
        {
            var schema = new CookingSceneSchema
            {
                defaultRecipeId = PrototypeRecipeId,
                ingredients = CreatePrototypeIngredients(),
                recipes = new List<RecipeData>
                {
                    new RecipeData(
                        PrototypeRecipeId,
                        "자유 버거",
                        new[]
                        {
                            new RecipeLayer(0, (int)IngredientType.BunBottom, 1, 1),
                            new RecipeLayer(1, (int)IngredientType.BunTop, 1, 1)
                        },
                        PrototypeBasePrice,
                        "prototype_freeform")
                },
                gradeConfigs = new List<GradeConfig>
                {
                    new GradeConfig(Grade.Perfect, 0, 1f, true, true),
                    new GradeConfig(Grade.Good, 1, 0.5f, true, true),
                    new GradeConfig(Grade.Normal, 2, 0f, true, false),
                    new GradeConfig(Grade.Bad, int.MaxValue, 0f, false, false)
                }
            };
            schema.Validate();
            return schema;
        }

        private static List<IngredientData> CreatePrototypeIngredients()
        {
            return new List<IngredientData>
            {
                CreateIngredient(IngredientType.Patty, "패티", ExpensiveIngredientCost, true, 6f),
                CreateIngredient(IngredientType.BunBottom, "하단 번", CheapIngredientCost),
                CreateIngredient(IngredientType.BunTop, "상단 번", CheapIngredientCost),
                CreateIngredient(IngredientType.ToppingLettuce, "양상추", CheapIngredientCost),
                CreateIngredient(IngredientType.ToppingTomato, "토마토", CheapIngredientCost),
                CreateIngredient(IngredientType.ToppingCheese, "치즈", ExpensiveIngredientCost),
                CreateIngredient(IngredientType.ToppingOnion, "양파", CheapIngredientCost),
                CreateIngredient(IngredientType.ToppingPickle, "피클", CheapIngredientCost),
                CreateIngredient(IngredientType.SauceKetchup, "케첩", CheapIngredientCost),
                CreateIngredient(IngredientType.SauceMustard, "머스터드", CheapIngredientCost),
                CreateIngredient(IngredientType.Bacon, "베이컨", ExpensiveIngredientCost, true, 6f),
                CreateIngredient(IngredientType.Egg, "계란", ExpensiveIngredientCost, true, 3f),
                CreateIngredient(IngredientType.ToppingJalapeno, "할라피뇨", CheapIngredientCost)
            };
        }

        private static IngredientData CreateIngredient(
            IngredientType type,
            string ingredientName,
            float costPerUse,
            bool grillable = false,
            float cookTime = 0f)
        {
            return new IngredientData(
                (int)type,
                ingredientName,
                type,
                0f,
                costPerUse,
                true,
                grillable,
                cookTime,
                cookTime);
        }
    }
}
