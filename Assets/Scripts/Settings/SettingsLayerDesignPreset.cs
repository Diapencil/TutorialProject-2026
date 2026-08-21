using System;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SheepSheepBurger.Settings
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Settings/Layer Design Preset", fileName = "SettingsLayerDesignPreset")]
    public sealed class SettingsLayerDesignPreset : ScriptableObject
    {
        [TextArea(3, 6)]
        [Tooltip("설정 레이어 디자인 담당자가 볼 메모입니다.")]
        public string memo =
            "다른 씬 위에 얹는 설정 레이어 프리셋입니다. 위치/크기는 프리팹의 RectTransform을 직접 드래그해서 수정하는 방식을 기본으로 둡니다.";

        [Header("Canvas")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Range(0f, 1f)]
        [Tooltip("0이면 너비 기준, 1이면 높이 기준으로 UI가 스케일됩니다.")]
        public float matchWidthOrHeight = 0.5f;

        [Tooltip("값이 클수록 다른 Canvas보다 앞쪽에 보입니다.")]
        public int canvasSortingOrder = 500;

        [Header("폰트")]
        public TMP_FontAsset fontAsset;

        [Header("자동 적용")]
        [InspectorName("게임 시작 때 프리셋 자동 적용")]
        [Tooltip("끄면 플레이 시작 때 프리셋 색/폰트/위치로 덮어쓰지 않습니다. 직접 수정한 Image 색을 유지하려면 꺼두세요.")]
        public bool applyDesignOnAwake = false;

        [InspectorName("인스펙터 변경 때 프리셋 자동 적용")]
        [Tooltip("끄면 프리팹/씬의 Inspector에서 직접 바꾼 색과 위치를 자동으로 덮어쓰지 않습니다.")]
        public bool applyDesignOnValidate = false;

        [InspectorName("프리셋 수정 시 열린 레이어 자동 적용")]
        [Tooltip("켜면 이 프리셋 값을 바꿀 때 열린 씬의 설정 레이어에 즉시 반영합니다.")]
        public bool applyToOpenLayersOnValidate = false;

        [Header("적용 옵션")]
        [InspectorName("RectTransform 위치/크기 자동 적용")]
        [Tooltip("끄면 프리팹/씬에서 손으로 수정한 UI 위치와 크기를 유지합니다.")]
        public bool applyRectTransformLayout = false;

        [Header("위치 / 크기")]
        public LayoutSettings layout = LayoutSettings.Default;

        [Header("글자")]
        public TextSettings text = TextSettings.Default;

        [Header("색")]
        public ColorSettings colors = ColorSettings.Default;

        [Serializable]
        public struct LayoutSettings
        {
            public Vector2 panelSize;
            public Vector2 panelAnchoredPosition;
            public Vector2 settingsButtonSize;
            public Vector2 settingsButtonMargin;
            public Vector2 innerBorderInset;
            public Vector2 titleSize;
            public Vector2 titleAnchoredPosition;
            public float rowStartY;
            public float rowSpacing;
            public float iconX;
            public float sliderX;
            public float valueBoxX;
            public float iconSize;
            public Vector2 sliderSize;
            public float sliderTrackHeight;
            public float sliderHandleSize;
            public Vector2 valueBoxSize;

            public static LayoutSettings Default => new LayoutSettings
            {
                panelSize = new Vector2(1240f, 720f),
                panelAnchoredPosition = Vector2.zero,
                settingsButtonSize = new Vector2(118f, 118f),
                settingsButtonMargin = new Vector2(48f, 44f),
                innerBorderInset = new Vector2(48f, 48f),
                titleSize = new Vector2(520f, 140f),
                titleAnchoredPosition = new Vector2(0f, 330f),
                rowStartY = 90f,
                rowSpacing = 180f,
                iconX = -420f,
                sliderX = 60f,
                valueBoxX = 500f,
                iconSize = 118f,
                sliderSize = new Vector2(650f, 96f),
                sliderTrackHeight = 20f,
                sliderHandleSize = 82f,
                valueBoxSize = new Vector2(150f, 86f)
            };
        }

        [Serializable]
        public struct TextSettings
        {
            [Min(1f)] public float titleFontSize;
            [Min(1f)] public float settingsButtonFontSize;
            [Min(1f)] public float valueFontSize;

            public static TextSettings Default => new TextSettings
            {
                titleFontSize = 70f,
                settingsButtonFontSize = 44f,
                valueFontSize = 46f
            };
        }

        [Serializable]
        public struct ColorSettings
        {
            public Color backdrop;
            public Color panelBackground;
            public Color panelOutline;
            public Color innerOutline;
            public Color titleBackground;
            public Color titleOutline;
            public Color titleText;
            public Color settingsButtonBackground;
            public Color settingsButtonHighlighted;
            public Color settingsButtonPressed;
            public Color settingsButtonOutline;
            public Color settingsButtonText;
            public Color iconBackground;
            public Color iconOutline;
            public Color sliderTrack;
            public Color sliderFill;
            public Color sliderHandle;
            public Color sliderHandleOutline;
            public Color valueBoxBackground;
            public Color valueBoxOutline;
            public Color valueText;

            public static ColorSettings Default => new ColorSettings
            {
                backdrop = new Color(0.06f, 0.10f, 0.08f, 0.48f),
                panelBackground = new Color(0.59f, 0.73f, 0.60f, 1f),
                panelOutline = new Color(0.12f, 0.30f, 0.21f, 1f),
                innerOutline = new Color(0.24f, 0.45f, 0.32f, 1f),
                titleBackground = new Color(0.48f, 0.64f, 0.49f, 1f),
                titleOutline = new Color(0.10f, 0.26f, 0.20f, 1f),
                titleText = new Color(0.07f, 0.14f, 0.10f, 1f),
                settingsButtonBackground = new Color(0.46f, 0.62f, 0.46f, 1f),
                settingsButtonHighlighted = new Color(0.55f, 0.70f, 0.55f, 1f),
                settingsButtonPressed = new Color(0.34f, 0.51f, 0.36f, 1f),
                settingsButtonOutline = new Color(0.08f, 0.22f, 0.15f, 1f),
                settingsButtonText = new Color(0.07f, 0.14f, 0.10f, 1f),
                iconBackground = new Color(0.72f, 0.83f, 0.71f, 1f),
                iconOutline = new Color(0.13f, 0.30f, 0.20f, 1f),
                sliderTrack = new Color(0.31f, 0.45f, 0.31f, 1f),
                sliderFill = new Color(0.16f, 0.36f, 0.23f, 1f),
                sliderHandle = new Color(0.20f, 0.44f, 0.27f, 1f),
                sliderHandleOutline = new Color(0.05f, 0.16f, 0.10f, 1f),
                valueBoxBackground = new Color(0.76f, 0.86f, 0.75f, 1f),
                valueBoxOutline = new Color(0.12f, 0.30f, 0.21f, 1f),
                valueText = new Color(0.08f, 0.15f, 0.11f, 1f)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.delayCall -= ApplyToOpenLayers;

            if (applyToOpenLayersOnValidate)
            {
                EditorApplication.delayCall += ApplyToOpenLayers;
            }
        }

        public void ApplyToOpenLayers()
        {
            EditorApplication.delayCall -= ApplyToOpenLayers;

            if (this == null)
            {
                return;
            }

            SettingsLayerDesignPresenter[] presenters =
                FindObjectsByType<SettingsLayerDesignPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < presenters.Length; i++)
            {
                if (presenters[i] != null && presenters[i].Preset == this)
                {
                    presenters[i].ApplyDesign();
                }
            }
        }
#endif
    }
}
