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
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            Image swipeSurface = canvasObject.GetComponent<Image>();
            swipeSurface.color = new Color(1f, 1f, 1f, 0.001f);
            swipeSurface.raycastTarget = true;

            var pageStripObject = new GameObject("CookingPageStrip", typeof(RectTransform));
            pageStrip = pageStripObject.GetComponent<RectTransform>();
            pageStrip.SetParent(canvasRoot, false);
            BurgerUiFactory.SetRect(
                pageStrip,
                Vector2.zero,
                new Vector2(ReferenceWidth * 3f, ReferenceHeight));

            view.CameraSlider = canvasObject.GetComponent<CookingCameraSlider>();
            view.CameraSlider.Configure(pageStrip, -ReferenceWidth, 0f, ReferenceWidth);

            RectTransform grillPage = CreatePage("GrillPage", BurgerPrototypeTheme.GrillZone, new Vector2(-ReferenceWidth, 0f));
            RectTransform boardPage = CreatePage("BoardPage", BurgerPrototypeTheme.BoardZone, Vector2.zero);
            RectTransform packagingPage = CreatePage("PackagingPage", BurgerPrototypeTheme.PackagingZone, new Vector2(ReferenceWidth, 0f));
            BuildGrillPage(grillPage);
            BuildBoardPage(boardPage);
            view.PackagingController = packagingPage.gameObject.AddComponent<BurgerPackagingController>();
            view.PackagingController.Configure(packagingPage, view.UiFont);

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

        private void BuildGrillPage(RectTransform page)
        {
            CreateText("GrillTitle", page, "요리 화면 · 불판 구역", 42, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(-470f, 485f), new Vector2(850f, 70f));
            RectTransform swipeHint = CreateRoundedPanel(
                "GrillSwipeHintPanel",
                page,
                BurgerPrototypeTheme.Panel,
                new Vector2(0f, 415f),
                new Vector2(980f, 54f),
                false,
                24f);
            CreateText("GrillSwipeHint", swipeHint, "← 왼쪽으로 20% 이상 스와이프하면 도마 구역으로 이동", 20, FontStyle.Bold, BurgerPrototypeTheme.Ink, Vector2.zero, swipeHint.sizeDelta);

            RectTransform tray = CreatePanel("RawGrillTray", page, "굽기 재료 트레이 · 무한 공급", new Vector2(0f, 230f), new Vector2(1660f, 250f));
            CreateTrayCard(
                tray,
                "RawPattySource",
                "패티볼",
                CookingDragKind.RawGrillItem,
                IngredientType.Patty,
                new Vector2(-420f, -12f),
                new Vector2(280f, 145f));
            CreateTrayCard(
                tray,
                "RawBaconSource",
                "베이컨",
                CookingDragKind.RawGrillItem,
                IngredientType.Bacon,
                new Vector2(0f, -12f),
                new Vector2(280f, 145f));
            CreateTrayCard(
                tray,
                "RawEggSource",
                "계란",
                CookingDragKind.RawGrillItem,
                IngredientType.Egg,
                new Vector2(420f, -12f),
                new Vector2(280f, 145f));
            CreateText("RawTrayHelp", tray, "한 번에 하나씩 불판으로 드래그 · 재료를 탭해 조리 시작", 17, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, -108f), new Vector2(1000f, 30f));

            RectTransform grillFrame = CreateRoundedPanel("GrillFrame", page, BurgerPrototypeTheme.BoardEdge, new Vector2(0f, -170f), new Vector2(1660f, 590f), false, 32f);
            view.GrillDropArea = CreateRoundedPanel("GrillDropArea", grillFrame, BurgerPrototypeTheme.Grill, new Vector2(-260f, 0f), new Vector2(1060f, 530f), true, 28f);
            for (int index = -5; index <= 5; index++)
            {
                BurgerUiFactory.CreateImage("GrillBar" + index, view.GrillDropArea, BurgerPrototypeTheme.GrillBar, new Vector2(index * 85f, 0f), new Vector2(16f, 470f), false);
            }

            CreateText("GrillAreaLabel", view.GrillDropArea, "불판 · 패티, 베이컨 또는 계란을 여기에 드롭", 23, FontStyle.Bold, BurgerPrototypeTheme.Hex("#F5FCFF"), new Vector2(0f, 225f), new Vector2(760f, 45f));

            RectTransform guide = CreateRoundedPanel("CookingGuide", grillFrame, BurgerPrototypeTheme.Guide, new Vector2(570f, 0f), new Vector2(470f, 530f), false, 28f);
            CreateText("CookingGuideTitle", guide, "재료를 탭하면 조리 시작", 24, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, 210f), new Vector2(420f, 55f));
            view.GrillStatusText = CreateText("GrillStatus", guide, "패티·베이컨·계란 중 하나를 불판에 놓아 주세요.", 22, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, 115f), new Vector2(410f, 115f));
            CreateText(
                "CookingGuideRules",
                guide,
                "패티·베이컨: 1면 3초 → 5초 안에 뒤집기 → 2면 3초\n\n계란: 3초 단면 조리\n\n완료 후 5초 방치 시 탐 · 완성 재료는 오른쪽 끝으로 드래그",
                19,
                FontStyle.Bold,
                BurgerPrototypeTheme.Ink,
                new Vector2(0f, -90f),
                new Vector2(410f, 260f));
            CreateDebugResetButton(page, new Vector2(805f, 475f));
        }

        private void BuildBoardPage(RectTransform page)
        {
            CreateText("BoardTitle", page, "요리 화면 · 도마 구역", 42, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(-470f, 485f), new Vector2(850f, 70f));
            RectTransform swipeHint = CreateRoundedPanel(
                "BoardSwipeHintPanel",
                page,
                BurgerPrototypeTheme.Panel,
                new Vector2(0f, 415f),
                new Vector2(980f, 54f),
                false,
                24f);
            CreateText("BoardSwipeHint", swipeHint, "→ 오른쪽: 불판 복귀 · 왼쪽: 포장대로 이동 ←", 20, FontStyle.Bold, BurgerPrototypeTheme.Ink, Vector2.zero, swipeHint.sizeDelta);

            RectTransform tray = CreatePanel("IngredientTray", page, "재료 트레이 · 무한 공급", new Vector2(0f, 200f), new Vector2(1660f, 320f));
            CreateBoardTrayCards(tray);

            RectTransform boardFrame = CreateRoundedPanel("BoardFrame", page, BurgerPrototypeTheme.BoardEdge, new Vector2(0f, -240f), new Vector2(1660f, 430f), false, 32f);
            RectTransform boardDropArea = CreateRoundedPanel("BoardDropArea", boardFrame, BurgerPrototypeTheme.Board, Vector2.zero, new Vector2(1600f, 370f), true, 28f);
            CreateText("BoardAreaLabel", boardDropArea, "도마 · 하단 번을 놓으면 이후 재료가 자동으로 쌓입니다", 22, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, 155f), new Vector2(900f, 40f));

            GameObject layerRootObject = new GameObject("BoardIngredientLayer", typeof(RectTransform));
            view.BoardLayerRoot = layerRootObject.GetComponent<RectTransform>();
            view.BoardLayerRoot.SetParent(boardDropArea, false);
            BurgerUiFactory.SetRect(view.BoardLayerRoot, Vector2.zero, boardDropArea.sizeDelta);
            view.SauceDrawingController = boardDropArea.gameObject.AddComponent<BurgerSauceDrawingController>();

            RectTransform statusPanel = CreateRoundedPanel("BoardStatusPanel", page, BurgerPrototypeTheme.Panel, new Vector2(0f, 10f), new Vector2(1100f, 52f), false, 23f);
            view.BoardStatusText = CreateText("BoardStatus", statusPanel, "먼저 하단 번을 도마의 원하는 위치에 놓으세요.", 21, FontStyle.Bold, BurgerPrototypeTheme.Ink, Vector2.zero, statusPanel.sizeDelta);
            view.BoardSummaryText = CreateText("BoardSummary", page, "배치 0개 · 토핑 0/8 · 소스 0획", 21, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, -485f), new Vector2(1400f, 45f));
            RectTransform toastPanel = CreateRoundedPanel("Toast", page, new Color(0.55f, 0.12f, 0.08f, 0.92f), new Vector2(0f, -230f), new Vector2(650f, 70f), false, 28f);
            view.ToastText = CreateText("ToastText", toastPanel, string.Empty, 24, FontStyle.Bold, Color.white, Vector2.zero, toastPanel.sizeDelta);
            view.ToastObject = toastPanel.gameObject;
            view.ToastObject.SetActive(false);
            CreateDebugResetButton(page, new Vector2(805f, 475f));
        }

        private void CreateBoardTrayCards(RectTransform tray)
        {
            foreach (BurgerTrayItemDefinition item in BurgerIngredientCatalog.GetBoardTrayItems())
            {
                CreateTrayCard(
                    tray,
                    item.ObjectName,
                    item.Label,
                    item.Kind,
                    item.Type,
                    item.Position,
                    item.CardSize);
            }
            CreateText("TrayLimit", tray, "토핑 최대 8개 · 구운 패티/베이컨/계란/번/소스 제외", 16, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, -142f), new Vector2(900f, 28f));
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

        private RectTransform CreatePage(string name, Color color, Vector2 position)
        {
            return BurgerUiFactory.CreateImage(name, pageStrip, color, position, new Vector2(ReferenceWidth, ReferenceHeight), false);
        }

        private RectTransform CreatePanel(string name, RectTransform parent, string title, Vector2 position, Vector2 size)
        {
            RectTransform panel = CreateRoundedPanel(name, parent, BurgerPrototypeTheme.Panel, position, size, false, 28f);
            CreateText(name + "Title", panel, title, 25, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, size.y * 0.5f - 38f), new Vector2(size.x - 24f, 45f));
            return panel;
        }

        private CookingTrayDragSource CreateTrayCard(
            RectTransform parent,
            string name,
            string label,
            CookingDragKind kind,
            IngredientType type,
            Vector2 position,
            Vector2 size)
        {
            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetTrayVisual(type);
            RectTransform card = CreateRoundedPanel(name, parent, BurgerPrototypeTheme.Card, position, size, true, 20f);
            card.gameObject.AddComponent<CanvasGroup>();
            float iconLimit = Mathf.Min(58f, size.y * 0.42f);
            float iconScale = Mathf.Min(
                iconLimit / Mathf.Max(1f, visual.Size.x),
                iconLimit / Mathf.Max(1f, visual.Size.y));
            Vector2 iconSize = visual.Size * iconScale;
            BurgerUiFactory.CreateShape(
                name + "Icon",
                card,
                visual.Shape,
                visual.Color,
                new Vector2(0f, size.y * 0.18f),
                iconSize,
                false,
                visual.SourceSprite);
            CreateText(name + "Label", card, label, size.x < 130f ? 16 : 20, FontStyle.Bold, BurgerPrototypeTheme.Ink, new Vector2(0f, -size.y * 0.25f), new Vector2(size.x - 8f, size.y * 0.42f));
            CookingTrayDragSource source = card.gameObject.AddComponent<CookingTrayDragSource>();
            source.Configure(
                controller,
                kind,
                type,
                visual.Shape,
                visual.Color,
                visual.Size,
                visual.SourceSprite);
            view.TraySources.Add(source);
            return source;
        }

        private void CreateDebugResetButton(RectTransform parent, Vector2 position)
        {
            RectTransform rect = CreateRoundedPanel("PrototypeReset", parent, BurgerPrototypeTheme.Accent, position, new Vector2(220f, 58f), true, 24f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Graphic>();
            button.onClick.AddListener(() => resetPrototype());
            CreateText("PrototypeResetLabel", rect, "프로토타입 리셋", 18, FontStyle.Bold, Color.white, Vector2.zero, rect.sizeDelta);
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
            outline.effectColor = BurgerPrototypeTheme.Border;
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
