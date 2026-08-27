using System.IO;
using SheepSheepBurger.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.EditorTools
{
    public static class SettingsLayerBuilder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string DataFolder = "Assets/Data";
        private const string SettingsDataFolder = DataFolder + "/Settings";
        private const string SettingsArtFolder = "Assets/GameAssets/Settings/UI";
        private const string SettingsDesignPresetPath = SettingsDataFolder + "/SettingsLayerDesignPreset.asset";
        private const string SettingsLayerPrefabPath = PrefabsFolder + "/SettingsLayer.prefab";
        private const string CircleSpritePath = SettingsArtFolder + "/settings_circle_placeholder.png";
        private const string ShopFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Shop Korean SDF.asset";
        private const string ExistingKoreanFontAssetPath = "Assets/@Developers/Lee/Fonts/NanumGothic SDF.asset";
        private const string SettingsFontCharacters = "설정0123456789";

        [MenuItem("SheepSheep/Build Settings Layer Prefab")]
        public static void BuildSettingsLayerPrefab()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(SettingsDataFolder);
            EnsureFolder(SettingsArtFolder);

            TMP_FontAsset fontAsset = EnsureSettingsFontAsset();
            Sprite circleSprite = EnsureCircleSprite();
            SettingsLayerDesignPreset designPreset = EnsureDesignPreset(fontAsset);

            GameObject root = CreateSettingsLayer(designPreset, circleSprite);
            PrefabUtility.SaveAsPrefabAsset(root, SettingsLayerPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SettingsLayerBuilder] 완료. 프리팹: {SettingsLayerPrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(SettingsLayerPrefabPath));
        }

        private static GameObject CreateSettingsLayer(SettingsLayerDesignPreset designPreset, Sprite circleSprite)
        {
            SettingsLayerDesignPreset.LayoutSettings layout = designPreset.layout;

            GameObject root = CreateUIObject("SettingsLayer", null);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect, Vector2.zero, Vector2.zero);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = designPreset.canvasSortingOrder;

            CanvasScaler canvasScaler = root.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = designPreset.referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = designPreset.matchWidthOrHeight;

            root.AddComponent<GraphicRaycaster>();

            GameObject panelLayer = CreateUIObject("SettingsPanelLayer", root.transform);
            RectTransform panelLayerRect = panelLayer.GetComponent<RectTransform>();
            SetStretch(panelLayerRect, Vector2.zero, Vector2.zero);
            CanvasGroup panelCanvasGroup = panelLayer.AddComponent<CanvasGroup>();

            Image backdrop = CreateImage("Backdrop", panelLayer.transform, null);
            SetStretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);

            Image panelBackground = CreateImage("Panel", panelLayer.transform, null);
            RectTransform panelRect = panelBackground.rectTransform;
            SetCenter(panelRect, layout.panelAnchoredPosition, layout.panelSize);
            Outline panelOutline = panelBackground.gameObject.AddComponent<Outline>();

            Image innerBorderImage = CreateImage("InnerBorder", panelRect, null);
            RectTransform innerBorderRect = innerBorderImage.rectTransform;
            SetStretch(innerBorderRect, layout.innerBorderInset, -layout.innerBorderInset);
            Outline innerBorderOutline = innerBorderImage.gameObject.AddComponent<Outline>();

            Image titleBackground = CreateImage("TitleBanner", panelRect, null);
            RectTransform titleRect = titleBackground.rectTransform;
            SetCenter(titleRect, layout.titleAnchoredPosition, layout.titleSize);
            Outline titleOutline = titleBackground.gameObject.AddComponent<Outline>();
            TMP_Text titleText = CreateText("TitleText", titleRect, "설정", designPreset.text.titleFontSize, designPreset.fontAsset);

            SettingsRowParts bgmRow = CreateSettingsRow("BGM", panelRect, layout.rowStartY, layout, circleSprite, designPreset);
            SettingsRowParts sfxRow = CreateSettingsRow("SFX", panelRect, layout.rowStartY - layout.rowSpacing, layout, circleSprite, designPreset);

            Button settingsButton = CreateCircleButton("SettingsButton", root.transform, circleSprite, "설정", designPreset);
            RectTransform settingsButtonRect = settingsButton.GetComponent<RectTransform>();
            SetTopRight(settingsButtonRect, layout.settingsButtonMargin, layout.settingsButtonSize);
            Image settingsButtonBackground = settingsButton.GetComponent<Image>();
            Outline settingsButtonOutline = settingsButton.gameObject.GetComponent<Outline>();
            TMP_Text settingsButtonText = settingsButton.GetComponentInChildren<TMP_Text>();
            settingsButton.gameObject.SetActive(false);

            SettingsLayerController controller = root.AddComponent<SettingsLayerController>();
            controller.Bind(panelCanvasGroup,
                            settingsButton,
                            bgmRow.Slider,
                            bgmRow.ValueText,
                            sfxRow.Slider,
                            sfxRow.ValueText);

            SettingsLayerDesignPresenter presenter = root.AddComponent<SettingsLayerDesignPresenter>();
            presenter.Bind(designPreset,
                           canvas,
                           canvasScaler,
                           backdrop,
                           settingsButtonRect,
                           settingsButton,
                           settingsButtonBackground,
                           settingsButtonOutline,
                           settingsButtonText,
                           panelRect,
                           panelBackground,
                           panelOutline,
                           innerBorderRect,
                           innerBorderImage,
                           innerBorderOutline,
                           titleRect,
                           titleBackground,
                           titleOutline,
                           titleText,
                           bgmRow.IconRect,
                           bgmRow.IconImage,
                           bgmRow.IconOutline,
                           bgmRow.SliderRoot,
                           bgmRow.TrackImage,
                           bgmRow.TrackRect,
                           bgmRow.FillImage,
                           bgmRow.HandleImage,
                           bgmRow.HandleOutline,
                           bgmRow.ValueBoxRect,
                           bgmRow.ValueBoxImage,
                           bgmRow.ValueBoxOutline,
                           bgmRow.ValueText,
                           sfxRow.IconRect,
                           sfxRow.IconImage,
                           sfxRow.IconOutline,
                           sfxRow.SliderRoot,
                           sfxRow.TrackImage,
                           sfxRow.TrackRect,
                           sfxRow.FillImage,
                           sfxRow.HandleImage,
                           sfxRow.HandleOutline,
                           sfxRow.ValueBoxRect,
                           sfxRow.ValueBoxImage,
                           sfxRow.ValueBoxOutline,
                           sfxRow.ValueText);

            presenter.ApplyDesign();
            controller.SetVisible(false);

            return root;
        }

        private static SettingsRowParts CreateSettingsRow(string rowName,
                                                          Transform parent,
                                                          float rowY,
                                                          SettingsLayerDesignPreset.LayoutSettings layout,
                                                          Sprite circleSprite,
                                                          SettingsLayerDesignPreset designPreset)
        {
            Image iconImage = CreateImage(rowName + "IconPlaceholder", parent, circleSprite);
            RectTransform iconRect = iconImage.rectTransform;
            SetCenter(iconRect, new Vector2(layout.iconX, rowY), Vector2.one * layout.iconSize);
            Outline iconOutline = iconImage.gameObject.AddComponent<Outline>();

            GameObject sliderObject = CreateUIObject(rowName + "VolumeSlider", parent);
            RectTransform sliderRoot = sliderObject.GetComponent<RectTransform>();
            SetCenter(sliderRoot, new Vector2(layout.sliderX, rowY), layout.sliderSize);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = SettingsLayerController.MinVolume;
            slider.maxValue = SettingsLayerController.MaxVolume;
            slider.wholeNumbers = true;
            slider.value = SettingsLayerController.MaxVolume;

            Image trackImage = CreateImage("Track", sliderRoot, null);
            RectTransform trackRect = trackImage.rectTransform;
            SetCenter(trackRect, Vector2.zero, new Vector2(layout.sliderSize.x, layout.sliderTrackHeight));

            RectTransform fillArea = CreateUIObject("Fill Area", sliderRoot).GetComponent<RectTransform>();
            SetStretch(fillArea, new Vector2(0f, 0f), Vector2.zero);
            fillArea.offsetMin = new Vector2(0f, 0f);
            fillArea.offsetMax = new Vector2(0f, 0f);

            Image fillImage = CreateImage("Fill", fillArea, null);
            RectTransform fillRect = fillImage.rectTransform;
            SetStretch(fillRect, Vector2.zero, Vector2.zero);

            RectTransform handleSlideArea = CreateUIObject("Handle Slide Area", sliderRoot).GetComponent<RectTransform>();
            SetStretch(handleSlideArea, Vector2.zero, Vector2.zero);

            Image handleImage = CreateImage("Handle", handleSlideArea, circleSprite);
            RectTransform handleRect = handleImage.rectTransform;
            SetCenter(handleRect, Vector2.zero, Vector2.one * layout.sliderHandleSize);
            Outline handleOutline = handleImage.gameObject.AddComponent<Outline>();

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;

            Image valueBoxImage = CreateImage(rowName + "ValueBox", parent, null);
            RectTransform valueBoxRect = valueBoxImage.rectTransform;
            SetCenter(valueBoxRect, new Vector2(layout.valueBoxX, rowY), layout.valueBoxSize);
            Outline valueBoxOutline = valueBoxImage.gameObject.AddComponent<Outline>();
            TMP_Text valueText = CreateText("ValueText", valueBoxRect, "10", designPreset.text.valueFontSize, designPreset.fontAsset);

            return new SettingsRowParts
            {
                IconRect = iconRect,
                IconImage = iconImage,
                IconOutline = iconOutline,
                Slider = slider,
                SliderRoot = sliderRoot,
                TrackImage = trackImage,
                TrackRect = trackRect,
                FillImage = fillImage,
                HandleImage = handleImage,
                HandleOutline = handleOutline,
                ValueBoxRect = valueBoxRect,
                ValueBoxImage = valueBoxImage,
                ValueBoxOutline = valueBoxOutline,
                ValueText = valueText
            };
        }

        private static Button CreateCircleButton(string name,
                                                 Transform parent,
                                                 Sprite circleSprite,
                                                 string label,
                                                 SettingsLayerDesignPreset designPreset)
        {
            Image image = CreateImage(name, parent, circleSprite);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            image.gameObject.AddComponent<Outline>();
            CreateText("Label", image.rectTransform, label, designPreset.text.settingsButtonFontSize, designPreset.fontAsset);
            return button;
        }

        private static TMP_Text CreateText(string name,
                                           Transform parent,
                                           string text,
                                           float fontSize,
                                           TMP_FontAsset fontAsset)
        {
            GameObject textObject = CreateUIObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetStretch(rect, Vector2.zero, Vector2.zero);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = fontAsset;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enableAutoSizing = false;
            return label;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = sprite != null;
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

        private static SettingsLayerDesignPreset EnsureDesignPreset(TMP_FontAsset fontAsset)
        {
            SettingsLayerDesignPreset preset =
                AssetDatabase.LoadAssetAtPath<SettingsLayerDesignPreset>(SettingsDesignPresetPath);

            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<SettingsLayerDesignPreset>();
                AssetDatabase.CreateAsset(preset, SettingsDesignPresetPath);
            }

            if (preset.fontAsset == null)
            {
                preset.fontAsset = fontAsset;
            }

            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static TMP_FontAsset EnsureSettingsFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ShopFontAssetPath);

            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ExistingKoreanFontAssetPath);
            }

            if (fontAsset == null)
            {
                return null;
            }

            _ = fontAsset.characterLookupTable;

            if (!fontAsset.HasCharacters(SettingsFontCharacters, out _))
            {
                AtlasPopulationMode originalMode = fontAsset.atlasPopulationMode;
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                if (!fontAsset.TryAddCharacters(SettingsFontCharacters, out string missingCharacters))
                {
                    Debug.LogWarning($"[SettingsLayerBuilder] 설정 폰트에 넣지 못한 문자가 있습니다: {missingCharacters}");
                }

                fontAsset.atlasPopulationMode = originalMode;
                EditorUtility.SetDirty(fontAsset);
            }

            return fontAsset;
        }

        private static Sprite EnsureCircleSprite()
        {
            if (!File.Exists(CircleSpritePath))
            {
                const int size = 256;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = (size - 2) * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                File.WriteAllBytes(CircleSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(CircleSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureSpriteImporter(CircleSpritePath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                return;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f))
            {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
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

        private static void SetCenter(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
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

        private struct SettingsRowParts
        {
            public RectTransform IconRect;
            public Image IconImage;
            public Outline IconOutline;
            public Slider Slider;
            public RectTransform SliderRoot;
            public Image TrackImage;
            public RectTransform TrackRect;
            public Image FillImage;
            public Image HandleImage;
            public Outline HandleOutline;
            public RectTransform ValueBoxRect;
            public Image ValueBoxImage;
            public Outline ValueBoxOutline;
            public TMP_Text ValueText;
        }
    }
}
