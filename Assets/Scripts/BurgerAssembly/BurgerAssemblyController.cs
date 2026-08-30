using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SheepSheepBurger.Audio;
using SheepSheepBurger.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BurgerAssemblyController : MonoBehaviour
    {
        [SerializeField] private BurgerSpriteCatalog spriteCatalog;
        [SerializeField] private CookingSceneSchema cookingSchema;

        [Header("Editable Cooking Scene")]
        [SerializeField] private bool generateEditableViewInEditor = true;
        [SerializeField] private Camera editableMainCamera;
        [SerializeField] private RectTransform editableCanvasRoot;
        [SerializeField] private RectTransform editablePanoramaRoot;
        [SerializeField] private RectTransform editableGrillRegion;
        [SerializeField] private RectTransform editableBoardRegion;
        [SerializeField] private RectTransform editablePackagingRegion;
        [SerializeField] private RectTransform editableGrillDropArea;
        [SerializeField] private RectTransform editableBoardDropArea;
        [SerializeField] private RectTransform editableIngredientTray;
        [SerializeField] private RectTransform editableDragLayer;

        private readonly BurgerCompletionPublisher completionPublisher = new BurgerCompletionPublisher();
        private readonly BurgerStackAssembler stackAssembler = new BurgerStackAssembler();
        private readonly List<CookingTrayDragSource> traySources = new List<CookingTrayDragSource>();
        private readonly List<CookableGrillItemView> grillItems = new List<CookableGrillItemView>();
        private readonly List<PlacedIngredientView> looseBoardIngredients = new List<PlacedIngredientView>();

        private Camera mainCamera;
        private RectTransform dragLayer;
        private PattyGreaseTrail pattyGreaseTrail;
        private RectTransform grillDropArea;
        private RectTransform boardLayerRoot;
        private CookingCameraSlider cameraSlider;
        private BurgerSauceDrawingController sauceDrawingController;
        private BurgerPackagingController packagingController;
        private CookableGrillItemView focusedGrillItem;
        private Text grillStatusText;
        private Text boardStatusText;
        private Text boardSummaryText;
        private Text cookingTimerText;
        private GameObject customerDialoguePopup;
        private Text customerDialogueSpeakerText;
        private Text customerDialogueBodyText;
        private Text toastText;
        private GameObject toastObject;
        private Font uiFont;
        private Coroutine toastRoutine;

        private bool hasActivePointerDrag;
        private CookingDragKind activeDragKind;
        private IngredientType activeDragType;
        private RectTransform dragGhost;
        private bool emitsPattyGrease;
        private Vector2 lastPointerScreen;
        private CookableGrillItemView draggedGrillItem;
        private PlacedIngredientView draggedBoardIngredient;
        private bool isDraggingCompletedBurger;
        private Vector2 completedBurgerDragOffset;
        private Vector2 completedBurgerBoardPosition;
        private Vector2 lastCompletedBurgerPointer;
        private int activeRecipeId = -1;
        private int currentOrderErrors;
        private bool currentOrderUsedHint;
        private bool currentOrderWasAttacked;
        private float cookingTimeRemaining = CookingPrototypeRules.CookingTimeLimitSeconds;
        private bool hasCookingTimeExpired;
        private int displayedCookingSeconds = -1;
        private string customerDialogueSpeaker = "손님";
        private string customerDialogue = "현재 손님의 대사가 없습니다.";

        public event Action<BurgerData> OnBurgerCompleted
        {
            add => completionPublisher.Completed += value;
            remove => completionPublisher.Completed -= value;
        }

        public BurgerData LastCompletedBurger => completionPublisher.LastCompletedBurger;

        public event Action<PaymentResult> OnPaymentCalculated
        {
            add => completionPublisher.PaymentCalculated += value;
            remove => completionPublisher.PaymentCalculated -= value;
        }

        public PaymentResult LastPaymentResult => completionPublisher.LastPaymentResult;

        public event Action OnCookingTimeExpired;
        /// <summary>첫 조리 튜토리얼 등 외부 흐름에서 조리 행동을 관찰할 때 사용합니다.</summary>
        public event Action<IngredientType> GrillIngredientPlaced;
        public event Action<IngredientType, PattyGrillPhase> GrillPhaseChanged;
        public event Action<IngredientType> BoardIngredientPlaced;
        public event Action<IngredientType> SauceApplied;

        public float CookingTimeRemaining => cookingTimeRemaining;

        public bool HasCookingTimeExpired => hasCookingTimeExpired;

        public bool IsCustomerDialoguePopupOpen =>
            customerDialoguePopup != null && customerDialoguePopup.activeSelf;

        public CookingSceneSchema CookingSchema => cookingSchema;

        public BurgerSpriteCatalog SpriteCatalog => spriteCatalog;

        public BurgerPackagingController PackagingController => packagingController;

        private RectTransform BurgerStackRoot => stackAssembler.BurgerStackRoot;

        public void SetSpriteCatalog(BurgerSpriteCatalog value)
        {
            spriteCatalog = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void SetCustomerDialogue(string speaker, string dialogue)
        {
            customerDialogueSpeaker = string.IsNullOrWhiteSpace(speaker) ? "손님" : speaker.Trim();
            customerDialogue = string.IsNullOrWhiteSpace(dialogue)
                ? "현재 손님의 대사가 없습니다."
                : dialogue.Trim();
            RefreshCustomerDialoguePopup();
        }

        public void SetCookingSchema(CookingSceneSchema value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            value.Validate();
            cookingSchema = value.Clone();
            activeRecipeId = cookingSchema.defaultRecipeId;
        }

        public void ConfigureCookingOrder(
            int recipeId,
            int errorCount,
            bool usedHint,
            bool wasAttacked)
        {
            if (errorCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(errorCount));
            }

            EnsureCookingSchema();
            cookingSchema.GetRecipe(recipeId);
            activeRecipeId = recipeId;
            currentOrderErrors = errorCount;
            currentOrderUsedHint = usedHint;
            currentOrderWasAttacked = wasAttacked;
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

            GameManager.GetOrCreate();
            BuildInterface();
            RefreshControls();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            QueueEditableViewBuild();
        }

        private void OnValidate()
        {
            QueueEditableViewBuild();
        }

        [ContextMenu("Ensure Editable Cooking View")]
        private void EnsureEditableCookingView()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BuildEditableViewNow();
        }

        [ContextMenu("Rebuild Editable Cooking View")]
        private void RebuildEditableCookingView()
        {
            if (Application.isPlaying)
            {
                return;
            }

            GameObject existingCanvas = FindSceneObject("CookingCanvas");
            if (existingCanvas != null)
            {
                DestroyControllerObject(existingCanvas);
            }

            ResetInterfaceReferences();
            BuildEditableViewNow();
        }

        private void QueueEditableViewBuild()
        {
            if (Application.isPlaying || !generateEditableViewInEditor)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall -= BuildEditableViewNow;
            UnityEditor.EditorApplication.delayCall += BuildEditableViewNow;
        }

        private void BuildEditableViewNow()
        {
            if (this == null ||
                Application.isPlaying ||
                !generateEditableViewInEditor ||
                !isActiveAndEnabled ||
                !gameObject.scene.IsValid() ||
                string.IsNullOrEmpty(gameObject.scene.path))
            {
                return;
            }

            try
            {
                BuildInterface();
                RefreshControls();
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[BurgerAssembly] Editable cooking view was not built: " + exception.Message, this);
            }
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TickCookingTimer(Time.deltaTime);

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

            if (!IsIngredientUnlocked(type))
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

        public bool IsIngredientUnlocked(IngredientType type)
        {
            return ShopProgressBridge.IsCookingIngredientUnlocked(type);
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

        public bool TryUseTrayItemOnClick(CookingDragKind kind, IngredientType type)
        {
            if (!CanBeginTrayDrag(kind, type))
            {
                ExplainBlockedTrayDrag(kind, type);
                return false;
            }

            bool used;
            if (kind == CookingDragKind.RawGrillItem)
            {
                SpawnRawGrillItem(type, GetAutomaticGrillSpawnPosition());
                used = true;
            }
            else if (kind == CookingDragKind.Ingredient)
            {
                Vector2 boardPosition = stackAssembler.BurgerStackRoot != null
                    ? stackAssembler.BurgerStackRoot.anchoredPosition
                    : Vector2.zero;
                used = TryPlaceIngredient(type, boardPosition);
            }
            else
            {
                used = false;
            }

            RefreshControls();
            return used;
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
                    if (grillItem.State.TryPressDough())
                    {
                        if (type == IngredientType.Patty)
                        {
                            AudioManager.GetOrCreate().PlaySfx(AudioCueIds.PressPatty);
                        }
                        AudioManager.GetOrCreate().PlaySfx(AudioCueIds.GrillSizzle);
                    }
                    SetGrillStatus(
                        type == IngredientType.Egg
                            ? $"계란 조리를 시작했습니다. {grillItem.State.FirstSideCookDuration:0.0}초 동안 익힙니다."
                            : $"{itemName} 1면을 {grillItem.State.FirstSideCookDuration:0.0}초 동안 굽습니다.",
                        false);
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    grillItem.State.TryFlip();
                    SetGrillStatus(
                        $"{itemName}을(를) 뒤집었습니다. 2면을 {grillItem.State.SecondSideCookDuration:0.0}초 동안 굽습니다.",
                        false);
                    break;
                case PattyGrillPhase.CookingSide1:
                    SetGrillStatus(
                        type == IngredientType.Egg
                            ? "계란을 익히고 있습니다."
                            : !grillItem.State.RequiresFlip
                                ? itemName + "을(를) 익히고 있습니다."
                            : "아직 뒤집을 수 없습니다. 1면 조리가 끝날 때까지 기다려 주세요.",
                        type != IngredientType.Egg && grillItem.State.RequiresFlip);
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
            BeginPattyGreaseTrail(grillItem.State);
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
            BeginPattyGreaseTrail(ingredient.CookingState);
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

            if (screenPosition.x < Screen.width * CookingPrototypeRules.CompletedBurgerTransferScreenRatio)
            {
                return;
            }

            if (cameraSlider.DestinationZone == CookingCameraZone.Grill)
            {
                cameraSlider.MoveToBoard();
            }
            else if (cameraSlider.DestinationZone == CookingCameraZone.Board)
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
                        ? $"{itemName} 1면 조리 중: {Mathf.Max(0f, grillItem.State.FirstSideCookDuration - grillItem.State.PhaseElapsed):0.0}초"
                        : $"{itemName} 조리 중: {Mathf.Max(0f, grillItem.State.FirstSideCookDuration - grillItem.State.PhaseElapsed):0.0}초";
                    grillStatusText.color = BurgerPrototypeTheme.Ink;
                    break;
                case PattyGrillPhase.ReadyToFlip:
                    grillStatusText.text = $"{itemName} 뒤집기 가능 — {grillItem.State.GetFlipTimeRemaining():0.0}초 안에 탭하세요.";
                    grillStatusText.color = BurgerPrototypeTheme.Attention;
                    break;
                case PattyGrillPhase.CookingSide2:
                    grillStatusText.text = $"{itemName} 2면 조리 중: {Mathf.Max(0f, grillItem.State.SecondSideCookDuration - grillItem.State.PhaseElapsed):0.0}초";
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
            if (spriteCatalog == null)
            {
                throw new InvalidOperationException(
                    "BurgerAssemblyController requires serialized Sprite references before building the interface.");
            }

            EnsureCookingSchema();
            spriteCatalog.Activate();
            if (IsInterfaceReady)
            {
                CacheEditableReferences();
                RefreshSceneShapeSprites(editableCanvasRoot);
                PrepareEditableInteractionObjects();
            }
            else if (!TryBindExistingInterface())
            {
                BurgerAssemblyViewReferences view = new BurgerAssemblyViewBuilder(
                    this,
                    ResetPrototype,
                    OpenCustomerDialoguePopup,
                    CloseCustomerDialoguePopup).Build();
                ApplyViewReferences(view);
            }

            CompleteInterfaceBinding();
        }

        private bool IsInterfaceReady =>
            cameraSlider != null &&
            dragLayer != null &&
            grillDropArea != null &&
            boardLayerRoot != null &&
            sauceDrawingController != null &&
            packagingController != null;

        private void ApplyViewReferences(BurgerAssemblyViewReferences view)
        {
            mainCamera = view.MainCamera;
            uiFont = view.UiFont;
            dragLayer = view.DragLayer;
            pattyGreaseTrail = view.PattyGreaseTrail;
            grillDropArea = view.GrillDropArea;
            boardLayerRoot = view.BoardLayerRoot;
            cameraSlider = view.CameraSlider;
            sauceDrawingController = view.SauceDrawingController;
            packagingController = view.PackagingController;
            grillStatusText = view.GrillStatusText;
            boardStatusText = view.BoardStatusText;
            boardSummaryText = view.BoardSummaryText;
            cookingTimerText = view.CookingTimerText;
            customerDialoguePopup = view.CustomerDialoguePopup;
            customerDialogueSpeakerText = view.CustomerDialogueSpeakerText;
            customerDialogueBodyText = view.CustomerDialogueBodyText;
            toastText = view.ToastText;
            toastObject = view.ToastObject;
            CacheEditableReferences();
        }

        private bool TryBindExistingInterface()
        {
            RectTransform canvasRoot = editableCanvasRoot != null
                ? editableCanvasRoot
                : FindSceneComponent<RectTransform>("CookingCanvas");
            if (canvasRoot == null)
            {
                return false;
            }

            RectTransform panoramaRoot = editablePanoramaRoot != null
                ? editablePanoramaRoot
                : FindChildByName<RectTransform>(canvasRoot, "CookingPanorama");
            if (panoramaRoot == null)
            {
                return false;
            }

            panoramaRoot.localScale = Vector3.one * BurgerAssemblyViewBuilder.ViewZoom;

            mainCamera = editableMainCamera != null
                ? editableMainCamera
                : Camera.main != null
                    ? Camera.main
                    : FindSceneComponent<Camera>("Main Camera");
            uiFont = ResolveUiFont();

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                if (mainCamera != null)
                {
                    canvas.planeDistance = Mathf.Max(1f, mainCamera.nearClipPlane + 0.1f);
                }
            }

            cameraSlider = canvasRoot.GetComponent<CookingCameraSlider>();
            if (cameraSlider == null)
            {
                cameraSlider = canvasRoot.gameObject.AddComponent<CookingCameraSlider>();
            }
            cameraSlider.Configure(
                panoramaRoot,
                BurgerAssemblyViewBuilder.GrillViewX,
                BurgerAssemblyViewBuilder.BoardViewX,
                BurgerAssemblyViewBuilder.PackagingViewX);

            dragLayer = editableDragLayer != null
                ? editableDragLayer
                : FindChildByName<RectTransform>(canvasRoot, "DragLayer");
            grillDropArea = editableGrillDropArea != null
                ? editableGrillDropArea
                : FindChildByName<RectTransform>(canvasRoot, "GrillDropArea");
            RectTransform boardDropArea = editableBoardDropArea != null
                ? editableBoardDropArea
                : FindChildByName<RectTransform>(canvasRoot, "BoardDropArea");
            boardLayerRoot = FindChildByName<RectTransform>(boardDropArea, "BoardIngredientLayer");
            if (boardLayerRoot == null || dragLayer == null || grillDropArea == null)
            {
                return false;
            }

            pattyGreaseTrail = FindChildByName<PattyGreaseTrail>(panoramaRoot, "PattyGreaseTrail");
            if (pattyGreaseTrail == null)
            {
                pattyGreaseTrail = PattyGreaseTrail.Create(panoramaRoot);
            }

            sauceDrawingController = boardDropArea.GetComponent<BurgerSauceDrawingController>();
            if (sauceDrawingController == null)
            {
                sauceDrawingController = boardDropArea.gameObject.AddComponent<BurgerSauceDrawingController>();
            }

            RectTransform packagingRegion = editablePackagingRegion != null
                ? editablePackagingRegion
                : FindChildByName<RectTransform>(canvasRoot, "PackagingRegion");
            if (packagingRegion == null)
            {
                return false;
            }

            packagingController = packagingRegion.GetComponent<BurgerPackagingController>();
            if (packagingController == null)
            {
                packagingController = packagingRegion.gameObject.AddComponent<BurgerPackagingController>();
            }

            cookingTimerText = FindChildByName<Text>(canvasRoot, "CookingTimerText");
            customerDialoguePopup = FindChildGameObjectByName(canvasRoot, "CustomerDialoguePopup");
            customerDialogueSpeakerText = FindChildByName<Text>(canvasRoot, "CustomerDialogueSpeakerText");
            customerDialogueBodyText = FindChildByName<Text>(canvasRoot, "CustomerDialogueBodyText");
            grillStatusText = FindChildByName<Text>(canvasRoot, "GrillStatusText");
            boardStatusText = FindChildByName<Text>(canvasRoot, "BoardStatusText");
            boardSummaryText = FindChildByName<Text>(canvasRoot, "BoardSummaryText");
            toastText = FindChildByName<Text>(canvasRoot, "ToastText");
            toastObject = toastText != null ? toastText.gameObject : null;

            CacheEditableReferences();
            RefreshSceneShapeSprites(canvasRoot);
            PrepareEditableInteractionObjects();
            return true;
        }

        private void CompleteInterfaceBinding()
        {
            traySources.Clear();
            ConfigureTraySources();
            BindSceneButton("LeftTrashReset", ResetPrototype);
            BindSceneButton("RightTrashReset", ResetPrototype);
            BindSceneButton("CustomerDialogueButton", OpenCustomerDialoguePopup);
            BindSceneButton("CustomerDialogueCloseButton", CloseCustomerDialoguePopup);
            PrepareEditableInteractionObjects();
            BurgerAssemblyViewBuilder.EnsureEventSystem();
            stackAssembler.Configure(boardLayerRoot);
            sauceDrawingController.Configure(
                boardLayerRoot,
                stackAssembler,
                cameraSlider,
                HandleSauceDrawingChanged,
                SetBoardStatus,
                type => SauceApplied?.Invoke(type));
            packagingController.Configure(editablePackagingRegion, uiFont);
            cameraSlider.ZoneChanged -= HandleCameraZoneChanged;
            cameraSlider.ZoneChanged += HandleCameraZoneChanged;
            ResetCookingTimer();
            RefreshCustomerDialoguePopup();
        }

        private void ConfigureTraySources()
        {
            ConfigureTraySource("RawPattySource", CookingDragKind.RawGrillItem, IngredientType.Patty);
            ConfigureTraySource("RawBaconSource", CookingDragKind.RawGrillItem, IngredientType.Bacon);
            ConfigureTraySource("RawEggSource", CookingDragKind.RawGrillItem, IngredientType.Egg);

            foreach (BurgerTrayItemDefinition item in BurgerIngredientCatalog.GetBoardTrayItems())
            {
                ConfigureTraySource(item.ObjectName, item.Kind, item.Type);
            }
        }

        private void ConfigureTraySource(
            string sourceName,
            CookingDragKind kind,
            IngredientType type)
        {
            CookingTrayDragSource source = FindChildByName<CookingTrayDragSource>(
                editableCanvasRoot,
                sourceName);
            if (source == null)
            {
                return;
            }

            if (source.GetComponent<CanvasGroup>() == null)
            {
                source.gameObject.AddComponent<CanvasGroup>();
            }

            BurgerIngredientVisual trayVisual = BurgerIngredientCatalog.GetTrayVisual(type);
            BurgerIngredientVisual dragVisual = kind == CookingDragKind.Ingredient
                ? BurgerIngredientCatalog.GetVisual(type)
                : trayVisual;
            RectTransform sourceRect = source.GetComponent<RectTransform>();
            SimpleShapeGraphic trayIcon = FindChildByName<SimpleShapeGraphic>(
                source.transform,
                sourceName + "Icon");
            if (trayIcon == null && sourceRect != null)
            {
                trayIcon = BurgerUiFactory.RebuildTrayVisualPile(
                    source.transform,
                    sourceName,
                    trayVisual,
                    sourceRect.sizeDelta,
                    !BurgerIngredientCatalog.IsSauce(type));
            }
            else if (trayIcon == null)
            {
                trayIcon = BurgerUiFactory.CreateShape(
                    sourceName + "Icon",
                    source.transform as RectTransform,
                    trayVisual.Shape,
                    trayVisual.Color,
                    Vector2.zero,
                    trayVisual.Size,
                    false,
                    trayVisual.SourceSprite);
            }

            source.Configure(
                this,
                kind,
                type,
                dragVisual.Shape,
                dragVisual.Color,
                dragVisual.Size,
                dragVisual.SourceSprite,
                trayIcon);
            if (!traySources.Contains(source))
            {
                traySources.Add(source);
            }
        }

        private void BindSceneButton(string objectName, UnityAction action)
        {
            if (editableCanvasRoot == null || action == null)
            {
                return;
            }

            Button button = FindChildByName<Button>(editableCanvasRoot, objectName);
            if (button == null)
            {
                return;
            }

            Graphic graphic = button.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void CacheEditableReferences()
        {
            editableMainCamera = mainCamera;
            editableCanvasRoot = cameraSlider != null
                ? cameraSlider.transform as RectTransform
                : editableCanvasRoot;
            editablePanoramaRoot = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "CookingPanorama")
                : editablePanoramaRoot;
            editableGrillRegion = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "GrillRegion")
                : editableGrillRegion;
            editableBoardRegion = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "BoardRegion")
                : editableBoardRegion;
            editablePackagingRegion = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "PackagingRegion")
                : editablePackagingRegion;
            editableGrillDropArea = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "GrillDropArea")
                : editableGrillDropArea;
            editableBoardDropArea = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "BoardDropArea")
                : editableBoardDropArea;
            editableIngredientTray = editableCanvasRoot != null
                ? FindChildByName<RectTransform>(editableCanvasRoot, "IngredientTray")
                : editableIngredientTray;
            editableDragLayer = dragLayer;
        }

        private void ResetInterfaceReferences()
        {
            mainCamera = null;
            dragLayer = null;
            pattyGreaseTrail = null;
            grillDropArea = null;
            boardLayerRoot = null;
            cameraSlider = null;
            sauceDrawingController = null;
            packagingController = null;
            grillStatusText = null;
            boardStatusText = null;
            boardSummaryText = null;
            cookingTimerText = null;
            customerDialoguePopup = null;
            customerDialogueSpeakerText = null;
            customerDialogueBodyText = null;
            toastText = null;
            toastObject = null;
            editableMainCamera = null;
            editableCanvasRoot = null;
            editablePanoramaRoot = null;
            editableGrillRegion = null;
            editableBoardRegion = null;
            editablePackagingRegion = null;
            editableGrillDropArea = null;
            editableBoardDropArea = null;
            editableIngredientTray = null;
            editableDragLayer = null;
            traySources.Clear();
        }

        private static Font ResolveUiFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
            return font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void RefreshSceneShapeSprites(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (SimpleShapeGraphic graphic in root.GetComponentsInChildren<SimpleShapeGraphic>(true))
            {
                if (graphic == null)
                {
                    continue;
                }

                SimpleShape shape = graphic.Shape;
                graphic.Shape = shape;
                HideOutlineWhenTransparent(graphic);
            }
        }

        private static void HideOutlineWhenTransparent(SimpleShapeGraphic graphic)
        {
            if (graphic.color.a > 0.01f)
            {
                return;
            }

            foreach (Outline outline in graphic.GetComponents<Outline>())
            {
                Color outlineColor = outline.effectColor;
                outlineColor.a = 0f;
                outline.effectColor = outlineColor;
            }
        }

        private void PrepareEditableInteractionObjects()
        {
            if (editableCanvasRoot == null)
            {
                return;
            }

            ApplyTemporaryGuideVisibility("GrillDropArea", BurgerPrototypeTheme.Hex("#E74C3C4D"));
            ApplyTemporaryGuideVisibility("BoardDropArea", BurgerPrototypeTheme.Hex("#18A9994D"));
            ApplyTemporaryGuideVisibility("PackagingTray", BurgerPrototypeTheme.Hex("#F4B9424D"));

            SetRaycastTarget("GrillDropArea", Application.isPlaying);
            SetRaycastTarget("BoardDropArea", Application.isPlaying);
            SetRaycastTarget("LeftTrashReset", Application.isPlaying);
            SetRaycastTarget("RightTrashReset", Application.isPlaying);

            SendChildBehindSiblings("GrillDropArea");
            SendChildBehindSiblings("BoardDropArea");
            BringChildAboveSiblings("RawGrillTray");
            BringChildAboveSiblings("IngredientTray");
            BringChildAboveSiblings("KetchupTray");
            BringChildAboveSiblings("MustardTray");
            BringChildAboveSiblings("ToastText");
            BringChildAboveSiblings("PackageButton");

            if (editableDragLayer != null)
            {
                editableDragLayer.SetAsLastSibling();
            }
        }

        private void ApplyTemporaryGuideVisibility(string objectName, Color visibleColor)
        {
            SimpleShapeGraphic graphic = FindChildByName<SimpleShapeGraphic>(editableCanvasRoot, objectName);
            if (graphic == null)
            {
                return;
            }

            graphic.color = CookingPrototypeRules.ShowTemporaryInteractionAreas ? visibleColor : Color.clear;
            foreach (Outline outline in graphic.GetComponents<Outline>())
            {
                outline.effectColor = graphic.color.a <= 0.01f
                    ? Color.clear
                    : BurgerPrototypeTheme.Border;
            }
        }

        private void SetRaycastTarget(string objectName, bool raycastTarget)
        {
            Graphic graphic = FindChildByName<Graphic>(editableCanvasRoot, objectName);
            if (graphic != null)
            {
                graphic.raycastTarget = raycastTarget;
            }
        }

        private void SendChildBehindSiblings(string objectName)
        {
            RectTransform rect = FindChildByName<RectTransform>(editableCanvasRoot, objectName);
            if (rect != null)
            {
                rect.SetAsFirstSibling();
            }
        }

        private void BringChildAboveSiblings(string objectName)
        {
            RectTransform rect = FindChildByName<RectTransform>(editableCanvasRoot, objectName);
            if (rect != null)
            {
                rect.SetAsLastSibling();
            }
        }

        private GameObject FindSceneObject(string objectName)
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return GameObject.Find(objectName);
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindChildTransformByName(root.transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private T FindSceneComponent<T>(string objectName) where T : Component
        {
            GameObject found = FindSceneObject(objectName);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static GameObject FindChildGameObjectByName(Transform parent, string childName)
        {
            Transform found = FindChildTransformByName(parent, childName);
            return found != null ? found.gameObject : null;
        }

        private static T FindChildByName<T>(Transform parent, string childName) where T : Component
        {
            Transform found = FindChildTransformByName(parent, childName);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static Transform FindChildTransformByName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.gameObject.name == childName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform found = FindChildTransformByName(parent.GetChild(index), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private Vector2 GetAutomaticGrillSpawnPosition()
        {
            int slot = grillItems.Count % 6;
            int column = slot % 3;
            int row = slot / 3;
            return new Vector2((column - 1) * 180f, 90f - row * 180f);
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

            PattyGrillState state = existingState ?? new PattyGrillState(
                type,
                ShopProgressBridge.GetGrillCookTimeMultiplier(),
                ShopProgressBridge.GetGrillBurnChance());
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
            state.PhaseChanged += phase => GrillPhaseChanged?.Invoke(type, phase);
            GrillIngredientPlaced?.Invoke(type);
            focusedGrillItem = view;
            if (type == IngredientType.Bacon)
            {
                AudioManager.GetOrCreate().PlaySfx(AudioCueIds.PlaceBacon);
            }
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
                BoardIngredientPlaced?.Invoke(type);
                PlayBoardIngredientPlacementSfx(type);
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

            BoardIngredientPlaced?.Invoke(type);

            PlayBoardIngredientPlacementSfx(type);

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
                PlayBoardIngredientPlacementSfx(type);
                if (completedBurger != null)
                {
                    CompleteBurger(completedBurger);
                }
                else
                {
                    SetBoardStatus(BurgerIngredientCatalog.GetDisplayName(type) + "을(를) 버거 위에 쌓았습니다.", false);
                }
                BoardIngredientPlaced?.Invoke(type);
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

        private static void PlayBoardIngredientPlacementSfx(IngredientType type)
        {
            string audioId;
            switch (type)
            {
                case IngredientType.ToppingCheese:
                    audioId = AudioCueIds.PlaceCheese;
                    break;
                case IngredientType.ToppingLettuce:
                case IngredientType.ToppingTomato:
                case IngredientType.ToppingOnion:
                case IngredientType.ToppingPickle:
                case IngredientType.ToppingJalapeno:
                    audioId = AudioCueIds.PlaceVegetable;
                    break;
                default:
                    return;
            }

            AudioManager.GetOrCreate().PlaySfx(audioId);
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
            PaymentResult paymentResult = cookingSchema.Evaluate(
                completedData,
                activeRecipeId,
                currentOrderErrors,
                currentOrderUsedHint,
                currentOrderWasAttacked);
            EnableCompletedBurgerDrag();
            SetBoardStatus("조립 완료! 햄버거 전체를 오른쪽 끝으로 드래그하세요.", false);
            ShowToast("조립 완료");
            completionPublisher.Publish(completedData, paymentResult);
            RefreshControls();
        }

        private void EnsureCookingSchema()
        {
            if (cookingSchema == null || !cookingSchema.IsConfigured)
            {
                cookingSchema = CookingSceneSchema.CreatePrototypeDefaults();
            }
            else
            {
                cookingSchema.Validate();
            }

            if (!cookingSchema.recipes.Any(recipe => recipe.id == activeRecipeId))
            {
                activeRecipeId = cookingSchema.defaultRecipeId;
            }
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
            if (!IsIngredientUnlocked(type))
            {
                ShowToast(BurgerIngredientCatalog.GetDisplayName(type) + "은(는) 상점에서 해금해야 합니다");
                return;
            }

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
            pattyGreaseTrail?.ClearTrail();
            CloseCustomerDialoguePopup();
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
            currentOrderErrors = 0;
            currentOrderUsedHint = false;
            currentOrderWasAttacked = false;
            packagingController?.ResetPackaging();
            cameraSlider.SetImmediate(CookingCameraZone.Grill);
            SetGrillStatus("패티·베이컨·계란 중 하나를 불판에 놓아 주세요.", false);
            SetBoardStatus("재료를 자유롭게 놓을 수 있습니다. 하단 번 가까이에 놓으면 자동으로 쌓입니다.", false);
            RefreshBoardSummary();
            RefreshControls();
        }

        private void OpenCustomerDialoguePopup()
        {
            if (customerDialoguePopup == null)
            {
                return;
            }

            RefreshCustomerDialoguePopup();
            customerDialoguePopup.transform.SetAsLastSibling();
            customerDialoguePopup.SetActive(true);
        }

        private void CloseCustomerDialoguePopup()
        {
            if (customerDialoguePopup != null)
            {
                customerDialoguePopup.SetActive(false);
            }
        }

        private void RefreshCustomerDialoguePopup()
        {
            if (customerDialogueSpeakerText != null)
            {
                customerDialogueSpeakerText.text = customerDialogueSpeaker;
            }
            if (customerDialogueBodyText != null)
            {
                customerDialogueBodyText.text = customerDialogue;
            }
        }

        private void TickCookingTimer(float deltaTime)
        {
            if (hasCookingTimeExpired || LastCompletedBurger != null || deltaTime <= 0f)
            {
                return;
            }

            cookingTimeRemaining = Mathf.Max(0f, cookingTimeRemaining - deltaTime);
            RefreshCookingTimerText();
            if (cookingTimeRemaining > 0f)
            {
                return;
            }

            hasCookingTimeExpired = true;
            RefreshCookingTimerText();
            Debug.Log("[BurgerAssembly] Cooking time expired. Dummy timeout event invoked.");
            OnCookingTimeExpired?.Invoke();
        }

        private void ResetCookingTimer()
        {
            cookingTimeRemaining = CookingPrototypeRules.CookingTimeLimitSeconds;
            hasCookingTimeExpired = false;
            displayedCookingSeconds = -1;
            RefreshCookingTimerText();
        }

        private void RefreshCookingTimerText()
        {
            if (cookingTimerText == null)
            {
                return;
            }

            int wholeSeconds = Mathf.CeilToInt(cookingTimeRemaining);
            if (wholeSeconds == displayedCookingSeconds &&
                cookingTimerText.color == (hasCookingTimeExpired
                    ? BurgerPrototypeTheme.Warning
                    : wholeSeconds <= 10
                        ? BurgerPrototypeTheme.Attention
                        : Color.white))
            {
                return;
            }

            displayedCookingSeconds = wholeSeconds;
            cookingTimerText.text = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
            cookingTimerText.color = hasCookingTimeExpired
                ? BurgerPrototypeTheme.Warning
                : wholeSeconds <= 10
                    ? BurgerPrototypeTheme.Attention
                    : Color.white;
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
                if (emitsPattyGrease)
                {
                    pattyGreaseTrail?.MoveToWorldPosition(dragGhost.position);
                }
            }
        }

        private void BeginPattyGreaseTrail(PattyGrillState state)
        {
            emitsPattyGrease = activeDragType == IngredientType.Patty &&
                state != null &&
                state.Phase != PattyGrillPhase.RawDough &&
                state.Phase != PattyGrillPhase.Flattened;
            if (emitsPattyGrease && dragGhost != null)
            {
                pattyGreaseTrail?.BeginTrailAtWorldPosition(dragGhost.position);
            }
        }

        private void CleanupPointerDrag()
        {
            pattyGreaseTrail?.EndTrail();
            emitsPattyGrease = false;
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
