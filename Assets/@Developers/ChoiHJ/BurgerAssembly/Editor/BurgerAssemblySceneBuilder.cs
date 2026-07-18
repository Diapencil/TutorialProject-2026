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

        [MenuItem("Sheep Sheep Burger/Build Cooking Prototype Scene")]
        public static void BuildAndVerify()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                VerifyModel();
                VerifyRuntimeInterface();
                BuildScene();
                Debug.Log("[BurgerAssembly] Cooking prototype scene build and verification completed successfully.");
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void VerifyRuntimeInterface()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controllerObject = new GameObject("CookingPrototypeVerification", typeof(BurgerAssemblyController));
            BurgerAssemblyController controller = controllerObject.GetComponent<BurgerAssemblyController>();

            InvokePrivate(controller, "BuildInterface");
            InvokePrivate(controller, "RefreshControls");

            RequireFind("CookingCanvas");
            RequireFind("GrillPage");
            RequireFind("BoardPage");
            RequireFind("GrillDropArea");
            RequireFind("BoardDropArea");
            RequireFind("IngredientTray");

            GameObject rawTray = RequireFind("RawPattySource");
            CookingTrayDragSource rawSource = rawTray.GetComponent<CookingTrayDragSource>();
            Require(rawSource != null && rawSource.Kind == CookingDragKind.RawPatty, "Raw patty tray source must create grill dough.");

            GameObject topBunTray = RequireFind("TopBunTray");
            CookingTrayDragSource topBunSource = topBunTray.GetComponent<CookingTrayDragSource>();
            Require(topBunSource != null && topBunSource.IngredientType == IngredientType.BunTop, "Top bun must be an infinite tray source.");

            CookingCameraSlider slider = RequireFind("CookingCanvas").GetComponent<CookingCameraSlider>();
            Require(slider != null, "Cooking canvas must provide same-scene grill/board camera sliding.");
        }

        private static void VerifyModel()
        {
            var board = new BurgerAssemblyState(2);
            Require(board.TryRegisterPlacement(IngredientType.ToppingCheese, out int firstLayer) && firstLayer == 0, "First topping should be accepted.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingCheese, out int secondLayer) && secondLayer == 1, "Duplicate toppings should be accepted.");
            Require(!board.TryRegisterPlacement(IngredientType.ToppingTomato, out _), "Configured topping limit must be enforced.");
            Require(board.TryRegisterPlacement(IngredientType.Patty, out _), "Patty must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.SauceKetchup, out _), "Sauce stamps must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.BunTop, out int topLayer), "Top bun must be accepted without requiring a bottom bun.");

            var placements = new List<IngredientPlacement>
            {
                new IngredientPlacement(IngredientType.BunTop, new Vector2(25f, 30f), topLayer),
                new IngredientPlacement(IngredientType.ToppingCheese, new Vector2(-10f, 5f), firstLayer)
            };
            Require(board.TryComplete(placements, out BurgerData burgerData), "Top bun drop must complete the burger.");
            Require(burgerData.ingredients.Count == 2, "BurgerData must capture every scanned board placement.");
            Require(burgerData.ingredients[0].layerOrder < burgerData.ingredients[1].layerOrder, "BurgerData must be sorted by layer order.");
            Require(!board.TryRegisterPlacement(IngredientType.BunBottom, out _), "Completed board must reject new placements.");

            var topOnlyBoard = new BurgerAssemblyState();
            Require(topOnlyBoard.TryRegisterPlacement(IngredientType.BunTop, out int topOnlyLayer), "Top bun must be placeable on an empty board.");
            Require(topOnlyBoard.TryComplete(
                new[] { new IngredientPlacement(IngredientType.BunTop, Vector2.zero, topOnlyLayer) },
                out BurgerData topOnlyBurger),
                "Missing bottom bun must not block cooking-part completion.");
            Require(topOnlyBurger.ingredients.Count == 1, "Top-only burger data must still be emitted.");

            var patty = new PattyGrillState();
            Require(!patty.TryFlip(), "Raw dough must ignore early flip input.");
            Require(patty.TryPressDough(), "Raw dough tap must flatten the patty.");
            Require(patty.Phase == PattyGrillPhase.Flattened, "Pressing must enter the flattened phase.");
            patty.Tick(0f);
            Require(patty.Phase == PattyGrillPhase.CookingSide1, "Flattened patty must begin first-side cooking.");
            patty.Tick(CookingPrototypeRules.FirstSideCookSeconds - 0.01f);
            Require(patty.Phase == PattyGrillPhase.CookingSide1 && !patty.TryFlip(), "Early flip input must remain ignored.");
            patty.Tick(0.01f);
            Require(patty.Phase == PattyGrillPhase.ReadyToFlip && patty.TryFlip(), "First side must become flippable after exactly three seconds.");
            patty.Tick(CookingPrototypeRules.FlipAnimationSeconds);
            Require(patty.Phase == PattyGrillPhase.CookingSide2, "Flip animation must lead to second-side cooking.");
            patty.Tick(CookingPrototypeRules.SecondSideCookSeconds);
            Require(patty.Phase == PattyGrillPhase.Done && patty.CanDragToBoard, "Second side must finish after three seconds.");
            patty.Tick(CookingPrototypeRules.DoneToOvercookedSeconds);
            Require(patty.Phase == PattyGrillPhase.Overcooked && !patty.CanDragToBoard, "Done patty must burn after five unattended seconds.");
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
            camera.backgroundColor = new Color(1f, 0.95f, 0.84f);
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

        private static GameObject RequireFind(string name)
        {
            GameObject found = GameObject.Find(name);
            Require(found != null, name + " must be created.");
            return found;
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
