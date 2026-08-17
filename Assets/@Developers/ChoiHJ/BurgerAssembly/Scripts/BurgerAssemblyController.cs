using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<CookableGrillItemView> grillItems = new List<CookableGrillItemView>();
        private readonly List<PlacedIngredientView> looseBoardIngredients = new List<PlacedIngredientView>();

        private Camera mainCamera;
        private RectTransform dragLayer;
        private RectTransform grillDropArea;
        private RectTransform boardLayerRoot;
        private CookingCameraSlider cameraSlider;
        private BurgerSauceDrawingController sauceDrawingController;
        private BurgerPackagingController packagingController;
        private CookableGrillItemView focusedGrillItem;
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
        private PlacedIngredientView draggedBoardIngredient;
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

        public bool HasSelectedSauce => sauceDrawingController != null &&
            sauceDrawingController.HasSelectedSauce;

        public void ToggleSauceTool(IngredientType type)
        {
            sauceDrawingController?.Toggle(type);
        }

        public void ForwardSaucePointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            sauceDrawingController?.OnPointerDown(eventData);
        }

        public void ForwardSauceDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            sauceDrawingController?.OnDrag(eventData);
        }

        public void ForwardSaucePointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            sauceDrawingController?.OnPointerUp(eventData);
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
                return BurgerIngredientCatalog.IsGrillIngredient(type);
            }

            if (kind == CookingDragKind.Sauce)
            {
                return false;
            }

            return CanCreateNewBoardIngredient(type);
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
                if (TryGetLocalPoint(grillDropArea, screenPosition, out Vector2 grillLocal))
                {
                    reachedValidDropArea = true;
                    SpawnRawGrillItem(activeDragType, grillLocal);
                    placed = true;
                }
            }
            else if (activeDragKind == CookingDragKind.Ingredient)
            {
                if (TryGetLocalPoint(boardLayerRoot, screenPosition, out Vector2 boardLocal))
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
            if (grillItem == null || !grillItems.Contains(grillItem))
            {
                return;
            }

            focusedGrillItem = grillItem;

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
                    SetGrillStatus("완료된 " + itemName + "을 원하는 위치로 옮길 수 있습니다.", false);
                    break;
                case PattyGrillPhase.Overcooked:
                    SetGrillStatus(itemName + "이(가) 탔지만 원하는 위치로 옮길 수 있습니다.", true);
                    break;
            }
        }

        public bool TryBeginCookedGrillItemDrag(CookableGrillItemView grillItem, Vector2 screenPosition)
        {
            if (grillItem == null || !grillItems.Contains(grillItem) ||
                hasActivePointerDrag || stackAssembler.IsCompleted)
            {
                return false;
            }

            string itemName = GetGrillItemName(grillItem.GrillIngredientType);
            BurgerIngredientVisual visual = GetBoardVisual(
                grillItem.GrillIngredientType,
                grillItem.State);
            hasActivePointerDrag = true;
            activeDragKind = CookingDragKind.CookedGrillItem;
            activeDragType = grillItem.GrillIngredientType;
            draggedGrillItem = grillItem;
            focusedGrillItem = grillItem;
            lastPointerScreen = screenPosition;
            CreateDragGhost(visual.Shape, visual.Color, visual.Size, visual.SourceSprite);
            PositionDragGhost(screenPosition);
            SetGrillStatus(itemName + "을(를) 이동합니다. 오른쪽 끝으로 옮기면 도마로 전환됩니다.", false);
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
            if (TryGetLocalPoint(boardLayerRoot, screenPosition, out Vector2 boardLocal))
            {
                reachedBoard = true;
                placed = TryPlaceBoardIngredient(placedType, boardLocal, grillItem.State, false);
            }
            else if (TryGetLocalPoint(grillDropArea, screenPosition, out Vector2 grillLocal))
            {
                RectTransform rect = grillItem.GetComponent<RectTransform>();
                rect.anchoredPosition = BurgerUiFactory.ClampInside(
                    grillDropArea.rect,
                    grillLocal,
                    rect.sizeDelta);
                placed = true;
            }

            CleanupPointerDrag();
            if (placed && reachedBoard)
            {
                grillItems.Remove(grillItem);
                if (focusedGrillItem == grillItem)
                {
                    focusedGrillItem = grillItems.Count > 0 ? grillItems[grillItems.Count - 1] : null;
                }
                DestroyControllerObject(grillItem.gameObject);
                SetBoardStatus(
                    BurgerIngredientCatalog.GetDisplayName(placedType) +
                    "을(를) 도마에 놓았습니다. 다시 구우려면 화면 왼쪽 끝으로 드래그하세요.",
                    false);
            }
            else if (!reachedBoard && cameraSlider.DestinationZone == CookingCameraZone.Board)
            {
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(placedType) + "을(를) 도마 영역 안에 놓아 주세요.", true);
                cameraSlider.MoveToGrill();
            }

            RefreshControls();
            return placed;
        }

        public bool TryBeginBoardIngredientDrag(PlacedIngredientView ingredient, Vector2 screenPosition)
        {
            if (ingredient == null || hasActivePointerDrag || stackAssembler.IsCompleted ||
                (!stackAssembler.Contains(ingredient) && !looseBoardIngredients.Contains(ingredient)))
            {
                return false;
            }

            BurgerIngredientVisual visual = GetBoardVisual(
                ingredient.IngredientType,
                ingredient.CookingState);
            hasActivePointerDrag = true;
            activeDragKind = CookingDragKind.BoardIngredient;
            activeDragType = ingredient.IngredientType;
            draggedBoardIngredient = ingredient;
            lastPointerScreen = screenPosition;
            CreateDragGhost(visual.Shape, visual.Color, visual.Size, visual.SourceSprite);
            PositionDragGhost(screenPosition);
            return true;
        }

        public void RequestBoardIngredientTransfer(PlacedIngredientView ingredient, Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || draggedBoardIngredient != ingredient ||
                ingredient == null || ingredient.CookingState == null)
            {
                return;
            }

            if (screenPosition.x <= Screen.width * CookingPrototypeRules.BoardToGrillTransferScreenRatio &&
                cameraSlider.DestinationZone == CookingCameraZone.Board)
            {
                cameraSlider.MoveToGrill();
                SetGrillStatus(
                    BurgerIngredientCatalog.GetDisplayName(ingredient.IngredientType) +
                    "을(를) 불판에 놓으면 기존 조리 상태에서 이어서 굽습니다.",
                    false);
            }
        }

        public bool EndBoardIngredientDrag(PlacedIngredientView ingredient, Vector2 screenPosition)
        {
            if (!hasActivePointerDrag || draggedBoardIngredient != ingredient || ingredient == null)
            {
                return false;
            }

            IngredientType ingredientType = ingredient.IngredientType;
            PattyGrillState cookingState = ingredient.CookingState;
            bool placed = false;
            bool transferredToGrill = false;
            if (cookingState != null &&
                TryGetLocalPoint(grillDropArea, screenPosition, out Vector2 grillLocal))
            {
                placed = RemoveBoardIngredient(ingredient);
                transferredToGrill = placed;
                if (placed)
                {
                    SpawnGrillItem(ingredientType, grillLocal, cookingState);
                }
            }
            else if (TryGetLocalPoint(boardLayerRoot, screenPosition, out Vector2 boardLocal))
            {
                placed = MoveBoardIngredient(ingredient, boardLocal);
            }

            CleanupPointerDrag();
            if (!placed && cameraSlider.DestinationZone == CookingCameraZone.Grill)
            {
                cameraSlider.MoveToBoard();
                SetBoardStatus("재료를 불판 영역 안에 놓아 주세요.", true);
            }
            else if (transferredToGrill)
            {
                SetGrillStatus(
                    BurgerIngredientCatalog.GetDisplayName(ingredientType) +
                    "을(를) 불판으로 옮겼습니다.",
                    false);
            }

            RefreshBoardSummary();
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

            if (!TryGetLocalPoint(boardLayerRoot, screenPosition, out Vector2 local))
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
            bool reachedPackaging = TryGetLocalPoint(
                packagingController.BurgerTray,
                screenPosition,
                out Vector2 trayLocal);
            bool placed = reachedPackaging &&
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
            if (grillItem == null || grillItem != focusedGrillItem || grillStatusText == null)
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
                    grillStatusText.text = $"{itemName} 조리 완료 — {grillItem.State.GetDoneTimeRemaining():0.0}초 뒤 탐 · 언제든 이동 가능";
                    grillStatusText.color = BurgerPrototypeTheme.Success;
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
                    focusedGrillItem == null
                        ? "패티·베이컨·계란을 불판에 놓아 주세요. 여러 재료를 동시에 올릴 수 있습니다."
                        : GetGrillItemName(focusedGrillItem.GrillIngredientType) + " 조리를 계속하거나 원하는 위치로 옮기세요.",
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
                else
                {
                    SetBoardStatus(
                        stackAssembler.HasBottomBun
                            ? "하단 번 가까이에 놓으면 쌓이고, 멀리 놓으면 도마에 남습니다."
                            : "재료를 자유롭게 놓을 수 있습니다. 하단 번을 놓으면 근처 재료를 쌓을 수 있습니다.",
                        false);
                }
            }
            else if (zone == CookingCameraZone.Packaging && packagingController != null)
            {
                packagingController.SetZoneEntered();
            }
        }

        private void SpawnRawGrillItem(IngredientType type, Vector2 localPosition)
        {
            SpawnGrillItem(type, localPosition, null);
        }

        private void SpawnGrillItem(
            IngredientType type,
            Vector2 localPosition,
            PattyGrillState existingState)
        {
            if (!BurgerIngredientCatalog.IsGrillIngredient(type))
            {
                return;
            }

            PattyGrillState state = existingState ?? new PattyGrillState(type);
            Vector2 size = CookableGrillItemView.GetGrillSize(type, state.Phase);

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
                CookableGrillItemView.GetPhaseLabel(type, state.Phase),
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
                phaseLabel,
                state);
            grillItems.Add(view);
            focusedGrillItem = view;
            SetGrillStatus(
                existingState == null
                    ? GetRawGrillItemName(type) + "을(를) 올렸습니다. 재료를 탭해서 조리를 시작해 주세요."
                    : GetGrillItemName(type) + "을(를) 다시 올렸습니다. 기존 상태에서 조리를 이어갑니다.",
                false);
        }

        private bool TryPlaceIngredient(IngredientType type, Vector2 localPosition)
        {
            return TryPlaceBoardIngredient(type, localPosition, null, false);
        }

        private bool TryPlaceBoardIngredient(
            IngredientType type,
            Vector2 localPosition,
            PattyGrillState cookingState,
            bool movingExisting)
        {
            if (!movingExisting && !CanCreateNewBoardIngredient(type))
            {
                ExplainBlockedBoardPlacement(type);
                return false;
            }

            bool shouldStack = type == IngredientType.BunBottom ||
                (stackAssembler.HasBottomBun && stackAssembler.IsNearStack(localPosition));
            if (!shouldStack)
            {
                CreateLooseBoardIngredient(type, localPosition, cookingState);
                SetBoardStatus(
                    BurgerIngredientCatalog.GetDisplayName(type) + "을(를) 도마에 놓았습니다.",
                    false);
                RefreshBoardSummary();
                return true;
            }

            if (!stackAssembler.TryPlace(
                    type,
                    localPosition,
                    this,
                    cookingState,
                    out _,
                    out BurgerData completedBurger))
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
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(type) + "을(를) 버거 위에 쌓았습니다.", false);
            }

            RefreshBoardSummary();
            return true;
        }

        private bool MoveBoardIngredient(PlacedIngredientView ingredient, Vector2 localPosition)
        {
            if (ingredient.IngredientType == IngredientType.BunBottom &&
                stackAssembler.Contains(ingredient))
            {
                stackAssembler.MoveStack(localPosition);
                return true;
            }

            bool nearStack = stackAssembler.HasBottomBun && stackAssembler.IsNearStack(localPosition);
            if (stackAssembler.Contains(ingredient))
            {
                if (nearStack)
                {
                    return stackAssembler.MoveIngredient(ingredient, localPosition);
                }

                IngredientType type = ingredient.IngredientType;
                PattyGrillState cookingState = ingredient.CookingState;
                if (!stackAssembler.RemoveIngredient(ingredient))
                {
                    return false;
                }
                CreateLooseBoardIngredient(type, localPosition, cookingState);
                SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(type) + "을(를) 스택에서 분리했습니다.", false);
                return true;
            }

            if (!looseBoardIngredients.Contains(ingredient))
            {
                return false;
            }

            if (nearStack)
            {
                IngredientType type = ingredient.IngredientType;
                PattyGrillState cookingState = ingredient.CookingState;
                if (!stackAssembler.TryPlace(
                        type,
                        localPosition,
                        this,
                        cookingState,
                        out _,
                        out BurgerData completedBurger))
                {
                    return false;
                }

                looseBoardIngredients.Remove(ingredient);
                DestroyControllerObject(ingredient.gameObject);
                if (completedBurger != null)
                {
                    CompleteBurger(completedBurger);
                }
                else
                {
                    SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(type) + "을(를) 버거 위에 쌓았습니다.", false);
                }
                return true;
            }

            RectTransform rect = ingredient.RectTransform;
            rect.anchoredPosition = BurgerUiFactory.ClampInside(
                boardLayerRoot.rect,
                localPosition,
                rect.sizeDelta);
            ingredient.SetPlacement(stackAssembler.ReserveLooseLayerOrder(), false);
            rect.SetAsLastSibling();
            return true;
        }

        private PlacedIngredientView CreateLooseBoardIngredient(
            IngredientType type,
            Vector2 localPosition,
            PattyGrillState cookingState)
        {
            BurgerIngredientVisual visual = GetBoardVisual(type, cookingState);
            int layerOrder = stackAssembler.ReserveLooseLayerOrder();
            Vector2 position = BurgerUiFactory.ClampInside(
                boardLayerRoot.rect,
                localPosition,
                visual.Size);
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(
                "Loose" + type + "_" + layerOrder,
                boardLayerRoot,
                visual.Shape,
                visual.Color,
                position,
                visual.Size,
                true,
                visual.SourceSprite);
            graphic.gameObject.AddComponent<CanvasGroup>();
            PlacedIngredientView view = graphic.gameObject.AddComponent<PlacedIngredientView>();
            view.Configure(this, type, layerOrder, false, cookingState);
            view.RectTransform.SetAsLastSibling();
            looseBoardIngredients.Add(view);
            return view;
        }

        private bool RemoveBoardIngredient(PlacedIngredientView ingredient)
        {
            if (stackAssembler.Contains(ingredient))
            {
                return stackAssembler.RemoveIngredient(ingredient);
            }

            if (!looseBoardIngredients.Remove(ingredient))
            {
                return false;
            }

            DestroyControllerObject(ingredient.gameObject);
            return true;
        }

        private bool CanCreateNewBoardIngredient(IngredientType type)
        {
            if (stackAssembler.IsCompleted || BurgerIngredientCatalog.IsSauce(type))
            {
                return false;
            }

            if (type == IngredientType.BunBottom)
            {
                return !stackAssembler.HasBottomBun;
            }

            if (type == IngredientType.BunTop)
            {
                return !stackAssembler.HasTopBun &&
                    !looseBoardIngredients.Any(item =>
                        item != null && item.IngredientType == IngredientType.BunTop);
            }

            return !BurgerAssemblyState.IsTopping(type) ||
                GetBoardToppingCount() < stackAssembler.MaximumToppings;
        }

        private int GetBoardToppingCount()
        {
            return stackAssembler.ToppingCount + looseBoardIngredients.Count(item =>
                item != null && BurgerAssemblyState.IsTopping(item.IngredientType));
        }

        private static BurgerIngredientVisual GetBoardVisual(
            IngredientType type,
            PattyGrillState cookingState)
        {
            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetVisual(type);
            if (cookingState == null || !BurgerIngredientCatalog.IsGrillIngredient(type))
            {
                return visual;
            }

            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            Sprite sprite;
            switch (cookingState.Phase)
            {
                case PattyGrillPhase.Overcooked:
                    sprite = sprites.GetBurntGrillIngredient(type);
                    break;
                case PattyGrillPhase.Flipping:
                case PattyGrillPhase.CookingSide2:
                case PattyGrillPhase.Done:
                    sprite = sprites.GetCookedGrillIngredient(type);
                    break;
                case PattyGrillPhase.RawDough:
                    sprite = sprites.GetInitialGrillIngredient(type);
                    break;
                default:
                    sprite = sprites.GetRawGrillIngredient(type);
                    break;
            }
            return new BurgerIngredientVisual(visual.Shape, Color.white, visual.Size, sprite);
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

            if (type == IngredientType.BunBottom && stackAssembler.HasBottomBun)
            {
                SetBoardStatus("하단 번은 한 번만 놓을 수 있습니다.", true);
                ShowToast("하단 번이 이미 있습니다");
                return;
            }

            if (BurgerAssemblyState.IsTopping(type) &&
                GetBoardToppingCount() >= stackAssembler.MaximumToppings)
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
            else if (BurgerAssemblyState.IsTopping(type) &&
                GetBoardToppingCount() >= stackAssembler.MaximumToppings)
            {
                ShowToast("더 이상 담을 수 없습니다");
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

            foreach (CookableGrillItemView grillItem in grillItems)
            {
                if (grillItem != null)
                {
                    DestroyControllerObject(grillItem.gameObject);
                }
            }
            grillItems.Clear();
            focusedGrillItem = null;

            foreach (PlacedIngredientView looseIngredient in looseBoardIngredients)
            {
                if (looseIngredient != null)
                {
                    DestroyControllerObject(looseIngredient.gameObject);
                }
            }
            looseBoardIngredients.Clear();

            completionPublisher.Reset();
            packagingController?.ResetPackaging();
            cameraSlider.SetImmediate(CookingCameraZone.Grill);
            SetGrillStatus("패티·베이컨·계란 중 하나를 불판에 놓아 주세요.", false);
            SetBoardStatus("재료를 자유롭게 놓을 수 있습니다. 하단 번 가까이에 놓으면 자동으로 쌓입니다.", false);
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
            builder.Append("배치 ")
                .Append(stackAssembler.PlacedIngredientCount + looseBoardIngredients.Count(item => item != null))
                .Append("개")
                .Append(" · 토핑 ").Append(GetBoardToppingCount()).Append('/').Append(stackAssembler.MaximumToppings)
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
            draggedBoardIngredient = null;
            if (dragGhost != null)
            {
                DestroyControllerObject(dragGhost.gameObject);
                dragGhost = null;
            }
        }

        private static void DestroyControllerObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private bool TryGetLocalPoint(RectTransform target, Vector2 screenPosition, out Vector2 local)
        {
            local = Vector2.zero;
            if (target == null || mainCamera == null)
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

            return target.rect.Contains(local);
        }

    }
}
