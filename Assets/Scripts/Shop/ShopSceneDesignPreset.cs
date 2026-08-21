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

            public static LayerOrderSettings Default => new LayerOrderSettings
            {
                gridPanel = 0,
                debtPanel = 5,
                sideBar = 10,
                topHud = 20,
                messageBar = 30,
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
            [Min(0f)] public float topHudHeight;
            [Min(0f)] public float sideBarWidth;
            [Min(0f)] public float messageAreaHeight;
            [Min(0f)] public float sideBarPadding;
            [Min(0f)] public float sideBarSpacing;
            [Min(0f)] public float tabButtonHeight;
            [Min(0f)] public float hudHorizontalPadding;
            [Min(0f)] public float messageHorizontalPadding;
            [Min(1)] public int slotColumnCount;
            public Vector2 slotCellSize;
            public Vector2 slotSpacing;
            public Vector2 debtInputSize;
            public Vector2 debtButtonSize;
            [Min(0f)] public float debtTextHeight;
            [Min(0f)] public float debtElementSpacing;
            [Min(0f)] public float inputHorizontalPadding;

            public static LayoutSettings Default => new LayoutSettings
            {
                topHudHeight = 100f,
                sideBarWidth = 260f,
                messageAreaHeight = 80f,
                sideBarPadding = 20f,
                sideBarSpacing = 20f,
                tabButtonHeight = 120f,
                hudHorizontalPadding = 20f,
                messageHorizontalPadding = 0f,
                slotColumnCount = 4,
                slotCellSize = new Vector2(300f, 400f),
                slotSpacing = new Vector2(40f, 40f),
                debtInputSize = new Vector2(520f, 80f),
                debtButtonSize = new Vector2(320f, 80f),
                debtTextHeight = 120f,
                debtElementSpacing = 40f,
                inputHorizontalPadding = 12f
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
                iconBottomAnchor = 0.4f,
                nameBottomAnchor = 0.25f,
                costBottomAnchor = 0.08f,
                innerPadding = 12f
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
                tabFontSize = 32f,
                slotNameFontSize = 28f,
                slotCostFontSize = 26f,
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
                mainText = Color.black,
                dDayText = Color.red,
                messageText = Color.black,
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
