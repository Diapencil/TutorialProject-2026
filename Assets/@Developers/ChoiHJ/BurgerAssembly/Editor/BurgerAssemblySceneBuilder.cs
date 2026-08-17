using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly.Editor
{
    public static class BurgerAssemblySceneBuilder
    {
        private const string SceneDirectory = "Assets/@Developers/ChoiHJ/BurgerAssembly/Scenes";
        private const string AssemblyScenePath = SceneDirectory + "/BurgerAssembly.unity";
        private const string LegacyPackagingScenePath = SceneDirectory + "/BurgerPackaging.unity";
        private const string SpriteDirectory = "Assets/@Developers/ChoiHJ/BurgerAssembly/Sprites";
        private const string ProvidedArtDirectory = SpriteDirectory + "/ProvidedArt";
        private const string EnvironmentDirectory = SpriteDirectory + "/Environment";

        [MenuItem("Sheep Sheep Burger/Build Unified Cooking Scene")]
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
                BuildScenes();
                Debug.Log("[BurgerAssembly] Cooking, assembly, and packaging pages built and verified successfully.");
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        [MenuItem("Sheep Sheep Burger/Play Background-Aligned Scene")]
        public static void OpenAssemblySceneAndPlay()
        {
            EditorSceneManager.OpenScene(AssemblyScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = true;
                }
            };
        }

        private static void VerifyRuntimeInterface()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var existingEventSystemObject = new GameObject("ExistingEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            StandaloneInputModule legacyInputModule = existingEventSystemObject.GetComponent<StandaloneInputModule>();
            var controllerObject = new GameObject("CookingPrototypeVerification", typeof(BurgerAssemblyController));
            BurgerAssemblyController controller = controllerObject.GetComponent<BurgerAssemblyController>();
            controller.SetSpriteCatalog(CreateSpriteCatalog());
            BurgerData publishedBurger = null;
            controller.OnBurgerCompleted += burger => publishedBurger = burger;

            InvokePrivate(controller, "BuildInterface");
            InvokePrivate(controller, "RefreshControls");

            RequireFind("CookingCanvas");
            RequireFind("GrillPage");
            RequireFind("BoardPage");
            GameObject packagingPage = RequireFind("PackagingPage");
            RequireFind("GrillDropArea");
            RequireFind("BoardDropArea");
            RequireFind("IngredientTray");
            RequireFind("PackagingBoard");
            RequireFind("PackagingTray");
            Button leftTrashReset = RequireFind("LeftTrashReset").GetComponent<Button>();
            Button rightTrashReset = RequireFind("RightTrashReset").GetComponent<Button>();
            Require(
                leftTrashReset != null && rightTrashReset != null,
                "Both illustrated trash cans must reset the prototype when clicked.");
            CanvasScaler canvasScaler = RequireFind("CookingCanvas").GetComponent<CanvasScaler>();
            Require(
                canvasScaler != null &&
                canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                canvasScaler.referenceResolution == new Vector2(1920f, 1080f) &&
                Mathf.Approximately(canvasScaler.matchWidthOrHeight, 1f),
                "Cooking UI must scale from its 1920x1080 reference resolution.");
            Button packageButton = RequireFind("PackageButton").GetComponent<Button>();
            Require(packageButton != null && !packageButton.interactable, "Packaging button must remain disabled until the actual burger reaches the tray.");

            GameObject rawTray = RequireFind("RawPattySource");
            CookingTrayDragSource rawSource = rawTray.GetComponent<CookingTrayDragSource>();
            Require(rawSource != null && rawSource.Kind == CookingDragKind.RawGrillItem, "Raw patty tray source must create a grill item.");
            Require(
                rawTray.GetComponent<SimpleShapeGraphic>() != null &&
                Mathf.Approximately(rawTray.GetComponent<SimpleShapeGraphic>().color.a, 0f),
                "Ingredient sources must use the illustrated bins without a card background.");
            SimpleShapeGraphic rawPattyIcon = RequireFind("RawPattySourceIcon").GetComponent<SimpleShapeGraphic>();
            Require(rawPattyIcon != null && rawPattyIcon.SourceSprite != null, "Tray art must use a serialized Sprite reference.");
            SimpleShapeGraphic grillBackdrop = RequireFind("KitchenStationBackground").GetComponent<SimpleShapeGraphic>();
            Require(
                grillBackdrop != null && grillBackdrop.SourceSprite == controller.SpriteCatalog.KitchenStationBackground,
                "The cooking canvas must use the serialized kitchen-station background exactly once.");
            Require(
                grillBackdrop.transform.parent != null &&
                grillBackdrop.transform.parent.name == "CookingPageStrip" &&
                grillBackdrop.rectTransform.sizeDelta.x > canvasScaler.referenceResolution.x &&
                Mathf.Approximately(grillBackdrop.rectTransform.sizeDelta.y, canvasScaler.referenceResolution.y),
                "The kitchen background must fit the full reference height without vertical cropping.");
            Require(
                GameObject.Find("GrillPageEnvironmentBackdrop") == null &&
                GameObject.Find("BoardPageEnvironmentBackdrop") == null &&
                GameObject.Find("PackagingPageEnvironmentBackdrop") == null,
                "Cooking pages must not create duplicated or cropped environment backgrounds.");
            Require(
                GameObject.Find("GrillTitle") == null &&
                GameObject.Find("CookingGuide") == null &&
                GameObject.Find("BoardTitle") == null &&
                GameObject.Find("BoardStatusPanel") == null &&
                GameObject.Find("PackagingHelpPanel") == null,
                "Artwork-aligned stations must not be covered by explanatory panels.");
            CookingTrayDragSource baconSource = RequireFind("RawBaconSource").GetComponent<CookingTrayDragSource>();
            CookingTrayDragSource eggSource = RequireFind("RawEggSource").GetComponent<CookingTrayDragSource>();
            Require(
                baconSource != null &&
                baconSource.Kind == CookingDragKind.RawGrillItem &&
                baconSource.IngredientType == IngredientType.Bacon,
                "Bacon must be available from the grill tray.");
            Require(
                eggSource != null &&
                eggSource.Kind == CookingDragKind.RawGrillItem &&
                eggSource.IngredientType == IngredientType.Egg,
                "Egg must be available from the grill tray.");

            GameObject topBunTray = RequireFind("TopBunTray");
            CookingTrayDragSource topBunSource = topBunTray.GetComponent<CookingTrayDragSource>();
            Require(topBunSource != null && topBunSource.IngredientType == IngredientType.BunTop, "Top bun must be an infinite tray source.");

            CookingCameraSlider slider = RequireFind("CookingCanvas").GetComponent<CookingCameraSlider>();
            Require(slider != null, "Cooking canvas must provide same-scene grill/board/packaging camera sliding.");
            Require(slider.GrillX < slider.BoardX && slider.BoardX < slider.PackagingX, "Camera zones must be arranged grill, board, then packaging from left to right.");
            Require(slider.CanMoveToZone == null || slider.CanMoveToZone(CookingCameraZone.Packaging), "Packaging must be accessible before assembly is complete.");

            EventSystem eventSystem = existingEventSystemObject.GetComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            Require(inputModule == null, "An existing EventSystem input setup must not be replaced.");
            Require(legacyInputModule.enabled, "An existing enabled input module must remain enabled.");

            InvokePrivate(controller, "SpawnRawGrillItem", IngredientType.Patty, Vector2.zero);
            CookableGrillItemView patty = RequireFind("CookablePatty").GetComponent<CookableGrillItemView>();
            Require(patty != null && patty.State.Phase == PattyGrillPhase.RawDough, "Verification patty must start as raw dough.");
            Require(patty.GrillIngredientType == IngredientType.Patty, "Verification grill item must preserve its ingredient type.");
            Require(controller.TryBeginCookedGrillItemDrag(patty, Vector2.zero), "Raw dough must be movable before cooking starts.");
            InvokePrivate(controller, "CleanupPointerDrag");
            Require(patty.State.Phase == PattyGrillPhase.RawDough, "Dragging raw dough must preserve its cooking state.");

            RectTransform boardLayerRoot = RequireFind("BoardIngredientLayer").GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            Vector2 visibleBoardCenter = RectTransformUtility.WorldToScreenPoint(
                Camera.main,
                boardLayerRoot.position);
            Require(
                controller.TryBeginTrayDrag(
                    CookingDragKind.Ingredient,
                    IngredientType.BunBottom,
                    SimpleShape.Circle,
                    new Color(0.91f, 0.65f, 0.30f),
                    new Vector2(250f, 250f),
                    null,
                    visibleBoardCenter),
                "Bottom-bun tray drag must begin while the visible board overlaps the grill camera stop.");
            controller.EndTrayDrag(visibleBoardCenter);
            Require(
                GameObject.Find("BurgerStackRoot") != null,
                "Dropping the bottom bun on the visible board rectangle must create the burger stack regardless of the current camera stop.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.ToppingTomato, new Vector2(350f, 0f)), "A distant topping must remain loose on the board.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.ToppingJalapeno, new Vector2(40f, 20f)), "A nearby topping must keep its exact top-view drop position.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.ToppingJalapeno, new Vector2(-30f, 30f)), "A consecutive duplicate topping must join the same layer.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.ToppingOnion, new Vector2(110f, 0f)), "A topping near the stack edge must be corrected slightly inward.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.ToppingJalapeno, new Vector2(20f, -45f)), "A repeated topping separated by another type must start a new layer.");
            Require((bool)InvokePrivate(controller, "TryPlaceIngredient", IngredientType.BunTop, new Vector2(0f, 40f)), "A nearby top bun must complete the burger stack.");
            Require(
                publishedBurger != null && ReferenceEquals(publishedBurger, controller.LastCompletedBurger),
                "Completion publishing must preserve the controller event and LastCompletedBurger API.");
            GameObject stackRoot = RequireFind("BurgerStackRoot");
            PlacedIngredientView[] allPlacedIngredients = UnityEngine.Object
                .FindObjectsByType<PlacedIngredientView>(FindObjectsSortMode.None);
            PlacedIngredientView bottomBunView = allPlacedIngredients.Single(view =>
                view.IngredientType == IngredientType.BunBottom && view.IsStacked);
            PlacedIngredientView looseTomatoView = allPlacedIngredients.Single(view =>
                view.IngredientType == IngredientType.ToppingTomato && !view.IsStacked);
            PlacedIngredientView[] jalapenos = allPlacedIngredients
                .Where(view => view.IngredientType == IngredientType.ToppingJalapeno && view.IsStacked)
                .OrderBy(view => view.RectTransform.GetSiblingIndex())
                .ToArray();
            PlacedIngredientView onion = allPlacedIngredients.Single(view =>
                view.IngredientType == IngredientType.ToppingOnion && view.IsStacked);
            RectTransform bottomBun = bottomBunView.RectTransform;
            Require(looseTomatoView.RectTransform.parent != stackRoot.transform, "A distant ingredient must not join the burger stack.");
            Require(Mathf.Abs(looseTomatoView.RectTransform.anchoredPosition.x - 350f) < 0.01f, "A distant ingredient must keep its board position.");
            Require(bottomBun.parent == stackRoot.transform && jalapenos.All(view => view.RectTransform.parent == stackRoot.transform), "Placed ingredients must share the burger stack root.");
            Require((jalapenos[0].RectTransform.anchoredPosition - new Vector2(40f, 20f)).sqrMagnitude < 0.01f, "A central top-view placement must not be recentered.");
            Require(jalapenos[0].LayerOrder == jalapenos[1].LayerOrder, "Consecutive duplicate ingredients must share one layer.");
            Require(jalapenos[2].LayerOrder > onion.LayerOrder && onion.LayerOrder > jalapenos[1].LayerOrder, "A 1 -> 2 -> 1 sequence must create three distinct layers.");
            Require(onion.RectTransform.anchoredPosition.magnitude < 110f && onion.RectTransform.anchoredPosition.magnitude > 80f, "An edge placement must move only slightly toward the bun center.");
            Require(publishedBurger.ingredients.Select(item => item.layerOrder).Distinct().Count() == 5, "The completed top-view burger must contain the expected five logical layers including both buns.");
            Require(bottomBun.GetComponent<SimpleShapeGraphic>().raycastTarget, "Placed ingredients must remain individually draggable.");
            GameObject completedBurgerDragHandle = RequireFind("CompletedBurgerDragHandle");
            BurgerPackagingController packaging = packagingPage.GetComponent<BurgerPackagingController>();
            Require(packaging != null, "Packaging page must own its packaging controller.");
            RectTransform originalStackRoot = stackRoot.GetComponent<RectTransform>();
            Require(
                (bool)InvokePrivate(controller, "PlaceCompletedBurgerOnPackagingTray", Vector2.zero),
                "The completed burger object must be accepted by the packaging tray.");
            Require(originalStackRoot.parent == packaging.BurgerTray, "Packaging must reparent the original burger stack instead of generating a preview copy.");
            Require(!completedBurgerDragHandle.activeSelf, "A burger placed on the packaging tray must stop being draggable.");
            Require(packageButton.interactable, "Packaging button must enable after the actual burger is placed on the tray.");
            InvokePrivate(packaging, "PackageBurger");
            Require(packaging.IsPackaged && !packageButton.interactable, "Packaging must complete once and disable the button.");
            RequireFind("PackageWrap");
            float grillGuideAlpha = RequireFind("GrillDropArea").GetComponent<SimpleShapeGraphic>().color.a;
            float boardGuideAlpha = RequireFind("BoardDropArea").GetComponent<SimpleShapeGraphic>().color.a;
            float packagingGuideAlpha = RequireFind("PackagingTray").GetComponent<SimpleShapeGraphic>().color.a;
            bool temporaryGuidesMatchSetting = CookingPrototypeRules.ShowTemporaryInteractionAreas
                ? grillGuideAlpha > 0f && boardGuideAlpha > 0f && packagingGuideAlpha > 0f
                : Mathf.Approximately(grillGuideAlpha, 0f) &&
                    Mathf.Approximately(boardGuideAlpha, 0f) &&
                    Mathf.Approximately(packagingGuideAlpha, 0f);
            Require(
                temporaryGuidesMatchSetting &&
                Mathf.Approximately(RequireFind("PackagingBoard").GetComponent<SimpleShapeGraphic>().color.a, 0f),
                "Temporary grill, board, and packaging guides must match the shared visibility setting.");
            Require(
                RequireFind("PackagingBoardFrame").GetComponent<RectTransform>().anchoredPosition.y > -100f,
                "The packaging hit area must stay on the tabletop rather than the floor.");
            leftTrashReset.onClick.Invoke();
            Require(
                controller.LastCompletedBurger == null && !packaging.HasBurger && !packageButton.interactable,
                "Clicking either trash can must reset cooking, assembly, and packaging state.");
        }

        private static void VerifyModel()
        {
            var board = new BurgerAssemblyState(8);
            Require(!board.TryRegisterPlacement(IngredientType.ToppingCheese, out _), "Ingredients must be rejected until a bottom bun is placed.");
            Require(board.TryRegisterPlacement(IngredientType.BunBottom, out int bottomLayer) && bottomLayer == 0, "Bottom bun must establish the burger stack.");
            Require(!board.TryRegisterPlacement(IngredientType.BunBottom, out _), "A burger stack must reject duplicate bottom buns.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingCheese, out int firstLayer) && firstLayer == 1, "First topping should be accepted.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingCheese, out int secondLayer) && secondLayer == firstLayer, "Consecutive duplicate toppings must share a layer.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingTomato, out int tomatoLayer) && tomatoLayer > secondLayer, "A different topping must create the next layer.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingCheese, out int repeatedCheeseLayer) && repeatedCheeseLayer > tomatoLayer, "A repeated topping after another type must create a fresh layer.");
            Require(board.TryUnregisterPlacement(IngredientType.ToppingCheese), "A moved topping must be removable from the stack model.");
            Require(board.TryRegisterPlacement(IngredientType.ToppingPickle, out _), "Removing a topping must free one topping slot.");
            Require(board.TryRegisterPlacement(IngredientType.Patty, out _), "Patty must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.Bacon, out _), "Bacon must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.Egg, out _), "Egg must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.SauceKetchup, out _), "Sauce stamps must not count toward the topping limit.");
            Require(board.TryRegisterPlacement(IngredientType.BunTop, out int topLayer), "Top bun must be accepted after the bottom bun.");

            var limitedBoard = new BurgerAssemblyState(2);
            Require(limitedBoard.TryRegisterPlacement(IngredientType.BunBottom, out _), "Limited board must accept its bottom bun.");
            Require(limitedBoard.TryRegisterPlacement(IngredientType.ToppingCheese, out _), "Limited board must accept its first topping.");
            Require(limitedBoard.TryRegisterPlacement(IngredientType.ToppingCheese, out _), "The topping limit counts items even when they share a layer.");
            Require(!limitedBoard.TryRegisterPlacement(IngredientType.ToppingTomato, out _), "Configured topping limit must be enforced independently from layer grouping.");

            var placements = new List<IngredientPlacement>
            {
                new IngredientPlacement(IngredientType.BunBottom, Vector2.zero, bottomLayer),
                new IngredientPlacement(IngredientType.BunTop, new Vector2(25f, 30f), topLayer),
                new IngredientPlacement(IngredientType.ToppingCheese, new Vector2(-10f, 5f), firstLayer)
            };
            Require(board.TryComplete(placements, out BurgerData burgerData), "Top bun drop must complete the burger.");
            Require(burgerData.ingredients.Count == 3, "BurgerData must capture every scanned board placement.");
            Require(burgerData.ingredients[0].layerOrder < burgerData.ingredients[1].layerOrder, "BurgerData must be sorted by layer order.");
            Require(!board.TryRegisterPlacement(IngredientType.BunBottom, out _), "Completed board must reject new placements.");

            var incompleteBoard = new BurgerAssemblyState();
            Require(!incompleteBoard.TryRegisterPlacement(IngredientType.BunTop, out _), "Top bun must be rejected on an empty board.");
            Require(!incompleteBoard.TryComplete(Array.Empty<IngredientPlacement>(), out _), "A burger without both buns must not complete.");

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
            Require(patty.Phase == PattyGrillPhase.Overcooked && patty.CanDragToBoard, "Burnt ingredients must remain movable.");

            var unattendedPatty = new PattyGrillState();
            Require(unattendedPatty.TryPressDough(), "Unattended patty must start cooking after being pressed.");
            unattendedPatty.Tick(CookingPrototypeRules.FirstSideCookSeconds);
            Require(unattendedPatty.Phase == PattyGrillPhase.ReadyToFlip, "First-side completion must enter the flip window.");
            unattendedPatty.Tick(CookingPrototypeRules.ReadyToFlipBurnSeconds - 0.01f);
            Require(unattendedPatty.Phase == PattyGrillPhase.ReadyToFlip, "Patty must remain flippable during the five-second window.");
            unattendedPatty.Tick(0.01f);
            Require(unattendedPatty.Phase == PattyGrillPhase.Overcooked, "Patty must burn when the five-second flip window expires.");

            var egg = new PattyGrillState(IngredientType.Egg);
            Require(!egg.RequiresFlip, "Egg must use the one-sided cooking profile.");
            Require(egg.TryPressDough(), "Egg tap must start cooking.");
            egg.Tick(CookingPrototypeRules.FirstSideCookSeconds);
            Require(egg.Phase == PattyGrillPhase.Done && egg.CanDragToBoard, "Egg must finish after one three-second cooking phase.");
        }

        private static void BuildScenes()
        {
            Directory.CreateDirectory(SceneDirectory);
            BuildAssemblyScene();

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(AssemblyScenePath, true)
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (!string.Equals(existing.path, AssemblyScenePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existing.path, LegacyPackagingScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildAssemblyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BurgerAssembly";

            CreateCamera(new Color(1f, 0.95f, 0.84f));

            var controllerObject = new GameObject("BurgerAssemblyGame", typeof(BurgerAssemblyController));
            controllerObject.transform.position = Vector3.zero;
            controllerObject.GetComponent<BurgerAssemblyController>().SetSpriteCatalog(CreateSpriteCatalog());

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AssemblyScenePath))
            {
                throw new InvalidOperationException("Failed to save " + AssemblyScenePath);
            }
        }

        private static void CreateCamera(Color backgroundColor)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static BurgerSpriteCatalog CreateSpriteCatalog()
        {
            var catalog = new BurgerSpriteCatalog();
            catalog.ConfigureSharedUi(
                LoadSprite(SpriteDirectory + "/UI/rectangle.png"),
                LoadSprite(SpriteDirectory + "/UI/circle.png"),
                LoadSprite(SpriteDirectory + "/UI/triangle.png"),
                LoadSprite(SpriteDirectory + "/UI/rounded_rectangle.png"));
            catalog.ConfigureEnvironment(
                LoadSprite(EnvironmentDirectory + "/kitchen_station_reference.png"));
            catalog.ConfigureCooking(
                LoadSprite(ProvidedArtDirectory + "/patty_ball.png"),
                LoadSprite(ProvidedArtDirectory + "/patty_raw.png"),
                LoadSprite(ProvidedArtDirectory + "/patty_cooked.png"),
                LoadSprite(ProvidedArtDirectory + "/patty_burnt.png"),
                LoadSprite(ProvidedArtDirectory + "/bacon_pile.png"),
                LoadSprite(ProvidedArtDirectory + "/bacon_raw.png"),
                LoadSprite(ProvidedArtDirectory + "/bacon_cooked.png"),
                LoadSprite(ProvidedArtDirectory + "/bacon_burnt.png"),
                LoadSprite(ProvidedArtDirectory + "/egg_carton.png"),
                LoadSprite(ProvidedArtDirectory + "/egg_raw.png"),
                LoadSprite(ProvidedArtDirectory + "/egg_cooked.png"),
                LoadSprite(ProvidedArtDirectory + "/egg_burnt.png"));
            catalog.ConfigureAssembly(
                LoadSprite(SpriteDirectory + "/Ingredients/bun_bottom.png"),
                LoadSprite(ProvidedArtDirectory + "/bun_top.png"),
                LoadSprite(ProvidedArtDirectory + "/lettuce_topdown.png"),
                LoadSprite(ProvidedArtDirectory + "/lettuce_topdown.png"),
                LoadSprite(ProvidedArtDirectory + "/tomato_slice.png"),
                LoadSprite(ProvidedArtDirectory + "/tomato_pile.png"),
                LoadSprite(SpriteDirectory + "/Ingredients/cheese.png"),
                LoadSprite(ProvidedArtDirectory + "/onion_slices.png"),
                LoadSprite(ProvidedArtDirectory + "/onion_pile.png"),
                LoadSprite(ProvidedArtDirectory + "/pickle_slices.png"),
                LoadSprite(ProvidedArtDirectory + "/pickle_pile.png"),
                LoadSprite(ProvidedArtDirectory + "/jalapeno_slices.png"),
                LoadSprite(ProvidedArtDirectory + "/jalapeno_pile.png"),
                LoadSprite(SpriteDirectory + "/Ingredients/ketchup.png"),
                LoadSprite(SpriteDirectory + "/Ingredients/mustard.png"),
                LoadSprite(ProvidedArtDirectory + "/burger_complete.png"));
            Require(catalog.IsConfigured, "Every burger Sprite must be assigned through the scene.");
            return catalog;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException("Sprite asset was not found at " + assetPath);
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

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }
            return method.Invoke(target, arguments);
        }
    }
}
