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
    public sealed class ShopSlotDesignPresenter : MonoBehaviour
    {
        [Header("디자인 프리셋")]
        [SerializeField] private ShopSceneDesignPreset preset;

        [Header("슬롯 참조")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Image purchaseButtonImage;
        [SerializeField] private Image soldOutOverlay;
        [SerializeField] private Image lockedOverlay;

        public ShopSceneDesignPreset Preset => preset;

        public void Bind(ShopSceneDesignPreset designPreset,
                         RectTransform boundRoot,
                         Image boundBackground,
                         RectTransform boundIconRect,
                         Image boundIconImage,
                         TMP_Text boundNameText,
                         TMP_Text boundCostText,
                         Button boundPurchaseButton,
                         Image boundPurchaseButtonImage,
                         Image boundSoldOutOverlay,
                         Image boundLockedOverlay)
        {
            preset = designPreset;
            root = boundRoot;
            background = boundBackground;
            iconRect = boundIconRect;
            iconImage = boundIconImage;
            nameText = boundNameText;
            costText = boundCostText;
            purchaseButton = boundPurchaseButton;
            purchaseButtonImage = boundPurchaseButtonImage;
            soldOutOverlay = boundSoldOutOverlay;
            lockedOverlay = boundLockedOverlay;
        }

        public void SetPresetIfNeeded(ShopSceneDesignPreset designPreset)
        {
            if (preset == null)
            {
                preset = designPreset;
            }
        }

        [ContextMenu("Apply Slot Design")]
        public void ApplyDesign()
        {
            if (preset == null)
            {
                return;
            }

            ShopSceneDesignPreset.LayoutSettings layout = preset.layout;
            ShopSceneDesignPreset.SlotSettings slot = preset.slot;
            ShopSceneDesignPreset.TextSettings text = preset.text;
            ShopSceneDesignPreset.ColorSettings colors = preset.colors;

            bool shouldApplyLayout = preset.applyRectTransformLayout;

            if (background != null)
            {
                background.sprite = preset.slotFrameSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = true;
                background.color = colors.slotBackground;
                background.raycastTarget = false;
            }

            if (shouldApplyLayout)
            {
                if (root != null)
                {
                    root.sizeDelta = layout.slotCellSize;
                }

                SetAnchors(iconRect, new Vector2(0f, slot.iconBottomAnchor), Vector2.one,
                           new Vector2(slot.innerPadding, slot.innerPadding),
                           new Vector2(-slot.innerPadding, -slot.innerPadding));
            }

            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
            }

            if (nameText != null)
            {
                nameText.fontSize = text.slotNameFontSize;
                nameText.color = colors.mainText;

                if (shouldApplyLayout)
                {
                    SetAnchors(nameText.rectTransform,
                               new Vector2(0f, slot.nameBottomAnchor),
                               new Vector2(1f, slot.iconBottomAnchor),
                               new Vector2(slot.innerPadding, 0f),
                               new Vector2(-slot.innerPadding, 0f));
                }
            }

            if (costText != null)
            {
                costText.fontSize = text.slotCostFontSize;
                costText.color = colors.mainText;

                if (shouldApplyLayout)
                {
                    SetAnchors(costText.rectTransform,
                               new Vector2(0f, slot.costBottomAnchor),
                               new Vector2(1f, slot.nameBottomAnchor),
                               new Vector2(slot.innerPadding, 0f),
                               new Vector2(-slot.innerPadding, 0f));
                }
            }

            if (purchaseButtonImage != null)
            {
                purchaseButtonImage.color = Color.clear;
            }

            if (purchaseButton != null)
            {
                purchaseButton.targetGraphic = purchaseButtonImage;
            }

            if (soldOutOverlay != null)
            {
                soldOutOverlay.color = colors.slotSoldOutOverlay;
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.color = colors.slotLockedOverlay;
            }

            MarkDirty();
        }

        private void Awake()
        {
            ApplyDesign();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplyDesignDelayed;
            EditorApplication.delayCall += ApplyDesignDelayed;
#else
            ApplyDesign();
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
