using SheepSheepBurger.SceneFlow;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SheepSheepBurger.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopOutsideReturnArea : MonoBehaviour
    {
        [Header("씬 이동")]
        [SerializeField] private string counterSceneName = "Counter";

        [Header("닫히지 않는 화면 영역")]
        [Tooltip("화면 전체를 0~1로 봤을 때, 이 Rect 안쪽 클릭은 상점에 머문다.")]
        [SerializeField] private Rect safeViewportRect = new Rect(0f, 0.151f, 1f, 0.7169f);

        [Tooltip("밖에서 누른 뒤 안쪽으로 드래그해 놓으면 씬 이동을 막는다.")]
        [SerializeField] private bool requirePointerUpOutside = true;

        private bool pointerStartedOutsideSafeArea;

        private void OnValidate()
        {
            safeViewportRect = ClampViewportRect(safeViewportRect);
        }

        private void Update()
        {
            if (TryGetPointerDown(out Vector2 downPosition))
            {
                pointerStartedOutsideSafeArea = IsOutsideSafeArea(downPosition);
            }

            if (!TryGetPointerUp(out Vector2 upPosition))
            {
                return;
            }

            bool canReturn = pointerStartedOutsideSafeArea &&
                (!requirePointerUpOutside || IsOutsideSafeArea(upPosition));

            pointerStartedOutsideSafeArea = false;

            if (canReturn)
            {
                ReturnToCounter();
            }
        }

        public void ReturnToCounter()
        {
            if (string.IsNullOrWhiteSpace(counterSceneName))
            {
                Debug.LogWarning("ShopOutsideReturnArea: counterSceneName is empty.");
                return;
            }

            SceneTransitionManager.LoadSceneSlideLeft(counterSceneName);
        }

        public void SetSafeViewportRect(Rect rect)
        {
            safeViewportRect = ClampViewportRect(rect);
        }

        private bool IsOutsideSafeArea(Vector2 screenPosition)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }

            Vector2 viewportPosition = new Vector2(
                Mathf.Clamp01(screenPosition.x / Screen.width),
                Mathf.Clamp01(screenPosition.y / Screen.height));

            return !safeViewportRect.Contains(viewportPosition);
        }

        private static Rect ClampViewportRect(Rect rect)
        {
            float xMin = Mathf.Clamp01(Mathf.Min(rect.xMin, rect.xMax));
            float xMax = Mathf.Clamp01(Mathf.Max(rect.xMin, rect.xMax));
            float yMin = Mathf.Clamp01(Mathf.Min(rect.yMin, rect.yMax));
            float yMax = Mathf.Clamp01(Mathf.Max(rect.yMin, rect.yMax));

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
#endif

            screenPosition = default;
            return false;
        }

        private static bool TryGetPointerUp(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonUp(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
#endif

            screenPosition = default;
            return false;
        }
    }
}
