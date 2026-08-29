using System.Text;
using SheepSheepBurger.Core;
using SheepSheepBurger.Counter;
using SheepSheepBurger.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SheepSheepBurger.Results
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class DayResultLayerController : MonoBehaviour
    {
        private const string PrefabResourcePath = "UI/DayResultLayer";
        private const string KoreanFontResourcePath = "Fonts & Materials/Shop Korean SDF";
        public const string RequiredFontCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz .,!?:;+-*/()[]{}<>%#&'\"₩C|OX" +
            "결과정산오늘의요약응대손님명건총매출재료비순이익평균보상힌트사용회등급주문품질성공률재료소비개없음외더보기상세로그아직기록된주문이없습니다" +
            "고객메뉴획득원가요구제출오차익힘전체완성미완성다음날닫기패티하단번상단양상추토마토치즈양파피클할라피뇨케첩머스터드" +
            "계란베이컨늑대기린사자코끼리캐시햄버거치즈버거비건채식핫도그후라이미아두쫀쿠와일드숲숲";
        private const int BuiltLayoutVersion = 3;
        private const int IngredientPreviewLimit = 8;

        public static DayResultLayerController Instance { get; private set; }

        [Header("표시")]
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private bool openOnDayComplete = true;
        [SerializeField] private bool showCompletedCurrentDayOnStart = true;
        [SerializeField] private bool createEventSystemIfMissing = true;
        [SerializeField] private string nextDaySceneName = "Counter";
        [SerializeField] private bool reloadCounterSceneOnNextDay = true;

        [Header("Canvas")]
        [SerializeField] private int canvasSortingOrder = 900;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField, Min(1f)] private float titleFontSize = 52f;
        [SerializeField, Min(1f)] private float sectionFontSize = 24f;
        [SerializeField, Min(1f)] private float logFontSize = 20f;
        [SerializeField, Min(1f)] private float buttonFontSize = 26f;

        [Header("색")]
        [SerializeField] private Color backdropColor = new Color(0.03f, 0.06f, 0.04f, 0.62f);
        [SerializeField] private Color panelColor = new Color(0.73f, 0.84f, 0.68f, 1f);
        [SerializeField] private Color sectionColor = new Color(0.88f, 0.94f, 0.82f, 0.98f);
        [SerializeField] private Color scrollColor = new Color(0.81f, 0.90f, 0.76f, 1f);
        [SerializeField] private Color outlineColor = new Color(0.17f, 0.36f, 0.24f, 1f);
        [SerializeField] private Color titleColor = new Color(0.06f, 0.16f, 0.09f, 1f);
        [SerializeField] private Color bodyTextColor = new Color(0.08f, 0.20f, 0.12f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.25f, 0.50f, 0.31f, 1f);
        [SerializeField] private Color buttonHighlightedColor = new Color(0.35f, 0.62f, 0.40f, 1f);
        [SerializeField] private Color buttonPressedColor = new Color(0.16f, 0.34f, 0.21f, 1f);

        [Header("문구")]
        [SerializeField] private string titleFormat = "D + {0} 정산";
        [SerializeField] private string summaryTitle = "오늘의 정산";
        [SerializeField] private string gradeTitle = "주문 품질";
        [SerializeField] private string ingredientTitle = "재료 사용";
        [SerializeField] private string logTitle = "상세 주문 로그";
        [SerializeField] private string closeButtonLabel = "닫기";
        [SerializeField] private string nextDayButtonLabel = "다음 날";

        [Header("참조")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backdrop;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text ingredientText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private RectTransform logContent;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button nextDayButton;
        [SerializeField, HideInInspector] private int builtLayoutVersion;

        private DayProgressRuntime dayProgress;

        public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying ||
                Instance != null ||
                FindFirstObjectByType<DayResultLayerController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name;
                return;
            }

            GameObject owner = new GameObject(nameof(DayResultLayerController), typeof(RectTransform));
            owner.AddComponent<DayResultLayerController>();
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                EnsureEventSystemIfNeeded();
            }

            BuildIfNeeded();
            ApplyVisuals();
            HookButtons();

            if (Application.isPlaying)
            {
                SubscribeToDayProgress();

                if (hideOnAwake)
                {
                    Close();
                }

                if (showCompletedCurrentDayOnStart && dayProgress != null && dayProgress.IsCurrentDayComplete)
                {
                    Open(dayProgress.DayState);
                }
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            SubscribeToDayProgress();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeFromDayProgress();
        }

        private void Update()
        {
            if (Application.isPlaying && dayProgress == null)
            {
                SubscribeToDayProgress();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnsubscribeFromDayProgress();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EditorApplication.delayCall -= ApplyEditorRefresh;
            EditorApplication.delayCall += ApplyEditorRefresh;
        }

        private void ApplyEditorRefresh()
        {
            EditorApplication.delayCall -= ApplyEditorRefresh;

            if (this == null)
            {
                return;
            }

            BuildIfNeeded();
            ApplyVisuals();
            Refresh(CreatePreviewDayState());
        }
#endif

        [ContextMenu("Show Current Day Result")]
        public void OpenCurrentDayResult()
        {
            if (Application.isPlaying)
            {
                Open(GameManager.GetOrCreate().State.GetOrCreateCurrentDayState());
                return;
            }

            Open(CreatePreviewDayState());
        }

        public void Open(DayState dayState)
        {
            BuildIfNeeded();
            ApplyVisuals();
            Refresh(dayState);
            SetVisible(true);
        }

        public void Close()
        {
            SetVisible(false);
        }

        [ContextMenu("Rebuild Result Layer Layout")]
        public void RebuildRoughLayout()
        {
            ClearChildren();
            ClearReferences();
            BuildIfNeeded();
            ApplyVisuals();
            Refresh(Application.isPlaying
                ? GameManager.GetOrCreate().State.GetOrCreateCurrentDayState()
                : CreatePreviewDayState());
        }

        [ContextMenu("Apply Result Layer Design Defaults")]
        public void ApplyPolishedDesignDefaults()
        {
            titleFontSize = 52f;
            sectionFontSize = 24f;
            logFontSize = 20f;
            buttonFontSize = 26f;

            backdropColor = new Color(0.03f, 0.06f, 0.04f, 0.62f);
            panelColor = new Color(0.73f, 0.84f, 0.68f, 1f);
            sectionColor = new Color(0.88f, 0.94f, 0.82f, 0.98f);
            scrollColor = new Color(0.81f, 0.90f, 0.76f, 1f);
            outlineColor = new Color(0.17f, 0.36f, 0.24f, 1f);
            titleColor = new Color(0.06f, 0.16f, 0.09f, 1f);
            bodyTextColor = new Color(0.08f, 0.20f, 0.12f, 1f);
            buttonColor = new Color(0.25f, 0.50f, 0.31f, 1f);
            buttonHighlightedColor = new Color(0.35f, 0.62f, 0.40f, 1f);
            buttonPressedColor = new Color(0.16f, 0.34f, 0.21f, 1f);

            titleFormat = "D + {0} 정산";
            summaryTitle = "오늘의 정산";
            gradeTitle = "주문 품질";
            ingredientTitle = "재료 사용";
            logTitle = "상세 주문 로그";
            closeButtonLabel = "닫기";
            nextDayButtonLabel = "다음 날";

            BuildIfNeeded();
            ApplyVisuals();
            Refresh(Application.isPlaying
                ? GameManager.GetOrCreate().State.GetOrCreateCurrentDayState()
                : CreatePreviewDayState());
        }

        public void BeginNextDay()
        {
            DayProgressRuntime runtime = DayProgressRuntime.GetOrCreate();
            runtime.BeginNextDay();
            Close();

            if (reloadCounterSceneOnNextDay && !string.IsNullOrWhiteSpace(nextDaySceneName))
            {
                SceneManager.LoadScene(nextDaySceneName);
            }
        }

        private void SubscribeToDayProgress()
        {
            DayProgressRuntime runtime = DayProgressRuntime.GetOrCreate();

            if (dayProgress == runtime)
            {
                return;
            }

            UnsubscribeFromDayProgress();
            dayProgress = runtime;

            if (dayProgress != null)
            {
                dayProgress.DayCompleted += HandleDayCompleted;
            }
        }

        private void UnsubscribeFromDayProgress()
        {
            if (dayProgress != null)
            {
                dayProgress.DayCompleted -= HandleDayCompleted;
                dayProgress = null;
            }
        }

        private void HandleDayCompleted(DayState completedDayState)
        {
            if (openOnDayComplete)
            {
                Open(completedDayState);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            SubscribeToDayProgress();
        }

        private void EnsureEventSystemIfNeeded()
        {
            if (!createEventSystemIfMissing || EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private void SetVisible(bool visible)
        {
            BuildIfNeeded();

            if (visible)
            {
                transform.SetAsLastSibling();
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void Refresh(DayState dayState)
        {
            if (dayState == null)
            {
                return;
            }

            dayState.EnsureInitialized(dayState.dayNumber);
            string title = string.Format(titleFormat, dayState.dayNumber);
            string summary = BuildSummaryText(dayState);
            string grades = BuildGradeText(dayState);
            string ingredients = BuildIngredientText(dayState);
            string logs = BuildOrderLogText(dayState);

            EnsureFontContainsCharacters(title + summary + grades + ingredients + logs + closeButtonLabel + nextDayButtonLabel);

            titleText.text = title;
            summaryText.text = summary;
            gradeText.text = grades;
            ingredientText.text = ingredients;
            logText.text = logs;
            UpdateLogContentHeight();
        }

        private string BuildSummaryText(DayState dayState)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(summaryTitle);
            builder.AppendLine($"응대 손님  {dayState.customersServed}명");
            builder.AppendLine($"힌트 사용  {dayState.ordersWithHint}회");
            builder.AppendLine($"총매출     {CurrencyUtil.ToDisplay(dayState.dailyRevenue)}");
            builder.AppendLine($"재료비     {CurrencyUtil.ToDisplay(dayState.dailyIngredientCost)}");
            builder.AppendLine($"순이익     {CurrencyUtil.ToDisplay(dayState.dailyProfit)}");
            builder.AppendLine($"평균 보상  {CurrencyUtil.ToDisplay(dayState.averageReward)}");
            return builder.ToString();
        }

        private string BuildGradeText(DayState dayState)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(gradeTitle);
            builder.AppendLine($"Perfect  {dayState.perfectCount}");
            builder.AppendLine($"Good     {dayState.goodCount}");
            builder.AppendLine($"Normal   {dayState.normalCount}");
            builder.AppendLine($"Bad      {dayState.badCount}");
            return builder.ToString();
        }

        private string BuildIngredientText(DayState dayState)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(ingredientTitle);

            if (dayState.ingredientUsages == null || dayState.ingredientUsages.Count == 0)
            {
                builder.AppendLine("소비 없음");
                return builder.ToString();
            }

            builder.AppendLine($"총 사용 {dayState.totalIngredientUses}개");

            int shown = Mathf.Min(IngredientPreviewLimit, dayState.ingredientUsages.Count);
            for (int i = 0; i < shown; i++)
            {
                IngredientUsageRecord usage = dayState.ingredientUsages[i];

                if (usage == null)
                {
                    continue;
                }

                string name = GetIngredientDisplayName(usage);
                builder.AppendLine($"{name} x{usage.count} | {CurrencyUtil.ToDisplay(usage.totalCost)}");
            }

            if (dayState.ingredientUsages.Count > shown)
            {
                builder.AppendLine($"외 {dayState.ingredientUsages.Count - shown}개");
            }

            return builder.ToString();
        }

        private string BuildOrderLogText(DayState dayState)
        {
            StringBuilder builder = new StringBuilder();

            if (dayState.orderResults == null || dayState.orderResults.Count == 0)
            {
                builder.AppendLine(logTitle);
                builder.AppendLine("아직 기록된 주문이 없습니다.");
                return builder.ToString();
            }

            builder.AppendLine($"{logTitle} ({dayState.orderResults.Count}건)");

            for (int i = 0; i < dayState.orderResults.Count; i++)
            {
                OrderResultRecord result = dayState.orderResults[i];

                if (result == null)
                {
                    continue;
                }

                string customerName = string.IsNullOrWhiteSpace(result.customerName)
                    ? $"Customer {result.customerId}"
                    : result.customerName;
                string recipeName = string.IsNullOrWhiteSpace(result.recipeName)
                    ? $"Recipe {result.recipeId}"
                    : result.recipeName;

                builder.AppendLine($"#{result.sequence} {customerName} | {recipeName}");
                builder.AppendLine($"   {result.grade}  +{CurrencyUtil.ToDisplay(result.reward)}  원가 {CurrencyUtil.ToDisplay(result.ingredientCost)}");
                builder.AppendLine($"   요구 {result.requestedIngredientCount} / 제출 {result.submittedIngredientCount} | 오차 {result.totalErrors} | 힌트 {(result.hintUsed ? "O" : "X")}");

                AppendConsumedIngredients(builder, result);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void AppendConsumedIngredients(StringBuilder builder, OrderResultRecord result)
        {
            if (result.consumedIngredients == null || result.consumedIngredients.Count == 0)
            {
                builder.AppendLine("  소비 재료: 없음");
                return;
            }

            builder.Append("  소비 재료: ");

            for (int i = 0; i < result.consumedIngredients.Count; i++)
            {
                IngredientUsageRecord usage = result.consumedIngredients[i];

                if (usage == null)
                {
                    continue;
                }

                if (i > 0)
                {
                    builder.Append(", ");
                }

                string name = GetIngredientDisplayName(usage);
                builder.Append($"{name} x{usage.count}");
            }

            builder.AppendLine();
        }

        private static string GetIngredientDisplayName(IngredientUsageRecord usage)
        {
            if (usage == null)
            {
                return "재료";
            }

            return string.IsNullOrWhiteSpace(usage.ingredientName)
                ? $"Ingredient {usage.ingredientId}"
                : usage.ingredientName;
        }

        private void UpdateLogContentHeight()
        {
            if (logText == null || logContent == null || logScrollRect == null)
            {
                return;
            }

            logText.ForceMeshUpdate();
            float viewportHeight = logScrollRect.viewport != null ? logScrollRect.viewport.rect.height : 360f;
            float targetHeight = Mathf.Max(viewportHeight, logText.preferredHeight + 48f);
            logContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            logScrollRect.verticalNormalizedPosition = 1f;
        }

        private void BuildIfNeeded()
        {
            EnsureCanvasComponents();

            if (panel != null && builtLayoutVersion == BuiltLayoutVersion)
            {
                return;
            }

            ClearChildren();
            ClearReferences();

            backdrop = CreateImage("Backdrop", transform, backdropColor);
            SetStretch(backdrop.rectTransform);

            panel = CreateImage("Panel", transform, panelColor).rectTransform;
            SetCenter(panel, new Vector2(1360f, 820f), Vector2.zero);
            AddShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -12f));
            AddOutline(panel.gameObject, outlineColor, new Vector2(5f, -5f));

            Image headerBand = CreateImage("HeaderBand", panel, buttonColor);
            SetTopLeft(headerBand.rectTransform, 0f, 0f, 1360f, 118f);
            headerBand.raycastTarget = false;

            Image headerHighlight = CreateImage("HeaderHighlight", panel, buttonHighlightedColor);
            SetTopLeft(headerHighlight.rectTransform, 72f, 84f, 1216f, 10f);
            headerHighlight.raycastTarget = false;

            titleText = CreateText("Title", panel, "", titleFontSize, titleColor, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            SetTopLeft(titleText.rectTransform, 0f, 18f, 1360f, 72f);

            summaryText = CreateSectionText("Summary", 70f, 148f, 420f, 232f);
            gradeText = CreateSectionText("Grades", 520f, 148f, 310f, 232f);
            ingredientText = CreateSectionText("Ingredients", 860f, 148f, 430f, 232f);

            BuildLogScroll();

            closeButton = CreateButton("CloseButton", closeButtonLabel);
            SetTopLeft(closeButton.transform as RectTransform, 1124f, 736f, 166f, 58f);

            nextDayButton = CreateButton("NextDayButton", nextDayButtonLabel);
            SetTopLeft(nextDayButton.transform as RectTransform, 936f, 736f, 166f, 58f);

            builtLayoutVersion = BuiltLayoutVersion;
        }

        private void EnsureCanvasComponents()
        {
            RectTransform rectTransform = transform as RectTransform;

            if (rectTransform != null)
            {
                SetStretch(rectTransform);
            }

            canvas = canvas != null ? canvas : GetComponent<Canvas>();
            canvas = canvas != null ? canvas : gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = canvasSortingOrder;

            canvasScaler = canvasScaler != null ? canvasScaler : GetComponent<CanvasScaler>();
            canvasScaler = canvasScaler != null ? canvasScaler : gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.matchWidthOrHeight = matchWidthOrHeight;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            canvasGroup = canvasGroup != null ? canvasGroup : gameObject.AddComponent<CanvasGroup>();
        }

        private TMP_Text CreateSectionText(string objectName, float x, float y, float width, float height)
        {
            Image background = CreateImage(objectName, panel, sectionColor);
            SetTopLeft(background.rectTransform, x, y, width, height);
            AddShadow(background.gameObject, new Color(0f, 0f, 0f, 0.13f), new Vector2(0f, -5f));
            AddOutline(background.gameObject, outlineColor, new Vector2(2f, -2f));

            Image accent = CreateImage(objectName + "Accent", background.rectTransform, buttonHighlightedColor);
            SetTopLeft(accent.rectTransform, 0f, 0f, width, 8f);
            accent.raycastTarget = false;

            TMP_Text text = CreateText(objectName + "Text",
                                       background.rectTransform,
                                       "",
                                       sectionFontSize,
                                       bodyTextColor,
                                       TextAlignmentOptions.TopLeft);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = sectionFontSize;
            SetStretch(text.rectTransform, 24f, 22f, 24f, 18f);
            return text;
        }

        private void BuildLogScroll()
        {
            Image scrollBackground = CreateImage("OrderLogScroll", panel, scrollColor);
            RectTransform scrollRoot = scrollBackground.rectTransform;
            SetTopLeft(scrollRoot, 70f, 412f, 1220f, 300f);
            AddShadow(scrollBackground.gameObject, new Color(0f, 0f, 0f, 0.13f), new Vector2(0f, -5f));
            AddOutline(scrollBackground.gameObject, outlineColor, new Vector2(2f, -2f));

            logScrollRect = scrollBackground.gameObject.AddComponent<ScrollRect>();
            logScrollRect.horizontal = false;
            logScrollRect.vertical = true;
            logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            logScrollRect.scrollSensitivity = 28f;

            Image viewportImage = CreateImage("Viewport", scrollRoot, Color.white);
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            RectTransform viewport = viewportImage.rectTransform;
            SetStretch(viewport, 16f, 14f, 16f, 14f);
            Mask mask = viewportImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            logContent = contentObject.transform as RectTransform;
            logContent.anchorMin = new Vector2(0f, 1f);
            logContent.anchorMax = new Vector2(1f, 1f);
            logContent.pivot = new Vector2(0.5f, 1f);
            logContent.anchoredPosition = Vector2.zero;
            logContent.sizeDelta = new Vector2(0f, 430f);

            logText = CreateText("LogText",
                                 logContent,
                                 "",
                                 logFontSize,
                                 bodyTextColor,
                                 TextAlignmentOptions.TopLeft);
            logText.enableWordWrapping = true;
            logText.overflowMode = TextOverflowModes.Overflow;
            SetStretch(logText.rectTransform, 24f, 20f, 24f, 20f);

            logScrollRect.viewport = viewport;
            logScrollRect.content = logContent;
        }

        private Button CreateButton(string objectName, string label)
        {
            Image buttonImage = CreateImage(objectName, panel, buttonColor);
            AddShadow(buttonImage.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -5f));
            AddOutline(buttonImage.gameObject, outlineColor, new Vector2(2f, -2f));

            Button button = buttonImage.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightedColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHighlightedColor;
            colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.45f);
            button.colors = colors;

            TMP_Text labelText = CreateText("Label",
                                            buttonImage.rectTransform,
                                            label,
                                            buttonFontSize,
                                            sectionColor,
                                            TextAlignmentOptions.Center);
            labelText.fontStyle = FontStyles.Bold;
            SetStretch(labelText.rectTransform, 8f, 4f, 8f, 4f);
            return button;
        }

        private Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private TMP_Text CreateText(string objectName,
                                    Transform parent,
                                    string text,
                                    float fontSize,
                                    Color color,
                                    TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontSizeMin = Mathf.Max(12f, fontSize * 0.65f);
            tmp.fontSizeMax = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            TMP_FontAsset resolvedFont = ResolveFontAsset();
            if (resolvedFont != null)
            {
                tmp.font = resolvedFont;
            }

            return tmp;
        }

        private TMP_FontAsset ResolveFontAsset()
        {
            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>(KoreanFontResourcePath);
            }

            return fontAsset;
        }

        private void EnsureFontContainsCharacters(string text)
        {
            TMP_FontAsset resolvedFont = ResolveFontAsset();
            if (resolvedFont == null)
            {
                return;
            }

            string requiredCharacters = RequiredFontCharacters + text;
            if (resolvedFont.HasCharacters(requiredCharacters, out _))
            {
                return;
            }

            AtlasPopulationMode originalMode = resolvedFont.atlasPopulationMode;
            resolvedFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            try
            {
                if (!resolvedFont.TryAddCharacters(requiredCharacters, out string missingCharacters) &&
                    !string.IsNullOrEmpty(missingCharacters))
                {
                    Debug.LogWarning($"[DayResultLayerController] 결과창 폰트에 넣지 못한 문자가 있습니다: {missingCharacters}");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[DayResultLayerController] 결과창 폰트 보정 중 오류: {exception.Message}");
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                resolvedFont.atlasPopulationMode = originalMode;
                EditorUtility.SetDirty(resolvedFont);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        private void ApplyVisuals()
        {
            EnsureCanvasComponents();

            if (backdrop != null) backdrop.color = backdropColor;
            if (panel != null)
            {
                SetImageColor(panel, panelColor);
                SetOutlineColor(panel.gameObject, outlineColor);
            }

            SetChildImageColor("HeaderBand", buttonColor);
            SetChildImageColor("HeaderHighlight", buttonHighlightedColor);
            ApplySectionVisuals(summaryText);
            ApplySectionVisuals(gradeText);
            ApplySectionVisuals(ingredientText);

            if (logScrollRect != null)
            {
                SetImageColor(logScrollRect.transform as RectTransform, scrollColor);
                SetOutlineColor(logScrollRect.gameObject, outlineColor);
            }

            if (titleText != null)
            {
                ApplyTextStyle(titleText, titleFontSize, titleColor, TextAlignmentOptions.Center);
                titleText.fontStyle = FontStyles.Bold;
            }
            if (summaryText != null) ApplyTextStyle(summaryText, sectionFontSize, bodyTextColor, TextAlignmentOptions.TopLeft);
            if (gradeText != null) ApplyTextStyle(gradeText, sectionFontSize, bodyTextColor, TextAlignmentOptions.TopLeft);
            if (ingredientText != null) ApplyTextStyle(ingredientText, sectionFontSize, bodyTextColor, TextAlignmentOptions.TopLeft);
            if (logText != null) ApplyTextStyle(logText, logFontSize, bodyTextColor, TextAlignmentOptions.TopLeft);
            ApplyButtonVisuals(closeButton);
            ApplyButtonVisuals(nextDayButton);
        }

        private void ApplyTextStyle(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.65f);
            text.fontSizeMax = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;

            TMP_FontAsset resolvedFont = ResolveFontAsset();
            if (resolvedFont != null)
            {
                text.font = resolvedFont;
            }
        }

        private void ApplySectionVisuals(TMP_Text sectionText)
        {
            if (sectionText == null)
            {
                return;
            }

            RectTransform background = sectionText.transform.parent as RectTransform;
            if (background == null)
            {
                return;
            }

            SetImageColor(background, sectionColor);
            SetOutlineColor(background.gameObject, outlineColor);

            Transform accent = background.Find(background.name + "Accent");
            SetImageColor(accent as RectTransform, buttonHighlightedColor);
        }

        private void ApplyButtonVisuals(Button button)
        {
            if (button == null)
            {
                return;
            }

            SetImageColor(button.transform as RectTransform, buttonColor);
            SetOutlineColor(button.gameObject, outlineColor);

            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightedColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHighlightedColor;
            colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.45f);
            button.colors = colors;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                ApplyTextStyle(label, buttonFontSize, sectionColor, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
            }
        }

        private void HookButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveListener(BeginNextDay);
                nextDayButton.onClick.AddListener(BeginNextDay);
            }
        }

        private static void SetImageColor(RectTransform rectTransform, Color color)
        {
            Image image = rectTransform != null ? rectTransform.GetComponent<Image>() : null;

            if (image != null)
            {
                image.color = color;
            }
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            outline = outline != null ? outline : target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private void SetChildImageColor(string childName, Color color)
        {
            if (panel == null)
            {
                return;
            }

            SetImageColor(panel.Find(childName) as RectTransform, color);
        }

        private static void SetOutlineColor(GameObject target, Color color)
        {
            if (target == null)
            {
                return;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = color;
            }
        }

        private static void SetStretch(RectTransform rectTransform,
                                       float left = 0f,
                                       float top = 0f,
                                       float right = 0f,
                                       float bottom = 0f)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static void SetCenter(RectTransform rectTransform, Vector2 size, Vector2 position)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private static void SetTopLeft(RectTransform rectTransform,
                                       float x,
                                       float y,
                                       float width,
                                       float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(x, -y);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localScale = Vector3.one;
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ClearReferences()
        {
            backdrop = null;
            panel = null;
            titleText = null;
            summaryText = null;
            gradeText = null;
            ingredientText = null;
            logScrollRect = null;
            logContent = null;
            logText = null;
            closeButton = null;
            nextDayButton = null;
        }

        private static DayState CreatePreviewDayState()
        {
            DayState preview = DayState.CreateForDay(1);
            preview.customersServed = 8;
            preview.dailyRevenue = 1280;
            preview.dailyIngredientCost = 360;
            preview.perfectCount = 3;
            preview.goodCount = 3;
            preview.normalCount = 1;
            preview.badCount = 1;
            preview.ordersWithHint = 2;
            preview.totalIngredientUses = 42;

            preview.ingredientUsages.Add(new IngredientUsageRecord
            {
                ingredientId = 1,
                ingredientName = "하단번",
                count = 8,
                unitCost = 10,
                totalCost = 80
            });
            preview.ingredientUsages.Add(new IngredientUsageRecord
            {
                ingredientId = 3,
                ingredientName = "패티",
                count = 6,
                unitCost = 25,
                totalCost = 150
            });
            preview.ingredientUsages.Add(new IngredientUsageRecord
            {
                ingredientId = 4,
                ingredientName = "치즈",
                count = 5,
                unitCost = 12,
                totalCost = 60
            });

            OrderResultRecord firstOrder = new OrderResultRecord
            {
                sequence = 1,
                customerId = 101,
                customerName = "늑대",
                orderId = 201,
                recipeId = 301,
                recipeName = "햄버거",
                grade = Grade.Perfect,
                reward = 180,
                ingredientCost = 45,
                hintUsed = false,
                requestedIngredientCount = 4,
                submittedIngredientCount = 4,
                burgerCompleted = true,
                ingredientErrors = 0,
                cookStateErrors = 0,
                totalErrors = 0
            };
            firstOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "하단번", count = 1 });
            firstOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "패티", count = 1 });
            firstOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "상단번", count = 1 });
            preview.orderResults.Add(firstOrder);

            OrderResultRecord secondOrder = new OrderResultRecord
            {
                sequence = 2,
                customerId = 102,
                customerName = "기린",
                orderId = 202,
                recipeId = 302,
                recipeName = "치즈버거",
                grade = Grade.Good,
                reward = 120,
                ingredientCost = 57,
                hintUsed = true,
                requestedIngredientCount = 5,
                submittedIngredientCount = 5,
                burgerCompleted = true,
                ingredientErrors = 1,
                cookStateErrors = 0,
                totalErrors = 1
            };
            secondOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "하단번", count = 1 });
            secondOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "패티", count = 1 });
            secondOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "치즈", count = 1 });
            secondOrder.consumedIngredients.Add(new IngredientUsageRecord { ingredientName = "상단번", count = 1 });
            preview.orderResults.Add(secondOrder);

            preview.EnsureInitialized(preview.dayNumber);
            return preview;
        }
    }
}
