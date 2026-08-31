using System.IO;
using SheepSheepBurger.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SheepSheepBurger.EditorTools
{
    public static class SettingsLayerBuilder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string SettingsArtFolder = "Assets/GameAssets/Settings/UI/OptionArt";
        private const string SettingsLayerPrefabPath = PrefabsFolder + "/SettingsLayer.prefab";

        private const string BackgroundPath = SettingsArtFolder + "/settings_panel_background.png";
        private const string BgmButtonPath = SettingsArtFolder + "/bgm_button.png";
        private const string BgmIconPath = SettingsArtFolder + "/bgm_icon.png";
        private const string BgmBarPath = SettingsArtFolder + "/bgm_bar.png";
        private const string BgmHandlePath = SettingsArtFolder + "/bgm_handle.png";
        private const string SfxButtonPath = SettingsArtFolder + "/sfx_button.png";
        private const string SfxIconPath = SettingsArtFolder + "/sfx_icon.png";
        private const string SfxBarPath = SettingsArtFolder + "/sfx_bar.png";
        private const string SfxHandlePath = SettingsArtFolder + "/sfx_handle.png";
        private const string ExitButtonPath = SettingsArtFolder + "/exit_to_start_button.png";

        private static readonly Vector2 ArtCanvasSize = new Vector2(2360f, 1640f);

        [MenuItem("SheepSheep/Build Settings Layer Prefab")]
        public static void BuildSettingsLayerPrefab()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(SettingsArtFolder);

            SettingsArtSprites art = LoadArtSprites();
            if (!art.IsComplete)
            {
                Debug.LogError("[SettingsLayerBuilder] One or more settings art sprites are missing.");
                return;
            }

            GameObject root = CreateSettingsLayer(art);
            PrefabUtility.SaveAsPrefabAsset(root, SettingsLayerPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SettingsLayerBuilder] Art settings prefab built: {SettingsLayerPrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(SettingsLayerPrefabPath));
        }

        private static GameObject CreateSettingsLayer(SettingsArtSprites art)
        {
            GameObject root = CreateUIObject("SettingsLayer", null);
            SetStretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 650;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ArtCanvasSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();

            GameObject panelLayer = CreateUIObject("SettingsPanelLayer", root.transform);
            SetStretch(panelLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            CanvasGroup panelCanvasGroup = panelLayer.AddComponent<CanvasGroup>();

            Image backdrop = CreateImage("Backdrop", panelLayer.transform, null, true);
            SetStretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.color = new Color(0.08f, 0.06f, 0.04f, 0.5f);

            RectTransform artRoot = CreateUIObject("FixedArtCanvas", panelLayer.transform).GetComponent<RectTransform>();
            SetCenter(artRoot, Vector2.zero, ArtCanvasSize);

            CreateFullCanvasArt("SettingsPanelBackground", artRoot, art.Background);
            CreateFullCanvasArt("BgmButtonArt", artRoot, art.BgmButton);
            CreateFullCanvasArt("BgmIconArt", artRoot, art.BgmIcon);
            CreateFullCanvasArt("BgmBarArt", artRoot, art.BgmBar);
            Image bgmHandleArt = CreateFullCanvasArt("BgmHandleArt", artRoot, art.BgmHandle);
            CreateFullCanvasArt("SfxButtonArt", artRoot, art.SfxButton);
            CreateFullCanvasArt("SfxIconArt", artRoot, art.SfxIcon);
            CreateFullCanvasArt("SfxBarArt", artRoot, art.SfxBar);
            Image sfxHandleArt = CreateFullCanvasArt("SfxHandleArt", artRoot, art.SfxHandle);
            CreateFullCanvasArt("ExitToStartButtonArt", artRoot, art.ExitButton);

            Slider bgmSlider = CreateInvisibleSlider(
                "BgmVolumeSlider",
                artRoot,
                new Rect(905f, 720f, 894f, 140f));
            Slider sfxSlider = CreateInvisibleSlider(
                "SfxVolumeSlider",
                artRoot,
                new Rect(899f, 1000f, 894f, 145f));
            Button exitButton = CreateInvisibleButton(
                "ExitToStartButton",
                artRoot,
                new Rect(1714f, 1180f, 147f, 174f));

            SettingsLayerController controller = root.AddComponent<SettingsLayerController>();
            controller.Bind(
                panelCanvasGroup,
                null,
                bgmSlider,
                null,
                sfxSlider,
                null,
                exitButton);

            SettingsArtSliderHandle bgmHandle = bgmHandleArt.gameObject.AddComponent<SettingsArtSliderHandle>();
            bgmHandle.Configure(bgmSlider, bgmHandleArt.rectTransform, 1041f, 949f, 1755f);

            SettingsArtSliderHandle sfxHandle = sfxHandleArt.gameObject.AddComponent<SettingsArtSliderHandle>();
            sfxHandle.Configure(sfxSlider, sfxHandleArt.rectTransform, 1035f, 943f, 1749f);

            controller.SetVisible(false);
            return root;
        }

        private static SettingsArtSprites LoadArtSprites()
        {
            return new SettingsArtSprites
            {
                Background = LoadArtSprite(BackgroundPath),
                BgmButton = LoadArtSprite(BgmButtonPath),
                BgmIcon = LoadArtSprite(BgmIconPath),
                BgmBar = LoadArtSprite(BgmBarPath),
                BgmHandle = LoadArtSprite(BgmHandlePath),
                SfxButton = LoadArtSprite(SfxButtonPath),
                SfxIcon = LoadArtSprite(SfxIconPath),
                SfxBar = LoadArtSprite(SfxBarPath),
                SfxHandle = LoadArtSprite(SfxHandlePath),
                ExitButton = LoadArtSprite(ExitButtonPath)
            };
        }

        private static Sprite LoadArtSprite(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[SettingsLayerBuilder] Missing settings art: {path}");
                return null;
            }

            ConfigureSpriteImporter(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Image CreateFullCanvasArt(string name, Transform parent, Sprite sprite)
        {
            Image image = CreateImage(name, parent, sprite, false);
            SetCenter(image.rectTransform, Vector2.zero, ArtCanvasSize);
            image.preserveAspect = false;
            image.color = Color.white;
            return image;
        }

        private static Slider CreateInvisibleSlider(string name, Transform parent, Rect pixelBounds)
        {
            Image hitArea = CreateImage(name, parent, null, true);
            SetFromTopLeftPixels(hitArea.rectTransform, pixelBounds);
            hitArea.color = new Color(1f, 1f, 1f, 0.002f);

            Slider slider = hitArea.gameObject.AddComponent<Slider>();
            slider.minValue = SettingsLayerController.MinVolume;
            slider.maxValue = SettingsLayerController.MaxVolume;
            slider.wholeNumbers = true;
            slider.value = SettingsLayerController.MaxVolume;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.targetGraphic = hitArea;

            RectTransform slideArea = CreateUIObject("Handle Slide Area", hitArea.transform).GetComponent<RectTransform>();
            SetStretch(slideArea, Vector2.zero, Vector2.zero);
            Image interactionHandle = CreateImage("InteractionHandle", slideArea, null, false);
            SetCenter(interactionHandle.rectTransform, Vector2.zero, Vector2.one);
            interactionHandle.color = Color.clear;
            slider.handleRect = interactionHandle.rectTransform;

            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;
            return slider;
        }

        private static Button CreateInvisibleButton(string name, Transform parent, Rect pixelBounds)
        {
            Image hitArea = CreateImage(name, parent, null, true);
            SetFromTopLeftPixels(hitArea.rectTransform, pixelBounds);
            hitArea.color = new Color(1f, 1f, 1f, 0.002f);
            Button button = hitArea.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void ConfigureSpriteImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
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

        private static void SetFromTopLeftPixels(RectTransform rect, Rect bounds)
        {
            Vector2 center = new Vector2(
                bounds.center.x - ArtCanvasSize.x * 0.5f,
                ArtCanvasSize.y * 0.5f - bounds.center.y);
            SetCenter(rect, center, bounds.size);
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

        private struct SettingsArtSprites
        {
            public Sprite Background;
            public Sprite BgmButton;
            public Sprite BgmIcon;
            public Sprite BgmBar;
            public Sprite BgmHandle;
            public Sprite SfxButton;
            public Sprite SfxIcon;
            public Sprite SfxBar;
            public Sprite SfxHandle;
            public Sprite ExitButton;

            public bool IsComplete =>
                Background != null &&
                BgmButton != null &&
                BgmIcon != null &&
                BgmBar != null &&
                BgmHandle != null &&
                SfxButton != null &&
                SfxIcon != null &&
                SfxBar != null &&
                SfxHandle != null &&
                ExitButton != null;
        }
    }
}
