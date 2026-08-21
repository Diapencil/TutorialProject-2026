using System;
using System.Collections.Generic;
using Core.Data;
using System.IO;
using SheepSheepBurger.BurgerAssembly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.Economy.Editor
{
    public static class EconomySystemVerifier
    {
        private const string SceneDirectory = "Assets/@Developers/Yu/Economy/Scenes";
        private const string ScenePath = SceneDirectory + "/ShopPrototype.unity";

        [MenuItem("Sheep Sheep Burger/Verify Economy System")]
        public static void VerifyOnly()
        {
            VerifyModel();
            Debug.Log("[Economy] Economy and shop verification completed successfully.");
        }

        [MenuItem("Sheep Sheep Burger/Build Shop Prototype Scene")]
        public static void BuildShopPrototypeScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                VerifyModel();
                BuildScene();
                Debug.Log("[Economy] Shop prototype scene build completed successfully.");
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void VerifyModel()
        {
            ShopCatalog shopCatalog = ShopCatalog.CreateDefault();
            RecipeCatalog recipeCatalog = RecipeCatalog.CreateDefault();
            CustomerCatalog customerCatalog = CustomerCatalog.CreateDefault();
            var economy = new EconomyService(shopCatalog, recipeCatalog);
            PlayerEconomyState state = PlayerEconomyState.CreateNewGame();

            Require(Approximately(EconomyRules.StartingDebt, 1500f), "Starting debt must be 1500C.");
            Require(EconomyRules.DebtDeadlineDays == 30, "Debt deadline must be 30 days.");
            Require(EconomyRules.CustomersPerDay == 8, "A day must serve eight customers.");
            Require(recipeCatalog.Recipes.Count == 10, "The burger recipe table must contain ten recipes.");
            Require(customerCatalog.Customers.Count == 4, "The customer table must contain four customer profiles.");

            RecipeData hamburger = recipeCatalog.GetRecipe(BurgerRecipeId.Hamburger);
            Require(Approximately(hamburger.price, 5f), "Hamburger must cost 5C.");

            OrderEvaluation excellent = economy.EvaluateBurger(BurgerRecipeId.Hamburger, CreateBurger(
                IngredientType.BunBottom,
                IngredientType.Patty,
                IngredientType.ToppingCheese,
                IngredientType.ToppingPickle,
                IngredientType.SauceKetchup,
                IngredientType.SauceMustard,
                IngredientType.BunTop));
            Require(excellent.satisfaction == CustomerSatisfaction.Excellent, "Exact recipe must be Excellent.");
            Require(Approximately(excellent.totalPayment, 10f) && Approximately(excellent.tip, 5f), "Excellent must pay burger price plus 5C tip.");
            Require(Approximately(excellent.ingredientCost, 1.5f), "Hamburger ingredient cost must be 1.5C: one cooked and six fresh ingredients.");

            OrderEvaluation good = economy.EvaluateBurger(BurgerRecipeId.Hamburger, CreateBurger(
                IngredientType.BunBottom,
                IngredientType.Patty,
                IngredientType.ToppingCheese,
                IngredientType.ToppingPickle,
                IngredientType.SauceKetchup,
                IngredientType.BunTop));
            Require(good.satisfaction == CustomerSatisfaction.Good, "One missing ingredient must be Good.");
            Require(Approximately(good.totalPayment, 8f) && Approximately(good.tip, 3f), "Good must pay burger price plus 3C tip.");

            OrderEvaluation normal = economy.EvaluateBurger(BurgerRecipeId.Hamburger, CreateBurger(
                IngredientType.BunBottom,
                IngredientType.Patty,
                IngredientType.ToppingCheese,
                IngredientType.ToppingPickle,
                IngredientType.BunTop));
            Require(normal.satisfaction == CustomerSatisfaction.Normal, "Two mismatches must be Normal.");
            Require(Approximately(normal.totalPayment, 5f), "Normal must pay burger price only.");

            OrderEvaluation terrible = economy.EvaluateBurger(BurgerRecipeId.Hamburger, CreateBurger(
                IngredientType.ToppingBacon,
                IngredientType.ToppingFriedEgg,
                IngredientType.ToppingJalapeno));
            Require(terrible.satisfaction == CustomerSatisfaction.Terrible, "Three or more mismatches must be Terrible.");
            Require(Approximately(terrible.totalPayment, 0f) && !terrible.customerPays, "Terrible customers must not pay.");

            for (int index = 0; index < EconomyRules.CustomersPerDay; index++)
            {
                Require(economy.TryRecordServedOrder(state, excellent), "The first eight customers must be accepted.");
            }
            Require(!economy.TryRecordServedOrder(state, excellent), "A ninth same-day customer must be rejected.");
            Require(Approximately(state.money, 80f), "Eight excellent hamburger orders should earn 80C before day costs.");
            Require(Approximately(state.materialCostToday, 12f), "Eight hamburgers should accumulate 12C in ingredient costs.");

            state.money = EconomyRules.DefaultShopItemPrice;
            ShopPurchaseResult purchase = economy.TryBuyItem(state, ShopItemId.Bacon);
            Require(purchase.success && state.HasPurchased(ShopItemId.Bacon), "Bacon should be purchasable and permanently unlocked at 500C.");
            Require(shopCatalog.GetUnlockedIngredients(state).Contains(IngredientType.ToppingBacon), "Purchased bacon must unlock unlimited bacon use.");

            state.money = 1000f;
            Require(economy.TryBuyToolUpgrade(state, ToolUpgradeType.GrillPlate, out ToolUpgradeData grill), "Grill should be upgradeable.");
            Require(grill.level == 1 && Approximately(grill.burnChance, 0.2f), "Grill level 1 must reduce burn chance from 30% to 20%.");

            RepairDamageEvent damage = economy.RecordRepairDamage(state, RepairDamageSeverity.Severe, "Customer broke the shop.");
            Require(Approximately(damage.cost, EconomyRules.FullStoreRepairCost), "Severe damage must cost the full 500C repair fee.");
            MedicalBillEvent medical = economy.RecordMedicalBill(state);
            Require(Approximately(medical.cost, EconomyRules.MedicalCareCost), "Treatment fee must be 100C.");

            float beforeClose = state.money;
            DaySettlement settlement = economy.CloseDay(state);
            Require(Approximately(settlement.materialCost, state.materialCostToday), "Day settlement must use accumulated material costs.");
            Require(Approximately(settlement.repairCost, EconomyRules.FullStoreRepairCost), "Day settlement must charge repair costs.");
            Require(Approximately(settlement.medicalCost, EconomyRules.MedicalCareCost), "Day settlement must charge medical costs.");
            Require(Approximately(settlement.moneyAfterSettlement, beforeClose - settlement.materialCost - settlement.repairCost - settlement.medicalCost), "Money after settlement must subtract material, repair, and medical costs.");
            Require(state.dayClosed, "Closing the day must lock the day until BeginNextDay.");

            state.BeginNextDay();
            Require(Approximately(state.materialCostToday, 0f) && Approximately(state.repairCostToday, 0f) && Approximately(state.medicalCostToday, 0f), "Daily cost counters must reset on the next day.");

            PlayerEconomyState repaymentState = PlayerEconomyState.CreateNewGame(300f);
            Require(economy.TryPayDebt(repaymentState, 125f, out float paidDebt), "Debt must be manually payable before the deadline.");
            Require(Approximately(paidDebt, 125f), "Manual debt payment must pay the requested amount when affordable.");
            Require(Approximately(repaymentState.money, 175f), "Manual debt payment must subtract money.");
            Require(Approximately(repaymentState.debtRemaining, 1375f), "Manual debt payment must reduce debt.");
            Require(economy.TryPayDebt(repaymentState, 999f, out float cappedDebtPayment), "Debt payment should use available money when requested amount is too high.");
            Require(Approximately(cappedDebtPayment, 175f), "Debt payment must cap at the player's available money.");
            Require(Approximately(repaymentState.money, 0f), "Capped debt payment must not make money negative.");

            PlayerEconomyState deadlineState = PlayerEconomyState.CreateNewGame(1700f);
            deadlineState.dayNumber = EconomyRules.DebtDeadlineDays;
            DaySettlement deadline = economy.CloseDay(deadlineState);
            Require(Approximately(deadline.debtPaid, EconomyRules.StartingDebt), "Deadline close should auto-pay debt when enough money is available.");
            Require(deadline.debtStatus == DebtStatus.Cleared, "Paid debt must be cleared.");
        }

        private static BurgerData CreateBurger(params IngredientType[] ingredients)
        {
            var placements = new List<IngredientPlacement>();
            for (int index = 0; index < ingredients.Length; index++)
            {
                placements.Add(new IngredientPlacement(ingredients[index], Vector2.zero, index));
            }

            return new BurgerData(placements);
        }

        private static void BuildScene()
        {
            Directory.CreateDirectory(SceneDirectory);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ShopPrototype";

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.71f, 0.85f, 0.65f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var shopObject = new GameObject("ShopScreen", typeof(ShopScreenController));
            shopObject.transform.position = Vector3.zero;
            var serializedController = new SerializedObject(shopObject.GetComponent<ShopScreenController>());
            serializedController.FindProperty("gameDatabase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) < 0.001f;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[Economy verification] " + message);
            }
        }
    }
}
