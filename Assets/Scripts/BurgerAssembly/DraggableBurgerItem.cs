using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CookingTrayDragSource :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler
    {
        private BurgerAssemblyController controller;
        private CanvasGroup canvasGroup;
        private SimpleShape ghostShape;
        private Color ghostColor;
        private Vector2 ghostSize;
        private Sprite ghostSprite;
        private SimpleShapeGraphic trayIcon;
        private bool ownsActiveDrag;

        public CookingDragKind Kind { get; private set; }

        public IngredientType IngredientType { get; private set; }

        public Sprite DragSprite => ghostSprite;

        public void Configure(
            BurgerAssemblyController targetController,
            CookingDragKind kind,
            IngredientType ingredientType,
            SimpleShape shape,
            Color color,
            Vector2 size,
            Sprite sourceSprite,
            SimpleShapeGraphic iconGraphic)
        {
            controller = targetController;
            Kind = kind;
            IngredientType = ingredientType;
            ghostShape = shape;
            ghostColor = color;
            ghostSize = size;
            ghostSprite = sourceSprite;
            trayIcon = iconGraphic;
            canvasGroup = GetComponent<CanvasGroup>();
            RefreshAppearance();
        }

        public void RefreshAppearance()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            bool isSauceTool = Kind == CookingDragKind.Sauce;
            bool unlocked = controller != null && controller.IsIngredientUnlocked(IngredientType);
            bool available = controller != null &&
                unlocked &&
                (isSauceTool
                    ? controller.CanUseSauceTool(IngredientType)
                    : controller.CanBeginTrayDrag(Kind, IngredientType));
            bool selected = isSauceTool &&
                controller != null &&
                controller.IsSauceToolSelected(IngredientType);
            canvasGroup.alpha = !unlocked ? 0f : selected ? 1f : available ? 0.86f : 0.48f;
            canvasGroup.blocksRaycasts = unlocked;
            canvasGroup.interactable = unlocked;
            if (trayIcon != null)
            {
                trayIcon.gameObject.SetActive(unlocked && !selected);
                trayIcon.preserveAspect = false;
            }

            Outline outline = GetComponent<Outline>();
            if (outline != null)
            {
                SimpleShapeGraphic background = GetComponent<SimpleShapeGraphic>();
                if (background != null && background.color.a <= 0.01f)
                {
                    outline.effectColor = Color.clear;
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    return;
                }

                outline.effectColor = selected
                    ? BurgerPrototypeTheme.Success
                    : BurgerPrototypeTheme.Border;
                outline.effectDistance = selected
                    ? new Vector2(4f, -4f)
                    : new Vector2(1.5f, -1.5f);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Kind == CookingDragKind.Sauce)
            {
                ownsActiveDrag = false;
                return;
            }

            ownsActiveDrag = controller != null && controller.TryBeginTrayDrag(
                Kind,
                IngredientType,
                ghostShape,
                ghostColor,
                ghostSize,
                ghostSprite,
                eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ownsActiveDrag)
            {
                controller.UpdatePointerDrag(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            ownsActiveDrag = false;
            controller.EndTrayDrag(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (Kind == CookingDragKind.Sauce)
            {
                controller?.ToggleSauceTool(IngredientType);
                return;
            }

            controller?.TryUseTrayItemOnClick(Kind, IngredientType);
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PlacedIngredientView :
        MonoBehaviour,
        IPointerDownHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerUpHandler
    {
        private BurgerAssemblyController controller;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool ownsActiveDrag;
        private bool forwardsSauceGesture;

        public IngredientType IngredientType { get; private set; }

        public int LayerOrder { get; private set; }

        public bool IsStacked { get; private set; }

        public PattyGrillState CookingState { get; private set; }

        public RectTransform RectTransform => rectTransform;

        public void Configure(
            BurgerAssemblyController targetController,
            IngredientType type,
            int layerOrder,
            bool isStacked,
            PattyGrillState cookingState = null)
        {
            controller = targetController;
            IngredientType = type;
            LayerOrder = layerOrder;
            IsStacked = isStacked;
            CookingState = cookingState;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            RefreshCookingAppearance();
        }

        public void SetPlacement(int layerOrder, bool isStacked)
        {
            LayerOrder = layerOrder;
            IsStacked = isStacked;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            forwardsSauceGesture = controller != null && controller.HasSelectedSauce;
            if (forwardsSauceGesture)
            {
                controller.ForwardSaucePointerDown(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (forwardsSauceGesture)
            {
                return;
            }

            ownsActiveDrag = controller != null &&
                controller.TryBeginBoardIngredientDrag(this, eventData.position);
            if (ownsActiveDrag && canvasGroup != null)
            {
                canvasGroup.alpha = 0.35f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (forwardsSauceGesture)
            {
                controller?.ForwardSauceDrag(eventData);
                return;
            }

            if (!ownsActiveDrag)
            {
                return;
            }

            controller.UpdatePointerDrag(eventData.position);
            controller.RequestBoardIngredientTransfer(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (forwardsSauceGesture || !ownsActiveDrag)
            {
                return;
            }

            ownsActiveDrag = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            controller.EndBoardIngredientDrag(this, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (forwardsSauceGesture)
            {
                controller?.ForwardSaucePointerUp(eventData);
            }
            forwardsSauceGesture = false;
        }

        public IngredientPlacement Capture(RectTransform coordinateRoot)
        {
            if (coordinateRoot == null)
            {
                throw new ArgumentNullException(nameof(coordinateRoot));
            }

            Vector3 local = coordinateRoot.InverseTransformPoint(rectTransform.position);
            return new IngredientPlacement(
                IngredientType,
                new Vector2(local.x, local.y),
                LayerOrder,
                CookingState);
        }

        private void RefreshCookingAppearance()
        {
            if (CookingState == null)
            {
                return;
            }

            SimpleShapeGraphic graphic = GetComponent<SimpleShapeGraphic>();
            if (graphic == null)
            {
                return;
            }

            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            PattyGrillPhase phase = CookingState.Phase;
            graphic.SourceSprite = phase == PattyGrillPhase.Overcooked
                ? sprites.GetBurntGrillIngredient(IngredientType)
                : phase == PattyGrillPhase.Flipping ||
                    phase == PattyGrillPhase.CookingSide2 ||
                    phase == PattyGrillPhase.Done
                    ? sprites.GetCookedGrillIngredient(IngredientType)
                    : phase == PattyGrillPhase.RawDough
                        ? sprites.GetInitialGrillIngredient(IngredientType)
                        : sprites.GetRawGrillIngredient(IngredientType);
            graphic.color = Color.white;
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CompletedBurgerDragView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BurgerAssemblyController controller;
        private bool ownsActiveDrag;

        public void Configure(BurgerAssemblyController targetController)
        {
            controller = targetController;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ownsActiveDrag = controller != null && controller.TryBeginCompletedBurgerDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ownsActiveDrag)
            {
                controller.UpdateCompletedBurgerDrag(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            ownsActiveDrag = false;
            controller.EndCompletedBurgerDrag(eventData.position);
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CookableGrillItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BurgerAssemblyController controller;
        private SimpleShapeGraphic graphic;
        private SimpleShapeGraphic pattyCookingEffect;
        private GrillCookingSmoke cookingSmoke;
        private Text phaseText;
        private bool isHeld;
        private bool ownsActiveDrag;

        public PattyGrillState State { get; private set; }

        public IngredientType GrillIngredientType { get; private set; }

        public bool IsHeld => isHeld;

        public SimpleShapeGraphic PattyCookingEffect => pattyCookingEffect;

        public ParticleSystem PattySmokeParticleSystem =>
            cookingSmoke != null ? cookingSmoke.ParticleSystem : null;

        public ParticleSystem CookingSmokeParticleSystem =>
            cookingSmoke != null ? cookingSmoke.ParticleSystem : null;

        public void Configure(
            BurgerAssemblyController targetController,
            IngredientType ingredientType,
            SimpleShapeGraphic targetGraphic,
            Text targetPhaseText,
            PattyGrillState existingState = null)
        {
            if (State != null)
            {
                State.PhaseChanged -= HandlePhaseChanged;
            }
            controller = targetController;
            GrillIngredientType = ingredientType;
            graphic = targetGraphic;
            phaseText = targetPhaseText;
            EnsurePattyCookingEffect();
            EnsureCookingSmoke();
            State = existingState ?? new PattyGrillState(ingredientType);
            State.PhaseChanged += HandlePhaseChanged;
            RefreshAppearance();
        }

        private void OnDestroy()
        {
            if (State != null)
            {
                State.PhaseChanged -= HandlePhaseChanged;
            }

            if (pattyCookingEffect != null)
            {
                GameObject effectObject = pattyCookingEffect.gameObject;
                pattyCookingEffect = null;
                effectObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(effectObject);
                }
                else
                {
                    DestroyImmediate(effectObject);
                }
            }
        }

        private void Update()
        {
            if (State == null)
            {
                return;
            }

            if (!isHeld)
            {
                State.Tick(Time.deltaTime);
            }

            RefreshAppearance();
            controller?.UpdateGrillItemStatus(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.HandleGrillItemTap(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ownsActiveDrag = controller != null && controller.TryBeginCookedGrillItemDrag(this, eventData.position);
            isHeld = ownsActiveDrag;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            controller.UpdatePointerDrag(eventData.position);
            controller.RequestGrillItemTransfer(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            ownsActiveDrag = false;
            controller.EndCookedGrillItemDrag(this, eventData.position);
            isHeld = false;
        }

        private void RefreshAppearance()
        {
            if (State == null || graphic == null)
            {
                return;
            }

            PattyGrillPhase phase = State.Phase;
            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            graphic.Shape = GrillIngredientType == IngredientType.Bacon
                ? SimpleShape.Rectangle
                : SimpleShape.Circle;
            graphic.SourceSprite = phase == PattyGrillPhase.Overcooked
                ? sprites.GetBurntGrillIngredient(GrillIngredientType)
                : phase == PattyGrillPhase.Flipping || phase == PattyGrillPhase.CookingSide2 || phase == PattyGrillPhase.Done
                    ? sprites.GetCookedGrillIngredient(GrillIngredientType)
                    : phase == PattyGrillPhase.RawDough
                        ? sprites.GetInitialGrillIngredient(GrillIngredientType)
                        : sprites.GetRawGrillIngredient(GrillIngredientType);
            graphic.rectTransform.sizeDelta = GetGrillSize(GrillIngredientType, phase);
            graphic.color = Color.white;

            if (phase == PattyGrillPhase.Flipping)
            {
                float t = Mathf.Clamp01(State.PhaseElapsed / CookingPrototypeRules.FlipAnimationSeconds);
                graphic.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f * t);
            }
            else
            {
                graphic.rectTransform.localEulerAngles = Vector3.zero;
            }

            RefreshPattyCookingEffect(sprites, phase);
            RefreshCookingSmoke(phase);

            if (phaseText != null)
            {
                phaseText.text = GetPhaseLabel(GrillIngredientType, phase);
            }
        }

        private void EnsurePattyCookingEffect()
        {
            if (GrillIngredientType != IngredientType.Patty || graphic == null || pattyCookingEffect != null)
            {
                return;
            }

            RectTransform parent = graphic.rectTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            pattyCookingEffect = BurgerUiFactory.CreateShape(
                graphic.gameObject.name + "CookingEffect",
                parent,
                SimpleShape.Circle,
                Color.white,
                graphic.rectTransform.anchoredPosition,
                graphic.rectTransform.sizeDelta,
                false,
                sprites.GetPattyCookingFrame(0f));
            pattyCookingEffect.rectTransform.SetSiblingIndex(graphic.rectTransform.GetSiblingIndex());
            pattyCookingEffect.gameObject.SetActive(false);
        }

        private void RefreshPattyCookingEffect(BurgerSpriteCatalog sprites, PattyGrillPhase phase)
        {
            if (GrillIngredientType != IngredientType.Patty)
            {
                return;
            }

            EnsurePattyCookingEffect();
            if (pattyCookingEffect == null)
            {
                return;
            }

            RectTransform sourceRect = graphic.rectTransform;
            RectTransform effectRect = pattyCookingEffect.rectTransform;
            effectRect.anchoredPosition = sourceRect.anchoredPosition;
            effectRect.sizeDelta = sourceRect.sizeDelta;
            effectRect.localScale = sourceRect.localScale;
            effectRect.localEulerAngles = Vector3.zero;

            bool showEffect = IsPattyCookingPhase(phase);
            if (showEffect)
            {
                pattyCookingEffect.SourceSprite = sprites.GetPattyCookingFrame(State.PhaseElapsed);
                pattyCookingEffect.color = Color.white;
            }
            pattyCookingEffect.gameObject.SetActive(showEffect);
        }

        private void EnsureCookingSmoke()
        {
            bool supportsSmoke = GrillIngredientType == IngredientType.Patty ||
                GrillIngredientType == IngredientType.Bacon;
            if (!supportsSmoke || graphic == null || cookingSmoke != null)
            {
                return;
            }

            cookingSmoke = graphic.gameObject.GetComponent<GrillCookingSmoke>();
            if (cookingSmoke == null)
            {
                cookingSmoke = graphic.gameObject.AddComponent<GrillCookingSmoke>();
            }
            cookingSmoke.Configure(graphic.rectTransform);
        }

        private void RefreshCookingSmoke(PattyGrillPhase phase)
        {
            if (GrillIngredientType != IngredientType.Patty && GrillIngredientType != IngredientType.Bacon)
            {
                return;
            }

            EnsureCookingSmoke();
            cookingSmoke?.SetState(phase, isHeld);
        }

        private static bool IsPattyCookingPhase(PattyGrillPhase phase)
        {
            return phase == PattyGrillPhase.CookingSide1 ||
                phase == PattyGrillPhase.ReadyToFlip ||
                phase == PattyGrillPhase.Flipping ||
                phase == PattyGrillPhase.CookingSide2;
        }

        public static string GetPhaseLabel(IngredientType type, PattyGrillPhase phase)
        {
            string itemName;
            switch (type)
            {
                case IngredientType.Patty:
                    itemName = "패티";
                    break;
                case IngredientType.Bacon:
                    itemName = "베이컨";
                    break;
                case IngredientType.Egg:
                    itemName = "계란";
                    break;
                default:
                    itemName = BurgerIngredientCatalog.GetDisplayName(type);
                    break;
            }

            switch (phase)
            {
                case PattyGrillPhase.RawDough:
                    return type == IngredientType.Patty
                        ? "패티볼\n탭해서 누르기"
                        : itemName + "\n탭해서 굽기";
                case PattyGrillPhase.Flattened:
                    return type == IngredientType.Patty ? "패티를 눌렀습니다" : "조리 시작";
                case PattyGrillPhase.CookingSide1:
                    return type == IngredientType.Egg ? "계란 조리 중" : "1면 조리 중";
                case PattyGrillPhase.ReadyToFlip: return "뒤집기 가능!\n5초 안에 탭하세요";
                case PattyGrillPhase.Flipping: return "뒤집는 중";
                case PattyGrillPhase.CookingSide2: return "2면 조리 중";
                case PattyGrillPhase.Done: return "완료!\n오른쪽 끝으로 드래그";
                case PattyGrillPhase.Overcooked: return "탔습니다\n이동 불가";
                default: return phase.ToString();
            }
        }

        public static Vector2 GetGrillSize(IngredientType type, PattyGrillPhase phase)
        {
            switch (type)
            {
                case IngredientType.Patty:
                    return phase == PattyGrillPhase.RawDough
                        ? new Vector2(150f, 150f)
                        : new Vector2(240f, 160f);
                case IngredientType.Bacon:
                    return new Vector2(330f, 180f);
                case IngredientType.Egg:
                    return new Vector2(270f, 170f);
                default:
                    return new Vector2(240f, 150f);
            }
        }

        private void HandlePhaseChanged(PattyGrillPhase phase)
        {
            RefreshAppearance();
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CookingCameraSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform targetContent;
        private float grillX;
        private float boardX;
        private float packagingX;
        private Vector2 dragStartScreen;
        private float dragStartContentX;
        private Coroutine tweenRoutine;

        public CookingCameraZone CurrentZone { get; private set; } = CookingCameraZone.Grill;

        public CookingCameraZone DestinationZone { get; private set; } = CookingCameraZone.Grill;

        public float GrillX => grillX;

        public float BoardX => boardX;

        public float PackagingX => packagingX;

        public float CurrentContentX =>
            targetContent != null ? targetContent.anchoredPosition.x : 0f;

        public Func<CookingCameraZone, bool> CanMoveToZone { get; set; }

        public event Action<CookingCameraZone> ZoneChanged;

        public void Configure(
            RectTransform content,
            float grillPageX,
            float boardPageX,
            float packagingPageX)
        {
            targetContent = content != null
                ? content
                : throw new ArgumentNullException(nameof(content));
            grillX = grillPageX;
            boardX = boardPageX;
            packagingX = packagingPageX;
            SetImmediate(CookingCameraZone.Grill);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetContent == null)
            {
                return;
            }

            CancelTween();
            dragStartScreen = eventData.position;
            dragStartContentX = targetContent.anchoredPosition.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetContent == null)
            {
                return;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenDelta = eventData.position.x - dragStartScreen.x;
            float contentDelta = (screenDelta / screenWidth) * Mathf.Abs(packagingX - grillX);
            float minimumContentX = CanAccess(CookingCameraZone.Packaging) ? -packagingX : -boardX;
            float maximumContentX = -grillX;
            SetContentX(Mathf.Clamp(
                dragStartContentX + contentDelta,
                minimumContentX,
                maximumContentX));
            UpdateZoneFromPosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (targetContent == null)
            {
                return;
            }

            // Keep the panorama exactly where the pointer released it. Logical
            // zones are still inferred for interaction routing, but no visual
            // page snapping occurs after a manual drag.
            UpdateZoneFromPosition();
        }

        public void MoveToBoard()
        {
            MoveTo(CookingCameraZone.Board);
        }

        public void MoveToGrill()
        {
            MoveTo(CookingCameraZone.Grill);
        }

        public void MoveToPackaging()
        {
            MoveTo(CookingCameraZone.Packaging);
        }

        public void MoveTo(CookingCameraZone zone)
        {
            if (targetContent == null)
            {
                return;
            }

            if (!CanAccess(zone))
            {
                zone = NearestAccessibleZone(-targetContent.anchoredPosition.x);
            }

            DestinationZone = zone;
            CancelTween();
            tweenRoutine = StartCoroutine(TweenCamera(zone));
        }

        public void SetImmediate(CookingCameraZone zone)
        {
            CancelTween();
            DestinationZone = zone;
            CurrentZone = zone;
            SetContentX(GetContentX(zone));
            ZoneChanged?.Invoke(zone);
        }

        public float GetZoneX(CookingCameraZone zone)
        {
            switch (zone)
            {
                case CookingCameraZone.Grill:
                    return grillX;
                case CookingCameraZone.Board:
                    return boardX;
                case CookingCameraZone.Packaging:
                    return packagingX;
                default:
                    throw new ArgumentOutOfRangeException(nameof(zone), zone, null);
            }
        }

        public float GetContentX(CookingCameraZone zone)
        {
            return -GetZoneX(zone);
        }

        private IEnumerator TweenCamera(CookingCameraZone zone)
        {
            float start = targetContent.anchoredPosition.x;
            float end = GetContentX(zone);
            float elapsed = 0f;
            while (elapsed < CookingPrototypeRules.CameraTweenSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / CookingPrototypeRules.CameraTweenSeconds);
                t = t * t * (3f - 2f * t);
                SetContentX(Mathf.Lerp(start, end, t));
                yield return null;
            }

            SetContentX(end);
            CurrentZone = zone;
            DestinationZone = zone;
            tweenRoutine = null;
            ZoneChanged?.Invoke(zone);
        }

        private void CancelTween()
        {
            if (tweenRoutine == null)
            {
                return;
            }

            StopCoroutine(tweenRoutine);
            tweenRoutine = null;
        }

        private void SetContentX(float x)
        {
            Vector2 position = targetContent.anchoredPosition;
            position.x = x;
            targetContent.anchoredPosition = position;
        }

        private void UpdateZoneFromPosition()
        {
            CookingCameraZone zone = NearestAccessibleZone(-targetContent.anchoredPosition.x);
            DestinationZone = zone;
            if (zone == CurrentZone)
            {
                return;
            }

            CurrentZone = zone;
            ZoneChanged?.Invoke(zone);
        }

        private CookingCameraZone NearestZone(float x)
        {
            CookingCameraZone nearest = CookingCameraZone.Grill;
            float nearestDistance = Mathf.Abs(x - grillX);
            float boardDistance = Mathf.Abs(x - boardX);
            if (boardDistance < nearestDistance)
            {
                nearest = CookingCameraZone.Board;
                nearestDistance = boardDistance;
            }

            if (Mathf.Abs(x - packagingX) < nearestDistance)
            {
                nearest = CookingCameraZone.Packaging;
            }

            return nearest;
        }

        private CookingCameraZone NearestAccessibleZone(float x)
        {
            CookingCameraZone nearest = NearestZone(x);
            return CanAccess(nearest) ? nearest : CookingCameraZone.Board;
        }

        private bool CanAccess(CookingCameraZone zone)
        {
            return CanMoveToZone == null || CanMoveToZone(zone);
        }
    }
}
