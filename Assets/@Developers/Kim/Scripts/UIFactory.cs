using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI 생성 반복 코드를 줄이기 위한 헬퍼입니다.
// 붙이는 오브젝트: 없음. CounterUI, CookingUI, BuildZone이 사용합니다.
public static class UIFactory
{
    private static Sprite whiteSprite;
    private static Sprite circleSprite;
    private static Font defaultFont;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "Runtime_White_1x1";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                Object.DontDestroyOnLoad(texture);

                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                whiteSprite.name = "Runtime_White_1x1_Sprite";
                Object.DontDestroyOnLoad(whiteSprite);
            }

            return whiteSprite;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
            {
                const int size = 32;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "Runtime_White_Circle";

                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.45f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = Mathf.Clamp01(radius - distance + 1f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                Object.DontDestroyOnLoad(texture);
                circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                circleSprite.name = "Runtime_White_Circle_Sprite";
                Object.DontDestroyOnLoad(circleSprite);
            }

            return circleSprite;
        }
    }

    public static Font DefaultFont
    {
        get
        {
            if (defaultFont == null)
            {
                string[] koreanFontFallbacks =
                {
                    "Apple SD Gothic Neo",
                    "AppleGothic",
                    "Arial Unicode MS",
                    "Noto Sans CJK KR",
                    "Arial"
                };

                defaultFont = Font.CreateDynamicFontFromOSFont(koreanFontFallbacks, 24);
                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            return defaultFont;
        }
    }

    public static Canvas CreateCanvas(string name)
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(name, typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    public static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 size, Sprite sprite = null)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : WhiteSprite;
        image.color = color;
        return rect;
    }

    public static Text CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text uiText = textObject.AddComponent<Text>();
        uiText.font = DefaultFont;
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.alignment = anchor;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return uiText;
    }

    public static Button CreateButton(string name, Transform parent, string label, Vector2 size, Color normalColor)
    {
        RectTransform rect = CreateImage(name, parent, normalColor, size);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        button.colors = colors;

        Text text = CreateText("Label", rect, label, 34, Color.white, TextAnchor.MiddleCenter);
        RectTransform textRect = text.GetComponent<RectTransform>();
        StretchToParent(textRect, Vector2.zero, Vector2.zero);
        return button;
    }

    public static RectTransform CreateIngredientIcon(IngredientType type, Transform parent, string objectName, bool raycastTarget)
    {
        IngredientDefinition definition = IngredientLibrary.Get(type);
        RectTransform icon = CreateImage(objectName, parent, definition.Color, new Vector2(100f, 100f));
        Image image = icon.GetComponent<Image>();
        image.raycastTarget = raycastTarget;

        Text label = CreateText("Label", icon, definition.DisplayName, 19, BestTextColor(definition.Color), TextAnchor.MiddleCenter);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        StretchToParent(labelRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        return icon;
    }

    public static void StretchToParent(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    public static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color BestTextColor(Color background)
    {
        float brightness = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
        return brightness > 0.58f ? new Color(0.12f, 0.10f, 0.08f) : Color.white;
    }
}
