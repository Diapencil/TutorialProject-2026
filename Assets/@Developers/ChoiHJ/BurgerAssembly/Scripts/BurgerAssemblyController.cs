using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float CanvasWorldScale = 0.01f;

        private readonly BurgerAssemblyState boardState = new BurgerAssemblyState();
        private readonly List<PlacedIngredientView> placedIngredients = new List<PlacedIngredientView>();
        private readonly List<CookingTrayDragSource> traySources = new List<CookingTrayDragSource>();

        private Camera mainCamera;
        private Canvas canvas;
        private RectTransform canvasRoot;
        private RectTransform dragLayer;
        private RectTransform grillDropArea;
        private RectTransform boardDropArea;
        private RectTransform boardLayerRoot;
        private CookingCameraSlider cameraSlider;
        private CookablePattyView activePatty;
        private Text grillStatusText;
        private Text boardStatusText;
        private Text boardSummaryText;
        private Text toastText;
        private GameObject toastObject;
        private Font uiFont;
        private Coroutine toastRoutine;

        private bool hasActivePointerDrag;
        private CookingDragKind activeDragKind;
        private IngredientType activeDragType;
        private RectTransform dragGhost;
        private Vector2 lastPointerScreen;
        private Vector2 lastSauceStampScreen;
        private bool hasSauceStampOrigin;
        private bool saucePlacedDuringDrag;
        private CookablePattyView draggedPatty;

        private static readonly Color Background = Hex("#FFF2D7");
        private static readonly Color Ink = Hex("#38271F");
        private static readonly Color Panel = Hex("#F5DFC0");
        private static readonly Color Board = Hex("#B97943");
        private static readonly Color BoardEdge = Hex("#75442B");
        private static readonly Color Grill = Hex("#302E2D");
        private static readonly Color GrillBar = Hex("#77716C");
        private static readonly Color Accent = Hex("#E77B3E");
        private static readonly Color RawPatty = Hex("#C86B61");
        private static readonly Color CookedPatty = Hex("#754129");
        private static readonly Color BurntPatty = Hex("#211B18");
        private static readonly Color Bun = Hex("#E8A64C");

        public event Action<BurgerData> OnBurgerCompleted;

        public BurgerData LastCompletedBurger { get; private set; }

        public bool CanEditBoard => !boardState.IsCompleted;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BuildInterface();
            RefreshControls();
        }

        private void Update()
        {
            if (hasActivePointerDrag && dragGhost != null)
            {
                PositionDragGhost(lastPointerScreen);
            }
        }

        public bool CanBeginTrayDrag(CookingDragKind kind, IngredientType type)
        {
            if (boardState.IsCompleted || hasActivePointerDrag)
            {
                return false;
            }

            if (kind == CookingDragKind.RawPatty)
            {
                return activePatty == null;
            }

            return boardState.CanPlace(type);
        }

        public bool TryBeginTrayDrag(
            CookingDragKind kind,
            IngredientType type,
            SimpleShape shape,
            Color color,
            Vector2 size,
            Vector2 screenPosition)
        {
            if (!CanBeginTrayDrag(kind, type))
            {
                ExplainBlockedTrayDrag(kind, type);
                return false;
            }

            hasActivePointerDrag = true;
            activeDragKind = kind;
            activeDragType = type;
            draggedPatty = null;
            lastPointerScreen = screenPosition;
            hasSauceStampOrigin = false;
            saucePlacedDuringDrag = false;
            CreateDragGhost(shape, color, size);
            PositionDragGhost(screenPosition);

            if (kind == CookingDragKind.Sauce)
            {
                SetBoardStatus("소스 용기를 도마 위에서 움직이면 10px 간격으로 소스가 뿌려집니다.", false);
            }

            return true;
        }

        public void UpdatePointerDrag(Vector2 screenPosition)
        {
            if (!hasActivePointerDrag)
            {
                return;
            }

            lastPointerScreen = screenPosition;
            PositionDragGhost(screenPosition);
            if (activeDragKind == CookingDragKind.Sauce)
            {
                UpdateSauceTrail(screenPosition);
            }
        }

        public void EndTrayDrag(Vector2 screenPosition)
        {
            if (!hasActivePointerDrag)
            {
                return;
            }

            bool placed = false;
            if (activeDragKind == CookingDragKind.RawPatty)
            {
                if (cameraSlider.DestinationZone == CookingCameraZone.Grill &&
                    TryGetLocalPoint(grillDropArea, screenPosition, CookingCameraZone.Grill, out Vector2 grillLocal))
                {
                    SpawnRawPatty(grillLocal);
                    placed = true;
                }
            }
            else if (activeDragKind == CookingDragKind.Ingredient)
            {
                if (cameraSlider.DestinationZone == CookingCameraZone.Board &&
                    TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 boardLocal))
                {
                    placed = TryPlaceIngredient(activeDragType, boardLocal);
                }
            }
            else if (activeDragKind == CookingDragKind.Sauce)
            {
                placed = saucePlacedDuringDrag;
            }

            if (!placed)
            {
                if (activeDragKind == CookingDragKind.RawPatty)
                {
                    SetGrillStatus("고기반죽을 불판 영역 안에 놓아 주세요.", true);
                }
                else if (activeDragKind != CookingDragKind.Sauce)
                {
                    SetBoardStatus("재료를 도마 영역 안에 놓아 주세요.", true);
                }
            }

            CleanupPointerDrag();
            RefreshControls();
        }

        public bool TryBeginPlacedIngredientMove(PlacedIngredientView view, Vector2 screenPosition)
        {
            if (!CanEditBoard || view == null)
            {
                return false;
            }

            int layerOrder = boardState.BringToFront();
            if (layerOrder < 0)
            {
                return false;
            }

            view.SetLayerOrder(layerOrder);
            MovePlacedIngredient(view, screenPosition);
            return true;
        }

        public void MovePlacedIngredient(PlacedIngredientView view, Vector2 screenPosition)
        {
            if (!CanEditBoard || view == null)
            {
                return;
            }

            if (TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 local))
            {
                RectTransform rect = view.GetComponent<RectTransform>();
                rect.anchoredPosition = ClampInside(boardLayerRoot.rect, local, rect.sizeDelta);
            }
        }

        public void EndPlacedIngredientMove(PlacedIngredientView view, Vector2 screenPosition)
        {
            MovePlacedIngredient(view, screenPosition);
            RefreshBoardSummary();
        }

        public void HandlePattyTap(CookablePattyView patty)
        {
            if (patty == null || patty != activePatty)
            {
                return;
            }

            switch (patty.State.Phase)
            {
                case PattyGrillPhase.RawDough:
                    patty.State.TryPressDough();
                    SetGrillStatus("패티를 눌렀습니다. 1면을 3초 동안 굽습니다.", false);
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    patty.State.TryFlip();
                    SetGrillStatus("패티를 뒤집었습니다. 2면을 3초 동안 굽습니다.", false);
                    break;
                case PattyGrillPhase.CookingSide1:
                    SetGrillStatus("아직 뒤집을 수 없습니다. 1면 조리가 끝날 때까지 기다려 주세요.", true);
                    break;
                case PattyGrillPhase.CookingSide2:
                case PattyGrillPhase.Flipping:
                    SetGrillStatus("2면을 조리하고 있습니다.", false);
                    break;
                case PattyGrillPhase.Done:
                    SetGrillStatus("완료된 패티를 오른쪽 화면 끝으로 드래그하세요.", false);
                    break;
                case PattyGrillPhase.Overcooked:
                    SetGrillStatus("패티가 타서 이동할 수 없습니다. 프로토타입 리셋을 사용해 주세요.", true);
                    break;
            }
        }

        public bool TryBeginCookedPattyDrag(CookablePattyView patty, Vector2 screenPosition)
        {
            if (patty == null || patty != activePatty || hasActivePointerDrag || boardState.IsCompleted)
            {
                return false;
            }

            if (!patty.State.CanDragToBoard)
            {
                HandlePattyTap(patty);
                return false;
            }

            hasActivePointerDrag = true;
            activeDragKind = CookingDragKind.CookedPatty;
            activeDragType = IngredientType.Patty;
            draggedPatty = patty;
            lastPointerScreen = screenPosition;
            CreateDragGhost(SimpleShape.Rectangle, CookedPatty, new Vector2(260f, 105f));
            PositionDragGhost(screenPosition);
            SetGrillStatus("패티를 화면 오른쪽 끝으로 옮기면 도마로 전환됩니다.", false);
            return true;
        }

        public void RequestPattyTransfer(Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || activeDragKind != CookingDragKind.CookedPatty)
            {
                return;
            }

            if (screenPosition.x >= Screen.width * CookingPrototypeRules.PattyEdgeTransferScreenRatio &&
                cameraSlider.DestinationZone == CookingCameraZone.Grill)
            {
                cameraSlider.MoveToBoard();
                SetBoardStatus("도마로 이동했습니다. 원하는 위치에서 패티를 놓으세요.", false);
            }
        }

        public bool EndCookedPattyDrag(CookablePattyView patty, Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || draggedPatty != patty)
            {
                return false;
            }

            bool placed = false;
            if (cameraSlider.DestinationZone == CookingCameraZone.Board &&
                TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 boardLocal))
            {
                placed = TryPlaceIngredient(IngredientType.Patty, boardLocal);
            }

            CleanupPointerDrag();
            if (placed)
            {
                activePatty = null;
                Destroy(patty.gameObject);
                SetBoardStatus("구운 패티를 도마에 놓았습니다.", false);
            }
            else
            {
                SetBoardStatus("패티를 도마 영역 안에 놓아 주세요.", true);
            }

            RefreshControls();
            return placed;
        }

        public void UpdatePattyStatus(CookablePattyView patty)
        {
            if (patty == null || patty != activePatty || grillStatusText == null)
            {
                return;
            }

            switch (patty.State.Phase)
            {
                case PattyGrillPhase.CookingSide1:
                    grillStatusText.text = $"1면 조리 중: {Mathf.Max(0f, CookingPrototypeRules.FirstSideCookSeconds - patty.State.PhaseElapsed):0.0}초";
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    grillStatusText.text = "뒤집기 가능! 패티를 탭하세요.";
                    grillStatusText.color = Hex("#E38B22");
                    break;
                case PattyGrillPhase.CookingSide2:
                    grillStatusText.text = $"2면 조리 중: {Mathf.Max(0f, CookingPrototypeRules.SecondSideCookSeconds - patty.State.PhaseElapsed):0.0}초";
                    break;
                case PattyGrillPhase.Done:
                    grillStatusText.text = $"조리 완료 — {patty.State.GetDoneTimeRemaining():0.0}초 뒤 탐";
                    grillStatusText.color = Hex("#287A3A");
                    break;
                case PattyGrillPhase.Overcooked:
                    grillStatusText.text = "조리 실패: 패티가 탔습니다.";
                    grillStatusText.color = Hex("#A33A2B");
                    break;
            }
        }

        private void BuildInterface()
        {
            if (canvas != null)
            {
                return;
            }

            EnsureCamera();
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            float halfPageWorld = ReferenceWidth * CanvasWorldScale * 0.5f;
            var canvasObject = new GameObject(
                "CookingCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CookingCameraSlider));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 10;

            canvasRoot = canvasObject.GetComponent<RectTransform>();
            canvasRoot.sizeDelta = new Vector2(ReferenceWidth * 2f, ReferenceHeight);
            canvasRoot.localScale = Vector3.one * CanvasWorldScale;
            canvasRoot.position = Vector3.zero;
            Image swipeSurface = canvasObject.GetComponent<Image>();
            swipeSurface.color = new Color(1f, 1f, 1f, 0.001f);
            swipeSurface.raycastTarget = true;

            cameraSlider = canvasObject.GetComponent<CookingCameraSlider>();
            cameraSlider.Configure(mainCamera, -halfPageWorld, halfPageWorld);
            cameraSlider.ZoneChanged += zone =>
            {
                if (zone == CookingCameraZone.Grill)
                {
                    SetGrillStatus("고기반죽을 불판에 놓고 탭해서 눌러 주세요.", false);
                }
                else
                {
                    SetBoardStatus("트레이에서 재료를 꺼내 도마 위에 자유롭게 놓으세요.", false);
                }
            };

            RectTransform grillPage = CreatePage("GrillPage", new Vector2(-ReferenceWidth * 0.5f, 0f));
            RectTransform boardPage = CreatePage("BoardPage", new Vector2(ReferenceWidth * 0.5f, 0f));
            BuildGrillPage(grillPage);
            BuildBoardPage(boardPage);

            GameObject dragLayerObject = new GameObject("DragLayer", typeof(RectTransform));
            dragLayer = dragLayerObject.GetComponent<RectTransform>();
            dragLayer.SetParent(canvasRoot, false);
            SetRect(dragLayer, Vector2.zero, canvasRoot.sizeDelta);
            dragLayer.SetAsLastSibling();

            CreateEventSystem();
        }

        private void BuildGrillPage(RectTransform page)
        {
            CreateText("GrillTitle", page, "불판 구역", 42, FontStyle.Bold, Ink, new Vector2(0f, 485f), new Vector2(700f, 70f));
            CreateText("GrillSwipeHint", page, "← 화면을 왼쪽으로 20% 이상 밀면 도마 구역으로 이동", 20, FontStyle.Bold, Hex("#785849"), new Vector2(0f, 430f), new Vector2(900f, 45f));

            RectTransform grillFrame = CreateImage("GrillFrame", page, BoardEdge, new Vector2(-150f, -35f), new Vector2(1190f, 760f), false);
            grillDropArea = CreateImage("GrillDropArea", grillFrame, Grill, Vector2.zero, new Vector2(1150f, 720f), true);
            for (int index = -5; index <= 5; index++)
            {
                CreateImage("GrillBar" + index, grillDropArea, GrillBar, new Vector2(index * 95f, 0f), new Vector2(18f, 670f), false);
            }

            CreateText("GrillAreaLabel", grillDropArea, "불판 — 고기반죽을 여기에 드롭", 24, FontStyle.Bold, Hex("#F7E8D2"), new Vector2(0f, 315f), new Vector2(600f, 45f));
            grillStatusText = CreateText("GrillStatus", page, "고기반죽을 불판에 놓고 탭해서 눌러 주세요.", 24, FontStyle.Bold, Ink, new Vector2(-150f, -445f), new Vector2(1100f, 55f));

            RectTransform tray = CreatePanel("RawPattyTray", page, "고기반죽 트레이", new Vector2(700f, 20f), new Vector2(360f, 760f));
            CreateText("RawTrayHelp", tray, "무한 공급\n드래그해도 원본 유지", 19, FontStyle.Normal, Hex("#72594D"), new Vector2(0f, 235f), new Vector2(300f, 75f));
            CreateTrayCard(
                tray,
                "RawPattySource",
                "고기반죽",
                CookingDragKind.RawPatty,
                IngredientType.Patty,
                SimpleShape.Circle,
                RawPatty,
                new Vector2(0f, 50f),
                new Vector2(270f, 190f),
                new Vector2(150f, 150f));
            CreateText("RawTraySteps", tray, "1. 불판에 드롭\n2. 탭해서 누르기\n3. 3초 후 뒤집기\n4. 다시 3초 굽기", 20, FontStyle.Bold, Ink, new Vector2(0f, -180f), new Vector2(300f, 170f));
            CreateDebugResetButton(page, new Vector2(805f, 475f));
        }

        private void BuildBoardPage(RectTransform page)
        {
            CreateText("BoardTitle", page, "도마 구역", 42, FontStyle.Bold, Ink, new Vector2(0f, 485f), new Vector2(700f, 70f));
            CreateText("BoardSwipeHint", page, "화면을 오른쪽으로 20% 이상 밀면 불판 구역으로 복귀 →", 20, FontStyle.Bold, Hex("#785849"), new Vector2(0f, 430f), new Vector2(900f, 45f));

            RectTransform boardFrame = CreateImage("BoardFrame", page, BoardEdge, new Vector2(-255f, -30f), new Vector2(1190f, 790f), false);
            boardDropArea = CreateImage("BoardDropArea", boardFrame, Board, Vector2.zero, new Vector2(1150f, 750f), true);
            CreateText("BoardAreaLabel", boardDropArea, "도마 — 정해진 슬롯 없이 자유 배치", 24, FontStyle.Bold, Hex("#FBE9CF"), new Vector2(0f, 330f), new Vector2(650f, 45f));

            GameObject layerRootObject = new GameObject("BoardIngredientLayer", typeof(RectTransform));
            boardLayerRoot = layerRootObject.GetComponent<RectTransform>();
            boardLayerRoot.SetParent(boardDropArea, false);
            SetRect(boardLayerRoot, Vector2.zero, boardDropArea.sizeDelta);

            RectTransform tray = CreatePanel("IngredientTray", page, "재료 트레이 · 무한 공급", new Vector2(705f, -5f), new Vector2(440f, 800f));
            CreateBoardTrayCards(tray);

            boardSummaryText = CreateText("BoardSummary", page, "배치 0개 · 토핑 0/8 · 소스 0개", 21, FontStyle.Bold, Ink, new Vector2(-255f, -448f), new Vector2(1150f, 45f));
            boardStatusText = CreateText("BoardStatus", page, "트레이에서 재료를 꺼내 도마 위에 자유롭게 놓으세요.", 22, FontStyle.Bold, Ink, new Vector2(200f, 385f), new Vector2(1200f, 45f));
            RectTransform toastPanel = CreateImage("Toast", page, new Color(0.55f, 0.12f, 0.08f, 0.92f), new Vector2(-255f, 0f), new Vector2(650f, 70f), false);
            toastText = CreateText("ToastText", toastPanel, string.Empty, 24, FontStyle.Bold, Color.white, Vector2.zero, toastPanel.sizeDelta);
            toastObject = toastPanel.gameObject;
            toastObject.SetActive(false);
            CreateDebugResetButton(page, new Vector2(805f, 475f));
        }

        private void CreateBoardTrayCards(RectTransform tray)
        {
            CreateTrayCard(tray, "BottomBunTray", "하단 번", CookingDragKind.Ingredient, IngredientType.BunBottom, SimpleShape.Circle, Bun, new Vector2(-125f, 220f), new Vector2(112f, 145f), new Vector2(280f, 90f));
            CreateTrayCard(tray, "TopBunTray", "상단 번\n완성", CookingDragKind.Ingredient, IngredientType.BunTop, SimpleShape.Circle, Bun, new Vector2(0f, 220f), new Vector2(112f, 145f), new Vector2(290f, 105f));
            CreateTrayCard(tray, "LettuceTray", "양상추", CookingDragKind.Ingredient, IngredientType.ToppingLettuce, SimpleShape.Circle, Hex("#63B94D"), new Vector2(125f, 220f), new Vector2(112f, 145f), new Vector2(125f, 55f));
            CreateTrayCard(tray, "TomatoTray", "토마토", CookingDragKind.Ingredient, IngredientType.ToppingTomato, SimpleShape.Circle, Hex("#E84B3C"), new Vector2(-125f, 55f), new Vector2(112f, 145f), new Vector2(110f, 48f));
            CreateTrayCard(tray, "CheeseTray", "치즈", CookingDragKind.Ingredient, IngredientType.ToppingCheese, SimpleShape.Triangle, Hex("#FFD84C"), new Vector2(0f, 55f), new Vector2(112f, 145f), new Vector2(135f, 75f));
            CreateTrayCard(tray, "OnionTray", "양파", CookingDragKind.Ingredient, IngredientType.ToppingOnion, SimpleShape.Triangle, Hex("#B981C7"), new Vector2(125f, 55f), new Vector2(112f, 145f), new Vector2(115f, 62f));
            CreateTrayCard(tray, "PickleTray", "피클", CookingDragKind.Ingredient, IngredientType.ToppingPickle, SimpleShape.Circle, Hex("#4C9B45"), new Vector2(-125f, -110f), new Vector2(112f, 145f), new Vector2(90f, 42f));
            CreateTrayCard(tray, "KetchupTray", "케첩", CookingDragKind.Sauce, IngredientType.SauceKetchup, SimpleShape.Circle, Hex("#D92E28"), new Vector2(0f, -110f), new Vector2(112f, 145f), new Vector2(70f, 100f));
            CreateTrayCard(tray, "MustardTray", "머스터드", CookingDragKind.Sauce, IngredientType.SauceMustard, SimpleShape.Circle, Hex("#F5C542"), new Vector2(125f, -110f), new Vector2(112f, 145f), new Vector2(70f, 100f));
            CreateText("TrayLimit", tray, "토핑 최대 8개 · 패티/번/소스 제외", 17, FontStyle.Bold, Hex("#75584A"), new Vector2(0f, -275f), new Vector2(390f, 45f));
        }

        private void SpawnRawPatty(Vector2 localPosition)
        {
            if (activePatty != null)
            {
                return;
            }

            Vector2 size = new Vector2(150f, 150f);
            localPosition = ClampInside(grillDropArea.rect, localPosition, size);
            SimpleShapeGraphic graphic = CreateShape("CookablePatty", grillDropArea, SimpleShape.Circle, RawPatty, localPosition, size, true);
            graphic.gameObject.AddComponent<CanvasGroup>();
            Text phaseLabel = CreateText("PattyPhase", graphic.rectTransform, "고기반죽\n탭해서 누르기", 18, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(240f, 100f));
            CookablePattyView view = graphic.gameObject.AddComponent<CookablePattyView>();
            view.Configure(this, graphic, phaseLabel, RawPatty, CookedPatty, BurntPatty);
            activePatty = view;
            SetGrillStatus("고기반죽을 올렸습니다. 패티를 탭해서 눌러 주세요.", false);
        }

        private bool TryPlaceIngredient(IngredientType type, Vector2 localPosition)
        {
            if (!boardState.TryRegisterPlacement(type, out int layerOrder))
            {
                if (BurgerAssemblyState.IsTopping(type) && boardState.ToppingCount >= boardState.MaximumToppings)
                {
                    ShowToast("더 이상 담을 수 없습니다");
                }
                return false;
            }

            IngredientVisual visual = GetVisual(type);
            localPosition = ClampInside(boardLayerRoot.rect, localPosition, visual.Size);
            bool isSauce = IsSauce(type);
            SimpleShapeGraphic graphic = CreateShape(type + "_" + layerOrder, boardLayerRoot, visual.Shape, visual.Color, localPosition, visual.Size, !isSauce);
            graphic.gameObject.AddComponent<CanvasGroup>();
            PlacedIngredientView view = graphic.gameObject.AddComponent<PlacedIngredientView>();
            view.Configure(this, type, layerOrder);
            placedIngredients.Add(view);

            if (type == IngredientType.BunTop)
            {
                CompleteBurger();
            }
            else
            {
                SetBoardStatus(GetDisplayName(type) + " 배치 완료", false);
            }

            RefreshBoardSummary();
            return true;
        }

        private void CompleteBurger()
        {
            List<IngredientPlacement> snapshot = placedIngredients
                .Where(view => view != null)
                .Select(view => view.Capture())
                .OrderBy(placement => placement.layerOrder)
                .ToList();

            if (!boardState.TryComplete(snapshot, out BurgerData burgerData))
            {
                return;
            }

            LastCompletedBurger = burgerData;
            string json = JsonUtility.ToJson(burgerData, true);
            Debug.Log("[BurgerAssembly] OnBurgerCompleted\n" + json);
            SetBoardStatus("조립 완료! BurgerData를 콘솔에 기록했습니다.", false);
            ShowToast("조립 완료");
            OnBurgerCompleted?.Invoke(burgerData);
            RefreshControls();
        }

        private void UpdateSauceTrail(Vector2 screenPosition)
        {
            if (cameraSlider.DestinationZone != CookingCameraZone.Board ||
                !TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 local))
            {
                hasSauceStampOrigin = false;
                return;
            }

            if (!hasSauceStampOrigin)
            {
                saucePlacedDuringDrag |= TryPlaceIngredient(activeDragType, local);
                lastSauceStampScreen = screenPosition;
                hasSauceStampOrigin = true;
                return;
            }

            Vector2 delta = screenPosition - lastSauceStampScreen;
            float distance = delta.magnitude;
            if (distance < CookingPrototypeRules.SauceStampSpacingPixels)
            {
                return;
            }

            Vector2 direction = delta / distance;
            while (distance >= CookingPrototypeRules.SauceStampSpacingPixels)
            {
                lastSauceStampScreen += direction * CookingPrototypeRules.SauceStampSpacingPixels;
                if (TryGetLocalPoint(boardLayerRoot, lastSauceStampScreen, CookingCameraZone.Board, out Vector2 stampLocal))
                {
                    saucePlacedDuringDrag |= TryPlaceIngredient(activeDragType, stampLocal);
                }
                distance = Vector2.Distance(screenPosition, lastSauceStampScreen);
            }
        }

        private void ExplainBlockedTrayDrag(CookingDragKind kind, IngredientType type)
        {
            if (boardState.IsCompleted)
            {
                ShowToast("이미 조립이 완료되었습니다");
                return;
            }

            if (kind == CookingDragKind.RawPatty && activePatty != null)
            {
                SetGrillStatus("현재 패티를 먼저 조리하거나 프로토타입을 리셋해 주세요.", true);
                return;
            }

            if (BurgerAssemblyState.IsTopping(type) && boardState.ToppingCount >= boardState.MaximumToppings)
            {
                ShowToast("더 이상 담을 수 없습니다");
            }
        }

        private void ResetPrototype()
        {
            CleanupPointerDrag();
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }
            if (toastObject != null)
            {
                toastObject.SetActive(false);
            }

            foreach (PlacedIngredientView view in placedIngredients)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            placedIngredients.Clear();

            if (activePatty != null)
            {
                Destroy(activePatty.gameObject);
                activePatty = null;
            }

            boardState.Reset();
            LastCompletedBurger = null;
            cameraSlider.SetImmediate(CookingCameraZone.Grill);
            SetGrillStatus("고기반죽을 불판에 놓고 탭해서 눌러 주세요.", false);
            SetBoardStatus("트레이에서 재료를 꺼내 도마 위에 자유롭게 놓으세요.", false);
            RefreshBoardSummary();
            RefreshControls();
        }

        private void RefreshControls()
        {
            foreach (CookingTrayDragSource source in traySources)
            {
                if (source != null)
                {
                    source.RefreshAppearance();
                }
            }
        }

        private void RefreshBoardSummary()
        {
            if (boardSummaryText == null)
            {
                return;
            }

            int sauceCount = placedIngredients.Count(view => view != null && IsSauce(view.IngredientType));
            var builder = new StringBuilder();
            builder.Append("배치 ").Append(placedIngredients.Count(view => view != null)).Append("개")
                .Append(" · 토핑 ").Append(boardState.ToppingCount).Append('/').Append(boardState.MaximumToppings)
                .Append(" · 소스 ").Append(sauceCount).Append("개");
            if (boardState.IsCompleted)
            {
                builder.Append(" · 완료");
            }
            boardSummaryText.text = builder.ToString();
        }

        private void SetGrillStatus(string message, bool warning)
        {
            if (grillStatusText == null)
            {
                return;
            }
            grillStatusText.text = message;
            grillStatusText.color = warning ? Hex("#A33A2B") : Ink;
        }

        private void SetBoardStatus(string message, bool warning)
        {
            if (boardStatusText == null)
            {
                return;
            }
            boardStatusText.text = message;
            boardStatusText.color = warning ? Hex("#A33A2B") : Ink;
        }

        private void ShowToast(string message)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastObject.SetActive(true);
            if (!Application.isPlaying)
            {
                return;
            }

            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }
            toastRoutine = StartCoroutine(HideToastAfterDelay());
        }

        private IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (toastObject != null)
            {
                toastObject.SetActive(false);
            }
            toastRoutine = null;
        }

        private void CreateDragGhost(SimpleShape shape, Color color, Vector2 size)
        {
            SimpleShapeGraphic graphic = CreateShape("DragGhost", dragLayer, shape, color, Vector2.zero, size, false);
            graphic.raycastTarget = false;
            CanvasGroup group = graphic.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.82f;
            dragGhost = graphic.rectTransform;
            dragGhost.SetAsLastSibling();
        }

        private void PositionDragGhost(Vector2 screenPosition)
        {
            if (dragGhost == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPosition, mainCamera, out Vector2 local))
            {
                dragGhost.anchoredPosition = local;
            }
        }

        private void CleanupPointerDrag()
        {
            hasActivePointerDrag = false;
            draggedPatty = null;
            hasSauceStampOrigin = false;
            saucePlacedDuringDrag = false;
            if (dragGhost != null)
            {
                Destroy(dragGhost.gameObject);
                dragGhost = null;
            }
        }

        private bool TryGetLocalPoint(RectTransform target, Vector2 screenPosition, CookingCameraZone targetZone, out Vector2 local)
        {
            local = Vector2.zero;
            if (target == null || mainCamera == null || cameraSlider == null)
            {
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(target.forward, target.position);
            if (!plane.Raycast(ray, out float enter))
            {
                return false;
            }

            Vector3 world = ray.GetPoint(enter);
            float targetCameraX = targetZone == CookingCameraZone.Grill ? cameraSlider.GrillX : cameraSlider.BoardX;
            world.x += targetCameraX - mainCamera.transform.position.x;
            Vector3 local3 = target.InverseTransformPoint(world);
            local = new Vector2(local3.x, local3.y);
            return target.rect.Contains(local);
        }

        private void EnsureCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.GetComponent<Camera>();
            }

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Background;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = ReferenceHeight * CanvasWorldScale * 0.5f;
            mainCamera.transform.position = new Vector3(-ReferenceWidth * CanvasWorldScale * 0.5f, 0f, -10f);
        }

        private RectTransform CreatePage(string name, Vector2 position)
        {
            RectTransform page = CreateImage(name, canvasRoot, Background, position, new Vector2(ReferenceWidth, ReferenceHeight), false);
            return page;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, string title, Vector2 position, Vector2 size)
        {
            RectTransform panel = CreateImage(name, parent, Panel, position, size, false);
            CreateText(name + "Title", panel, title, 25, FontStyle.Bold, Ink, new Vector2(0f, size.y * 0.5f - 38f), new Vector2(size.x - 24f, 45f));
            return panel;
        }

        private CookingTrayDragSource CreateTrayCard(
            RectTransform parent,
            string name,
            string label,
            CookingDragKind kind,
            IngredientType type,
            SimpleShape shape,
            Color color,
            Vector2 position,
            Vector2 size,
            Vector2 ghostSize)
        {
            RectTransform card = CreateImage(name, parent, Hex("#FFFCF4"), position, size, true);
            card.gameObject.AddComponent<CanvasGroup>();
            float iconSize = Mathf.Min(58f, size.y * 0.42f);
            CreateShape(name + "Icon", card, shape, color, new Vector2(0f, size.y * 0.18f), new Vector2(iconSize, iconSize), false);
            CreateText(name + "Label", card, label, size.x < 130f ? 16 : 20, FontStyle.Bold, Ink, new Vector2(0f, -size.y * 0.25f), new Vector2(size.x - 8f, size.y * 0.42f));
            CookingTrayDragSource source = card.gameObject.AddComponent<CookingTrayDragSource>();
            source.Configure(this, kind, type, shape, color, ghostSize);
            traySources.Add(source);
            return source;
        }

        private void CreateDebugResetButton(RectTransform parent, Vector2 position)
        {
            RectTransform rect = CreateImage("PrototypeReset", parent, Accent, position, new Vector2(220f, 58f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(ResetPrototype);
            CreateText("PrototypeResetLabel", rect, "프로토타입 리셋", 18, FontStyle.Bold, Color.white, Vector2.zero, rect.sizeDelta);
        }

        private RectTransform CreateImage(string name, RectTransform parent, Color color, Vector2 position, Vector2 size, bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private SimpleShapeGraphic CreateShape(string name, RectTransform parent, SimpleShape shape, Color color, Vector2 position, Vector2 size, bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(SimpleShapeGraphic));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            SimpleShapeGraphic graphic = gameObject.GetComponent<SimpleShapeGraphic>();
            graphic.Shape = shape;
            graphic.color = color;
            graphic.raycastTarget = raycastTarget;
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
            text.raycastTarget = false;
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

        private static Vector2 ClampInside(Rect bounds, Vector2 local, Vector2 itemSize)
        {
            float halfWidth = Mathf.Min(bounds.width * 0.5f, itemSize.x * 0.5f);
            float halfHeight = Mathf.Min(bounds.height * 0.5f, itemSize.y * 0.5f);
            return new Vector2(
                Mathf.Clamp(local.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth),
                Mathf.Clamp(local.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight));
        }

        private static void CreateEventSystem()
        {
            EventSystem existing = FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                existing.pixelDragThreshold = Mathf.RoundToInt(CookingPrototypeRules.DragThresholdPixels);
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            eventSystem.pixelDragThreshold = Mathf.RoundToInt(CookingPrototypeRules.DragThresholdPixels);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static string GetDisplayName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return "구운 패티";
                case IngredientType.BunBottom: return "하단 번";
                case IngredientType.BunTop: return "상단 번";
                case IngredientType.ToppingLettuce: return "양상추";
                case IngredientType.ToppingTomato: return "토마토";
                case IngredientType.ToppingCheese: return "치즈";
                case IngredientType.ToppingOnion: return "양파";
                case IngredientType.ToppingPickle: return "피클";
                case IngredientType.SauceKetchup: return "케첩";
                case IngredientType.SauceMustard: return "머스터드";
                default: return type.ToString();
            }
        }

        private static IngredientVisual GetVisual(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return new IngredientVisual(SimpleShape.Rectangle, CookedPatty, new Vector2(260f, 105f));
                case IngredientType.BunBottom: return new IngredientVisual(SimpleShape.Circle, Bun, new Vector2(280f, 90f));
                case IngredientType.BunTop: return new IngredientVisual(SimpleShape.Circle, Bun, new Vector2(290f, 105f));
                case IngredientType.ToppingLettuce: return new IngredientVisual(SimpleShape.Circle, Hex("#63B94D"), new Vector2(125f, 55f));
                case IngredientType.ToppingTomato: return new IngredientVisual(SimpleShape.Circle, Hex("#E84B3C"), new Vector2(110f, 48f));
                case IngredientType.ToppingCheese: return new IngredientVisual(SimpleShape.Triangle, Hex("#FFD84C"), new Vector2(135f, 75f));
                case IngredientType.ToppingOnion: return new IngredientVisual(SimpleShape.Triangle, Hex("#B981C7"), new Vector2(115f, 62f));
                case IngredientType.ToppingPickle: return new IngredientVisual(SimpleShape.Circle, Hex("#4C9B45"), new Vector2(90f, 42f));
                case IngredientType.SauceKetchup: return new IngredientVisual(SimpleShape.Circle, Hex("#D92E28"), new Vector2(22f, 22f));
                case IngredientType.SauceMustard: return new IngredientVisual(SimpleShape.Circle, Hex("#F5C542"), new Vector2(22f, 22f));
                default: return new IngredientVisual(SimpleShape.Rectangle, Color.white, new Vector2(80f, 50f));
            }
        }

        private static bool IsSauce(IngredientType type)
        {
            return type == IngredientType.SauceKetchup || type == IngredientType.SauceMustard;
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
