using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    internal sealed class BurgerAssemblyViewReferences
    {
        public Camera MainCamera { get; set; }

        public Font UiFont { get; set; }

        public Text CookingTimerText { get; set; }

        public RectTransform DragLayer { get; set; }

        public RectTransform GrillDropArea { get; set; }

        public RectTransform BoardLayerRoot { get; set; }

        public CookingCameraSlider CameraSlider { get; set; }

        public BurgerSauceDrawingController SauceDrawingController { get; set; }

        public BurgerPackagingController PackagingController { get; set; }

        public Text GrillStatusText { get; set; }

        public Text BoardStatusText { get; set; }

        public Text BoardSummaryText { get; set; }

        public Text ToastText { get; set; }

        public GameObject ToastObject { get; set; }

        public List<CookingTrayDragSource> TraySources { get; } = new List<CookingTrayDragSource>();
    }

    internal sealed class BurgerAssemblyViewBuilder
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float CanvasWorldScale = 0.01f;
        // The source station art is 3397 x 1440. Fitting it to the reference
        // height keeps the top and bottom of the kitchen visible on wide screens.
        private const float PanoramaWidth = ReferenceHeight * (3397f / 1440f);
        private const float GrillViewX = -315f;
        private const float BoardViewX = 0f;
        private const float PackagingViewX = 315f;

        private readonly BurgerAssemblyController controller;
        private readonly Action resetPrototype;
        private readonly BurgerAssemblyViewReferences view = new BurgerAssemblyViewReferences();

        private RectTransform canvasRoot;
        private RectTransform pageStrip;

        public BurgerAssemblyViewBuilder(BurgerAssemblyController controller, Action resetPrototype)
        {
            this.controller = controller != null
                ? controller
                : throw new ArgumentNullException(nameof(controller));
            this.resetPrototype = resetPrototype ?? throw new ArgumentNullException(nameof(resetPrototype));
        }

        public BurgerAssemblyViewReferences Build()
        {
            view.MainCamera = EnsureCamera();
            view.UiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
            if (view.UiFont == null)
            {
                view.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var canvasObject = new GameObject(
                "CookingCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CookingCameraSlider));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = view.MainCamera;
            canvas.planeDistance = Mathf.Max(1f, view.MainCamera.nearClipPlane + 0.1f);
            canvas.sortingOrder = 10;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Height matching guarantees that the full top and bottom of the
            // station remain visible on displays wider than 16:9.
            canvasScaler.matchWidthOrHeight = 1f;

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            Image swipeSurface = canvasObject.GetComponent<Image>();
            swipeSurface.color = BurgerPrototypeTheme.Background;
            swipeSurface.raycastTarget = true;

            var pageStripObject = new GameObject("CookingPageStrip", typeof(RectTransform));
            pageStrip = pageStripObject.GetComponent<RectTransform>();
            pageStrip.SetParent(canvasRoot, false);
            BurgerUiFactory.SetRect(
                pageStrip,
                Vector2.zero,
                new Vector2(PanoramaWidth, ReferenceHeight));
            CreateEnvironmentBackground();

            view.CameraSlider = canvasObject.GetComponent<CookingCameraSlider>();
            view.CameraSlider.Configure(pageStrip, GrillViewX, BoardViewX, PackagingViewX);

            RectTransform grillPage = CreatePage("GrillPage", new Vector2(GrillViewX, 0f));
            RectTransform boardPage = CreatePage("BoardPage", new Vector2(BoardViewX, 0f));
            RectTransform packagingPage = CreatePage("PackagingPage", new Vector2(PackagingViewX, 0f));
            BuildGrillPage(grillPage);
            BuildBoardPage(boardPage);
            view.PackagingController = packagingPage.gameObject.AddComponent<BurgerPackagingController>();
            view.PackagingController.Configure(packagingPage, view.UiFont);
            BuildTrashResetHotspots();
            BuildCookingTimerOverlay();

            GameObject dragLayerObject = new GameObject("DragLayer", typeof(RectTransform));
            view.DragLayer = dragLayerObject.GetComponent<RectTransform>();
            view.DragLayer.SetParent(canvasRoot, false);
            BurgerUiFactory.SetRect(
                view.DragLayer,
                Vector2.zero,
                new Vector2(ReferenceWidth, ReferenceHeight));
            view.DragLayer.SetAsLastSibling();

            EnsureEventSystem();
            return view;
        }

        private void BuildCookingTimerOverlay()
        {
            RectTransform panel = CreateRoundedPanel(
                "CookingTimerPanel",
                canvasRoot,
                new Color(0.08f, 0.09f, 0.08f, 0.78f),
                new Vector2(0f, 490f),
                new Vector2(190f, 64f),
                false,
                18f);
            view.CookingTimerText = CreateText(
                "CookingTimerText",
                panel,
                "01:00",
                34,
                FontStyle.Bold,
                Color.white,
                Vector2.zero,
                panel.sizeDelta);
            Outline outline = view.CookingTimerText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void BuildTrashResetHotspots()
        {
            CreateTrashResetHotspot(
                "LeftTrashReset",
                new Vector2(-1195f, -40f),
                new Vector2(120f, 380f));
            CreateTrashResetHotspot(
                "RightTrashReset",
                new Vector2(835f, -55f),
                new Vector2(120f, 380f));
        }

        private void CreateTrashResetHotspot(string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRoundedPanel(
                name,
                pageStrip,
                Color.clear,
                position,
                size,
                true,
                0f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Graphic>();
            button.onClick.AddListener(() => resetPrototype());
        }

        private void BuildGrillPage(RectTransform page)
        {
            // Positions are registered against the artwork itself. The illustrated
            // shelf is the source tray and the dark plate is the cooking surface.
            RectTransform tray = CreateStationTray(
                "RawGrillTray",
                page,
                new Vector2(-225f, 300f),
                new Vector2(470f, 130f));
            CreateTrayCard(
                tray,
                "RawPattySource",
                CookingDragKind.RawGrillItem,
                IngredientType.Patty,
                new Vector2(-140f, 0f),
                new Vector2(110f, 90f));
            CreateTrayCard(
                tray,
                "RawBaconSource",
                CookingDragKind.RawGrillItem,
                IngredientType.Bacon,
                Vector2.zero,
                new Vector2(110f, 90f));
            CreateTrayCard(
                tray,
                "RawEggSource",
                CookingDragKind.RawGrillItem,
                IngredientType.Egg,
                new Vector2(140f, 0f),
                new Vector2(110f, 90f));

            view.GrillDropArea = CreateRoundedPanel(
                "GrillDropArea",
                page,
                GetTemporaryInteractionAreaColor("#E74C3C4D"),
                new Vector2(-225f, -70f),
                new Vector2(680f, 580f),
                true,
                0f);
        }

        private void BuildBoardPage(RectTransform page)
        {
            // Ingredient sources sit directly inside the bins painted in the art.
            RectTransform tray = CreateStationTray(
                "IngredientTray",
                page,
                new Vector2(240f, 160f),
                new Vector2(900f, 400f));
            CreateBoardTrayCards(tray);

            RectTransform boardDropArea = CreateRoundedPanel(
                "BoardDropArea",
                page,
                GetTemporaryInteractionAreaColor("#18A9994D"),
                new Vector2(250f, -245f),
                new Vector2(900f, 280f),
                true,
                0f);

            GameObject layerRootObject = new GameObject("BoardIngredientLayer", typeof(RectTransform));
            view.BoardLayerRoot = layerRootObject.GetComponent<RectTransform>();
            view.BoardLayerRoot.SetParent(boardDropArea, false);
            BurgerUiFactory.SetRect(view.BoardLayerRoot, Vector2.zero, boardDropArea.sizeDelta);
            view.SauceDrawingController = boardDropArea.gameObject.AddComponent<BurgerSauceDrawingController>();

            // Keep only transient feedback. It has no panel or explanatory copy.
            view.ToastText = CreateText(
                "ToastText",
                page,
                string.Empty,
                24,
                FontStyle.Bold,
                Color.white,
                new Vector2(250f, 460f),
                new Vector2(620f, 54f));
            Outline toastOutline = view.ToastText.gameObject.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            toastOutline.effectDistance = new Vector2(2f, -2f);
            view.ToastObject = view.ToastText.gameObject;
            view.ToastObject.SetActive(false);
        }

        private void CreateBoardTrayCards(RectTransform tray)
        {
            foreach (BurgerTrayItemDefinition item in BurgerIngredientCatalog.GetBoardTrayItems())
            {
                CreateTrayCard(
                    tray,
                    item.ObjectName,
                    item.Kind,
                    item.Type,
                    item.Position,
                    item.CardSize);
            }
        }

        private Camera EnsureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.GetComponent<Camera>();
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = BurgerPrototypeTheme.Background;
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = ReferenceHeight * CanvasWorldScale * 0.5f;
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            }
            return mainCamera;
        }

        private void CreateEnvironmentBackground()
        {
            Sprite background = controller.SpriteCatalog.KitchenStationBackground;
            BurgerUiFactory.CreateShape(
                "KitchenStationBackground",
                pageStrip,
                SimpleShape.Rectangle,
                Color.white,
                Vector2.zero,
                new Vector2(PanoramaWidth, ReferenceHeight),
                false,
                background);
        }

        private RectTransform CreatePage(string name, Vector2 position)
        {
            return BurgerUiFactory.CreateImage(
                name,
                pageStrip,
                new Color(1f, 1f, 1f, 0f),
                position,
                new Vector2(ReferenceWidth, ReferenceHeight),
                false);
        }

        private RectTransform CreateStationTray(string name, RectTransform parent, Vector2 position, Vector2 size)
        {
            return CreateRoundedPanel(
                name,
                parent,
                new Color(1f, 1f, 1f, 0f),
                position,
                size,
                false,
                0f);
        }

        private CookingTrayDragSource CreateTrayCard(
            RectTransform parent,
            string name,
            CookingDragKind kind,
            IngredientType type,
            Vector2 position,
            Vector2 size)
        {
            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetTrayVisual(type);
            RectTransform card = CreateRoundedPanel(name, parent, Color.clear, position, size, true, 0f);
            CanvasGroup cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            float iconLimit = Mathf.Min(78f, size.y * 0.72f);
            float iconScale = Mathf.Min(
                iconLimit / Mathf.Max(1f, visual.Size.x),
                iconLimit / Mathf.Max(1f, visual.Size.y));
            Vector2 iconSize = visual.Size * iconScale;
            SimpleShapeGraphic trayIcon = BurgerUiFactory.CreateShape(
                name + "Icon",
                card,
                visual.Shape,
                visual.Color,
                Vector2.zero,
                iconSize,
                false,
                visual.SourceSprite);
            CookingTrayDragSource source = card.gameObject.AddComponent<CookingTrayDragSource>();
            source.Configure(
                controller,
                kind,
                type,
                visual.Shape,
                visual.Color,
                visual.Size,
                visual.SourceSprite,
                trayIcon);
            view.TraySources.Add(source);

            // 상점에서 아직 해금하지 않은 재료는 트레이 칸을 비워 둔다.
            // 상점 토핑 탭에서 구매하면 다음 진입 때 이 칸이 채워진다.
            // Core.IngredientType과 이름이 겹치므로 using 대신 전체 이름으로 부른다.
            if (!SheepSheepBurger.Core.ShopProgressBridge.IsCookingIngredientUnlocked(type))
            {
                cardGroup.alpha = 0f;
                cardGroup.interactable = false;
                cardGroup.blocksRaycasts = false;
                source.enabled = false;
            }

            return source;
        }

        private RectTransform CreateRoundedPanel(
            string name,
            RectTransform parent,
            Color color,
            Vector2 position,
            Vector2 size,
            bool raycastTarget,
            float cornerRadius)
        {
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(name, parent, SimpleShape.RoundedRectangle, color, position, size, raycastTarget);
            graphic.CornerRadius = cornerRadius;
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            Color border = BurgerPrototypeTheme.Border;
            border.a = color.a <= 0.01f ? 0f : border.a;
            outline.effectColor = border;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            return graphic.rectTransform;
        }

        private Text CreateText(
            string name,
            RectTransform parent,
            string value,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions)
        {
            return BurgerUiFactory.CreateText(name, parent, view.UiFont, value, size, style, color, position, dimensions);
        }

        private static Color GetTemporaryInteractionAreaColor(string color)
        {
            return CookingPrototypeRules.ShowTemporaryInteractionAreas
                ? BurgerPrototypeTheme.Hex(color)
                : Color.clear;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            bool createdEventSystem = false;
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
                createdEventSystem = true;
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            BaseInputModule[] existingModules = eventSystem.GetComponents<BaseInputModule>();
            bool hasConfiguredModule = false;
            foreach (BaseInputModule existingModule in existingModules)
            {
                if (existingModule != null && existingModule.enabled)
                {
                    hasConfiguredModule = true;
                    break;
                }
            }

            if (inputModule == null && (createdEventSystem || !hasConfiguredModule))
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (createdEventSystem)
            {
                eventSystem.pixelDragThreshold = Mathf.RoundToInt(CookingPrototypeRules.DragThresholdPixels);
            }

            bool shouldConfigureInputModule =
                inputModule != null &&
                (createdEventSystem || !hasConfiguredModule || inputModule.enabled);
            if (shouldConfigureInputModule)
            {
                inputModule.enabled = true;
                if (inputModule.actionsAsset == null || inputModule.point == null || inputModule.leftClick == null)
                {
                    inputModule.AssignDefaultActions();
                }
            }
        }
    }
}
