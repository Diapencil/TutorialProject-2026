// 카운터 씬에서 구매한 장식 오브젝트만 표시한다.
using System;
using System.Collections.Generic;
using SheepSheepBurger.Core;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SheepSheepBurger.Counter
{
    [DisallowMultipleComponent]
    public sealed class CounterDecorationPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class DecorationVisual
        {
            public int decorationId;
            public GameObject visual;
        }

        [SerializeField] private List<DecorationVisual> decorationVisuals = new List<DecorationVisual>();
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private RectTransform placementRoot;
        [SerializeField] private Vector2 defaultVisualSize = new Vector2(88f, 88f);
        [SerializeField] private Vector2 placementPreviewSize = new Vector2(96f, 96f);

        private DecorationData pendingDecoration;
        private Image previewImage;
        private RectTransform previewRect;

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Refresh();
                TryBeginPendingPlacement();
            }
        }

        private void Update()
        {
            if (pendingDecoration == null)
            {
                return;
            }

            if (TryGetPointerPosition(out Vector2 screenPosition))
            {
                MovePreview(screenPosition);
            }

            if (TryGetPointerDown(out screenPosition))
            {
                PlacePendingDecoration(screenPosition);
            }
        }

        [ContextMenu("Refresh Purchased Decorations")]
        public void Refresh()
        {
            GameState state = GameManager.GetOrCreate().State;
            ResolveCatalog();
            ResolvePlacementRoot();

            for (int i = 0; i < decorationVisuals.Count; i++)
            {
                DecorationVisual entry = decorationVisuals[i];
                if (entry?.visual != null)
                {
                    bool purchased = state.IsDecorationPurchased(entry.decorationId);
                    DecorationData decoration = FindDecoration(entry.decorationId);
                    ApplyDecorationVisual(entry.visual, decoration, state);
                    entry.visual.SetActive(purchased && entry.decorationId != pendingDecoration?.id);
                }
            }
        }

        private void TryBeginPendingPlacement()
        {
            pendingDecoration = CounterDecorationPlacementSession.Consume();
            if (pendingDecoration == null)
            {
                return;
            }

            ResolvePlacementRoot();
            EnsurePreview();
            if (previewImage != null)
            {
                previewImage.sprite = pendingDecoration.sprite;
                previewImage.enabled = pendingDecoration.sprite != null;
            }

            if (previewRect != null)
            {
                previewRect.sizeDelta = placementPreviewSize;
            }

            Refresh();
        }

        private void PlacePendingDecoration(Vector2 screenPosition)
        {
            if (pendingDecoration == null || !TryGetLocalPoint(screenPosition, out Vector2 localPosition))
            {
                return;
            }

            GameState state = GameManager.GetOrCreate().State;
            state.PurchaseDecoration(pendingDecoration.id);
            state.SetDecorationPosition(pendingDecoration.id, localPosition);
            GameManager.SaveCurrentGame();

            pendingDecoration = null;
            DestroyPreview();
            Refresh();
        }

        private void MovePreview(Vector2 screenPosition)
        {
            EnsurePreview();
            if (previewRect == null || !TryGetLocalPoint(screenPosition, out Vector2 localPosition))
            {
                return;
            }

            previewRect.anchoredPosition = localPosition;
        }

        private void ApplyDecorationVisual(GameObject visual, DecorationData decoration, GameState state)
        {
            if (visual == null || decoration == null)
            {
                return;
            }

            Image image = visual.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = decoration.sprite;
                image.enabled = decoration.sprite != null;
                image.preserveAspect = true;
            }

            RectTransform rect = visual.transform as RectTransform;
            if (rect != null)
            {
                if (state.TryGetDecorationPosition(decoration.id, out Vector2 savedPosition))
                {
                    rect.anchoredPosition = savedPosition;
                }
                else
                {
                    rect.anchoredPosition = decoration.counterPosition;
                }

                if (rect.sizeDelta == Vector2.zero)
                {
                    rect.sizeDelta = defaultVisualSize;
                }
            }
        }

        private DecorationData FindDecoration(int id)
        {
            ResolveCatalog();
            if (catalog == null || catalog.Decorations == null)
            {
                return null;
            }

            for (int i = 0; i < catalog.Decorations.Length; i++)
            {
                DecorationData decoration = catalog.Decorations[i];
                if (decoration != null && decoration.id == id)
                {
                    return decoration;
                }
            }

            return null;
        }

        private void EnsurePreview()
        {
            ResolvePlacementRoot();
            if (previewRect != null)
            {
                return;
            }

            var previewObject = new GameObject("DecorationPlacementPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewObject.transform.SetParent(placementRoot != null ? placementRoot : transform, false);
            previewRect = (RectTransform)previewObject.transform;
            previewRect.sizeDelta = placementPreviewSize;
            previewImage = previewObject.GetComponent<Image>();
            previewImage.raycastTarget = false;
            previewImage.preserveAspect = true;
            previewObject.transform.SetAsLastSibling();
        }

        private void DestroyPreview()
        {
            if (previewRect != null)
            {
                Destroy(previewRect.gameObject);
            }

            previewRect = null;
            previewImage = null;
        }

        private void ResolveCatalog()
        {
            if (catalog == null)
            {
                catalog = ShopCatalog.LoadDefault();
            }
        }

        private void ResolvePlacementRoot()
        {
            if (placementRoot == null)
            {
                placementRoot = transform as RectTransform;
            }
        }

        private bool TryGetLocalPoint(Vector2 screenPosition, out Vector2 localPosition)
        {
            ResolvePlacementRoot();
            if (placementRoot == null)
            {
                localPosition = default;
                return false;
            }

            Canvas canvas = placementRoot.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                placementRoot,
                screenPosition,
                camera,
                out localPosition);
        }

        private static bool TryGetPointerPosition(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            screenPosition = Input.mousePosition;
            return true;
#else
            screenPosition = default;
            return false;
#endif
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
#endif

            screenPosition = default;
            return false;
        }
    }
}
