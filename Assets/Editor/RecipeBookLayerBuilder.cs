using System.Collections.Generic;
using System.IO;
using SheepSheepBurger.RecipeBook;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 옛날 전역 RecipeData 와 이름이 겹치므로 겹치지 않는 별칭으로 Core 타입만 집는다.
using CoreRecipeData = SheepSheepBurger.Core.RecipeData;

namespace SheepSheepBurger.EditorTools
{
    /// <summary>
    /// 햄버거 도감(레시피 북) 레이어 프리팹을 통째로 만들어 준다.
    /// 메뉴: SheepSheep → Build Recipe Book Layer Prefab
    ///
    /// 결과물:
    ///  - Assets/Prefabs/RecipeBookEntry.prefab  (격자 칸 1개)
    ///  - Assets/Prefabs/RecipeBookLayer.prefab  (도감 전체 레이어 + 우상단 책 버튼)
    ///
    /// 아트 리소스 없이 둥근 패널 스프라이트를 여기서 절차적으로 만들어 쓴다.
    /// 팔레트는 육식동물 선술집(클로우 시티) 느낌.
    /// </summary>
    public static class RecipeBookLayerBuilder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string ArtFolder = "Assets/GameAssets/RecipeBook/UI";
        private const string EntryPrefabPath = PrefabsFolder + "/RecipeBookEntry.prefab";
        private const string LayerPrefabPath = PrefabsFolder + "/RecipeBookLayer.prefab";
        private const string BookIconPath = "Assets/GameAssets/RecipeBook/free-icon-book-207114.png";

        private const int CanvasSortingOrder = 850;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // ── 팔레트 ──
        private static readonly Color Ink          = new Color(0.16f, 0.11f, 0.06f, 1f);
        private static readonly Color Walnut       = new Color(0.20f, 0.13f, 0.09f, 1f);
        private static readonly Color WalnutDark   = new Color(0.09f, 0.06f, 0.04f, 1f);
        private static readonly Color Parchment    = new Color(0.93f, 0.86f, 0.70f, 1f);
        private static readonly Color ParchmentDim = new Color(0.62f, 0.55f, 0.42f, 1f);
        private static readonly Color Gold         = new Color(0.80f, 0.58f, 0.24f, 1f);
        private static readonly Color Cream        = new Color(1f, 0.96f, 0.86f, 1f);
        private static readonly Color BrickRed     = new Color(0.60f, 0.24f, 0.17f, 1f);
        private static readonly Color Shadow       = new Color(0f, 0f, 0f, 0.32f);

        [MenuItem("SheepSheep/Build Recipe Book Layer Prefab")]
        public static void Build()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(ArtFolder);

            // 폰트는 Dynamic 모드라 필요한 글자를 실행 중에 알아서 추가한다. 폰트 애셋은 건드리지 않는다.
            TMP_FontAsset font = ResolveFont();
            Sprite panelSprite = EnsureRoundedSprite("panel", 40);
            Sprite cardSprite = EnsureRoundedSprite("card", 20);

            GameObject entryRoot = CreateEntry(font, cardSprite);
            GameObject entryPrefab = PrefabUtility.SaveAsPrefabAsset(entryRoot, EntryPrefabPath);
            Object.DestroyImmediate(entryRoot);
            RecipeBookEntryView entryPrefabView = entryPrefab.GetComponent<RecipeBookEntryView>();

