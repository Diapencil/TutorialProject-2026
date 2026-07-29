using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerAssemblyController : MonoBehaviour
    {
        [SerializeField] private BurgerSpriteCatalog spriteCatalog;

        private readonly BurgerCompletionPublisher completionPublisher = new BurgerCompletionPublisher();
        private readonly BurgerStackAssembler stackAssembler = new BurgerStackAssembler();
        private readonly List<CookingTrayDragSource> traySources = new List<CookingTrayDragSource>();

        private Camera mainCamera;
        private RectTransform dragLayer;
        private RectTransform grillDropArea;
        private RectTransform boardLayerRoot;
        private CookingCameraSlider cameraSlider;
        private BurgerSauceDrawingController sauceDrawingController;
        private BurgerPackagingController packagingController;
        private CookableGrillItemView activeGrillItem;
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
        private CookableGrillItemView draggedGrillItem;
        private bool isDraggingCompletedBurger;
        private Vector2 completedBurgerDragOffset;
        private Vector2 completedBurgerBoardPosition;
        private Vector2 lastCompletedBurgerPointer;

        public event Action<BurgerData> OnBurgerCompleted
        {
            add => completionPublisher.Completed += value;
            remove => completionPublisher.Completed -= value;
        }

        public BurgerData LastCompletedBurger => completionPublisher.LastCompletedBurger;

        public BurgerSpriteCatalog SpriteCatalog => spriteCatalog;

        private RectTransform BurgerStackRoot => stackAssembler.BurgerStackRoot;

        public void SetSpriteCatalog(BurgerSpriteCatalog value)
        {
            spriteCatalog = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool CanUseSauceTool(IngredientType type)
        {
            return sauceDrawingController != null &&
                sauceDrawingController.CanSelect(type);
        }

        public bool IsSauceToolSelected(IngredientType type)
        {
            return sauceDrawingController != null &&
                sauceDrawingController.IsSelected(type);
        }

        public void ToggleSauceTool(IngredientType type)
        {
            sauceDrawingController?.Toggle(type);
        }

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

            if (isDraggingCompletedBurger && BurgerStackRoot != null)
            {
                PositionCompletedBurger(lastCompletedBurgerPointer);
            }
        }

        private void OnDestroy()
        {
            if (cameraSlider != null)
            {
                cameraSlider.ZoneChanged -= HandleCameraZoneChanged;
            }
        }

        public bool CanBeginTrayDrag(CookingDragKind kind, IngredientType type)
        {
            if (stackAssembler.IsCompleted || hasActivePointerDrag)
            {
                return false;
            }

            if (kind == CookingDragKind.RawGrillItem)
            {
                return BurgerIngredientCatalog.IsGrillIngredient(type) &&
                    activeGrillItem == null;
            }

            if (kind == CookingDragKind.Sauce)
            {
                return false;
            }

            return stackAssembler.CanPlace(type);
        }

        public bool TryBeginTrayDrag(
            CookingDragKind kind,
            IngredientType type,
            SimpleShape shape,
            Color color,
            Vector2 size,
            Sprite sourceSprite,
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
            draggedGrillItem = null;
            lastPointerScreen = screenPosition;
            CreateDragGhost(shape, color, size, sourceSprite);
            PositionDragGhost(screenPosition);

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
        }

        public void EndTrayDrag(Vector2 screenPosition)
        {
            if (!hasActivePointerDrag)
            {
                return;
            }

            bool placed = false;
            bool reachedValidDropArea = false;
            if (activeDragKind == CookingDragKind.RawGrillItem)
            {
                if (cameraSlider.DestinationZone == CookingCameraZone.Grill &&
                    TryGetLocalPoint(grillDropArea, screenPosition, CookingCameraZone.Grill, out Vector2 grillLocal))
                {
                    reachedValidDropArea = true;
                    SpawnRawGrillItem(activeDragType, grillLocal);
                    placed = true;
                }
            }
            else if (activeDragKind == CookingDragKind.Ingredient)
            {
                if (cameraSlider.DestinationZone == CookingCameraZone.Board &&
                    TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 boardLocal))
                {
                    reachedValidDropArea = true;
                    placed = TryPlaceIngredient(activeDragType, boardLocal);
                }
            }
            if (!placed && !reachedValidDropArea)
            {
                if (activeDragKind == CookingDragKind.RawGrillItem)
                {
                    SetGrillStatus("굽기 재료를 불판 영역 안에 놓아 주세요.", true);
                }
                else
                {
                    SetBoardStatus("재료를 도마 영역 안에 놓아 주세요.", true);
                }
            }

            CleanupPointerDrag();
            RefreshControls();
        }

        public void HandleGrillItemTap(CookableGrillItemView grillItem)
        {
            if (grillItem == null || grillItem != activeGrillItem)
            {
                return;
            }

            IngredientType type = grillItem.GrillIngredientType;
            string itemName = GetGrillItemName(type);
            switch (grillItem.State.Phase)
            {
                case PattyGrillPhase.RawDough:
                    grillItem.State.TryPressDough();
                    SetGrillStatus(
                        type == IngredientType.Egg
                            ? "계란 조리를 시작했습니다. 3초 동안 익힙니다."
                            : itemName + " 1면을 3초 동안 굽습니다.",
                        false);
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    grillItem.State.TryFlip();
                    SetGrillStatus(itemName + "을(를) 뒤집었습니다. 2면을 3초 동안 굽습니다.", false);
                    break;
                case PattyGrillPhase.CookingSide1:
                    SetGrillStatus(
                        type == IngredientType.Egg
                            ? "계란을 익히고 있습니다."
                            : "아직 뒤집을 수 없습니다. 1면 조리가 끝날 때까지 기다려 주세요.",
                        type != IngredientType.Egg);
                    break;
                case PattyGrillPhase.CookingSide2:
                case PattyGrillPhase.Flipping:
                    SetGrillStatus("2면을 조리하고 있습니다.", false);
                    break;
                case PattyGrillPhase.Done:
                    SetGrillStatus(
                        stackAssembler.HasBottomBun
                            ? "완료된 " + itemName + "을(를) 오른쪽 화면 끝으로 드래그하세요."
                            : "도마 구역에 하단 번을 먼저 놓은 뒤 " + itemName + "을(를) 옮겨 주세요.",
                        !stackAssembler.HasBottomBun);
                    break;
                case PattyGrillPhase.Overcooked:
                    SetGrillStatus(itemName + "이(가) 타서 이동할 수 없습니다. 프로토타입 리셋을 사용해 주세요.", true);
                    break;
            }
        }

        public bool TryBeginCookedGrillItemDrag(CookableGrillItemView grillItem, Vector2 screenPosition)
        {
            if (grillItem == null || grillItem != activeGrillItem || hasActivePointerDrag || stackAssembler.IsCompleted)
            {
                return false;
            }

            if (!grillItem.State.CanDragToBoard)
            {
                ExplainBlockedGrillItemDrag(grillItem.GrillIngredientType, grillItem.State.Phase);
                return false;
            }

            string itemName = GetGrillItemName(grillItem.GrillIngredientType);
            if (!stackAssembler.HasBottomBun)
            {
                SetGrillStatus("도마 구역에 하단 번을 먼저 놓은 뒤 " + itemName + "을(를) 옮겨 주세요.", true);
                ShowToast("하단 번을 먼저 놓아 주세요");
                return false;
            }

            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetVisual(grillItem.GrillIngredientType);
            hasActivePointerDrag = true;
            activeDragKind = CookingDragKind.CookedGrillItem;
            activeDragType = grillItem.GrillIngredientType;
            draggedGrillItem = grillItem;
            lastPointerScreen = screenPosition;
            CreateDragGhost(visual.Shape, visual.Color, visual.Size, visual.SourceSprite);
            PositionDragGhost(screenPosition);
            SetGrillStatus(itemName + "을(를) 화면 오른쪽 끝으로 옮기면 도마로 전환됩니다.", false);
            return true;
        }

        public void RequestGrillItemTransfer(Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || activeDragKind != CookingDragKind.CookedGrillItem)
            {
                return;
            }

            if (screenPosition.x >= Screen.width * CookingPrototypeRules.PattyEdgeTransferScreenRatio &&
                cameraSlider.DestinationZone == CookingCameraZone.Grill)
            {
                cameraSlider.MoveToBoard();
                SetBoardStatus(
                    "도마로 이동했습니다. 원하는 위치에서 " +
                    BurgerIngredientCatalog.GetDisplayName(activeDragType) +
                    "을(를) 놓으세요.",
                    false);
            }
        }

        public bool EndCookedGrillItemDrag(CookableGrillItemView grillItem, Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || draggedGrillItem != grillItem)
            {
                return false;
            }

            IngredientType placedType = grillItem.GrillIngredientType;
            bool placed = false;
            bool reachedBoard = false;
            if (cameraSlider.DestinationZone == CookingCameraZone.Board &&
                TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 boardLocal))
            {
                reachedBoard = true;
                placed = TryPlaceIngredient(placedType, boardLocal);
            }

            CleanupPointerDrag();
            if (placed)
            {
                activeGrillItem = null;
                Destroy(grillItem.gameObject);
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(placedType) + "을(를) 도마에 놓았습니다.", false);
            }
            else if (!reachedBoard)
            {
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(placedType) + "을(를) 도마 영역 안에 놓아 주세요.", true);
            }

            RefreshControls();
            return placed;
        }

        public bool TryBeginCompletedBurgerDrag(Vector2 screenPosition)
        {
            if (!stackAssembler.IsCompleted || stackAssembler.BurgerStackRoot == null || isDraggingCompletedBurger ||
                hasActivePointerDrag || packagingController == null || packagingController.HasBurger)
            {
                return false;
            }

            if (!TryGetLocalPoint(boardLayerRoot, screenPosition, CookingCameraZone.Board, out Vector2 local))
            {
                return false;
            }

            completedBurgerBoardPosition = BurgerStackRoot.anchoredPosition;
            BurgerStackRoot.SetParent(dragLayer, true);
            BurgerStackRoot.SetAsLastSibling();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragLayer,
                    screenPosition,
                    mainCamera,
                    out Vector2 dragLocal))
            {
                RestoreCompletedBurgerToBoard();
                return false;
            }

            isDraggingCompletedBurger = true;
            lastCompletedBurgerPointer = screenPosition;
            completedBurgerDragOffset = BurgerStackRoot.anchoredPosition - dragLocal;
            SetBoardStatus("완성된 햄버거를 화면 오른쪽 끝으로 드래그해 포장대로 보내세요.", false);
            return true;
        }

        public void UpdateCompletedBurgerDrag(Vector2 screenPosition)
        {
            if (!isDraggingCompletedBurger || BurgerStackRoot == null)
            {
                return;
            }

            lastCompletedBurgerPointer = screenPosition;
            PositionCompletedBurger(screenPosition);

            if (screenPosition.x >= Screen.width * CookingPrototypeRules.CompletedBurgerTransferScreenRatio &&
                cameraSlider.DestinationZone == CookingCameraZone.Board)
            {
                cameraSlider.MoveToPackaging();
                packagingController.SetBurgerDragInProgress();
            }
        }

        public void EndCompletedBurgerDrag(Vector2 screenPosition)
        {
            if (!isDraggingCompletedBurger)
            {
                return;
            }

            UpdateCompletedBurgerDrag(screenPosition);
            bool reachedPackaging = cameraSlider.DestinationZone == CookingCameraZone.Packaging;
            bool placed = reachedPackaging &&
                TryGetLocalPoint(
                    packagingController.BurgerTray,
                    screenPosition,
                    CookingCameraZone.Packaging,
                    out Vector2 trayLocal) &&
                PlaceCompletedBurgerOnPackagingTray(trayLocal);
            isDraggingCompletedBurger = false;

            if (placed)
            {
                return;
            }

            RestoreCompletedBurgerToBoard();
            if (reachedPackaging)
            {
                packagingController.SetBurgerDropRejected();
                cameraSlider.MoveToBoard();
            }
            else
            {
                SetBoardStatus("조립 완료! 햄버거 전체를 오른쪽 끝으로 드래그하세요.", false);
            }
        }

        public void UpdateGrillItemStatus(CookableGrillItemView grillItem)
        {
            if (grillItem == null || grillItem != activeGrillItem || grillStatusText == null)
            {
                return;
            }

            string itemName = GetGrillItemName(grillItem.GrillIngredientType);
            switch (grillItem.State.Phase)
            {
                case PattyGrillPhase.CookingSide1:
                    grillStatusText.text = grillItem.State.RequiresFlip
                        ? $"{itemName} 1면 조리 중: {Mathf.Max(0f, CookingPrototypeRules.FirstSideCookSeconds - grillItem.State.PhaseElapsed):0.0}초"
                        : $"{itemName} 조리 중: {Mathf.Max(0f, CookingPrototypeRules.FirstSideCookSeconds - grillItem.State.PhaseElapsed):0.0}초";
                    grillStatusText.color = BurgerPrototypeTheme.Ink;
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    grillStatusText.text = $"{itemName} 뒤집기 가능 — {grillItem.State.GetFlipTimeRemaining():0.0}초 안에 탭하세요.";
                    grillStatusText.color = BurgerPrototypeTheme.Attention;
                    break;
                case PattyGrillPhase.CookingSide2:
                    grillStatusText.text = $"{itemName} 2면 조리 중: {Mathf.Max(0f, CookingPrototypeRules.SecondSideCookSeconds - grillItem.State.PhaseElapsed):0.0}초";
                    grillStatusText.color = BurgerPrototypeTheme.Ink;
                    break;
                case PattyGrillPhase.Done:
                    grillStatusText.text = stackAssembler.HasBottomBun
                        ? $"{itemName} 조리 완료 — {grillItem.State.GetDoneTimeRemaining():0.0}초 뒤 탐"
                        : $"{itemName} 조리 완료 — {grillItem.State.GetDoneTimeRemaining():0.0}초 안에 도마에 하단 번을 놓으세요";
                    grillStatusText.color = stackAssembler.HasBottomBun
                        ? BurgerPrototypeTheme.Success
                        : BurgerPrototypeTheme.Attention;
                    break;
                case PattyGrillPhase.Overcooked:
                    grillStatusText.text = "조리 실패: " + itemName + "이(가) 탔습니다.";
                    grillStatusText.color = BurgerPrototypeTheme.Warning;
                    break;
            }
        }

        private void BuildInterface()
        {
            if (cameraSlider != null)
            {
                return;
            }

            if (spriteCatalog == null)
            {
                throw new InvalidOperationException(
                    "BurgerAssemblyController requires serialized Sprite references before building the interface.");
            }

            spriteCatalog.Activate();
            BurgerAssemblyViewReferences view = new BurgerAssemblyViewBuilder(this, ResetPrototype).Build();
            mainCamera = view.MainCamera;
            uiFont = view.UiFont;
            dragLayer = view.DragLayer;
            grillDropArea = view.GrillDropArea;
            boardLayerRoot = view.BoardLayerRoot;
            cameraSlider = view.CameraSlider;
            sauceDrawingController = view.SauceDrawingController;
            packagingController = view.PackagingController;
            grillStatusText = view.GrillStatusText;
            boardStatusText = view.BoardStatusText;
            boardSummaryText = view.BoardSummaryText;
            toastText = view.ToastText;
            toastObject = view.ToastObject;
            stackAssembler.Configure(boardLayerRoot);
            sauceDrawingController.Configure(
                boardLayerRoot,
                stackAssembler,
                cameraSlider,
                HandleSauceDrawingChanged,
                SetBoardStatus);
            traySources.AddRange(view.TraySources);
            cameraSlider.ZoneChanged += HandleCameraZoneChanged;
        }

        private void HandleSauceDrawingChanged()
        {
            RefreshControls();
            RefreshBoardSummary();
        }

        private void HandleCameraZoneChanged(CookingCameraZone zone)
        {
            if (zone == CookingCameraZone.Grill)
            {
                SetGrillStatus(
                    activeGrillItem == null
                        ? "패티·베이컨·계란 중 하나를 불판에 놓아 주세요."
                        : GetGrillItemName(activeGrillItem.GrillIngredientType) + " 조리를 계속해 주세요.",
                    false);
            }
            else if (zone == CookingCameraZone.Board)
            {
                if (sauceDrawingController != null && sauceDrawingController.HasSelectedSauce)
                {
                    SetBoardStatus(
                        "소스 모드: 도마를 누른 채 움직여 그리세요. 선택한 소스 통을 다시 누르면 기본 마우스로 돌아갑니다.",
                        false);
                }
                else if (packagingController != null && packagingController.HasBurger)
                {
                    SetBoardStatus("완성된 햄버거는 포장 트레이에 놓여 있습니다.", false);
                }
                else if (stackAssembler.IsCompleted)
                {
                    SetBoardStatus("조립 완료! 햄버거 전체를 오른쪽 끝으로 드래그하세요.", false);
                }
                else if (!stackAssembler.HasBottomBun)
                {
                    SetBoardStatus("먼저 하단 번을 도마의 원하는 위치에 놓으세요.", false);
                }
                else
                {
                    SetBoardStatus("재료를 놓으면 하단 번 위에 자동으로 쌓입니다.", false);
                }
            }
            else if (zone == CookingCameraZone.Packaging && packagingController != null)
            {
                packagingController.SetZoneEntered();
            }
        }

        private void SpawnRawGrillItem(IngredientType type, Vector2 localPosition)
        {
            if (activeGrillItem != null || !BurgerIngredientCatalog.IsGrillIngredient(type))
            {
                return;
            }

            Vector2 size;
            switch (type)
            {
                case IngredientType.Patty:
                    size = new Vector2(150f, 150f);
                    break;
                case IngredientType.Bacon:
                    size = new Vector2(330f, 180f);
                    break;
                case IngredientType.Egg:
                    size = new Vector2(270f, 170f);
                    break;
                default:
                    return;
            }

            localPosition = BurgerUiFactory.ClampInside(grillDropArea.rect, localPosition, size);
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(
                "Cookable" + type,
                grillDropArea,
                type == IngredientType.Bacon ? SimpleShape.Rectangle : SimpleShape.Circle,
                BurgerPrototypeTheme.RawPatty,
                localPosition,
                size,
                true,
                spriteCatalog.GetInitialGrillIngredient(type));
            graphic.gameObject.AddComponent<CanvasGroup>();
            Text phaseLabel = BurgerUiFactory.CreateText(
                type + "Phase",
                graphic.rectTransform,
                uiFont,
                CookableGrillItemView.GetPhaseLabel(type, PattyGrillPhase.RawDough),
                18,
                FontStyle.Bold,
                Color.white,
                Vector2.zero,
                new Vector2(240f, 100f));
            Outline labelOutline = phaseLabel.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            labelOutline.effectDistance = new Vector2(1.5f, -1.5f);
            CookableGrillItemView view = graphic.gameObject.AddComponent<CookableGrillItemView>();
            view.Configure(
                this,
                type,
                graphic,
                phaseLabel);
            activeGrillItem = view;
            SetGrillStatus(
                GetRawGrillItemName(type) + "을(를) 올렸습니다. 재료를 탭해서 조리를 시작해 주세요.",
                false);
        }

        private bool TryPlaceIngredient(IngredientType type, Vector2 localPosition)
        {
            if (type != IngredientType.BunBottom && stackAssembler.BurgerStackRoot == null)
            {
                SetBoardStatus("먼저 하단 번을 도마에 놓아 햄버거의 기준 위치를 정하세요.", true);
                ShowToast("하단 번을 먼저 놓아 주세요");
                return false;
            }

            if (!stackAssembler.TryPlace(type, localPosition, out BurgerData completedBurger))
            {
                ExplainBlockedBoardPlacement(type);
                return false;
            }

            if (completedBurger != null)
            {
                CompleteBurger(completedBurger);
            }
            else
            {
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(type) + " 배치 완료", false);
            }

            RefreshBoardSummary();
            return true;
        }

        private void CompleteBurger(BurgerData burgerData)
        {
            var completedData = new BurgerData(
                burgerData.ingredients,
                sauceDrawingController.AttachSauceNearBurger(
                    BurgerStackRoot,
                    stackAssembler.StackHalfWidth,
                    stackAssembler.StackMinY,
                    stackAssembler.StackMaxY));
            EnableCompletedBurgerDrag();
            SetBoardStatus("조립 완료! 햄버거 전체를 오른쪽 끝으로 드래그하세요.", false);
            ShowToast("조립 완료");
            completionPublisher.Publish(completedData);
            RefreshControls();
        }

        private void EnableCompletedBurgerDrag()
        {
            if (BurgerStackRoot == null || BurgerStackRoot.Find("CompletedBurgerDragHandle") != null)
            {
                return;
            }

            Vector2 handleSize = new Vector2(
                stackAssembler.StackHalfWidth * 2f + 40f,
                stackAssembler.StackMaxY - stackAssembler.StackMinY + 40f);
            Vector2 handlePosition = new Vector2(
                0f,
                (stackAssembler.StackMinY + stackAssembler.StackMaxY) * 0.5f);
            RectTransform handle = BurgerUiFactory.CreateImage(
                "CompletedBurgerDragHandle",
                BurgerStackRoot,
                new Color(1f, 1f, 1f, 0.001f),
                handlePosition,
                handleSize,
                true);
            CompletedBurgerDragView dragView = handle.gameObject.AddComponent<CompletedBurgerDragView>();
            dragView.Configure(this);
            handle.SetAsLastSibling();
        }

        private void PositionCompletedBurger(Vector2 screenPosition)
        {
            if (BurgerStackRoot == null || dragLayer == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragLayer,
                    screenPosition,
                    mainCamera,
                    out Vector2 local))
            {
                BurgerStackRoot.anchoredPosition = local + completedBurgerDragOffset;
            }
        }

        private bool PlaceCompletedBurgerOnPackagingTray(Vector2 trayLocal)
        {
            if (BurgerStackRoot == null || packagingController == null ||
                !packagingController.TryPlaceBurger(
                    BurgerStackRoot,
                    trayLocal,
                    stackAssembler.StackHalfWidth,
                    stackAssembler.StackMinY,
                    stackAssembler.StackMaxY))
            {
                return false;
            }

            RectTransform dragHandle = BurgerStackRoot.Find("CompletedBurgerDragHandle") as RectTransform;
            if (dragHandle != null)
            {
                dragHandle.gameObject.SetActive(false);
            }

            SetBoardStatus("완성된 햄버거를 포장 트레이에 놓았습니다.", false);
            return true;
        }

        private void RestoreCompletedBurgerToBoard()
        {
            if (BurgerStackRoot == null || boardLayerRoot == null)
            {
                return;
            }

            BurgerStackRoot.SetParent(boardLayerRoot, false);
            BurgerUiFactory.SetRect(BurgerStackRoot, completedBurgerBoardPosition, Vector2.zero);
        }

        private void ExplainBlockedTrayDrag(CookingDragKind kind, IngredientType type)
        {
            if (stackAssembler.IsCompleted)
            {
                ShowToast("이미 조립이 완료되었습니다");
                return;
            }

            if (kind == CookingDragKind.RawGrillItem && activeGrillItem != null)
            {
                SetGrillStatus(
                    "현재 " + GetGrillItemName(activeGrillItem.GrillIngredientType) +
                    "을(를) 먼저 조리하거나 프로토타입을 리셋해 주세요.",
                    true);
                return;
            }

            if (type == IngredientType.BunBottom && stackAssembler.HasBottomBun)
            {
                SetBoardStatus("하단 번은 한 번만 놓을 수 있습니다.", true);
                ShowToast("하단 번이 이미 있습니다");
                return;
            }

            if (type != IngredientType.BunBottom && !stackAssembler.HasBottomBun)
            {
                SetBoardStatus("먼저 하단 번을 도마의 원하는 위치에 놓으세요.", true);
                ShowToast("하단 번을 먼저 놓아 주세요");
                return;
            }

            if (BurgerAssemblyState.IsTopping(type) &&
                stackAssembler.ToppingCount >= stackAssembler.MaximumToppings)
            {
                ShowToast("더 이상 담을 수 없습니다");
            }
        }

        private void ExplainBlockedBoardPlacement(IngredientType type)
        {
            if (stackAssembler.IsCompleted)
            {
                ShowToast("이미 조립이 완료되었습니다");
            }
            else if (type == IngredientType.BunBottom && stackAssembler.HasBottomBun)
            {
                SetBoardStatus("하단 번은 한 번만 놓을 수 있습니다.", true);
                ShowToast("하단 번이 이미 있습니다");
            }
            else if (type != IngredientType.BunBottom && !stackAssembler.HasBottomBun)
            {
                SetBoardStatus("먼저 하단 번을 도마의 원하는 위치에 놓으세요.", true);
                ShowToast("하단 번을 먼저 놓아 주세요");
            }
            else if (BurgerAssemblyState.IsTopping(type) &&
                stackAssembler.ToppingCount >= stackAssembler.MaximumToppings)
            {
                ShowToast("더 이상 담을 수 없습니다");
            }
        }

        private void ExplainBlockedGrillItemDrag(IngredientType type, PattyGrillPhase phase)
        {
            string itemName = GetGrillItemName(type);
            switch (phase)
            {
                case PattyGrillPhase.RawDough:
                    SetGrillStatus(GetRawGrillItemName(type) + "은(는) 아직 드래그할 수 없습니다. 먼저 탭해 주세요.", true);
                    break;
                case PattyGrillPhase.Flattened:
                case PattyGrillPhase.CookingSide1:
                    SetGrillStatus("아직 이동할 수 없습니다. " + itemName + " 조리가 끝날 때까지 기다려 주세요.", true);
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    SetGrillStatus(itemName + "을(를) 드래그하지 말고 5초 안에 탭해서 뒤집어 주세요.", true);
                    break;
                case PattyGrillPhase.Flipping:
                case PattyGrillPhase.CookingSide2:
                    SetGrillStatus("아직 이동할 수 없습니다. 2면 조리가 끝날 때까지 기다려 주세요.", true);
                    break;
                case PattyGrillPhase.Overcooked:
                    SetGrillStatus(itemName + "이(가) 타서 이동할 수 없습니다. 프로토타입 리셋을 사용해 주세요.", true);
                    break;
            }
        }

        private static string GetGrillItemName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return "패티";
                case IngredientType.Bacon: return "베이컨";
                case IngredientType.Egg: return "계란";
                default: return BurgerIngredientCatalog.GetDisplayName(type);
            }
        }

        private static string GetRawGrillItemName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Patty: return "패티볼";
                case IngredientType.Bacon: return "생베이컨";
                case IngredientType.Egg: return "날계란";
                default: return BurgerIngredientCatalog.GetDisplayName(type);
            }
        }

        private void ResetPrototype()
        {
            CleanupPointerDrag();
            isDraggingCompletedBurger = false;
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }
            if (toastObject != null)
            {
                toastObject.SetActive(false);
            }

            sauceDrawingController?.ResetDrawing();
            stackAssembler.Reset();

            if (activeGrillItem != null)
            {
                Destroy(activeGrillItem.gameObject);
                activeGrillItem = null;
            }

            completionPublisher.Reset();
            packagingController?.ResetPackaging();
            cameraSlider.SetImmediate(CookingCameraZone.Grill);
            SetGrillStatus("패티·베이컨·계란 중 하나를 불판에 놓아 주세요.", false);
            SetBoardStatus("먼저 하단 번을 도마의 원하는 위치에 놓으세요.", false);
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

            var builder = new StringBuilder();
            builder.Append("배치 ").Append(stackAssembler.PlacedIngredientCount).Append("개")
                .Append(" · 토핑 ").Append(stackAssembler.ToppingCount).Append('/').Append(stackAssembler.MaximumToppings)
                .Append(" · 소스 ")
                .Append(sauceDrawingController != null ? sauceDrawingController.StrokeCount : 0)
                .Append("획");
            if (stackAssembler.IsCompleted)
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
            grillStatusText.color = warning ? BurgerPrototypeTheme.Warning : BurgerPrototypeTheme.Ink;
        }

        private void SetBoardStatus(string message, bool warning)
        {
            if (boardStatusText == null)
            {
                return;
            }
            boardStatusText.text = message;
            boardStatusText.color = warning ? BurgerPrototypeTheme.Warning : BurgerPrototypeTheme.Ink;
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

        private void CreateDragGhost(SimpleShape shape, Color color, Vector2 size, Sprite sourceSprite = null)
        {
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(
                "DragGhost",
                dragLayer,
                shape,
                color,
                Vector2.zero,
                size,
                false,
                sourceSprite);
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
            draggedGrillItem = null;
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

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    target,
                    screenPosition,
                    mainCamera,
                    out local))
            {
                return false;
            }

            local.x += cameraSlider.CurrentContentX - cameraSlider.GetContentX(targetZone);
            return target.rect.Contains(local);
        }

    }
}
