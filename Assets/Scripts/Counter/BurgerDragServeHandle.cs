using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SheepSheepBurger.Counter
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BurgerDragServeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform dropTarget;
        [SerializeField] private CanvasGroup canvasGroup;
        [Min(0f), SerializeField] private float enterAnimationDuration = 0.4f;
        [Tooltip("등장 시작 위치를 기준 위치에서 아래로 얼마나 떨어뜨릴지(화면 밖에서 올라오는 느낌을 위한 값)입니다.")]
        [SerializeField] private float enterAnimationDistance = 700f;

        public event Action Dropped;

        private RectTransform rectTransform;
        private RectTransform parentRectTransform;
        private Canvas canvas;
        private Vector2 originAnchoredPosition;
        private Coroutine enterCoroutine;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            parentRectTransform = rectTransform.parent as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            originAnchoredPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            if (enterCoroutine != null) StopCoroutine(enterCoroutine);
            enterCoroutine = StartCoroutine(PlayEnterAnimation());
        }

        private IEnumerator PlayEnterAnimation()
        {
            var startPosition = originAnchoredPosition + Vector2.down * enterAnimationDistance;
            rectTransform.anchoredPosition = startPosition;

            var elapsed = 0f;
            while (elapsed < enterAnimationDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / enterAnimationDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, originAnchoredPosition, eased);
                yield return null;
            }

            rectTransform.anchoredPosition = originAnchoredPosition;
            enterCoroutine = null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (enterCoroutine != null)
            {
                StopCoroutine(enterCoroutine);
                enterCoroutine = null;
            }
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, eventData.position, camera, out var localPoint))
                rectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (dropTarget != null && RectTransformUtility.RectangleContainsScreenPoint(dropTarget, eventData.position, camera))
                Dropped?.Invoke();
            else
                rectTransform.anchoredPosition = originAnchoredPosition;
        }
    }
}
