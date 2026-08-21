using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SheepSheepBurger.Shop
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShopSceneDesignController : MonoBehaviour
    {
        [Header("디자인 프리셋")]
        [SerializeField] private ShopSceneDesignPreset preset;

        [Header("Canvas / Camera")]
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("패널")]
        [SerializeField] private RectTransform topHud;
        [SerializeField] private Image topHudBackground;
        [SerializeField] private RectTransform sideBar;
        [SerializeField] private Image sideBarBackground;
        [SerializeField] private VerticalLayoutGroup sideBarLayout;
        [SerializeField] private RectTransform gridPanel;
        [SerializeField] private Image gridPanelBackground;
        [SerializeField] private RectTransform slotParent;
        [SerializeField] private GridLayoutGroup slotGrid;
        [SerializeField] private RectTransform debtPanel;
        [SerializeField] private Image debtPanelBackground;
        [SerializeField] private RectTransform messageBar;
        [SerializeField] private Image messageBarBackground;

        [Header("텍스트")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text dDayText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text debtRemainingText;

        [Header("입력 / 버튼")]
        [SerializeField] private TMP_InputField repayInputField;
        [SerializeField] private Image repayInputBackground;
        [SerializeField] private Button repayConfirmButton;
        [SerializeField] private Image repayConfirmBackground;
        [SerializeField] private Button[] tabButtons = Array.Empty<Button>();

        [Header("슬롯 프리팹")]
        [SerializeField] private ShopSlotDesignPresenter slotPrefabDesign;

        public ShopSceneDesignPreset Preset => preset;

        public void Bind(ShopSceneDesignPreset designPreset,
                         Camera boundCamera,
                         Canvas boundCanvas,
                         CanvasScaler boundCanvasScaler,
                         RectTransform boundTopHud,
                         Image boundTopHudBackground,
                         RectTransform boundSideBar,
                         Image boundSideBarBackground,
                         VerticalLayoutGroup boundSideBarLayout,
                         RectTransform boundGridPanel,
                         Image boundGridPanelBackground,
                         RectTransform boundSlotParent,
                         GridLayoutGroup boundSlotGrid,
                         RectTransform boundDebtPanel,
                         Image boundDebtPanelBackground,
                         RectTransform boundMessageBar,
                         Image boundMessageBarBackground,
                         TMP_Text boundGoldText,
                         TMP_Text boundDDayText,
                         TMP_Text boundMessageText,
                         TMP_Text boundDebtRemainingText,
                         TMP_InputField boundRepayInputField,
                         Image boundRepayInputBackground,
                         Button boundRepayConfirmButton,
                         Image boundRepayConfirmBackground,
                         Button[] boundTabButtons,
                         ShopSlotDesignPresenter boundSlotPrefabDesign)
        {
            preset = designPreset;
            uiCamera = boundCamera;
            canvas = boundCanvas;
            canvasScaler = boundCanvasScaler;
            topHud = boundTopHud;
            topHudBackground = boundTopHudBackground;
            sideBar = boundSideBar;
            sideBarBackground = boundSideBarBackground;
            sideBarLayout = boundSideBarLayout;
            gridPanel = boundGridPanel;
            gridPanelBackground = boundGridPanelBackground;
            slotParent = boundSlotParent;
            slotGrid = boundSlotGrid;
            debtPanel = boundDebtPanel;
            debtPanelBackground = boundDebtPanelBackground;
            messageBar = boundMessageBar;
            messageBarBackground = boundMessageBarBackground;
            goldText = boundGoldText;
            dDayText = boundDDayText;
            messageText = boundMessageText;
            debtRemainingText = boundDebtRemainingText;
            repayInputField = boundRepayInputField;
            repayInputBackground = boundRepayInputBackground;
            repayConfirmButton = boundRepayConfirmButton;
            repayConfirmBackground = boundRepayConfirmBackground;
            tabButtons = boundTabButtons ?? Array.Empty<Button>();
            slotPrefabDesign = boundSlotPrefabDesign;
        }

        [ContextMenu("Apply Design Settings")]
        public void ApplyDesign()
        {
            if (preset == null)
            {
                return;
            }

            ApplyCanvasAndCamera();
            ApplyPanelLayout();
            ApplyText();
            ApplyColorsAndButtons();
            ApplySlotDesign();
            MarkDirty();
        }

        private void Awake()
        {
            ApplyDesign();
        }

        private void OnValidate()
        {
            ApplyDesign();
        }

        private void ApplyCanvasAndCamera()
        {
            if (uiCamera != null)
            {
                uiCamera.backgroundColor = preset.cameraBackgroundColor;
                uiCamera.orthographicSize = preset.cameraOrthographicSize;
            }

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = preset.canvasSortingOrder;
                canvas.planeDistance = preset.canvasPlaneDistance;
            }

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = preset.referenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = preset.matchWidthOrHeight;
            }
        }

        private void ApplyPanelLayout()
        {
            ShopSceneDesignPreset.LayoutSettings layout = preset.layout;
            Vector2 centerOffsetMin = new Vector2(layout.sideBarWidth, layout.messageAreaHeight);
            Vector2 centerOffsetMax = new Vector2(0f, -layout.topHudHeight);

            SetAnchors(topHud, new Vector2(0f, 1f), Vector2.one,
                       new Vector2(0f, -layout.topHudHeight), Vector2.zero);
            SetAnchors(sideBar, Vector2.zero, new Vector2(0f, 1f),
                       Vector2.zero, new Vector2(layout.sideBarWidth, -layout.topHudHeight));
            SetAnchors(gridPanel, Vector2.zero, Vector2.one, centerOffsetMin, centerOffsetMax);
            SetAnchors(debtPanel, Vector2.zero, Vector2.one, centerOffsetMin, centerOffsetMax);
            SetAnchors(messageBar, Vector2.zero, new Vector2(1f, 0f),
                       Vector2.zero, new Vector2(0f, layout.messageAreaHeight));

            if (goldText != null)
            {
                SetAnchors(goldText.rectTransform, Vector2.zero, Vector2.one,
                           new Vector2(layout.hudHorizontalPadding, 0f),
                           new Vector2(-layout.hudHorizontalPadding, 0f));
            }

            if (dDayText != null)
            {
                SetAnchors(dDayText.rectTransform, Vector2.zero, Vector2.one,
                           new Vector2(layout.hudHorizontalPadding, 0f),
                           new Vector2(-layout.hudHorizontalPadding, 0f));
            }

            if (messageText != null)
            {
                SetAnchors(messageText.rectTransform, Vector2.zero, Vector2.one,
                           new Vector2(layout.messageHorizontalPadding, 0f),
                           new Vector2(-layout.messageHorizontalPadding, 0f));
            }

            if (sideBarLayout != null)
            {
                int padding = Mathf.RoundToInt(layout.sideBarPadding);
                sideBarLayout.padding = new RectOffset(padding, padding, padding, padding);
                sideBarLayout.spacing = layout.sideBarSpacing;
            }

            ApplyTabLayout(layout.tabButtonHeight);

            if (slotGrid != null)
            {
                slotGrid.cellSize = layout.slotCellSize;
                slotGrid.spacing = layout.slotSpacing;
                slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                slotGrid.constraintCount = Mathf.Max(1, layout.slotColumnCount);
                slotGrid.childAlignment = TextAnchor.MiddleCenter;
            }

            ApplyDebtLayout(layout);
            ApplyLayerOrder();
        }

        private void ApplyTabLayout(float tabButtonHeight)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null)
                {
                    continue;
                }

                LayoutElement layoutElement = tabButtons[i].GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = tabButtons[i].gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.preferredHeight = tabButtonHeight;
                layoutElement.minHeight = tabButtonHeight;
            }
        }

        private void ApplyDebtLayout(ShopSceneDesignPreset.LayoutSettings layout)
        {
            if (debtRemainingText != null)
            {
                RectTransform rect = debtRemainingText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(layout.debtInputSize.x, layout.debtTextHeight);
                rect.anchoredPosition = new Vector2(0f, layout.debtTextHeight + layout.debtElementSpacing);
            }

            if (repayInputField != null)
            {
                RectTransform rect = (RectTransform)repayInputField.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = layout.debtInputSize;
                rect.anchoredPosition = Vector2.zero;

                if (repayInputField.textViewport != null)
                {
                    SetAnchors(repayInputField.textViewport, Vector2.zero, Vector2.one,
                               new Vector2(layout.inputHorizontalPadding, 0f),
                               new Vector2(-layout.inputHorizontalPadding, 0f));
                }
            }

            if (repayConfirmButton != null)
            {
                RectTransform rect = (RectTransform)repayConfirmButton.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = layout.debtButtonSize;
                rect.anchoredPosition = new Vector2(0f, -(layout.debtInputSize.y + layout.debtElementSpacing));
            }
        }

        private void ApplyLayerOrder()
        {
            ShopSceneDesignPreset.LayerOrderSettings order = preset.layerOrder;
            SetLocalZ(gridPanel, order.gridPanelZ);
            SetLocalZ(debtPanel, order.debtPanelZ);
            SetLocalZ(sideBar, order.sideBarZ);
            SetLocalZ(topHud, order.topHudZ);
            SetLocalZ(messageBar, order.messageBarZ);

            OrderedTransform[] ordered =
            {
                new OrderedTransform(gridPanel, order.gridPanel),
                new OrderedTransform(debtPanel, order.debtPanel),
                new OrderedTransform(sideBar, order.sideBar),
                new OrderedTransform(topHud, order.topHud),
                new OrderedTransform(messageBar, order.messageBar)
            };

            Array.Sort(ordered, (left, right) => left.order.CompareTo(right.order));

            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].transform != null)
                {
                    ordered[i].transform.SetSiblingIndex(i);
                }
            }
        }

        private void ApplyText()
        {
            ShopSceneDesignPreset.TextSettings textSettings = preset.text;
            ShopSceneDesignPreset.ColorSettings colorSettings = preset.colors;

            SetText(goldText, textSettings.hudFontSize, colorSettings.mainText);
            SetText(dDayText, textSettings.hudFontSize, colorSettings.dDayText);
            SetText(messageText, textSettings.messageFontSize, colorSettings.messageText);
            SetText(debtRemainingText, textSettings.debtFontSize, colorSettings.mainText);

            for (int i = 0; i < tabButtons.Length; i++)
            {
                TMP_Text label = GetButtonLabel(tabButtons[i]);
                SetText(label, textSettings.tabFontSize, colorSettings.mainText);
            }

            if (repayConfirmButton != null)
            {
                SetText(GetButtonLabel(repayConfirmButton), textSettings.tabFontSize, colorSettings.mainText);
            }

            if (repayInputField != null)
            {
                SetText(repayInputField.textComponent, textSettings.inputFontSize, colorSettings.mainText);
                SetText(repayInputField.placeholder as TMP_Text, textSettings.inputFontSize, colorSettings.placeholderText);
            }
        }

        private void ApplyColorsAndButtons()
        {
            ShopSceneDesignPreset.ColorSettings colorSettings = preset.colors;

            SetImage(topHudBackground, colorSettings.topHudBackground, false);
            SetImage(sideBarBackground, colorSettings.sideBarBackground, false);
            SetImage(gridPanelBackground, colorSettings.gridPanelBackground, false);
            SetImage(debtPanelBackground, colorSettings.debtPanelBackground, false);
            SetImage(messageBarBackground, colorSettings.messageBarBackground, false);
            SetImage(repayInputBackground, colorSettings.inputBackground, true);
            SetImage(repayConfirmBackground, colorSettings.actionButtonBackground, true);

            for (int i = 0; i < tabButtons.Length; i++)
            {
                ApplyButtonColors(tabButtons[i], colorSettings.tabNormal, colorSettings.tabHighlighted,
                                  colorSettings.tabPressed, colorSettings.tabSelected);
            }

            ApplyButtonColors(repayConfirmButton, colorSettings.actionButtonBackground, colorSettings.tabHighlighted,
                              colorSettings.tabPressed, colorSettings.tabSelected);
        }

        private void ApplySlotDesign()
        {
            if (slotPrefabDesign != null)
            {
                slotPrefabDesign.SetPresetIfNeeded(preset);
                slotPrefabDesign.ApplyDesign();
            }

            ShopSlotDesignPresenter[] sceneSlots =
                GetComponentsInChildren<ShopSlotDesignPresenter>(true);

            for (int i = 0; i < sceneSlots.Length; i++)
            {
                if (sceneSlots[i] == null || sceneSlots[i] == slotPrefabDesign)
                {
                    continue;
                }

                sceneSlots[i].SetPresetIfNeeded(preset);
                sceneSlots[i].ApplyDesign();
            }
        }

        private static void ApplyButtonColors(Button button, Color normal, Color highlighted,
                                              Color pressed, Color disabled)
        {
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.disabledColor = disabled;
            button.colors = colors;

            if (button.targetGraphic is Image image)
            {
                image.color = button.interactable ? normal : disabled;
            }
        }

        private static TMP_Text GetButtonLabel(Button button)
        {
            return button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
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

        private static void SetText(TMP_Text target, float fontSize, Color color)
        {
            if (target == null)
            {
                return;
            }

            target.fontSize = fontSize;
            target.color = color;
        }

        private static void SetLocalZ(RectTransform rect, float z)
        {
            if (rect == null)
            {
                return;
            }

            Vector3 localPosition = rect.localPosition;
            localPosition.z = z;
            rect.localPosition = localPosition;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private readonly struct OrderedTransform
        {
            public readonly RectTransform transform;
            public readonly int order;

            public OrderedTransform(RectTransform transform, int order)
            {
                this.transform = transform;
                this.order = order;
            }
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
