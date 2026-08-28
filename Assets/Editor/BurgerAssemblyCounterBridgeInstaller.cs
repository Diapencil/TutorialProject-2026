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
        // 실제 씬 경로. 예전 @Developers/ChoiHJ 경로는 비어 있어서 브릿지가 설치되지 않았다.
        public const string ScenePath = "Assets/Scenes/BurgerAssembly.unity";
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

            EnsureBridge(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 지금 열려 있는 씬에 카운터 복귀 브릿지를 붙이고 배선한다.
        /// 씬을 새로 굽는 경로(BurgerAssemblySceneBuilder)에서도 반드시 호출해야 한다.
        /// 호출한 쪽이 씬 저장을 책임진다.
        /// </summary>
        public static void EnsureBridge(BurgerAssemblyController controller)
        {
            if (controller == null)
            {
                Debug.LogError("[BurgerAssemblyCounterBridge] controller가 null이라 브릿지를 붙이지 못했습니다.");
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

            Debug.Log(missingAssets == 0
                ? "[BurgerAssemblyCounterBridge] 카운터 복귀 브릿지와 재료 매핑 13종을 연결했습니다."
                : $"[BurgerAssemblyCounterBridge] 브릿지는 붙였지만 재료 매핑 {missingAssets}건을 찾지 못했습니다. 위 로그를 확인하세요.");
        }
    }
}
