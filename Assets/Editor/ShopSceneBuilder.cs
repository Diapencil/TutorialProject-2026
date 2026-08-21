using System.Collections.Generic;
using System.IO;
using SheepSheepBurger.Core;
using SheepSheepBurger.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SheepSheepBurger.EditorTools
{
    /// <summary>
    /// .unity / .prefab 파일을 텍스트로 직접 쓰지 않고 전부 정식 에디터 API로 생성한다.
    /// 같은 메뉴를 다시 눌러도 안전하도록 애셋은 있으면 갱신, 없으면 생성한다.
    /// </summary>
    public static class ShopSceneBuilder
    {
        // ── 경로 ───────────────────────────────────────────────
        private const string ScenesFolder = "Assets/Scenes";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string DataFolder = "Assets/Data";
        private const string ShopDataFolder = "Assets/Data/Shop";
        private const string IngredientsFolder = "Assets/Data/Shop/Ingredients";
        private const string UpgradesFolder = "Assets/Data/Shop/Upgrades";
        private const string DecorationsFolder = "Assets/Data/Shop/Decorations";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpFontMaterialsFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        private const string ShopFontSourcePath = "Assets/@Developers/Lee/Fonts/NanumGothic.ttf";
        private const string ShopFontAssetPath = TmpFontMaterialsFolder + "/Shop Korean SDF.asset";
        private const string ExistingKoreanFontAssetPath = "Assets/@Developers/Lee/Fonts/NanumGothic SDF.asset";
        private const string ShopDesignPresetPath = ShopDataFolder + "/ShopSceneDesignPreset.asset";

        private const string ScenePath = ScenesFolder + "/ShopScene.unity";
        private const string SlotPrefabPath = PrefabsFolder + "/ShopSlot.prefab";

        // ── 레이아웃 수치 ──────────────────────────────────────
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ScalerMatch = 0.5f;

        private const float TopHudHeight = 100f;
        private const float SideBarWidth = 260f;
        private const float SideBarSpacing = 20f;
        private const float SideBarPadding = 20f;
        private const float TabButtonHeight = 120f;
        private const float MessageAreaHeight = 80f;

        private const float SlotCellWidth = 300f;
        private const float SlotCellHeight = 400f;
        private const float SlotSpacingX = 40f;
        private const float SlotSpacingY = 40f;
        private const int SlotColumnCount = 4;
        private const int ShopSlotCount = 4;

        private const float HudFontSize = 40f;
        private const float TabFontSize = 32f;
        private const float SlotNameFontSize = 28f;
        private const float SlotCostFontSize = 26f;
        private const float MessageFontSize = 30f;
        private const float DebtFontSize = 60f;
        private const float InputFontSize = 32f;
        private const int ShopFontSamplingPointSize = 90;
        private const int ShopFontAtlasPadding = 9;
        private const int ShopFontAtlasSize = 2048;

        // TODO(기획확인): 최초 스펙은 Screen Space - Overlay였지만, Game view 카메라 렌더링 확인을 위해 UI 카메라를 둔다.
        private static readonly Color CameraBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        private const float CameraOrthographicSize = 5.4f;
        private const float CameraDepth = -1f;
        private const float CanvasPlaneDistance = 10f;

        private const float SlotIconTopRatio = 0.4f;
        private const float SlotNameTopRatio = 0.25f;
        private const float SlotCostTopRatio = 0.08f;
        private const float SlotInnerPadding = 12f;

        private const float SoldOutOverlayAlpha = 0.6f;
        private const float LockedOverlayAlpha = 0.8f;
        private const float LockedOverlayGrey = 0.5f;

        private const float DebtTextHeight = 120f;
        private const float DebtInputWidth = 520f;
        private const float DebtInputHeight = 80f;
        private const float DebtButtonWidth = 320f;
        private const float DebtButtonHeight = 80f;
        private const float DebtElementSpacing = 40f;

        // ── 샘플 데이터 수치 ───────────────────────────────────
        private const int UpgradeMaxLevel = 4;

        // TODO(기획확인): 레벨별 비용은 전부 임시값이다.
        private static readonly int[] UpgradeCostPerLevel = { 5000, 10000, 20000, 40000 };

        // TODO(기획확인): 정확한 수치 미확정. 15→13→11→9→7초 기준으로 배율을 계산한다.
        private static readonly float[] FryerCookSeconds = { 15f, 13f, 11f, 9f, 7f };

        private static readonly float[] GrillBurnChancePerLevel = { 0.3f, 0.2f, 0.1f, 0f };

        private const int IngredientGrillableCostPerUse = 3;
        private const int IngredientRawCostPerUse = 2;
        private const int IngredientGrillableUnlockCost = 1000;
        private const int IngredientRawUnlockCost = 800;

        // TODO(기획확인): 장식 가격은 전부 임시값이다.
        private static readonly int[] DecorationCosts = { 1000, 2000, 3000, 5000 };

        private static readonly string ShopFontCharacters = string.Concat(
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            " .,!?:;+-*/()[]{}<>%#&'\"₩C",
            "토핑업수리장식상환하기갚을금액입력구매불가공사중캐이나인이부족합니다올바른보유빚모두갚았습니다를찾을수없습니다",
            "베이컨계란후라이피클할라피뇨튀김기그릴판꽃화분배너피규어마네키네코",
            "잔여부채레벨"
        );

        private static TMP_FontAsset activeFontAsset;

        [MenuItem("SheepSheep/Build Shop Scene")]
        public static void BuildShopScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!EnsureTextMeshProResources())
            {
                return;
            }

            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(ShopDataFolder);
            EnsureFolder(IngredientsFolder);
            EnsureFolder(UpgradesFolder);
            EnsureFolder(DecorationsFolder);
            EnsureFolder(TmpFontMaterialsFolder);

            activeFontAsset = EnsureShopFontAsset();
            ConfigureTextMeshProSettings(activeFontAsset);
            ShopSceneDesignPreset designPreset = EnsureShopDesignPreset();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // (A) 슬롯 프리팹
            ShopSlotUI slotPrefab = BuildSlotPrefab(designPreset);

            // (C) 샘플 SO 애셋
            IngredientData[] ingredients = ConfigureSampleIngredients();
            UpgradeData[] upgrades = CreateSampleUpgrades();
            DecorationData[] decorations = CreateSampleDecorations();

            // (B) 씬
            BuildScene(designPreset, slotPrefab, ingredients, upgrades, decorations);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[ShopSceneBuilder] 완료.\n씬: {ScenePath}\n프리팹: {SlotPrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static bool EnsureTextMeshProResources()
        {
            if (File.Exists(TmpSettingsPath))
            {
                return true;
            }

            bool shouldImport = Application.isBatchMode || EditorUtility.DisplayDialog(
                "TextMeshPro 리소스 없음",
                "TMP Essential Resources가 없어 자동으로 임포트합니다.\n" +
                "한글 폰트 애셋은 아직 별도로 연결해야 합니다.",
                "임포트",
                "취소");

            if (!shouldImport)
            {
                return false;
            }

            Debug.Log("[ShopSceneBuilder] TMP Essential Resources를 자동 임포트합니다.");
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();

            if (File.Exists(TmpSettingsPath))
            {
                return true;
            }

            Debug.LogError("[ShopSceneBuilder] TMP Essential Resources 임포트에 실패했습니다.");
            return false;
        }

        private static TMP_FontAsset EnsureShopFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ShopFontAssetPath);

            if (fontAsset == null)
            {
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ShopFontSourcePath);

                if (sourceFont != null)
                {
                    Debug.Log($"[ShopSceneBuilder] 상점 한글 TMP 폰트 에셋을 생성합니다: {ShopFontAssetPath}");
                    fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont,
                                                              ShopFontSamplingPointSize,
                                                              ShopFontAtlasPadding,
                                                              GlyphRenderMode.SDFAA,
                                                              ShopFontAtlasSize,
                                                              ShopFontAtlasSize,
                                                              AtlasPopulationMode.Dynamic,
                                                              true);

                    if (fontAsset != null)
                    {
                        fontAsset.name = "Shop Korean SDF";
                        fontAsset.material.name = "Shop Korean SDF Material";
                        fontAsset.atlasTextures[0].name = "Shop Korean SDF Atlas";
                        AssetDatabase.CreateAsset(fontAsset, ShopFontAssetPath);
                    }
                }
            }

            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ExistingKoreanFontAssetPath);

                if (fontAsset == null)
                {
                    Debug.LogWarning("[ShopSceneBuilder] 한글 TMP 폰트 에셋을 찾지 못해 TMP 기본 폰트를 사용합니다.");
                    return null;
                }

                Debug.LogWarning($"[ShopSceneBuilder] 상점 전용 폰트 생성에 실패해 기존 한글 폰트를 사용합니다: {ExistingKoreanFontAssetPath}");
                return fontAsset;
            }

            if (!HasAllShopCharacters(fontAsset))
            {
                AddShopCharacters(fontAsset);
            }

            AddFontSubAssets(fontAsset);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            return fontAsset;
        }

        private static bool HasAllShopCharacters(TMP_FontAsset fontAsset)
        {
            _ = fontAsset.characterLookupTable;
            return fontAsset.HasCharacters(ShopFontCharacters, out _);
        }

        private static void AddShopCharacters(TMP_FontAsset fontAsset)
        {
            AtlasPopulationMode originalMode = fontAsset.atlasPopulationMode;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            if (!fontAsset.TryAddCharacters(ShopFontCharacters, out string missingCharacters))
            {
                Debug.LogWarning($"[ShopSceneBuilder] 상점 폰트에 넣지 못한 문자가 있습니다: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = originalMode;
        }

        private static void AddFontSubAssets(TMP_FontAsset fontAsset)
        {
            AddSubAssetIfNeeded(fontAsset.material, fontAsset);

            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null)
            {
                return;
            }

            for (int i = 0; i < atlasTextures.Length; i++)
            {
                Texture2D atlasTexture = atlasTextures[i];
                if (atlasTexture != null && string.IsNullOrEmpty(atlasTexture.name))
                {
                    atlasTexture.name = i == 0 ? "Shop Korean SDF Atlas" : $"Shop Korean SDF Atlas {i}";
                }

                AddSubAssetIfNeeded(atlasTexture, fontAsset);
            }
        }

        private static void AddSubAssetIfNeeded(Object subAsset, TMP_FontAsset fontAsset)
        {
            if (subAsset == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(subAsset);
            if (assetPath == ShopFontAssetPath)
            {
                return;
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            AssetDatabase.AddObjectToAsset(subAsset, fontAsset);
        }

        private static void ConfigureTextMeshProSettings(TMP_FontAsset fontAsset)
        {
            TMP_Settings settings = TMP_Settings.instance;

            if (settings == null || fontAsset == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            serialized.FindProperty("m_defaultFontAssetPath").stringValue = "Fonts & Materials/";

            SerializedProperty fallbackFonts = serialized.FindProperty("m_fallbackFontAssets");
            bool hasFont = false;

            for (int i = 0; i < fallbackFonts.arraySize; i++)
            {
                if (fallbackFonts.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                {
                    hasFont = true;
                    break;
                }
            }

            if (!hasFont)
            {
                int index = fallbackFonts.arraySize;
                fallbackFonts.InsertArrayElementAtIndex(index);
                fallbackFonts.GetArrayElementAtIndex(index).objectReferenceValue = fontAsset;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static ShopSceneDesignPreset EnsureShopDesignPreset()
        {
            ShopSceneDesignPreset preset = AssetDatabase.LoadAssetAtPath<ShopSceneDesignPreset>(ShopDesignPresetPath);

            if (preset != null)
            {
                return preset;
            }

            preset = ScriptableObject.CreateInstance<ShopSceneDesignPreset>();
            AssetDatabase.CreateAsset(preset, ShopDesignPresetPath);
            AssetDatabase.SaveAssets();

            return preset;
        }

        // ══════════════════════════════════════════════════════
        // (A) 슬롯 프리팹
        // ══════════════════════════════════════════════════════
        private static ShopSlotUI BuildSlotPrefab(ShopSceneDesignPreset designPreset)
        {
            GameObject root = CreateUIObject("ShopSlot", null);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(SlotCellWidth, SlotCellHeight);

            Image background = root.AddComponent<Image>();
            background.color = Color.white;

            ShopSlotUI slotUI = root.AddComponent<ShopSlotUI>();

            // 아이콘 — 상단 60%
            GameObject iconObject = CreateUIObject("IconImage", root.transform);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            SetAnchors(iconRect, new Vector2(0f, SlotIconTopRatio), Vector2.one,
                       new Vector2(SlotInnerPadding, SlotInnerPadding),
                       new Vector2(-SlotInnerPadding, -SlotInnerPadding));
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            // 아트 리소스 입고 전까지는 스프라이트가 없으므로 꺼둔다.
            iconImage.enabled = false;

            TextMeshProUGUI nameText = CreateText("NameText", root.transform, string.Empty,
                                                  SlotNameFontSize, TextAlignmentOptions.Center, Color.black);
            SetAnchors(nameText.rectTransform,
                       new Vector2(0f, SlotNameTopRatio), new Vector2(1f, SlotIconTopRatio),
                       new Vector2(SlotInnerPadding, 0f), new Vector2(-SlotInnerPadding, 0f));

            TextMeshProUGUI costText = CreateText("CostText", root.transform, string.Empty,
                                                  SlotCostFontSize, TextAlignmentOptions.Center, Color.black);
            SetAnchors(costText.rectTransform,
                       new Vector2(0f, SlotCostTopRatio), new Vector2(1f, SlotNameTopRatio),
                       new Vector2(SlotInnerPadding, 0f), new Vector2(-SlotInnerPadding, 0f));

            // 구매 버튼 — 슬롯 전체를 덮는 투명 이미지
            GameObject buttonObject = CreateUIObject("PurchaseButton", root.transform);
            StretchFull((RectTransform)buttonObject.transform);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.clear;
            Button purchaseButton = buttonObject.AddComponent<Button>();
            purchaseButton.targetGraphic = buttonImage;

            GameObject soldOutOverlay = CreateOverlay("SoldOutOverlay", root.transform,
                                                      new Color(0f, 0f, 0f, SoldOutOverlayAlpha));
            GameObject lockedOverlay = CreateOverlay("LockedOverlay", root.transform,
                                                     new Color(LockedOverlayGrey, LockedOverlayGrey,
                                                               LockedOverlayGrey, LockedOverlayAlpha));
            Image soldOutOverlayImage = soldOutOverlay.GetComponent<Image>();
            Image lockedOverlayImage = lockedOverlay.GetComponent<Image>();

            ShopSlotDesignPresenter designPresenter = root.AddComponent<ShopSlotDesignPresenter>();
            designPresenter.Bind(designPreset, rootRect, background, iconRect, iconImage,
                                 nameText, costText, purchaseButton, buttonImage,
                                 soldOutOverlayImage, lockedOverlayImage);
            designPresenter.ApplyDesign();

            // SerializeField 6개를 코드로 연결
            SerializedObject serialized = new SerializedObject(slotUI);
            serialized.FindProperty("iconImage").objectReferenceValue = iconImage;
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("costText").objectReferenceValue = costText;
            serialized.FindProperty("purchaseButton").objectReferenceValue = purchaseButton;
            serialized.FindProperty("soldOutOverlay").objectReferenceValue = soldOutOverlay;
            serialized.FindProperty("lockedOverlay").objectReferenceValue = lockedOverlay;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            Object.DestroyImmediate(root);

            return savedPrefab != null ? savedPrefab.GetComponent<ShopSlotUI>() : null;
        }

        private static GameObject CreateOverlay(string name, Transform parent, Color color)
        {
            GameObject overlayObject = CreateUIObject(name, parent);
            StretchFull((RectTransform)overlayObject.transform);
            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.color = color;
            overlayObject.SetActive(false);
            return overlayObject;
        }

        // ══════════════════════════════════════════════════════
        // (B) 씬
        // ══════════════════════════════════════════════════════
        private static void BuildScene(ShopSceneDesignPreset designPreset, ShopSlotUI slotPrefab,
                                       IngredientData[] ingredients,
                                       UpgradeData[] upgrades, DecorationData[] decorations)
        {
            EnsureEventSystem();

            Camera uiCamera = CreateMainCamera();

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = CanvasPlaneDistance;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScalerMatch;

            canvasObject.AddComponent<GraphicRaycaster>();

            ShopManager shopManager = canvasObject.AddComponent<ShopManager>();
            ShopSceneDesignController designController = canvasObject.AddComponent<ShopSceneDesignController>();

            // ── TopHud ──
            GameObject topHud = CreateUIObject("TopHud", canvasObject.transform);
            RectTransform topHudRect = (RectTransform)topHud.transform;
            SetAnchors(topHudRect, new Vector2(0f, 1f), Vector2.one,
                       new Vector2(0f, -TopHudHeight), Vector2.zero);
            Image topHudBackground = topHud.AddComponent<Image>();
            topHudBackground.color = Color.clear;
            topHudBackground.raycastTarget = false;

            TextMeshProUGUI goldText = CreateText("GoldText", topHud.transform, "0.0C",
                                                  HudFontSize, TextAlignmentOptions.Left, Color.black);
            SetAnchors(goldText.rectTransform, Vector2.zero, Vector2.one,
                       new Vector2(SideBarPadding, 0f), new Vector2(-SideBarPadding, 0f));

            TextMeshProUGUI dDayText = CreateText("DDayText", topHud.transform, "D-30",
                                                  HudFontSize, TextAlignmentOptions.Right, Color.red);
            SetAnchors(dDayText.rectTransform, Vector2.zero, Vector2.one,
                       new Vector2(SideBarPadding, 0f), new Vector2(-SideBarPadding, 0f));

            // ── SideBar ──
            GameObject sideBar = CreateUIObject("SideBar", canvasObject.transform);
            RectTransform sideBarRect = (RectTransform)sideBar.transform;
            SetAnchors(sideBarRect, Vector2.zero, new Vector2(0f, 1f),
                       Vector2.zero, new Vector2(SideBarWidth, -TopHudHeight));
            Image sideBarBackground = sideBar.AddComponent<Image>();
            sideBarBackground.color = Color.clear;
            sideBarBackground.raycastTarget = false;

            VerticalLayoutGroup layout = sideBar.AddComponent<VerticalLayoutGroup>();
            layout.spacing = SideBarSpacing;
            layout.padding = new RectOffset((int)SideBarPadding, (int)SideBarPadding,
                                            (int)SideBarPadding, (int)SideBarPadding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            Button toppingTabButton = CreateTabButton("ToppingTabButton", sideBar.transform, "토핑");
            Button upgradeTabButton = CreateTabButton("UpgradeTabButton", sideBar.transform, "업&수리");
            Button decorationTabButton = CreateTabButton("DecorationTabButton", sideBar.transform, "장식");
            Button debtTabButton = CreateTabButton("DebtTabButton", sideBar.transform, "D-day");

            // ── 중앙 영역 (GridPanel / DebtPanel 공통 사각형) ──
            Vector2 centerOffsetMin = new Vector2(SideBarWidth, MessageAreaHeight);
            Vector2 centerOffsetMax = new Vector2(0f, -TopHudHeight);

            GameObject gridPanel = CreateUIObject("GridPanel", canvasObject.transform);
            RectTransform gridPanelRect = (RectTransform)gridPanel.transform;
            SetAnchors(gridPanelRect, Vector2.zero, Vector2.one,
                       centerOffsetMin, centerOffsetMax);
            Image gridPanelBackground = gridPanel.AddComponent<Image>();
            gridPanelBackground.color = Color.clear;
            gridPanelBackground.raycastTarget = false;

            GameObject slotParent = CreateUIObject("SlotParent", gridPanel.transform);
            RectTransform slotParentRect = (RectTransform)slotParent.transform;
            StretchFull(slotParentRect);
            GridLayoutGroup grid = slotParent.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(SlotCellWidth, SlotCellHeight);
            grid.spacing = new Vector2(SlotSpacingX, SlotSpacingY);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = SlotColumnCount;
            grid.childAlignment = TextAnchor.MiddleCenter;

            GameObject debtPanel = CreateUIObject("DebtPanel", canvasObject.transform);
            RectTransform debtPanelRect = (RectTransform)debtPanel.transform;
            SetAnchors(debtPanelRect, Vector2.zero, Vector2.one,
                       centerOffsetMin, centerOffsetMax);
            Image debtPanelBackground = debtPanel.AddComponent<Image>();
            debtPanelBackground.color = Color.clear;
            debtPanelBackground.raycastTarget = false;

            TextMeshProUGUI debtRemainingText = CreateText("DebtRemainingText", debtPanel.transform,
                                                           "1500.0C", DebtFontSize,
                                                           TextAlignmentOptions.Center, Color.black);
            RectTransform debtTextRect = debtRemainingText.rectTransform;
            debtTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            debtTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            debtTextRect.pivot = new Vector2(0.5f, 0.5f);
            debtTextRect.sizeDelta = new Vector2(DebtInputWidth, DebtTextHeight);
            debtTextRect.anchoredPosition = new Vector2(0f, DebtTextHeight + DebtElementSpacing);

            TMP_InputField repayInputField = CreateInputField("RepayInputField", debtPanel.transform,
                                                              "갚을 금액 입력");
            RectTransform inputRect = (RectTransform)repayInputField.transform;
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = new Vector2(DebtInputWidth, DebtInputHeight);
            inputRect.anchoredPosition = Vector2.zero;

            Button repayConfirmButton = CreateButton("RepayConfirmButton", debtPanel.transform, "상환하기");
            RectTransform repayRect = (RectTransform)repayConfirmButton.transform;
            repayRect.anchorMin = new Vector2(0.5f, 0.5f);
            repayRect.anchorMax = new Vector2(0.5f, 0.5f);
            repayRect.pivot = new Vector2(0.5f, 0.5f);
            repayRect.sizeDelta = new Vector2(DebtButtonWidth, DebtButtonHeight);
            repayRect.anchoredPosition = new Vector2(0f, -(DebtInputHeight + DebtElementSpacing));

            debtPanel.SetActive(false);

            // ── MessageBar ──
            GameObject messageBar = CreateUIObject("MessageBar", canvasObject.transform);
            RectTransform messageBarRect = (RectTransform)messageBar.transform;
            SetAnchors(messageBarRect, Vector2.zero, new Vector2(1f, 0f),
                       Vector2.zero, new Vector2(0f, MessageAreaHeight));
            Image messageBarBackground = messageBar.AddComponent<Image>();
            messageBarBackground.color = Color.clear;
            messageBarBackground.raycastTarget = false;

            TextMeshProUGUI messageText = CreateText("MessageText", messageBar.transform, string.Empty,
                                                     MessageFontSize, TextAlignmentOptions.Center, Color.black);
            StretchFull(messageText.rectTransform);

            // ── GameManagerObj ──
            GameObject gameManagerObject = new GameObject("GameManagerObj");
            gameManagerObject.AddComponent<SheepSheepBurger.Core.GameManager>();

            WireShopManager(shopManager, slotPrefab, ingredients, upgrades, decorations,
                            toppingTabButton, upgradeTabButton, decorationTabButton, debtTabButton,
                            gridPanel, debtPanel, slotParent.transform,
                            goldText, dDayText, messageText,
                            debtRemainingText, repayInputField, repayConfirmButton);

            Image inputBackground = repayInputField.targetGraphic as Image;
            Image repayButtonBackground = repayConfirmButton.targetGraphic as Image;
            ShopSlotDesignPresenter slotPrefabDesign = slotPrefab != null
                ? slotPrefab.GetComponent<ShopSlotDesignPresenter>()
                : null;
            designController.Bind(designPreset, uiCamera, canvas, scaler,
                                  topHudRect, topHudBackground,
                                  sideBarRect, sideBarBackground, layout,
                                  gridPanelRect, gridPanelBackground,
                                  slotParentRect, grid,
                                  debtPanelRect, debtPanelBackground,
                                  messageBarRect, messageBarBackground,
                                  goldText, dDayText, messageText, debtRemainingText,
                                  repayInputField, inputBackground,
                                  repayConfirmButton, repayButtonBackground,
                                  new[] { toppingTabButton, upgradeTabButton, decorationTabButton, debtTabButton },
                                  slotPrefabDesign);
            designController.ApplyDesign();
        }

        private static Camera CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.depth = CameraDepth;

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = true;

            return camera;
        }

        private static void WireShopManager(ShopManager shopManager, ShopSlotUI slotPrefab,
                                            IngredientData[] ingredients, UpgradeData[] upgrades,
                                            DecorationData[] decorations,
                                            Button toppingTab, Button upgradeTab,
                                            Button decorationTab, Button debtTab,
                                            GameObject gridPanel, GameObject debtPanel,
                                            Transform slotParent,
                                            TMP_Text goldText, TMP_Text dDayText, TMP_Text messageText,
                                            TMP_Text debtRemainingText, TMP_InputField repayInputField,
                                            Button repayConfirmButton)
        {
            SerializedObject serialized = new SerializedObject(shopManager);

            SetObjectArray(serialized, "allIngredients", ingredients);
            SetObjectArray(serialized, "allUpgrades", upgrades);
            SetObjectArray(serialized, "allDecorations", decorations);

            serialized.FindProperty("toppingTabButton").objectReferenceValue = toppingTab;
            serialized.FindProperty("upgradeTabButton").objectReferenceValue = upgradeTab;
            serialized.FindProperty("decorationTabButton").objectReferenceValue = decorationTab;
            serialized.FindProperty("debtTabButton").objectReferenceValue = debtTab;

            serialized.FindProperty("gridPanel").objectReferenceValue = gridPanel;
            serialized.FindProperty("debtPanel").objectReferenceValue = debtPanel;

            serialized.FindProperty("slotParent").objectReferenceValue = slotParent;
            serialized.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            serialized.FindProperty("slotCount").intValue = ShopSlotCount;

            serialized.FindProperty("goldText").objectReferenceValue = goldText;
            serialized.FindProperty("dDayText").objectReferenceValue = dDayText;
            serialized.FindProperty("messageText").objectReferenceValue = messageText;

            serialized.FindProperty("debtRemainingText").objectReferenceValue = debtRemainingText;
            serialized.FindProperty("repayInputField").objectReferenceValue = repayInputField;
            serialized.FindProperty("repayConfirmButton").objectReferenceValue = repayConfirmButton;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            // 이 프로젝트는 Active Input Handling이 Input System Package로 설정되어 있어
            // StandaloneInputModule을 쓰면 런타임에 예외가 난다.
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        // ══════════════════════════════════════════════════════
        // (C) 샘플 SO 애셋
        // ══════════════════════════════════════════════════════
        private static IngredientData[] ConfigureSampleIngredients()
        {
            List<IngredientData> result = new List<IngredientData>();

            AddIngredient(result, "Bacon", 1, "베이컨", SheepSheepBurger.Core.IngredientType.Topping,
                          IngredientGrillableUnlockCost, IngredientGrillableCostPerUse, true);
            AddIngredient(result, "FriedEgg", 2, "계란후라이", SheepSheepBurger.Core.IngredientType.Topping,
                          IngredientGrillableUnlockCost, IngredientGrillableCostPerUse, true);
            AddIngredient(result, "Pickle", 3, "피클", SheepSheepBurger.Core.IngredientType.Topping,
                          IngredientRawUnlockCost, IngredientRawCostPerUse, false);
            AddIngredient(result, "Jalapeno", 4, "할라피뇨", SheepSheepBurger.Core.IngredientType.Topping,
                          IngredientRawUnlockCost, IngredientRawCostPerUse, false);

            return result.ToArray();
        }

        private static void AddIngredient(List<IngredientData> result, string assetName, int id,
                                          string ingredientName, SheepSheepBurger.Core.IngredientType type,
                                          int unlockCost, int costPerUse, bool grillable)
        {
            string path = $"{IngredientsFolder}/{assetName}.asset";
            IngredientData data = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<IngredientData>();
            }

            data.id = id;
            data.ingredientName = ingredientName;
            data.type = type;
            data.unlockCost = unlockCost;
            data.costPerUse = costPerUse;
            data.grillable = grillable;

            // 상점에서 해금하는 재료이므로 기본 해금이 아니다.
            data.isDefaultUnlocked = false;

            // TODO(기획확인): 샘플 재료별 cookTimeMin/Max가 확정되지 않아 기본값으로 둔다.
            data.cookTimeMin = 0f;
            data.cookTimeMax = 0f;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            result.Add(data);
        }

        private static UpgradeData[] CreateSampleUpgrades()
        {
            float[] fryerMultipliers = new float[FryerCookSeconds.Length];
            for (int i = 0; i < FryerCookSeconds.Length; i++)
            {
                fryerMultipliers[i] = FryerCookSeconds[i] / FryerCookSeconds[0];
            }

            UpgradeData fryer = CreateOrUpdateUpgrade("Fryer", 1, "튀김기", UpgradeType.Fryer,
                                                      fryerMultipliers, new float[0]);

            // TODO(기획확인): 그릴판의 레벨별 조리 시간 배율은 스펙에 없어 비워 둔다.
            UpgradeData grill = CreateOrUpdateUpgrade("Grill", 2, "그릴판", UpgradeType.Grill,
                                                      new float[0], GrillBurnChancePerLevel);

            return new[] { fryer, grill };
        }

        private static UpgradeData CreateOrUpdateUpgrade(string assetName, int id, string upgradeName,
                                                         UpgradeType type, float[] cookTimeMultiplier,
                                                         float[] burnChancePerLevel)
        {
            string path = $"{UpgradesFolder}/{assetName}.asset";
            UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<UpgradeData>();
            }

            data.id = id;
            data.name = upgradeName;
            data.type = type;
            data.maxLevel = UpgradeMaxLevel;
            data.costPerLevel = new List<int>(UpgradeCostPerLevel);
            data.timeReduction = new List<float>(cookTimeMultiplier);
            data.burnChancePerLevel = new List<float>(burnChancePerLevel);

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static DecorationData[] CreateSampleDecorations()
        {
            return new[]
            {
                CreateOrUpdateDecoration("FlowerPot", 1, "꽃 화분", DecorationCosts[0]),
                CreateOrUpdateDecoration("Banner", 2, "배너", DecorationCosts[1]),
                CreateOrUpdateDecoration("Figure", 3, "피규어", DecorationCosts[2]),
                CreateOrUpdateDecoration("ManekiNeko", 4, "마네키네코", DecorationCosts[3])
            };
        }

        private static DecorationData CreateOrUpdateDecoration(string assetName, int id,
                                                               string decorationName, int cost)
        {
            string path = $"{DecorationsFolder}/{assetName}.asset";
            DecorationData data = AssetDatabase.LoadAssetAtPath<DecorationData>(path);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<DecorationData>();
            }

            data.id = id;
            data.decorationName = decorationName;
            data.cost = cost;

            // TODO(기획확인): 카운터 배치 좌표가 스토리보드에 없어 원점으로 둔다.
            data.counterPosition = Vector2.zero;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        // ══════════════════════════════════════════════════════
        // UI 생성 헬퍼
        // ══════════════════════════════════════════════════════
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject created = new GameObject(name, typeof(RectTransform));

            if (parent != null)
            {
                created.transform.SetParent(parent, false);
            }

            return created;
        }

        private static void StretchFull(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string content,
                                                  float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = CreateUIObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();

            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;

            if (activeFontAsset != null)
            {
                text.font = activeFontAsset;
            }

            return text;
        }

        private static Button CreateTabButton(string name, Transform parent, string label)
        {
            Button button = CreateButton(name, parent, label);

            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = TabButtonHeight;
            layoutElement.minHeight = TabButtonHeight;

            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = CreateUIObject(name, parent);

            Image image = buttonObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText($"{name}Label", buttonObject.transform, label,
                                              TabFontSize, TextAlignmentOptions.Center, Color.black);
            StretchFull(text.rectTransform);

            return button;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderText)
        {
            GameObject inputObject = CreateUIObject(name, parent);

            // TMP_InputField는 OnEnable에서 참조를 검사하므로 배선이 끝난 뒤에 켠다.
            inputObject.SetActive(false);

            Image background = inputObject.AddComponent<Image>();
            background.color = Color.white;

            TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
            inputField.targetGraphic = background;

            GameObject textArea = CreateUIObject("TextArea", inputObject.transform);
            RectTransform textAreaRect = (RectTransform)textArea.transform;
            SetAnchors(textAreaRect, Vector2.zero, Vector2.one,
                       new Vector2(SlotInnerPadding, 0f), new Vector2(-SlotInnerPadding, 0f));
            textArea.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholder = CreateText("Placeholder", textArea.transform, placeholderText,
                                                     InputFontSize, TextAlignmentOptions.Left, Color.grey);
            StretchFull(placeholder.rectTransform);

            TextMeshProUGUI text = CreateText("Text", textArea.transform, string.Empty,
                                              InputFontSize, TextAlignmentOptions.Left, Color.black);
            StretchFull(text.rectTransform);

            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.text = string.Empty;

            inputObject.SetActive(true);

            return inputField;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
