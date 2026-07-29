using System;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerPackagingController : MonoBehaviour
    {
        private static readonly Color Ink = Hex("#18323D");
        private static readonly Color Border = Hex("#1C3540");
        private static readonly Color Board = Hex("#FFF8EA");
        private static readonly Color Tray = Hex("#F2E1BF");
        private static readonly Color BoardEdge = Hex("#62BFE3");
        private static readonly Color Accent = Hex("#4BAED4");

        private RectTransform pageRoot;
        private RectTransform burgerTray;
        private RectTransform currentBurgerRoot;
        private Font uiFont;
        private Text statusText;
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
            statusText.text = "햄버거가 트레이에 놓였습니다. 포장하기 버튼을 누르세요.";
            statusText.color = Ink;
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
            if (statusText != null)
            {
                statusText.text = "포장대는 언제든 확인할 수 있습니다. 완성된 햄버거를 트레이로 옮겨 주세요.";
                statusText.color = Ink;
            }
        }

        public void SetZoneEntered()
        {
            if (statusText == null)
            {
                return;
            }

            if (!HasBurger)
            {
                statusText.text = "완성된 햄버거를 조립 구역에서 드래그해 중앙 트레이에 놓으세요.";
                statusText.color = Ink;
            }
            else if (!isPackaged)
            {
                statusText.text = "햄버거가 트레이에 있습니다. 오른쪽의 포장하기 버튼을 누르세요.";
                statusText.color = Ink;
            }
        }

        public void SetBurgerDragInProgress()
        {
            if (statusText != null && !HasBurger)
            {
                statusText.text = "드래그 중인 햄버거를 중앙 트레이 안에 놓으세요.";
                statusText.color = Ink;
            }
        }

        public void SetBurgerDropRejected()
        {
            if (statusText != null && !HasBurger)
            {
                statusText.text = "트레이 밖에 놓았습니다. 조립 구역으로 돌아가 다시 옮겨 주세요.";
                statusText.color = Hex("#A33A2B");
            }
        }

        private void BuildInterface()
        {
            CreateText("PackagingTitle", pageRoot, "햄버거 포장대", 52, FontStyle.Bold, Ink, new Vector2(0f, 470f), new Vector2(900f, 80f));
            CreateText("PackagingHelp", pageRoot, "완성된 햄버거를 중앙 트레이에 직접 놓은 뒤 오른쪽 버튼을 누르세요.", 24, FontStyle.Bold, Ink, new Vector2(0f, 405f), new Vector2(1250f, 50f));
            CreateText("PackagingSwipeHint", pageRoot, "→ 오른쪽으로 스와이프하면 조립 구역으로 돌아갑니다", 20, FontStyle.Bold, Ink, new Vector2(0f, 350f), new Vector2(900f, 42f));

            RectTransform boardFrame = CreateRoundedPanel(
                "PackagingBoardFrame",
                pageRoot,
                BoardEdge,
                new Vector2(-170f, -40f),
                new Vector2(1120f, 700f),
                false,
                34f);
            RectTransform boardRoot = CreateRoundedPanel(
                "PackagingBoard",
                boardFrame,
                Board,
                Vector2.zero,
                new Vector2(1060f, 640f),
                false,
                30f);
            CreateText("PackagingBoardLabel", boardRoot, "포장 트레이 · 햄버거를 여기에 드롭", 25, FontStyle.Bold, Ink, new Vector2(0f, 275f), new Vector2(700f, 45f));

            burgerTray = CreateRoundedPanel(
                "PackagingTray",
                boardRoot,
                Tray,
                new Vector2(0f, -35f),
                new Vector2(900f, 500f),
                false,
                26f);
            CreateText("PackagingTrayHint", burgerTray, "햄버거 대기 트레이", 22, FontStyle.Bold, Ink, new Vector2(0f, 205f), new Vector2(500f, 40f));

            RectTransform buttonRect = CreateRoundedPanel(
                "PackageButton",
                pageRoot,
                Accent,
                new Vector2(650f, -25f),
                new Vector2(320f, 130f),
                true,
                30f);
            packageButton = buttonRect.gameObject.AddComponent<Button>();
            packageButton.targetGraphic = buttonRect.GetComponent<Graphic>();
            packageButton.onClick.AddListener(PackageBurger);
            CreateText("PackageButtonLabel", buttonRect, "포장하기", 34, FontStyle.Bold, Color.white, Vector2.zero, buttonRect.sizeDelta);

            statusText = CreateText(
                "PackagingStatus",
                pageRoot,
                string.Empty,
                25,
                FontStyle.Bold,
                Ink,
                new Vector2(0f, -455f),
                new Vector2(1400f, 60f));
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
            statusText.text = "포장이 완료되었습니다!";
            statusText.color = Hex("#287A3A");
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
                new Color(0.82f, 0.94f, 1f, 0.88f),
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
            CreateText("PackageWrapLabel", wrapRect, "포장 완료", 34, FontStyle.Bold, Ink, new Vector2(0f, -92f), new Vector2(size.x - 30f, 60f));
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
            outline.effectColor = Border;
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

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.magenta;
        }
    }
}
