using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    public enum BurgerDragItemKind
    {
        BottomBun,
        TopBun,
        Ingredient,
        RawPatty,
        CookedPatty
    }

    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class DraggableBurgerItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Canvas canvas;
        private Func<bool> canDrag;
        private Action onBlocked;
        private SimpleShape shape;
        private Color shapeColor;
        private Vector2 ghostSize;
        private RectTransform dragGhost;

        public BurgerDragItemKind Kind { get; private set; }

        public BurgerIngredientId Ingredient { get; private set; }

        public bool IsDragging { get; private set; }

        public bool CanDragNow => canDrag == null || canDrag();

        public void Configure(
            BurgerDragItemKind kind,
            BurgerIngredientId ingredient,
            Canvas targetCanvas,
            SimpleShape ghostShape,
            Color ghostColor,
            Vector2 dragGhostSize,
            Func<bool> dragCondition,
            Action blockedAction)
        {
            Kind = kind;
            Ingredient = ingredient;
            canvas = targetCanvas;
            shape = ghostShape;
            shapeColor = ghostColor;
            ghostSize = dragGhostSize;
            canDrag = dragCondition;
            onBlocked = blockedAction;
            RefreshAppearance();
        }

        public void RefreshAppearance()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            group.alpha = CanDragNow ? 1f : 0.48f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDragNow)
            {
                onBlocked?.Invoke();
                IsDragging = false;
                return;
            }

            IsDragging = true;
            CreateGhost(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsDragging && dragGhost != null)
            {
                dragGhost.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CompleteDrop();
        }

        public void CompleteDrop()
        {
            IsDragging = false;
            if (dragGhost != null)
            {
                Destroy(dragGhost.gameObject);
                dragGhost = null;
            }
        }

        private void CreateGhost(Vector2 screenPosition)
        {
            GameObject ghostObject = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(SimpleShapeGraphic));
            dragGhost = ghostObject.GetComponent<RectTransform>();
            dragGhost.SetParent(canvas.transform, false);
            dragGhost.SetAsLastSibling();
            dragGhost.sizeDelta = ghostSize;
            dragGhost.position = screenPosition;

            CanvasGroup group = ghostObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0.82f;

            SimpleShapeGraphic graphic = ghostObject.GetComponent<SimpleShapeGraphic>();
            graphic.Shape = shape;
            graphic.color = shapeColor;
            graphic.raycastTarget = false;
        }
    }

    public sealed class BurgerBoardDropZone : MonoBehaviour, IDropHandler
    {
        private BurgerAssemblyController controller;

        public void Configure(BurgerAssemblyController targetController)
        {
            controller = targetController;
        }

        public void OnDrop(PointerEventData eventData)
        {
            DraggableBurgerItem item = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<DraggableBurgerItem>();
            if (item != null && item.IsDragging)
            {
                controller.TryDropOnBoard(item);
                item.CompleteDrop();
            }
        }
    }

    public sealed class GrillDropZone : MonoBehaviour, IDropHandler
    {
        private BurgerAssemblyController controller;

        public void Configure(BurgerAssemblyController targetController)
        {
            controller = targetController;
        }

        public void OnDrop(PointerEventData eventData)
        {
            DraggableBurgerItem item = eventData.pointerDrag == null
                ? null
                : eventData.pointerDrag.GetComponent<DraggableBurgerItem>();
            if (item != null && item.IsDragging)
            {
                controller.TryDropOnGrill(item);
                item.CompleteDrop();
            }
        }
    }
}
