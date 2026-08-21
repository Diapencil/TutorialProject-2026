using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SheepSheepBurger.Shop
{
    [CreateAssetMenu(menuName = "SheepSheepBurger/Shop/Scene Design Preset", fileName = "ShopSceneDesignPreset")]
    public sealed class ShopSceneDesignPreset : ScriptableObject
    {
        [TextArea(3, 6)]
        [Tooltip("디자인 담당자가 볼 메모입니다. 값이 클수록 앞쪽에 보이는 항목은 '앞뒤 순서' 섹션에서 조정합니다.")]
        public string memo = "상점 씬 디자인용 프리셋입니다. 앞뒤 순서는 값이 클수록 화면 앞쪽에 보입니다.";

        [Header("Canvas / Camera")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Range(0f, 1f)]
        [Tooltip("0이면 너비 기준, 1이면 높이 기준으로 UI가 스케일됩니다.")]
        public float matchWidthOrHeight = 0.5f;

        [Tooltip("Canvas 자체의 정렬 순서입니다. 값이 클수록 다른 Canvas보다 앞에 보입니다.")]
        public int canvasSortingOrder = 0;

        [Tooltip("Screen Space - Camera Canvas가 카메라에서 떨어진 거리입니다.")]
        public float canvasPlaneDistance = 10f;

        [Tooltip("UI 카메라의 Orthographic Size입니다.")]
        public float cameraOrthographicSize = 5.4f;

        public Color cameraBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);

        [Header("이미지 리소스")]
        [Tooltip("상점 전체 배경 이미지입니다. Canvas 전체에 꽉 차게 깔립니다.")]
        public Sprite backgroundSprite;

        [Tooltip("버튼 기본 상태 이미지입니다. 클릭을 떼면 이 이미지로 돌아옵니다.")]
        public Sprite buttonNormalSprite;

        [Tooltip("버튼 클릭 중 이미지입니다. 마우스/터치가 눌려 있는 동안 표시됩니다.")]
        public Sprite buttonPressedSprite;

        [Tooltip("상점 항목 카드 프레임 이미지입니다.")]
        public Sprite slotFrameSprite;

        [Header("적용 옵션")]
        [InspectorName("RectTransform 위치/크기 자동 적용")]
        [Tooltip("끄면 UI 위치/크기/앵커는 씬의 RectTransform 값을 그대로 사용합니다. Unity에서 직접 드래그해서 배치하려면 꺼두세요.")]
        public bool applyRectTransformLayout = false;

        [Header("앞뒤 순서")]
        [Tooltip("값이 클수록 나중에 그려져 화면 앞쪽에 보입니다.")]
        public LayerOrderSettings layerOrder = LayerOrderSettings.Default;

        [Header("위치 / 크기")]
        public LayoutSettings layout = LayoutSettings.Default;

        [Header("슬롯 내부")]
        public SlotSettings slot = SlotSettings.Default;

        [Header("글자")]
        public TextSettings text = TextSettings.Default;

        [Header("색")]
        public ColorSettings colors = ColorSettings.Default;

        [Serializable]
        public struct LayerOrderSettings
        {
            [Tooltip("BackgroundImage 앞뒤 순서입니다. 값이 작을수록 뒤에 보입니다.")]
            public int background;

            [Tooltip("GridPanel 앞뒤 순서입니다. 값이 클수록 앞에 보입니다.")]
            public int gridPanel;

            [Tooltip("DebtPanel 앞뒤 순서입니다. 값이 클수록 앞에 보입니다.")]
            public int debtPanel;

            [Tooltip("SideBar 앞뒤 순서입니다. 값이 클수록 앞에 보입니다.")]
            public int sideBar;

            [Tooltip("TopHud 앞뒤 순서입니다. 값이 클수록 앞에 보입니다.")]
            public int topHud;

            [Tooltip("MessageBar 앞뒤 순서입니다. 값이 클수록 앞에 보입니다.")]
            public int messageBar;

            [Tooltip("GridPanel Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float gridPanelZ;

            [Tooltip("DebtPanel Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float debtPanelZ;

            [Tooltip("SideBar Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float sideBarZ;

            [Tooltip("TopHud Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float topHudZ;

            [Tooltip("MessageBar Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float messageBarZ;

            [Tooltip("BackgroundImage Z 위치입니다. 값이 커질수록 카메라/앞쪽으로 이동합니다.")]
            public float backgroundZ;

            public static LayerOrderSettings Default => new LayerOrderSettings
            {
                background = -100,
                gridPanel = 0,
                debtPanel = 5,
                sideBar = 10,
                topHud = 20,
                messageBar = 30,
                backgroundZ = 0f,
                gridPanelZ = 0f,
                debtPanelZ = 0f,
                sideBarZ = 0f,
                topHudZ = 0f,
                messageBarZ = 0f
            };
        }

        [Serializable]
        public struct LayoutSettings
        {
            [HideInInspector] public int slotLayoutVersion;
            [Min(0f)] public float topHudHeight;
            [Min(0f)] public float sideBarWidth;
            [Min(0f)] public float messageAreaHeight;
            [Min(0f)] public float sideBarPadding;
            [Min(0f)] public float sideBarTopPadding;
            [Min(0f)] public float sideBarBottomPadding;
            [Min(0f)] public float sideBarSpacing;
            [Min(0f)] public float tabButtonHeight;
            [Min(0f)] public float hudHorizontalPadding;
            [Min(0f)] public float messageHorizontalPadding;
            [Tooltip("슬롯을 몇 줄로 배치할지 정합니다. 1이면 가로 슬라이드 한 줄입니다.")]
            [Min(1)] public int slotRowCount;

            [Tooltip("슬롯이 보이는 마스크 영역의 왼쪽 여백입니다.")]
            [Min(0f)] public float slotViewportPaddingLeft;

            [Tooltip("슬롯이 보이는 마스크 영역의 오른쪽 여백입니다.")]
            [Min(0f)] public float slotViewportPaddingRight;

            [Tooltip("슬롯이 보이는 마스크 영역의 위쪽 여백입니다. 값이 커질수록 슬롯 영역이 아래로 내려옵니다.")]
            [Min(0f)] public float slotViewportPaddingTop;

            [Tooltip("슬롯이 보이는 마스크 영역의 아래쪽 여백입니다. 값이 커질수록 슬롯 영역이 위로 올라옵니다.")]
            [Min(0f)] public float slotViewportPaddingBottom;

            [Tooltip("첫 슬롯이 시작되는 안쪽 왼쪽 여백입니다.")]
            [Min(0f)] public float slotContentPaddingLeft;

            [Tooltip("마지막 슬롯 뒤의 안쪽 오른쪽 여백입니다.")]
            [Min(0f)] public float slotContentPaddingRight;

            [Tooltip("슬롯 하나의 가로/세로 크기입니다.")]
            public Vector2 slotCellSize;

            [Tooltip("슬롯 사이의 가로/세로 간격입니다.")]
            public Vector2 slotSpacing;

            [Tooltip("슬라이드를 끝까지 밀었을 때 살짝 튕기는 정도입니다.")]
            [Min(0f)] public float slotScrollElasticity;

            [Tooltip("마우스 휠/트랙패드 스크롤 민감도입니다.")]
            [Min(0f)] public float slotScrollSensitivity;
            public Vector2 debtInputSize;
            public Vector2 debtButtonSize;
            [Min(0f)] public float debtTextHeight;
            [Min(0f)] public float debtElementSpacing;
            [Min(0f)] public float inputHorizontalPadding;

            public static LayoutSettings Default => new LayoutSettings
            {
                slotLayoutVersion = 1,
                topHudHeight = 96f,
                sideBarWidth = 360f,
                messageAreaHeight = 80f,
                sideBarPadding = 34f,
                sideBarTopPadding = 170f,
                sideBarBottomPadding = 24f,
                sideBarSpacing = 18f,
                tabButtonHeight = 132f,
                hudHorizontalPadding = 20f,
                messageHorizontalPadding = 0f,
                slotRowCount = 1,
                slotViewportPaddingLeft = 0f,
                slotViewportPaddingRight = 0f,
                slotViewportPaddingTop = 48f,
                slotViewportPaddingBottom = 296f,
                slotContentPaddingLeft = 72f,
                slotContentPaddingRight = 72f,
                slotCellSize = new Vector2(388f, 560f),
                slotSpacing = new Vector2(52f, 0f),
                slotScrollElasticity = 0.08f,
                slotScrollSensitivity = 35f,
                debtInputSize = new Vector2(520f, 80f),
                debtButtonSize = new Vector2(320f, 80f),
                debtTextHeight = 120f,
                debtElementSpacing = 40f,
                inputHorizontalPadding = 28f
            };
        }

        [Serializable]
        public struct SlotSettings
        {
            [Range(0f, 1f)] public float iconBottomAnchor;
            [Range(0f, 1f)] public float nameBottomAnchor;
            [Range(0f, 1f)] public float costBottomAnchor;
            [Min(0f)] public float innerPadding;

            public static SlotSettings Default => new SlotSettings
            {
                iconBottomAnchor = 0.53f,
                nameBottomAnchor = 0.38f,
                costBottomAnchor = 0.24f,
                innerPadding = 28f
            };
        }

        [Serializable]
        public struct TextSettings
        {
            [Min(1f)] public float hudFontSize;
            [Min(1f)] public float tabFontSize;
            [Min(1f)] public float slotNameFontSize;
            [Min(1f)] public float slotCostFontSize;
            [Min(1f)] public float messageFontSize;
            [Min(1f)] public float debtFontSize;
            [Min(1f)] public float inputFontSize;

            public static TextSettings Default => new TextSettings
            {
                hudFontSize = 40f,
                tabFontSize = 30f,
                slotNameFontSize = 26f,
                slotCostFontSize = 24f,
                messageFontSize = 30f,
                debtFontSize = 60f,
                inputFontSize = 32f
            };
        }

        [Serializable]
        public struct ColorSettings
        {
            public Color topHudBackground;
            public Color sideBarBackground;
            public Color gridPanelBackground;
            public Color debtPanelBackground;
            public Color messageBarBackground;
            public Color tabNormal;
            public Color tabHighlighted;
            public Color tabPressed;
            public Color tabSelected;
            public Color actionButtonBackground;
            public Color inputBackground;
            public Color slotBackground;
            public Color slotSoldOutOverlay;
            public Color slotLockedOverlay;
            public Color mainText;
            public Color dDayText;
            public Color messageText;
            public Color placeholderText;

            public static ColorSettings Default => new ColorSettings
            {
                topHudBackground = new Color(1f, 1f, 1f, 0f),
                sideBarBackground = new Color(1f, 1f, 1f, 0f),
                gridPanelBackground = new Color(1f, 1f, 1f, 0f),
                debtPanelBackground = new Color(1f, 1f, 1f, 0f),
                messageBarBackground = new Color(1f, 1f, 1f, 0f),
                tabNormal = Color.white,
                tabHighlighted = new Color(0.92f, 0.92f, 0.92f, 1f),
                tabPressed = new Color(0.78f, 0.78f, 0.78f, 1f),
                tabSelected = new Color(0.72f, 0.72f, 0.72f, 1f),
                actionButtonBackground = Color.white,
                inputBackground = Color.white,
                slotBackground = Color.white,
                slotSoldOutOverlay = new Color(0f, 0f, 0f, 0.6f),
                slotLockedOverlay = new Color(0.5f, 0.5f, 0.5f, 0.8f),
                mainText = new Color(0.19f, 0.13f, 0.08f, 1f),
                dDayText = new Color(0.64f, 0.12f, 0.08f, 1f),
                messageText = new Color(0.19f, 0.13f, 0.08f, 1f),
                placeholderText = Color.grey
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.delayCall -= ApplyToOpenScenes;
            EditorApplication.delayCall += ApplyToOpenScenes;
        }

        public void ApplyToOpenScenes()
        {
            EditorApplication.delayCall -= ApplyToOpenScenes;

            if (this == null)
            {
                return;
            }

            ShopSceneDesignController[] controllers =
                FindObjectsByType<ShopSceneDesignController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && controllers[i].Preset == this)
                {
                    controllers[i].ApplyDesign();
                }
            }
        }
#endif
    }
}
