using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 재료 트레이 아이콘의 드래그 입력과 손맛 피드백을 담당합니다.
// 붙이는 오브젝트: CookingScene에서 생성되는 각 재료 트레이 아이콘.
[RequireComponent(typeof(RectTransform))]
public class IngredientDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public IngredientType ingredientType;
    public BuildZone buildZone;
    public Canvas canvas;

    private RectTransform selfRect;
    private RectTransform canvasRect;
    private RectTransform dragRoot;
    private Vector2 trayReturnPosition;
    private bool isDragging;

    public void Initialize(IngredientType type, BuildZone targetBuildZone, Canvas targetCanvas)
    {
        ingredientType = type;
        buildZone = targetBuildZone;
        canvas = targetCanvas;
        selfRect = GetComponent<RectTransform>();
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    // 드래그 시작: 떠 있는 복사본을 만들고 살짝 확대합니다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (buildZone == null || canvas == null)
        {
            return;
        }

        isDragging = true;
        CacheTrayReturnPosition(eventData);
        dragRoot = CreateDragVisual();
        dragRoot.SetAsLastSibling();
        dragRoot.localScale = Vector3.one * 1.15f;
        UpdateDragPosition(eventData);
    }

    // 드래그 중: 포인터를 따라가고 좌우 이동량에 맞춰 살짝 기울입니다.
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragRoot == null)
        {
            return;
        }

        UpdateDragPosition(eventData);
        float zRotation = Mathf.Clamp(-eventData.delta.x * 0.65f, -15f, 15f);
        dragRoot.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    // 드롭 종료: BuildZone 안이면 스택에 넣고, 밖이면 트레이로 튕겨 돌아갑니다.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || dragRoot == null)
        {
            return;
        }

        isDragging = false;

        if (buildZone.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
        {
            RectTransform acceptedVisual = dragRoot;
            dragRoot = null;
            buildZone.AcceptIngredient(ingredientType, acceptedVisual);
        }
        else
        {
            StartCoroutine(ReturnAndDestroyRoutine(dragRoot));
            dragRoot = null;
        }
    }

    private RectTransform CreateDragVisual()
    {
        IngredientDefinition definition = IngredientLibrary.Get(ingredientType);

        GameObject rootObject = new GameObject(ingredientType + "_Drag", typeof(RectTransform));
        rootObject.transform.SetParent(canvas.transform, false);
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(100f, 100f);

        CanvasGroup group = rootObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        RectTransform shadow = UIFactory.CreateImage("DragShadow", root, new Color(0f, 0f, 0f, 0.3f), new Vector2(90f, 90f));
        shadow.anchoredPosition = new Vector2(8f, -8f);
        shadow.GetComponent<Image>().raycastTarget = false;

        RectTransform card = UIFactory.CreateImage("Card", root, definition.Color, new Vector2(100f, 100f));
        card.anchoredPosition = Vector2.zero;
        card.GetComponent<Image>().raycastTarget = false;

        Text label = UIFactory.CreateText("Label", card, definition.DisplayName, 20, Color.white, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        UIFactory.StretchToParent(labelRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));

        return root;
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            dragRoot.anchoredPosition = localPoint;
        }
    }

    private void CacheTrayReturnPosition(PointerEventData eventData)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, selfRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventData.pressEventCamera,
            out trayReturnPosition);
    }

    private IEnumerator ReturnAndDestroyRoutine(RectTransform visual)
    {
        if (visual == null)
        {
            yield break;
        }

        yield return SimpleTween.MoveTo(visual, trayReturnPosition, 0.25f, SimpleTween.EaseOutBack);
        yield return SimpleTween.Shake(visual, 0.16f, 5f);

        if (visual != null)
        {
            Destroy(visual.gameObject);
        }
    }
}