            List<CoreRecipeData> recipes = FindAllCoreRecipes();
            GameObject layerRoot = CreateLayer(font, panelSprite, cardSprite, entryPrefabView, recipes);
            PrefabUtility.SaveAsPrefabAsset(layerRoot, LayerPrefabPath);
            Object.DestroyImmediate(layerRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RecipeBookLayerBuilder] 완료.\n- {LayerPrefabPath}\n- {EntryPrefabPath}\n" +
                      $"레시피 {recipes.Count}개 연결됨. (테스트용으로 id 1 해금 처리)");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(LayerPrefabPath));
        }

        // ────────────────────────────── 격자 칸 ──────────────────────────────

        private static GameObject CreateEntry(TMP_FontAsset font, Sprite cardSprite)
        {
            GameObject root = CreateUIObject("RecipeBookEntry", null);
            SetSize(root.GetComponent<RectTransform>(), new Vector2(168f, 210f));

            Image background = root.AddComponent<Image>();
            background.sprite = cardSprite;
            background.type = Image.Type.Sliced;
            background.color = Parchment;
            AddOutline(root, WalnutDark, new Vector2(0f, -3f));

            Button button = root.AddComponent<Button>();
            button.targetGraphic = background;
            SetButtonTint(button, background);

            // 미해금일 때 카드를 어둡게 덮는 오버레이
            Image lockedBadge = CreateImage("LockedBadge", root.transform, cardSprite);
            lockedBadge.type = Image.Type.Sliced;
            lockedBadge.color = new Color(0.12f, 0.09f, 0.06f, 0.55f);
            lockedBadge.raycastTarget = false;
            SetStretch(lockedBadge.rectTransform, Vector2.zero, Vector2.zero);
            lockedBadge.gameObject.SetActive(false);

            TMP_Text label = CreateText("Label", root.transform, "???", 26f, font, TextAlignmentOptions.Center);
            SetStretch(label.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));

            RecipeBookEntryView view = root.AddComponent<RecipeBookEntryView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("lockedBadge").objectReferenceValue = lockedBadge.gameObject;
            so.FindProperty("unlockedColor").colorValue = Ink;
            so.FindProperty("lockedColor").colorValue = Cream;
            so.FindProperty("lockedText").stringValue = "? ? ?";
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ────────────────────────────── 레이어 ──────────────────────────────

        private static GameObject CreateLayer(TMP_FontAsset font,
                                              Sprite panelSprite,
                                              Sprite cardSprite,
                                              RecipeBookEntryView entryPrefabView,
                                              List<CoreRecipeData> recipes)
        {
            GameObject root = CreateUIObject("RecipeBookLayer", null);
            SetStretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // 항상 보이는 "도감 열기" 버튼 (우상단). 이 Canvas 를 쓰므로 화면 고정.
            Sprite bookSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BookIconPath);
            Image openImage = CreateImage("OpenButton", root.transform, bookSprite);
            openImage.color = Color.white;
            openImage.preserveAspect = true;
            SetTopRight(openImage.rectTransform, new Vector2(40f, 40f), new Vector2(128f, 128f));
            Button openButton = openImage.gameObject.AddComponent<Button>();
            openButton.targetGraphic = openImage;
            SetButtonTint(openButton, openImage);
            if (bookSprite == null)
            {
                openImage.sprite = cardSprite;
                openImage.type = Image.Type.Sliced;
                openImage.color = Gold;
                CreateText("Label", openImage.transform, "도감", 24f, font, TextAlignmentOptions.Center);
            }

            // ── PanelLayer (켜지고 꺼지는 부분) ──
            GameObject panelLayer = CreateUIObject("PanelLayer", root.transform);
            SetStretch(panelLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            CanvasGroup panelCanvasGroup = panelLayer.AddComponent<CanvasGroup>();

            Image backdrop = CreateImage("Backdrop", panelLayer.transform, null);
            SetStretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.color = new Color(0f, 0f, 0f, 0.55f);
            Button backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;

            // 그림자
            Image shadow = CreatePanel(panelLayer.transform, "PanelShadow", panelSprite, Shadow);
            SetCenter(shadow.rectTransform, new Vector2(10f, -14f), new Vector2(1224f, 844f));

            // 본체 패널 (짙은 호두나무색)
            Image panel = CreatePanel(panelLayer.transform, "Panel", panelSprite, Walnut);
            SetCenter(panel.rectTransform, Vector2.zero, new Vector2(1200f, 820f));
            AddOutline(panel.gameObject, WalnutDark, new Vector2(0f, -5f));

            // 안쪽 금색 테두리 라인
            Image rim = CreatePanel(panel.transform, "Rim", panelSprite, new Color(0f, 0f, 0f, 0f));
            SetStretch(rim.rectTransform, new Vector2(14f, 14f), new Vector2(-14f, -14f));
            rim.raycastTarget = false;
            AddOutline(rim.gameObject, new Color(0.85f, 0.68f, 0.38f, 0.7f), new Vector2(2f, -2f));

            // 타이틀 배너
            Image titleBanner = CreatePanel(panel.transform, "TitleBanner", panelSprite, Gold);
            SetCenterTop(titleBanner.rectTransform, new Vector2(0f, -18f), new Vector2(1150f, 96f));
            AddOutline(titleBanner.gameObject, WalnutDark, new Vector2(2f, -2f));
            TMP_Text titleText = CreateText("TitleText", titleBanner.transform, "햄버거 도감", 46f, font, TextAlignmentOptions.Center);
            titleText.color = Cream;

            TMP_Text progressText = CreateText("ProgressText", panel.transform, "0 / 0", 26f, font, TextAlignmentOptions.TopRight);
            SetCenterTop(progressText.rectTransform, new Vector2(-60f, -130f), new Vector2(320f, 44f));
            progressText.color = Cream;

            Image closeButtonImage = CreatePanel(panel.transform, "CloseButton", cardSprite, BrickRed);
            SetTopRight(closeButtonImage.rectTransform, new Vector2(14f, 14f), new Vector2(62f, 62f));
            AddOutline(closeButtonImage.gameObject, WalnutDark, new Vector2(1f, -2f));
            Button closeButton = closeButtonImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButtonImage;
            SetButtonTint(closeButton, closeButtonImage);
            TMP_Text closeLabel = CreateText("Label", closeButtonImage.transform, "X", 28f, font, TextAlignmentOptions.Center);
            closeLabel.color = Cream;

            // 양피지 격자 영역
            Image parchmentArea = CreatePanel(panel.transform, "GridFrame", panelSprite, Parchment);
            SetCenter(parchmentArea.rectTransform, new Vector2(0f, -48f), new Vector2(1120f, 588f));
            AddOutline(parchmentArea.gameObject, WalnutDark, new Vector2(0f, -3f));

            Image scrollView = CreateImage("GridScroll", parchmentArea.transform, null);
            SetStretch(scrollView.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            scrollView.color = new Color(0f, 0f, 0f, 0.04f);
            scrollView.gameObject.AddComponent<RectMask2D>();
            ScrollRect scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            GameObject content = CreateUIObject("Content", scrollView.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(168f, 210f);
            grid.spacing = new Vector2(20f, 20f);
            grid.padding = new RectOffset(22, 22, 22, 22);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = scrollView.rectTransform;
            scrollRect.content = contentRect;

            // ── 상세창 ──
            Image detailShadow = CreatePanel(panel.transform, "DetailShadow", panelSprite, Shadow);
            SetCenter(detailShadow.rectTransform, new Vector2(8f, -12f), new Vector2(800f, 580f));
            CanvasGroup detailCanvasGroup = detailShadow.gameObject.AddComponent<CanvasGroup>();

            Image detail = CreatePanel(detailShadow.transform, "DetailPanel", panelSprite, Parchment);
            SetCenter(detail.rectTransform, new Vector2(-8f, 12f), new Vector2(780f, 560f));
            AddOutline(detail.gameObject, WalnutDark, new Vector2(0f, -4f));

            Image detailHeader = CreatePanel(detail.transform, "DetailHeader", panelSprite, Gold);
            SetCenterTop(detailHeader.rectTransform, new Vector2(0f, -14f), new Vector2(740f, 84f));
            AddOutline(detailHeader.gameObject, WalnutDark, new Vector2(1f, -2f));
            TMP_Text detailName = CreateText("DetailName", detailHeader.transform, "", 38f, font, TextAlignmentOptions.Center);
            detailName.color = Cream;

            TMP_Text detailPrice = CreateText("DetailPrice", detail.transform, "", 28f, font, TextAlignmentOptions.Center);
            SetCenterTop(detailPrice.rectTransform, new Vector2(0f, -112f), new Vector2(700f, 44f));
            detailPrice.color = new Color(0.45f, 0.32f, 0.12f, 1f);

            TMP_Text detailIngredients = CreateText("DetailIngredients", detail.transform, "", 25f, font, TextAlignmentOptions.TopLeft);
            SetCenter(detailIngredients.rectTransform, new Vector2(0f, -24f), new Vector2(640f, 320f));
            detailIngredients.color = Ink;

            Image detailCloseImage = CreatePanel(detail.transform, "DetailCloseButton", cardSprite, Gold);
            SetCenterBottom(detailCloseImage.rectTransform, new Vector2(0f, 22f), new Vector2(200f, 60f));
            AddOutline(detailCloseImage.gameObject, WalnutDark, new Vector2(1f, -2f));
            Button detailClose = detailCloseImage.gameObject.AddComponent<Button>();
            detailClose.targetGraphic = detailCloseImage;
            SetButtonTint(detailClose, detailCloseImage);
            TMP_Text detailCloseLabel = CreateText("Label", detailCloseImage.transform, "닫기", 26f, font, TextAlignmentOptions.Center);
            detailCloseLabel.color = Ink;

            // 패널이 항상 책 버튼 위에 오도록
            panelLayer.transform.SetAsLastSibling();

            // ── 컨트롤러 연결 ──
            RecipeBookLayerController controller = root.AddComponent<RecipeBookLayerController>();
            SerializedObject so = new SerializedObject(controller);
            SetRef(so, "layerCanvasGroup", panelCanvasGroup);
            SetRef(so, "openButton", openButton);
            SetRef(so, "closeButton", closeButton);
            SetRef(so, "backdropButton", backdropButton);
            SetRef(so, "entryGridPlaceholder", contentRect);
            SetRef(so, "entryPrefab", entryPrefabView);
            SetRef(so, "progressText", progressText);
            SetRef(so, "detailCanvasGroup", detailCanvasGroup);
            SetRef(so, "detailNameText", detailName);
            SetRef(so, "detailIngredientsText", detailIngredients);
            SetRef(so, "detailPriceText", detailPrice);
            SetRef(so, "detailCloseButton", detailClose);

            SerializedProperty recipesProp = so.FindProperty("allRecipes");
            recipesProp.arraySize = recipes.Count;
            for (int i = 0; i < recipes.Count; i++)
            {
                recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
            }

            SerializedProperty idsProp = so.FindProperty("testUnlockedIds");
            idsProp.arraySize = 1;
            idsProp.GetArrayElementAtIndex(0).intValue = 1;

            so.ApplyModifiedPropertiesWithoutUndo();

            SetGroupHidden(panelCanvasGroup);
            SetGroupHidden(detailCanvasGroup);

            return root;
        }

        // ────────────────────────────── 스프라이트 생성 ──────────────────────────────

        private static Sprite EnsureRoundedSprite(string key, int radius)
        {
            string path = ArtFolder + "/rounded_" + key + ".png";
            int size = radius * 2 + 8;

            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, RoundedAlpha(x, y, size, radius)));
                    }
                }

                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.textureType = TextureImporterType.Sprite;
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = new Vector4(radius, radius, radius, radius);
                settings.spritePixelsPerUnit = 100f;
                settings.spriteExtrude = 0;
                settings.alphaIsTransparency = true;
                settings.mipmapEnabled = false;
                importer.SetTextureSettings(settings);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float RoundedAlpha(int x, int y, int size, int radius)
        {
            float fx = x + 0.5f;
            float fy = y + 0.5f;
            float nearestX = Mathf.Clamp(fx, radius, size - radius);
            float nearestY = Mathf.Clamp(fy, radius, size - radius);
            float distance = Mathf.Sqrt((fx - nearestX) * (fx - nearestX) + (fy - nearestY) * (fy - nearestY));
            return Mathf.Clamp01(radius - distance + 0.5f);
        }

        // ────────────────────────────── UI 헬퍼 ──────────────────────────────

        private static Image CreatePanel(Transform parent, string name, Sprite rounded, Color color)
        {
            Image image = CreateImage(name, parent, rounded);
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void SetButtonTint(Button button, Graphic target)
        {
            button.targetGraphic = target;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private static void SetGroupHidden(CanvasGroup group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[RecipeBookLayerBuilder] 프로퍼티를 못 찾음: {propertyName}");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static List<CoreRecipeData> FindAllCoreRecipes()
        {
            List<CoreRecipeData> result = new List<CoreRecipeData>();

            string[] filters =
            {
                "t:" + typeof(CoreRecipeData).FullName,
                "t:RecipeData",
                "t:ScriptableObject",
            };

            foreach (string filter in filters)
            {
                foreach (string guid in AssetDatabase.FindAssets(filter))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    CoreRecipeData recipe = AssetDatabase.LoadAssetAtPath<CoreRecipeData>(path);
                    if (recipe != null && !result.Contains(recipe))
                    {
                        result.Add(recipe);
                    }
                }

                if (result.Count > 0)
                {
                    break;
                }
            }

            result.Sort((a, b) => a.id.CompareTo(b.id));
            return result;
        }

        private static TMP_FontAsset ResolveFont()
        {
            string[] candidates =
            {
                "Assets/Fonts/NanumGothic SDF.asset",
                "Assets/TextMesh Pro/Resources/Fonts & Materials/Shop Korean SDF.asset",
            };

            foreach (string path in candidates)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null)
                {
                    return font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static TMP_Text CreateText(string name,
                                           Transform parent,
                                           string text,
                                           float fontSize,
                                           TMP_FontAsset font,
                                           TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(name, parent);
            SetStretch(textObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (font != null)
            {
                label.font = font;
            }

            label.fontSize = fontSize;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.color = Color.white;
            return label;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = true;
            return image;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetSize(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }

        private static void SetCenter(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetCenterTop(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetCenterBottom(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetTopRight(RectTransform rect, Vector2 margin, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-margin.x, -margin.y);
            rect.sizeDelta = size;
        }
    }
}
