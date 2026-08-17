using System;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerPackagingController : MonoBehaviour
    {
        private static readonly Color Border = BurgerPrototypeTheme.Border;
        private static readonly Color Accent = BurgerPrototypeTheme.Accent;

        private RectTransform pageRoot;
        private RectTransform burgerTray;
        private RectTransform currentBurgerRoot;
        private Font uiFont;
        private Button packageButton;
        private GameObject packageWrap;
        private float burgerHalfWidth;
        private float burgerMinY;
        private float burgerMaxY;
        private bool isPackaged;

        public RectTransform BurgerTray => burgerTray;

        public bool HasBurger => currentBurgerRoot != null;

        public bool IsPackaged => isPackaged;

        public void Configure(RectTransform page, Font font)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (pageRoot != null)
            {
                return;
            }

            pageRoot = page;
            uiFont = font;
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            BuildInterface();
            ResetPackaging();
        }

        public bool TryPlaceBurger(
            RectTransform burgerRoot,
            Vector2 trayLocalPosition,
            float halfWidth,
            float minimumY,
            float maximumY)
        {
            if (burgerRoot == null || burgerTray == null || HasBurger)
            {
                return false;
            }

            ClearPackageWrap();
            currentBurgerRoot = burgerRoot;
            burgerHalfWidth = Mathf.Max(0f, halfWidth);
            burgerMinY = minimumY;
            burgerMaxY = maximumY;
            isPackaged = false;

            currentBurgerRoot.SetParent(burgerTray, false);
            SetRect(
                currentBurgerRoot,
                ClampBurgerPosition(trayLocalPosition),
                Vector2.zero);
            currentBurgerRoot.SetAsLastSibling();

            packageButton.interactable = true;
            return true;
        }

        public void ResetPackaging()
        {
            currentBurgerRoot = null;
            burgerHalfWidth = 0f;
            burgerMinY = 0f;
            burgerMaxY = 0f;
            isPackaged = false;
            ClearPackageWrap();
            if (packageButton != null)
            {
                packageButton.interactable = false;
            }
        }

        public void SetZoneEntered()
        {
        }

        public void SetBurgerDragInProgress()
        {
        }

        public void SetBurgerDropRejected()
        {
        }

        private void BuildInterface()
        {
            // The desk painted on the far right is the complete packaging area.
            // These objects provide hit testing only and render no replacement desk.
            RectTransform boardFrame = CreateRoundedPanel(
                "PackagingBoardFrame",
                pageRoot,
                Color.clear,
                new Vector2(730f, -55f),
                new Vector2(360f, 260f),
                false,
                0f);
            RectTransform boardRoot = CreateRoundedPanel(
                "PackagingBoard",
                boardFrame,
                Color.clear,
                Vector2.zero,
                new Vector2(350f, 250f),
                false,
                0f);

            burgerTray = CreateRoundedPanel(
                "PackagingTray",
                boardRoot,
                CookingPrototypeRules.ShowTemporaryInteractionAreas
                    ? BurgerPrototypeTheme.Hex("#F4B9424D")
                    : Color.clear,
                Vector2.zero,
                new Vector2(330f, 230f),
                false,
                0f);

            RectTransform buttonRect = CreateRoundedPanel(
                "PackageButton",
                pageRoot,
                Accent,
                new Vector2(730f, -230f),
                new Vector2(180f, 68f),
                true,
                22f);
            packageButton = buttonRect.gameObject.AddComponent<Button>();
            packageButton.targetGraphic = buttonRect.GetComponent<Graphic>();
            packageButton.onClick.AddListener(PackageBurger);
            CreateText("PackageButtonLabel", buttonRect, "포장", 24, FontStyle.Bold, Color.white, Vector2.zero, buttonRect.sizeDelta);
        }

        private void PackageBurger()
        {
            if (!HasBurger || packageButton == null || !packageButton.interactable)
            {
                return;
            }

            isPackaged = true;
            packageButton.interactable = false;
            CreatePackageWrap();
        }

        private void CreatePackageWrap()
        {
            ClearPackageWrap();
            Vector2 size = new Vector2(
                Mathf.Max(420f, burgerHalfWidth * 2f + 100f),
                Mathf.Max(280f, burgerMaxY - burgerMinY + 100f));
            Vector2 center = currentBurgerRoot.anchoredPosition +
                new Vector2(0f, (burgerMinY + burgerMaxY) * 0.5f);
            RectTransform wrapRect = CreateRoundedPanel(
                "PackageWrap",
                burgerTray,
                Color.clear,
                center,
                size,
                false,
                36f);
            BurgerUiFactory.CreateShape(
                "PackagedBurgerArt",
                wrapRect,
                SimpleShape.Circle,
                Color.white,
                new Vector2(0f, 22f),
                new Vector2(260f, 145f),
                false,
                BurgerSpriteCatalog.RequireActive().CompletedBurger);
            packageWrap = wrapRect.gameObject;
            packageWrap.transform.SetAsLastSibling();
        }

        private Vector2 ClampBurgerPosition(Vector2 desired)
        {
            Rect bounds = burgerTray.rect;
            float minimumX = bounds.xMin + burgerHalfWidth;
            float maximumX = bounds.xMax - burgerHalfWidth;
            float minimumY = bounds.yMin - burgerMinY;
            float maximumY = bounds.yMax - burgerMaxY;
            float x = minimumX <= maximumX ? Mathf.Clamp(desired.x, minimumX, maximumX) : bounds.center.x;
            float y = minimumY <= maximumY
                ? Mathf.Clamp(desired.y, minimumY, maximumY)
                : bounds.center.y - (burgerMinY + burgerMaxY) * 0.5f;
            return new Vector2(x, y);
        }

        private void ClearPackageWrap()
        {
            if (packageWrap == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(packageWrap);
            }
            else
            {
                DestroyImmediate(packageWrap);
            }
            packageWrap = null;
        }

        private RectTransform CreateRoundedPanel(
            string name,
            RectTransform parent,
            Color color,
            Vector2 position,
            Vector2 size,
            bool raycastTarget,
            float cornerRadius)
        {
            SimpleShapeGraphic graphic = CreateShape(name, parent, SimpleShape.RoundedRectangle, color, position, size, raycastTarget);
            graphic.CornerRadius = cornerRadius;
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            Color border = Border;
            border.a = color.a <= 0.01f ? 0f : border.a;
            outline.effectColor = border;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            return graphic.rectTransform;
        }

        private static SimpleShapeGraphic CreateShape(
            string name,
            RectTransform parent,
            SimpleShape shape,
            Color color,
            Vector2 position,
            Vector2 size,
            bool raycastTarget)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(SimpleShapeGraphic));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            SimpleShapeGraphic graphic = gameObject.GetComponent<SimpleShapeGraphic>();
            graphic.Shape = shape;
            graphic.color = color;
            graphic.raycastTarget = raycastTarget;
            return graphic;
        }

        private Text CreateText(
            string name,
            RectTransform parent,
            string value,
            int size,
            FontStyle style,
            Color color,
            Vector2 position,
            Vector2 dimensions)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, dimensions);
            Text text = gameObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

    }
}
