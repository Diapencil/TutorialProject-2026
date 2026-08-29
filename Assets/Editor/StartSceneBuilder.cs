using System;
using System.Collections.Generic;
using System.Linq;
using SheepSheepBurger.Start;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SheepSheepBurger.Start.Editor
{
    public static class StartSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/StartScene.unity";
        private const string CounterBackgroundPath = "Assets/Sprites/Environment/counter.png";
        private const string BurgerHeroPath = "Assets/Sprites/ProvidedArt/burger_complete.png";
        private const string RoundedRectanglePath = "Assets/Sprites/UI/rounded_rectangle.png";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Shop Korean SDF.asset";

        [MenuItem("Sheep Sheep Burger/Build Start Scene")]
        public static void BuildStartScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StartScene";

            Camera mainCamera = CreateCamera();
            RectTransform canvasRoot = CreateCanvas(mainCamera);
            Sprite roundedRectangle = LoadAsset<Sprite>(RoundedRectanglePath);
            TMP_FontAsset font = LoadAsset<TMP_FontAsset>(FontPath);

            Image background = CreateImage(
                "CounterBackground",
                canvasRoot,
                LoadAsset<Sprite>(CounterBackgroundPath),
                Color.white,
                Vector2.zero,
                Vector2.zero,
                false);
            Stretch(background.rectTransform);

            Image boardPanel = CreateImage(
                "TitleBoardPanel",
                canvasRoot,
                roundedRectangle,
                new Color(0.105f, 0.18f, 0.13f, 0.9f),
                new Vector2(-205f, 95f),
                new Vector2(930f, 540f),
                false);
            AddShadow(boardPanel.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(9f, -9f));

            TMP_Text titleText = CreateText(
                "GameTitleText",
                boardPanel.rectTransform,
                font,
                "SHEEP SHEEP BURGER",
                68f,
                FontStyles.Bold,
                new Color(1f, 0.93f, 0.72f, 1f),
                new Vector2(0f, 155f),
                new Vector2(820f, 105f));
            AddShadow(titleText.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(3f, -3f));

            TMP_Text subtitleText = CreateText(
                "SubtitleText",
                boardPanel.rectTransform,
                font,
                "오늘도 맛있는 하루를 시작해 볼까요?",
                27f,
                FontStyles.Normal,
                new Color(0.88f, 0.92f, 0.78f, 1f),
                new Vector2(-105f, 67f),
                new Vector2(560f, 52f));

            Image burgerHero = CreateImage(
                "BurgerHero",
                boardPanel.rectTransform,
                LoadAsset<Sprite>(BurgerHeroPath),
                Color.white,
                new Vector2(270f, -65f),
                new Vector2(285f, 160f),
                true);
            burgerHero.raycastTarget = false;

            Button startButton = CreateButton(
                "StartGameButton",
                boardPanel.rectTransform,
                roundedRectangle,
                new Vector2(-135f, -82f),
                new Vector2(360f, 98f));
            TMP_Text startButtonText = CreateText(
                "StartGameButtonText",
                startButton.transform as RectTransform,
                font,
                "게임 시작",
                34f,
                FontStyles.Bold,
                new Color(0.18f, 0.12f, 0.075f, 1f),
                Vector2.zero,
                Vector2.zero);
            Stretch(startButtonText.rectTransform);

            CreateText(
                "TemporaryTitleHint",
                boardPanel.rectTransform,
                font,
                "TEMPORARY TITLE  •  PROTOTYPE",
                16f,
                FontStyles.Normal,
                new Color(0.82f, 0.84f, 0.7f, 0.72f),
                new Vector2(0f, -224f),
                new Vector2(620f, 34f));

            GameObject controllerObject = new GameObject("StartSceneController", typeof(StartSceneController));
            controllerObject.GetComponent<StartSceneController>().Configure(
                startButton,
                titleText,
                subtitleText,
                startButtonText);

            CreateEventSystem();
            AddStartSceneFirstInBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[StartScene] Editable start scene built and added as build index 0.");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.055f, 0.04f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            return camera;
        }

        private static RectTransform CreateCanvas(Camera mainCamera)
        {
            GameObject canvasObject = new GameObject(
                "StartCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = 10f;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject.GetComponent<RectTransform>();
        }

        private static Image CreateImage(
            string name,
            RectTransform parent,
            Sprite sprite,
            Color color,
            Vector2 position,
            Vector2 size,
            bool preserveAspect)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, position, size);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            Sprite sprite,
            Vector2 position,
            Vector2 size)
        {
            Image image = CreateImage(
                name,
                parent,
                sprite,
                new Color(0.96f, 0.62f, 0.26f, 1f),
                position,
                size,
                false);
            image.raycastTarget = true;
            AddShadow(image.gameObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(6f, -6f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.76f, 1f);
            colors.pressedColor = new Color(0.8f, 0.68f, 0.55f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            return button;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            eventSystem.firstSelectedGameObject = GameObject.Find("StartGameButton");
            InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static void AddStartSceneFirstInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static T LoadAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null
                ? asset
                : throw new InvalidOperationException("Missing asset: " + path);
        }
    }
}
