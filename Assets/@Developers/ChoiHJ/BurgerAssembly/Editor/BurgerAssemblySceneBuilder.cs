using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.BurgerAssembly.Editor
{
    public static class BurgerAssemblySceneBuilder
    {
        private const string SceneDirectory = "Assets/@Developers/ChoiHJ/BurgerAssembly/Scenes";
        private const string ScenePath = SceneDirectory + "/BurgerAssembly.unity";

        [MenuItem("Sheep Sheep Burger/Build Burger Assembly Scene")]
        public static void BuildAndVerify()
        {
            VerifyModel();
            VerifyRuntimeInterface();
            BuildScene();
            Debug.Log("[BurgerAssembly] Scene build and model verification completed successfully.");
        }

        private static void VerifyRuntimeInterface()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controllerObject = new GameObject("BurgerAssemblyVerification", typeof(BurgerAssemblyController));
            BurgerAssemblyController controller = controllerObject.GetComponent<BurgerAssemblyController>();

            InvokePrivate(controller, "BuildInterface");
            InvokePrivate(controller, "RefreshControls");

            Require(GameObject.Find("CuttingBoard") != null, "The cutting board UI must be created.");
            Require(GameObject.Find("BottomBunButton") != null, "The bottom bun button must be created.");
            Require(GameObject.Find("TopBunButton") != null, "The top bun button must be created.");
            Require(GameObject.Find("IngredientPanel") != null, "The ingredient panel must be created.");
            Require(GameObject.Find("SaucePanel") != null, "The sauce panel must be created.");
            Require(GameObject.Find("GrillPanel") != null, "The grill panel must be created.");
            Require(GameObject.Find("GrillDropZone").GetComponent<GrillDropZone>() != null, "The grill drop zone must be configured.");

            DraggableBurgerItem bottomBun = GameObject.Find("BottomBunButton").GetComponent<DraggableBurgerItem>();
            DraggableBurgerItem topBun = GameObject.Find("TopBunButton").GetComponent<DraggableBurgerItem>();
            Require(bottomBun.CanDragNow, "The bottom bun must be draggable initially.");
            Require(!topBun.CanDragNow, "The top bun must not be draggable before assembly starts.");
            Require(GameObject.Find("RawPattyDrag").GetComponent<DraggableBurgerItem>().Kind == BurgerDragItemKind.RawPatty, "The ingredient panel must expose a raw patty drag source.");
        }

        private static void VerifyModel()
        {
            var state = new BurgerAssemblyState(3);
            Require(!state.TryAdd(BurgerIngredientId.Patty), "Ingredients must be rejected before the bottom bun.");
            Require(state.TryStart(), "The bottom bun must start assembly.");
            Require(!state.TryFinish(), "An empty burger must not finish.");
            Require(state.TryAdd(BurgerIngredientId.Patty), "First ingredient should be accepted.");
            Require(state.TryAdd(BurgerIngredientId.Cheese), "Second ingredient should be accepted.");
            Require(state.TryAdd(BurgerIngredientId.Ketchup), "Third ingredient should be accepted.");
            Require(!state.TryAdd(BurgerIngredientId.Tomato), "The configured layer limit must be enforced.");
            Require(state.TryFinish(), "The top bun must finish a burger with ingredients.");
            Require(!state.TryAdd(BurgerIngredientId.Tomato), "Ingredients must be rejected after completion.");

            var shuffledClassic = new List<BurgerIngredientId>
            {
                BurgerIngredientId.Tomato,
                BurgerIngredientId.Cheese,
                BurgerIngredientId.Ketchup,
                BurgerIngredientId.Lettuce,
                BurgerIngredientId.Patty
            };
            Require(BurgerRecipeCatalog.Classic.Matches(shuffledClassic), "Recipe matching must accept the correct ingredient multiset.");
            shuffledClassic.Add(BurgerIngredientId.Onion);
            Require(!BurgerRecipeCatalog.Classic.Matches(shuffledClassic), "Recipe matching must reject extra ingredients.");

            var grill = new PattyGrillState();
            Require(!grill.TryCook(), "An empty grill must not cook a patty.");
            Require(grill.TryLoadRawPatty(), "A raw patty must load onto an empty grill.");
            Require(!grill.TryLoadRawPatty(), "A busy grill must reject another raw patty.");
            Require(!grill.TryTakeCookedPatty(), "A raw patty must not be removable as cooked.");
            Require(grill.TryCook(), "A loaded raw patty must become cooked.");
            Require(grill.TryTakeCookedPatty(), "A cooked patty must be removable from the grill.");
            Require(grill.Phase == PattyGrillPhase.Empty, "The grill must be empty after taking the cooked patty.");
        }

        private static void BuildScene()
        {
            Directory.CreateDirectory(SceneDirectory);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BurgerAssembly";

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0.96f, 0.86f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var controllerObject = new GameObject("BurgerAssemblyGame", typeof(BurgerAssemblyController));
            controllerObject.transform.position = Vector3.zero;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath);
            }

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!string.Equals(existing.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("[BurgerAssembly verification] " + message);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            method.Invoke(target, null);
        }
    }
}
