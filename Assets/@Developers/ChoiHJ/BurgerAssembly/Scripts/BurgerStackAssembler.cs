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
        private float currentStackY;
        private float stackMinY;
        private float stackMaxY;
        private float stackHalfWidth;

        public BurgerStackAssembler(int maximumToppings = CookingPrototypeRules.MaximumToppings)
        {
            state = new BurgerAssemblyState(maximumToppings);
        }

        public RectTransform BurgerStackRoot { get; private set; }

        public bool IsCompleted => state.IsCompleted;

        public bool HasBottomBun => state.HasBottomBun;

        public int ToppingCount => state.ToppingCount;

        public int MaximumToppings => state.MaximumToppings;

        public int PlacedIngredientCount => placedIngredients.Count(view => view != null);

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

        public bool TryPlace(IngredientType type, Vector2 localPosition, out BurgerData completedBurger)
        {
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
                float layerRise = Mathf.Min(
                    CookingPrototypeRules.BurgerStackLayerSpacing,
                    Mathf.Max(18f, visual.Size.y * 0.45f));
                currentStackY += layerRise;
                stackedPosition = new Vector2(0f, currentStackY);
                ExpandStackBounds(stackedPosition, visual.Size);
            }

            KeepBurgerStackInsideBoard();
            SimpleShapeGraphic graphic = BurgerUiFactory.CreateShape(
                type + "_" + layerOrder,
                BurgerStackRoot,
                visual.Shape,
                visual.Color,
                stackedPosition,
                visual.Size,
                false,
                visual.SourceSprite);
            PlacedIngredientView view = graphic.gameObject.AddComponent<PlacedIngredientView>();
            view.Configure(type, layerOrder);
            placedIngredients.Add(view);

            if (type == IngredientType.BunTop)
            {
                state.TryComplete(CaptureCurrentBurger(), out completedBurger);
            }

            return true;
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
            currentStackY = 0f;
            stackMinY = 0f;
            stackMaxY = 0f;
            stackHalfWidth = 0f;
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

            currentStackY = 0f;
            stackMinY = -bottomBunSize.y * 0.5f;
            stackMaxY = bottomBunSize.y * 0.5f;
            stackHalfWidth = bottomBunSize.x * 0.5f;
        }

        private void ExpandStackBounds(Vector2 localPosition, Vector2 size)
        {
            stackHalfWidth = Mathf.Max(stackHalfWidth, Mathf.Abs(localPosition.x) + size.x * 0.5f);
            stackMinY = Mathf.Min(stackMinY, localPosition.y - size.y * 0.5f);
            stackMaxY = Mathf.Max(stackMaxY, localPosition.y + size.y * 0.5f);
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
