using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SheepSheepBurger.Settings
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SettingsLayerDesignPresenter : MonoBehaviour
    {
        [Header("디자인 프리셋")]
        [SerializeField] private SettingsLayerDesignPreset preset;

        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("패널")]
        [SerializeField] private Image backdrop;
        [SerializeField] private RectTransform settingsButtonRoot;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Image settingsButtonBackground;
        [SerializeField] private Outline settingsButtonOutline;
        [SerializeField] private TMP_Text settingsButtonText;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Outline panelOutline;
        [SerializeField] private RectTransform innerBorder;
        [SerializeField] private Image innerBorderImage;
        [SerializeField] private Outline innerBorderOutline;
        [SerializeField] private RectTransform titleBanner;
        [SerializeField] private Image titleBackground;
        [SerializeField] private Outline titleOutline;
        [SerializeField] private TMP_Text titleText;

        [Header("BGM 행")]
        [SerializeField] private RectTransform bgmIcon;
        [SerializeField] private Image bgmIconImage;
        [SerializeField] private Outline bgmIconOutline;
        [SerializeField] private RectTransform bgmSliderRoot;
        [SerializeField] private Image bgmSliderTrack;
        [SerializeField] private RectTransform bgmSliderTrackRect;
        [SerializeField] private Image bgmSliderFill;
        [SerializeField] private Image bgmSliderHandle;
        [SerializeField] private Outline bgmSliderHandleOutline;
        [SerializeField] private RectTransform bgmValueBox;
        [SerializeField] private Image bgmValueBoxBackground;
        [SerializeField] private Outline bgmValueBoxOutline;
        [SerializeField] private TMP_Text bgmValueText;

        [Header("효과음 행")]
        [SerializeField] private RectTransform sfxIcon;
        [SerializeField] private Image sfxIconImage;
        [SerializeField] private Outline sfxIconOutline;
        [SerializeField] private RectTransform sfxSliderRoot;
        [SerializeField] private Image sfxSliderTrack;
        [SerializeField] private RectTransform sfxSliderTrackRect;
        [SerializeField] private Image sfxSliderFill;
        [SerializeField] private Image sfxSliderHandle;
        [SerializeField] private Outline sfxSliderHandleOutline;
        [SerializeField] private RectTransform sfxValueBox;
        [SerializeField] private Image sfxValueBoxBackground;
        [SerializeField] private Outline sfxValueBoxOutline;
        [SerializeField] private TMP_Text sfxValueText;

        public SettingsLayerDesignPreset Preset => preset;

        public void Bind(SettingsLayerDesignPreset designPreset,
                         Canvas boundCanvas,
                         CanvasScaler boundCanvasScaler,
                         Image boundBackdrop,
                         RectTransform boundSettingsButtonRoot,
                         Button boundSettingsButton,
                         Image boundSettingsButtonBackground,
                         Outline boundSettingsButtonOutline,
                         TMP_Text boundSettingsButtonText,
                         RectTransform boundPanel,
                         Image boundPanelBackground,
                         Outline boundPanelOutline,
                         RectTransform boundInnerBorder,
                         Image boundInnerBorderImage,
                         Outline boundInnerBorderOutline,
                         RectTransform boundTitleBanner,
                         Image boundTitleBackground,
                         Outline boundTitleOutline,
                         TMP_Text boundTitleText,
                         RectTransform boundBgmIcon,
                         Image boundBgmIconImage,
                         Outline boundBgmIconOutline,
                         RectTransform boundBgmSliderRoot,
                         Image boundBgmSliderTrack,
                         RectTransform boundBgmSliderTrackRect,
                         Image boundBgmSliderFill,
                         Image boundBgmSliderHandle,
                         Outline boundBgmSliderHandleOutline,
                         RectTransform boundBgmValueBox,
                         Image boundBgmValueBoxBackground,
                         Outline boundBgmValueBoxOutline,
                         TMP_Text boundBgmValueText,
                         RectTransform boundSfxIcon,
                         Image boundSfxIconImage,
                         Outline boundSfxIconOutline,
                         RectTransform boundSfxSliderRoot,
                         Image boundSfxSliderTrack,
                         RectTransform boundSfxSliderTrackRect,
                         Image boundSfxSliderFill,
                         Image boundSfxSliderHandle,
                         Outline boundSfxSliderHandleOutline,
                         RectTransform boundSfxValueBox,
                         Image boundSfxValueBoxBackground,
                         Outline boundSfxValueBoxOutline,
                         TMP_Text boundSfxValueText)
        {
            preset = designPreset;
            canvas = boundCanvas;
            canvasScaler = boundCanvasScaler;
            backdrop = boundBackdrop;
            settingsButtonRoot = boundSettingsButtonRoot;
            settingsButton = boundSettingsButton;
            settingsButtonBackground = boundSettingsButtonBackground;
            settingsButtonOutline = boundSettingsButtonOutline;
            settingsButtonText = boundSettingsButtonText;
            panel = boundPanel;
            panelBackground = boundPanelBackground;
            panelOutline = boundPanelOutline;
            innerBorder = boundInnerBorder;
            innerBorderImage = boundInnerBorderImage;
            innerBorderOutline = boundInnerBorderOutline;
            titleBanner = boundTitleBanner;
            titleBackground = boundTitleBackground;
            titleOutline = boundTitleOutline;
            titleText = boundTitleText;
            bgmIcon = boundBgmIcon;
            bgmIconImage = boundBgmIconImage;
            bgmIconOutline = boundBgmIconOutline;
            bgmSliderRoot = boundBgmSliderRoot;
            bgmSliderTrack = boundBgmSliderTrack;
            bgmSliderTrackRect = boundBgmSliderTrackRect;
            bgmSliderFill = boundBgmSliderFill;
            bgmSliderHandle = boundBgmSliderHandle;
            bgmSliderHandleOutline = boundBgmSliderHandleOutline;
            bgmValueBox = boundBgmValueBox;
            bgmValueBoxBackground = boundBgmValueBoxBackground;
            bgmValueBoxOutline = boundBgmValueBoxOutline;
            bgmValueText = boundBgmValueText;
            sfxIcon = boundSfxIcon;
            sfxIconImage = boundSfxIconImage;
            sfxIconOutline = boundSfxIconOutline;
            sfxSliderRoot = boundSfxSliderRoot;
            sfxSliderTrack = boundSfxSliderTrack;
            sfxSliderTrackRect = boundSfxSliderTrackRect;
            sfxSliderFill = boundSfxSliderFill;
            sfxSliderHandle = boundSfxSliderHandle;
            sfxSliderHandleOutline = boundSfxSliderHandleOutline;
            sfxValueBox = boundSfxValueBox;
            sfxValueBoxBackground = boundSfxValueBoxBackground;
            sfxValueBoxOutline = boundSfxValueBoxOutline;
            sfxValueText = boundSfxValueText;
        }

        [ContextMenu("Apply Settings Layer Design")]
        public void ApplyDesign()
        {
            if (preset == null)
            {
                return;
            }

            ApplyCanvas();

            if (preset.applyRectTransformLayout)
            {
                ApplyLayout();
            }

            ApplyText();
            ApplyColors();
            MarkDirty();
        }

        private void Awake()
        {
            if (ShouldApplyDesignOnAwake())
            {
                ApplyDesign();
            }
            else
            {
                ApplyCanvas();
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplyDesignDelayed;

            if (ShouldApplyDesignOnValidate())
            {
                EditorApplication.delayCall += ApplyDesignDelayed;
            }
#else
            if (ShouldApplyDesignOnValidate())
            {
                ApplyDesign();
            }
#endif
        }

#if UNITY_EDITOR
        private void ApplyDesignDelayed()
        {
            EditorApplication.delayCall -= ApplyDesignDelayed;

            if (this == null)
            {
                return;
            }

            ApplyDesign();
        }
#endif

        private void ApplyCanvas()
        {
            if (canvas != null)
            {
                Camera mainCamera = Camera.main;
                canvas.renderMode = mainCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 100f;
                canvas.overrideSorting = true;
                canvas.sortingOrder = preset.canvasSortingOrder;
            }

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = preset.referenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = preset.matchWidthOrHeight;
            }
        }

        private bool ShouldApplyDesignOnAwake()
        {
            return preset != null && preset.applyDesignOnAwake;
        }

        private bool ShouldApplyDesignOnValidate()
        {
            return preset != null && preset.applyDesignOnValidate;
        }

        private void ApplyLayout()
        {
            SettingsLayerDesignPreset.LayoutSettings layout = preset.layout;

            if (backdrop != null)
            {
                SetStretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            }

            SetTopRight(settingsButtonRoot, layout.settingsButtonMargin, layout.settingsButtonSize);
            SetCenter(panel, layout.panelAnchoredPosition, layout.panelSize);

            if (innerBorder != null)
            {
                SetStretch(innerBorder, layout.innerBorderInset, -layout.innerBorderInset);
            }

            SetCenter(titleBanner, layout.titleAnchoredPosition, layout.titleSize);

            ApplyRowLayout(layout.rowStartY,
                           layout,
                           bgmIcon,
                           bgmSliderRoot,
                           bgmSliderTrackRect,
                           bgmValueBox);

            ApplyRowLayout(layout.rowStartY - layout.rowSpacing,
                           layout,
                           sfxIcon,
                           sfxSliderRoot,
                           sfxSliderTrackRect,
                           sfxValueBox);
        }

        private static void ApplyRowLayout(float rowY,
                                           SettingsLayerDesignPreset.LayoutSettings layout,
                                           RectTransform icon,
                                           RectTransform sliderRoot,
                                           RectTransform sliderTrack,
                                           RectTransform valueBox)
        {
            SetCenter(icon, new Vector2(layout.iconX, rowY), Vector2.one * layout.iconSize);
            SetCenter(sliderRoot, new Vector2(layout.sliderX, rowY), layout.sliderSize);
            SetCenter(sliderTrack, Vector2.zero, new Vector2(layout.sliderSize.x, layout.sliderTrackHeight));
            SetCenter(valueBox, new Vector2(layout.valueBoxX, rowY), layout.valueBoxSize);
        }

        private void ApplyText()
        {
            TMP_FontAsset fontAsset = preset.fontAsset;
            SettingsLayerDesignPreset.TextSettings textSettings = preset.text;
            SettingsLayerDesignPreset.ColorSettings colorSettings = preset.colors;

            ApplyText(titleText, fontAsset, textSettings.titleFontSize, colorSettings.titleText);
            ApplyText(settingsButtonText, fontAsset, textSettings.settingsButtonFontSize, colorSettings.settingsButtonText);
            ApplyText(bgmValueText, fontAsset, textSettings.valueFontSize, colorSettings.valueText);
            ApplyText(sfxValueText, fontAsset, textSettings.valueFontSize, colorSettings.valueText);
        }

        private void ApplyColors()
        {
            SettingsLayerDesignPreset.ColorSettings colors = preset.colors;

            SetImage(backdrop, colors.backdrop, true);
            SetImage(settingsButtonBackground, colors.settingsButtonBackground, true);
            SetOutline(settingsButtonOutline, colors.settingsButtonOutline, new Vector2(4f, -4f));
            ApplyButtonColors(settingsButton,
                              colors.settingsButtonBackground,
                              colors.settingsButtonHighlighted,
                              colors.settingsButtonPressed);
            SetImage(panelBackground, colors.panelBackground, true);
            SetOutline(panelOutline, colors.panelOutline, new Vector2(6f, -6f));
            SetImage(innerBorderImage, new Color(colors.panelBackground.r,
                                                 colors.panelBackground.g,
                                                 colors.panelBackground.b,
                                                 0.01f),
                     false);
            SetOutline(innerBorderOutline, colors.innerOutline, new Vector2(4f, -4f));
            SetImage(titleBackground, colors.titleBackground, true);
            SetOutline(titleOutline, colors.titleOutline, new Vector2(5f, -5f));

            ApplyRowColors(bgmIconImage,
                           bgmIconOutline,
                           bgmSliderTrack,
                           bgmSliderFill,
                           bgmSliderHandle,
                           bgmSliderHandleOutline,
                           bgmValueBoxBackground,
                           bgmValueBoxOutline,
                           colors);

            ApplyRowColors(sfxIconImage,
                           sfxIconOutline,
                           sfxSliderTrack,
                           sfxSliderFill,
                           sfxSliderHandle,
                           sfxSliderHandleOutline,
                           sfxValueBoxBackground,
                           sfxValueBoxOutline,
                           colors);
        }

        private static void ApplyRowColors(Image iconImage,
                                           Outline iconOutline,
                                           Image sliderTrack,
                                           Image sliderFill,
                                           Image sliderHandle,
                                           Outline sliderHandleOutline,
                                           Image valueBoxBackground,
                                           Outline valueBoxOutline,
                                           SettingsLayerDesignPreset.ColorSettings colors)
        {
            SetImage(iconImage, colors.iconBackground, true);
            SetOutline(iconOutline, colors.iconOutline, new Vector2(4f, -4f));
            SetImage(sliderTrack, colors.sliderTrack, true);
            SetImage(sliderFill, colors.sliderFill, true);
            SetImage(sliderHandle, colors.sliderHandle, true);
            SetOutline(sliderHandleOutline, colors.sliderHandleOutline, new Vector2(4f, -4f));
            SetImage(valueBoxBackground, colors.valueBoxBackground, true);
            SetOutline(valueBoxOutline, colors.valueBoxOutline, new Vector2(4f, -4f));
        }

        private static void ApplyText(TMP_Text text,
                                      TMP_FontAsset fontAsset,
                                      float fontSize,
                                      Color color)
        {
            if (text == null)
            {
                return;
            }

            if (fontAsset != null)
            {
                text.font = fontAsset;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static void SetImage(Image image, Color color, bool raycastTarget)
        {
            if (image == null)
            {
                return;
            }

            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        private static void SetOutline(Outline outline, Color color, Vector2 distance)
        {
            if (outline == null)
            {
                return;
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetTopRight(RectTransform rect, Vector2 margin, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-margin.x, -margin.y);
            rect.sizeDelta = size;
        }

        private static void SetCenter(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ApplyButtonColors(Button button, Color normal, Color highlighted, Color pressed)
        {
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            EditorUtility.SetDirty(this);

            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}
