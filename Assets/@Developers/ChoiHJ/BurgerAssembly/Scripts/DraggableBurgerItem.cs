using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CookingTrayDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BurgerAssemblyController controller;
        private CanvasGroup canvasGroup;
        private SimpleShape ghostShape;
        private Color ghostColor;
        private Vector2 ghostSize;
        private bool ownsActiveDrag;

        public CookingDragKind Kind { get; private set; }

        public IngredientType IngredientType { get; private set; }

        public void Configure(
            BurgerAssemblyController targetController,
            CookingDragKind kind,
            IngredientType ingredientType,
            SimpleShape shape,
            Color color,
            Vector2 size)
        {
            controller = targetController;
            Kind = kind;
            IngredientType = ingredientType;
            ghostShape = shape;
            ghostColor = color;
            ghostSize = size;
            canvasGroup = GetComponent<CanvasGroup>();
            RefreshAppearance();
        }

        public void RefreshAppearance()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            bool available = controller != null && controller.CanBeginTrayDrag(Kind, IngredientType);
            canvasGroup.alpha = available ? 1f : 0.48f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ownsActiveDrag = controller != null && controller.TryBeginTrayDrag(
                Kind,
                IngredientType,
                ghostShape,
                ghostColor,
                ghostSize,
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
    }

    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PlacedIngredientView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BurgerAssemblyController controller;
        private RectTransform rectTransform;
        private bool isMoving;

        public IngredientType IngredientType { get; private set; }

        public int LayerOrder { get; private set; }

        public void Configure(BurgerAssemblyController targetController, IngredientType type, int layerOrder)
        {
            controller = targetController;
            IngredientType = type;
            LayerOrder = layerOrder;
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
            transform.SetAsLastSibling();
        }

        public IngredientPlacement Capture()
        {
            return new IngredientPlacement(IngredientType, rectTransform.anchoredPosition, LayerOrder);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isMoving = controller != null && controller.TryBeginPlacedIngredientMove(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isMoving)
            {
                controller.MovePlacedIngredient(this, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isMoving)
            {
                return;
            }

            isMoving = false;
            controller.EndPlacedIngredientMove(this, eventData.position);
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CookablePattyView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BurgerAssemblyController controller;
        private SimpleShapeGraphic graphic;
        private Text phaseText;
        private Color rawColor;
        private Color cookedColor;
        private Color burntColor;
        private bool isHeld;
        private bool ownsActiveDrag;

        public PattyGrillState State { get; private set; }

        public bool IsHeld => isHeld;

        public void Configure(
            BurgerAssemblyController targetController,
            SimpleShapeGraphic targetGraphic,
            Text targetPhaseText,
            Color raw,
            Color cooked,
            Color burnt)
        {
            controller = targetController;
            graphic = targetGraphic;
            phaseText = targetPhaseText;
            rawColor = raw;
            cookedColor = cooked;
            burntColor = burnt;
            State = new PattyGrillState();
            State.PhaseChanged += _ => RefreshAppearance();
            RefreshAppearance();
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
            controller?.UpdatePattyStatus(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.HandlePattyTap(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ownsActiveDrag = controller != null && controller.TryBeginCookedPattyDrag(this, eventData.position);
            isHeld = ownsActiveDrag;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            controller.UpdatePointerDrag(eventData.position);
            controller.RequestPattyTransfer(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ownsActiveDrag)
            {
                return;
            }

            ownsActiveDrag = false;
            controller.EndCookedPattyDrag(this, eventData.position);
            isHeld = false;
        }

        private void RefreshAppearance()
        {
            if (State == null || graphic == null)
            {
                return;
            }

            PattyGrillPhase phase = State.Phase;
            graphic.Shape = phase == PattyGrillPhase.RawDough ? SimpleShape.Circle : SimpleShape.Rectangle;
            graphic.rectTransform.sizeDelta = phase == PattyGrillPhase.RawDough
                ? new Vector2(150f, 150f)
                : new Vector2(260f, 105f);

            if (phase == PattyGrillPhase.Overcooked)
            {
                graphic.color = burntColor;
            }
            else
            {
                graphic.color = Color.Lerp(rawColor, cookedColor, State.GetNormalizedProgress());
            }

            if (phase == PattyGrillPhase.Flipping)
            {
                float t = Mathf.Clamp01(State.PhaseElapsed / CookingPrototypeRules.FlipAnimationSeconds);
                graphic.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f * t);
            }
            else
            {
                graphic.rectTransform.localEulerAngles = Vector3.zero;
            }

            if (phaseText != null)
            {
                phaseText.text = GetPhaseLabel(phase);
            }
        }

        public static string GetPhaseLabel(PattyGrillPhase phase)
        {
            switch (phase)
            {
                case PattyGrillPhase.RawDough: return "고기반죽\n탭해서 누르기";
                case PattyGrillPhase.Flattened: return "패티를 눌렀습니다";
                case PattyGrillPhase.CookingSide1: return "1면 조리 중";
                case PattyGrillPhase.ReadyToFlip: return "뒤집기 가능!\n패티를 탭하세요";
                case PattyGrillPhase.Flipping: return "뒤집는 중";
                case PattyGrillPhase.CookingSide2: return "2면 조리 중";
                case PattyGrillPhase.Done: return "완료!\n오른쪽 끝으로 드래그";
                case PattyGrillPhase.Overcooked: return "탔습니다\n이동 불가";
                default: return phase.ToString();
            }
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CookingCameraSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Camera targetCamera;
        private float grillX;
        private float boardX;
        private Vector2 dragStartScreen;
        private float dragStartCameraX;
        private CookingCameraZone dragStartZone;
        private Coroutine tweenRoutine;

        public CookingCameraZone CurrentZone { get; private set; } = CookingCameraZone.Grill;

        public CookingCameraZone DestinationZone { get; private set; } = CookingCameraZone.Grill;

        public float GrillX => grillX;

        public float BoardX => boardX;

        public event Action<CookingCameraZone> ZoneChanged;

        public void Configure(Camera camera, float grillCameraX, float boardCameraX)
        {
            targetCamera = camera;
            grillX = grillCameraX;
            boardX = boardCameraX;
            SetImmediate(CookingCameraZone.Grill);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (targetCamera == null)
            {
                return;
            }

            CancelTween();
            dragStartScreen = eventData.position;
            dragStartCameraX = targetCamera.transform.position.x;
            dragStartZone = NearestZone(dragStartCameraX);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetCamera == null)
            {
                return;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenDelta = eventData.position.x - dragStartScreen.x;
            float worldDelta = -(screenDelta / screenWidth) * Mathf.Abs(boardX - grillX);
            SetCameraX(Mathf.Clamp(dragStartCameraX + worldDelta, Mathf.Min(grillX, boardX), Mathf.Max(grillX, boardX)));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (targetCamera == null)
            {
                return;
            }

            float screenDelta = eventData.position.x - dragStartScreen.x;
            float threshold = Mathf.Max(1f, Screen.width) * CookingPrototypeRules.SwipeThresholdScreenRatio;
            CookingCameraZone target = dragStartZone;
            if (Mathf.Abs(screenDelta) >= threshold)
            {
                if (dragStartZone == CookingCameraZone.Grill && screenDelta < 0f)
                {
                    target = CookingCameraZone.Board;
                }
                else if (dragStartZone == CookingCameraZone.Board && screenDelta > 0f)
                {
                    target = CookingCameraZone.Grill;
                }
            }

            MoveTo(target);
        }

        public void MoveToBoard()
        {
            MoveTo(CookingCameraZone.Board);
        }

        public void MoveToGrill()
        {
            MoveTo(CookingCameraZone.Grill);
        }

        public void MoveTo(CookingCameraZone zone)
        {
            if (targetCamera == null)
            {
                return;
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
            SetCameraX(zone == CookingCameraZone.Grill ? grillX : boardX);
            ZoneChanged?.Invoke(zone);
        }

        private IEnumerator TweenCamera(CookingCameraZone zone)
        {
            float start = targetCamera.transform.position.x;
            float end = zone == CookingCameraZone.Grill ? grillX : boardX;
            float elapsed = 0f;
            while (elapsed < CookingPrototypeRules.CameraTweenSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / CookingPrototypeRules.CameraTweenSeconds);
                t = t * t * (3f - 2f * t);
                SetCameraX(Mathf.Lerp(start, end, t));
                yield return null;
            }

            SetCameraX(end);
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

        private void SetCameraX(float x)
        {
            Vector3 position = targetCamera.transform.position;
            position.x = x;
            targetCamera.transform.position = position;
        }

        private CookingCameraZone NearestZone(float x)
        {
            return Mathf.Abs(x - grillX) <= Mathf.Abs(x - boardX)
                ? CookingCameraZone.Grill
                : CookingCameraZone.Board;
        }
    }
}
