using System.Collections.Generic;
using System.Linq;
using SheepSheepBurger.Counter;
using SheepSheepBurger.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SheepSheepBurger.EditorTools
{
    public static class CounterShortcutButtonsInstaller
    {
        private const string CounterScenePath = "Assets/Scenes/Counter.unity";
        private const string ShopScenePath = "Assets/Scenes/ShopScene.unity";
        private const string SettingsLayerPrefabPath = "Assets/Prefabs/SettingsLayer.prefab";
        private const string ButtonSpritePath = "Assets/Scenes/Square.png";
        private const string ShortcutObjectName = "CounterShortcutButtons";
        private const string ShopButtonName = "shop button";
        private const string SettingsButtonName = "setting button";

        private static readonly Vector3 ShopButtonPosition = new Vector3(6.42f, 4.21f, 0f);
        private static readonly Vector3 SettingsButtonPosition = new Vector3(7.81f, 4.21f, 0f);

        [MenuItem("SheepSheep/Wire Counter Shortcut Buttons")]
        public static void WireCounterShortcutButtons()
        {
            Scene scene = EditorSceneManager.OpenScene(CounterScenePath, OpenSceneMode.Single);

            Sprite buttonSprite = LoadButtonSprite();
            SpriteRenderer shopButton = FindOrCreateShortcutButton(ShopButtonName, ShopButtonPosition, buttonSprite);
            SpriteRenderer settingsButton = FindOrCreateShortcutButton(SettingsButtonName, SettingsButtonPosition, buttonSprite);
            SettingsLayerController settingsLayer = FindOrCreateSettingsLayer(scene);
            CounterShortcutButtons shortcutButtons = FindOrCreateShortcutController();

            SerializedObject serialized = new SerializedObject(shortcutButtons);
            serialized.FindProperty("targetCamera").objectReferenceValue = Camera.main;
            serialized.FindProperty("shopButtonRenderer").objectReferenceValue = shopButton;
            serialized.FindProperty("settingsButtonRenderer").objectReferenceValue = settingsButton;
            serialized.FindProperty("shopSceneName").stringValue = "ShopScene";
            serialized.FindProperty("settingsLayer").objectReferenceValue = settingsLayer;
            serialized.FindProperty("settingsLayerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(SettingsLayerPrefabPath);
            serialized.FindProperty("closeSettingsLayerOnStart").boolValue = true;
            serialized.FindProperty("showBuiltInSettingsButtonOnlyWhenLayerOpen").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureSceneInBuildSettings(CounterScenePath);
            EnsureSceneInBuildSettings(ShopScenePath);

            EditorUtility.SetDirty(shortcutButtons);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CounterShortcutButtonsInstaller] Counter shortcut buttons are wired.");
            EditorGUIUtility.PingObject(shortcutButtons);
        }

        private static Sprite LoadButtonSprite()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ButtonSpritePath);
            Sprite sprite = assets.OfType<Sprite>().FirstOrDefault();

            if (sprite == null)
            {
                Debug.LogWarning($"[CounterShortcutButtonsInstaller] Button sprite not found: {ButtonSpritePath}");
            }

            return sprite;
        }

        private static SpriteRenderer FindOrCreateShortcutButton(string objectName, Vector3 position, Sprite sprite)
        {
            GameObject buttonObject = GameObject.Find(objectName);

            if (buttonObject == null)
            {
                buttonObject = new GameObject(objectName);
                buttonObject.transform.position = position;
            }

            SpriteRenderer renderer = buttonObject.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer = buttonObject.AddComponent<SpriteRenderer>();
            }

            if (sprite != null)
            {
                renderer.sprite = sprite;
            }

            buttonObject.transform.position = position;
            buttonObject.transform.localScale = Vector3.one;
            renderer.color = Color.white;
            renderer.sortingOrder = 30;
            EditorUtility.SetDirty(buttonObject);
            return renderer;
        }

        private static SettingsLayerController FindOrCreateSettingsLayer(Scene scene)
        {
            SettingsLayerController existing =
                Object.FindFirstObjectByType<SettingsLayerController>(FindObjectsInactive.Include);

            if (existing != null)
            {
                return existing;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsLayerPrefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[CounterShortcutButtonsInstaller] Settings layer prefab not found: {SettingsLayerPrefabPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "SettingsLayer";
            SettingsLayerController controller = instance.GetComponent<SettingsLayerController>();
            controller?.Close();
            return controller;
        }

        private static CounterShortcutButtons FindOrCreateShortcutController()
        {
            GameObject shortcutObject = GameObject.Find(ShortcutObjectName);

            if (shortcutObject == null)
            {
                shortcutObject = new GameObject(ShortcutObjectName);
            }

            CounterShortcutButtons shortcutButtons = shortcutObject.GetComponent<CounterShortcutButtons>();

            if (shortcutButtons == null)
            {
                shortcutButtons = shortcutObject.AddComponent<CounterShortcutButtons>();
            }

            return shortcutButtons;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
            {
                Debug.LogWarning($"[CounterShortcutButtonsInstaller] Scene not found: {scenePath}");
                return;
            }

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
