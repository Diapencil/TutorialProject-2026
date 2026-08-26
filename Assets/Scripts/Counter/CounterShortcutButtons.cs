using SheepSheepBurger.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SheepSheepBurger.Counter
{
    [DisallowMultipleComponent]
    public sealed class CounterShortcutButtons : MonoBehaviour
    {
        [Header("클릭 대상")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer shopButtonRenderer;
        [SerializeField] private SpriteRenderer settingsButtonRenderer;
        [SerializeField, Min(0f)] private float hitPadding = 0.08f;

        [Header("상점")]
        [SerializeField] private string shopSceneName = "ShopScene";

        [Header("설정 레이어")]
        [SerializeField] private SettingsLayerController settingsLayer;
        [SerializeField] private GameObject settingsLayerPrefab;
        [SerializeField] private bool closeSettingsLayerOnStart = true;
        [SerializeField] private bool showBuiltInSettingsButtonOnlyWhenLayerOpen = true;

        private SpriteRenderer pressedRenderer;
        private GameObject builtInSettingsButton;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();

            if (closeSettingsLayerOnStart && settingsLayer != null)
            {
                settingsLayer.Close();
            }

            SyncBuiltInSettingsButtonVisibility();
        }

        private void Update()
        {
            SyncBuiltInSettingsButtonVisibility();

            if (TryGetPointerDown(out Vector2 downPosition))
            {
                pressedRenderer = ShouldIgnoreShortcutInput() ? null : HitTest(downPosition);
            }

            if (!TryGetPointerUp(out Vector2 upPosition))
            {
                return;
            }

            if (ShouldIgnoreShortcutInput())
            {
                pressedRenderer = null;
                return;
            }

            SpriteRenderer releasedRenderer = HitTest(upPosition);

            if (pressedRenderer != null && pressedRenderer == releasedRenderer)
            {
                InvokeButton(pressedRenderer);
            }

            pressedRenderer = null;
        }

        public void OpenShopScene()
        {
            if (string.IsNullOrWhiteSpace(shopSceneName))
            {
                Debug.LogWarning("CounterShortcutButtons: shopSceneName is empty.");
                return;
            }

            SceneManager.LoadScene(shopSceneName);
        }

        public void ToggleSettingsLayer()
        {
            SettingsLayerController layer = EnsureSettingsLayer();

            if (layer == null)
            {
                Debug.LogWarning("CounterShortcutButtons: SettingsLayerController is missing.");
                return;
            }

            layer.Toggle();
            SyncBuiltInSettingsButtonVisibility();
        }

        private void ResolveReferences()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (shopButtonRenderer == null)
            {
                shopButtonRenderer = FindSpriteRendererByName("shop button");
            }

            if (settingsButtonRenderer == null)
            {
                settingsButtonRenderer = FindSpriteRendererByName("setting button");
            }

            if (settingsLayer == null)
            {
                settingsLayer = FindFirstObjectByType<SettingsLayerController>(FindObjectsInactive.Include);
            }
        }

        private SettingsLayerController EnsureSettingsLayer()
        {
            ResolveReferences();

            if (settingsLayer != null)
            {
                return settingsLayer;
            }

            if (settingsLayerPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(settingsLayerPrefab);
            instance.name = settingsLayerPrefab.name;
            settingsLayer = instance.GetComponent<SettingsLayerController>();
            return settingsLayer;
        }

        private void InvokeButton(SpriteRenderer renderer)
        {
            if (renderer == settingsButtonRenderer)
            {
                ToggleSettingsLayer();
                return;
            }

            if (settingsLayer != null && settingsLayer.IsOpen)
            {
                return;
            }

            if (renderer == shopButtonRenderer)
            {
                OpenShopScene();
            }
        }

        private SpriteRenderer HitTest(Vector2 screenPosition)
        {
            if (ContainsScreenPoint(settingsButtonRenderer, screenPosition))
            {
                return settingsButtonRenderer;
            }

            if (settingsLayer != null && settingsLayer.IsOpen)
            {
                return null;
            }

            return ContainsScreenPoint(shopButtonRenderer, screenPosition) ? shopButtonRenderer : null;
        }

        private bool ContainsScreenPoint(SpriteRenderer renderer, Vector2 screenPosition)
        {
            Camera cameraForHit = targetCamera != null ? targetCamera : Camera.main;

            if (renderer == null || renderer.sprite == null || cameraForHit == null)
            {
                return false;
            }

            float zDistance = Mathf.Abs(renderer.transform.position.z - cameraForHit.transform.position.z);
            Vector3 worldPoint = cameraForHit.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
            Bounds bounds = renderer.bounds;
            bounds.Expand(hitPadding);
            return bounds.Contains(worldPoint);
        }

        private void SyncBuiltInSettingsButtonVisibility()
        {
            if (!showBuiltInSettingsButtonOnlyWhenLayerOpen)
            {
                return;
            }

            SettingsLayerController layer = EnsureSettingsLayer();

            if (layer == null)
            {
                return;
            }

            if (builtInSettingsButton == null)
            {
                Transform buttonTransform = FindChildRecursive(layer.transform, "SettingsButton");
                builtInSettingsButton = buttonTransform != null ? buttonTransform.gameObject : null;
            }

            if (builtInSettingsButton != null && builtInSettingsButton.activeSelf != layer.IsOpen)
            {
                builtInSettingsButton.SetActive(layer.IsOpen);
            }
        }

        private static SpriteRenderer FindSpriteRendererByName(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<SpriteRenderer>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, childName);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool ShouldIgnoreShortcutInput()
        {
            return settingsLayer != null && settingsLayer.IsOpen && IsPointerOverUi();
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

                if (touch.phase == TouchPhase.Ended)
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
