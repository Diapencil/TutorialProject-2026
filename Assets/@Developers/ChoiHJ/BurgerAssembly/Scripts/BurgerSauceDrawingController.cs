using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerSauceDrawingController :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        private readonly List<SauceStrokeGraphic> boardStrokes = new List<SauceStrokeGraphic>();

        private RectTransform boardLayerRoot;
        private BurgerStackAssembler stackAssembler;
        private CookingCameraSlider pageSlider;
        private Action drawingChanged;
        private Action<string, bool> setBoardStatus;
        private SauceStrokeGraphic activeStroke;
        private IngredientType selectedSauce;
        private Vector2 lastPoint;
        private bool hasSelectedSauce;
        private bool drawingGesture;
        private bool forwardedPageGesture;
        private int drawnStrokeCount;

        public bool HasSelectedSauce => hasSelectedSauce;

        public int StrokeCount => drawnStrokeCount;

        public int PointCount => boardStrokes
            .Where(stroke => stroke != null)
            .Sum(stroke => stroke.PointCount);

        internal void Configure(
            RectTransform targetBoardLayerRoot,
            BurgerStackAssembler targetStackAssembler,
            CookingCameraSlider targetPageSlider,
            Action onDrawingChanged,
            Action<string, bool> boardStatusSetter)
        {
            boardLayerRoot = targetBoardLayerRoot != null
                ? targetBoardLayerRoot
                : throw new ArgumentNullException(nameof(targetBoardLayerRoot));
            stackAssembler = targetStackAssembler ??
                throw new ArgumentNullException(nameof(targetStackAssembler));
            pageSlider = targetPageSlider ??
                throw new ArgumentNullException(nameof(targetPageSlider));
            drawingChanged = onDrawingChanged;
            setBoardStatus = boardStatusSetter;
        }

        internal bool CanSelect(IngredientType type)
        {
            return BurgerIngredientCatalog.IsSauce(type) &&
                stackAssembler != null &&
                !stackAssembler.IsCompleted;
        }

        internal bool IsSelected(IngredientType type)
        {
            return hasSelectedSauce && selectedSauce == type;
        }

        internal void Toggle(IngredientType type)
        {
            if (!CanSelect(type))
            {
                return;
            }

            EndStroke();
            if (hasSelectedSauce && selectedSauce == type)
            {
                hasSelectedSauce = false;
                setBoardStatus?.Invoke(
                    "기본 마우스로 돌아왔습니다. 재료를 드래그하거나 화면을 스와이프할 수 있습니다.",
                    false);
            }
            else
            {
                selectedSauce = type;
                hasSelectedSauce = true;
                setBoardStatus?.Invoke(
                    BurgerIngredientCatalog.GetDisplayName(type) +
                    " 소스 모드: 도마를 누른 채 움직여 원하는 곳에 그리세요. 소스 통을 다시 누르면 종료됩니다.",
                    false);
            }

            drawingChanged?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            forwardedPageGesture = !hasSelectedSauce;
            drawingGesture = hasSelectedSauce;
            if (forwardedPageGesture)
            {
                pageSlider?.OnBeginDrag(eventData);
                return;
            }

            if (TryGetBoardPoint(eventData, out Vector2 local))
            {
                BeginStroke(local);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (forwardedPageGesture)
            {
                pageSlider?.OnDrag(eventData);
                return;
            }

            if (!drawingGesture || !hasSelectedSauce)
            {
                return;
            }

            if (!TryGetBoardPoint(eventData, out Vector2 local))
            {
                EndStroke();
                return;
            }

            if (activeStroke == null)
            {
                BeginStroke(local);
            }
            else
            {
                ContinueStroke(local);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (forwardedPageGesture)
            {
                pageSlider?.OnEndDrag(eventData);
            }
            else
            {
                EndStroke();
            }

            forwardedPageGesture = false;
            drawingGesture = false;
        }

        internal List<SauceStrokeData> AttachSauceNearBurger(
            RectTransform burgerRoot,
            float stackHalfWidth,
            float stackMinY,
            float stackMaxY)
        {
            var attachedData = new List<SauceStrokeData>();
            if (burgerRoot == null || boardLayerRoot == null || stackAssembler == null)
            {
                return attachedData;
            }

            EndStroke();
            hasSelectedSauce = false;
            Vector2 burgerBoardPosition = burgerRoot.anchoredPosition;
            float padding = CookingPrototypeRules.SauceBurgerAttachPadding;

            foreach (SauceStrokeGraphic boardStroke in boardStrokes.ToArray())
            {
                if (boardStroke == null)
                {
                    boardStrokes.Remove(boardStroke);
                    continue;
                }

                List<Vector2> nearbyBoardPoints = boardStroke.ExtractPoints(point =>
                {
                    Vector2 relative = point - burgerBoardPosition;
                    return Mathf.Abs(relative.x) <= stackHalfWidth + padding &&
                        relative.y >= stackMinY - padding &&
                        relative.y <= stackMaxY + padding;
                });
                if (nearbyBoardPoints.Count == 0)
                {
                    continue;
                }

                List<Vector2> rootLocalPoints = nearbyBoardPoints
                    .Select(point => point - burgerBoardPosition)
                    .ToList();
                SauceStrokeGraphic attachedStroke = CreateStroke(
                    burgerRoot,
                    boardStroke.SauceType,
                    boardStroke.LayerOrder,
                    rootLocalPoints);
                stackAssembler.InsertDecorationAtLayer(attachedStroke.rectTransform, boardStroke.LayerOrder);
                stackAssembler.IncludeDecorationBounds(rootLocalPoints, attachedStroke.Diameter);
                attachedData.Add(new SauceStrokeData(
                    boardStroke.SauceType,
                    nearbyBoardPoints,
                    boardStroke.LayerOrder));

                if (boardStroke.PointCount == 0)
                {
                    boardStrokes.Remove(boardStroke);
                    DestroyObject(boardStroke.gameObject);
                }
            }

            drawingChanged?.Invoke();
            return attachedData;
        }

        internal void ResetDrawing()
        {
            EndStroke();
            hasSelectedSauce = false;
            drawingGesture = false;
            forwardedPageGesture = false;
            foreach (SauceStrokeGraphic stroke in boardStrokes)
            {
                if (stroke != null)
                {
                    DestroyObject(stroke.gameObject);
                }
            }
            boardStrokes.Clear();
            drawnStrokeCount = 0;
            drawingChanged?.Invoke();
        }

        private void BeginStroke(Vector2 local)
        {
            if (!hasSelectedSauce || stackAssembler == null)
            {
                return;
            }

            int layerOrder = stackAssembler.ReserveLayerOrder();
            if (layerOrder < 0)
            {
                return;
            }

            activeStroke = CreateStroke(
                boardLayerRoot,
                selectedSauce,
                layerOrder,
                new[] { local });
            activeStroke.rectTransform.SetAsLastSibling();
            boardStrokes.Add(activeStroke);
            drawnStrokeCount++;
            lastPoint = local;
            drawingChanged?.Invoke();
        }

        private void ContinueStroke(Vector2 local)
        {
            if (activeStroke == null)
            {
                return;
            }

            Vector2 delta = local - lastPoint;
            float distance = delta.magnitude;
            float spacing = CookingPrototypeRules.SauceStampSpacingPixels;
            if (distance < spacing)
            {
                return;
            }

            Vector2 direction = delta / distance;
            while (distance >= spacing)
            {
                if (activeStroke.PointCount >= CookingPrototypeRules.MaximumSaucePointsPerStroke)
                {
                    EndStroke();
                    BeginStroke(lastPoint);
                    if (activeStroke == null)
                    {
                        return;
                    }
                }

                lastPoint += direction * spacing;
                activeStroke.AddPoint(lastPoint);
                distance = Vector2.Distance(local, lastPoint);
            }
            drawingChanged?.Invoke();
        }

        private void EndStroke()
        {
            activeStroke = null;
        }

        private SauceStrokeGraphic CreateStroke(
            RectTransform parent,
            IngredientType type,
            int layerOrder,
            IEnumerable<Vector2> points)
        {
            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetVisual(type);
            var strokeObject = new GameObject(
                "SauceStroke_" + type + "_" + layerOrder,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(SauceStrokeGraphic));
            RectTransform rect = strokeObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            BurgerUiFactory.SetRect(rect, Vector2.zero, boardLayerRoot.rect.size);

            SauceStrokeGraphic graphic = strokeObject.GetComponent<SauceStrokeGraphic>();
            graphic.Configure(
                type,
                BurgerIngredientCatalog.GetSauceBrushSprite(),
                visual.Color,
                visual.Size.x,
                layerOrder);
            graphic.SetPoints(points);
            return graphic;
        }

        private bool TryGetBoardPoint(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            return boardLayerRoot != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    boardLayerRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out local) &&
                boardLayerRoot.rect.Contains(local);
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
