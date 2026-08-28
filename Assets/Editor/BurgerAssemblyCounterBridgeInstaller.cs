using SheepSheepBurger.Counter;
using SheepSheepBurger.BurgerAssembly;
using SheepSheepBurger.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BurgerAssemblyIngredientType = SheepSheepBurger.BurgerAssembly.IngredientType;
using CoreIngredientData = SheepSheepBurger.Core.IngredientData;

namespace SheepSheepBurger.Counter.Editor
{
    /// <summary>
    /// One-shot editor tool that wires a BurgerAssemblyCounterBridge into the
    /// BurgerAssembly scene: it locates the scene's BurgerAssemblyController,
    /// creates (or reuses) a bridge GameObject, and fills in the
    /// BurgerAssembly IngredientType -> Core.IngredientData mapping from the
    /// asset files under Assets/Data/Ingredients.
    /// </summary>
    public static class BurgerAssemblyCounterBridgeInstaller
    {
        // 실제 씬 경로. 예전 @Developers/ChoiHJ 경로는 존재하지 않아 설치가 조용히 실패했다.
        private const string ScenePath = "Assets/Scenes/BurgerAssembly.unity";
        private const string BridgeObjectName = "CounterReturnBridge";

        private static readonly (BurgerAssemblyIngredientType type, string assetPath)[] Mappings =
        {
            (BurgerAssemblyIngredientType.Patty, "Assets/Data/Ingredients/Patty.asset"),
            (BurgerAssemblyIngredientType.BunBottom, "Assets/Data/Ingredients/BunBottom.asset"),
            (BurgerAssemblyIngredientType.BunTop, "Assets/Data/Ingredients/BunTop.asset"),
            (BurgerAssemblyIngredientType.ToppingLettuce, "Assets/Data/Ingredients/Lettuce.asset"),
            (BurgerAssemblyIngredientType.ToppingTomato, "Assets/Data/Ingredients/Tomato.asset"),
            (BurgerAssemblyIngredientType.ToppingCheese, "Assets/Data/Ingredients/Cheese.asset"),
            (BurgerAssemblyIngredientType.ToppingOnion, "Assets/Data/Ingredients/onion.asset"),
            (BurgerAssemblyIngredientType.ToppingPickle, "Assets/Data/Ingredients/Pickle.asset"),
            (BurgerAssemblyIngredientType.SauceKetchup, "Assets/Data/Ingredients/Ketchup.asset"),
            (BurgerAssemblyIngredientType.SauceMustard, "Assets/Data/Ingredients/Mustard.asset"),
            (BurgerAssemblyIngredientType.Bacon, "Assets/Data/Ingredients/Bacon.asset"),
            (BurgerAssemblyIngredientType.Egg, "Assets/Data/Ingredients/egg.asset"),
            (BurgerAssemblyIngredientType.ToppingJalapeno, "Assets/Data/Ingredients/Jalapeno.asset")
        };

        [MenuItem("Sheep Sheep Burger/Wire Counter Return Bridge")]
        public static void InstallBridge()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var controller = Object.FindFirstObjectByType<BurgerAssemblyController>();
            if (controller == null)
            {
                Debug.LogError("[BurgerAssemblyCounterBridge] BurgerAssemblyController was not found in " + ScenePath);
                return;
            }

            var bridgeObject = GameObject.Find(BridgeObjectName);
            if (bridgeObject == null)
            {
                bridgeObject = new GameObject(BridgeObjectName);
            }

            var bridge = bridgeObject.GetComponent<BurgerAssemblyCounterBridge>();
            if (bridge == null)
            {
                bridge = bridgeObject.AddComponent<BurgerAssemblyCounterBridge>();
            }

            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("controller").objectReferenceValue = controller;

            var counterSceneNameProperty = serialized.FindProperty("counterSceneName");
            if (string.IsNullOrEmpty(counterSceneNameProperty.stringValue))
            {
                counterSceneNameProperty.stringValue = "Counter";
            }

            var returnDelayProperty = serialized.FindProperty("returnDelaySeconds");
            if (returnDelayProperty.floatValue <= 0f)
            {
                returnDelayProperty.floatValue = 1.5f;
            }

            var mapProperty = serialized.FindProperty("ingredientMap");
            mapProperty.arraySize = Mappings.Length;
            var missingAssets = 0;
            for (var index = 0; index < Mappings.Length; index++)
            {
                var (type, assetPath) = Mappings[index];
                var ingredient = AssetDatabase.LoadAssetAtPath<CoreIngredientData>(assetPath);
                if (ingredient == null)
                {
                    Debug.LogError($"[BurgerAssemblyCounterBridge] Could not load IngredientData at {assetPath}");
                    missingAssets++;
                    continue;
                }

                var element = mapProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("burgerAssemblyType").intValue = (int)type;
                element.FindPropertyRelative("coreIngredient").objectReferenceValue = ingredient;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bridge);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(missingAssets == 0
                ? "[BurgerAssemblyCounterBridge] Wired the controller reference and all 13 ingredient mappings, and saved the scene."
                : $"[BurgerAssemblyCounterBridge] Saved the scene, but {missingAssets} ingredient mapping(s) could not be resolved. Check the log above.");
        }
    }
}
