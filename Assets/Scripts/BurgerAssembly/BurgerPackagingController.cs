using System;
using System.Collections;
using System.Linq;
using SheepSheepBurger.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.BurgerAssembly
{
    [DisallowMultipleComponent]
    public sealed class BurgerPackagingController : MonoBehaviour
    {
        private const string PackagingResourcePath = "BurgerAssembly/Packaging";
        private const string OpenBoxSpriteName = "burger_box_open";
        private const string ClosedBoxSpriteName = "burger_box_closed";
        private const string ClosingFramePrefix = "burger_box_closing_";
        private const float ClosingFrameSeconds = 0.07f;

        private static readonly Color Border = BurgerPrototypeTheme.Border;
        private static readonly Vector2 BoxArtPosition = new Vector2(0f, 145f);
        private static readonly Vector2 BoxArtSize = new Vector2(270f, 437.44f);

        private RectTransform pageRoot;
        private RectTransform burgerTray;
        private RectTransform currentBurgerRoot;
        private Button legacyPackageButton;
        private Image openBoxImage;
        private Image closingBoxImage;
        private Sprite openBoxSprite;
        private Sprite closedBoxSprite;
        private Sprite[] closingFrames = Array.Empty<Sprite>();
        private Coroutine closingRoutine;
        private float burgerHalfWidth;
        private float burgerMinY;
        private float burgerMaxY;
        private bool isClosing;
        private bool isPackaged;

        public RectTransform BurgerTray => burgerTray;

        public bool HasBurger => currentBurgerRoot != null;

        public bool IsPackaged => isPackaged;

        public event Action Packaged;

        public void Configure(RectTransform page, Font font)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            pageRoot = page;
            if (!TryBindExistingInterface())
            {
                BuildInterface();
            }

            EnsureBoxArt();
            DisableLegacyPackageButton();
            ResetPackaging();
        }

        public bool TryPlaceBurger(
            RectTransform burgerRoot,
            Vector2 trayLocalPosition,
            float halfWidth,
            float minimumY,
            float maximumY)
        {
            if (burgerRoot == null || burgerTray == null || HasBurger || isClosing)
            {
                return false;
            }

            currentBurgerRoot = burgerRoot;
            burgerHalfWidth = Mathf.Max(0f, halfWidth);
            burgerMinY = minimumY;
            burgerMaxY = maximumY;
            isPackaged = false;

            ResetBoxVisuals();
            currentBurgerRoot.SetParent(burgerTray, false);
            currentBurgerRoot.gameObject.SetActive(true);
            SetRect(currentBurgerRoot, ClampBurgerPosition(Vector2.zero), Vector2.zero);
            currentBurgerRoot.SetAsLastSibling();
            closingBoxImage?.transform.SetAsLastSibling();

            AudioManager.GetOrCreate().PlaySfx(AudioCueIds.PlaceInBox);
            BeginBoxClosing();
            return true;
        }

        public void ResetPackaging()
        {
            if (closingRoutine != null)
            {
                StopCoroutine(closingRoutine);
                closingRoutine = null;
            }

            currentBurgerRoot = null;
            burgerHalfWidth = 0f;
            burgerMinY = 0f;
            burgerMaxY = 0f;
            isClosing = false;
            isPackaged = false;
            ResetBoxVisuals();
            DisableLegacyPackageButton();
        }

        public void SetZoneEntered()
        {
            EnsureBoxArt();
        }

        public void SetBurgerDragInProgress()
        {
        }

        public void SetBurgerDropRejected()
        {
        }

        private void BuildInterface()
        {
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
        }

        private bool TryBindExistingInterface()
        {
            burgerTray = FindChildByName<RectTransform>(pageRoot, "PackagingTray");
            if (burgerTray == null)
            {
                return false;
            }

            RectTransform buttonRect = FindChildByName<RectTransform>(pageRoot, "PackageButton");
            legacyPackageButton = buttonRect != null ? buttonRect.GetComponent<Button>() : null;
            return true;
        }

        private void DisableLegacyPackageButton()
        {
            if (legacyPackageButton == null)
            {
                return;
            }

            legacyPackageButton.onClick.RemoveAllListeners();
            legacyPackageButton.interactable = false;
            legacyPackageButton.gameObject.SetActive(false);
        }

        private void PackageBurger()
        {
            BeginBoxClosing();
        }

        private void BeginBoxClosing()
        {
            if (!HasBurger || isClosing || isPackaged)
            {
                return;
            }

            EnsureBoxArt();
            isClosing = true;
            AudioManager.GetOrCreate().PlaySfx(AudioCueIds.WrapPackage);

            if (!Application.isPlaying || closingFrames.Length == 0)
            {
                CompletePackaging();
                return;
            }

            closingRoutine = StartCoroutine(PlayClosingAnimation());
        }

        private IEnumerator PlayClosingAnimation()
        {
            closingBoxImage.enabled = true;
            closingBoxImage.transform.SetAsLastSibling();
            for (int i = 0; i < closingFrames.Length; i++)
            {
                closingBoxImage.sprite = closingFrames[i];
                yield return new WaitForSecondsRealtime(ClosingFrameSeconds);
            }

            closingRoutine = null;
            CompletePackaging();
        }

        private void CompletePackaging()
        {
            if (isPackaged)
            {
                return;
            }

            EnsureBoxArt();
            openBoxImage.enabled = false;
            closingBoxImage.sprite = closedBoxSprite != null
                ? closedBoxSprite
                : closingFrames.LastOrDefault();
            closingBoxImage.enabled = closingBoxImage.sprite != null;
            closingBoxImage.transform.SetAsLastSibling();

            if (currentBurgerRoot != null)
            {
                currentBurgerRoot.gameObject.SetActive(false);
            }

            isClosing = false;
            isPackaged = true;
            Packaged?.Invoke();
        }

        private void EnsureBoxArt()
        {
            if (burgerTray == null)
            {
                return;
            }

            LoadBoxSprites();
            if (openBoxImage == null)
            {
                openBoxImage = FindChildByName<Image>(burgerTray, "BurgerBoxOpenArt");
            }
            if (openBoxImage == null)
            {
                openBoxImage = CreateSpriteImage("BurgerBoxOpenArt", burgerTray);
            }

            if (closingBoxImage == null)
            {
                closingBoxImage = FindChildByName<Image>(burgerTray, "BurgerBoxClosingArt");
            }
            if (closingBoxImage == null)
            {
                closingBoxImage = CreateSpriteImage("BurgerBoxClosingArt", burgerTray);
            }

            openBoxImage.sprite = openBoxSprite;
            openBoxImage.preserveAspect = true;
            openBoxImage.raycastTarget = false;
            closingBoxImage.preserveAspect = true;
            closingBoxImage.raycastTarget = false;
            SetRect(openBoxImage.rectTransform, BoxArtPosition, BoxArtSize);
            SetRect(closingBoxImage.rectTransform, BoxArtPosition, BoxArtSize);
            openBoxImage.transform.SetAsFirstSibling();
            closingBoxImage.transform.SetAsLastSibling();
        }

        private void LoadBoxSprites()
        {
            if (openBoxSprite != null && closedBoxSprite != null && closingFrames.Length > 0)
            {
                return;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(PackagingResourcePath);
            openBoxSprite = sprites.FirstOrDefault(sprite => sprite.name == OpenBoxSpriteName);
            closedBoxSprite = sprites.FirstOrDefault(sprite => sprite.name == ClosedBoxSpriteName);
            closingFrames = sprites
                .Where(sprite => sprite.name.StartsWith(ClosingFramePrefix, StringComparison.Ordinal))
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
        }

        private void ResetBoxVisuals()
        {
            EnsureBoxArt();
            if (openBoxImage != null)
            {
                openBoxImage.sprite = openBoxSprite;
                openBoxImage.enabled = openBoxSprite != null;
                openBoxImage.transform.SetAsFirstSibling();
            }
            if (closingBoxImage != null)
            {
                closingBoxImage.sprite = null;
                closingBoxImage.enabled = false;
                closingBoxImage.transform.SetAsLastSibling();
            }
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

        private static Image CreateSpriteImage(string name, RectTransform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static T FindChildByName<T>(Transform parent, string childName) where T : Component
        {
            if (parent == null)
            {
                return null;
            }

            foreach (T component in parent.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.name == childName)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
