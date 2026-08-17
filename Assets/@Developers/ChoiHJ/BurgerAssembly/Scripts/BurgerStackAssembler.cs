using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SheepSheepBurger.BurgerAssembly
{
    internal sealed class BurgerStackAssembler
    {
        private readonly BurgerAssemblyState state;
        private readonly List<PlacedIngredientView> placedIngredients = new List<PlacedIngredientView>();

        private RectTransform boardLayerRoot;
        private float stackMinY;
        private float stackMaxY;
        private float stackHalfWidth;
        private float stackAnchorRadius;
        private int nextLooseLayerOrder;

        public BurgerStackAssembler(int maximumToppings = CookingPrototypeRules.MaximumToppings)
        {
            state = new BurgerAssemblyState(maximumToppings);
        }

        public RectTransform BurgerStackRoot { get; private set; }

        public bool IsCompleted => state.IsCompleted;

        public bool HasBottomBun => state.HasBottomBun;

        public bool HasTopBun => state.HasTopBun;

        public int ToppingCount => state.ToppingCount;

        public int MaximumToppings => state.MaximumToppings;

        public int PlacedIngredientCount => placedIngredients.Count(view => view != null);

        public int StackLayerCount => placedIngredients
            .Where(view => view != null)
            .Select(view => view.LayerOrder)
            .Distinct()
            .Count();

        public float StackMinY => stackMinY;

        public float StackMaxY => stackMaxY;

        public float StackHalfWidth => stackHalfWidth;

        public void Configure(RectTransform targetBoardLayerRoot)
        {
            boardLayerRoot = targetBoardLayerRoot != null
                ? targetBoardLayerRoot
                : throw new ArgumentNullException(nameof(targetBoardLayerRoot));
        }

        public bool CanPlace(IngredientType type)
        {
            return !BurgerIngredientCatalog.IsSauce(type) && state.CanPlace(type);
        }

        public int ReserveLayerOrder()
        {
            return state.BringToFront();
        }

        public int ReserveLooseLayerOrder()
        {
            return nextLooseLayerOrder++;
        }

        public bool TryPlace(
            IngredientType type,
            Vector2 localPosition,
            BurgerAssemblyController controller,
            PattyGrillState cookingState,
            out PlacedIngredientView placedView,
            out BurgerData completedBurger)
        {
            placedView = null;
            completedBurger = null;
            if (boardLayerRoot == null)
            {
                throw new InvalidOperationException("BurgerStackAssembler must be configured before placement.");
            }

            if (BurgerIngredientCatalog.IsSauce(type))
            {
                return false;
            }

            if (type != IngredientType.BunBottom && BurgerStackRoot == null)
            {
                return false;
            }

            if (!state.TryRegisterPlacement(type, out int layerOrder))
            {
                return false;
            }

            BurgerIngredientVisual visual = BurgerIngredientCatalog.GetVisual(type);
            Vector2 stackedPosition;

            if (type == IngredientType.BunBottom)
            {
                InitializeBurgerStack(localPosition, visual.Size);
                stackedPosition = Vector2.zero;
            }
            else
            {
                stackedPosition = CorrectStackLocalPosition(localPosition);
                ExpandStackBounds(stackedPosition, visual.Size);
            }

            KeepBurgerStackInsideBoard();
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(
                type + "_Layer" + layerOrder + "_Item" + placedIngredients.Count,
                BurgerStackRoot,
                visual.Shape,
                visual.Color,
                stackedPosition,
                visual.Size,
                true,
                GetPlacementSprite(type, cookingState, visual.SourceSprite));
            graphic.gameObject.AddComponent<CanvasGroup>();
            placedView = graphic.gameObject.AddComponent<PlacedIngredientView>();
            placedView.Configure(controller, type, layerOrder, true, cookingState);
            placedIngredients.Add(placedView);

            if (type == IngredientType.BunTop)
            {
                state.TryComplete(CaptureCurrentBurger(), out completedBurger);
            }

            return true;
        }

        public bool IsNearStack(Vector2 boardLocalPosition)
        {
            if (BurgerStackRoot == null)
            {
                return false;
            }

            Vector2 relative = boardLocalPosition - BurgerStackRoot.anchoredPosition;
            float padding = CookingPrototypeRules.BurgerStackSnapPadding;
            return relative.magnitude <= stackAnchorRadius + padding;
        }

        public void MoveStack(Vector2 boardLocalPosition)
        {
            if (BurgerStackRoot != null)
            {
                BurgerStackRoot.anchoredPosition = ClampBurgerStackPosition(boardLocalPosition);
            }
        }

        public bool MoveIngredient(PlacedIngredientView view, Vector2 boardLocalPosition)
        {
            if (view == null || view.IngredientType == IngredientType.BunBottom ||
                !placedIngredients.Contains(view) || !IsNearStack(boardLocalPosition))
            {
                return false;
            }

            view.RectTransform.anchoredPosition = CorrectStackLocalPosition(boardLocalPosition);
            RebuildStackBounds();
            return true;
        }

        public bool RemoveIngredient(PlacedIngredientView view)
        {
            if (view == null || view.IngredientType == IngredientType.BunBottom ||
                !placedIngredients.Contains(view) || !state.TryUnregisterPlacement(view.IngredientType))
            {
                return false;
            }

            placedIngredients.Remove(view);
            DestroyObject(view.gameObject);
            RebuildStackLayout();
            return true;
        }

        public bool Contains(PlacedIngredientView view)
        {
            return view != null && placedIngredients.Contains(view);
        }

        public void InsertDecorationAtLayer(RectTransform decoration, int layerOrder)
        {
            if (decoration == null || BurgerStackRoot == null)
            {
                return;
            }

            int targetIndex = 0;
            for (int index = 0; index < BurgerStackRoot.childCount; index++)
            {
                Transform child = BurgerStackRoot.GetChild(index);
                PlacedIngredientView ingredient = child.GetComponent<PlacedIngredientView>();
                SauceStrokeGraphic sauce = child.GetComponent<SauceStrokeGraphic>();
                int childOrder = ingredient != null
                    ? ingredient.LayerOrder
                    : sauce != null
                        ? sauce.LayerOrder
                        : int.MaxValue;
                if (childOrder <= layerOrder)
                {
                    targetIndex = index + 1;
                }
            }
            decoration.SetSiblingIndex(targetIndex);
        }

        public void IncludeDecorationBounds(IEnumerable<Vector2> localPoints, float diameter)
        {
            if (localPoints == null)
            {
                throw new ArgumentNullException(nameof(localPoints));
            }

            Vector2 pointSize = Vector2.one * Mathf.Max(0f, diameter);
            foreach (Vector2 point in localPoints)
            {
                ExpandStackBounds(point, pointSize);
            }
        }

        public void Reset()
        {
            if (BurgerStackRoot != null)
            {
                DestroyObject(BurgerStackRoot.gameObject);
                BurgerStackRoot = null;
            }
            else
            {
                foreach (PlacedIngredientView view in placedIngredients)
                {
                    if (view != null)
                    {
                        DestroyObject(view.gameObject);
                    }
                }
            }

            placedIngredients.Clear();
            state.Reset();
            stackMinY = 0f;
            stackMaxY = 0f;
            stackHalfWidth = 0f;
            stackAnchorRadius = 0f;
            nextLooseLayerOrder = 0;
        }

        private void InitializeBurgerStack(Vector2 boardLocalPosition, Vector2 bottomBunSize)
        {
            var stackObject = new GameObject("BurgerStackRoot", typeof(RectTransform));
            BurgerStackRoot = stackObject.GetComponent<RectTransform>();
            BurgerStackRoot.SetParent(boardLayerRoot, false);
            BurgerUiFactory.SetRect(
                BurgerStackRoot,
                BurgerUiFactory.ClampInside(boardLayerRoot.rect, boardLocalPosition, bottomBunSize),
                Vector2.zero);

            stackMinY = -bottomBunSize.y * 0.5f;
            stackMaxY = bottomBunSize.y * 0.5f;
            stackHalfWidth = bottomBunSize.x * 0.5f;
            stackAnchorRadius = Mathf.Min(bottomBunSize.x, bottomBunSize.y) * 0.5f;
        }

        private void ExpandStackBounds(Vector2 localPosition, Vector2 size)
        {
            stackHalfWidth = Mathf.Max(stackHalfWidth, Mathf.Abs(localPosition.x) + size.x * 0.5f);
            stackMinY = Mathf.Min(stackMinY, localPosition.y - size.y * 0.5f);
            stackMaxY = Mathf.Max(stackMaxY, localPosition.y + size.y * 0.5f);
        }

        private void RebuildStackLayout()
        {
            placedIngredients.RemoveAll(view => view == null);
            RebuildStackBounds();
        }

        private void RebuildStackBounds()
        {
            placedIngredients.RemoveAll(view => view == null);
            PlacedIngredientView bottomBun = placedIngredients
                .FirstOrDefault(view => view.IngredientType == IngredientType.BunBottom);
            if (bottomBun == null)
            {
                return;
            }

            Vector2 bottomSize = bottomBun.RectTransform.sizeDelta;
            stackMinY = -bottomSize.y * 0.5f;
            stackMaxY = bottomSize.y * 0.5f;
            stackHalfWidth = bottomSize.x * 0.5f;
            stackAnchorRadius = Mathf.Min(bottomSize.x, bottomSize.y) * 0.5f;
            bottomBun.RectTransform.anchoredPosition = Vector2.zero;

            foreach (PlacedIngredientView view in placedIngredients
                         .Where(item => item != bottomBun)
                         .OrderBy(item => item.LayerOrder))
            {
                Vector2 size = view.RectTransform.sizeDelta;
                ExpandStackBounds(view.RectTransform.anchoredPosition, size);
            }

            KeepBurgerStackInsideBoard();
        }

        private void KeepBurgerStackInsideBoard()
        {
            if (BurgerStackRoot != null)
            {
                BurgerStackRoot.anchoredPosition = ClampBurgerStackPosition(BurgerStackRoot.anchoredPosition);
            }
        }

        private Vector2 ClampBurgerStackPosition(Vector2 desired)
        {
            Rect bounds = boardLayerRoot.rect;
            float minimumX = bounds.xMin + stackHalfWidth;
            float maximumX = bounds.xMax - stackHalfWidth;
            float minimumY = bounds.yMin - stackMinY;
            float maximumY = bounds.yMax - stackMaxY;

            float x = minimumX <= maximumX ? Mathf.Clamp(desired.x, minimumX, maximumX) : bounds.center.x;
            float y = minimumY <= maximumY
                ? Mathf.Clamp(desired.y, minimumY, maximumY)
                : bounds.center.y - (stackMinY + stackMaxY) * 0.5f;
            return new Vector2(x, y);
        }

        private List<IngredientPlacement> CaptureCurrentBurger()
        {
            return placedIngredients
                .Where(view => view != null)
                .Select(view => view.Capture(boardLayerRoot))
                .OrderBy(placement => placement.layerOrder)
                .ToList();
        }

        private Vector2 CorrectStackLocalPosition(Vector2 boardLocalPosition)
        {
            Vector2 relative = boardLocalPosition - BurgerStackRoot.anchoredPosition;
            float distance = relative.magnitude;
            float unadjustedRadius = stackAnchorRadius *
                CookingPrototypeRules.BurgerStackUnadjustedRadiusRatio;
            if (distance <= unadjustedRadius || distance <= Mathf.Epsilon)
            {
                return relative;
            }

            float correctedDistance = Mathf.Max(
                unadjustedRadius,
                distance - CookingPrototypeRules.BurgerStackEdgeCorrectionDistance);
            return relative * (correctedDistance / distance);
        }

        private static Sprite GetPlacementSprite(
            IngredientType type,
            PattyGrillState cookingState,
            Sprite fallback)
        {
            if (cookingState == null || !BurgerIngredientCatalog.IsGrillIngredient(type))
            {
                return fallback;
            }

            BurgerSpriteCatalog sprites = BurgerSpriteCatalog.RequireActive();
            switch (cookingState.Phase)
            {
                case PattyGrillPhase.Overcooked:
                    return sprites.GetBurntGrillIngredient(type);
                case PattyGrillPhase.Flipping:
                case PattyGrillPhase.CookingSide2:
                case PattyGrillPhase.Done:
                    return sprites.GetCookedGrillIngredient(type);
                case PattyGrillPhase.RawDough:
                    return sprites.GetInitialGrillIngredient(type);
                default:
                    return sprites.GetRawGrillIngredient(type);
            }
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
