using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerAssemblyController : MonoBehaviour
    {
        private readonly BurgerAssemblyState state = new BurgerAssemblyState();
        private readonly PattyGrillState grillState = new PattyGrillState();
        private readonly List<GameObject> layerVisuals = new List<GameObject>();
        private readonly List<DraggableBurgerItem> dragSources = new List<DraggableBurgerItem>();

        private Canvas canvas;
        private RectTransform layerRoot;
        private RectTransform grillPattyRoot;
        private Text statusText;
        private Text layerListText;
        private Text grillStatusText;
        private Button grillButton;
        private Button resetButton;
        private Font uiFont;
        private GameObject grillPattyVisual;

        private static readonly Color Background = Hex("#FFF4DC");
        private static readonly Color Ink = Hex("#35261F");
        private static readonly Color Panel = Hex("#F4DFC1");
        private static readonly Color Board = Hex("#B87842");
        private static readonly Color BoardEdge = Hex("#7B4528");
        private static readonly Color Accent = Hex("#E98242");
        private static readonly Color Bun = Hex("#E7A54B");
        private static readonly Color RawPatty = Hex("#C86B61");
        private static readonly Color CookedPatty = Hex("#754129");

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BuildInterface();
            RefreshControls();
        }

        public void TryDropOnBoard(DraggableBurgerItem item)
        {
            switch (item.Kind)
            {
                case BurgerDragItemKind.BottomBun:
                    StartBurger();
                    break;
                case BurgerDragItemKind.TopBun:
                    FinishBurger();
                    break;
                case BurgerDragItemKind.RawPatty:
                    statusText.text = "생패티는 도마에 올릴 수 없습니다. 오른쪽 그릴에서 먼저 구워 주세요.";
                    statusText.color = Hex("#A33A2B");
                    break;
                case BurgerDragItemKind.CookedPatty:
                    if (AddLayer(BurgerIngredientId.Patty))
                    {
                        grillState.TryTakeCookedPatty();
                        ClearGrill();
                    }
                    break;
                case BurgerDragItemKind.Ingredient:
                    AddLayer(item.Ingredient);
                    break;
            }

            RefreshControls();
        }

        public void TryDropOnGrill(DraggableBurgerItem item)
        {
            if (item.Kind != BurgerDragItemKind.RawPatty)
            {
                statusText.text = "그릴에는 생패티만 올릴 수 있습니다.";
                statusText.color = Hex("#A33A2B");
                return;
            }

            if (!grillState.TryLoadRawPatty())
            {
                statusText.text = "그릴이 이미 사용 중입니다.";
                statusText.color = Hex("#A33A2B");
                return;
            }

            LoadRawPattyOnGrill();
            RefreshControls();
        }

        private void BuildInterface()
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);

            var canvasObject = new GameObject("BurgerAssemblyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            CreateImage("Background", canvasRect, Background, Vector2.zero, new Vector2(1920f, 1080f));
            CreateText("Title", canvasRect, "슆슆버거 - 드래그 조립대", 40, FontStyle.Bold, Ink, new Vector2(0f, 500f), new Vector2(1100f, 70f));
            CreateText(
                "Order",
                canvasRect,
                "오늘 주문: 클래식 버거  |  케첩 + 구운 패티 + 치즈 + 양상추 + 토마토",
                24,
                FontStyle.Bold,
                Hex("#7A3523"),
                new Vector2(0f, 445f),
                new Vector2(1350f, 50f));

            CreateBoard(canvasRect);
            CreateBunPanel(canvasRect);
            CreateIngredientPanel(canvasRect);
            CreateGrillPanel(canvasRect);
            CreateSaucePanel(canvasRect);
            CreateStatusPanel(canvasRect);
            CreateEventSystem();
        }

        private void CreateBoard(RectTransform canvasRect)
        {
            RectTransform shadow = CreateImage("BoardShadow", canvasRect, BoardEdge, new Vector2(-180f, 35f), new Vector2(820f, 590f));
            RectTransform board = CreateImage("CuttingBoard", canvasRect, Board, new Vector2(-180f, 45f), new Vector2(790f, 560f));
            BurgerBoardDropZone dropZone = board.gameObject.AddComponent<BurgerBoardDropZone>();
            dropZone.Configure(this);

            CreateText("BoardLabel", board, "도마 - 여기에 드롭", 23, FontStyle.Bold, Hex("#FBE9CF"), new Vector2(0f, 235f), new Vector2(360f, 40f));
            RectTransform handle = CreateImage("BoardHandle", shadow, BoardEdge, new Vector2(445f, 0f), new Vector2(85f, 170f));
            CreateShape("HandleHole", handle, SimpleShape.Circle, Background, Vector2.zero, new Vector2(34f, 65f));

            GameObject root = new GameObject("BurgerLayers", typeof(RectTransform));
            layerRoot = root.GetComponent<RectTransform>();
            layerRoot.SetParent(board, false);
            SetRect(layerRoot, new Vector2(0f, -25f), new Vector2(560f, 430f));
        }

        private void CreateBunPanel(RectTransform canvasRect)
        {
            RectTransform panel = CreatePanel("BunPanel", canvasRect, "번", new Vector2(-800f, 80f), new Vector2(250f, 620f));

            CreateDragCard(
                panel,
                "TopBunButton",
                "상단 번\n완성",
                BurgerDragItemKind.TopBun,
                BurgerIngredientId.Patty,
                SimpleShape.Circle,
                Bun,
                new Vector2(0f, 125f),
                new Vector2(205f, 180f),
                new Vector2(370f, 105f),
                () => state.Phase == BurgerAssemblyPhase.Assembling && state.Layers.Count > 0,
                () => SetHint("재료를 한 개 이상 올린 뒤 상단 번을 드래그하세요."));

            CreateDragCard(
                panel,
                "BottomBunButton",
                "하단 번\n만들기 시작",
                BurgerDragItemKind.BottomBun,
                BurgerIngredientId.Patty,
                SimpleShape.Circle,
                Bun,
                new Vector2(0f, -130f),
                new Vector2(205f, 180f),
                new Vector2(370f, 90f),
                () => state.Phase == BurgerAssemblyPhase.WaitingForBottomBun,
                () => SetHint("현재 버거를 먼저 완성하거나 다시 만들기를 눌러 주세요."));
        }

        private void CreateIngredientPanel(RectTransform canvasRect)
        {
            RectTransform panel = CreatePanel("IngredientPanel", canvasRect, "속재료", new Vector2(365f, 80f), new Vector2(250f, 620f));

            AddIngredientDragCard(panel, "RawPatty", "생패티", BurgerDragItemKind.RawPatty, BurgerIngredientId.Patty, SimpleShape.Rectangle, RawPatty, 190f, () => grillState.Phase == PattyGrillPhase.Empty, "생패티는 오른쪽 그릴로 드래그하세요.");
            AddIngredientDragCard(panel, "Cheese", "치즈", BurgerDragItemKind.Ingredient, BurgerIngredientId.Cheese, SimpleShape.Triangle, Hex("#FFD84C"), 105f, CanAddIngredient, "먼저 하단 번을 도마에 올려 주세요.");
            AddIngredientDragCard(panel, "Lettuce", "양상추", BurgerDragItemKind.Ingredient, BurgerIngredientId.Lettuce, SimpleShape.Circle, Hex("#63B94D"), 20f, CanAddIngredient, "먼저 하단 번을 도마에 올려 주세요.");
            AddIngredientDragCard(panel, "Tomato", "토마토", BurgerDragItemKind.Ingredient, BurgerIngredientId.Tomato, SimpleShape.Circle, Hex("#E84B3C"), -65f, CanAddIngredient, "먼저 하단 번을 도마에 올려 주세요.");
            AddIngredientDragCard(panel, "Onion", "양파", BurgerDragItemKind.Ingredient, BurgerIngredientId.Onion, SimpleShape.Triangle, Hex("#B981C7"), -150f, CanAddIngredient, "먼저 하단 번을 도마에 올려 주세요.");
            AddIngredientDragCard(panel, "Pickle", "피클", BurgerDragItemKind.Ingredient, BurgerIngredientId.Pickle, SimpleShape.Circle, Hex("#4C9B45"), -235f, CanAddIngredient, "먼저 하단 번을 도마에 올려 주세요.");
        }

        private void CreateGrillPanel(RectTransform canvasRect)
        {
            RectTransform panel = CreatePanel("GrillPanel", canvasRect, "그릴", new Vector2(700f, 80f), new Vector2(300f, 620f));
            RectTransform grillSurface = CreateImage("GrillDropZone", panel, Hex("#34312F"), new Vector2(0f, 85f), new Vector2(245f, 245f));
            GrillDropZone dropZone = grillSurface.gameObject.AddComponent<GrillDropZone>();
            dropZone.Configure(this);

            for (int index = -2; index <= 2; index++)
            {
                CreateImage("GrillBar" + index, grillSurface, Hex("#77716C"), new Vector2(index * 42f, 0f), new Vector2(12f, 220f));
            }

            GameObject pattyRootObject = new GameObject("GrillPattyRoot", typeof(RectTransform));
            grillPattyRoot = pattyRootObject.GetComponent<RectTransform>();
            grillPattyRoot.SetParent(grillSurface, false);
            SetRect(grillPattyRoot, Vector2.zero, new Vector2(210f, 100f));

            grillStatusText = CreateText("GrillStatus", panel, "비어 있음\n생패티를 드롭하세요", 19, FontStyle.Bold, Hex("#5F493D"), new Vector2(0f, -72f), new Vector2(250f, 60f));
            grillButton = CreateChoiceButton(panel, "CookButton", "굽기", SimpleShape.Rectangle, Hex("#D86E31"), new Vector2(0f, -158f), new Vector2(220f, 72f), CookPatty);
            CreateText("GrillHelp", panel, "익힌 패티만 도마에\n드래그할 수 있습니다.", 17, FontStyle.Normal, Hex("#745B4D"), new Vector2(0f, -245f), new Vector2(250f, 60f));
        }

        private void CreateSaucePanel(RectTransform canvasRect)
        {
            RectTransform panel = CreatePanel("SaucePanel", canvasRect, "소스", new Vector2(-180f, -430f), new Vector2(790f, 170f));

            CreateDragCard(panel, "KetchupButton", "케첩", BurgerDragItemKind.Ingredient, BurgerIngredientId.Ketchup, SimpleShape.Circle, Hex("#D92E28"), new Vector2(-225f, -18f), new Vector2(230f, 82f), new Vector2(320f, 22f), CanAddIngredient, () => SetHint("먼저 하단 번을 도마에 올려 주세요."));
            CreateDragCard(panel, "MustardButton", "머스터드", BurgerDragItemKind.Ingredient, BurgerIngredientId.Mustard, SimpleShape.Triangle, Hex("#F5C542"), new Vector2(35f, -18f), new Vector2(230f, 82f), new Vector2(320f, 22f), CanAddIngredient, () => SetHint("먼저 하단 번을 도마에 올려 주세요."));
            resetButton = CreateChoiceButton(panel, "ResetButton", "다시 만들기", SimpleShape.Rectangle, Accent, new Vector2(285f, -18f), new Vector2(190f, 82f), ResetBurger);
        }

        private void CreateStatusPanel(RectTransform canvasRect)
        {
            RectTransform panel = CreateImage("StatusPanel", canvasRect, Hex("#FFF9EC"), new Vector2(-180f, 355f), new Vector2(790f, 100f));
            statusText = CreateText("Status", panel, "하단 번을 도마 위로 드래그하세요.", 23, FontStyle.Bold, Ink, new Vector2(0f, 20f), new Vector2(740f, 42f));
            layerListText = CreateText("LayerList", panel, "현재 조합: 비어 있음", 18, FontStyle.Normal, Hex("#745B4D"), new Vector2(0f, -25f), new Vector2(740f, 40f));
        }

        private void AddIngredientDragCard(RectTransform panel, string objectName, string label, BurgerDragItemKind kind, BurgerIngredientId id, SimpleShape shape, Color color, float y, System.Func<bool> condition, string blockedMessage)
        {
            IngredientVisual visual = GetVisual(id);
            if (kind == BurgerDragItemKind.RawPatty)
            {
                visual = new IngredientVisual(SimpleShape.Rectangle, RawPatty, new Vector2(350f, 60f));
            }

            CreateDragCard(panel, objectName + "Drag", label, kind, id, shape, color, new Vector2(0f, y), new Vector2(210f, 72f), visual.Size, condition, () => SetHint(blockedMessage));
        }

        private DraggableBurgerItem CreateDragCard(
            RectTransform parent,
            string name,
            string label,
            BurgerDragItemKind kind,
            BurgerIngredientId ingredient,
            SimpleShape shape,
            Color shapeColor,
            Vector2 position,
            Vector2 size,
            Vector2 ghostSize,
            System.Func<bool> condition,
            System.Action blockedAction)
        {
            RectTransform rect = CreateImage(name, parent, Hex("#FFFCF4"), position, size);
            rect.gameObject.AddComponent<CanvasGroup>();
            float iconSize = Mathf.Min(size.y - 14f, 58f);
            SimpleShapeGraphic icon = CreateShape(name + "Icon", rect, shape, shapeColor, new Vector2(-size.x * 0.31f, 0f), new Vector2(iconSize, iconSize));
            icon.raycastTarget = false;
            Text labelText = CreateText(name + "Label", rect, label, size.y > 100f ? 22 : 19, FontStyle.Bold, Ink, new Vector2(size.x * 0.1f, 0f), new Vector2(size.x * 0.64f, size.y - 8f));
            labelText.raycastTarget = false;

            DraggableBurgerItem draggable = rect.gameObject.AddComponent<DraggableBurgerItem>();
            draggable.Configure(kind, ingredient, canvas, shape, shapeColor, ghostSize, condition, blockedAction);
            dragSources.Add(draggable);
            return draggable;
        }

        private bool CanAddIngredient()
        {
            return state.Phase == BurgerAssemblyPhase.Assembling && state.Layers.Count < state.MaximumLayers;
        }

        private void StartBurger()
        {
            if (!state.TryStart())
            {
                return;
            }

            AddBurgerVisual("하단 번", SimpleShape.Circle, Bun, new Vector2(370f, 90f));
            SetHint("재료와 소스를 도마 위로 드래그하세요.", false);
        }

        private bool AddLayer(BurgerIngredientId ingredient)
        {
            if (!state.TryAdd(ingredient))
            {
                SetHint(state.Phase == BurgerAssemblyPhase.WaitingForBottomBun
                    ? "먼저 하단 번을 도마에 올려 주세요."
                    : "재료는 최대 10개까지 올릴 수 있습니다.");
                return false;
            }

            IngredientVisual visual = GetVisual(ingredient);
            AddBurgerVisual(GetDisplayName(ingredient), visual.Shape, visual.Color, visual.Size);
            SetHint(GetDisplayName(ingredient) + " 추가! 상단 번을 드래그하면 완성됩니다.", false);
            return true;
        }

        private void FinishBurger()
        {
            if (!state.TryFinish())
            {
                SetHint(state.Layers.Count == 0 ? "재료를 한 개 이상 올려 주세요." : "지금은 완성할 수 없습니다.");
                return;
            }

            AddBurgerVisual("상단 번", SimpleShape.Circle, Bun, new Vector2(370f, 105f));
            BurgerRecipe matchedRecipe = BurgerRecipeCatalog.FindMatch(state.Layers);
            if (BurgerRecipeCatalog.Classic.Matches(state.Layers))
            {
                statusText.text = "주문 성공! 클래식 버거가 완성되었습니다.";
                statusText.color = Hex("#287A3A");
            }
            else if (matchedRecipe != null)
            {
                statusText.text = matchedRecipe.Name + " 완성! 하지만 오늘 주문과는 다릅니다.";
                statusText.color = Hex("#9A5D13");
            }
            else
            {
                statusText.text = "자유 조합 버거 완성! 오늘 주문의 재료를 다시 확인해 보세요.";
                statusText.color = Hex("#9A5D13");
            }
        }

        private void LoadRawPattyOnGrill()
        {
            grillPattyVisual = CreateShape("RawPattyOnGrill", grillPattyRoot, SimpleShape.Rectangle, RawPatty, Vector2.zero, new Vector2(190f, 62f)).gameObject;
            grillStatusText.text = "생패티 준비 완료\n굽기 버튼을 누르세요";
            SetHint("생패티를 그릴에 올렸습니다. 굽기 버튼을 누르세요.", false);
        }

        private void CookPatty()
        {
            if (grillState.Phase == PattyGrillPhase.Empty)
            {
                SetHint("먼저 생패티를 그릴 위로 드래그하세요.");
                return;
            }

            if (grillState.Phase == PattyGrillPhase.Cooked)
            {
                SetHint("패티가 이미 익었습니다. 도마 위로 드래그하세요.", false);
                return;
            }

            if (grillPattyVisual != null)
            {
                Destroy(grillPattyVisual);
            }

            if (!grillState.TryCook())
            {
                SetHint("현재 패티를 구울 수 없습니다.");
                return;
            }
            SimpleShapeGraphic cookedGraphic = CreateShape("CookedPattyOnGrill", grillPattyRoot, SimpleShape.Rectangle, CookedPatty, Vector2.zero, new Vector2(190f, 62f));
            cookedGraphic.gameObject.AddComponent<CanvasGroup>();
            DraggableBurgerItem cookedDrag = cookedGraphic.gameObject.AddComponent<DraggableBurgerItem>();
            cookedDrag.Configure(
                BurgerDragItemKind.CookedPatty,
                BurgerIngredientId.Patty,
                canvas,
                SimpleShape.Rectangle,
                CookedPatty,
                new Vector2(350f, 60f),
                CanAddIngredient,
                () => SetHint("하단 번을 먼저 도마에 올린 뒤 익힌 패티를 드래그하세요."));
            dragSources.Add(cookedDrag);
            grillPattyVisual = cookedGraphic.gameObject;
            grillStatusText.text = "패티 굽기 완료!\n도마로 드래그하세요";
            SetHint("패티가 익었습니다. 그릴의 패티를 도마 위로 드래그하세요.", false);
            RefreshControls();
        }

        private void ClearGrill()
        {
            if (grillPattyVisual != null)
            {
                DraggableBurgerItem draggable = grillPattyVisual.GetComponent<DraggableBurgerItem>();
                if (draggable != null)
                {
                    dragSources.Remove(draggable);
                }

                Destroy(grillPattyVisual);
                grillPattyVisual = null;
            }

            grillState.Reset();
            grillStatusText.text = "비어 있음\n생패티를 드롭하세요";
        }

        private void ResetBurger()
        {
            state.Reset();
            foreach (GameObject visual in layerVisuals)
            {
                Destroy(visual);
            }

            layerVisuals.Clear();
            ClearGrill();
            statusText.color = Ink;
            statusText.text = "하단 번을 도마 위로 드래그하세요.";
            RefreshControls();
        }

        private void AddBurgerVisual(string objectName, SimpleShape shape, Color color, Vector2 size)
        {
            float y = -150f + layerVisuals.Count * 35f;
            SimpleShapeGraphic graphic = CreateShape(objectName, layerRoot, shape, color, new Vector2(0f, y), size);
            graphic.raycastTarget = false;
            layerVisuals.Add(graphic.gameObject);
        }

        private void RefreshControls()
        {
            bool waiting = state.Phase == BurgerAssemblyPhase.WaitingForBottomBun;
            bool completed = state.Phase == BurgerAssemblyPhase.Completed;

            resetButton.interactable = !waiting || grillState.Phase != PattyGrillPhase.Empty;
            grillButton.interactable = grillState.Phase == PattyGrillPhase.Raw;
            foreach (DraggableBurgerItem source in dragSources)
            {
                if (source != null)
                {
                    source.RefreshAppearance();
                }
            }

            var builder = new StringBuilder("현재 조합: ");
            if (state.Layers.Count == 0)
            {
                builder.Append(waiting ? "비어 있음" : "하단 번");
            }
            else
            {
                builder.Append("하단 번");
                foreach (BurgerIngredientId ingredient in state.Layers)
                {
                    builder.Append(" > ").Append(GetDisplayName(ingredient));
                }

                if (completed)
                {
                    builder.Append(" > 상단 번");
                }
            }

            layerListText.text = builder.ToString();
        }

        private void SetHint(string message, bool warning = true)
        {
            statusText.text = message;
            statusText.color = warning ? Hex("#A33A2B") : Ink;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, string title, Vector2 position, Vector2 size)
        {
            RectTransform panel = CreateImage(name, parent, Panel, position, size);
            CreateText(name + "Title", panel, title, 26, FontStyle.Bold, Ink, new Vector2(0f, size.y * 0.5f - 40f), new Vector2(size.x - 30f, 45f));
            return panel;
        }

        private Button CreateChoiceButton(RectTransform parent, string name, string label, SimpleShape shape, Color shapeColor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateImage(name, parent, Hex("#FFFCF4"), position, size);
            Image background = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);

            ColorBlock colors = button.colors;
            colors.normalColor = Hex("#FFFCF4");
            colors.highlightedColor = Hex("#FFE5B9");
            colors.pressedColor = Hex("#F5C687");
            colors.disabledColor = Hex("#CFC5B8");
            button.colors = colors;

            float iconSize = Mathf.Min(size.y - 18f, 58f);
            SimpleShapeGraphic icon = CreateShape(name + "Icon", rect, shape, shapeColor, new Vector2(-size.x * 0.31f, 0f), new Vector2(iconSize, iconSize));
            icon.raycastTarget = false;
            Text text = CreateText(name + "Label", rect, label, 20, FontStyle.Bold, Ink, new Vector2(size.x * 0.1f, 0f), new Vector2(size.x * 0.66f, size.y - 10f));
            text.raycastTarget = false;
            return button;
        }

        private RectTransform CreateImage(string name, RectTransform parent, Color color, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            gameObject.GetComponent<Image>().color = color;
            return rect;
        }

        private SimpleShapeGraphic CreateShape(string name, RectTransform parent, SimpleShape shape, Color color, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(SimpleShapeGraphic));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            SimpleShapeGraphic graphic = gameObject.GetComponent<SimpleShapeGraphic>();
            graphic.Shape = shape;
            graphic.color = color;
            return graphic;
        }

        private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, dimensions);

            Text text = gameObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void CreateEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static string GetDisplayName(BurgerIngredientId ingredient)
        {
            switch (ingredient)
            {
                case BurgerIngredientId.Patty: return "구운 패티";
                case BurgerIngredientId.Cheese: return "치즈";
                case BurgerIngredientId.Lettuce: return "양상추";
                case BurgerIngredientId.Tomato: return "토마토";
                case BurgerIngredientId.Onion: return "양파";
                case BurgerIngredientId.Pickle: return "피클";
                case BurgerIngredientId.Ketchup: return "케첩";
                case BurgerIngredientId.Mustard: return "머스터드";
                default: return ingredient.ToString();
            }
        }

        private static IngredientVisual GetVisual(BurgerIngredientId ingredient)
        {
            switch (ingredient)
            {
                case BurgerIngredientId.Patty: return new IngredientVisual(SimpleShape.Rectangle, CookedPatty, new Vector2(350f, 60f));
                case BurgerIngredientId.Cheese: return new IngredientVisual(SimpleShape.Triangle, Hex("#FFD84C"), new Vector2(370f, 75f));
                case BurgerIngredientId.Lettuce: return new IngredientVisual(SimpleShape.Circle, Hex("#63B94D"), new Vector2(390f, 55f));
                case BurgerIngredientId.Tomato: return new IngredientVisual(SimpleShape.Circle, Hex("#E84B3C"), new Vector2(330f, 48f));
                case BurgerIngredientId.Onion: return new IngredientVisual(SimpleShape.Triangle, Hex("#B981C7"), new Vector2(330f, 62f));
                case BurgerIngredientId.Pickle: return new IngredientVisual(SimpleShape.Circle, Hex("#4C9B45"), new Vector2(260f, 42f));
                case BurgerIngredientId.Ketchup: return new IngredientVisual(SimpleShape.Rectangle, Hex("#D92E28"), new Vector2(320f, 22f));
                case BurgerIngredientId.Mustard: return new IngredientVisual(SimpleShape.Rectangle, Hex("#F5C542"), new Vector2(320f, 22f));
                default: return new IngredientVisual(SimpleShape.Rectangle, Color.white, new Vector2(320f, 40f));
            }
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.magenta;
        }

        private readonly struct IngredientVisual
        {
            public IngredientVisual(SimpleShape shape, Color color, Vector2 size)
            {
                Shape = shape;
                Color = color;
                Size = size;
            }

            public SimpleShape Shape { get; }
            public Color Color { get; }
            public Vector2 Size { get; }
        }
    }
}
